using TMPro;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>Shows how many beats were made in total, with a small bounce on change.</summary>
    public class BeatCounterUI : MonoBehaviour
    {
        [SerializeField] TMP_Text countText;
        [SerializeField] TMP_Text labelText;
        [SerializeField] float punchScale = 1.15f;
        [SerializeField] float punchDuration = 0.12f;

        BeatManager beats;
        float punchTimer;

        void Start()
        {
            beats = GameManager.Instance.Beats;
            beats.BeatCountChanged += OnCountChanged;
            Refresh(beats.TotalBeats);
            Loc.Changed += OnLanguageChanged;
            enabled = false;
        }

        void OnDestroy()
        {
            if (beats != null) beats.BeatCountChanged -= OnCountChanged;
            Loc.Changed -= OnLanguageChanged;
        }

        void OnLanguageChanged() => Refresh(beats.TotalBeats);

        void OnCountChanged(long total)
        {
            Refresh(total);
            punchTimer = punchDuration;
            enabled = true;
        }

        void Refresh(long total)
        {
            countText.text = NumberFormatter.Format(total);
            if (labelText != null) labelText.text = Loc.P("hud.beats_made", total);
        }

        void Update()
        {
            punchTimer -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(punchTimer / punchDuration);
            countText.transform.localScale = Vector3.one * Mathf.Lerp(1f, punchScale, t);
            if (punchTimer <= 0f) enabled = false;
        }
    }
}
