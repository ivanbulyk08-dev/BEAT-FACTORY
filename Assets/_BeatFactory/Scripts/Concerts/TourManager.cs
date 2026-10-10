using System;
using System.Collections.Generic;

namespace BeatFactory
{
    /// <summary>What a tour card promises.</summary>
    public struct TourQuote
    {
        public TourData data;
        public string title;
        public string titleName;
        public int shows;
        public float hours;
        public int capacity;
        public int attendance;
        public double revenue;
        public double fans;
        public float popularity;
        public int xp;
        public double cost;
        public bool albumBonus;
        public string albumTitle;
        public bool unlocked;
        public string lockReason;
    }

    /// <summary>
    /// Tours run in real time (UTC), so they keep going while the game is closed. Rewards are fixed at the start and
    /// paid when the player collects the finished tour. A NEW ALBUM gives the next tour +25% revenue and +40% fans.
    /// </summary>
    public class TourManager
    {
        public const double AlbumRevenueBonus = 1.25;
        public const double AlbumFansBonus = 1.4;

        readonly GameManager game;
        readonly ConcertManager concerts;
        readonly List<ActiveTour> tours = new List<ActiveTour>();
        readonly HashSet<ActiveTour> announced = new HashSet<ActiveTour>();
        readonly Random rnd = new Random();
        float checkTimer;

        public event Action<ActiveTour> TourStarted;
        /// <summary>A tour reached its last show (also fired after loading for tours that finished while away).</summary>
        public event Action<ActiveTour> TourFinished;
        public event Action<ActiveTour> TourCollected;

        public TourManager(GameManager game, ConcertManager concerts)
        {
            this.game = game;
            this.concerts = concerts;
        }

        public IReadOnlyList<ActiveTour> Active => tours;

        public ActiveTour ActiveFor(ArtistData a)
        {
            if (a == null) return null;
            foreach (var t in tours) if (t.artistId == a.id) return t;
            return null;
        }

        public List<ActiveTour> Finished()
        {
            var now = DateTime.UtcNow;
            return tours.FindAll(t => t.IsFinished(now));
        }

        public TourQuote Quote(ArtistData a, TourKind kind)
        {
            var d = Tours.Get(kind);
            var ctx = concerts.Context(a);
            var c = ctx.career;
            var venue = Venues.Get(d.venue);
            var show = ConcertRewardCalculator.Quote(ctx, venue, d.capacityScale);
            bool album = !string.IsNullOrEmpty(c.newAlbumId);

            var q = new TourQuote
            {
                data = d,
                title = Tours.Title(a, kind, album ? c.newAlbumTitle : null),
                titleName = Tours.TitleName(a, kind, album ? c.newAlbumTitle : null),
                shows = d.shows,
                hours = d.hours,
                capacity = show.capacity * d.shows,
                attendance = show.expectedAttendance * d.shows,
                albumBonus = album,
                albumTitle = c.newAlbumTitle,
            };
            q.revenue = Math.Round(show.TotalRevenue * d.shows * 0.9 * (album ? AlbumRevenueBonus : 1));
            q.fans = Math.Round(show.fans * d.shows * 0.8 * (album ? AlbumFansBonus : 1));
            q.popularity = show.popularity * d.shows * 0.5f;
            q.xp = (int)Math.Round(show.xp * d.shows * 0.7);
            q.cost = ConcertRewardCalculator.RoundPrice(q.revenue * d.costShare);

            q.unlocked = true;
            if (!show.unlocked) { q.unlocked = false; q.lockReason = Loc.F("tour.lock_venue", "venue", venue.Name, "reason", show.lockShort); }
            else if (c.ConcertLevel < d.requiredConcertLevel) { q.unlocked = false; q.lockReason = Loc.F("tour.lock_level", "level", d.requiredConcertLevel); }
            else if (c.popularity < d.requiredPopularity) { q.unlocked = false; q.lockReason = Loc.F("concert.lock_popularity_short", "value", NumberFormatter.Percent(d.requiredPopularity)); }
            else if (game.Studios.Level < d.requiredStudio) { q.unlocked = false; q.lockReason = Loc.F("common.needs", "what", game.Studios.Get(d.requiredStudio)?.DisplayName ?? Loc.T("common.better_studio")); }
            return q;
        }

        public bool CanStart(ArtistData a, TourKind kind)
        {
            if (concerts.Block(a) != ConcertManager.BlockKind.None) return false;
            var q = Quote(a, kind);
            return q.unlocked && game.Economy.CanAfford(q.cost);
        }

        public ActiveTour Start(ArtistData a, TourKind kind)
        {
            if (!CanStart(a, kind)) return null;
            var q = Quote(a, kind);
            if (!game.Economy.TrySpend(q.cost)) return null;

            var now = DateTime.UtcNow;
            var t = new ActiveTour
            {
                artistId = a.id, kind = (int)kind, title = q.title, titleName = q.titleName, shows = q.shows,
                startUtcTicks = now.Ticks, endUtcTicks = now.AddHours(q.hours).Ticks,
                revenue = q.revenue, fans = q.fans, popularity = q.popularity, xp = q.xp,
                attendance = q.attendance, capacity = q.capacity, albumBonus = q.albumBonus,
            };
            var pool = new List<string>(Venues.Cities);
            for (int i = 0; i < q.shows; i++)
            {
                if (pool.Count == 0) pool.AddRange(Venues.Cities);
                int k = rnd.Next(pool.Count);
                t.cities.Add(pool[k]);
                pool.RemoveAt(k);
            }
            var c = concerts.Career(a);
            if (q.albumBonus) { c.newAlbumId = ""; c.newAlbumTitle = ""; } // the album gets its tour
            tours.Add(t);
            TourStarted?.Invoke(t);
            concerts.NotifyChanged();
            return t;
        }

        /// <summary>Pays a finished tour: money, fans, popularity, XP and a history entry.</summary>
        public bool Collect(ActiveTour t)
        {
            if (t == null || !tours.Contains(t) || !t.IsFinished(DateTime.UtcNow)) return false;
            tours.Remove(t);
            announced.Remove(t);
            var a = FindArtist(t.artistId);
            game.Economy.AddMoney(t.revenue);
            if (a != null)
            {
                var c = concerts.Career(a);
                c.fans += t.fans;
                c.lastFansGain = t.fans;
                c.totalFansGained += t.fans;
                t.popularity = concerts.AddPopularity(c, t.popularity);
                c.concertXp += t.xp;
                c.concerts += t.shows;
                c.totalRevenue += t.revenue;
                c.toursCompleted++;
                var d = Tours.Get(t.Kind);
                if ((int)d.venue > c.bestVenue) c.bestVenue = (int)d.venue;
                float fill = t.capacity > 0 ? t.attendance / (float)t.capacity : 0;
                if (fill > c.bestAttendance) c.bestAttendance = fill;
                if (t.revenue > c.bestConcertRevenue) { c.bestConcertRevenue = t.revenue; c.SetBestConcert((int)d.venue, "", t.titleName, t.kind, t.shows, t.title); }
                if (t.Kind >= TourKind.World) { c.worldTours++; concerts.Achievements.Unlock("world_tour"); }
                if (d.venue >= VenueTier.Arena) concerts.Achievements.Unlock("arena_ready");
                if (d.venue >= VenueTier.Stadium) concerts.Achievements.Unlock("stadium");
                concerts.Achievements.Unlock("first_show");
                c.AddRecord(new ConcertRecord
                {
                    name = t.title, tourName = t.titleName, tourKind = t.kind, shows = t.shows, venue = (int)d.venue, city = t.cities.Count > 0 ? t.cities[t.cities.Count - 1] : "",
                    attendance = t.attendance, capacity = t.capacity, revenue = t.revenue, fans = t.fans, tour = true, utcTicks = DateTime.UtcNow.Ticks,
                });
            }
            concerts.CheckEmpire();
            TourCollected?.Invoke(t);
            concerts.NotifyChanged();
            return true;
        }

        ArtistData FindArtist(string id)
        {
            foreach (var a in game.Artists.Artists) if (a.id == id) return a;
            return null;
        }

        public ArtistData ArtistOf(ActiveTour t) => t != null ? FindArtist(t.artistId) : null;

        public void Tick(float dt)
        {
            checkTimer += dt;
            if (checkTimer < 1f) return;
            checkTimer = 0f;
            var now = DateTime.UtcNow;
            foreach (var t in tours.ToArray())
                if (t.IsFinished(now) && announced.Add(t))
                    TourFinished?.Invoke(t);
        }

        public List<ActiveTour> GetSaveData() => new List<ActiveTour>(tours);

        public void LoadState(IEnumerable<ActiveTour> list)
        {
            tours.Clear();
            announced.Clear();
            if (list != null)
                foreach (var t in list)
                    if (t != null && !string.IsNullOrEmpty(t.artistId))
                    {
                        if (t.cities == null) t.cities = new List<string>();
                        tours.Add(t);
                    }
        }
    }
}
