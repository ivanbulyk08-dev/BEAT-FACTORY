// Compiled only when the Google Mobile Ads SDK is installed and the scripting define BEATFACTORY_ADMOB is set
// (Player Settings → Other Settings → Scripting Define Symbols). See ADS_SETUP.md.
#if BEATFACTORY_ADMOB
using System;
using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Google AdMob (Google Mobile Ads Unity plugin). Flow: user consent (UMP, required for EEA/UK) → MobileAds.Initialize →
    /// preload one interstitial + one rewarded ad; after each show (or a failed load) the next one is loaded with a back-off.
    /// All SDK callbacks are moved to the Unity main thread before touching the game.
    /// App ID lives in Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset (Android manifest is generated from it).
    /// </summary>
    public class AdMobAdsProvider : IAdsProvider
    {
        readonly string interstitialId, rewardedId;
        readonly MonoBehaviour runner;
        InterstitialAd interstitial;
        RewardedAd rewarded;
        bool initialized, loadingInterstitial, loadingRewarded;
        int interstitialRetry, rewardedRetry;

        public AdMobAdsProvider(MonoBehaviour runner, string interstitialId, string rewardedId)
        {
            this.runner = runner;
            this.interstitialId = interstitialId;
            this.rewardedId = rewardedId;
        }

        public void Initialize()
        {
            // On Android Unity pauses while a full-screen ad is shown; make iOS behave the same.
            MobileAds.SetiOSAppPauseOnBackground(true);
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            RequestConsent();
            // Returning users who already consented can request ads right away.
            if (ConsentInformation.CanRequestAds()) StartSdk();
            // No internet at launch (or the consent form failed): keep trying in the background,
            // so ads start as soon as the phone is online again — without restarting the game.
            if (runner != null) runner.StartCoroutine(RetryUntilStarted());
        }

        void RequestConsent()
        {
            ConsentInformation.Update(new ConsentRequestParameters(), updateError =>
            {
                if (updateError != null) Debug.LogWarning("[Ads] Consent update failed: " + updateError.Message);
                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null) Debug.LogWarning("[Ads] Consent form: " + formError.Message);
                    if (ConsentInformation.CanRequestAds()) StartSdk();
                });
            });
        }

        IEnumerator RetryUntilStarted()
        {
            while (!initialized)
            {
                yield return new WaitForSecondsRealtime(30f);
                if (initialized) yield break;
                if (Application.internetReachability == NetworkReachability.NotReachable) continue;
                Debug.Log("[Ads] Not started yet — retrying consent / initialization.");
                if (ConsentInformation.CanRequestAds()) StartSdk();
                else RequestConsent();
            }
        }

        void StartSdk()
        {
            if (initialized) return;
            Debug.Log("[Ads] Consent OK → initializing Google Mobile Ads SDK…");
            initialized = true;
            MobileAds.Initialize(status =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    Debug.Log("[Ads] AdMob initialized.");
                    LoadInterstitial();
                    LoadRewarded();
                });
            });
        }

        /// <summary>True when the privacy options button must be shown (EEA users can change consent).</summary>
        public static bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public static void ShowPrivacyOptions() => ConsentForm.ShowPrivacyOptionsForm(e => { if (e != null) Debug.LogWarning("[Ads] Privacy form: " + e.Message); });

        // ------------------------------------------------------------------ interstitial

        public bool IsInterstitialReady => interstitial != null && interstitial.CanShowAd();

        void LoadInterstitial()
        {
            if (!initialized || loadingInterstitial || string.IsNullOrEmpty(interstitialId)) return;
            loadingInterstitial = true;
            interstitial?.Destroy();
            interstitial = null;
            InterstitialAd.Load(interstitialId, new AdRequest(), (ad, error) =>
            {
                loadingInterstitial = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning("[Ads] Interstitial failed to load: " + error);
                    Retry(ref interstitialRetry, LoadInterstitial);
                    return;
                }
                interstitialRetry = 0;
                interstitial = ad;
                Debug.Log("[Ads] Interstitial loaded and ready.");
            });
        }

        public void ShowInterstitial(string placement, Action onClosed)
        {
            if (!IsInterstitialReady) { onClosed?.Invoke(); LoadInterstitial(); return; }
            var ad = interstitial;
            bool done = false;
            void Finish()
            {
                if (done) return;
                done = true;
                onClosed?.Invoke();
                LoadInterstitial();
            }
            ad.OnAdFullScreenContentClosed += () => MobileAdsEventExecutor.ExecuteInUpdate(Finish);
            ad.OnAdFullScreenContentFailed += e => MobileAdsEventExecutor.ExecuteInUpdate(Finish);
            ad.Show();
        }

        // ------------------------------------------------------------------ rewarded

        public bool IsRewardedReady => rewarded != null && rewarded.CanShowAd();

        void LoadRewarded()
        {
            if (!initialized || loadingRewarded || string.IsNullOrEmpty(rewardedId)) return;
            loadingRewarded = true;
            Debug.Log("[Ads] Loading rewarded ad: " + rewardedId);
            rewarded?.Destroy();
            rewarded = null;
            RewardedAd.Load(rewardedId, new AdRequest(), (ad, error) =>
            {
                loadingRewarded = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning("[Ads] Rewarded failed to load: " + error);
                    Retry(ref rewardedRetry, LoadRewarded);
                    return;
                }
                rewardedRetry = 0;
                rewarded = ad;
                Debug.Log("[Ads] Rewarded ad loaded and ready.");
            });
        }

        public void ShowRewarded(string placement, Action<bool> onFinished)
        {
            if (!IsRewardedReady) { onFinished?.Invoke(false); LoadRewarded(); return; }
            var ad = rewarded;
            bool earned = false, done = false;
            void Finish()
            {
                if (done) return;
                done = true;
                onFinished?.Invoke(earned); // reward only if the user watched it to the end
                LoadRewarded();
            }
            // On some Android devices "reward earned" arrives a moment AFTER "closed": wait briefly before deciding.
            ad.OnAdFullScreenContentClosed += () => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("[Ads] Rewarded ad closed.");
                if (runner != null) runner.StartCoroutine(After(0.25f, Finish)); else Finish();
            });
            ad.OnAdFullScreenContentFailed += e => MobileAdsEventExecutor.ExecuteInUpdate(Finish);
            ad.OnAdFullScreenContentFailed += e => Debug.LogWarning("[Ads] Rewarded ad failed to show: " + e);
            ad.Show(reward =>
            {
                earned = true;
                Debug.Log("[Ads] User earned the reward (" + reward.Amount + " " + reward.Type + ").");
            });
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Exponential back-off (2 s, 4 s … max 64 s) so a failing network doesn't spam requests.</summary>
        void Retry(ref int attempt, Action load)
        {
            attempt = Math.Min(attempt + 1, 6);
            float delay = Mathf.Pow(2, attempt);
            if (runner != null) runner.StartCoroutine(After(delay, load));
        }

        static IEnumerator After(float seconds, Action a)
        {
            yield return new WaitForSecondsRealtime(seconds);
            a();
        }
    }
}
#endif
