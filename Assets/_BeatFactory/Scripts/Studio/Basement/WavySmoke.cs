using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Animates smoke that is painted into a room: the smoke is cut out as its own image and drawn on a small grid mesh whose
    /// columns sway in a travelling wave (anchored at the source, stronger higher up), slowly stretching upward and breathing
    /// in opacity. The painting under it has the smoke removed, so the moving smoke never doubles.
    /// </summary>
    public class WavySmoke : Image
    {
        [Tooltip("Sideways sway at the top, as a share of the image width.")]
        public float sway = 0.12f;
        public float speed = 1.6f, waves = 1.3f;
        [Range(0f, 1f)] public float minAlpha = 0.65f;
        const int Cols = 2, Rows = 16;

        float t;

        void Update()
        {
            if (!Application.isPlaying) return;
            t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            var c = color;
            c.a = Mathf.Lerp(minAlpha, 1f, 0.5f + 0.5f * Mathf.Sin(t * 0.9f));
            color = c; // also refreshes the mesh
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (sprite == null) return;
            var r = GetPixelAdjustedRect();
            var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            Color32 c32 = color;
            float stretch = 1f + 0.04f * Mathf.Sin(t * 0.7f);
            for (int y = 0; y <= Rows; y++)
            {
                float v = y / (float)Rows;                       // 0 at the cigarette, 1 at the top
                float dx = Mathf.Sin(v * waves * Mathf.PI * 2f - t * speed) * sway * r.width * v;
                float py = r.yMin + v * r.height * stretch;
                for (int x = 0; x <= Cols; x++)
                {
                    float u = x / (float)Cols;
                    vh.AddVert(new Vector3(r.xMin + u * r.width + dx, py), c32,
                        new Vector2(Mathf.Lerp(uv.x, uv.z, u), Mathf.Lerp(uv.y, uv.w, v)));
                }
            }
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Cols; x++)
                {
                    int i = y * (Cols + 1) + x;
                    vh.AddTriangle(i, i + Cols + 1, i + Cols + 2);
                    vh.AddTriangle(i, i + Cols + 2, i + 1);
                }
        }
    }
}
