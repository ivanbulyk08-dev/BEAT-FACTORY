using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools.Tests
{
    /// <summary>
    /// Fast EditMode tests (Window > General > Test Runner > EditMode > Run All).
    /// They use the real game classes and data assets but never touch the player's save file or the open scene.
    /// </summary>
    public class BeatFactoryEditModeTests
    {
        const string Data = BeatFactorySceneBuilder.Root + "/Data/";

        // ------------------------------------------------------------------ core loop

        class Loop
        {
            public EconomyManager economy;
            public BeatManager beats;
            public ProducerManager producers;
            public StudioManager studios;
            public GenreManager genres;
            public ArtistManager artists;
            public StreamManager streams;
        }

        static Loop CreateLoop()
        {
            var eco = AssetDatabase.LoadAssetAtPath<EconomyConfig>(Data + "Config/EconomyConfig.asset");
            var quality = AssetDatabase.LoadAssetAtPath<BeatQualityConfig>(Data + "Config/BeatQualityConfig.asset");
            var streamCfg = AssetDatabase.LoadAssetAtPath<StreamConfig>(Data + "Config/StreamConfig.asset");
            var l = new Loop();
            var income = new MultiplierStack();
            var speed = new MultiplierStack();
            l.economy = new EconomyManager(eco);
            l.beats = new BeatManager(l.economy, eco, quality, income);
            l.producers = new ProducerManager(DefaultGameData.LoadProducers(), l.economy, l.beats, eco, speed, new MultiplierStack());
            l.beats.ProducerBaseIncome = () => l.producers.BaseIncomePerSecond;
            l.studios = new StudioManager(DefaultGameData.LoadStudios(), l.economy);
            l.producers.StudioLevel = () => l.studios.Level;
            l.genres = new GenreManager(DefaultGameData.LoadGenres(), l.economy, () => l.studios.Level);
            l.artists = new ArtistManager(DefaultGameData.LoadArtists(), l.economy, () => l.studios.Level);
            l.streams = new StreamManager(streamCfg, l.economy, l.beats);
            income.Register("Genre", () => l.genres.Multiplier);
            income.Register("Studio", () => l.studios.IncomeMultiplier);
            income.Register("Artists", () => l.artists.Multiplier);
            speed.Register("Studio", () => l.studios.SpeedMultiplier);
            return l;
        }

        [Test]
        public void MakeBeat_EarnsMoney()
        {
            var l = CreateLoop();
            l.beats.MakeManualBeat();
            Assert.Greater(l.economy.Money, 0);
        }

        [Test]
        public void FirstProducer_AffordableWithFewTaps()
        {
            var l = CreateLoop();
            var first = l.producers.Producers[0];
            int taps = 0;
            while (!l.producers.CanBuy(first, 1) && taps < 1000) { l.beats.MakeManualBeat(); taps++; }
            Assert.Less(taps, 200, "The first producer should be reachable within the first minute.");
            Assert.IsTrue(l.producers.TryBuyLevels(first, 1));
            Assert.Greater(l.producers.IncomePerSecond, 0);
        }

        [Test]
        public void Producers_EarnAutomatically_AndUpgradesIncreaseIncome()
        {
            var l = CreateLoop();
            var first = l.producers.Producers[0];
            l.economy.AddMoney(first.NextCost);
            Assert.IsTrue(l.producers.TryBuyLevels(first, 1));
            double before = l.economy.Money;
            for (int i = 0; i < 300; i++) l.producers.Tick(0.1f);
            Assert.Greater(l.economy.Money, before, "Hired producers must earn while time passes.");

            double incomeBefore = l.producers.IncomePerSecond;
            l.economy.AddMoney(1e6);
            Assert.IsTrue(l.producers.TryBuyLevels(first, 1));
            Assert.Greater(l.producers.IncomePerSecond, incomeBefore);
        }

        [Test]
        public void Studio_Genre_Artist_Purchases_Work()
        {
            var l = CreateLoop();
            l.economy.AddMoney(1e9);
            Assert.IsTrue(l.genres.TryUnlockNext());
            Assert.Greater(l.genres.Multiplier, 1);
            Assert.IsTrue(l.studios.TryUpgrade());
            Assert.AreEqual(1, l.studios.Level);
            var artist = l.artists.Artists[0];
            Assert.IsTrue(l.artists.TrySign(artist));
            Assert.Greater(l.artists.Multiplier, 1);
        }

        [Test]
        public void StudioPrices_MatchThePlan()
        {
            double[] plan = { 0, 5e6, 25e6, 150e6, 1e9, 7e9, 50e9, 350e9, 2.5e12, 20e12 };
            var studios = DefaultGameData.LoadStudios();
            Assert.AreEqual(plan.Length, studios.Length);
            for (int i = 1; i < plan.Length; i++) Assert.AreEqual(plan[i], studios[i].upgradeCost, studios[i].displayName);
        }

        // ------------------------------------------------------------------ saves

        static readonly MethodInfo TryRead = typeof(SaveManager).GetMethod("TryRead", BindingFlags.NonPublic | BindingFlags.Static);

        static string TempFile(string name, string content)
        {
            string dir = Path.Combine(Path.GetTempPath(), "bf_tests");
            Directory.CreateDirectory(dir);
            string f = Path.Combine(dir, name);
            File.WriteAllText(f, content);
            return f;
        }

        [Test]
        public void Save_RoundTrip_KeepsValues()
        {
            var data = new SaveData { money = 1234.5, totalBeats = 77, studioLevel = 3, maxSeenUtcTicks = 42 };
            data.settings.language = "ru";
            var back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            Assert.AreEqual(1234.5, back.money);
            Assert.AreEqual(77, back.totalBeats);
            Assert.AreEqual(3, back.studioLevel);
            Assert.AreEqual(42, back.maxSeenUtcTicks);
            Assert.AreEqual("ru", back.settings.language);
        }

        [Test]
        public void Save_OldVersion_IsMigrated()
        {
            string f = TempFile("v1.json", "{\"version\":1,\"money\":5,\"streams\":5000,\"lifetimeStreams\":10000}");
            var d = (SaveData)TryRead.Invoke(null, new object[] { f });
            Assert.NotNull(d);
            Assert.AreEqual(SaveManager.CurrentVersion, d.version);
            Assert.AreEqual(50, d.streams, 1e-9, "v2 converts old stream counts (÷100).");
            Assert.NotNull(d.goals);
            Assert.NotNull(d.chemistry);
            Assert.NotNull(d.artistCareers);
        }

        [Test]
        public void Save_CorruptedOrEmpty_ReturnsNullInsteadOfCrashing()
        {
            LogAssertIgnore();
            Assert.IsNull(TryRead.Invoke(null, new object[] { TempFile("bad.json", "{\"money\":12,\"produ") }));
            Assert.IsNull(TryRead.Invoke(null, new object[] { TempFile("empty.json", "") }));
        }

        static void LogAssertIgnore() => UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

        // ------------------------------------------------------------------ device clock & ad cooldown

        [Test]
        public void ClockGuard_MovingTheClockBackAndForth_PaysOnlyOnce()
        {
            var go = new GameObject("TestGM");
            try
            {
                var gm = go.AddComponent<GameManager>(); // Awake does not run in Edit Mode: only the clock math is used
                long now = DateTime.UtcNow.Ticks;
                long future = now + TimeSpan.FromHours(10).Ticks;
                // a previous session already "saw" a time 10 hours in the future (clock was moved forward)
                gm.LoadClock(future, 0);
                // the save is from one hour ago: nothing is paid, that time was already used
                Assert.AreEqual(0, gm.TrustedSecondsSince(now - TimeSpan.FromHours(1).Ticks), 1e-6);
                // normal case: nothing seen beyond the save → real time away is paid
                gm.LoadClock(0, 0);
                Assert.AreEqual(3600, gm.TrustedSecondsSince(DateTime.UtcNow.Ticks - TimeSpan.FromHours(1).Ticks), 2.0);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void InstantViral_Cooldown_NeverLongerThanOneCooldown()
        {
            var go = new GameObject("TestGM");
            try
            {
                var gm = go.AddComponent<GameManager>();
                gm.LoadClock(0, DateTime.UtcNow.AddDays(3).Ticks); // clock was moved back after watching
                Assert.LessOrEqual(gm.SecondsUntilInstantViral, GameManager.InstantViralCooldownMinutes * 60 + 1);
                gm.LoadClock(0, 0);
                Assert.IsTrue(gm.InstantViralReady);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        // ------------------------------------------------------------------ localization

        static Dictionary<string, string> Table(string code)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(BeatFactorySceneBuilder.Root + "/Resources/Localization/" + code + ".txt");
            Assert.NotNull(asset, "Missing table " + code);
            return LocalizationDatabase.Parse(asset.text);
        }

        static string Placeholders(string v) =>
            string.Join(",", Regex.Matches(v, @"\{[a-z0-9]+\}").Cast<Match>().Select(m => m.Value).Distinct().OrderBy(s => s));

        static readonly Regex PluralSuffix = new Regex(@"\.(one|few|many|other)$");

        [Test]
        public void Localization_AllLanguagesHaveEveryKey_WithSamePlaceholders()
        {
            var en = Table("en");
            var problems = new List<string>();
            foreach (var info in LanguageInfo.All)
            {
                if (info.id == Language.EN) continue;
                var t = Table(info.code);
                foreach (var pair in en)
                {
                    if (PluralSuffix.IsMatch(pair.Key))
                    {
                        string b = PluralSuffix.Replace(pair.Key, "");
                        if (!t.ContainsKey(b + ".other")) problems.Add(info.code + ": " + b + ".other");
                        continue;
                    }
                    if (!t.TryGetValue(pair.Key, out var v)) { problems.Add(info.code + ": missing " + pair.Key); continue; }
                    if (Placeholders(pair.Value) != Placeholders(v)) problems.Add(info.code + ": placeholders " + pair.Key);
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems.Take(30)));
        }

        [Test]
        public void Plurals_Russian_FollowSlavicRules()
        {
            var ru = LanguageInfo.Get(Language.RU);
            Assert.AreEqual("one", ru.PluralCategory(1));
            Assert.AreEqual("one", ru.PluralCategory(21));
            Assert.AreEqual("few", ru.PluralCategory(3));
            Assert.AreEqual("few", ru.PluralCategory(24));
            Assert.AreEqual("many", ru.PluralCategory(5));
            Assert.AreEqual("many", ru.PluralCategory(11));
            Assert.AreEqual("many", ru.PluralCategory(14));
            Assert.AreEqual("many", ru.PluralCategory(1_500_000)); // shown as "1,5M фанатов"
            Assert.AreEqual("other", ru.PluralCategory(1.5));
        }

        [Test]
        public void Plurals_OtherLanguages()
        {
            Assert.AreEqual("one", LanguageInfo.Get(Language.EN).PluralCategory(1));
            Assert.AreEqual("other", LanguageInfo.Get(Language.EN).PluralCategory(0));
            Assert.AreEqual("one", LanguageInfo.Get(Language.FR).PluralCategory(0));
            Assert.AreEqual("one", LanguageInfo.Get(Language.PT_BR).PluralCategory(1));
            Assert.AreEqual("other", LanguageInfo.Get(Language.ZH_CN).PluralCategory(1));
            Assert.AreEqual("other", LanguageInfo.Get(Language.KO).PluralCategory(1));
        }

        [Test]
        public void Localization_DeviceLanguageDetection()
        {
            Assert.AreEqual(Language.RU, LanguageInfo.FromSystem(SystemLanguage.Russian));
            Assert.AreEqual(Language.ZH_CN, LanguageInfo.FromSystem(SystemLanguage.ChineseSimplified));
            Assert.AreEqual(Language.PT_BR, LanguageInfo.FromSystem(SystemLanguage.Portuguese));
            Assert.AreEqual(Language.EN, LanguageInfo.FromSystem(SystemLanguage.Polish), "Unsupported → English");
            Assert.AreEqual(Language.EN, LanguageInfo.FromSystem(SystemLanguage.Ukrainian), "Unsupported → English");
        }

        [Test]
        public void Fonts_CoverEveryCharacterOfEveryTable()
        {
            var fonts = new[] { "Anton SDF", "Oswald SDF", "Roboto SDF" } // every game font, with its fallbacks
                .Select(n => AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(BeatFactorySceneBuilder.Root + "/Fonts/" + n + ".asset")).ToArray();
            var missing = new HashSet<char>();
            foreach (var info in LanguageInfo.All)
            {
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(BeatFactorySceneBuilder.Root + "/Resources/Localization/" + info.code + ".txt");
                foreach (char c in asset.text + asset.text.ToUpperInvariant())
                {
                    if (c < 32 || char.IsWhiteSpace(c) || char.IsSurrogate(c)) continue;
                    foreach (var f in fonts)
                        if (!Covers(f, c, 0)) { missing.Add(c); break; }
                }
            }
            Assert.IsEmpty(missing, "Characters without a glyph (rebuild: Tools > Beat Factory > Localization > Build Font Fallbacks): " + new string(missing.Take(40).ToArray()));
        }

        /// <summary>Read-only coverage check (never adds glyphs to the font assets).</summary>
        static bool Covers(TMPro.TMP_FontAsset f, char c, int depth)
        {
            if (f == null || depth > 3) return false;
            if (f.characterLookupTable != null && f.characterLookupTable.ContainsKey(c)) return true;
            if (f.atlasPopulationMode == TMPro.AtlasPopulationMode.Dynamic && f.sourceFontFile != null && f.sourceFontFile.HasCharacter(c)) return true;
            if (f.fallbackFontAssetTable != null)
                foreach (var fb in f.fallbackFontAssetTable) if (Covers(fb, c, depth + 1)) return true;
            return false;
        }
    }
}
