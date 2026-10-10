using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Full-screen placeholder "ad" for testing. Counts down, then CLOSE gives the reward.
    /// SKIP closes immediately without reward. The game is paused while it's open, like a real ad.
    /// </summary>
    public class FakeAdOverlay : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text placementText;
        [SerializeField] TMP_Text countdownText;
        [SerializeField] Button closeButton;
        [SerializeField] Button skipButton;
        [SerializeField] float duration = 3f;

        Action<bool> callback;
        bool rewarded;
        float remaining;
        float savedTimeScale = 1f;

        public bool IsShowing { get; private set; }

        void Awake()
        {
            closeButton.onClick.AddListener(() => Finish(true));
            skipButton.onClick.AddListener(() => Finish(false));
            root.SetActive(false);
            enabled = false;
        }

        public void Show(string title, string placement, bool rewarded, Action<bool> onFinished)
        {
            this.rewarded = rewarded;
            callback = onFinished;
            remaining = duration;
            IsShowing = true;

            titleText.text = Loc.F("ads.test_title", "type", title);
            placementText.text = Loc.F("ads.test_placement", "placement", placement);
            closeButton.gameObject.SetActive(false);
            skipButton.gameObject.SetActive(rewarded);

            // Like a real ad: pause the game and its sound.
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            AudioListener.pause = true;

            root.SetActive(true);
            root.transform.SetAsLastSibling();
            enabled = true;
            UpdateCountdown();
        }

        void Update()
        {
            remaining -= Time.unscaledDeltaTime;
            UpdateCountdown();
            if (remaining <= 0f && !closeButton.gameObject.activeSelf)
                closeButton.gameObject.SetActive(true);
        }

        void UpdateCountdown()
        {
            countdownText.text = remaining > 0 ? Mathf.CeilToInt(remaining).ToString() : (rewarded ? Loc.T("ads.reward_earned") : "");
        }

        void Finish(bool watchedToEnd)
        {
            if (!IsShowing) return;
            IsShowing = false;
            enabled = false;
            root.SetActive(false);
            Time.timeScale = savedTimeScale;
            AudioListener.pause = false;

            var cb = callback;
            callback = null;
            cb?.Invoke(rewarded && watchedToEnd && remaining <= 0f || (!rewarded && watchedToEnd));
        }
    }
}
