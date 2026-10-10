using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>SETTINGS tab: music/sound/notification switches, replay tutorial, reset save, stats.</summary>
    public class SettingsScreenUI : MonoBehaviour
    {
        [SerializeField] ToggleSwitch musicToggle;
        [SerializeField] ToggleSwitch soundToggle;
        [SerializeField] ToggleSwitch notificationsToggle;
        [SerializeField] Slider masterSlider;
        [SerializeField] Slider musicSlider;
        [SerializeField] Slider sfxSlider;
        [Header("Concerts")]
        [SerializeField] ToggleSwitch concertMusicToggle;
        [SerializeField] Slider concertMusicSlider;
        [SerializeField] Button concertQualityButton;
        [SerializeField] TMP_Text concertQualityLabel;
        [SerializeField] Button replayTutorialButton;
        [SerializeField] Button resetButton;
        [SerializeField] TMP_Text statsText;
        [SerializeField] TMP_Text versionText;
        [SerializeField] Button languageButton;
        [SerializeField] Button privacyPolicyButton;
        [SerializeField] Button privacyOptionsButton;
        [SerializeField] ConfirmDialog confirmDialog;
        [SerializeField] TutorialUI tutorial;
        [SerializeField] ScreenNavigator navigator;
        [SerializeField] Color dangerColor = new Color(1f, 0.24f, 0.35f);

        GameManager gm;
        float timer;

        void Start()
        {
            gm = GameManager.Instance;
            var s = gm.Settings;

            musicToggle.Changed += on => { s.musicOn = on; s.NotifyChanged(); };
            soundToggle.Changed += on => { s.soundOn = on; s.NotifyChanged(); };
            notificationsToggle.Changed += on => { s.notificationsOn = on; s.NotifyChanged(); };
            masterSlider.onValueChanged.AddListener(v => { s.masterVolume = v; s.NotifyChanged(); });
            musicSlider.onValueChanged.AddListener(v => { s.musicVolume = v; s.NotifyChanged(); });
            sfxSlider.onValueChanged.AddListener(v => { s.sfxVolume = v; s.NotifyChanged(); });
            if (concertMusicToggle != null) concertMusicToggle.Changed += on => { s.concertMusicOn = on; s.NotifyChanged(); };
            if (concertMusicSlider != null) concertMusicSlider.onValueChanged.AddListener(v => { s.concertMusicVolume = v; s.NotifyChanged(); });
            if (concertQualityButton != null)
                concertQualityButton.onClick.AddListener(() =>
                {
                    // AUTO → LOW → MEDIUM → HIGH → AUTO
                    s.concertQuality = s.concertQuality >= 2 ? -1 : s.concertQuality + 1;
                    s.NotifyChanged();
                    RefreshQuality();
                });

            replayTutorialButton.onClick.AddListener(() =>
            {
                tutorial.Restart();
                navigator.Show("HOME");
            });

            resetButton.onClick.AddListener(() => confirmDialog.Show(
                Loc.T("settings.reset_title"),
                Loc.T("settings.reset_message"),
                Loc.T("settings.reset_confirm"), dangerColor,
                () => gm.ResetProgress()));

            if (languageButton != null)
                languageButton.onClick.AddListener(() => { if (LanguagePicker.Instance != null) LanguagePicker.Instance.Show(); });
            if (privacyPolicyButton != null)
                privacyPolicyButton.onClick.AddListener(() => { if (AppLinks.PrivacyPolicyUrl.Length > 0) Application.OpenURL(AppLinks.PrivacyPolicyUrl); });
            if (privacyOptionsButton != null)
                privacyOptionsButton.onClick.AddListener(() => AdsManager.Instance?.ShowPrivacyOptions());
            Loc.Changed += Refresh;
            Refresh();
        }

        void OnDestroy() => Loc.Changed -= Refresh;

        void OnEnable()
        {
            if (gm != null) Refresh();
        }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < 1f) return;
            timer = 0f;
            RefreshStats();
            RefreshPrivacy(); // consent status arrives asynchronously after launch
        }

        void Refresh()
        {
            var s = gm.Settings;
            musicToggle.SetImmediate(s.musicOn);
            soundToggle.SetImmediate(s.soundOn);
            notificationsToggle.SetImmediate(s.notificationsOn);
            masterSlider.SetValueWithoutNotify(s.masterVolume);
            musicSlider.SetValueWithoutNotify(s.musicVolume);
            sfxSlider.SetValueWithoutNotify(s.sfxVolume);
            if (concertMusicToggle != null) concertMusicToggle.SetImmediate(s.concertMusicOn);
            if (concertMusicSlider != null) concertMusicSlider.SetValueWithoutNotify(s.concertMusicVolume);
            RefreshQuality();
            RefreshStats();
            versionText.text = Loc.F("settings.version", "version", Application.version);
            if (languageButton != null)
                languageButton.GetComponentInChildren<TMP_Text>().text = "<size=80%>" + Loc.T("settings.language") + ":</size>  " + Loc.Info.nativeName;
            RefreshPrivacy();
        }

        /// <summary>Policy link only when it exists; consent settings only where Google requires them (EEA/UK).</summary>
        void RefreshPrivacy()
        {
            if (privacyPolicyButton != null) privacyPolicyButton.gameObject.SetActive(AppLinks.PrivacyPolicyUrl.Length > 0);
            if (privacyOptionsButton != null) privacyOptionsButton.gameObject.SetActive(AdsManager.Instance != null && AdsManager.Instance.PrivacyOptionsRequired);
        }

        static readonly string[] QualityKeys = { "settings.quality_low", "settings.quality_medium", "settings.quality_high" };

        void RefreshQuality()
        {
            if (concertQualityLabel == null) return;
            var s = gm.Settings;
            concertQualityLabel.text = s.concertQuality < 0
                ? Loc.F("settings.quality_auto", "quality", Loc.T(QualityKeys[s.ConcertQualityLevel]))
                : Loc.T(QualityKeys[s.concertQuality]);
        }

        void RefreshStats()
        {
            const string col = "<pos=62%><color=#F2F0FF><b>";
            const string end = "</b></color>\n";
            statsText.text =
                Loc.T("settings.stat_beats") + col + NumberFormatter.Format(gm.Beats.TotalBeats) + end +
                Loc.T("settings.stat_hits") + col + NumberFormatter.Format(gm.Beats.HitBeats) + " / " + NumberFormatter.Format(gm.Beats.ViralBeats) + end +
                Loc.T("settings.stat_money") + col + NumberFormatter.FormatMoney(gm.Economy.LifetimeEarnings) + end +
                Loc.T("settings.stat_streams") + col + NumberFormatter.Format(gm.Streams.LifetimeStreams) + end +
                Loc.T("settings.stat_restarts") + col + gm.Prestige.Count + "</b></color>";
        }
    }
}
