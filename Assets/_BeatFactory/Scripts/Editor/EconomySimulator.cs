using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools
{
    /// <summary>
    /// Simulates players with the CURRENT data assets (producers, studios, genres, artists, albums, EconomyConfig).
    /// The simulated player buys whatever pays itself back fastest (studios, producers, genres, artists),
    /// releases albums when ready, and plays in sessions depending on the profile (offline time pays like the game).
    /// Menu: Tools > Beat Factory > Simulate Economy (all profiles).
    /// </summary>
    public static class EconomySimulator
    {
        public class Profile
        {
            public string name;
            /// <summary>Is the player in the game at second t?</summary>
            public Func<double, bool> playing;
            /// <summary>MAKE BEAT taps per second while playing.</summary>
            public Func<double, double> taps;
            /// <summary>Average extra income from boosts while playing (0.3 = +30%).</summary>
            public double boostBonus;
            public double hours;
        }

        public static readonly Profile Active = new Profile
        {
            name = "ACTIVE", hours = 30, boostBonus = 0.3,
            playing = t => t < 3 * 3600 || (t % 3600) < 20 * 60,
            taps = t => t < 120 ? 3 : t < 600 ? 2 : t < 1800 ? 0.7 : 0.25,
        };
        public static readonly Profile Optimized = new Profile
        {
            name = "OPTIMIZED", hours = 30, boostBonus = 1.2, // money ×2 + producer ×3 boosts kept up most of the time
            playing = t => t < 5 * 3600 || (t % 3600) < 30 * 60,
            taps = t => t < 600 ? 3 : 1,
        };
        public static readonly Profile Casual = new Profile
        {
            name = "CASUAL", hours = 96, boostBonus = 0.15,
            playing = t => (t % (3 * 3600)) < 10 * 60,
            taps = t => 1,
        };
        public static readonly Profile Offline = new Profile
        {
            name = "OFFLINE", hours = 240, boostBonus = 0.05,
            playing = t => (t % (8 * 3600)) < 5 * 60,
            taps = t => 0.3,
        };

        [MenuItem("Tools/Beat Factory/Simulate Economy")]
        static void Menu()
        {
            foreach (var p in new[] { Active, Optimized, Casual, Offline }) Debug.Log(Run(p, true));
        }

        /// <summary>Old entry point: active player for the given seconds.</summary>
        public static string Run(int seconds) => Run(new Profile
        {
            name = Active.name, hours = seconds / 3600.0, boostBonus = Active.boostBonus, playing = Active.playing, taps = Active.taps,
        }, false);

        static string Fmt(double t) => $"{(int)(t / 3600)}:{(int)(t / 60) % 60:00}";

        public static string Run(Profile profile, bool compact)
        {
            var eco = AssetDatabase.LoadAssetAtPath<EconomyConfig>(BeatFactorySceneBuilder.Root + "/Data/Config/EconomyConfig.asset");
            var producers = DefaultGameData.LoadProducers();
            var studios = DefaultGameData.LoadStudios();
            var genres = DefaultGameData.LoadGenres();
            var artists = DefaultGameData.LoadArtists();
            var albums = DefaultGameData.LoadAlbums();
            var tiers = BeatQualityConfig.CreateDefaultTiers();

            int[] level = new int[producers.Length];
            int studio = 0, genre = 1, artist = 0, album = 0;
            double money = eco.startingMoney, beats = 0, offlineAway = 0;
            var log = new StringBuilder();
            log.AppendLine($"=== {profile.name} player ===");

            double BeatValue()
            {
                double vc = studios[studio].viralChanceMultiplier <= 0 ? 1 : studios[studio].viralChanceMultiplier;
                double w = 0, q = 0, sShare = 0;
                foreach (var t in tiers)
                {
                    double c = t.quality >= BeatQuality.Hit ? t.chancePercent * vc : t.chancePercent;
                    w += c; q += c * t.multiplier; sShare += c * t.streamShare;
                }
                return q / w + sShare / w * 0.8;
            }
            double Global() => genres[genre - 1].incomeMultiplier * studios[studio].incomeMultiplier *
                               artists.Take(artist).Aggregate(1.0, (a, x) => a * x.incomeMultiplier) *
                               albums.Take(album).Aggregate(1.0, (a, x) => a * x.reputationMultiplier);
            double BaseOf(int i, int lv)
            {
                if (lv <= 0) return 0;
                int ms = eco.producerMilestoneEvery > 0 ? lv / eco.producerMilestoneEvery : 0;
                return producers[i].baseIncome * lv * Math.Pow(eco.producerMilestoneMultiplier, ms) / producers[i].cycleSeconds;
            }
            double ProducerBase(int[] lv)
            {
                double s = 0;
                for (int i = 0; i < producers.Length; i++) s += BaseOf(i, lv[i]);
                return s * studios[studio].speedMultiplier;
            }
            double Income(double t, int[] lv, double global, bool playing)
            {
                double prod = ProducerBase(lv) * global * BeatValue();
                if (!playing) return 0;
                double taps = profile.taps(t) * (eco.manualBeatBaseReward + eco.manualBeatProducerShare * ProducerBase(lv)) * global * BeatValue();
                return (prod + taps) * (1 + profile.boostBonus);
            }
            double Cost(int i) => level[i] == 0 ? producers[i].hireCost
                : producers[i].upgradeBaseCost * Math.Pow(producers[i].costGrowth > 0 ? producers[i].costGrowth : eco.upgradeCostGrowth, level[i] - 1);

            var studioTimes = new double[studios.Length];
            int nextReport = 0;
            double[] reports = { 600, 1800, 3600, 7200, 14400, 28800, 86400, 172800 };
            double end = profile.hours * 3600;
            const int dt = 2;

            for (double t = 0; t < end; t += dt)
            {
                bool playing = profile.playing(t);
                if (!playing)
                {
                    offlineAway += dt;
                    continue;
                }
                if (offlineAway > 0)
                {
                    // back from offline: producers worked at offline efficiency, capped by the studio's offline hours
                    double cap = (studios[studio].offlineHours > 0 ? studios[studio].offlineHours : eco.maxOfflineHours) * 3600;
                    money += ProducerBase(level) * Global() * BeatValue() * Math.Min(offlineAway, cap) * eco.offlineEfficiency;
                    beats += ProducerBeats(level) * Math.Min(offlineAway, cap);
                    offlineAway = 0;
                }

                double global = Global();
                double income = Income(t, level, global, true);
                money += income * dt;
                beats += (ProducerBeats(level) + profile.taps(t)) * dt;

                // albums release as soon as they are ready (in order)
                if (album < albums.Length)
                {
                    var a = albums[album];
                    int hired = level.Count(l => l > 0);
                    if (beats >= a.requiredBeats && hired >= a.requiredProducers && studio >= a.requiredStudio && artist >= a.requiredArtists)
                    {
                        money += Math.Max(100, ProducerBase(level) * global * BeatValue() * a.rewardIncomeSeconds);
                        album++;
                        if (!compact) log.AppendLine($"{Fmt(t)}  ALBUM {a.title}");
                    }
                }

                for (int guard = 0; guard < 60; guard++)
                {
                    string best = null;
                    double bestScore = double.MaxValue, bestCost = 0;
                    Action act = null;
                    for (int i = 0; i < producers.Length; i++)
                    {
                        if (i > 0 && level[i - 1] == 0) break;
                        if (studio < producers[i].requiredStudio) break;
                        var lv = (int[])level.Clone();
                        lv[i]++;
                        double gain = Income(t, lv, global, true) - income, c = Cost(i);
                        double score = c / Math.Max(gain, 1e-9);
                        if (score < bestScore) { int k = i; bestScore = score; bestCost = c; best = (level[i] == 0 ? "HIRE " : "UP ") + producers[i].displayName; act = () => level[k]++; }
                    }
                    if (genre < genres.Length && genres[genre].requiredStudioLevel <= studio)
                    {
                        double c = genres[genre].unlockCost, gain = income * (genres[genre].incomeMultiplier / genres[genre - 1].incomeMultiplier - 1);
                        if (c / Math.Max(gain, 1e-9) < bestScore) { bestScore = c / Math.Max(gain, 1e-9); bestCost = c; best = "GENRE " + genres[genre].displayName; act = () => genre++; }
                    }
                    if (studio + 1 < studios.Length)
                    {
                        var n = studios[studio + 1];
                        double newGlobal = global / studios[studio].incomeMultiplier * n.incomeMultiplier;
                        double gain = income * (newGlobal / global * n.speedMultiplier / studios[studio].speedMultiplier - 1);
                        // studios also unlock things: value them a bit higher than their raw multiplier
                        double score = n.upgradeCost / Math.Max(gain * 1.5, 1e-9);
                        if (score < bestScore) { bestScore = score; bestCost = n.upgradeCost; best = "STUDIO " + n.displayName; act = () => { studio++; studioTimes[studio] = t; }; }
                    }
                    if (artist < artists.Length && artists[artist].requiredStudioLevel <= studio)
                    {
                        double c = artists[artist].cost, gain = income * (artists[artist].incomeMultiplier - 1);
                        if (c / Math.Max(gain, 1e-9) < bestScore) { bestScore = c / Math.Max(gain, 1e-9); bestCost = c; best = "ARTIST " + artists[artist].displayName; act = () => artist++; }
                    }
                    if (best == null || money < bestCost) break;
                    money -= bestCost;
                    act();
                    income = Income(t, level, Global(), true);
                    if (!compact && (best.StartsWith("STUDIO") || best.StartsWith("HIRE")))
                        log.AppendLine($"{Fmt(t)}  {best}  ${bestCost:N0}  (income ${income:N0}/s)");
                }

                if (nextReport < reports.Length && t >= reports[nextReport])
                {
                    if (!compact)
                        log.AppendLine($"---- {Fmt(t)}: ${income:N0}/s, studio {studio}, producers [{string.Join(",", level)}], artists {artist}, albums {album}");
                    nextReport++;
                }
            }

            log.Append("Studios reached: ");
            for (int s = 1; s < studios.Length; s++)
                log.Append(studioTimes[s] > 0 ? $"L{s + 1} {Fmt(studioTimes[s])}  " : $"L{s + 1} —  ");
            log.AppendLine();
            return log.ToString();

            double ProducerBeats(int[] lv)
            {
                double b = 0;
                for (int i = 0; i < producers.Length; i++) if (lv[i] > 0) b += 1.0 / producers[i].cycleSeconds;
                return b * studios[studio].speedMultiplier;
            }
        }
    }
}
