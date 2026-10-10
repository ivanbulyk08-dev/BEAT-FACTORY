using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Big non-blocking celebration: screen flash + card with header, title and value that pops in,
    /// pulses and fades out. Used for HIT/VIRAL beats, genre unlocks, studio upgrades, etc.
    /// Call it through GameAnnouncements, or directly: popup.Show(...).
    /// </summary>
    public class CelebrationPopup : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform panel;
        [SerializeField] Image panelGlow;
        [SerializeField] TMP_Text headerText;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text valueText;
        [SerializeField] Image screenFlash;

        [Header("Timing")]
        [SerializeField] float popInDuration = 0.3f;
        [SerializeField] float holdDuration = 1.8f;
        [SerializeField] float fadeOutDuration = 0.4f;
        [SerializeField] float flashDuration = 0.5f;

        float time;
        float flashAlpha;
        Color flashColor;
        int currentPriority;
        bool showing;

        void Awake() => HideImmediate();

        /// <summary>
        /// Shows the popup. A popup with lower priority than the one on screen is ignored.
        /// </summary>
        public void Show(string header, string title, string value, Color color, float flashStrength, int priority)
        {
            if (showing && priority < currentPriority) return;
            currentPriority = priority;

            headerText.text = header;
            headerText.color = color;
            titleText.text = title ?? "";
            valueText.text = value ?? "";
            panelGlow.color = new Color(color.r, color.g, color.b, 0.35f);

            flashColor = color;
            flashAlpha = flashStrength;

            time = 0f;
            showing = true;
            group.gameObject.SetActive(true);
            screenFlash.gameObject.SetActive(flashStrength > 0f);
            enabled = true;
            Apply();
        }

        void Update()
        {
            time += Time.unscaledDeltaTime;
            Apply();
        }

        void Apply()
        {
            // Screen flash fades out quickly.
            if (screenFlash.gameObject.activeSelf)
            {
                float f = Mathf.Clamp01(time / flashDuration);
                var c = flashColor;
                c.a = flashAlpha * (1f - f) * (1f - f);
                screenFlash.color = c;
                if (f >= 1f) screenFlash.gameObject.SetActive(false);
            }

            // Card: pop in with overshoot, gentle pulse while holding, fade out.
            float scale, alpha;
            if (time < popInDuration)
            {
                float p = time / popInDuration;
                scale = EaseOutBack(p);
                alpha = Mathf.Clamp01(p * 2f);
            }
            else if (time < popInDuration + holdDuration)
            {
                scale = 1f + Mathf.Sin((time - popInDuration) * 8f) * 0.025f;
                alpha = 1f;
            }
            else
            {
                float p = Mathf.Clamp01((time - popInDuration - holdDuration) / fadeOutDuration);
                scale = 1f + p * 0.1f;
                alpha = 1f - p;
            }

            panel.localScale = new Vector3(scale, scale, 1f);
            group.alpha = alpha;

            if (time >= popInDuration + holdDuration + fadeOutDuration) HideImmediate();
        }

        void HideImmediate()
        {
            showing = false;
            currentPriority = 0;
            group.alpha = 0f;
            group.gameObject.SetActive(false);
            screenFlash.gameObject.SetActive(false);
            enabled = false;
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
        }
    }
}
