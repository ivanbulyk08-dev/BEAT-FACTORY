using System;
using System.Collections.Generic;

namespace BeatFactory
{
    /// <summary>
    /// Everything that is written to the save file. Plain data only — no logic.
    /// When adding a field, give it a safe default so old save files still load.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Increase when the format changes in a way that needs migration.</summary>
        public int version = SaveManager.CurrentVersion;

        // Economy
        public double money;
        public double runEarnings;
        public double lifetimeEarnings;
        public double famePoints;
        public int prestigeCount;

        // Beats
        public long totalBeats;
        public long manualBeats;
        public long hitBeats;
        public long viralBeats;

        // Progression
        public List<ProducerSave> producers = new List<ProducerSave>();
        public int genresUnlocked = 1;
        public int studioLevel;
        public List<string> signedArtists = new List<string>();

        // Career (kept after prestige)
        public List<string> releasedAlbums = new List<string>();
        public PlayerAppearance appearance = new PlayerAppearance();
        public int bestStudioLevel;
        public List<ArtistCareer> artistCareers = new List<ArtistCareer>();
        public List<ActiveTour> activeTours = new List<ActiveTour>();
        public List<string> achievements = new List<string>();
        /// <summary>DRIP STUDIO outfits of detailed artists (item ids). Missing = default outfit.</summary>
        public List<ArtistOutfitSave> artistOutfits = new List<ArtistOutfitSave>();
        /// <summary>STORE items bought (wardrobe item ids).</summary>
        public List<string> storeOwned = new List<string>();
        // Goals & chemistry (kept after prestige)
        public GoalsSave goals = new GoalsSave();
        public ChemistrySave chemistry = new ChemistrySave();

        // Streams
        public double streams;
        public double lifetimeStreams;
        public List<string> releasedPlatforms = new List<string>();

        // Boosts
        public List<BoostSave> activeBoosts = new List<BoostSave>();
        public long nextFreeBoostUtcTicks;
        /// <summary>Next INSTANT VIRAL ad (cooldown). 0 = available.</summary>
        public long nextInstantViralUtcTicks;

        // Monetization
        public bool adsRemoved;

        // Tutorial
        public int tutorialStep;

        // Time (UTC ticks) — used for offline earnings.
        public long lastSaveUtcTicks;
        public long firstPlayUtcTicks;
        /// <summary>Latest device time ever seen. Offline time is counted from here, so moving the clock back and forth pays only once.</summary>
        public long maxSeenUtcTicks;

        public GameSettings settings = new GameSettings();
    }

    [Serializable]
    public class ProducerSave
    {
        public string id;
        public int level;
        public float timer;
    }
}
