using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Original abstract logos generated at runtime from a seed (id) and a brand color: a gradient badge with one of
    /// several emblems (rings, play mark, waveform, burst, vinyl, bolt, hexagon, sound arcs, diamond stack, crescent).
    /// Random per item but stable (same id → same logo). No real brand marks are reproduced.
    /// </summary>
    public static class LogoGenerator
    {
        const int Size = 128;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <param name="index">Position in its list: neighbours never share an emblem (-1 = pick from the id).</param>
        public static Sprite Get(string id, Color color, int index = -1)
        {
            string key = id + ColorUtility.ToHtmlStringRGB(color) + index;
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = Make(id, color, index);
            cache[key] = s;
            return s;
        }

        // ------------------------------------------------------------------ tiny SDF painter

        delegate float Sdf(Vector2 p);

        static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;
        static float Ring(Vector2 p, Vector2 c, float r, float w) => Mathf.Abs((p - c).magnitude - r) - w * 0.5f;
        static float Box(Vector2 p, Vector2 c, Vector2 h, float r)
        {
            var d = new Vector2(Mathf.Abs(p.x - c.x) - h.x + r, Mathf.Abs(p.y - c.y) - h.y + r);
            return new Vector2(Mathf.Max(d.x, 0), Mathf.Max(d.y, 0)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0) - r;
        }
        static float Poly(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]), s = 1;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                var e = v[j] - v[i]; var w = p - v[i];
                var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }
        static Vector2 Rot(Vector2 p, Vector2 c, float deg)
        {
            float a = deg * Mathf.Deg2Rad, cs = Mathf.Cos(a), sn = Mathf.Sin(a);
            var d = p - c;
            return c + new Vector2(d.x * cs - d.y * sn, d.x * sn + d.y * cs);
        }

        static Sprite Make(string id, Color color, int index)
        {
            int h = Math.Abs((id ?? "x").GetHashCode());
            var r = new System.Random(h);
            var px = new Color[Size * Size];
            var c = new Vector2(64, 64);
            Color.RGBToHSV(color, out float hue, out float sat, out float val);
            Color dark = Color.HSVToRGB(hue, Mathf.Clamp01(sat * 0.9f), 0.18f);
            Color light = Color.HSVToRGB(Mathf.Repeat(hue + (float)(r.NextDouble() * 0.16 - 0.08), 1f), Mathf.Clamp01(sat), Mathf.Max(0.75f, val));
            Color accent = Color.HSVToRGB(Mathf.Repeat(hue + 0.5f + (float)(r.NextDouble() * 0.2 - 0.1), 1f), 0.6f, 1f);
            bool roundBadge = r.NextDouble() < 0.5;
            int emblem = r.Next(10);
            if (index >= 0) emblem = (Math.Abs((id ?? "").Split('_')[0].GetHashCode()) + index * 7) % 10; // 7 is coprime with 10 → 10 different in a row
            float rot = (float)(r.NextDouble() * 40 - 20);
            int bars = 4 + r.Next(4);

            Sdf badge = roundBadge ? (Sdf)(p => Circle(p, c, 60)) : (p => Box(p, c, new Vector2(58, 58), 22));
            Sdf shape;
            switch (emblem)
            {
                case 0: shape = p => Mathf.Min(Ring(p, c, 34, 7), Mathf.Min(Ring(p, c, 18, 6), Circle(p, c, 6))); break;          // rings
                case 1:
                {
                    var tri = new[] { new Vector2(50, 38), new Vector2(50, 90), new Vector2(92, 64) };
                    shape = p => Mathf.Min(Poly(p, tri) - 3, Ring(p, c, 44, 5)); break;                                             // play mark
                }
                case 2: shape = p =>                                                                                                 // waveform
                {
                    float best = 999;
                    for (int i = 0; i < bars; i++)
                    {
                        float x = 34 + i * (60f / (bars - 1));
                        float hh = 10 + 26 * Mathf.Abs(Mathf.Sin(i * 1.7f + h % 7));
                        best = Mathf.Min(best, Box(p, new Vector2(x, 64), new Vector2(3.5f, hh), 3.5f));
                    }
                    return best;
                }; break;
                case 3:
                {
                    var star = new Vector2[16];
                    for (int i = 0; i < 16; i++)
                    {
                        float a = i * Mathf.PI / 8f + rot * Mathf.Deg2Rad;
                        float rr = i % 2 == 0 ? 42 : 16;
                        star[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr;
                    }
                    shape = p => Poly(p, star); break;                                                                               // burst
                }
                case 4: shape = p => Mathf.Min(Mathf.Max(Circle(p, c, 40), -Circle(p, c, 34)), Mathf.Min(Ring(p, c, 22, 2), Circle(p, c, 8))); break; // vinyl
                case 5:
                {
                    var bolt = new[] { new Vector2(70, 104), new Vector2(42, 60), new Vector2(62, 60), new Vector2(54, 24), new Vector2(88, 72), new Vector2(66, 72) };
                    shape = p => Poly(Rot(p, c, rot * 0.5f), bolt) - 1; break;                                                       // bolt
                }
                case 6:
                {
                    var hex = new Vector2[6];
                    for (int i = 0; i < 6; i++) { float a = i * Mathf.PI / 3f + Mathf.PI / 6f; hex[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 40; }
                    shape = p => Mathf.Min(Mathf.Abs(Poly(p, hex)) - 4, Circle(p, c, 12)); break;                                    // hexagon
                }
                case 7: shape = p =>                                                                                                 // sound arcs
                {
                    var q = Rot(p, c, rot) - new Vector2(40, 64);
                    float a = Mathf.Abs(Mathf.Atan2(q.y, q.x));
                    float best = Circle(p, Rot(new Vector2(40, 64), c, -rot), 8);
                    if (a < 0.9f) for (int i = 1; i <= 3; i++) best = Mathf.Min(best, Mathf.Abs(q.magnitude - i * 17f) - 3.5f);
                    return best;
                }; break;
                case 8: shape = p =>                                                                                                 // diamond stack
                {
                    var q = Rot(p, c, 45);
                    return Mathf.Min(Box(q, c + new Vector2(-12, -12), new Vector2(14, 14), 3), Mathf.Min(Box(q, c + new Vector2(12, 12), new Vector2(14, 14), 3), Box(q, c + new Vector2(12, -12), new Vector2(8, 8), 2)));
                }; break;
                default: shape = p => Mathf.Max(Circle(p, c, 40), -Circle(p, c + Rot(new Vector2(16, 10), Vector2.zero, rot), 34)); break; // crescent
            }

            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float bd = badge(p);
                float ba = Mathf.Clamp01(0.5f - bd);
                if (ba <= 0) { px[y * Size + x] = Color.clear; continue; }
                // diagonal gradient badge + soft inner glow
                float g = Mathf.Clamp01((x + y) / (2f * Size));
                var col = Color.Lerp(dark, light * 0.75f, g);
                col = Color.Lerp(col, light, Mathf.Clamp01(1f - (p - c).magnitude / 70f) * 0.25f);
                float sd = shape(p);
                float sa = Mathf.Clamp01(0.5f - sd);
                var emblemCol = Color.Lerp(Color.white, accent, Mathf.Clamp01((y - 30f) / 90f) * 0.35f);
                // drop shadow under the emblem
                float shadow = Mathf.Clamp01(0.5f - shape(p + new Vector2(-2, 3))) * 0.35f;
                col = Color.Lerp(col, Color.black, shadow * (1 - sa));
                col = Color.Lerp(col, emblemCol, sa);
                // rim
                col = Color.Lerp(col, Color.white, Mathf.Clamp01(1f - Mathf.Abs(bd + 2f) / 1.5f) * 0.25f);
                col.a = ba;
                px[y * Size + x] = col;
            }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Logo_" + id };
            tex.SetPixels(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100);
        }
    }
}
