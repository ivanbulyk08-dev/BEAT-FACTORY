using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// Turns the user's seated producer image (Art/Reference/producer_seated_source.png, 1024×1536, black background, no alpha)
    /// into two transparent layers for the basement: BODY (producer + chair) and HEAD (nods to the beat).
    /// The background is removed by flood-filling the near-black border colour; the image's own laptop, keyboard, headphones and
    /// desk are erased so the studio's desk and live laptop show through. The head is cut at the neck and also kept in the body
    /// under a soft band, so the nod never opens a gap.
    /// </summary>
    public static class SeatedProducerCutout
    {
        public const string Source = BeatFactorySceneBuilder.Root + "/Art/Reference/producer_seated_source.png";
        public const string OutDir = BeatFactorySceneBuilder.Root + "/Art/Studios/studio_01/producer";
        public const string BodyPath = OutDir + "/producer_body.png";
        public const string HeadPath = OutDir + "/producer_head.png";

        /// <summary>Neck pivot of the head layer (source pixels, top-left origin).</summary>
        public static readonly Vector2 Neck = new Vector2(432, 478);
        /// <summary>Chair emblem centre (source pixels, top-left origin) — aligned to the painted chair.</summary>
        public static readonly Vector2 Emblem = new Vector2(215, 755);
        public const int W = 1024, H = 1536;

        // regions to erase (source pixels, top-left origin): laptop, keys above the forearm, headphones, desk strips, left keys
        static readonly Vector2[][] Erase =
        {
            P((652, 470), (990, 470), (990, 684), (908, 684), (908, 662), (812, 662), (652, 668)),
            P((672, 664), (813, 662), (813, 694), (677, 706)),
            P((906, 636), (1024, 636), (1024, 805), (906, 805)),
            P((770, 721), (1024, 719), (1024, 805), (770, 805)),
            P((600, 757), (772, 757), (772, 784), (600, 784)),
            P((492, 708), (600, 708), (600, 756), (492, 756)),
        };

        static Vector2[] P(params (float x, float y)[] pts)
        {
            var v = new Vector2[pts.Length];
            for (int i = 0; i < pts.Length; i++) v[i] = new Vector2(pts[i].x, pts[i].y);
            return v;
        }

        static bool Inside(Vector2 p, Vector2[] poly)
        {
            bool c = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) c = !c;
            return c;
        }

        [MenuItem("Tools/Beat Factory/Art/Cut Out Seated Producer")]
        public static void Build()
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(Source));
            var src = tex.GetPixels32(); // bottom-up rows
            Object.DestroyImmediate(tex);
            Color32 At(int x, int y) => src[(H - 1 - y) * W + x];          // top-left origin

            // 1. background = near-black connected to the border
            var bg = new bool[W * H];
            var stack = new Stack<int>();
            for (int x = 0; x < W; x++) { stack.Push(x); stack.Push((H - 1) * W + x); }
            for (int y = 0; y < H; y++) { stack.Push(y * W); stack.Push(y * W + W - 1); }
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                if (bg[i]) continue;
                int x = i % W, y = i / W;
                var c = At(x, y);
                if (Mathf.Max(c.r, Mathf.Max(c.g, c.b)) > 1) continue; // only the pure-black background: dark hair strands are 1-10
                bg[i] = true;
                if (x > 0) stack.Push(i - 1);
                if (x < W - 1) stack.Push(i + 1);
                if (y > 0) stack.Push(i - W);
                if (y < H - 1) stack.Push(i + W);
            }
            // 2. alpha: background 0, a soft 1-px edge, the image's own desk/laptop erased (with a soft border)
            var alpha = new float[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    if (bg[i]) continue;
                    int n = 0;
                    if (x > 0 && bg[i - 1]) n++;
                    if (x < W - 1 && bg[i + 1]) n++;
                    if (y > 0 && bg[i - W]) n++;
                    if (y < H - 1 && bg[i + W]) n++;
                    float a = n >= 2 ? 0.45f : n == 1 ? 0.75f : 1f;
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    foreach (var poly in Erase) if (Inside(p, poly)) { a = 0; break; }
                    alpha[i] = a;
                }
            // 3. layers: head = above the neck line (soft), body = everything (keeps the head area under a soft band too)
            var body = new Color32[W * H];
            var head = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    if (alpha[i] <= 0) continue;
                    var c = At(x, y);
                    int ti = (H - 1 - y) * W + x; // back to bottom-up for the texture
                    bool headZone = x > 300 && x < 600 && y < Neck.y + 10;
                    float hk = headZone ? Mathf.Clamp01((Neck.y + 10 - y) / 24f) : 0f;
                    head[ti] = new Color32(c.r, c.g, c.b, (byte)(255 * alpha[i] * hk));
                    // body: below the neck fully; the head area stays only as a dimmed under-layer near the cut
                    float bk = headZone ? Mathf.Clamp01((y - (Neck.y - 70)) / 40f) : 1f;
                    body[ti] = new Color32(c.r, c.g, c.b, (byte)(255 * alpha[i] * bk));
                }
            Directory.CreateDirectory(OutDir);
            Save(body, BodyPath);
            Save(head, HeadPath);
            AssetDatabase.Refresh();
            foreach (var path in new[] { BodyPath, HeadPath })
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.maxTextureSize = 2048;
                var s = new TextureImporterSettings();
                imp.ReadTextureSettings(s);
                s.spriteAlignment = (int)SpriteAlignment.Custom;
                // both layers share the full canvas; the head pivots on the neck, the body on the chair emblem
                var pivot = path == HeadPath ? Neck : Emblem;
                s.spritePivot = new Vector2(pivot.x / W, 1f - pivot.y / H);
                s.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(s);
                imp.SaveAndReimport();
            }
            Debug.Log("[Producer] cut out → " + OutDir);
        }

        static void Save(Color32[] px, string path)
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            t.SetPixels32(px);
            t.Apply();
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
        }
    }
}
