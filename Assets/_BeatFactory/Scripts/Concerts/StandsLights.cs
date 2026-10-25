using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Stadium stands: thousands of LED wristbands / phone lights in the seats behind the stage, synced to the music.
    /// One mesh (CrowdLayer) of tiny glow dots over the stands of the stadium art; no per-fan objects.
    /// Intro: a few scattered lights · verse: a slow colour wave · build-up/drop/chorus/big moment: colour waves on the
    /// beat sweeping across the stadium · outro: a calm sea of white phone lights. Pulses are soft (no hard strobing).
    /// </summary>
    public class StandsLights : MonoBehaviour
    {
        CrowdLayer layer;
        Vector2[] dots = new Vector2[0];
        float[] seed = new float[0];
        Color[] palette = { Color.white };
        System.Func<float> clock;
        float bpm = 120, popularity;
        bool on;
        int frame;

        /// <summary>Music section (set by the director).</summary>
        public ShowPart Part { get; set; }

        public static StandsLights Create(RectTransform parent, Texture atlas)
        {
            var rt = UIKit.Rect("StandsLights", parent);
            UIKit.Stretch(rt);
            rt.gameObject.AddComponent<CanvasRenderer>();
            var layer = rt.gameObject.AddComponent<CrowdLayer>();
            layer.texture = atlas;
            layer.raycastTarget = false;
            var s = rt.gameObject.AddComponent<StandsLights>();
            s.layer = layer;
            return s;
        }

        /// <summary>Only the stadium gets the synchronized stands (the other venues have no seats behind the stage).</summary>
        public void Setup(VenueTier tier, float artistPopularity, Color[] colors, int quality, float beatsPerMinute, System.Func<float> showClock)
        {
            on = tier == VenueTier.Stadium;
            gameObject.SetActive(on);
            if (!on) return;
            popularity = artistPopularity;
            palette = colors != null && colors.Length > 0 ? colors : palette;
            bpm = beatsPerMinute;
            clock = showClock;

            // seats of venue_4.png (design space 800×1400, y up): rows under the big screen and the side towers,
            // skipping the speaker stacks in front of them
            float step = quality == 0 ? 11f : quality == 1 ? 8.5f : 7f;
            var rnd = new System.Random(1234);
            var list = new System.Collections.Generic.List<Vector2>();
            for (float y = 646; y < 812; y += step * 0.8f)
                for (float x = 3; x < 797; x += step)
                {
                    bool underScreen = y <= 766;
                    bool sideTower = x < 104 || x > 696;
                    if (!underScreen && !sideTower) continue;
                    if ((x > 116 && x < 190) || (x > 610 && x < 684)) continue; // speaker stacks
                    list.Add(new Vector2(x + (float)rnd.NextDouble() * step * 0.6f, y + (float)rnd.NextDouble() * step * 0.4f));
                }
            dots = list.ToArray();
            seed = new float[dots.Length];
            for (int i = 0; i < seed.Length; i++) seed[i] = (float)rnd.NextDouble();
            Part = ShowPart.Intro;
        }

        void Update()
        {
            if (!on || dots.Length == 0) return;
            // half rate is plenty for distant lights (saves vertex uploads on phones)
            if ((++frame & 1) == 1) return;
            float t = clock != null ? clock() : Time.unscaledTime;
            float beat = t * bpm / 60f;
            float pulse = Mathf.Pow(1f - Mathf.Repeat(beat, 1f), 2f);
            float fame = Mathf.Clamp01(popularity / 100f);
            var uv = CrowdAtlas.UV(CrowdAtlas.GlowCol, CrowdAtlas.PropRow);

            layer.Begin();
            for (int i = 0; i < dots.Length; i++)
            {
                var d = dots[i];
                float r = seed[i];
                Color c;
                float a;
                switch (Part)
                {
                    case ShowPart.Intro:
                        if (r > 0.25f + 0.2f * fame) continue;
                        c = Color.white;
                        a = 0.25f + 0.25f * Mathf.Sin(t * 2f + r * 40f);
                        break;
                    case ShowPart.Verse:
                    {
                        float w = Mathf.Sin(d.x * 0.012f - t * 1.6f);
                        c = Color.Lerp(palette[0], palette[palette.Length > 1 ? 1 : 0], 0.5f + 0.5f * w);
                        a = 0.35f + 0.2f * w;
                        break;
                    }
                    case ShowPart.Outro:
                        if (r > 0.75f) continue;
                        c = new Color(0.9f, 0.95f, 1f);
                        a = 0.35f + 0.25f * Mathf.Sin(t * 3f + r * 50f);
                        break;
                    default:
                    {
                        // colour band per beat sweeping left → right, soft pulse on the kick
                        int band = Mathf.FloorToInt(beat) + Mathf.FloorToInt(d.x / 200f);
                        c = palette[((band % palette.Length) + palette.Length) % palette.Length];
                        float sweep = Mathf.Repeat(beat * 0.5f, 1f) * 900f - 50f;
                        float near = Mathf.Clamp01(1f - Mathf.Abs(d.x - sweep) / 160f);
                        a = 0.35f + 0.3f * pulse + 0.3f * near;
                        break;
                    }
                }
                float size = 5.5f + 2.5f * r;
                c.a = Mathf.Clamp01(a) * Mathf.Lerp(0.6f, 1f, fame);
                layer.Quad(d - new Vector2(size * 0.5f, size * 0.5f), new Vector2(size, size), uv, c);
            }
            layer.Commit();
        }
    }
}
