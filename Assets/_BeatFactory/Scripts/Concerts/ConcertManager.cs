using System;
using System.Collections.Generic;

namespace BeatFactory
{
    /// <summary>
    /// Artist careers and concerts: SIGN → MUSIC → POPULARITY → FANS → CONCERTS → BIGGER VENUES → TOURS.
    /// Fans and popularity grow from beats, viral beats, albums, social media buzz and shows; popularity is limited by
    /// the studio (better rooms → better songs → bigger careers). Concerts pay ticket sales plus merch/streams that
    /// scale with producer income, and give fans, popularity and concert XP.
    /// </summary>
    public class ConcertManager
    {
        readonly GameManager game;
        readonly Dictionary<string, ArtistCareer> careers = new Dictionary<string, ArtistCareer>();
        readonly System.Random rnd = new System.Random();
        DateTime lastViralUtc = DateTime.MinValue;
        float tickTimer;
        float trendTimer = 150f;

        /// <summary>Fired after a concert was performed and its rewards applied.</summary>
        public event Action<ConcertResult> ConcertPerformed;
        /// <summary>Social media buzz: artist, fans gained.</summary>
        public event Action<ArtistData, double> Trending;
        /// <summary>Any career stat changed (UI refresh; fired at most once per second for passive growth).</summary>
        public event Action Changed;

        public AchievementManager Achievements { get; }

        public ConcertManager(GameManager game, AchievementManager achievements)
        {
            this.game = game;
            Achievements = achievements;
        }

        // ------------------------------------------------------------------ careers

        public ArtistCareer Career(ArtistData a)
        {
            if (a == null) return null;
            if (!careers.TryGetValue(a.id, out var c))
            {
                c = ArtistCareer.CreateFor(a);
                careers[a.id] = c;
            }
            return c;
        }

        public bool HasCareer(ArtistData a) => a != null && careers.ContainsKey(a.id);

        public IEnumerable<ArtistData> SignedArtists()
        {
            foreach (var a in game.Artists.Artists)
                if (game.Artists.IsSigned(a)) yield return a;
        }

        public int StudioLevel => game.Studios.Level;
        public float PopularityCap => ConcertRewardCalculator.PopularityCap(StudioLevel);

        /// <summary>Producer income without temporary ad boosts (so a 2X boost doesn't double concert prices).</summary>
        public double BaseIncomePerSecond => game.Producers.IncomePerSecond / Math.Max(1.0, game.Boosts.ProducerIncomeFactor);

        public bool RecentViral => (DateTime.UtcNow - lastViralUtc).TotalMinutes < 10;

        public ConcertContext Context(ArtistData a) => new ConcertContext
        {
            artist = a,
            career = Career(a),
            studioLevel = StudioLevel,
            incomePerSecond = BaseIncomePerSecond,
            recentViral = RecentViral,
        };

        /// <summary>Adds popularity up to the studio's limit. Returns what was actually gained.</summary>
        public float AddPopularity(ArtistCareer c, float amount)
        {
            float cap = PopularityCap;
            if (amount <= 0 || c.popularity >= cap) return 0;
            float before = c.popularity;
            c.popularity = Math.Min(cap, c.popularity + amount);
            if (c.popularity >= 90) Achievements.Unlock("superstar");
            return c.popularity - before;
        }

        // ------------------------------------------------------------------ concerts

        public ConcertQuote Quote(ArtistData a, VenueTier tier) => ConcertRewardCalculator.Quote(Context(a), Venues.Get(tier));

        public bool IsOnTour(ArtistData a) => game.Tours != null && game.Tours.ActiveFor(a) != null;

        public enum BlockKind { None, NotSigned, OnTour, Resting }

        /// <summary>Why the artist can't play right now.</summary>
        public BlockKind Block(ArtistData a)
        {
            if (!game.Artists.IsSigned(a)) return BlockKind.NotSigned;
            if (IsOnTour(a)) return BlockKind.OnTour;
            if (Career(a).IsResting(DateTime.UtcNow)) return BlockKind.Resting;
            return BlockKind.None;
        }

        /// <summary>Why the artist can't play right now, as player text (null = ready).</summary>
        public string BlockReason(ArtistData a)
        {
            switch (Block(a))
            {
                case BlockKind.NotSigned: return Loc.T("concert.block_not_signed");
                case BlockKind.OnTour: return Loc.T("concert.block_on_tour");
                case BlockKind.Resting: return Loc.F("concert.block_resting", "time", NumberFormatter.FormatClock(Career(a).RestSeconds(DateTime.UtcNow)));
                default: return null;
            }
        }

        public bool CanBook(ArtistData a, VenueTier tier)
        {
            if (Block(a) != BlockKind.None) return false;
            var q = Quote(a, tier);
            return q.unlocked && game.Economy.CanAfford(q.bookingCost);
        }

        /// <summary>Pays the booking, plays the show and applies every reward immediately (safe if the app closes).</summary>
        public ConcertResult Book(ArtistData a, VenueTier tier)
        {
            if (!CanBook(a, tier)) return null;
            var ctx = Context(a);
            var q = ConcertRewardCalculator.Quote(ctx, Venues.Get(tier));
            if (!game.Economy.TrySpend(q.bookingCost)) return null;

            var c = ctx.career;
            var r = ConcertRewardCalculator.Perform(ctx, q, rnd);
            r.city = Venues.Cities[rnd.Next(Venues.Cities.Length)];
            r.showName = a.displayName + " — " + r.city;
            r.levelBefore = c.ConcertLevel;
            r.stageBefore = c.StageIndex;

            game.Economy.AddMoney(r.Revenue);
            c.fans += r.fans;
            c.lastFansGain = r.fans;
            c.totalFansGained += r.fans;
            r.popularity = AddPopularity(c, r.popularity);
            c.concertXp += r.xp;
            c.concerts++;
            if (r.soldOut) c.soldOuts++;
            c.totalRevenue += r.Revenue;
            if ((int)tier > c.bestVenue) c.bestVenue = (int)tier;
            if (r.fill > c.bestAttendance) c.bestAttendance = r.fill;
            if (r.Revenue > c.bestConcertRevenue)
            {
                c.bestConcertRevenue = r.Revenue;
                c.SetBestConcert((int)tier, r.city, null, 0, 0);
            }
            c.restUntilTicks = DateTime.UtcNow.AddMinutes(Venues.Get(tier).cooldownMinutes).Ticks;
            c.AddRecord(new ConcertRecord
            {
                venue = (int)tier, city = r.city, attendance = r.attendance, capacity = r.capacity,
                revenue = r.Revenue, fans = r.fans, soldOut = r.soldOut, utcTicks = DateTime.UtcNow.Ticks,
            });
            r.levelAfter = c.ConcertLevel;
            r.stageAfter = c.StageIndex;

            Achievements.Unlock("first_show");
            if (r.soldOut) Achievements.Unlock("sold_out");
            if (tier >= VenueTier.Arena) Achievements.Unlock("arena_ready");
            if (tier >= VenueTier.Stadium) Achievements.Unlock("stadium");
            CheckEmpire();

            Changed?.Invoke();
            ConcertPerformed?.Invoke(r);
            return r;
        }

        public double TotalConcertRevenue
        {
            get
            {
                double sum = 0;
                foreach (var c in careers.Values) sum += c.totalRevenue;
                return sum;
            }
        }

        public void CheckEmpire()
        {
            if (TotalConcertRevenue >= 1e9) Achievements.Unlock("music_empire");
        }

        // ------------------------------------------------------------------ career growth from the rest of the game

        /// <summary>Album release: the featured artist (biggest signed name) gets a NEW ALBUM and a big push; the roster gets buzz.</summary>
        public void OnAlbumReleased(AlbumData album)
        {
            ArtistData featured = null;
            foreach (var a in SignedArtists()) featured = a; // same pick as the release ceremony ("feat.")
            foreach (var a in SignedArtists())
            {
                var c = Career(a);
                bool feat = a == featured;
                double gain = c.fans * (feat ? 0.15 : 0.05) + (feat ? 20_000 * (StudioLevel + 1) : 0);
                c.fans += Math.Round(gain);
                c.totalFansGained += Math.Round(gain);
                AddPopularity(c, feat ? 5f : 2f);
                if (feat)
                {
                    c.newAlbumId = album.id;
                    c.newAlbumTitle = album.title;
                }
            }
            Changed?.Invoke();
        }

        public void OnBeat(BeatResult r)
        {
            if (r.Source == BeatSource.Offline || !r.IsSpecial) return;
            var pick = RandomSigned();
            if (pick == null) return;
            var c = Career(pick);
            bool viral = r.Quality == BeatQuality.Viral;
            if (viral) lastViralUtc = DateTime.UtcNow;
            double gain = Math.Round(c.fans * (viral ? 0.02 : 0.003) + (viral ? 500 : 50) * (StudioLevel + 1));
            c.fans += gain;
            c.totalFansGained += gain;
            AddPopularity(c, viral ? 0.5f : 0.05f);
            Changed?.Invoke();
        }

        /// <summary>Better studio → better songs: the whole roster gets a push.</summary>
        public void OnStudioUpgraded()
        {
            foreach (var a in SignedArtists()) AddPopularity(Career(a), 1f);
            Changed?.Invoke();
        }

        ArtistData RandomSigned()
        {
            var list = new List<ArtistData>(SignedArtists());
            return list.Count == 0 ? null : list[rnd.Next(list.Count)];
        }

        /// <summary>Streams of the catalogue: fans per second for each signed artist (scales with the studio and popularity).</summary>
        double PassiveFansPerSecond(ArtistCareer c) =>
            (0.35 + 0.5 * Math.Pow(StudioLevel + 1, 1.6)) * (0.5 + c.popularity / 40.0);

        const float PassivePopularityPerSecond = 0.0015f;

        public void Tick(float dt)
        {
            tickTimer += dt;
            if (tickTimer >= 1f)
            {
                float s = tickTimer;
                tickTimer = 0f;
                bool any = false;
                foreach (var a in SignedArtists())
                {
                    if (IsOnTour(a)) continue;
                    var c = Career(a);
                    c.fans += PassiveFansPerSecond(c) * s * ArtistCareer.FanGrowth(a);
                    AddPopularity(c, PassivePopularityPerSecond * (1f + 0.15f * StudioLevel) * s);
                    any = true;
                }
                if (any) Changed?.Invoke();
            }

            // social media: every few minutes one of your artists trends
            trendTimer -= dt;
            if (trendTimer <= 0f)
            {
                trendTimer = 180f + (float)rnd.NextDouble() * 180f;
                var pick = RandomSigned();
                if (pick != null)
                {
                    var c = Career(pick);
                    double gain = Math.Round(c.fans * 0.03 + 200 * (StudioLevel + 1));
                    c.fans += gain;
                    c.totalFansGained += gain;
                    AddPopularity(c, 0.5f);
                    Changed?.Invoke();
                    Trending?.Invoke(pick, gain);
                }
            }
        }

        /// <summary>Catalogue streams kept growing the fan base while the game was closed.</summary>
        public void ProcessOffline(double seconds, double maxSeconds)
        {
            double s = Math.Min(Math.Max(0, seconds), maxSeconds);
            if (s <= 0) return;
            foreach (var a in SignedArtists())
            {
                if (IsOnTour(a)) continue;
                var c = Career(a);
                c.fans += PassiveFansPerSecond(c) * s * 0.5;
                AddPopularity(c, PassivePopularityPerSecond * (1f + 0.15f * StudioLevel) * (float)s * 0.5f);
            }
            Changed?.Invoke();
        }

        public void NotifyChanged() => Changed?.Invoke();

        // ------------------------------------------------------------------ save

        public List<ArtistCareer> GetSaveData() => new List<ArtistCareer>(careers.Values);

        public void LoadState(IEnumerable<ArtistCareer> list)
        {
            careers.Clear();
            if (list != null)
                foreach (var c in list)
                    if (c != null && !string.IsNullOrEmpty(c.id))
                    {
                        if (c.history == null) c.history = new List<ConcertRecord>();
                        careers[c.id] = c;
                    }
            Changed?.Invoke();
        }
    }
}
