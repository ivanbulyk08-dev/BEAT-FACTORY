using System;

namespace BeatFactory
{
    /// <summary>
    /// What the game needs from an ad network. To connect a real SDK (Unity Ads, AdMob, AppLovin ...):
    /// 1. Install the SDK package.
    /// 2. Create a class that implements this interface by calling the SDK.
    /// 3. Return it from AdsManager.CreateProvider().
    /// Nothing else in the game has to change.
    /// Test implementation: FakeAdsProvider.
    /// </summary>
    public interface IAdsProvider
    {
        void Initialize();

        bool IsRewardedReady { get; }

        /// <summary>Shows a rewarded ad. Callback: true = watched to the end (give the reward), false = skipped/failed.</summary>
        void ShowRewarded(string placement, Action<bool> onFinished);

        bool IsInterstitialReady { get; }

        /// <summary>Shows a full-screen ad between game moments. Callback when it closes.</summary>
        void ShowInterstitial(string placement, Action onClosed);
    }
}
