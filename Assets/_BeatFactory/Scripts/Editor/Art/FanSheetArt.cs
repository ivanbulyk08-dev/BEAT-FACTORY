using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// Front-row concert fans: the user's sheet (Art/Reference/fans_sheet.png, figures on a flat #00FF00 background) is
    /// chroma-keyed (soft alpha from the "greenness", green spill removed from the edges) and split into one transparent
    /// PNG per fan by connected regions. Output: Art/Venues/fans/fan_N.png, ordered row by row, left to right.
    /// </summary>
    public static class FanSheetArt
    {
        public const string Sheet = BeatFactorySceneBuilder.Root + "/Art/Reference/fans_sheet.png";
        public const string OutDir = BeatFactorySceneBuilder.Root + "/Art/Venues/fans";

        public static string FanPath(int i) => OutDir + "/fan_" + i + ".png";

        [MenuItem("Beat Factory/Art/Cut Concert Fans")]
        public static int Cut()
        {
            if (!File.Exists(Sheet)) return 0;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(Sheet));
            int w = tex.width, h = tex.height;
            var src = tex.GetPixels32();
            Object.DestroyImmediate(tex);

            // key: alpha from how much green dominates; despill edges
            var px = new Color32[w * h];
            var solid = new bool[w * h];
            for (int i = 0; i < px.Length; i++)
            {
                var c = src[i];
                int other = Mathf.Max(c.r, c.b);
                int k = c.g - other;
                float a = Mathf.Clamp01((110f - k) / 80f);
                byte g = (byte)Mathf.Min(c.g, other + 12);
                px[i] = new Color32(c.r, a < 1f ? g : c.g, c.b, (byte)(a * 255));
                solid[i] = a > 0.5f;
            }

            // connected regions (8-neighbour) on the solid mask
            var label = new int[w * h];
            var boxes = new List<RectInt>();
            var sizes = new List<int>();
            var stack = new Stack<int>();
            for (int s = 0; s < w * h; s++)
            {
                if (!solid[s] || label[s] != 0) continue;
                int id = boxes.Count + 1, n = 0, x0 = w, y0 = h, x1 = 0, y1 = 0;
                stack.Push(s);
                label[s] = id;
                while (stack.Count > 0)
                {
                    int i = stack.Pop();
                    n++;
                    int x = i % w, y = i / w;
                    x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                            int j = ny * w + nx;
                            if (!solid[j] || label[j] != 0) continue;
                            label[j] = id;
                            stack.Push(j);
                        }
                }
                boxes.Add(new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1));
                sizes.Add(n);
            }
            // fans = big regions; small bits (a loose hair tip, a ring) join the fan whose box contains / is nearest to them
            var fans = new List<int>();
            for (int i = 0; i < boxes.Count; i++) if (sizes[i] > 4000) fans.Add(i);
            var owner = new int[boxes.Count];
            for (int i = 0; i < boxes.Count; i++)
            {
                owner[i] = -1;
                if (sizes[i] > 4000) { owner[i] = i; continue; }
                float best = float.MaxValue;
                foreach (var f in fans)
                {
                    float d = (boxes[f].center - boxes[i].center).sqrMagnitude;
                    if (d < best) { best = d; owner[i] = f; }
                }
            }
            // texture rows are bottom-up: "top row" in the image = larger y here
            fans.Sort((a, b) =>
            {
                int ra = boxes[a].center.y > h / 2 ? 0 : 1, rb = boxes[b].center.y > h / 2 ? 0 : 1;
                return ra != rb ? ra.CompareTo(rb) : boxes[a].center.x.CompareTo(boxes[b].center.x);
            });

            Directory.CreateDirectory(OutDir);
            for (int k = 0; k < fans.Count; k++)
            {
                int f = fans[k];
                var box = boxes[f];
                for (int i = 0; i < boxes.Count; i++) if (owner[i] == f) { var b = boxes[i]; box = Union(box, b); }
                int pad = 3;
                box = new RectInt(Mathf.Max(0, box.x - pad), Mathf.Max(0, box.y - pad),
                    Mathf.Min(w, box.xMax + pad) - Mathf.Max(0, box.x - pad), Mathf.Min(h, box.yMax + pad) - Mathf.Max(0, box.y - pad));
                var o = new Color32[box.width * box.height];
                for (int y = 0; y < box.height; y++)
                    for (int x = 0; x < box.width; x++)
                    {
                        int i = (box.y + y) * w + box.x + x;
                        int l = label[i];
                        bool mine = l == 0 ? true : owner[l - 1] == f; // soft edge pixels (unlabelled) belong to whoever is there
                        o[y * box.width + x] = mine ? px[i] : new Color32(0, 0, 0, 0);
                    }
                var t = new Texture2D(box.width, box.height, TextureFormat.RGBA32, false);
                t.SetPixels32(o);
                t.Apply();
                File.WriteAllBytes(FanPath(k), t.EncodeToPNG());
                Object.DestroyImmediate(t);
            }
            AssetDatabase.Refresh();
            for (int k = 0; k < fans.Count; k++)
            {
                var imp = AssetImporter.GetAtPath(FanPath(k)) as TextureImporter;
                if (imp == null) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.maxTextureSize = 1024;
                imp.SaveAndReimport();
            }
            return fans.Count;
        }

        static RectInt Union(RectInt a, RectInt b)
        {
            int x0 = Mathf.Min(a.xMin, b.xMin), y0 = Mathf.Min(a.yMin, b.yMin);
            return new RectInt(x0, y0, Mathf.Max(a.xMax, b.xMax) - x0, Mathf.Max(a.yMax, b.yMax) - y0);
        }
    }
}
