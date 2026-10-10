using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// Tiny vector-style painter: shapes are signed distance functions (SDF), drawn with anti-aliasing
    /// onto a texture. Coordinates are in pixels, origin bottom-left, y up.
    /// Used to generate all placeholder art (props, characters, icons, effects) as PNG files.
    /// </summary>
    public class SdfPainter
    {
        public readonly int W, H;
        readonly Color[] px;

        public SdfPainter(int w, int h)
        {
            W = w;
            H = h;
            px = new Color[w * h];
        }

        // ------------------------------------------------------------------ drawing

        /// <summary>Fills a shape. <paramref name="bounds"/> limits the work area (x, y, w, h); null = whole canvas.</summary>
        public void Fill(Func<Vector2, float> sdf, Color color, RectInt? bounds = null) =>
            Fill(sdf, _ => color, bounds);

        public void Fill(Func<Vector2, float> sdf, Func<Vector2, Color> color, RectInt? bounds = null)
        {
            var b = bounds ?? new RectInt(0, 0, W, H);
            int x0 = Mathf.Max(0, b.xMin), x1 = Mathf.Min(W, b.xMax);
            int y0 = Mathf.Max(0, b.yMin), y1 = Mathf.Min(H, b.yMax);
            for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d = sdf(p);
                if (d > 1f) continue;
                float cov = Mathf.Clamp01(0.5f - d);
                if (cov <= 0f) continue;
                Blend(x, y, color(p), cov);
            }
        }

        /// <summary>Soft glow: alpha falls off from the shape edge over <paramref name="radius"/> pixels.</summary>
        public void Glow(Func<Vector2, float> sdf, Color color, float radius)
        {
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float d = sdf(new Vector2(x + 0.5f, y + 0.5f));
                if (d >= radius) continue;
                float a = d <= 0 ? 1f : 1f - d / radius;
                Blend(x, y, color, a * a);
            }
        }

        /// <summary>Vertical gradient over the whole canvas (background).</summary>
        public void VerticalGradient(Color bottom, Color top)
        {
            for (int y = 0; y < H; y++)
            {
                var c = Color.Lerp(bottom, top, y / (float)(H - 1));
                for (int x = 0; x < W; x++) px[y * W + x] = c;
            }
        }

        void Blend(int x, int y, Color src, float coverage)
        {
            int i = y * W + x;
            float a = src.a * coverage;
            var dst = px[i];
            float outA = a + dst.a * (1f - a);
            if (outA <= 0f) return;
            px[i] = new Color(
                (src.r * a + dst.r * dst.a * (1f - a)) / outA,
                (src.g * a + dst.g * dst.a * (1f - a)) / outA,
                (src.b * a + dst.b * dst.a * (1f - a)) / outA,
                outA);
        }

        /// <summary>Copies another canvas into this one at (dx, dy) — no blending.</summary>
        public void Blit(SdfPainter src, int dx, int dy)
        {
            for (int y = 0; y < src.H; y++)
            {
                int ty = y + dy;
                if (ty < 0 || ty >= H) continue;
                for (int x = 0; x < src.W; x++)
                {
                    int tx = x + dx;
                    if (tx >= 0 && tx < W) px[ty * W + tx] = src.px[y * src.W + x];
                }
            }
        }

        public Color GetPixel(int x, int y) => px[Mathf.Clamp(y, 0, H - 1) * W + Mathf.Clamp(x, 0, W - 1)];
        public void SetPixel(int x, int y, Color c) => px[y * W + x] = c;

        // ------------------------------------------------------------------ saving

        /// <summary>Writes the PNG and imports it as a UI sprite.</summary>
        public void Save(string assetPath, Vector4 border = default)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------ shapes (all return signed distance in pixels)

        public static Func<Vector2, float> Circle(Vector2 c, float r) => p => (p - c).magnitude - r;

        public static Func<Vector2, float> Ellipse(Vector2 c, Vector2 radii) => p =>
        {
            // good-enough ellipse distance (scaled circle)
            var q = new Vector2((p.x - c.x) / radii.x, (p.y - c.y) / radii.y);
            float k = q.magnitude;
            return (k - 1f) * Mathf.Min(radii.x, radii.y);
        };

        /// <summary>Rounded box centered at c, half-size h, corner radius r, rotated by angle (degrees).</summary>
        public static Func<Vector2, float> Box(Vector2 c, Vector2 h, float r = 0f, float angle = 0f)
        {
            float s = Mathf.Sin(-angle * Mathf.Deg2Rad), co = Mathf.Cos(-angle * Mathf.Deg2Rad);
            return p =>
            {
                var d0 = p - c;
                var q = new Vector2(d0.x * co - d0.y * s, d0.x * s + d0.y * co);
                var d = new Vector2(Mathf.Abs(q.x) - h.x + r, Mathf.Abs(q.y) - h.y + r);
                return new Vector2(Mathf.Max(d.x, 0), Mathf.Max(d.y, 0)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0) - r;
            };
        }

        /// <summary>Box from bottom-left corner + size (easier for layouts).</summary>
        public static Func<Vector2, float> RRect(float x, float y, float w, float h, float r = 0f) =>
            Box(new Vector2(x + w * 0.5f, y + h * 0.5f), new Vector2(w * 0.5f, h * 0.5f), r);

        public static Func<Vector2, float> Capsule(Vector2 a, Vector2 b, float r) => p =>
        {
            var pa = p - a;
            var ba = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * t).magnitude - r;
        };

        public static Func<Vector2, float> Ring(Vector2 c, float r, float thickness) => p =>
            Mathf.Abs((p - c).magnitude - r) - thickness * 0.5f;

        /// <summary>Part of a ring between two angles (degrees, counter-clockwise from +x).</summary>
        public static Func<Vector2, float> Arc(Vector2 c, float r, float thickness, float fromDeg, float toDeg)
        {
            var a = c + new Vector2(Mathf.Cos(fromDeg * Mathf.Deg2Rad), Mathf.Sin(fromDeg * Mathf.Deg2Rad)) * r;
            var b = c + new Vector2(Mathf.Cos(toDeg * Mathf.Deg2Rad), Mathf.Sin(toDeg * Mathf.Deg2Rad)) * r;
            return p =>
            {
                var d = p - c;
                float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                float rel = Mathf.Repeat(ang - fromDeg, 360f);
                float span = Mathf.Repeat(toDeg - fromDeg, 360f);
                if (rel <= span) return Mathf.Abs(d.magnitude - r) - thickness * 0.5f;
                return Mathf.Min((p - a).magnitude, (p - b).magnitude) - thickness * 0.5f;
            };
        }

        /// <summary>Filled polygon (any shape, vertices in order).</summary>
        public static Func<Vector2, float> Polygon(params Vector2[] v) => p =>
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                var e = v[j] - v[i];
                var w = p - v[i];
                var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        };

        public static Func<Vector2, float> Union(Func<Vector2, float> a, Func<Vector2, float> b) => p => Mathf.Min(a(p), b(p));
        public static Func<Vector2, float> Subtract(Func<Vector2, float> a, Func<Vector2, float> b) => p => Mathf.Max(a(p), -b(p));
        public static Func<Vector2, float> Intersect(Func<Vector2, float> a, Func<Vector2, float> b) => p => Mathf.Max(a(p), b(p));

        /// <summary>Smooth union (blobby merge), k = blend size in pixels.</summary>
        public static Func<Vector2, float> SmoothUnion(Func<Vector2, float> a, Func<Vector2, float> b, float k) => p =>
        {
            float da = a(p), db = b(p);
            float h = Mathf.Clamp01(0.5f + 0.5f * (db - da) / k);
            return Mathf.Lerp(db, da, h) - k * h * (1 - h);
        };

        public static Func<Vector2, Color> VGradient(float y0, Color c0, float y1, Color c1) =>
            p => Color.Lerp(c0, c1, Mathf.InverseLerp(y0, y1, p.y));

        public static Color Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            c.a = alpha;
            return c;
        }
    }
}
