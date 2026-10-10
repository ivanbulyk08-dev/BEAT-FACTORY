using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Listens to gameplay events and decides which celebrations to show.
    /// Add new announcements here (studio upgrades, artists, prestige ...).
    /// </summary>
    public class GameAnnouncements : MonoBehaviour
    {
        [SerializeField] CelebrationPopup popup;

        [Header("Flash strength")]
        [SerializeField] float hitFlash = 0.25f;
        [SerializeField] float viralFlash = 0.55f;
        [SerializeField] float unlockFlash = 0.4f;

        // Higher priority popups are not interrupted by lower ones.
        const int PriorityHit = 1;
        const int PriorityViral = 2;
        const int PriorityUnlock = 3;

        [SerializeField] Color studioColor = new Color(0.24f, 0.88f, 1f);

        BeatManager beats;
        GenreManager genres;
        StudioManager studios;
        ArtistManager artists;

        void Start()
        {
            var gm = GameManager.Instance;
            beats = gm.Beats;
            genres = gm.Genres;
            studios = gm.Studios;
            artists = gm.Artists;
            beats.BeatsCreated += OnBeatsCreated;
            genres.GenreUnlocked += OnGenreUnlocked;
            studios.StudioUpgraded += OnStudioUpgraded;
            artists.ArtistSigned += OnArtistSigned;
            streams = gm.Streams;
            streams.PlatformReleased += OnPlatformReleased;
            prestige = gm.Prestige;
            prestige.Prestiged += OnPrestiged;
            boosts = gm.Boosts;
            boosts.BoostStarted += OnBoostStarted;
            gm.Achievements.Unlocked += OnAchievement;
            gm.Concerts.Trending += OnTrending;
            gm.Tours.TourStarted += OnTourStarted;
            gm.Missions.Completed += OnMissionCompleted;
            gm.Challenges.Completed += OnChallengeCompleted;
            gm.Chemistry.LevelUp += OnChemistryLevelUp;
        }

        void OnMissionCompleted(MissionDefinition m)
        {
            AudioManager.Play(SoundId.Unlock);
            popup.Show(Loc.T("notify.mission_complete"), m.Title, Loc.F("notify.claim_in_goals_reward", "reward", m.reward.Describe(GameManager.Instance)), new Color(0.95f, 0.76f, 0.31f), 0.25f, PriorityHit);
        }

        void OnChallengeCompleted(string title, bool weekly)
        {
            AudioManager.Play(SoundId.Unlock);
            popup.Show(Loc.T(weekly ? "notify.weekly_done" : "notify.daily_done"), Loc.Upper(title),
                Loc.T("notify.claim_in_goals"), new Color(0.24f, 0.88f, 1f), 0.2f, PriorityHit);
        }

        void OnChemistryLevelUp(ArtistData a, ProducerData p, ChemistryLevel level)
        {
            bool perfect = level == ChemistryLevel.Perfect;
            AudioManager.Play(perfect ? SoundId.Viral : SoundId.Unlock);
            popup.Show(perfect ? Loc.T("notify.perfect_chemistry") : Loc.F("notify.chemistry_level", "level", ArtistChemistryManager.LevelName(level)),
                a.displayName.ToUpperInvariant() + " × " + Loc.Upper(p.DisplayName),
                Loc.T(perfect ? "notify.chemistry_max" : "notify.chemistry_better"),
                perfect ? new Color(1f, 0.82f, 0.24f) : new Color(1f, 0.24f, 0.78f), perfect ? 0.5f : 0.2f, perfect ? PriorityUnlock : PriorityHit);
        }

        void OnAchievement(AchievementDef a)
        {
            AudioManager.Play(SoundId.Unlock);
            popup.Show(Loc.T("notify.achievement"), a.Title, a.Description, new Color(0.95f, 0.76f, 0.31f), unlockFlash, PriorityUnlock + 1);
        }

        void OnTrending(ArtistData a, double fans)
        {
            popup.Show(Loc.T("notify.trending"), a.displayName.ToUpperInvariant(), Loc.P("notify.trending_fans", fans), ArtistGenres.Color(a.genre), 0.15f, 0);
        }

        void OnTourStarted(ActiveTour t)
        {
            popup.Show(Loc.T("notify.tour_started"), t.Title, Loc.P("notify.tour_back", t.shows, "time", NumberFormatter.FormatDuration((t.endUtcTicks - t.startUtcTicks) / (double)System.TimeSpan.TicksPerSecond)),
                new Color(1f, 0.24f, 0.78f), unlockFlash, PriorityUnlock);
        }

        BoostManager boosts;

        void OnBoostStarted(BoostDefinition boost, float seconds)
        {
            // Lowest priority: any other celebration wins.
            popup.Show(Loc.T("notify.boost"), boost.DisplayName, "+" + NumberFormatter.FormatDuration(seconds), boost.color, 0.2f, 0);
        }

        StreamManager streams;
        PrestigeManager prestige;

        [SerializeField] Color fameColor = new Color(1f, 0.54f, 0.24f);

        void OnPrestiged(double fameGained)
        {
            popup.Show(Loc.T("notify.label_restarted"),
                Loc.F("notify.fame_gained", "fame", "+" + NumberFormatter.Format(fameGained)),
                Loc.F("common.income_forever", "mult", NumberFormatter.FormatMultiplier(GameManager.Instance.Economy.FameMultiplier)),
                fameColor,
                0.6f,
                PriorityUnlock + 1);
        }

        void OnPlatformReleased(StreamPlatform platform)
        {
            popup.Show(Loc.T("notify.now_streaming"),
                Loc.F("notify.on_platform", "platform", platform.displayName.ToUpperInvariant()),
                Loc.F("common.streams_mult", "mult", NumberFormatter.FormatMultiplier(platform.streamMultiplier)),
                platform.color,
                unlockFlash,
                PriorityUnlock);
        }

        void OnDestroy()
        {
            if (beats != null) beats.BeatsCreated -= OnBeatsCreated;
            if (genres != null) genres.GenreUnlocked -= OnGenreUnlocked;
            if (studios != null) studios.StudioUpgraded -= OnStudioUpgraded;
            if (artists != null) artists.ArtistSigned -= OnArtistSigned;
            if (streams != null) streams.PlatformReleased -= OnPlatformReleased;
            if (prestige != null) prestige.Prestiged -= OnPrestiged;
            if (boosts != null) boosts.BoostStarted -= OnBoostStarted;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.Achievements.Unlocked -= OnAchievement;
                gm.Concerts.Trending -= OnTrending;
                gm.Tours.TourStarted -= OnTourStarted;
                gm.Missions.Completed -= OnMissionCompleted;
                gm.Challenges.Completed -= OnChallengeCompleted;
                gm.Chemistry.LevelUp -= OnChemistryLevelUp;
            }
        }

        void OnArtistSigned(ArtistData artist)
        {
            // The contract popup has its own signing ceremony.
            if (ArtistContractPopup.Instance != null && ArtistContractPopup.Instance.IsOpen) return;
            popup.Show(Loc.T("notify.artist_signed"),
                artist.displayName.ToUpperInvariant(),
                Loc.F("common.income", "mult", NumberFormatter.FormatMultiplier(artist.incomeMultiplier)),
                artist.color,
                unlockFlash,
                PriorityUnlock);
        }

        void OnStudioUpgraded(StudioData studio)
        {
            popup.Show(Loc.T("notify.studio_upgrade"),
                Loc.Upper(studio.DisplayName),
                Loc.F("common.income", "mult", NumberFormatter.FormatMultiplier(studio.incomeMultiplier)) + "  " +
                Loc.F("common.speed", "mult", NumberFormatter.FormatMultiplier(studio.speedMultiplier)),
                studioColor,
                unlockFlash,
                PriorityUnlock);
        }

        void OnBeatsCreated(BeatResult result)
        {
            if (!result.IsSpecial) return;
            var tier = beats.QualityConfig.GetTier(result.Quality);
            bool viral = result.Quality == BeatQuality.Viral;

            popup.Show(Loc.F(viral ? "notify.viral_beat" : "notify.quality_beat", "quality", tier.Label),
                "\"" + result.Title + "\"",
                "+" + NumberFormatter.FormatMoney(result.Reward),
                tier.color,
                viral ? viralFlash : hitFlash,
                viral ? PriorityViral : PriorityHit);
        }

        void OnGenreUnlocked(GenreData genre)
        {
            popup.Show(Loc.T("notify.new_genre"),
                Loc.Upper(genre.DisplayName),
                Loc.F("common.income", "mult", NumberFormatter.FormatMultiplier(genre.incomeMultiplier)),
                genre.accentColor,
                unlockFlash,
                PriorityUnlock);
        }
    }
}
