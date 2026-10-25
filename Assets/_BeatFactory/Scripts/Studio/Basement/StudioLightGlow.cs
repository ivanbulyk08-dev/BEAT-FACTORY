using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Soft animated light over a painted light source (transparent glow image, no 2D lights needed):
    /// LAMP = warm bulb, slow subtle brightness drift and a very occasional short natural flicker (never off);
    /// NEON = slow "breathing" of an LED strip. Brightness changes are smooth; nothing blinks aggressively.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class StudioLightGlow : MonoBehaviour
    {
        public enum Kind { Lamp, Neon }
        public Kind kind = Kind.Lamp;
        [Range(0f, 1f)] public float baseAlpha = 0.35f;
        [Tooltip("How much the brightness drifts (0.05 = ±5%).")]
        [Range(0f, 0.5f)] public float variation = 0.08f;
        [Tooltip("Seconds per breathing cycle (neon) / drift speed (lamp).")]
        public float period = 5f;
        [Tooltip("Lamp: seconds between flickers (random in this range).")]
        public Vector2 flickerEvery = new Vector2(18f, 45f);

        Graphic g;
        Color c0;
        float t, phase, nextFlicker, flickerT = -1f;
        Vector3 scale0;

        void Awake()
        {
            g = GetComponent<Graphic>();
            c0 = g.color;
            g.raycastTarget = false;
            scale0 = transform.localScale;
            phase = Random.value * 10f;
            nextFlicker = Random.Range(flickerEvery.x, flickerEvery.y);
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            t += dt;
            float k;
            if (kind == Kind.Neon)
            {
                k = 1f + variation * Mathf.Sin((t + phase) * Mathf.PI * 2f / Mathf.Max(0.5f, period));
            }
            else
            {
                // warm drift (two slow noises) + rare flicker: two or three quick dips over ~0.35 s
                k = 1f + variation * (Mathf.PerlinNoise((t + phase) / Mathf.Max(0.5f, period), 0.3f) - 0.5f) * 2f;
                nextFlicker -= dt;
                if (nextFlicker <= 0f) { flickerT = 0f; nextFlicker = Random.Range(flickerEvery.x, flickerEvery.y); }
                if (flickerT >= 0f)
                {
                    flickerT += dt;
                    float f = flickerT / 0.35f;
                    if (f >= 1f) flickerT = -1f;
                    else k *= 1f - 0.22f * Mathf.Abs(Mathf.Sin(f * Mathf.PI * 3f)) * (1f - f);
                }
            }
            var c = c0;
            c.a = Mathf.Clamp01(baseAlpha * k);
            g.color = c;
            transform.localScale = scale0 * (kind == Kind.Lamp ? 0.97f + 0.03f * k : 1f);
        }
    }
}
