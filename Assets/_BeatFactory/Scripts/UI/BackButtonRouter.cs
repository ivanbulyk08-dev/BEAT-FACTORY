using UnityEngine;
using UnityEngine.InputSystem;

namespace BeatFactory
{
    /// <summary>
    /// Android system Back button (Escape on a keyboard). Closes whatever is on top, in this order:
    /// ads/concerts/tour results are left alone (they need the player's own tap) → language picker → the last opened
    /// popup → full-screen pages (Goals, artist career) → back to HOME → on HOME the app goes to the background
    /// (like every Android app), never quits and never loses progress.
    /// </summary>
    public class BackButtonRouter : MonoBehaviour
    {
        [SerializeField] ScreenNavigator navigator;
        [SerializeField] FakeAdOverlay fakeAd;

        float lastPress;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (Time.unscaledTime - lastPress < 0.2f) return; // one press = one step
            lastPress = Time.unscaledTime;
            HandleBack();
        }

        public void HandleBack()
        {
            // things that must finish by themselves
            if (fakeAd != null && fakeAd.IsShowing) return;
            if (AdsManager.Instance != null && AdsManager.Instance.IsShowingAd) return;
            if (ConcertShowUI.Instance != null && ConcertShowUI.Instance.IsPlaying) return;
            if (TourCompletePopup.Instance != null && TourCompletePopup.Instance.IsOpen) return;

            if (LanguagePicker.Instance != null && LanguagePicker.Instance.IsOpen) { LanguagePicker.Instance.Hide(); Click(); return; }

            var stack = ModalWindow.OpenStack;
            for (int i = stack.Count - 1; i >= 0; i--)
                if (stack[i] != null && stack[i].IsOpen) { stack[i].Back(); Click(); return; }

            if (GoalsScreen.Instance != null && GoalsScreen.Instance.IsOpen) { GoalsScreen.Instance.Hide(); Click(); return; }
            if (ArtistCareerScreen.Instance != null && ArtistCareerScreen.Instance.IsOpen) { ArtistCareerScreen.Instance.Hide(); Click(); return; }

            if (navigator != null && navigator.Current > 0) { navigator.Show(0); return; }

            MoveToBackground();
        }

        static void Click() => AudioManager.Play(SoundId.Click);

        /// <summary>Android: put the game in the background (the activity keeps running, progress is saved on pause).</summary>
        static void MoveToBackground()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    activity.Call<bool>("moveTaskToBack", true);
            }
            catch (System.Exception e) { Debug.LogWarning("[Back] " + e.Message); }
#endif
        }
    }
}
