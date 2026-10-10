using System;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>Rewarded ad placements (names are sent to the ad network for reporting).</summary>
    public static class AdPlacements
    {
        public const string DoubleOffline = "double_offline_earnings";
        public const string Boost = "boost";
        public const string InstantViral = "instant_viral";
        public const string AfterPrestige = "after_prestige";
    }

    /// <summary>
    /// The only class the game talks to for ads. Decides WHEN ads may show (frequency caps)
    /// and forwards to an IAdsProvider (test overlay now, real SDK later).
    /// Rewarded ads are always the player's choice; interstitials are rare and only at natural breaks.
    /// </summary>
    public class AdsManager : MonoBehaviour
    {
        public static AdsManager Instance { get; private set; }

        [Header("TEST MODE — keep ON while developing")]
        [Tooltip("ON: every build uses Google's official TEST ad units (safe: no real ads, no risk for your AdMob account).\nOFF: release builds use your real ad units below.")]
        [SerializeField] bool useTestAdUnits = true;
        [Tooltip("Editor: show the placeholder overlay instead of AdMob (AdMob ads only run on a phone).")]
        [SerializeField] bool fakeAdsInEditor = true;
        [Header("Test ads")]
        [SerializeField] FakeAdOverlay fakeOverlay;

        // The App IDs live in Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset (Assets → Google Mobile Ads → Settings).
        public const string AndroidAppId = "ca-app-pub-7578662571343965~7686882859";

        [Header("REAL ad units (used only when Test Mode is OFF)")]
        [SerializeField] string androidInterstitialId = "ca-app-pub-7578662571343965/5100317777";
        [Tooltip("Paste your Android Rewarded ad unit here when you create it in AdMob.")]
        [SerializeField] string androidRewardedId = "";
        [SerializeField] string iosInterstitialId = "";
        [SerializeField] string iosRewardedId = "";

        // Google's official test ad units — always safe to use
        public const string TestInterstitialAndroid = "ca-app-pub-3940256099942544/1033173712";
        public const string TestRewardedAndroid = "ca-app-pub-3940256099942544/5224354917";
        public const string TestInterstitialIOS = "ca-app-pub-3940256099942544/4411468910";
        public const string TestRewardedIOS = "ca-app-pub-3940256099942544/1712485313";

        [Header("Rules")]
        [SerializeField] bool rewardedEnabled = true;
        [SerializeField] bool interstitialsEnabled = true;
        [Tooltip("No interstitial during the first N seconds of a session.")]
        [SerializeField] float interstitialStartDelay = 300f;
        [Tooltip("At least N seconds between two interstitials (and after any rewarded ad).")]
        [SerializeField] float interstitialMinInterval = 240f;
        [SerializeField] int maxInterstitialsPerSession = 5;

        IAdsProvider provider;
        float lastAdTime = -9999f;
        int interstitialsShown;

        /// <summary>Set by a future "Remove Ads" purchase (saved in GameManager). Disables interstitials; rewarded stay available.</summary>
        public bool AdsRemoved
        {
            get => GameManager.Instance != null && GameManager.Instance.AdsRemoved;
            set { if (GameManager.Instance != null) GameManager.Instance.AdsRemoved = value; }
        }

        /// <summary>
        /// True while an ad is on screen. Real ad SDKs often pause the app while showing an ad;
        /// GameManager uses this to NOT treat that as "player left the game".
        /// </summary>
        public bool IsShowingAd { get; private set; }

        public event Action<string> RewardGranted;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            provider = CreateProvider();
            provider.Initialize();
            GameManager.Instance?.Stats?.HookAds(this); // "watch an ad" challenges count real rewarded views
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>AdMob on Android/iOS phones; the placeholder overlay in the Editor.</summary>
        IAdsProvider CreateProvider()
        {
#if UNITY_EDITOR
            if (fakeAdsInEditor)
            {
                Debug.Log("[Ads] Editor: using the placeholder ad overlay (real AdMob ads only run on a phone).");
                return new FakeAdsProvider(fakeOverlay);
            }
#endif
#if BEATFACTORY_ADMOB && (UNITY_ANDROID || UNITY_IOS)
            string interstitial = Pick(PlatformReal(true), PlatformTest(true));
            string rewarded = Pick(PlatformReal(false), PlatformTest(false));
            Debug.Log("[Ads] AdMob provider. Test mode: " + useTestAdUnits + " | interstitial: " + interstitial + " | rewarded: " + rewarded);
            return new AdMobAdsProvider(this, interstitial, rewarded);
#else
            Debug.Log("[Ads] AdMob is not enabled for this platform — using the placeholder overlay.");
            return new FakeAdsProvider(fakeOverlay);
#endif
        }

        /// <summary>Test unit while Test Mode is on (or when the real unit is still empty).</summary>
        string Pick(string real, string test)
        {
            if (useTestAdUnits) return test;
            if (!string.IsNullOrEmpty(real)) return real;
            // never silent: a release without a real unit earns nothing (the Editor release check blocks such builds)
            Debug.LogError("[Ads] Test Mode is OFF but a real ad unit ID is missing — using Google's TEST unit. Fill it in on the AdsManager.");
            return test;
        }

#if UNITY_IOS
        string PlatformReal(bool interstitial) => interstitial ? iosInterstitialId : iosRewardedId;
        static string PlatformTest(bool interstitial) => interstitial ? TestInterstitialIOS : TestRewardedIOS;
#else
        string PlatformReal(bool interstitial) => interstitial ? androidInterstitialId : androidRewardedId;
        static string PlatformTest(bool interstitial) => interstitial ? TestInterstitialAndroid : TestRewardedAndroid;
#endif

        /// <summary>
        /// True when the player must be able to change their ad consent (EEA/UK, Google UMP). The SETTINGS screen shows
        /// a PRIVACY SETTINGS button then. Always false in the Editor placeholder.
        /// </summary>
        public bool PrivacyOptionsRequired
        {
            get
            {
#if BEATFACTORY_ADMOB && (UNITY_ANDROID || UNITY_IOS)
                return provider is AdMobAdsProvider && AdMobAdsProvider.PrivacyOptionsRequired;
#else
                return false;
#endif
            }
        }

        /// <summary>Opens Google's consent form so the player can change their choice.</summary>
        public void ShowPrivacyOptions()
        {
#if BEATFACTORY_ADMOB && (UNITY_ANDROID || UNITY_IOS)
            if (provider is AdMobAdsProvider) AdMobAdsProvider.ShowPrivacyOptions();
#endif
        }

        /// <summary>Read by the release-build check (Editor): never ship test ads by accident.</summary>
        public bool UsesTestAdUnits => useTestAdUnits;
        public bool HasRealAdUnits(bool ios) => ios
            ? !string.IsNullOrEmpty(iosInterstitialId) && !string.IsNullOrEmpty(iosRewardedId)
            : !string.IsNullOrEmpty(androidInterstitialId) && !string.IsNullOrEmpty(androidRewardedId);

        public bool IsRewardedAvailable => rewardedEnabled && !IsShowingAd && provider != null && provider.IsRewardedReady;

        /// <summary>
        /// Shows a rewarded ad. <paramref name="onResult"/> is called EXACTLY ONCE: true only if the ad was watched to the end
        /// (give the reward), false if it wasn't ready, was closed early or failed. A second tap while an ad is open is ignored.
        /// </summary>
        public void ShowRewarded(string placement, Action<bool> onResult)
        {
            if (IsShowingAd)
            {
                Debug.Log("[Ads] Rewarded '" + placement + "' ignored: an ad is already on screen.");
                return;
            }
            if (!IsRewardedAvailable)
            {
                Debug.Log("[Ads] Rewarded '" + placement + "' not ready yet (still loading).");
                onResult?.Invoke(false);
                return;
            }

            Debug.Log("[Ads] Showing rewarded ad for '" + placement + "'.");
            IsShowingAd = true;
            bool answered = false;
            provider.ShowRewarded(placement, success =>
            {
                if (answered) { Debug.LogWarning("[Ads] Duplicate rewarded callback ignored (no double reward)."); return; }
                answered = true;
                IsShowingAd = false;
                lastAdTime = Time.realtimeSinceStartup;
                Debug.Log(success ? "[Ads] Rewarded ad COMPLETED → reward for '" + placement + "'."
                                  : "[Ads] Rewarded ad closed early / failed → NO reward.");
                if (success) RewardGranted?.Invoke(placement);
                onResult?.Invoke(success);
            });
        }

        /// <summary>Same as TryShowInterstitial, but waits a bit first (e.g. let a celebration play).</summary>
        public void TryShowInterstitialDelayed(string placement, float delaySeconds) =>
            StartCoroutine(ShowDelayed(placement, delaySeconds));

        System.Collections.IEnumerator ShowDelayed(string placement, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            TryShowInterstitial(placement);
        }

        /// <summary>Shows an interstitial only if all frequency rules allow it. Safe to call at any natural break.</summary>
        public bool TryShowInterstitial(string placement, Action onClosed = null)
        {
            float now = Time.realtimeSinceStartup;
            bool allowed = interstitialsEnabled && !AdsRemoved
                           && now >= interstitialStartDelay
                           && now - lastAdTime >= interstitialMinInterval
                           && interstitialsShown < maxInterstitialsPerSession
                           && provider != null && provider.IsInterstitialReady;
            if (!allowed)
            {
                onClosed?.Invoke();
                return false;
            }

            interstitialsShown++;
            lastAdTime = now;
            IsShowingAd = true;
            provider.ShowInterstitial(placement, () =>
            {
                IsShowingAd = false;
                onClosed?.Invoke();
            });
            return true;
        }
    }
}
