using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// The studio card: studio name, level pips and card color.
    /// Plays a short transition (bounce + color fade) when the studio is upgraded.
    /// </summary>
    public class StudioCardUI : MonoBehaviour
    {
        [SerializeField] Image cardImage;
        [SerializeField] TMP_Text studioNameText;
        [SerializeField] Image[] levelPips;
        [SerializeField] Color pipOffColor = new Color(1f, 1f, 1f, 0.12f);
        [SerializeField] Color pipOnColor = new Color(0.24f, 0.88f, 1f);
        [SerializeField] EqualizerVisualizer equalizer;

        [Header("Upgrade transition")]
        [SerializeField] float transitionDuration = 0.6f;
        [SerializeField] float bounceScale = 1.05f;

        StudioManager studios;
        Color fromColor, toColor;
        float time;

        void Start()
        {
            studios = GameManager.Instance.Studios;
            studios.CurrentChanged += OnStudioChanged;

            Apply(studios.Current);
            if (studios.Current != null) cardImage.color = studios.Current.panelColor;
            Loc.Changed += OnLanguageChanged;
            enabled = false;
        }

        void OnDestroy()
        {
            if (studios != null) studios.CurrentChanged -= OnStudioChanged;
            Loc.Changed -= OnLanguageChanged;
        }

        void OnLanguageChanged() => Apply(studios.Current);

        void OnStudioChanged(StudioData studio)
        {
            Apply(studio);
            if (studio == null) return;
            fromColor = cardImage.color;
            toColor = studio.panelColor;
            time = 0f;
            enabled = true;
        }

        void Apply(StudioData studio)
        {
            if (studio == null) return;
            studioNameText.text = Loc.Upper(studio.DisplayName);

            for (int i = 0; i < levelPips.Length; i++)
            {
                levelPips[i].gameObject.SetActive(i < studios.Count);
                levelPips[i].color = i <= studios.Level ? pipOnColor : pipOffColor;
            }

            if (equalizer != null) equalizer.SetBarCount(studio.equalizerBars);
        }

        void Update()
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / transitionDuration);
            cardImage.color = Color.Lerp(fromColor, toColor, t);

            // Bounce: up quickly, settle back.
            float bounce = Mathf.Sin(t * Mathf.PI) * (bounceScale - 1f);
            transform.localScale = Vector3.one * (1f + bounce);

            if (t >= 1f)
            {
                transform.localScale = Vector3.one;
                enabled = false;
            }
        }
    }
}
