using System;
using System.IO;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Saves and loads the game as a JSON file in Application.persistentDataPath.
    /// Writes are safe: the new file is written to a temp file first, and the previous
    /// save is kept as a backup that is used if the main file is ever corrupted.
    /// </summary>
    public class SaveManager
    {
        public const int CurrentVersion = 2;
        const string FileName = "beatfactory_save.json";

        readonly string path;
        readonly string tempPath;
        readonly string backupPath;

        public SaveManager()
        {
            path = Path.Combine(Application.persistentDataPath, FileName);
            tempPath = path + ".tmp";
            backupPath = path + ".bak";
        }

        public string FilePath => path;
        public bool HasSave => File.Exists(path) || File.Exists(backupPath);

        /// <summary>Time of the last successful save (UTC).</summary>
        public DateTime LastSaveUtc { get; private set; }

        // ------------------------------------------------------------------ file IO

        public bool Write(SaveData data)
        {
            try
            {
                data.version = CurrentVersion;
                data.lastSaveUtcTicks = DateTime.UtcNow.Ticks;
                string json = JsonUtility.ToJson(data, false);

                File.WriteAllText(tempPath, json);
                if (File.Exists(path)) File.Copy(path, backupPath, true);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tempPath, path);

                LastSaveUtc = new DateTime(data.lastSaveUtcTicks, DateTimeKind.Utc);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveManager] Save failed: " + e.Message);
                return false;
            }
        }

        /// <summary>Loads the save file (or its backup). Returns null if there is no valid save.</summary>
        public SaveData Read()
        {
            var data = TryRead(path);
            if (data == null && File.Exists(backupPath))
            {
                Debug.LogWarning("[SaveManager] Main save missing or corrupted, loading backup.");
                data = TryRead(backupPath);
            }
            if (data == null) QuarantineUnreadable();
            if (data != null && data.lastSaveUtcTicks > 0)
                LastSaveUtc = new DateTime(data.lastSaveUtcTicks, DateTimeKind.Utc);
            return data;
        }

        /// <summary>
        /// Both the save and its backup exist but can't be read: keep copies before the new game's autosave
        /// overwrites them (progress can still be recovered by hand or by a future fix).
        /// </summary>
        void QuarantineUnreadable()
        {
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            foreach (var f in new[] { path, backupPath })
            {
                try
                {
                    if (!File.Exists(f) || new FileInfo(f).Length == 0) continue;
                    string copy = f + ".corrupt-" + stamp;
                    File.Copy(f, copy, true);
                    Debug.LogWarning("[SaveManager] Unreadable save kept as " + Path.GetFileName(copy));
                }
                catch (Exception e) { Debug.LogError("[SaveManager] Could not keep unreadable save: " + e.Message); }
            }
        }

        static SaveData TryRead(string file)
        {
            try
            {
                if (!File.Exists(file)) return null;
                string json = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(json)) return null;
                var data = JsonUtility.FromJson<SaveData>(json);
                return Migrate(data);
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveManager] Could not read " + Path.GetFileName(file) + ": " + e.Message);
                return null;
            }
        }

        /// <summary>Upgrades old save formats. Add steps here when CurrentVersion increases.</summary>
        static SaveData Migrate(SaveData data)
        {
            if (data == null) return null;
            if (data.producers == null) data.producers = new System.Collections.Generic.List<ProducerSave>();
            if (data.settings == null) data.settings = new GameSettings();
            if (data.signedArtists == null) data.signedArtists = new System.Collections.Generic.List<string>();
            if (data.activeBoosts == null) data.activeBoosts = new System.Collections.Generic.List<BoostSave>();
            if (data.releasedPlatforms == null) data.releasedPlatforms = new System.Collections.Generic.List<string>();
            if (data.releasedAlbums == null) data.releasedAlbums = new System.Collections.Generic.List<string>();
            if (data.appearance == null) data.appearance = new PlayerAppearance();
            if (data.artistCareers == null) data.artistCareers = new System.Collections.Generic.List<ArtistCareer>();
            if (data.activeTours == null) data.activeTours = new System.Collections.Generic.List<ActiveTour>();
            if (data.achievements == null) data.achievements = new System.Collections.Generic.List<string>();
            if (data.artistOutfits == null) data.artistOutfits = new System.Collections.Generic.List<ArtistOutfitSave>();
            if (data.storeOwned == null) data.storeOwned = new System.Collections.Generic.List<string>();
            if (data.goals == null) data.goals = new GoalsSave();
            if (data.chemistry == null) data.chemistry = new ChemistrySave();
            if (data.genresUnlocked < 1) data.genresUnlocked = 1;
            // v2: 1 stream = $1 (was $0.01), so old stream counts are divided by 100 to keep their money value.
            if (data.version < 2)
            {
                data.streams /= 100.0;
                data.lifetimeStreams /= 100.0;
            }
            data.version = CurrentVersion;
            return data;
        }

        /// <summary>Deletes the save and its backup.</summary>
        public void Delete()
        {
            foreach (var f in new[] { path, tempPath, backupPath })
            {
                try { if (File.Exists(f)) File.Delete(f); }
                catch (Exception e) { Debug.LogError("[SaveManager] Could not delete " + f + ": " + e.Message); }
            }
            LastSaveUtc = default;
        }

        // ------------------------------------------------------------------ game <-> data

        /// <summary>Collects the current state of every system into a SaveData.</summary>
        public static SaveData Capture(GameManager gm, SaveData previous)
        {
            var data = new SaveData
            {
                money = gm.Economy.Money,
                runEarnings = gm.Economy.RunEarnings,
                lifetimeEarnings = gm.Economy.LifetimeEarnings,
                famePoints = gm.Economy.FamePoints,
                prestigeCount = gm.Prestige.Count,

                totalBeats = gm.Beats.TotalBeats,
                manualBeats = gm.Beats.ManualBeats,
                hitBeats = gm.Beats.HitBeats,
                viralBeats = gm.Beats.ViralBeats,

                genresUnlocked = gm.Genres.UnlockedCount,
                studioLevel = gm.Studios.Level,
                signedArtists = gm.Artists.GetSignedIds(),
                releasedAlbums = gm.Albums.GetReleasedIds(),
                appearance = gm.Appearance.Current.Clone(),
                bestStudioLevel = gm.Appearance.BestTier,
                artistCareers = gm.Concerts.GetSaveData(),
                activeTours = gm.Tours.GetSaveData(),
                achievements = gm.Achievements.GetIds(),
                artistOutfits = gm.Wardrobe.GetSaveData(),
                storeOwned = gm.Wardrobe.GetOwned(),
                goals = CaptureGoals(gm),
                chemistry = gm.Chemistry.GetSaveData(),

                streams = gm.Streams.Streams,
                lifetimeStreams = gm.Streams.LifetimeStreams,
                releasedPlatforms = gm.Streams.GetReleasedIds(),

                activeBoosts = gm.Boosts.GetSaveData(),
                nextFreeBoostUtcTicks = gm.Boosts.NextFreeUtcTicks,
                adsRemoved = gm.AdsRemoved,
                tutorialStep = gm.TutorialStep,

                maxSeenUtcTicks = gm.NoteTrustedNow(),
                nextInstantViralUtcTicks = gm.NextInstantViralUtcTicks,
                firstPlayUtcTicks = previous != null && previous.firstPlayUtcTicks > 0 ? previous.firstPlayUtcTicks : DateTime.UtcNow.Ticks,
                settings = gm.Settings,
            };

            foreach (var p in gm.Producers.Producers)
                data.producers.Add(new ProducerSave { id = p.Data.id, level = p.Level, timer = p.CycleTimer });

            return data;
        }

        static GoalsSave CaptureGoals(GameManager gm)
        {
            var g = new GoalsSave { stats = gm.Stats.GetSaveData() };
            gm.Missions.Save(g);
            gm.Challenges.Save(g);
            return g;
        }

        /// <summary>Restores every system from a SaveData.</summary>
        public static void Apply(GameManager gm, SaveData data)
        {
            gm.Economy.LoadState(data.money, data.runEarnings, data.lifetimeEarnings, data.famePoints);
            gm.Beats.LoadState(data.totalBeats, data.manualBeats, data.hitBeats, data.viralBeats);
            foreach (var p in data.producers)
                gm.Producers.LoadProducer(p.id, p.level, p.timer);
            // Studio before genres: genre requirements depend on the studio level.
            gm.Studios.LoadState(data.studioLevel);
            gm.Genres.LoadState(data.genresUnlocked);
            gm.Artists.LoadState(data.signedArtists);
            gm.Albums.LoadState(data.releasedAlbums);
            gm.Achievements.LoadState(data.achievements);
            gm.Wardrobe.LoadState(data.artistOutfits);
            gm.Wardrobe.LoadOwned(data.storeOwned);
            gm.Concerts.LoadState(data.artistCareers);
            gm.Tours.LoadState(data.activeTours);
            gm.Stats.LoadState(data.goals?.stats);
            gm.Missions.Load(data.goals);
            gm.Challenges.Load(data.goals);
            gm.Chemistry.LoadState(data.chemistry);
            gm.Appearance.LoadState(data.appearance, System.Math.Max(data.bestStudioLevel, data.studioLevel));
            gm.Streams.LoadState(data.streams, data.lifetimeStreams, data.releasedPlatforms);
            gm.Prestige.LoadState(data.prestigeCount);
            gm.Boosts.LoadState(data.activeBoosts, data.nextFreeBoostUtcTicks);
            gm.LoadAdsRemoved(data.adsRemoved);
            gm.LoadClock(data.maxSeenUtcTicks, data.nextInstantViralUtcTicks);
            gm.LoadTutorialStep(data.tutorialStep);
            gm.Settings.CopyFrom(data.settings);
        }
    }
}
