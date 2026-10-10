using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// First-time tutorial: a small banner + a pulsing frame around the button to press.
    /// It never blocks the game; each step completes by itself when the player does the action,
    /// and steps that are already done (e.g. old save) are skipped automatically.
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        public const int StepCount = 6;

        [SerializeField] RectTransform banner;
        [SerializeField] TMP_Text messageText;
        [SerializeField] TMP_Text progressText;
        [SerializeField] Button actionButton;
        [SerializeField] TMP_Text actionLabel;
        [SerializeField] Button skipButton;
        [SerializeField] RectTransform highlight;

        [Header("Targets")]
        [SerializeField] RectTransform makeBeatTarget;
        [SerializeField] RectTransform genreTarget;
        [Tooltip("Container of the HOME producer rows (rows are created at runtime).")]
        [SerializeField] RectTransform producerRows;
        [SerializeField] float highlightPadding = 14f;

        GameManager gm;
        int step;
        float checkTimer;
        float pulseTime;
        float bannerPunch;

        string Message(int s)
        {
            string body;
            if (s == 4)
            {
                var g = gm.Genres.Next;
                body = Loc.F("tutorial.step4", "genre", g != null ? g.DisplayName : "Hip-Hop",
                    "mult", NumberFormatter.FormatMultiplier(g != null ? g.incomeMultiplier : 1.5));
            }
            else body = Loc.T("tutorial.step" + s);
            return "<b>" + Loc.T("tutorial.step" + s + "_title") + "</b>\n" + body;
        }

        void Start()
        {
            gm = GameManager.Instance;
            step = gm.TutorialStep;
            actionButton.onClick.AddListener(Advance);
            skipButton.onClick.AddListener(Finish);

            // Returning players: don't greet them like strangers.
            if (step == 0 && gm.Beats.TotalBeats > 0) step = 1;

            SkipCompletedSteps();
            ShowStep();
            Loc.Changed += OnLanguageChanged;
        }

        void OnDestroy() => Loc.Changed -= OnLanguageChanged;

        void OnLanguageChanged()
        {
            if (!IsDone) { messageText.text = Message(step); actionLabel.text = Loc.T(step == 0 ? "tutorial.lets_go" : "tutorial.got_it"); }
        }

        bool IsDone => step >= StepCount;

        Producer FirstProducer => gm.Producers.Producers.Count > 0 ? gm.Producers.Producers[0] : null;

        /// <summary>True when the current step's goal is reached.</summary>
        bool StepComplete()
        {
            var p = FirstProducer;
            switch (step)
            {
                case 1: return p == null || p.IsHired || gm.Economy.Money >= p.NextCost;
                case 2: return p == null || p.IsHired;
                case 3: return p == null || p.Level >= 2;
                case 4: return gm.Genres.UnlockedCount >= 2;
                default: return false; // 0 and 5 wait for a button press
            }
        }

        void SkipCompletedSteps()
        {
            while (step > 0 && step < StepCount - 1 && StepComplete()) step++;
            gm.TutorialStep = step;
        }

        void ShowStep()
        {
            if (IsDone)
            {
                banner.gameObject.SetActive(false);
                highlight.gameObject.SetActive(false);
                enabled = false;
                return;
            }

            banner.gameObject.SetActive(true);
            messageText.text = Message(step);
            bool needsButton = step == 0 || step == StepCount - 1;
            actionButton.gameObject.SetActive(needsButton);
            actionLabel.text = Loc.T(step == 0 ? "tutorial.lets_go" : "tutorial.got_it");
            skipButton.gameObject.SetActive(!needsButton);
            bannerPunch = 0.25f;
            RefreshProgress();
            UpdateHighlight();
        }

        void Advance()
        {
            step++;
            SkipCompletedSteps();
            gm.TutorialStep = step;
            if (!IsDone) AudioManager.Play(SoundId.Upgrade);
            ShowStep();
        }

        /// <summary>Starts the tutorial again from the welcome step (Settings > Replay tutorial).</summary>
        public void Restart()
        {
            if (gm == null) gm = GameManager.Instance;
            step = 0;
            gm.TutorialStep = 0;
            banner.gameObject.SetActive(true);
            enabled = true;
            ShowStep();
        }

        void Finish()
        {
            step = StepCount;
            gm.TutorialStep = step;
            ShowStep();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // Check goals a few times per second (cheap).
            checkTimer += dt;
            if (checkTimer >= 0.2f)
            {
                checkTimer = 0f;
                if (step > 0 && step < StepCount - 1 && StepComplete()) Advance();
                else RefreshProgress();
                if (IsDone) return;
            }

            UpdateHighlight();

            // Banner pop when a new step appears.
            if (bannerPunch > 0f)
            {
                bannerPunch -= dt;
                float t = Mathf.Clamp01(bannerPunch / 0.25f);
                banner.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * Mathf.PI));
            }
        }

        void RefreshProgress()
        {
            var p = FirstProducer;
            string text = "";
            if (p != null && (step == 1 || step == 3))
                text = NumberFormatter.FormatMoney(gm.Economy.Money) + " / " + NumberFormatter.FormatMoney(p.NextCost);
            else if (step == 4 && gm.Genres.Next != null && gm.Genres.MeetsStudioRequirement(gm.Genres.Next))
                text = NumberFormatter.FormatMoney(gm.Economy.Money) + " / " + NumberFormatter.FormatMoney(gm.Genres.Next.unlockCost);
            progressText.text = text;
        }

        RectTransform CurrentTarget()
        {
            switch (step)
            {
                case 1: return makeBeatTarget;
                case 2:
                case 3:
                    if (producerRows == null) return null;
                    var row = producerRows.Find("Row_" + (FirstProducer != null ? FirstProducer.Data.id : ""));
                    return row != null ? row.Find("LevelButton") as RectTransform : null;
                case 4: return genreTarget;
                default: return null;
            }
        }

        static readonly Vector3[] corners = new Vector3[4];

        void UpdateHighlight()
        {
            var target = CurrentTarget();
            bool show = target != null && target.gameObject.activeInHierarchy;
            if (highlight.gameObject.activeSelf != show) highlight.gameObject.SetActive(show);
            if (!show) return;

            // Fit the frame around the target (works no matter where the target is in the hierarchy).
            target.GetWorldCorners(corners);
            var parent = (RectTransform)highlight.parent;
            Vector2 min = parent.InverseTransformPoint(corners[0]);
            Vector2 max = parent.InverseTransformPoint(corners[2]);
            highlight.anchorMin = highlight.anchorMax = new Vector2(0.5f, 0.5f);
            highlight.pivot = new Vector2(0.5f, 0.5f);
            highlight.anchoredPosition = (min + max) * 0.5f - parent.rect.center;
            highlight.sizeDelta = (max - min) + Vector2.one * highlightPadding * 2f;

            pulseTime += Time.unscaledDeltaTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(pulseTime * 5f);
            highlight.localScale = Vector3.one * (1f + 0.04f * pulse);
            var img = highlight.GetComponent<Graphic>();
            var c = img.color;
            c.a = Mathf.Lerp(0.45f, 1f, pulse);
            img.color = c;
        }
    }
}
