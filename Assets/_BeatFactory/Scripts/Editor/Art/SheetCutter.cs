using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// Cuts parts out of a character sheet whose "transparent" checkerboard is painted into the image (no real alpha):
    /// for each box the checkerboard connected to the box border is flood-filled away (light/mid neutral greys), the edge
    /// is softened by one pixel, the result is trimmed and optionally scaled. Grey details enclosed by the character
    /// (chains, white soles) survive because only background connected to the border is removed.
    /// </summary>
    public static class SheetCutter
    {
        public class Cut
        {
            public string name;
            public Color32[] px;
            public int w, h;
            /// <summary>Where the trimmed image's top-left sat on the sheet (top-left origin).</summary>
            public Vector2Int sheetTopLeft;
        }

        public static Texture2D LoadSheet(string path)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(path));
            return tex;
        }

        static bool Checker(Color32 c)
        {
            int max = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return max - min <= 20 && max >= 92 && max <= 232;
        }

        /// <summary>Box in sheet pixels, top-left origin (as seen in an image viewer).</summary>
        public static Cut CutOut(Texture2D sheet, string name, RectInt box, float scale = 1f)
        {
            int W = box.width, H = box.height;
            var src = new Color32[W * H];
            var all = sheet.GetPixels32();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int sx = box.x + x, sy = sheet.height - 1 - (box.y + y); // flip to texture coords
                    src[y * W + x] = all[Mathf.Clamp(sy, 0, sheet.height - 1) * sheet.width + Mathf.Clamp(sx, 0, sheet.width - 1)];
                }
            // src row 0 = top of the box
            var bg = new bool[W * H];
            var stack = new Stack<int>();
            for (int x = 0; x < W; x++) { stack.Push(x); stack.Push((H - 1) * W + x); }
            for (int y = 0; y < H; y++) { stack.Push(y * W); stack.Push(y * W + W - 1); }
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                if (bg[i] || !Checker(src[i])) continue;
                bg[i] = true;
                int x = i % W, y = i / W;
                if (x > 0) stack.Push(i - 1);
                if (x < W - 1) stack.Push(i + 1);
                if (y > 0) stack.Push(i - W);
                if (y < H - 1) stack.Push(i + W);
            }
            // enclosed checkerboard (e.g. between the legs) is background too when the patch is big; thin grey details stay
            var seen = new bool[W * H];
            var comp = new List<int>();
            for (int start = 0; start < W * H; start++)
            {
                if (bg[start] || seen[start] || !Checker(src[start])) continue;
                comp.Clear();
                stack.Push(start);
                while (stack.Count > 0)
                {
                    int i = stack.Pop();
                    if (seen[i] || bg[i] || !Checker(src[i])) continue;
                    seen[i] = true;
                    comp.Add(i);
                    int x = i % W, y = i / W;
                    if (x > 0) stack.Push(i - 1);
                    if (x < W - 1) stack.Push(i + 1);
                    if (y > 0) stack.Push(i - W);
                    if (y < H - 1) stack.Push(i + W);
                }
                if (comp.Count >= 70) foreach (int i in comp) bg[i] = true;
            }
            // alpha + 1px soft edge
            var outPx = new Color32[W * H];
            int x0 = W, y0 = H, x1 = -1, y1 = -1;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    if (bg[i]) { outPx[i] = new Color32(0, 0, 0, 0); continue; }
                    int n = 0;
                    if (x > 0 && bg[i - 1]) n++;
                    if (x < W - 1 && bg[i + 1]) n++;
                    if (y > 0 && bg[i - W]) n++;
                    if (y < H - 1 && bg[i + W]) n++;
                    var c = src[i];
                    c.a = (byte)(n >= 2 ? 110 : n == 1 ? 190 : 255);
                    outPx[i] = c;
                    if (x < x0) x0 = x; if (y < y0) y0 = y; if (x > x1) x1 = x; if (y > y1) y1 = y;
                }
            if (x1 < 0) return null;
            // drop tiny specks (flood leftovers): keep the trim box as is, it's fine for sprites
            int tw = x1 - x0 + 1, th = y1 - y0 + 1;
            var trimmed = new Color32[tw * th];
            for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                    trimmed[(th - 1 - y) * tw + x] = outPx[(y0 + y) * W + (x0 + x)]; // back to bottom-up rows
            var cut = new Cut { name = name, px = trimmed, w = tw, h = th, sheetTopLeft = new Vector2Int(box.x + x0, box.y + y0) };
            return Mathf.Approximately(scale, 1f) ? cut : Scale(cut, scale);
        }

        /// <summary>Bilinear resize (straight alpha, colour weighted by alpha so edges don't go dark).</summary>
        public static Cut Scale(Cut c, float s)
        {
            int nw = Mathf.Max(1, Mathf.RoundToInt(c.w * s)), nh = Mathf.Max(1, Mathf.RoundToInt(c.h * s));
            var o = new Color32[nw * nh];
            for (int y = 0; y < nh; y++)
                for (int x = 0; x < nw; x++)
                {
                    float fx = (x + 0.5f) / s - 0.5f, fy = (y + 0.5f) / s - 0.5f;
                    int ix = Mathf.FloorToInt(fx), iy = Mathf.FloorToInt(fy);
                    float tx = fx - ix, ty = fy - iy;
                    float r = 0, g = 0, b = 0, a = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        int sx = Mathf.Clamp(ix + (k & 1), 0, c.w - 1), sy = Mathf.Clamp(iy + (k >> 1), 0, c.h - 1);
                        float wgt = ((k & 1) == 1 ? tx : 1 - tx) * ((k >> 1) == 1 ? ty : 1 - ty);
                        var p = c.px[sy * c.w + sx];
                        float pa = p.a / 255f * wgt;
                        r += p.r * pa; g += p.g * pa; b += p.b * pa; a += pa;
                    }
                    o[y * nw + x] = a > 0.0001f ? new Color32((byte)(r / a), (byte)(g / a), (byte)(b / a), (byte)Mathf.Clamp(a * 255f, 0, 255)) : new Color32(0, 0, 0, 0);
                }
            return new Cut { name = c.name, px = o, w = nw, h = nh, sheetTopLeft = c.sheetTopLeft };
        }

        public static void Save(Cut c, string path)
        {
            var t = new Texture2D(c.w, c.h, TextureFormat.RGBA32, false);
            t.SetPixels32(c.px);
            t.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
        }

        /// <summary>Contact sheet of cuts on a dark background (for checking the cut-outs by eye).</summary>
        public static void Montage(List<Cut> cuts, string path, int cell = 200)
        {
            int cols = 8, rows = (cuts.Count + cols - 1) / cols;
            var t = new Texture2D(cols * cell, rows * cell, TextureFormat.RGBA32, false);
            var fill = new Color32[t.width * t.height];
            for (int i = 0; i < fill.Length; i++) fill[i] = ((i / t.width / 8 + i % t.width / 8) % 2 == 0) ? new Color32(40, 120, 60, 255) : new Color32(30, 90, 45, 255);
            t.SetPixels32(fill);
            for (int k = 0; k < cuts.Count; k++)
            {
                var c = cuts[k];
                float s = Mathf.Min(1f, (cell - 8f) / Mathf.Max(c.w, c.h));
                var sc = s < 1f ? Scale(c, s) : c;
                int ox = (k % cols) * cell + 4, oy = (rows - 1 - k / cols) * cell + 4;
                for (int y = 0; y < sc.h; y++)
                    for (int x = 0; x < sc.w; x++)
                    {
                        var p = sc.px[y * sc.w + x];
                        if (p.a == 0) continue;
                        var bgc = t.GetPixel(ox + x, oy + y);
                        float a = p.a / 255f;
                        t.SetPixel(ox + x, oy + y, new Color(Mathf.Lerp(bgc.r, p.r / 255f, a), Mathf.Lerp(bgc.g, p.g / 255f, a), Mathf.Lerp(bgc.b, p.b / 255f, a), 1));
                    }
            }
            t.Apply();
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
        }
    }
}
