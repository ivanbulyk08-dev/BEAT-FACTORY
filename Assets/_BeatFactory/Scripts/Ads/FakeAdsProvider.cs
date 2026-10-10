using System;

namespace BeatFactory
{
    /// <summary>
    /// Test ads: shows a placeholder overlay with a countdown instead of a real ad.
    /// Used in the Editor and until a real ad SDK is connected.
    /// </summary>
    public class FakeAdsProvider : IAdsProvider
    {
        readonly FakeAdOverlay overlay;

        public FakeAdsProvider(FakeAdOverlay overlay)
        {
            this.overlay = overlay;
        }

        public void Initialize() { }

        public bool IsRewardedReady => overlay != null && !overlay.IsShowing;
        public bool IsInterstitialReady => overlay != null && !overlay.IsShowing;

        public void ShowRewarded(string placement, Action<bool> onFinished)
        {
            if (!IsRewardedReady) { onFinished?.Invoke(false); return; }
            overlay.Show(Loc.T("ads.rewarded_ad"), placement, rewarded: true, onFinished);
        }

        public void ShowInterstitial(string placement, Action onClosed)
        {
            if (!IsInterstitialReady) { onClosed?.Invoke(); return; }
            overlay.Show(Loc.T("ads.interstitial_ad"), placement, rewarded: false, _ => onClosed?.Invoke());
        }
    }
}
