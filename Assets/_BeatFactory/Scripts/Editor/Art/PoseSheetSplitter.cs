using System;
using System.Collections.Generic;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// Splits a character pose sheet whose "transparency" is a checkerboard painted into the pixels into one image per figure.
    /// Pure C# (no UnityEngine) so the same code can be checked outside the editor.
    ///  1. Background = light neutral pixels flood-filled from the sheet border (+ given seeds for enclosed holes such as the
    ///     gap under a raised arm). White sneakers survive because their dark outline stops the fill.
    ///  2. Figures that touch are split by a geodesic race from one seed point per figure; loose islands (hair tips, chain bits)
    ///     go to the nearest figure.
    ///  3. The light halo on the outline is turned into soft alpha and recoloured from the inside, so no white fringe shows
    ///     on dark rooms.
    /// Pixel arrays are RGBA, 4 bytes per pixel, top-left origin.
    /// </summary>
    public static class PoseSheetSplitter
    {
        public struct Seed
        {
            public string name;
            public int x, y;
            public Seed(string name, int x, int y) { this.name = name; this.x = x; this.y = y; }
        }

        public class Figure
        {
            public string name;
            public byte[] rgba;
            public int w, h;
            /// <summary>Top-left of the trimmed image on the sheet.</summary>
            public int sheetX, sheetY;
        }

        static bool Light(byte[] p, int i, int minV, int tol)
        {
            int r = p[i * 4], g = p[i * 4 + 1], b = p[i * 4 + 2];
            int mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b));
            return mx - mn <= tol && mn >= minV;
        }

        public static bool[] BackgroundMask(byte[] p, int w, int h, int minV, int tol, int[] holeSeeds)
        {
            var bg = new bool[w * h];
            var st = new Stack<int>();
            for (int x = 0; x < w; x++) { st.Push(x); st.Push((h - 1) * w + x); }
            for (int y = 0; y < h; y++) { st.Push(y * w); st.Push(y * w + w - 1); }
            if (holeSeeds != null)
                for (int k = 0; k + 1 < holeSeeds.Length; k += 2) st.Push(holeSeeds[k + 1] * w + holeSeeds[k]);
            while (st.Count > 0)
            {
                int i = st.Pop();
                if (bg[i] || !Light(p, i, minV, tol)) continue;
                bg[i] = true;
                int x = i % w, y = i / w;
                if (x > 0) st.Push(i - 1);
                if (x < w - 1) st.Push(i + 1);
                if (y > 0) st.Push(i - w);
                if (y < h - 1) st.Push(i + w);
            }
            return bg;
        }

        /// <summary>Owner figure index per pixel (-1 = background / dropped speck). Several seeds may share a name (one figure);
        /// the index is the position of the name's first seed in <see cref="FigureNames"/>.</summary>
        public static int[] Assign(bool[] bg, int w, int h, Seed[] seeds)
        {
            var owner = new int[w * h];
            for (int i = 0; i < owner.Length; i++) owner[i] = -1;
            var q = new Queue<int>();
            var names = FigureNames(seeds);
            for (int s = 0; s < seeds.Length; s++)
            {
                int i = seeds[s].y * w + seeds[s].x;
                if (bg[i]) throw new Exception("seed " + seeds[s].name + " is on background");
                owner[i] = names.IndexOf(seeds[s].name);
                q.Enqueue(i);
            }
            // race through foreground (8-connected)
            while (q.Count > 0)
            {
                int i = q.Dequeue();
                int x = i % w, y = i / w;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        int n = ny * w + nx;
                        if (bg[n] || owner[n] >= 0) continue;
                        owner[n] = owner[i];
                        q.Enqueue(n);
                    }
            }
            // islands not connected to any seed: nearest figure (spread over everything, then keep only foreground)
            var near = (int[])owner.Clone();
            for (int i = 0; i < near.Length; i++) if (near[i] >= 0) q.Enqueue(i);
            while (q.Count > 0)
            {
                int i = q.Dequeue();
                int x = i % w, y = i / w;
                if (x > 0 && near[i - 1] < 0) { near[i - 1] = near[i]; q.Enqueue(i - 1); }
                if (x < w - 1 && near[i + 1] < 0) { near[i + 1] = near[i]; q.Enqueue(i + 1); }
                if (y > 0 && near[i - w] < 0) { near[i - w] = near[i]; q.Enqueue(i - w); }
                if (y < h - 1 && near[i + w] < 0) { near[i + w] = near[i]; q.Enqueue(i + w); }
            }
            for (int i = 0; i < owner.Length; i++) if (!bg[i] && owner[i] < 0) owner[i] = near[i];
            return owner;
        }

        public static List<string> FigureNames(Seed[] seeds)
        {
            var names = new List<string>();
            foreach (var s in seeds) if (!names.Contains(s.name)) names.Add(s.name);
            return names;
        }

        /// <summary>Distance (in px, 4-neighbour steps, capped) from each pixel to the nearest pixel that is not this figure.</summary>
        static int[] EdgeDistance(int[] owner, int idx, int w, int h, int cap)
        {
            var d = new int[w * h];
            var q = new Queue<int>();
            for (int i = 0; i < d.Length; i++)
            {
                if (owner[i] != idx) { d[i] = 0; q.Enqueue(i); }
                else d[i] = cap;
            }
            while (q.Count > 0)
            {
                int i = q.Dequeue();
                int x = i % w, y = i / w, nd = d[i] + 1;
                if (nd >= cap) continue;
                if (x > 0 && d[i - 1] > nd) { d[i - 1] = nd; q.Enqueue(i - 1); }
                if (x < w - 1 && d[i + 1] > nd) { d[i + 1] = nd; q.Enqueue(i + 1); }
                if (y > 0 && d[i - w] > nd) { d[i - w] = nd; q.Enqueue(i - w); }
                if (y < h - 1 && d[i + w] > nd) { d[i + w] = nd; q.Enqueue(i + w); }
            }
            return d;
        }

        public static Figure Extract(byte[] p, int[] owner, int w, int h, int idx, string name, int pad)
        {
            var dist = EdgeDistance(owner, idx, w, h, 4);
            int x0 = w, y0 = h, x1 = -1, y1 = -1;
            for (int i = 0; i < owner.Length; i++)
            {
                if (owner[i] != idx) continue;
                int x = i % w, y = i / w;
                if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y;
            }
            if (x1 < 0) return null;
            x0 -= pad; y0 -= pad; x1 += pad; y1 += pad;
            int ow = x1 - x0 + 1, oh = y1 - y0 + 1;
            var o = new byte[ow * oh * 4];
            for (int y = 0; y < oh; y++)
                for (int x = 0; x < ow; x++)
                {
                    int sx = x0 + x, sy = y0 + y;
                    if (sx < 0 || sy < 0 || sx >= w || sy >= h) continue;
                    int i = sy * w + sx;
                    if (owner[i] != idx) continue;
                    int r = p[i * 4], g = p[i * 4 + 1], b = p[i * 4 + 2];
                    float a = 1f;
                    int dd = dist[i];
                    if (dd <= 2)
                    {
                        // outline band: light neutral pixels are mostly checkerboard mixed into the edge
                        int mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b));
                        if (mx - mn < 30 && mx > 140) a = Math.Max(0f, Math.Min(1f, (250f - mx) / 110f));
                        if (dd == 1) a *= 0.8f;
                        if (a < 1f) InteriorColor(p, owner, dist, w, h, sx, sy, idx, ref r, ref g, ref b);
                    }
                    int k = (y * ow + x) * 4;
                    o[k] = (byte)r; o[k + 1] = (byte)g; o[k + 2] = (byte)b; o[k + 3] = (byte)Math.Round(a * 255f);
                }
            return new Figure { name = name, rgba = o, w = ow, h = oh, sheetX = x0, sheetY = y0 };
        }

        /// <summary>Average colour of nearby pixels that lie deeper inside the figure (used to recolour the soft edge).</summary>
        static void InteriorColor(byte[] p, int[] owner, int[] dist, int w, int h, int cx, int cy, int idx, ref int r, ref int g, ref int b)
        {
            int sr = 0, sg = 0, sb = 0, n = 0;
            for (int dy = -3; dy <= 3; dy++)
                for (int dx = -3; dx <= 3; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    int i = y * w + x;
                    if (owner[i] != idx || dist[i] < 3) continue;
                    sr += p[i * 4]; sg += p[i * 4 + 1]; sb += p[i * 4 + 2]; n++;
                }
            if (n == 0) return;
            r = sr / n; g = sg / n; b = sb / n;
        }
    }
}
