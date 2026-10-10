using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Entry point of the game. Creates every gameplay system and gives other scripts access to them
    /// through GameManager.Instance (for example: GameManager.Instance.Economy.AddMoney(100)).
    /// It does NOT contain gameplay logic itself — each system lives in its own class.
    /// </summary>
    [DefaultExecutionOrder(-1000)] // Runs before UI scripts so systems exist when the UI subscribes.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configs (drag the assets from Assets/_BeatFactory/Data/Config)")]
        [SerializeField] EconomyConfig economyConfig;
        [SerializeField] BeatQualityConfig beatQualityConfig;
        [SerializeField] StreamConfig streamConfig;
        [SerializeField] BoostConfig boostConfig;

        [Header("Game Data (order = order in the game)")]
        [SerializeField] ProducerData[] producers;
        [SerializeField] GenreData[] genres;
        [SerializeField] StudioData[] studios;
        [SerializeField] ArtistData[] artists;
        [SerializeField] AlbumData[] albums;

        [Header("Performance")]
        [SerializeField] int targetFrameRate = 60;

        [Header("Saving")]
        [Tooltip("Seconds between automatic saves.")]
        [SerializeField] float autosaveInterval = 10f;
        [Tooltip("Turn off to test without loading/saving progress.")]
        [SerializeField] bool savingEnabled = true;

        public EconomyConfig EconomyConfig => economyConfig;
        public GameSettings Settings { get; private set; } = new GameSettings();
        public SaveManager Save { get; private set; }

        /// <summary>Seconds between the last save and this game start (0 on a fresh game). Used for offline earnings.</summary>
        public double SecondsAway { get; private set; }

        /// <summary>True if this session started from an existing save file.</summary>
        public bool LoadedFromSave { get; private set; }

        /// <summary>"Remove Ads" purchased (future in-app purchase). Saved.</summary>
        public bool AdsRemoved
        {
            get => adsRemoved;
            set { adsRemoved = value; RequestSave(); }
        }
        bool adsRemoved;

        /// <summary>Used by the SaveManager.</summary>
        public void LoadAdsRemoved(bool value) => adsRemoved = value;

        /// <summary>Current tutorial step (TutorialUI.StepCount = finished). Saved.</summary>
        public int TutorialStep
        {
            get => tutorialStep;
            set
            {
                if (tutorialStep == value) return;
                tutorialStep = value;
                RequestSave();
            }
        }
        int tutorialStep;

        /// <summary>Used by the SaveManager.</summary>
        public void LoadTutorialStep(int value) => tutorialStep = value;

        SaveData lastSaveData;
        float autosaveTimer;
        bool saveRequested;
        bool isResetting;
        public EconomyManager Economy { get; private set; }
        /// <summary>All income multipliers combined (fame, genre, studio, artists, boosts).</summary>
        public MultiplierStack Income { get; private set; }
        /// <summary>All production speed multipliers combined (studio, boosts).</summary>
        public MultiplierStack Speed { get; private set; }
        /// <summary>Multipliers that only affect producers' income (PRODUCER BOOST).</summary>
        public MultiplierStack ProducerIncome { get; private set; }
        public BoostManager Boosts { get; private set; }
        public BeatManager Beats { get; private set; }
        public ProducerManager Producers { get; private set; }
        public StudioManager Studios { get; private set; }
        public GenreManager Genres { get; private set; }
        public ArtistManager Artists { get; private set; }
        public AlbumManager Albums { get; private set; }
        public AppearanceManager Appearance { get; private set; }
        public StreamManager Streams { get; private set; }
        public PrestigeManager Prestige { get; private set; }
        public OfflineEarningsManager Offline { get; private set; }
        public AchievementManager Achievements { get; private set; }
        /// <summary>Artist careers: fans, popularity, concerts.</summary>
        public ConcertManager Concerts { get; private set; }
        public TourManager Tours { get; private set; }
        /// <summary>Gameplay stats for missions and challenges.</summary>
        public StatsTracker Stats { get; private set; }
        public MissionManager Missions { get; private set; }
        public ChallengeManager Challenges { get; private set; }
        public ArtistChemistryManager Chemistry { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Application.targetFrameRate = targetFrameRate;

            if (economyConfig == null)
            {
                Debug.LogError("[GameManager] EconomyConfig is not assigned! Using default values.", this);
                economyConfig = ScriptableObject.CreateInstance<EconomyConfig>();
            }

            if (beatQualityConfig == null)
            {
                Debug.LogError("[GameManager] BeatQualityConfig is not assigned! Using default values.", this);
                beatQualityConfig = ScriptableObject.CreateInstance<BeatQualityConfig>();
            }

            if (streamConfig == null)
            {
                Debug.LogError("[GameManager] StreamConfig is not assigned! Using default values.", this);
                streamConfig = ScriptableObject.CreateInstance<StreamConfig>();
            }

            if (boostConfig == null)
            {
                Debug.LogError("[GameManager] BoostConfig is not assigned! Using default values.", this);
                boostConfig = ScriptableObject.CreateInstance<BoostConfig>();
            }

            Economy = new EconomyManager(economyConfig);
            Income = new MultiplierStack();
            Speed = new MultiplierStack();
            ProducerIncome = new MultiplierStack();
            Boosts = new BoostManager(boostConfig);
            Beats = new BeatManager(Economy, economyConfig, beatQualityConfig, Income);
            Producers = new ProducerManager(producers ?? new ProducerData[0], Economy, Beats, economyConfig, Speed, ProducerIncome);
            Beats.ProducerBaseIncome = () => Producers.BaseIncomePerSecond;
            Studios = new StudioManager(studios ?? new StudioData[0], Economy);
            Producers.StudioLevel = () => Studios.Level;
            Genres = new GenreManager(genres ?? new GenreData[0], Economy, () => Studios.Level);
            Artists = new ArtistManager(artists ?? new ArtistData[0], Economy, () => Studios.Level);
            Albums = new AlbumManager(albums ?? new AlbumData[0], this);
            Appearance = new AppearanceManager();
            Streams = new StreamManager(streamConfig, Economy, Beats);
            // Offline time is paid without boosts: divide out every boost effect, including VIRAL BOOST's better beat quality.
            Offline = new OfflineEarningsManager(economyConfig, Producers, Beats, () =>
                Boosts.ProducerIncomeFactor * Beats.Quality.AverageMultiplier / Beats.Quality.AverageMultiplierAt(StudioViralChance));
            Offline.MaxHoursOverride = () => Studios.Current != null ? Studios.Current.offlineHours : 0;
            Prestige = new PrestigeManager(this, economyConfig);
            Achievements = new AchievementManager();
            Concerts = new ConcertManager(this, Achievements);
            Tours = new TourManager(this, Concerts);
            Stats = new StatsTracker(this);
            Missions = new MissionManager(this, Stats);
            Challenges = new ChallengeManager(this, Stats, new DeviceTime());
            Chemistry = new ArtistChemistryManager(this);

            RegisterMultipliers();
            RegisterCareerHooks();
            RegisterGoalHooks();
            LoadGame();
            RegisterSaveTriggers();
            // existing saves: missions already achieved complete silently (claimable), today's challenges are created
            Stats.SnapshotLevels();
            Missions.Check(notify: false);
            Challenges.Refresh();
            UpdateSpecialChance();

            // Producers kept working while the game was closed (tours run on real time and need nothing here).
            Offline.Process(SecondsAway);
            Concerts.ProcessOffline(SecondsAway, Offline.MaxOfflineSeconds);
        }

        /// <summary>Missions/challenges read the real stats; chemistry grows from real work together and gives small bonuses.</summary>
        void RegisterGoalHooks()
        {
            Stats.Hook();
            Producers.ProducerProduced += Chemistry.OnProduced;
            Albums.Released += (_, __) => Chemistry.OnAlbum();
            Concerts.ConcertPerformed += Chemistry.OnConcert;
            Tours.TourCollected += Chemistry.OnTour;
            Streams.BonusMultiplier = () => Chemistry.StreamMultiplier;
            ArtistCareer.ExtraPerformance = a => Chemistry.Modifier(a).performance;
            ArtistCareer.ExtraFanGrowth = a => Chemistry.Modifier(a).fanGrowth;
            Chemistry.LevelUp += (_, __, ___) => UpdateSpecialChance();
            Chemistry.Changed += () => chemistryDirty = true;
            Artists.Changed += UpdateSpecialChance;
        }

        bool chemistryDirty;
        float chemistryTimer;

        /// <summary>HIT/VIRAL chance = VIRAL BOOST × studio gear × artist chemistry.</summary>
        void UpdateSpecialChance()
        {
            if (Beats == null || Chemistry == null) return;
            Beats.Quality.SpecialChanceMultiplier = Boosts.ViralChanceMultiplier * StudioViralChance * Chemistry.ViralChanceMultiplier;
            Income.NotifyChanged();
        }

        /// <summary>Artist careers grow from the rest of the game: viral beats, albums, better studios.</summary>
        void RegisterCareerHooks()
        {
            Beats.BeatsCreated += Concerts.OnBeat;
            Albums.Released += (album, _) => Concerts.OnAlbumReleased(album);
            Studios.StudioUpgraded += _ => Concerts.OnStudioUpgraded();
        }

        /// <summary>
        /// Reward for the INSTANT VIRAL ad: one guaranteed VIRAL beat (×100). Its base is the bigger of a manual beat
        /// and 5 seconds of producer base income (≈ 3-4 minutes of income), so it helps without skipping progression.
        /// </summary>
        /// <summary>HIT/VIRAL chance bonus from the current studio's gear.</summary>
        public float StudioViralChance => Studios?.Current != null ? (float)System.Math.Max(1.0, Studios.Current.viralChanceMultiplier) : 1f;

        public BeatResult GiveInstantViral()
        {
            NextInstantViralUtcTicks = System.DateTime.UtcNow.AddMinutes(InstantViralCooldownMinutes).Ticks;
            RequestSave();
            double baseReward = System.Math.Max(Beats.ManualBeatBase, Producers.BaseIncomePerSecond * 5.0);
            return Beats.CreateGuaranteedBeat(baseReward, BeatQuality.Viral, BeatSource.Manual);
        }

        // ------------------------------------------------------------------ saving

        void LoadGame()
        {
            Save = new SaveManager();
            if (!savingEnabled) return;

            lastSaveData = Save.Read();
            if (lastSaveData == null) return;

            SaveManager.Apply(this, lastSaveData);
            LoadedFromSave = true;

            if (lastSaveData.lastSaveUtcTicks > 0)
                SecondsAway = TrustedSecondsSince(lastSaveData.lastSaveUtcTicks);
        }

        // ------------------------------------------------------------------ device clock

        /// <summary>Latest device time ever seen (saved). The device clock can be changed by the player.</summary>
        public long MaxSeenUtcTicks { get; private set; }

        /// <summary>Records "now" if it is the latest time seen and returns the latest time (for the save).</summary>
        public long NoteTrustedNow()
        {
            MaxSeenUtcTicks = System.Math.Max(MaxSeenUtcTicks, System.DateTime.UtcNow.Ticks);
            return MaxSeenUtcTicks;
        }

        /// <summary>
        /// Seconds since <paramref name="sinceTicks"/>, but never counting time the game has already "seen":
        /// moving the clock forward pays once, moving it back and forward again pays nothing.
        /// A clock moved back gives 0 (no penalty, no reward).
        /// </summary>
        public double TrustedSecondsSince(long sinceTicks)
        {
            long from = System.Math.Max(sinceTicks, MaxSeenUtcTicks);
            long now = System.DateTime.UtcNow.Ticks;
            NoteTrustedNow();
            return System.Math.Max(0, (now - from) / (double)System.TimeSpan.TicksPerSecond);
        }

        /// <summary>Used by the SaveManager.</summary>
        public void LoadClock(long maxSeenUtcTicks, long nextInstantViralUtcTicks)
        {
            MaxSeenUtcTicks = maxSeenUtcTicks;
            // clock moved back? never make the player wait longer than one cooldown
            long maxNext = System.DateTime.UtcNow.AddMinutes(InstantViralCooldownMinutes).Ticks;
            NextInstantViralUtcTicks = System.Math.Min(System.Math.Max(0, nextInstantViralUtcTicks), maxNext);
        }

        // ------------------------------------------------------------------ INSTANT VIRAL (rewarded ad) cooldown

        /// <summary>Minutes between two INSTANT VIRAL ads (unlimited ads made progression too fast).</summary>
        public const float InstantViralCooldownMinutes = 15f;
        public long NextInstantViralUtcTicks { get; private set; }
        public bool InstantViralReady => System.DateTime.UtcNow.Ticks >= NextInstantViralUtcTicks;
        public double SecondsUntilInstantViral => System.Math.Max(0, (NextInstantViralUtcTicks - System.DateTime.UtcNow.Ticks) / (double)System.TimeSpan.TicksPerSecond);

        /// <summary>Important purchases trigger a save soon after, not only the timed autosave.</summary>
        void RegisterSaveTriggers()
        {
            Producers.ProducerChanged += _ => RequestSave();
            Genres.GenreUnlocked += _ => RequestSave();
            Studios.StudioUpgraded += _ => RequestSave();
            Artists.ArtistSigned += _ => RequestSave();
            Albums.Released += (_, __) => SaveNow();
            Appearance.Changed += RequestSave;
            Streams.PlatformReleased += _ => RequestSave();
            Streams.CashedOut += _ => RequestSave();
            Prestige.Prestiged += _ => SaveNow(); // save immediately: a lost prestige would feel terrible
            Settings.Changed += RequestSave;
            Boosts.BoostStarted += (_, __) => RequestSave();
            Concerts.ConcertPerformed += _ => SaveNow();
            Concerts.Trending += (_, __) => RequestSave();
            Tours.TourStarted += _ => SaveNow();
            Tours.TourCollected += _ => SaveNow();
            Achievements.Unlocked += _ => RequestSave();
            Missions.Completed += _ => RequestSave();
            Missions.Claimed += (_, __) => SaveNow();
            Challenges.Changed += RequestSave;
        }

        /// <summary>Asks for a save at the end of this frame (cheap to call often).</summary>
        public void RequestSave() => saveRequested = true;

        public void SaveNow()
        {
            if (!savingEnabled || isResetting || Save == null) return;
            lastSaveData = SaveManager.Capture(this, lastSaveData);
            Save.Write(lastSaveData);
            autosaveTimer = 0f;
            saveRequested = false;
        }

        /// <summary>Deletes all progress and restarts the game scene.</summary>
        public void ResetProgress()
        {
            isResetting = true;
            Save?.Delete();
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.buildIndex >= 0
                ? gameObject.scene.buildIndex
                : 0);
        }

        void LateUpdate()
        {
            autosaveTimer += Time.unscaledDeltaTime;
            // Requested saves are batched (max 1/s) — e.g. dragging a volume slider asks for a save every frame.
            if ((saveRequested && autosaveTimer >= 1f) || autosaveTimer >= autosaveInterval) SaveNow();
        }

        System.DateTime? pausedAtUtc;

        // Mobile: the app can be killed at any moment after going to background, so save then.
        // When it comes back (without being killed), pay offline earnings for the time in background.
        void OnApplicationPause(bool paused)
        {
            // In the Editor, the Pause button also calls this — don't treat it as "app went to background".
            if (Application.isEditor)
            {
                if (paused) SaveNow();
                return;
            }

            // An ad SDK took over the screen: just save, it's not the player leaving.
            if (AdsManager.Instance != null && AdsManager.Instance.IsShowingAd)
            {
                if (paused) SaveNow();
                return;
            }

            if (paused)
            {
                // Never leave uncollected money behind: it would be lost if the app gets killed.
                Offline.Collect();
                pausedAtUtc = System.DateTime.UtcNow;
                NoteTrustedNow();
                SaveNow();
            }
            else if (pausedAtUtc.HasValue)
            {
                double seconds = TrustedSecondsSince(pausedAtUtc.Value.Ticks);
                pausedAtUtc = null;
                Offline.Process(seconds);
                Concerts.ProcessOffline(seconds, Offline.MaxOfflineSeconds);
            }
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) SaveNow();
        }

        void OnApplicationQuit()
        {
            Offline.Collect();
            SaveNow();
        }

        /// <summary>Connects every system's multiplier to the income and speed stacks.</summary>
        void RegisterMultipliers()
        {
            Income.Register("Fame", () => Economy.FameMultiplier);
            Economy.FameChanged += _ => Income.NotifyChanged();

            Income.Register("Genre", () => Genres.Multiplier);
            Genres.CurrentChanged += _ => Income.NotifyChanged();

            Income.Register("Artists", () => Artists.Multiplier);
            Artists.Changed += Income.NotifyChanged;

            Income.Register("Albums", () => Albums.Multiplier);
            Albums.Changed += Income.NotifyChanged;

            // Customization items unlock with the best studio ever reached (kept after prestige).
            Studios.CurrentChanged += _ => Appearance.ReachTier(Studios.Level);

            Income.Register("Studio", () => Studios.IncomeMultiplier);
            Speed.Register("Studio", () => Studios.SpeedMultiplier);
            Studios.CurrentChanged += _ =>
            {
                Income.NotifyChanged();
                Speed.NotifyChanged();
            };

            Income.Register("Boost", () => Boosts.IncomeMultiplier);
            Speed.Register("Boost", () => Boosts.SpeedMultiplier);
            ProducerIncome.Register("Boost", () => Boosts.ProducerIncomeMultiplier);
            // HIT/VIRAL chance = VIRAL BOOST × the studio's gear bonus
            Studios.CurrentChanged += _ =>
            {
                Beats.Quality.SpecialChanceMultiplier = Boosts.ViralChanceMultiplier * StudioViralChance * (Chemistry != null ? Chemistry.ViralChanceMultiplier : 1f);
                Income.NotifyChanged();
            };
            Boosts.Changed += () =>
            {
                Beats.Quality.SpecialChanceMultiplier = Boosts.ViralChanceMultiplier * StudioViralChance * (Chemistry != null ? Chemistry.ViralChanceMultiplier : 1f);
                Income.NotifyChanged();
                Speed.NotifyChanged();
                ProducerIncome.NotifyChanged();
            };

            // A VIRAL beat rewards a short 2X MONEY boost.
            Beats.BeatsCreated += result =>
            {
                if (result.Quality == BeatQuality.Viral && result.Source != BeatSource.Offline && boostConfig.viralBeatMoneyBoostSeconds > 0)
                    Boosts.Activate(BoostType.Money, boostConfig.viralBeatMoneyBoostSeconds);
            };
        }

        void Update()
        {
            // The only per-frame gameplay update: all producers are ticked from here.
            Producers.Tick(Time.deltaTime);
            Boosts.Tick(Time.unscaledDeltaTime);
            Concerts.Tick(Time.unscaledDeltaTime);
            Tours.Tick(Time.unscaledDeltaTime);
            Missions.Tick(Time.unscaledDeltaTime);
            Challenges.Tick(Time.unscaledDeltaTime);
            chemistryTimer += Time.unscaledDeltaTime;
            if (chemistryDirty && chemistryTimer > 1f)
            {
                chemistryTimer = 0;
                chemistryDirty = false;
                UpdateSpecialChance();
                RequestSave();
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---- Debug helpers: right-click the GameManager component header in Play Mode ----

        [ContextMenu("Debug/Add $1,000")]
        void DebugAddSmallMoney() => Economy?.AddMoney(1_000);

        [ContextMenu("Debug/Add $1M")]
        void DebugAddBigMoney() => Economy?.AddMoney(1_000_000);

        [ContextMenu("Debug/Force HIT beat")]
        void DebugHit() => DebugForceQuality(BeatQuality.Hit);

        [ContextMenu("Debug/Force VIRAL beat")]
        void DebugViral() => DebugForceQuality(BeatQuality.Viral);

        void DebugForceQuality(BeatQuality quality)
        {
            if (Beats == null) return;
            // Temporarily make special beats certain, create one beat, then restore.
            var tiers = beatQualityConfig.tiers;
            var saved = new float[tiers.Length];
            for (int i = 0; i < tiers.Length; i++)
            {
                saved[i] = tiers[i].chancePercent;
                tiers[i].chancePercent = tiers[i].quality == quality ? 1f : 0f;
            }
            Beats.MakeManualBeat();
            for (int i = 0; i < tiers.Length; i++) tiers[i].chancePercent = saved[i];
        }

        [ContextMenu("Debug/Unlock next genre (free)")]
        void DebugUnlockGenre()
        {
            Genres?.ForceUnlockNext();
        }

        [ContextMenu("Debug/Upgrade studio (free)")]
        void DebugUpgradeStudio() => Studios?.ForceUpgrade();

        [ContextMenu("Debug/Sign next artist (free)")]
        void DebugSignArtist()
        {
            if (Artists == null) return;
            foreach (var a in Artists.Artists)
                if (!Artists.IsSigned(a)) { Artists.ForceSign(a); return; }
        }

        [ContextMenu("Debug/Artists: +50% popularity, ×10 fans, +2000 XP")]
        void DebugBoostArtists()
        {
            foreach (var a in Concerts.SignedArtists())
            {
                var c = Concerts.Career(a);
                c.popularity = System.Math.Min(100f, c.popularity + 50f);
                c.fans *= 10;
                c.concertXp += 2000;
                c.restUntilTicks = 0;
            }
            Concerts.NotifyChanged();
        }

        [ContextMenu("Debug/Challenges: start a new day + week")]
        void DebugResetChallenges() => Challenges.DebugForceReset();

        [ContextMenu("Debug/Finish all tours now")]
        void DebugFinishTours()
        {
            foreach (var t in Tours.Active) t.endUtcTicks = System.DateTime.UtcNow.Ticks;
        }

        [ContextMenu("Debug/Add 100K streams")]
        void DebugAddStreams() => Streams?.AddStreams(100_000);

        [ContextMenu("Debug/Add $1B (unlock prestige)")]
        void DebugAddPrestigeMoney() => Economy?.AddMoney(1_000_000_000);

        [ContextMenu("Debug/Activate all boosts")]
        void DebugAllBoosts()
        {
            foreach (var b in boostConfig.boosts) Boosts?.Activate(b.type);
        }

        [ContextMenu("Debug/Add 10 Fame")]
        void DebugAddFame() => Economy?.AddFame(10);

        [ContextMenu("Debug/Simulate 2h 35m offline")]
        void DebugOffline() => Offline?.Process(2 * 3600 + 35 * 60);

        [ContextMenu("Debug/Simulate 12h offline (capped)")]
        void DebugOfflineCapped() => Offline?.Process(12 * 3600);

        [ContextMenu("Debug/Save now")]
        void DebugSave()
        {
            SaveNow();
            Debug.Log("[GameManager] Saved to " + Save.FilePath);
        }

        [ContextMenu("Debug/Reset progress (delete save)")]
        void DebugReset() => ResetProgress();
    }
}
