using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Life for the fans painted into a venue: the painted phone screens glint (soft shimmer + flashes on the beat).
    /// The painting itself is not moved — warping one flat picture only made the whole crowd float, it never looks like
    /// separate people. Individual moving fans need separate transparent fan sprites. Design space 800×1400.
    /// </summary>
    public class PaintedCrowd : MaskableGraphic
    {

        const float W = ConcertStageView.W, H = ConcertStageView.H;

        Texture tex;
        float top;
        float beat, energy, clock;
        readonly List<Image> glints = new List<Image>();

        // front row: separate fan cut-outs right in front of the camera, each moving on its own
        class Fan { public RectTransform rt; public Vector2 pos; public float phase, beatShift, sway, amp; }
        readonly List<Fan> fans = new List<Fan>();
        RectTransform fanLayer;
        Vector2[] phones = new Vector2[0];
        float[] phonePhase = new float[0];

        public override Texture mainTexture => tex != null ? tex : s_WhiteTexture;

        public static PaintedCrowd Create(RectTransform parent, Sprite glow)
        {
            var go = new GameObject("PaintedCrowd", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var c = go.AddComponent<PaintedCrowd>();
            c.raycastTarget = false;
            for (int i = 0; i < 48; i++)
            {
                var g = UIKit.Image("Glint" + i, rt, new Color(1f, 0.85f, 1f, 0f));
                g.sprite = glow;
                g.raycastTarget = false;
                var grt = g.rectTransform;
                grt.anchorMin = grt.anchorMax = Vector2.zero;
                g.gameObject.SetActive(false);
                c.glints.Add(g);
            }
            return c;
        }

        /// <summary>The venue painting, the stage edge (design y) and the painted phone screens (design positions).</summary>
        public void Setup(Texture venueTexture, float stageEdgeY, Vector2[] phoneScreens, int seed)
        {
            tex = venueTexture;
            top = stageEdgeY;
            phones = phoneScreens ?? new Vector2[0];
            var rnd = new System.Random(seed);
            phonePhase = new float[phones.Length];
            for (int i = 0; i < phones.Length; i++) phonePhase[i] = (float)rnd.NextDouble();
            for (int i = 0; i < glints.Count; i++)
            {
                bool on = i < phones.Length;
                glints[i].gameObject.SetActive(on);
                if (!on) continue;
                float near = 1f - Mathf.Clamp01(phones[i].y / Mathf.Max(1f, top)); // nearer fans: bigger phones
                glints[i].rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(26f, 64f, near);
            }
            energy = 0.3f;

        }

        /// <summary>
        /// Puts the fan sprites in a row across the bottom of the picture (cut at the waist below the frame), nearer ones
        /// bigger and darker (backlit by the stage). Each gets its own beat offset, jump height and sway.
        /// </summary>
        /// <summary>Front-row fans and phone glints belong to the main view; hidden while the show cuts to another angle.</summary>
        public void ShowOverlays(bool on)
        {
            if (fanLayer != null) fanLayer.gameObject.SetActive(on);
            for (int i = 0; i < glints.Count; i++) glints[i].enabled = on;
        }

        public void SetupFans(Sprite[] sprites, int seed)
        {
            if (fanLayer == null)
            {
                fanLayer = UIKit.Rect("FrontFans", (RectTransform)transform.parent);
                fanLayer.anchorMin = Vector2.zero;
                fanLayer.anchorMax = Vector2.one;
                fanLayer.offsetMin = fanLayer.offsetMax = Vector2.zero;
            }
            fanLayer.SetSiblingIndex(transform.GetSiblingIndex() + 1);
            foreach (var f in fans) Destroy(f.rt.gameObject);
            fans.Clear();
            if (sprites == null || sprites.Length == 0) return;
            var rnd = new System.Random(seed);
            // order of the picks shuffled so every venue / show gets a different row
            var order = new List<int>();
            for (int i = 0; i < sprites.Length; i++) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--) { int j = rnd.Next(i + 1); (order[i], order[j]) = (order[j], order[i]); }
            float[] xs = { -40f, 150f, 330f, 500f, 680f, 850f };
            var list = new List<(Fan f, float scale, Sprite s)>();
            for (int k = 0; k < xs.Length; k++)
            {
                var s = sprites[order[k % order.Count]];
                if (s == null) continue;
                bool near = k % 2 == 0;
                float scale = (near ? 1.0f : 0.82f) * (0.92f + 0.16f * (float)rnd.NextDouble());
                var f = new Fan
                {
                    pos = new Vector2(xs[k] + ((float)rnd.NextDouble() - 0.5f) * 30f, -170f + (near ? 0f : 90f)),
                    phase = (float)rnd.NextDouble() * 6.28f,
                    beatShift = rnd.Next(3) == 0 ? 0.5f : ((float)rnd.NextDouble() - 0.5f) * 0.2f, // some jump on the off-beat
                    sway = 1.5f + 2.5f * (float)rnd.NextDouble(),
                    amp = near ? 1f : 0.8f,
                };
                list.Add((f, scale, s));
            }
            list.Sort((a, b) => a.scale.CompareTo(b.scale)); // smaller (further) first
            foreach (var (f, scale, s) in list)
            {
                var img = UIKit.Image("Fan", fanLayer, Color.white);
                img.sprite = s;
                img.raycastTarget = false;
                float k = Mathf.InverseLerp(0.75f, 1.08f, scale);
                img.color = Color.Lerp(new Color(0.62f, 0.55f, 0.75f), new Color(0.5f, 0.44f, 0.62f), k); // nearer = darker
                f.rt = img.rectTransform;
                f.rt.anchorMin = f.rt.anchorMax = Vector2.zero;
                f.rt.pivot = new Vector2(0.5f, 0f);
                float wDesign = 420f * scale;
                f.rt.sizeDelta = new Vector2(wDesign, wDesign * s.rect.height / s.rect.width); // scaled to the stage below
                f.rt.localScale = Vector3.one;
                fans.Add(f);
            }
        }

        /// <summary>Called every frame by the stage: beats elapsed (fractional), crowd excitement 0..1, show time.</summary>
        public void Tick(float beats, float excitement, float time)
        {
            beat = beats;
            energy = Mathf.MoveTowards(energy, Mathf.Clamp01(excitement), Time.unscaledDeltaTime * 2f);
            clock = time;
            var parent = (RectTransform)transform;
            float u = parent.rect.width / W;
            foreach (var f in fans)
            {
                float b = Mathf.Repeat(beat + f.beatShift, 1f);
                float jump = Mathf.Pow(Mathf.Sin(b * Mathf.PI), 2f) * Mathf.Lerp(4f, 40f, energy) * f.amp;
                f.rt.anchoredPosition = new Vector2(f.pos.x, f.pos.y + jump) * u;
                f.rt.localScale = new Vector3(u * (1f + 0.02f * jump / 40f), u * (1f - 0.015f * jump / 40f), 1f);
                f.rt.localEulerAngles = new Vector3(0, 0, Mathf.Sin(clock * 1.6f + f.phase) * f.sway * Mathf.Lerp(0.4f, 1f, energy));
            }
            for (int i = 0; i < phones.Length && i < glints.Count; i++)
            {
                var p = phones[i];
                var g = glints[i];
                g.rectTransform.anchoredPosition = p * u;
                // soft shimmer + a flash now and then on the beat
                float ph = phonePhase[i];
                float shimmer = 0.5f + 0.5f * Mathf.Sin((clock * 2.3f + ph * 6.28f));
                float hit = Mathf.Repeat(beat + ph, 4f) < 0.25f ? 1f - Mathf.Repeat(beat + ph, 4f) / 0.25f : 0f;
                var c = g.color;
                c.a = (0.12f + 0.2f * shimmer) * (0.4f + 0.6f * energy) + 0.45f * hit * energy;
                g.color = c;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear(); // the painting stays as painted
    }
}
