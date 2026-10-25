using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools
{
    /// <summary>
    /// Economy simulation with the CURRENT data assets and the game's formulas: producers (levels, milestones, cost growth),
    /// beat quality, genres, studios, artists, albums, stream platforms, money missions, daily challenges, concerts,
    /// free boosts, rewarded-ad boosts (with the combined-boost cap), INSTANT VIRAL, VIRAL-beat 2X MONEY (with cooldown)
    /// and offline earnings. The simulated player buys whatever pays itself back fastest.
    /// Menu: Tools > Beat Factory > Simulate Economy (writes Temp/bf_economy_report.txt and logs it).
    /// Not simulated: prestige, tours, artist fan growth (concert venues follow the studio level instead).
    /// </summary>
    public static class EconomySimulator
    {
        public class Profile
        {
            public string name, description;
            public double days;
            /// <summary>Is the player in the game at second t (calendar time)?</summary>
            public Func<double, bool> playing;
            /// <summary>MAKE BEAT taps per second at second t.</summary>
            public Func<double, double> taps;
            public bool freeBoosts = true;
            /// <summary>Artists the player keeps booking concerts for.</summary>
            public int concertArtists;
            /// <summary>Daily challenge money (seconds of income per day).</summary>
            public double challengeSecondsPerDay;
            /// <summary>Seconds between rewarded boost ads (0 = never).</summary>
            public double adEverySeconds;
            public BoostType[] adBoosts = new BoostType[0];
            public bool instantViral, doubleOffline;
        }

        static bool Awake(double t) => t % 86400 < 16 * 3600;

        public static readonly Profile Casual = new Profile
        {
            name = "CASUAL", description = "10 min every 3 h while awake, 1 tap/s, free boosts, 1 artist on stage, no ads", days = 30,
            playing = t => t % (3 * 3600) < 10 * 60 && Awake(t), taps = t => 1, concertArtists = 1, challengeSecondsPerDay = 600,
        };
        public static readonly Profile Active = new Profile
        {
            name = "ACTIVE", description = "2 h first session, then 30 min every 2 h while awake, taps 3→0.5/s, free boosts, 3 artists on stage, no ads", days = 16,
            playing = t => t < 7200 || (t % 7200 < 1800 && Awake(t)),
            taps = t => t < 120 ? 3 : t < 600 ? 2 : t < 3600 ? 1.2 : 0.5, concertArtists = 3, challengeSecondsPerDay = 2700,
        };
        public static readonly Profile Optimized = new Profile
        {
            name = "OPTIMIZED", description = "4 h first session, then 30 min every hour while awake, taps 3→1.5/s, best-ROI buys, 6 artists on stage, no ads", days = 10,
            playing = t => t < 4 * 3600 || (t % 3600 < 1800 && Awake(t)),
            taps = t => t < 600 ? 3 : 1.5, concertArtists = 6, challengeSecondsPerDay = 2700,
        };
        public static readonly Profile AdOccasional = new Profile
        {
            name = "AD_OCCASIONAL", description = "OPTIMIZED + one PRODUCER BOOST ad every 30 min of play + doubled offline", days = 10,
            playing = Optimized.playing, taps = Optimized.taps, concertArtists = 6, challengeSecondsPerDay = 2700,
            adEverySeconds = 1800, adBoosts = new[] { BoostType.Producer }, doubleOffline = true,
        };
        public static readonly Profile AdFrequent = new Profile
        {
            name = "AD_FREQUENT", description = "OPTIMIZED + PRODUCER and 2X MONEY ads every 5 min (always on), INSTANT VIRAL on cooldown, doubled offline", days = 10,
            playing = Optimized.playing, taps = Optimized.taps, concertArtists = 6, challengeSecondsPerDay = 2700,
            adEverySeconds = 300, adBoosts = new[] { BoostType.Producer, BoostType.Money }, instantViral = true, doubleOffline = true,
        };
        public static Profile[] All => new[] { Casual, Active, Optimized, AdOccasional, AdFrequent };

        public static string ReportFile => Path.Combine(Path.GetTempPath(), "bf_economy_report.txt");

        [MenuItem("Tools/Beat Factory/Simulate Economy")]
        public static void Menu()
        {
            var sb = new StringBuilder();
            foreach (var p in All) sb.AppendLine(Run(p).Describe());
            File.WriteAllText(ReportFile, sb.ToString());
            Debug.Log(sb.ToString());
        }

        // ------------------------------------------------------------------ results

        public static readonly double[] Milestones = { 100, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11, 1e12, 1e13 };

        public class Result
        {
            public Profile profile;
            /// <summary>Calendar / play seconds when lifetime earnings first reached Milestones[i] (-1 = never).</summary>
            public double[] earnedAt, earnedAtPlay;
            /// <summary>Calendar / play seconds when studio i was bought (index 0 = Basement = 0).</summary>
            public double[] studioAt, studioAtPlay;
            public double firstPurchaseAt = -1, firstHireAt = -1;
            public readonly Dictionary<string, double> sources = new Dictionary<string, double>();
            /// <summary>(play seconds, income per second) samples.</summary>
            public readonly List<KeyValuePair<double, double>> incomeSamples = new List<KeyValuePair<double, double>>();
            public readonly List<string> log = new List<string>();

            public double PlayMinutesTo(double earned)
            {
                int i = Array.IndexOf(Milestones, earned);
                return i >= 0 && earnedAtPlay[i] >= 0 ? earnedAtPlay[i] / 60 : double.PositiveInfinity;
            }

            public double StudioPlayHours(int level) =>
                level < studioAtPlay.Length && studioAtPlay[level] >= 0 ? studioAtPlay[level] / 3600 : double.PositiveInfinity;

            public string Describe()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"=== {profile.name}: {profile.description}");
                sb.AppendLine($"  first purchase {Fmt(firstPurchaseAt)} · first producer hired {Fmt(firstHireAt)}   (times = play time; calendar in [ ])");
                sb.Append("  lifetime earnings:");
                for (int i = 0; i < Milestones.Length; i++)
                    if (earnedAt[i] >= 0) sb.Append($"  ${Short(Milestones[i])} {Fmt(earnedAtPlay[i])}{Cal(earnedAt[i], earnedAtPlay[i])}");
                sb.AppendLine();
                sb.Append("  studios:");
                for (int s = 1; s < studioAt.Length; s++)
                    sb.Append(studioAt[s] >= 0 ? $"  L{s + 1} {Fmt(studioAtPlay[s])}{Cal(studioAt[s], studioAtPlay[s])}" : $"  L{s + 1} —");
                sb.AppendLine();
                sb.Append("  income per minute:");
                foreach (double m in new double[] { 5, 15, 30, 60, 120, 300, 600, 1200, 2400 })
                {
                    var sample = incomeSamples.LastOrDefault(x => x.Key <= m * 60);
                    if (sample.Key > 0 && incomeSamples.Last().Key >= m * 60) sb.Append($"  {Fmt(m * 60)}: ${Short(sample.Value * 60)}");
                }
                sb.AppendLine();
                double total = sources.Values.Sum();
                sb.AppendLine("  income sources: " + string.Join(", ", sources.Where(x => x.Value > 0).OrderByDescending(x => x.Value)
                    .Select(x => $"{x.Key} {100 * x.Value / total:0.0}%")));
                sb.AppendLine("  key purchases: " + string.Join(" · ", log.Take(40)));
                return sb.ToString();
            }

            static string Cal(double cal, double play) => Math.Abs(cal - play) < 1 ? "" : $" [{Fmt(cal)}]";
        }

        public static string Fmt(double t)
        {
            if (t < 0) return "—";
            if (t >= 86400) return $"{(int)(t / 86400)}d{(int)(t / 3600) % 24}h";
            if (t >= 3600) return $"{(int)(t / 3600)}h{(int)(t / 60) % 60:00}";
            return $"{(int)(t / 60)}m{(int)(t % 60):00}s";
        }

        static string Short(double v)
        {
            string[] u = { "", "K", "M", "B", "T", "Qa" };
            int i = 0;
            while (v >= 1000 && i < u.Length - 1) { v /= 1000; i++; }
            return v.ToString(v < 10 ? "0.##" : v < 100 ? "0.#" : "0") + u[i];
        }

        // ------------------------------------------------------------------ simulation

        static T Load<T>(string name) where T : ScriptableObject =>
            AssetDatabase.LoadAssetAtPath<T>(BeatFactorySceneBuilder.Root + "/Data/Config/" + name + ".asset");

        /// <summary>Best concert venue an artist usually reaches at each studio level (popularity cap / fans).</summary>
        static readonly int[] VenueByStudio = { 0, 0, 1, 2, 2, 3, 3, 3, 4, 4 };
        /// <summary>Typical attendance at each venue (ECONOMY_BALANCE.md → Concerts).</summary>
        static readonly double[] VenueFill = { 0.35, 0.5, 0.8, 0.95, 1.0 };

        public static Result Run(Profile profile)
        {
            var eco = Load<EconomyConfig>("EconomyConfig");
            var quality = Load<BeatQualityConfig>("BeatQualityConfig");
            var streamCfg = Load<StreamConfig>("StreamConfig");
            var boostCfg = Load<BoostConfig>("BoostConfig");
            var producers = DefaultGameData.LoadProducers();
            var studios = DefaultGameData.LoadStudios();
            var genres = DefaultGameData.LoadGenres();
            var artists = DefaultGameData.LoadArtists().OrderBy(a => a.requiredStudioLevel).ThenBy(a => a.cost).ToArray();
            var albums = DefaultGameData.LoadAlbums();
            var platforms = streamCfg.platforms;
            var missions = MissionCatalog.All.Where(m => m.reward.kind == RewardKind.Money).ToList();
            var tiers = quality.tiers;
            var venues = Venues.All;

            var r = new Result
            {
                profile = profile,
                earnedAt = Enumerable.Repeat(-1.0, Milestones.Length).ToArray(),
                earnedAtPlay = Enumerable.Repeat(-1.0, Milestones.Length).ToArray(),
                studioAt = Enumerable.Repeat(-1.0, studios.Length).ToArray(),
                studioAtPlay = Enumerable.Repeat(-1.0, studios.Length).ToArray(),
            };
            r.studioAt[0] = r.studioAtPlay[0] = 0;
            foreach (var k in new[] { "manual", "producers", "streams", "missions", "challenges", "albums", "concerts", "offline", "ad rewards" }) r.sources[k] = 0;

            int[] level = new int[producers.Length];
            int studio = 0, genre = 0, artist = 0, album = 0, platform = platforms.Count(p => p.releaseCost <= 0);
            double money = eco.startingMoney, earned = 0, beats = 0, upgrades = 0, away = 0, playTime = 0;
            double moneyBoostEnd = -1, producerBoostEnd = -1, nextFree = 0, nextAd = 0, nextInstant = 0, viralAcc = 0, viralBoostReady = 0;
            int day = -1;
            var done = new HashSet<string>();

            double Viral() => Math.Max(1.0, studios[studio].viralChanceMultiplier);
            double AvgQuality()
            {
                double w = 0, q = 0;
                foreach (var t in tiers) { double c = t.quality >= BeatQuality.Hit ? t.chancePercent * Viral() : t.chancePercent; w += c; q += c * t.multiplier; }
                return q / w;
            }
            double AvgStream()
            {
                double w = 0, s = 0;
                foreach (var t in tiers) { double c = t.quality >= BeatQuality.Hit ? t.chancePercent * Viral() : t.chancePercent; w += c; s += c * t.streamShare; }
                return s / w;
            }
            double ViralChance()
            {
                double w = 0, v = 0;
                foreach (var t in tiers) { double c = t.quality >= BeatQuality.Hit ? t.chancePercent * Viral() : t.chancePercent; w += c; if (t.quality == BeatQuality.Viral) v += c; }
                return v / w;
            }
            double PlatformSum()
            {
                double s = 0;
                for (int i = 0; i < platform; i++) s += platforms[i].streamMultiplier;
                return s; // streams × money per stream: the stream count cancels out
            }
            double Fixed() => genres[genre].incomeMultiplier * studios[studio].incomeMultiplier *
                              artists.Take(artist).Aggregate(1.0, (a, x) => a * x.incomeMultiplier) *
                              albums.Take(album).Aggregate(1.0, (a, x) => a * x.reputationMultiplier);
            double MoneyBoost(double t) => t < moneyBoostEnd ? boostCfg.Get(BoostType.Money).multiplier : 1;
            double ProducerBoost(double t)
            {
                double raw = MoneyBoost(t) * (t < producerBoostEnd ? boostCfg.Get(BoostType.Producer).multiplier : 1);
                return Math.Min(raw, boostCfg.maxCombinedProducerBoost) / MoneyBoost(t); // cap on money × producer
            }
            double BaseOf(int i, int lv)
            {
                if (lv <= 0) return 0;
                int ms = eco.producerMilestoneEvery > 0 ? lv / eco.producerMilestoneEvery : 0;
                return producers[i].baseIncome * lv * Math.Pow(eco.producerMilestoneMultiplier, ms) / producers[i].cycleSeconds;
            }
            double ProducerBaseRaw(int[] lv)
            {
                double s = 0;
                for (int i = 0; i < producers.Length; i++) s += BaseOf(i, lv[i]);
                return s * studios[studio].speedMultiplier;
            }
            double BeatsPerSecond()
            {
                double b = 0;
                for (int i = 0; i < producers.Length; i++) if (level[i] > 0) b += 1.0 / producers[i].cycleSeconds;
                return b * studios[studio].speedMultiplier;
            }
            // returns (producers, manual, streams)
            (double prod, double man, double streams) Income(double t, int[] lv, double taps)
            {
                double g = Fixed() * MoneyBoost(t), q = AvgQuality(), s = AvgStream() * PlatformSum();
                double pb = ProducerBaseRaw(lv) * ProducerBoost(t);
                double manBase = eco.manualBeatBaseReward + eco.manualBeatProducerShare * pb;
                return (pb * g * q, taps * manBase * g * q, (pb + taps * manBase) * g * s);
            }
            double Total((double prod, double man, double streams) x) => x.prod + x.man + x.streams;
            double Unboosted() => ProducerBaseRaw(level) * Fixed() * AvgQuality();
            double MoneyFor(double seconds) =>
                Math.Max((eco.manualBeatBaseReward + eco.manualBeatProducerShare * ProducerBaseRaw(level)) * Fixed() * Math.Max(3, seconds / 10), Unboosted() * seconds);
            void Add(string source, double v)
            {
                money += v;
                earned += v;
                r.sources[source] += v;
            }
            double Stat(StatId id)
            {
                switch (id)
                {
                    case StatId.BeatsMade: return beats;
                    case StatId.BeatEarnings: case StatId.MoneyEarned: return earned;
                    case StatId.ProducerUpgrades: return upgrades;
                    case StatId.BestStudio: return studio;
                    case StatId.AlbumsReleased: return album;
                    case StatId.ArtistsSignedLifetime: case StatId.ArtistsSigned: return artist;
                    default: return -1; // stats this simulation doesn't track (fans, concerts, streams …): reward not counted
                }
            }
            double Cost(int i) => level[i] == 0 ? producers[i].hireCost
                : producers[i].upgradeBaseCost * Math.Pow(producers[i].costGrowth > 0 ? producers[i].costGrowth : eco.upgradeCostGrowth, level[i] - 1);

            double end = profile.days * 86400;
            const double dt = 1;
            for (double t = 0; t < end; t += dt)
            {
                if (!profile.playing(t))
                {
                    away += dt;
                    continue;
                }
                if (away > 0)
                {
                    double cap = (studios[studio].offlineHours > 0 ? studios[studio].offlineHours : eco.maxOfflineHours) * 3600;
                    double counted = Math.Min(away, cap);
                    if (away >= eco.minOfflineSeconds)
                        Add("offline", Unboosted() * counted * eco.offlineEfficiency * (profile.doubleOffline ? 2 : 1));
                    beats += BeatsPerSecond() * counted;
                    away = 0;
                }
                playTime += dt;
                int d = (int)(t / 86400);
                if (d != day)
                {
                    day = d;
                    if (t > 600) Add("challenges", Unboosted() * profile.challengeSecondsPerDay);
                }

                // free boost (the simulated player picks PRODUCER BOOST), rewarded-ad boosts, INSTANT VIRAL
                double maxStack = boostCfg.maxStackMinutes * 60;
                if (profile.freeBoosts && t >= nextFree)
                {
                    producerBoostEnd = Math.Min(Math.Max(producerBoostEnd, t) + boostCfg.Get(BoostType.Producer).durationSeconds, t + maxStack);
                    nextFree = t + boostCfg.freeBoostCooldownMinutes * 60;
                }
                if (profile.adEverySeconds > 0 && t >= nextAd)
                {
                    nextAd = t + profile.adEverySeconds;
                    foreach (var b in profile.adBoosts)
                    {
                        if (b == BoostType.Producer) producerBoostEnd = Math.Min(Math.Max(producerBoostEnd, t) + boostCfg.Get(b).durationSeconds, t + maxStack);
                        if (b == BoostType.Money) moneyBoostEnd = Math.Min(Math.Max(moneyBoostEnd, t) + boostCfg.Get(b).durationSeconds, t + maxStack);
                    }
                }
                if (profile.instantViral && t >= nextInstant)
                {
                    nextInstant = t + GameManager.InstantViralCooldownMinutes * 60;
                    double pb = ProducerBaseRaw(level) * ProducerBoost(t);
                    double baseReward = Math.Max(eco.manualBeatBaseReward + eco.manualBeatProducerShare * pb, pb * 5);
                    var viral = quality.GetTier(BeatQuality.Viral);
                    Add("ad rewards", baseReward * Fixed() * MoneyBoost(t) * (viral.multiplier + viral.streamShare * PlatformSum()));
                }

                double taps = profile.taps(t);
                var inc = Income(t, level, taps);
                Add("producers", inc.prod * dt);
                Add("manual", inc.man * dt);
                Add("streams", inc.streams * dt);
                double bps = BeatsPerSecond() + taps;
                beats += bps * dt;

                // VIRAL beats give a short 2X MONEY (once per cooldown)
                viralAcc += bps * dt * ViralChance();
                if (viralAcc >= 1)
                {
                    viralAcc -= 1;
                    if (boostCfg.viralBeatMoneyBoostSeconds > 0 && t >= viralBoostReady)
                    {
                        moneyBoostEnd = Math.Min(Math.Max(moneyBoostEnd, t) + boostCfg.viralBeatMoneyBoostSeconds, t + maxStack);
                        viralBoostReady = t + boostCfg.viralBeatBoostCooldownMinutes * 60;
                    }
                }

                // concerts: each booked artist plays the best venue on its cooldown; booking costs 35% of the revenue
                if (artist > 0 && profile.concertArtists > 0)
                {
                    int vi = Math.Min(venues.Length - 1, VenueByStudio[Math.Min(studio, VenueByStudio.Length - 1)]);
                    var v = venues[vi];
                    double fill = VenueFill[vi], ticket = (v.ticketMin + v.ticketMax) / 2.0;
                    double revenue = v.capacity * fill * ticket + Unboosted() * v.incomeSeconds * fill;
                    Add("concerts", (1 - ConcertRewardCalculator.BookingShare) * revenue / (v.cooldownMinutes * 60) * Math.Min(artist, profile.concertArtists) * dt);
                }

                foreach (var m in missions)
                    if (!done.Contains(m.id) && Stat(m.stat) >= m.target)
                    {
                        done.Add(m.id);
                        Add("missions", MoneyFor(m.reward.amount * GoalReward.Scale));
                    }

                if (album < albums.Length)
                {
                    var a = albums[album];
                    int hired = level.Count(l => l > 0);
                    if (beats >= a.requiredBeats && hired >= a.requiredProducers && studio >= a.requiredStudio && artist >= a.requiredArtists)
                    {
                        Add("albums", Math.Max(100, Income(t, level, 0).prod * a.rewardIncomeSeconds));
                        album++;
                    }
                }

                for (int i = 0; i < Milestones.Length; i++)
                    if (r.earnedAt[i] < 0 && earned >= Milestones[i]) { r.earnedAt[i] = t; r.earnedAtPlay[i] = playTime; }
                if ((int)playTime % 60 == 0) r.incomeSamples.Add(new KeyValuePair<double, double>(playTime, Total(inc)));

                // purchases: best payback first (studios valued ×1.5 for what they unlock, artists +15% while they can play concerts)
                for (int guard = 0; guard < 80; guard++)
                {
                    double cur = Total(Income(t, level, taps));
                    string best = null;
                    double bestScore = double.MaxValue, bestCost = 0;
                    Action act = null;
                    for (int i = 0; i < producers.Length; i++)
                    {
                        if (i > 0 && level[i - 1] == 0) break;
                        if (studio < producers[i].requiredStudio) break;
                        var lv = (int[])level.Clone();
                        lv[i]++;
                        double gain = Total(Income(t, lv, taps)) - cur, c = Cost(i), score = c / Math.Max(gain, 1e-9);
                        if (score < bestScore)
                        {
                            int k = i;
                            bestScore = score; bestCost = c; best = (level[i] == 0 ? "HIRE " : "UP ") + producers[i].displayName;
                            act = () => { if (level[k] > 0) upgrades++; else if (r.firstHireAt < 0) r.firstHireAt = playTime; level[k]++; };
                        }
                    }
                    if (genre + 1 < genres.Length && genres[genre + 1].requiredStudioLevel <= studio)
                    {
                        double c = genres[genre + 1].unlockCost, gain = cur * (genres[genre + 1].incomeMultiplier / genres[genre].incomeMultiplier - 1);
                        if (c / Math.Max(gain, 1e-9) < bestScore) { bestScore = c / Math.Max(gain, 1e-9); bestCost = c; best = "GENRE " + genres[genre + 1].displayName; act = () => genre++; }
                    }
                    if (studio + 1 < studios.Length)
                    {
                        var n = studios[studio + 1];
                        double gain = cur * (n.incomeMultiplier / studios[studio].incomeMultiplier * n.speedMultiplier / studios[studio].speedMultiplier - 1) * 1.5;
                        if (n.upgradeCost / Math.Max(gain, 1e-9) < bestScore)
                        {
                            bestScore = n.upgradeCost / Math.Max(gain, 1e-9); bestCost = n.upgradeCost; best = "STUDIO " + n.displayName;
                            act = () => { studio++; r.studioAt[studio] = t; r.studioAtPlay[studio] = playTime; };
                        }
                    }
                    if (artist < artists.Length && artists[artist].requiredStudioLevel <= studio)
                    {
                        double c = artists[artist].cost, gain = cur * (artists[artist].incomeMultiplier - 1 + (profile.concertArtists > artist ? 0.15 : 0));
                        if (c / Math.Max(gain, 1e-9) < bestScore) { bestScore = c / Math.Max(gain, 1e-9); bestCost = c; best = "ARTIST " + artists[artist].displayName; act = () => artist++; }
                    }
                    if (platform < platforms.Length)
                    {
                        double c = platforms[platform].releaseCost, gain = inc.streams / Math.Max(PlatformSum(), 1e-9) * platforms[platform].streamMultiplier;
                        if (c / Math.Max(gain, 1e-9) < bestScore) { bestScore = c / Math.Max(gain, 1e-9); bestCost = c; best = "PLATFORM " + platforms[platform].displayName; act = () => platform++; }
                    }
                    if (best == null || money < bestCost) break;
                    money -= bestCost;
                    act();
                    if (r.firstPurchaseAt < 0) r.firstPurchaseAt = playTime;
                    if (best.StartsWith("STUDIO") || best.StartsWith("HIRE") || best.StartsWith("GENRE"))
                        r.log.Add($"{Fmt(playTime)} {best} ${Short(bestCost)}");
                }
            }
            return r;
        }
    }
}
