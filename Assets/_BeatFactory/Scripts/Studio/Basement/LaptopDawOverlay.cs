using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// A live DAW on the laptop painted into the studio artwork: toolbar with blinking indicators, track headers, scrolling
    /// purple/magenta clips, a playback cursor, an animated waveform and two level meters.
    /// Everything is drawn as a small procedural mesh in the screen's own (u, v) space and mapped onto the laptop's four
    /// painted corners (bilinear), so it follows the screen's perspective and never covers the frame or keyboard.
    /// MAKE BEAT briefly raises the activity (bigger waveform, jumpier meters, a soft screen glow) which then eases back.
    /// About 150 quads, no textures, no allocations per frame.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class LaptopDawOverlay : MaskableGraphic
    {
        [Tooltip("Screen corners as fractions of THIS rect (0..1, bottom-left origin): top-left, top-right, bottom-right, bottom-left.")]
        public Vector2 topLeft = new Vector2(0.556f, 0.553f), topRight = new Vector2(0.693f, 0.552f),
            bottomRight = new Vector2(0.677f, 0.498f), bottomLeft = new Vector2(0.542f, 0.503f);
        [Range(0f, 1f)] public float opacity = 0.92f;
        public Color background = new Color(0.07f, 0.06f, 0.14f);
        public Color panel = new Color(0.13f, 0.12f, 0.22f);
        public Color purple = new Color(0.62f, 0.32f, 1f);
        public Color magenta = new Color(1f, 0.28f, 0.82f);
        public Color cyan = new Color(0.3f, 0.85f, 1f);

        float time, activity = 1f, glow;
        Rect r;

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying) return;
            raycastTarget = false;
            var gm = GameManager.Instance;
            if (gm != null) gm.Beats.BeatsCreated += OnBeat;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Application.isPlaying && GameManager.Instance != null) GameManager.Instance.Beats.BeatsCreated -= OnBeat;
        }

        void OnBeat(BeatResult b)
        {
            if (b.Source != BeatSource.Manual) return;
            activity = Mathf.Min(2.4f, activity + 0.9f);
            glow = 1f;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            time += dt;
            activity = Mathf.MoveTowards(activity, 1f, dt * 0.8f);   // eases back to idle in ~1.5 s
            glow = Mathf.MoveTowards(glow, 0f, dt * 2.5f);
            SetVerticesDirty();
        }

        // ------------------------------------------------------------------ mesh

        Vector2 Map(float u, float v)
        {
            // u: left → right, v: top → bottom of the laptop screen
            var top = Vector2.Lerp(topLeft, topRight, u);
            var bottom = Vector2.Lerp(bottomLeft, bottomRight, u);
            var n = Vector2.Lerp(top, bottom, v);
            return new Vector2(r.xMin + n.x * r.width, r.yMin + n.y * r.height);
        }

        void Quad(VertexHelper vh, float u0, float v0, float u1, float v1, Color c)
        {
            u0 = Mathf.Clamp01(u0); u1 = Mathf.Clamp01(u1); v0 = Mathf.Clamp01(v0); v1 = Mathf.Clamp01(v1);
            if (u1 <= u0 || v1 <= v0) return;
            c.a *= opacity * color.a;
            Color32 c32 = c;
            int i = vh.currentVertCount;
            vh.AddVert(Map(u0, v0), c32, Vector2.zero);
            vh.AddVert(Map(u1, v0), c32, Vector2.zero);
            vh.AddVert(Map(u1, v1), c32, Vector2.zero);
            vh.AddVert(Map(u0, v1), c32, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        static float Hash(float x) => Mathf.Repeat(Mathf.Sin(x * 12.9898f) * 43758.5453f, 1f);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            r = rectTransform.rect;
            float t = time, a = activity;

            Quad(vh, 0, 0, 1, 1, background);
            // toolbar + transport indicators
            Quad(vh, 0, 0, 1, 0.075f, panel);
            for (int i = 0; i < 5; i++)
            {
                bool on = Mathf.Repeat(t * (0.6f + i * 0.17f) + i * 0.31f, 1f) < 0.55f;
                Quad(vh, 0.02f + i * 0.035f, 0.02f, 0.042f + i * 0.035f, 0.055f, on ? (i == 0 ? magenta : cyan) : panel * 1.4f);
            }
            Quad(vh, 0.42f, 0.018f, 0.6f, 0.057f, panel * 1.5f);                 // BPM / time display
            Quad(vh, 0.43f, 0.03f, 0.43f + 0.16f * Mathf.Repeat(t * 0.25f, 1f), 0.045f, purple * 0.9f);

            // track headers
            const int tracks = 6;
            float top = 0.1f, bottom = 0.7f, rowH = (bottom - top) / tracks;
            Quad(vh, 0, top, 0.17f, bottom, panel);
            for (int k = 0; k < tracks; k++)
            {
                float v0 = top + k * rowH;
                Quad(vh, 0.015f, v0 + rowH * 0.2f, 0.12f, v0 + rowH * 0.45f, panel * 1.6f);
                bool mute = Mathf.Repeat(t * 0.2f + k * 0.37f, 1f) < 0.08f;
                Quad(vh, 0.135f, v0 + rowH * 0.2f, 0.155f, v0 + rowH * 0.55f, mute ? magenta : panel * 1.8f);
                Quad(vh, 0.17f, v0 + rowH - 0.004f, 1f, v0 + rowH, panel * 1.3f);      // row line
            }
            // bar grid
            for (int g = 0; g < 8; g++)
            {
                float u = 0.17f + Mathf.Repeat(g / 8f - t * 0.04f, 1f) * 0.83f;
                Quad(vh, u, top, u + 0.003f, bottom, panel * 1.25f);
            }
            // scrolling clips (purple / magenta), the arrangement loops every 25 s
            for (int k = 0; k < tracks; k++)
            {
                float v0 = top + k * rowH + rowH * 0.18f, v1 = top + (k + 1) * rowH - rowH * 0.18f;
                var c = k % 3 == 1 ? magenta : k % 3 == 2 ? cyan * 0.85f + purple * 0.15f : purple;
                for (int n = 0; n < 4; n++)
                {
                    float start = Hash(k * 7 + n * 13) * 0.9f + n * 0.25f;
                    float len = 0.08f + Hash(k * 3 + n * 5 + 1) * 0.16f;
                    float u0 = 0.17f + (Mathf.Repeat(start - t * 0.04f, 1.2f) - 0.1f) * 0.83f;
                    Quad(vh, Mathf.Max(0.17f, u0), v0, Mathf.Min(1f, u0 + len * 0.83f), v1, c * 0.75f);
                    // little "notes" inside the clip
                    for (int m = 0; m < 3; m++)
                    {
                        float nu = u0 + (0.15f + m * 0.25f) * len * 0.83f;
                        if (nu > 0.17f && nu < 1f) Quad(vh, nu, v0 + (v1 - v0) * (0.2f + 0.2f * Hash(k + n + m)), nu + 0.012f, v0 + (v1 - v0) * (0.45f + 0.2f * Hash(k + n + m)), c * 1.15f);
                    }
                }
            }
            // playback cursor
            float cu = 0.17f + Mathf.Repeat(t * 0.09f, 1f) * 0.83f;
            Quad(vh, cu, top - 0.02f, cu + 0.005f, bottom, new Color(1f, 1f, 1f, 0.85f));

            // waveform (master)
            Quad(vh, 0.0f, 0.73f, 1f, 0.98f, panel * 0.9f);
            const int bars = 44;
            for (int b = 0; b < bars; b++)
            {
                float u0 = 0.02f + b * (0.88f / bars);
                float n = Mathf.PerlinNoise(b * 0.35f - t * 2.2f, t * 0.7f);
                float beat = Mathf.Pow(1f - Mathf.Repeat(t * 2f + b * 0.01f, 1f), 3f);
                float h = Mathf.Clamp01((0.15f + 0.55f * n + 0.25f * beat) * (0.6f + 0.4f * a));
                float mid = 0.855f;
                var col = Color.Lerp(purple, magenta, n);
                Quad(vh, u0, mid - 0.1f * h, u0 + 0.013f, mid + 0.1f * h, col);
            }
            // level meters
            for (int ch = 0; ch < 2; ch++)
            {
                float level = Mathf.Clamp01((0.35f + 0.45f * Mathf.PerlinNoise(t * (4f + ch), ch * 3.1f)) * (0.65f + 0.35f * a));
                float u0 = 0.925f + ch * 0.03f;
                Quad(vh, u0, 0.74f, u0 + 0.02f, 0.97f, panel * 1.5f);
                float topV = 0.97f - 0.23f * level;
                Quad(vh, u0, Mathf.Max(topV, 0.83f), u0 + 0.02f, 0.97f, cyan * 0.9f);
                if (topV < 0.83f) Quad(vh, u0, Mathf.Max(topV, 0.77f), u0 + 0.02f, 0.83f, purple);
                if (topV < 0.77f) Quad(vh, u0, topV, u0 + 0.02f, 0.77f, magenta);
            }
            // MAKE BEAT: a soft glow pulse over the whole screen
            if (glow > 0.01f) Quad(vh, 0, 0, 1, 1, new Color(magenta.r, magenta.g, magenta.b, 0.18f * glow));
        }
    }
}
