using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools
{
    /// <summary>
    /// Creates the default ScriptableObject data (producers, later genres/studios/artists).
    /// Existing assets are NEVER overwritten, so your Inspector tweaks are safe.
    /// </summary>
    public static class DefaultGameData
    {
        public const string ProducersFolder = BeatFactorySceneBuilder.Root + "/Data/Producers";

        struct ProducerDef
        {
            public string id, name, letters, color;
            public double hire, upgrade, growth, income;
            public float cycle;
            public int studio;
        }

        // Balance v4 (see ECONOMY_BALANCE.md): hiring pays back in minutes early and hours late; upgrades cost
        // upgrade * growth^(level-1) with growth 1.6 → 2.0, so every producer saturates and the next one becomes the goal.
        // Each producer earns ~5x more per second than the previous one but costs 8-80x more.
        static readonly ProducerDef[] Producers =
        {
            new ProducerDef { id = "beginner",  name = "Beginner Producer",  letters = "BP", color = "3DE1FF", hire = 1_500,           upgrade = 400,            growth = 1.6, income = 1.4,    cycle = 4,  studio = 0 },
            new ProducerDef { id = "melody",    name = "Melody Maker",       letters = "MM", color = "4DFFA8", hire = 12_000,          upgrade = 3_000,          growth = 1.6, income = 12,     cycle = 6,  studio = 0 },
            new ProducerDef { id = "drum",      name = "Drum Producer",      letters = "DP", color = "FFD23D", hire = 100_000,         upgrade = 25_000,         growth = 1.7, income = 80,     cycle = 8,  studio = 0 },
            new ProducerDef { id = "sample",    name = "Sample Producer",    letters = "SP", color = "FF8A3D", hire = 8_000_000,       upgrade = 2_000_000,      growth = 1.9, income = 400,    cycle = 10, studio = 1 },
            new ProducerDef { id = "sound",     name = "Sound Designer",     letters = "SD", color = "B65CFF", hire = 250_000_000,     upgrade = 60_000_000,     growth = 2.0, income = 2_200,  cycle = 12, studio = 2 },
            new ProducerDef { id = "hitmaker",  name = "Hitmaker",           letters = "HM", color = "FF3DC8", hire = 5_000_000_000,   upgrade = 1_250_000_000,  growth = 2.0, income = 12_750, cycle = 15, studio = 3 },
            new ProducerDef { id = "superstar", name = "Superstar Producer", letters = "SS", color = "FF3D5A", hire = 140_000_000_000, upgrade = 35_000_000_000, growth = 2.0, income = 80_000, cycle = 20, studio = 4 },
        };

        /// <summary>
        /// Writes the default balance (prices, incomes, multipliers) onto EXISTING data assets.
        /// Normal scene builds never overwrite your tweaks; use this to reset the economy to the defaults.
        /// </summary>
        [MenuItem("Tools/Beat Factory/Apply Default Economy Balance")]
        public static void ApplyBalance()
        {
            var eco = AssetDatabase.LoadAssetAtPath<EconomyConfig>(BeatFactorySceneBuilder.Root + "/Data/Config/EconomyConfig.asset");
            if (eco != null)
            {
                var d = ScriptableObject.CreateInstance<EconomyConfig>();
                eco.manualBeatBaseReward = d.manualBeatBaseReward;
                eco.manualBeatProducerShare = d.manualBeatProducerShare;
                eco.upgradeCostGrowth = d.upgradeCostGrowth;
                eco.producerMilestoneEvery = d.producerMilestoneEvery;
                eco.producerMilestoneMultiplier = d.producerMilestoneMultiplier;
                eco.maxOfflineHours = d.maxOfflineHours;
                eco.offlineEfficiency = d.offlineEfficiency;
                eco.prestigeMinRunEarnings = d.prestigeMinRunEarnings;
                eco.fameUnitEarnings = d.fameUnitEarnings;
                Object.DestroyImmediate(d);
                EditorUtility.SetDirty(eco);
            }
            var producers = LoadProducers();
            for (int i = 0; i < producers.Length; i++)
            {
                var p = Producers[i];
                producers[i].hireCost = p.hire;
                producers[i].upgradeBaseCost = p.upgrade;
                producers[i].baseIncome = p.income;
                producers[i].cycleSeconds = p.cycle;
                producers[i].costGrowth = p.growth;
                producers[i].requiredStudio = p.studio;
                EditorUtility.SetDirty(producers[i]);
            }
            var studios = LoadStudios();
            for (int i = 0; i < studios.Length; i++)
            {
                ApplyStudio(studios[i], Studios[i]);
                EditorUtility.SetDirty(studios[i]);
            }
            var genres = LoadGenres();
            for (int i = 0; i < genres.Length; i++)
            {
                genres[i].unlockCost = Genres[i].cost;
                genres[i].incomeMultiplier = Genres[i].multiplier;
                genres[i].requiredStudioLevel = Genres[i].studio;
                EditorUtility.SetDirty(genres[i]);
            }
            var map = ArtistAssetsById();
            for (int i = 0; i < Artists.Length; i++)
            {
                var spec = Artists[i];
                if (!map.TryGetValue(spec.id, out var a)) continue;
                a.cost = ArtistCost(i, spec);
                a.incomeMultiplier = ArtistMultiplier(i);
                a.requiredStudioLevel = ArtistLevel(i);
                EditorUtility.SetDirty(a);
            }
            var albums = LoadAlbums();
            for (int i = 0; i < albums.Length && i < Albums.Length; i++)
            {
                albums[i].requiredBeats = Albums[i].beats;
                albums[i].requiredProducers = Albums[i].producers;
                albums[i].requiredStudio = Albums[i].studio;
                albums[i].requiredArtists = Albums[i].artists;
                albums[i].rewardIncomeSeconds = Albums[i].seconds;
                albums[i].reputationMultiplier = Albums[i].rep;
                EditorUtility.SetDirty(albums[i]);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Beat Factory] Default economy balance applied.");
        }

        /// <summary>
        /// The BeatQualityConfig asset was created before streams existed, so its streamShare values are 0.
        /// Fill them with the defaults once (only if ALL are still 0, so manual edits are kept).
        /// </summary>
        public static void MigrateQualityStreamShares(string path)
        {
            var config = AssetDatabase.LoadAssetAtPath<BeatQualityConfig>(path);
            if (config == null || config.tiers == null) return;
            foreach (var t in config.tiers)
                if (t.streamShare > 0) return;

            var defaults = BeatQualityConfig.CreateDefaultTiers();
            foreach (var t in config.tiers)
                foreach (var d in defaults)
                    if (d.quality == t.quality) t.streamShare = d.streamShare;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Generates missing placeholder sounds and fills the AudioConfig (only empty slots, so your own clips are kept).
        /// </summary>
        const string GeneratedAudioMarker = BeatFactorySceneBuilder.Root + "/Audio/";

        public static void EnsureAudio(string configPath)
        {
            AudioGen.SfxLibrary.GenerateAll(overwrite: false);
            var config = BeatFactorySceneBuilder.EnsureAsset<AudioConfig>(configPath);

            // id, category, volume, pitch variation, min interval, music duck
            var defs = new (SoundId id, SoundCategory cat, float vol, float pitch, float interval, float duck, SoundId source)[]
            {
                (SoundId.Beat,             SoundCategory.Production, 0.62f, 0.015f, 0.06f, 0f,   SoundId.Beat),
                (SoundId.BeatGood,         SoundCategory.Production, 0.62f, 0.01f,  0.06f, 0f,   SoundId.BeatGood),
                (SoundId.ProducerBeginner, SoundCategory.Production, 0.6f,  0.01f,  0.15f, 0f,   SoundId.ProducerBeginner),
                (SoundId.ProducerMelody,   SoundCategory.Production, 0.6f,  0.0f,   0.15f, 0f,   SoundId.ProducerMelody),
                (SoundId.ProducerDrums,    SoundCategory.Production, 0.6f,  0.01f,  0.15f, 0f,   SoundId.ProducerDrums),
                (SoundId.Hire,             SoundCategory.Production, 0.6f,  0.01f,  0.15f, 0f,   SoundId.ProducerBeginner),
                (SoundId.Upgrade,          SoundCategory.Production, 0.55f, 0.01f,  0.08f, 0f,   SoundId.Upgrade),
                (SoundId.Click,            SoundCategory.UI,         0.45f, 0.03f,  0.04f, 0f,   SoundId.Click),
                (SoundId.ClickStrong,      SoundCategory.UI,         0.55f, 0.01f,  0.08f, 0f,   SoundId.ClickStrong),
                (SoundId.Tab,              SoundCategory.UI,         0.4f,  0.03f,  0.05f, 0f,   SoundId.Tab),
                (SoundId.Money,            SoundCategory.SFX,        0.5f,  0.01f,  0.12f, 0f,   SoundId.Money),
                (SoundId.CashOut,          SoundCategory.SFX,        0.55f, 0.01f,  0.12f, 0f,   SoundId.Money),
                (SoundId.Collect,          SoundCategory.SFX,        0.6f,  0f,     0.3f,  0.3f, SoundId.Collect),
                (SoundId.Unlock,           SoundCategory.SFX,        0.62f, 0f,     0.3f,  0.3f, SoundId.Unlock),
                (SoundId.StudioTransition, SoundCategory.SFX,        0.7f,  0f,     0.5f,  0.5f, SoundId.StudioTransition),
                (SoundId.Boost,            SoundCategory.SFX,        0.55f, 0.01f,  0.3f,  0f,   SoundId.Boost),
                (SoundId.Hit,              SoundCategory.SFX,        0.72f, 0f,     0.3f,  0.35f, SoundId.Hit),
                (SoundId.Viral,            SoundCategory.SFX,        0.8f,  0f,     0.5f,  0.6f, SoundId.Viral),
                (SoundId.Prestige,         SoundCategory.SFX,        0.85f, 0f,     1f,    0.7f, SoundId.Prestige),
            };

            var list = new List<SoundEntry>(config.sounds ?? new SoundEntry[0]);
            foreach (var d in defs)
            {
                var entry = list.Find(e => e.id == d.id);
                bool isNew = entry == null;
                if (isNew)
                {
                    entry = new SoundEntry { id = d.id };
                    list.Add(entry);
                }

                // Keep YOUR clips: only (re)fill entries that are empty or still point at generated audio.
                bool generated = isNew || entry.variants == null || entry.variants.Length == 0
                                 || System.Array.TrueForAll(entry.variants, c => c == null || AssetDatabase.GetAssetPath(c).StartsWith(GeneratedAudioMarker));
                if (!generated) continue;

                var clips = new List<AudioClip>();
                foreach (var (path, _) in AudioGen.SfxLibrary.Library[d.source])
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip != null) clips.Add(clip);
                }
                entry.variants = clips.ToArray();
                entry.clip = null;
                entry.category = d.cat;
                entry.volume = d.vol;
                entry.pitchVariation = d.pitch;
                entry.minInterval = d.interval;
                entry.duckMusic = d.duck;
            }
            config.sounds = list.ToArray();

            if (config.music == null || AssetDatabase.GetAssetPath(config.music).StartsWith(GeneratedAudioMarker))
                config.music = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioGen.SfxLibrary.MusicPath);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            // Old single-file placeholder sounds are replaced by the new library.
            if (AssetDatabase.IsValidFolder(BeatFactorySceneBuilder.Root + "/Audio/Generated"))
                AssetDatabase.DeleteAsset(BeatFactorySceneBuilder.Root + "/Audio/Generated");
        }

        public const string ArtistsFolder = BeatFactorySceneBuilder.Root + "/Data/Artists";

        // The artist roster (identity + look + balance) lives in ArtGenerator.Roster (Art/ArtGenerator.Artists.cs).
        // Signed artists stack (multipliers are multiplied): 32 artists × ×1.06–×1.2 ≈ ×46 in total.
        static Art.ArtGenerator.ArtistSpec[] Artists => Art.ArtGenerator.Roster;

        /// <summary>Room that unlocks each roster artist (every room brings new artists).</summary>
        static readonly int[] ArtistRoom = { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 5, 5, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9 };
        /// <summary>Contract price as a share of the artist's room price (cheapest first in each room).</summary>
        static readonly double[] ArtistPriceShare = { 0.06, 0.12, 0.25, 0.45, 0.7 };

        public static int ArtistLevel(int order) => order < ArtistRoom.Length ? ArtistRoom[order] : StudioCount - 1;

        static double ArtistCost(int order, Art.ArtGenerator.ArtistSpec s)
        {
            int room = ArtistLevel(order);
            if (room == 0) return s.cost * 10; // basement artists: small hand-made prices ($150K–$600K)
            int first = System.Array.IndexOf(ArtistRoom, room);
            int k = Mathf.Clamp(order - first, 0, ArtistPriceShare.Length - 1);
            double raw = StudioCost(room) * ArtistPriceShare[k];
            double mag = System.Math.Pow(10, System.Math.Floor(System.Math.Log10(raw)) - 1);
            return System.Math.Round(raw / mag) * mag; // two significant digits, like a real price tag
        }

        /// <summary>Each signed artist multiplies all income a little; later rooms bring stronger artists (×1.03 → ×1.06, ≈×4 for all 32).</summary>
        static double ArtistMultiplier(int order)
        {
            int room = ArtistLevel(order);
            return room <= 1 ? 1.03 : room <= 4 ? 1.04 : room <= 7 ? 1.05 : 1.06;
        }

        static void ApplyRoster(ArtistData asset, Art.ArtGenerator.ArtistSpec s, int order)
        {
            asset.id = s.id;
            asset.displayName = s.name;
            asset.shortName = s.name.Length >= 2 ? s.name.Substring(0, 2).ToUpperInvariant() : s.name;
            asset.genre = s.genre;
            asset.role = ArtistGenres.Label(s.genre);
            asset.musicStyle = ArtistGenres.Label(s.genre);
            asset.personality = s.bio;
            asset.beatStyle = s.beat;
            asset.popularity = s.popularity;
            asset.monthlyListeners = s.listeners;
            asset.color = ArtistGenres.Color(s.genre);
            asset.cost = ArtistCost(order, s);
            asset.incomeMultiplier = ArtistMultiplier(order);
            asset.requiredStudioLevel = ArtistLevel(order);
        }

        /// <summary>Finds an artist asset by id anywhere in the Artists folder (file names may differ).</summary>
        static Dictionary<string, ArtistData> ArtistAssetsById()
        {
            var map = new Dictionary<string, ArtistData>();
            if (!AssetDatabase.IsValidFolder(ArtistsFolder)) return map;
            foreach (var guid in AssetDatabase.FindAssets("t:ArtistData", new[] { ArtistsFolder }))
            {
                var a = AssetDatabase.LoadAssetAtPath<ArtistData>(AssetDatabase.GUIDToAssetPath(guid));
                if (a != null && !string.IsNullOrEmpty(a.id) && !map.ContainsKey(a.id)) map[a.id] = a;
            }
            return map;
        }

        // ------------------------------------------------------------------ albums (fictional titles)

        public const string AlbumsFolder = BeatFactorySceneBuilder.Root + "/Data/Albums";

        struct AlbumDef
        {
            public string id, title, kind, tagline, accent;
            public AlbumGenre genre;
            public long beats;
            public int producers, studio, artists;
            public double seconds, rep;
        }

        static readonly AlbumDef[] Albums =
        {
            new AlbumDef { id = "basement_tapes", title = "BASEMENT TAPES", kind = "FIRST TAPE", genre = AlbumGenre.Underground, accent = "FF3DC8",
                tagline = "Recorded between the pipes and the cables.", beats = 25, producers = 1, studio = 0, artists = 0, seconds = 120, rep = 1.10 },
            new AlbumDef { id = "night_shift", title = "NIGHT SHIFT", kind = "EP", genre = AlbumGenre.Trap, accent = "FF3D5A",
                tagline = "Dark 808s made after midnight.", beats = 400, producers = 2, studio = 1, artists = 0, seconds = 180, rep = 1.12 },
            new AlbumDef { id = "neon_riot", title = "NEON RIOT", kind = "MIXTAPE", genre = AlbumGenre.Rage, accent = "3DE1FF",
                tagline = "Distorted synths, zero rules.", beats = 3000, producers = 3, studio = 2, artists = 1, seconds = 240, rep = 1.16 },
            new AlbumDef { id = "cold_block", title = "COLD BLOCK", kind = "DEBUT ALBUM", genre = AlbumGenre.Drill, accent = "B8C2D6",
                tagline = "Minimal, cold, relentless.", beats = 15000, producers = 4, studio = 4, artists = 2, seconds = 300, rep = 1.2 },
            new AlbumDef { id = "velvet_hours", title = "VELVET HOURS", kind = "ALBUM", genre = AlbumGenre.RnB, accent = "FFB38A",
                tagline = "Warm keys and late-night vocals.", beats = 80000, producers = 5, studio = 6, artists = 3, seconds = 360, rep = 1.25 },
            new AlbumDef { id = "signal_noise", title = "SIGNAL // NOISE", kind = "DELUXE ALBUM", genre = AlbumGenre.Electronic, accent = "4DFFA8",
                tagline = "The future, on repeat.", beats = 400000, producers = 6, studio = 8, artists = 4, seconds = 420, rep = 1.30 },
        };

        public static string AlbumPath(int index) => $"{AlbumsFolder}/{index + 1:00}_{Albums[index].id}.asset";

        public static void EnsureAlbums()
        {
            for (int i = 0; i < Albums.Length; i++)
            {
                var d = Albums[i];
                string coverPath = Art.ArtGenerator.AlbumCoverPath(d.id);
                if (!System.IO.File.Exists(coverPath))
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(coverPath));
                    Art.ArtGenerator.AlbumCoverArt(d.genre, i).Save(coverPath);
                }

                string path = AlbumPath(i);
                var asset = AssetDatabase.LoadAssetAtPath<AlbumData>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<AlbumData>();
                    asset.id = d.id;
                    asset.title = d.title;
                    asset.kind = d.kind;
                    asset.tagline = d.tagline;
                    asset.genre = d.genre;
                    ColorUtility.TryParseHtmlString("#" + d.accent, out asset.accent);
                    asset.requiredBeats = d.beats;
                    asset.requiredProducers = d.producers;
                    asset.requiredStudio = d.studio;
                    asset.requiredArtists = d.artists;
                    asset.rewardIncomeSeconds = d.seconds;
                    asset.reputationMultiplier = d.rep;
                    BeatFactorySceneBuilder.EnsureFolderPublic(AlbumsFolder);
                    AssetDatabase.CreateAsset(asset, path);
                }
                if (asset.cover == null)
                {
                    asset.cover = AssetDatabase.LoadAssetAtPath<Sprite>(coverPath);
                    EditorUtility.SetDirty(asset);
                }
            }
            AssetDatabase.SaveAssets();
        }

        public static AlbumData[] LoadAlbums()
        {
            var list = new List<AlbumData>();
            for (int i = 0; i < Albums.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<AlbumData>(AlbumPath(i));
                if (asset != null) list.Add(asset);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Creates missing roster artists. Assets from before the roster (no beat style yet) are upgraded once
        /// to their roster identity and balance; after that your Inspector edits are kept.
        /// </summary>
        public static void EnsureArtists()
        {
            BeatFactorySceneBuilder.EnsureFolderPublic(ArtistsFolder);
            var existing = ArtistAssetsById();
            for (int i = 0; i < Artists.Length; i++)
            {
                var spec = Artists[i];
                if (existing.TryGetValue(spec.id, out var asset))
                {
                    if (string.IsNullOrEmpty(asset.beatStyle))
                    {
                        ApplyRoster(asset, spec, i);
                        asset.avatar = null; // regenerated portrait is assigned by the builder
                        EditorUtility.SetDirty(asset);
                    }
                    continue;
                }
                asset = ScriptableObject.CreateInstance<ArtistData>();
                ApplyRoster(asset, spec, i);
                AssetDatabase.CreateAsset(asset, $"{ArtistsFolder}/{i + 1:00}_{spec.id}.asset");
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Artists in roster order (= price order).</summary>
        public static ArtistData[] LoadArtists()
        {
            var map = ArtistAssetsById();
            var list = new List<ArtistData>();
            foreach (var spec in Artists)
                if (map.TryGetValue(spec.id, out var a)) list.Add(a);
            return list.ToArray();
        }

        public const string StudiosFolder = BeatFactorySceneBuilder.Root + "/Data/Studios";

        struct StudioDef
        {
            public string id, name, panel, tagline;
            public double cost, speed, income, offline, viral;
            public int bars;
            public string[] perks;
        }

        // Rooms v2: 10 long-term milestones. Prices grow faster (×5–8 per room) than the income bonus (×1.5–2 per room),
        // so income keeps rising fast but the next room is always a big goal. Ids are kept from v1 for save compatibility.
        static readonly StudioDef[] Studios =
        {
            new StudioDef { id = "bedroom",      name = "Basement",                cost = 0,                   income = 1,   speed = 1,    offline = 2,  viral = 1,    bars = 6,  panel = "17132B",
                tagline = "Pipes, cables and a cheap monitor. Everyone starts somewhere.", perks = new[] { "Your first studio" } },
            new StudioDef { id = "small",        name = "Bedroom Studio",          cost = 5_000_000,           income = 2,   speed = 1.1,  offline = 3,  viral = 1.1,  bars = 9,  panel = "1A1633",
                tagline = "A real desk, real monitors, LED vibes.", perks = new[] { "Better monitors & MIDI keyboard", "LED lighting" } },
            new StudioDef { id = "professional", name = "Small Pro Studio",        cost = 25_000_000,          income = 2.5, speed = 1.2,  offline = 4,  viral = 1.2,  bars = 12, panel = "1D183B",
                tagline = "Acoustic treatment. You're taking this seriously now.", perks = new[] { "Acoustic treatment", "Studio rack gear" } },
            new StudioDef { id = "complex",      name = "Professional Studio",     cost = 150_000_000,         income = 3,   speed = 1.3,  offline = 5,  viral = 1.35, bars = 16, panel = "211A45",
                tagline = "Mixing console, vocal booth, multiple screens.", perks = new[] { "Vocal booth", "Mixing console" } },
            new StudioDef { id = "company",      name = "Modern Studio",           cost = 1_000_000_000,       income = 4,   speed = 1.4,  offline = 6,  viral = 1.5,  bars = 20, panel = "251C50",
                tagline = "The billion-dollar room. Neon, glass and gold records.", perks = new[] { "Modern equipment", "Gold records wall" } },
            new StudioDef { id = "label",        name = "Luxury Studio",           cost = 7_000_000_000,       income = 5,   speed = 1.5,  offline = 8,  viral = 1.75, bars = 24, panel = "2A1D5C",
                tagline = "Leather, marble, chandeliers. Producers fight to work here.", perks = new[] { "Luxury lounge", "Curved ultra-wide screen" } },
            new StudioDef { id = "empire",       name = "Major Studio",            cost = 50_000_000_000,      income = 6,   speed = 1.6,  offline = 9,  viral = 2,    bars = 28, panel = "301E6A",
                tagline = "Multiple rooms, multiple sessions, all day.", perks = new[] { "Second production room", "Live room" } },
            new StudioDef { id = "elite",        name = "Elite Studio",            cost = 350_000_000_000,     income = 7.5, speed = 1.75, offline = 10, viral = 2.3,  bars = 30, panel = "34206E",
                tagline = "LED wall, artist lounge, light beams. Spectacular.", perks = new[] { "LED video wall", "Artist lounge" } },
            new StudioDef { id = "record_label", name = "Record Label Studio",     cost = 2_500_000_000_000,   income = 9,   speed = 1.9,  offline = 12, viral = 2.6,  bars = 32, panel = "382274",
                tagline = "Your own label HQ above the city.", perks = new[] { "Executive floor", "Skyline view" } },
            new StudioDef { id = "mega_complex", name = "Mega Music Complex",      cost = 20_000_000_000_000,  income = 12,  speed = 2,    offline = 14, viral = 3,    bars = 32, panel = "3C247A",
                tagline = "Stage, arena lights and a hundred speakers. The final boss.", perks = new[] { "Arena stage", "Ultimate prestige room" } },
        };

        public static int StudioCount => Studios.Length;
        public static double StudioCost(int level) => Studios[Mathf.Clamp(level, 0, Studios.Length - 1)].cost;

        public static string StudioPath(int index) =>
            $"{StudiosFolder}/{index + 1:00}_{Studios[index].id}.asset";

        static void ApplyStudio(StudioData asset, StudioDef d)
        {
            asset.id = d.id;
            asset.displayName = d.name;
            asset.upgradeCost = d.cost;
            asset.speedMultiplier = d.speed;
            asset.incomeMultiplier = d.income;
            asset.offlineHours = d.offline;
            asset.viralChanceMultiplier = d.viral;
            asset.equalizerBars = d.bars;
            asset.tagline = d.tagline;
            asset.unlocks = d.perks;
            ColorUtility.TryParseHtmlString("#" + d.panel, out asset.panelColor);
        }

        /// <summary>Creates missing rooms; v1 rooms (no tagline yet) are upgraded once to the v2 progression.</summary>
        public static void EnsureStudios()
        {
            BeatFactorySceneBuilder.EnsureFolderPublic(StudiosFolder);
            for (int i = 0; i < Studios.Length; i++)
            {
                string path = StudioPath(i);
                var existing = AssetDatabase.LoadAssetAtPath<StudioData>(path);
                if (existing != null)
                {
                    if (string.IsNullOrEmpty(existing.tagline))
                    {
                        ApplyStudio(existing, Studios[i]);
                        EditorUtility.SetDirty(existing);
                    }
                    continue;
                }
                var asset = ScriptableObject.CreateInstance<StudioData>();
                ApplyStudio(asset, Studios[i]);
                AssetDatabase.CreateAsset(asset, path);
            }
            AssetDatabase.SaveAssets();
        }

        public static StudioData[] LoadStudios()
        {
            var list = new List<StudioData>();
            for (int i = 0; i < Studios.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<StudioData>(StudioPath(i));
                if (asset != null) list.Add(asset);
            }
            return list.ToArray();
        }

        public const string GenresFolder = BeatFactorySceneBuilder.Root + "/Data/Genres";

        struct GenreDef
        {
            public string id, name, accent, background;
            public double cost, multiplier;
            public int studio;
        }

        // Each genre multiplies ALL income. Later genres need a better studio (Phase 8).
        static readonly GenreDef[] Genres =
        {
            new GenreDef { id = "lofi",         name = "Lo-Fi",        cost = 0,                           multiplier = 1,   studio = 0, accent = "3DE1FF", background = "0D0B1A" },
            new GenreDef { id = "hiphop",       name = "Hip-Hop",      cost = 250,                         multiplier = 1.3, studio = 0, accent = "FFD23D", background = "120E12" },
            new GenreDef { id = "trap",         name = "Trap",         cost = 2_000_000,                   multiplier = 1.5, studio = 1, accent = "FF3D5A", background = "140A10" },
            new GenreDef { id = "rage",         name = "Rage",         cost = 40_000_000,                  multiplier = 1.75, studio = 2, accent = "FF5A2D", background = "160A08" },
            new GenreDef { id = "hyperpop",     name = "Hyperpop",     cost = 1_500_000_000,               multiplier = 2,   studio = 4, accent = "FF3DC8", background = "160A18" },
            new GenreDef { id = "electronic",   name = "Electronic",   cost = 80_000_000_000,              multiplier = 2.25, studio = 6, accent = "3DFFB0", background = "07141A" },
            new GenreDef { id = "experimental", name = "Experimental", cost = 600_000_000_000,             multiplier = 2.5, studio = 7, accent = "B65CFF", background = "0F0820" },
            new GenreDef { id = "future",       name = "Future",       cost = 40_000_000_000_000,          multiplier = 3,   studio = 9, accent = "E0F0FF", background = "06101E" },
        };

        public static string GenrePath(int index) =>
            $"{GenresFolder}/{index + 1:00}_{Genres[index].id}.asset";

        public static void EnsureGenres()
        {
            for (int i = 0; i < Genres.Length; i++)
            {
                string path = GenrePath(i);
                if (AssetDatabase.LoadAssetAtPath<GenreData>(path) != null) continue;

                var d = Genres[i];
                var asset = ScriptableObject.CreateInstance<GenreData>();
                asset.id = d.id;
                asset.displayName = d.name;
                asset.unlockCost = d.cost;
                asset.incomeMultiplier = d.multiplier;
                asset.requiredStudioLevel = d.studio;
                ColorUtility.TryParseHtmlString("#" + d.accent, out asset.accentColor);
                ColorUtility.TryParseHtmlString("#" + d.background, out asset.backgroundColor);
                AssetDatabase.CreateAsset(asset, path);
            }
            AssetDatabase.SaveAssets();
        }

        public static GenreData[] LoadGenres()
        {
            var list = new List<GenreData>();
            for (int i = 0; i < Genres.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GenreData>(GenrePath(i));
                if (asset != null) list.Add(asset);
            }
            return list.ToArray();
        }

        public static string ProducerPath(int index) =>
            $"{ProducersFolder}/{index + 1:00}_{Producers[index].id}.asset";

        /// <summary>Creates missing producer assets.</summary>
        public static void EnsureProducers()
        {
            for (int i = 0; i < Producers.Length; i++)
            {
                string path = ProducerPath(i);
                if (AssetDatabase.LoadAssetAtPath<ProducerData>(path) != null) continue;

                var d = Producers[i];
                var asset = ScriptableObject.CreateInstance<ProducerData>();
                asset.id = d.id;
                asset.displayName = d.name;
                asset.shortName = d.letters;
                ColorUtility.TryParseHtmlString("#" + d.color, out asset.color);
                asset.hireCost = d.hire;
                asset.upgradeBaseCost = d.upgrade;
                asset.costGrowth = d.growth;
                asset.baseIncome = d.income;
                asset.cycleSeconds = d.cycle;
                AssetDatabase.CreateAsset(asset, path);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Loads producer assets in game order (call after NewScene).</summary>
        public static ProducerData[] LoadProducers()
        {
            var list = new List<ProducerData>();
            for (int i = 0; i < Producers.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ProducerData>(ProducerPath(i));
                if (asset != null) list.Add(asset);
            }
            return list.ToArray();
        }
    }
}
