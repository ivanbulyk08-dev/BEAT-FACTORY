using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// One mesh, one draw call: thousands of tinted quads from the crowd atlas, rebuilt from arrays each frame
    /// (no per-fan GameObjects). Positions are in venue design units (800×1400, origin bottom-left).
    /// </summary>
    public class CrowdLayer : MaskableGraphic
    {
        public Texture texture;
        public Vector2 designSize = new Vector2(800, 1400);

        Mesh mesh;
        readonly List<Vector3> verts = new List<Vector3>(16384);
        readonly List<Color32> colors = new List<Color32>(16384);
        readonly List<Vector2> uvs = new List<Vector2>(16384);
        readonly List<int> tris = new List<int>(24576);
        float kx, ky, ox, oy;

        public override Texture mainTexture => texture != null ? texture : s_WhiteTexture;
        public int QuadCount => verts.Count / 4;

        /// <summary>Starts a new frame of quads (call Commit when done).</summary>
        public void Begin()
        {
            verts.Clear(); colors.Clear(); uvs.Clear(); tris.Clear();
            var r = rectTransform.rect;
            kx = r.width / designSize.x;
            ky = r.height / designSize.y;
            ox = r.xMin;
            oy = r.yMin;
        }

        Vector3 L(Vector2 p) => new Vector3(ox + p.x * kx, oy + p.y * ky, 0);

        /// <summary>Axis-aligned quad: bottom-left + size (design units). flip = mirror the texture horizontally.</summary>
        public void Quad(Vector2 bl, Vector2 size, Rect uv, Color32 c, bool flip = false)
        {
            int i = verts.Count;
            if (i >= 65000) return;
            verts.Add(L(bl));
            verts.Add(L(new Vector2(bl.x, bl.y + size.y)));
            verts.Add(L(bl + size));
            verts.Add(L(new Vector2(bl.x + size.x, bl.y)));
            float u0 = flip ? uv.xMax : uv.xMin, u1 = flip ? uv.xMin : uv.xMax;
            uvs.Add(new Vector2(u0, uv.yMin));
            uvs.Add(new Vector2(u0, uv.yMax));
            uvs.Add(new Vector2(u1, uv.yMax));
            uvs.Add(new Vector2(u1, uv.yMin));
            colors.Add(c); colors.Add(c); colors.Add(c); colors.Add(c);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i + 2); tris.Add(i + 3); tris.Add(i);
        }

        /// <summary>Rotated quad hanging from a pivot point at its top center (arms): direction = unit vector toward the hand.</summary>
        public void Limb(Vector2 pivot, Vector2 dir, float width, float length, Rect uv, Color32 c)
        {
            var side = new Vector2(-dir.y, dir.x) * (width * 0.5f);
            var end = pivot + dir * length;
            int i = verts.Count;
            if (i >= 65000) return;
            // cell orientation: top of the cell = pivot (shoulder), bottom = hand
            verts.Add(L(end - side));
            verts.Add(L(pivot - side));
            verts.Add(L(pivot + side));
            verts.Add(L(end + side));
            uvs.Add(new Vector2(uv.xMin, uv.yMin));
            uvs.Add(new Vector2(uv.xMin, uv.yMax));
            uvs.Add(new Vector2(uv.xMax, uv.yMax));
            uvs.Add(new Vector2(uv.xMax, uv.yMin));
            colors.Add(c); colors.Add(c); colors.Add(c); colors.Add(c);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i + 2); tris.Add(i + 3); tris.Add(i);
        }

        public void Commit() => SetVerticesDirty();

        protected override void UpdateGeometry()
        {
            if (mesh == null)
            {
                mesh = new Mesh { name = "Crowd" };
                mesh.MarkDynamic(); // 16-bit indices: the UI CanvasRenderer only reads 16-bit (each layer stays < 65k vertices)
            }
            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            canvasRenderer.SetMesh(mesh);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (mesh != null) Destroy(mesh);
        }
    }
}
