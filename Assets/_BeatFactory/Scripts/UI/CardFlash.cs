using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>Brief white glow over a card (e.g. after an upgrade). Call Flash().</summary>
    [RequireComponent(typeof(Graphic))]
    public class CardFlash : MonoBehaviour
    {
        [SerializeField] float duration = 0.55f;
        [SerializeField] float peakAlpha = 0.28f;

        Graphic graphic;
        float t = 1f;

        void Awake()
        {
            graphic = GetComponent<Graphic>();
            SetAlpha(0f);
            enabled = false;
        }

        public void Flash()
        {
            t = 0f;
            enabled = true;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime / duration;
            SetAlpha(peakAlpha * (1f - Mathf.Clamp01(t)) * Mathf.Clamp01(t * 6f));
            if (t >= 1f) enabled = false;
        }

        void SetAlpha(float a)
        {
            if (graphic == null) graphic = GetComponent<Graphic>();
            var c = graphic.color;
            c.a = a;
            graphic.color = c;
        }
    }
}

