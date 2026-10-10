using System;
using System.Collections.Generic;

namespace BeatFactory
{
    /// <summary>Every number missions and challenges can track. All values come from the real gameplay systems.</summary>
    public enum StatId
    {
        // production
        BeatsMade, BeatEarnings, HitBeats, ViralBeats, ProducerUpgrades, MoneyEarned,
        // artists
        ArtistsSigned, ArtistsSignedLifetime, ArtistGenres, TotalPopularity, MaxArtistFans, TotalFans, MaxConcertLevel, SuccessfulArtists,
        // releases
        AlbumsReleased, Streams,
        // concerts
        Concerts, ConcertAttendees, ConcertFans, SoldOutClubs, SoldOutHalls, ArenaShows, StadiumShows, ToursCompleted,
        // studio / career
        BestStudio, FamePoints, AdsWatched,
    }

    /// <summary>
    /// The single source of truth for missions and challenges. Live values are read from the game's systems
    /// (beats, economy, artists, albums, streams, concerts, studio); counters that no system keeps (producer upgrades,
    /// money from beats, attendees, ads watched …) are counted here from the systems' events and saved.
    /// Lifetime values never go down (prestige doesn't erase career history).
    /// </summary>
    public class StatsTracker
    {
        readonly GameManager game;
        readonly Dictionary<StatId, double> counters = new Dictionary<StatId, double>();
        readonly Dictionary<string, int> knownLevels = new Dictionary<string, int>();

        /// <summary>Fired (throttled by the managers) whenever a tracked number may have changed.</summary>
        public event Action Changed;

        public StatsTracker(GameManager game)
        {
            this.game = game;
        }

        /// <summary>Subscribes to the gameplay events (call once after all systems exist).</summary>
        public void Hook()
        {
            game.Beats.BeatsCreated += r => { if (r.Reward > 0) Add(StatId.BeatEarnings, r.Reward); };
            game.Economy.MoneyEarned += m => Add(StatId.MoneyEarned, m);
            game.Producers.ProducerChanged += p =>
            {
                knownLevels.TryGetValue(p.Data.id, out int before);
                if (p.Level > before && before > 0) Add(StatId.ProducerUpgrades, p.Level - before);
                knownLevels[p.Data.id] = p.Level;
            };
            game.Artists.ArtistSigned += _ => Add(StatId.ArtistsSignedLifetime, 1);
            game.Concerts.ConcertPerformed += r =>
            {
                Add(StatId.Concerts, 1);
                Add(StatId.ConcertAttendees, r.attendance);
                Add(StatId.ConcertFans, r.fans);
                if (r.soldOut && r.venue.tier == VenueTier.Club) Add(StatId.SoldOutClubs, 1);
                if (r.soldOut && r.venue.tier == VenueTier.ConcertHall) Add(StatId.SoldOutHalls, 1);
                if (r.venue.tier == VenueTier.Arena) Add(StatId.ArenaShows, 1);
                if (r.venue.tier == VenueTier.Stadium) Add(StatId.StadiumShows, 1);
            };
            game.Tours.TourCollected += t =>
            {
                Add(StatId.ToursCompleted, 1);
                Add(StatId.Concerts, t.shows);
                Add(StatId.ConcertAttendees, t.attendance);
                Add(StatId.ConcertFans, t.fans);
            };
        }

        /// <summary>Ads manager may be created after the game manager: hook it when it appears.</summary>
        public void HookAds(AdsManager ads)
        {
            if (ads != null) ads.RewardGranted += _ => Add(StatId.AdsWatched, 1);
        }

        void Add(StatId id, double amount)
        {
            if (amount <= 0 || double.IsNaN(amount) || double.IsInfinity(amount)) return;
            counters.TryGetValue(id, out double v);
            counters[id] = v + amount;
            Changed?.Invoke();
        }

        double Counter(StatId id) => counters.TryGetValue(id, out double v) ? v : 0;

        /// <summary>Current value of a stat (live from the systems, or the saved counter).</summary>
        public double Value(StatId id)
        {
            switch (id)
            {
                case StatId.BeatsMade: return game.Beats.TotalBeats;
                case StatId.HitBeats: return game.Beats.HitBeats;
                case StatId.ViralBeats: return game.Beats.ViralBeats;
                case StatId.MoneyEarned: return Math.Max(Counter(StatId.MoneyEarned), game.Economy.LifetimeEarnings);
                case StatId.ArtistsSigned: return game.Artists.SignedCount;
                // saves from before the goals update: artists signed back then still count
                case StatId.ArtistsSignedLifetime: return Math.Max(Counter(StatId.ArtistsSignedLifetime), game.Artists.SignedCount);
                case StatId.ArtistGenres:
                {
                    var set = new HashSet<ArtistGenre>();
                    foreach (var a in game.Artists.Artists) if (game.Artists.IsSigned(a)) set.Add(a.genre);
                    return set.Count;
                }
                case StatId.TotalPopularity:
                {
                    double sum = 0;
                    foreach (var a in game.Concerts.SignedArtists()) sum += game.Concerts.Career(a).popularity;
                    return sum;
                }
                case StatId.MaxArtistFans:
                case StatId.TotalFans:
                case StatId.MaxConcertLevel:
                case StatId.SuccessfulArtists:
                {
                    double max = 0, total = 0, level = 0, ok = 0;
                    foreach (var a in game.Artists.Artists)
                    {
                        if (!game.Concerts.HasCareer(a)) continue;
                        var c = game.Concerts.Career(a);
                        max = Math.Max(max, c.fans);
                        total += c.fans;
                        level = Math.Max(level, c.ConcertLevel);
                        if (c.popularity >= 40) ok++;
                    }
                    return id == StatId.MaxArtistFans ? max : id == StatId.TotalFans ? total : id == StatId.MaxConcertLevel ? level : ok;
                }
                case StatId.AlbumsReleased: return game.Albums.ReleasedCount;
                case StatId.Streams: return game.Streams.LifetimeStreams;
                case StatId.BestStudio: return Math.Max(game.Studios.Level, game.Appearance.BestTier);
                case StatId.FamePoints: return game.Economy.FamePoints;
                default: return Counter(id);
            }
        }

        public void SnapshotLevels()
        {
            knownLevels.Clear();
            foreach (var p in game.Producers.Producers) knownLevels[p.Data.id] = p.Level;
        }

        // ------------------------------------------------------------------ save

        public List<StatEntry> GetSaveData()
        {
            var list = new List<StatEntry>();
            foreach (var kv in counters) list.Add(new StatEntry { id = kv.Key.ToString(), value = kv.Value });
            return list;
        }

        public void LoadState(IEnumerable<StatEntry> list)
        {
            counters.Clear();
            if (list != null)
                foreach (var e in list)
                    if (e != null && Enum.TryParse(e.id, out StatId id)) counters[id] = Math.Max(0, e.value);
            SnapshotLevels();
        }
    }

    [Serializable]
    public class StatEntry
    {
        public string id;
        public double value;
    }
}
