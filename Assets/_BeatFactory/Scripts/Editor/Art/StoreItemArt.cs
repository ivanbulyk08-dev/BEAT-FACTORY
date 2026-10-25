using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// STORE clothes for the main character in the studio rooms, from the user's product pictures
    /// (Art/Store/source/*.png, flat white or black background):
    ///  1. the background is removed → transparent store picture (Art/Store/items).
    ///  2. for every pose sprite of the character, his own tee / pants pixels (inside a rough per-pose region, dark cloth
    ///     only — skin, hair, chains and sneakers stay) are refilled with the product's look, keeping the pose's folds as
    ///     shading → one overlay PNG per item and pose (Art/Store/outfits), drawn over the pose in the game.
    ///  3. one CharacterOutfitItem per product + the StoreCatalog.
    /// An approximation of the real garment on the pose (the cut stays his own tee / baggy pants).
    /// </summary>
    public static class StoreItemArt
    {
        const string Src = BeatFactorySceneBuilder.Root + "/Art/Store/source";
        const string Items = BeatFactorySceneBuilder.Root + "/Art/Store/items";
        const string Outfits = BeatFactorySceneBuilder.Root + "/Art/Store/outfits";
        const string ItemDir = BeatFactorySceneBuilder.Root + "/Data/Store/Items";
        public const string CatalogPath = BeatFactorySceneBuilder.Root + "/Data/Store/StoreCatalog.asset";

        class Def
        {
            public string id, source, name;
            public WardrobeSlot slot;
            public double price;
            public bool darkBackground;
            /// <summary>Folder (Art/Store/source/…) with the whole character drawn in this item, one PNG per pose name.</summary>
            public string fullFigures;
        }

        static readonly Def[] Defs =
        {
            new Def { id = "outfit_tee_nun", source = "top_tee_nun", name = "Nun Print Tee", slot = WardrobeSlot.Top, price = 2_500_000, darkBackground = true },
            new Def { id = "outfit_pants_cargo_stitch", source = "pants_cargo_stitch", name = "Stitch Cargo Pants", slot = WardrobeSlot.Pants, price = 10_000_000 },
            new Def { id = "outfit_pants_leather_zip", source = "pants_leather_zip", name = "Zip Leather Pants", slot = WardrobeSlot.Pants, price = 40_000_000, fullFigures = "pants_leather_zip_poses" },
            new Def { id = "outfit_pants_graffiti_denim", source = "pants_graffiti_denim", name = "Graffiti Denim", slot = WardrobeSlot.Pants, price = 60_000_000 },
            new Def { id = "outfit_jacket_moto_black", source = "jacket_moto_black", name = "Moto Leather Jacket", slot = WardrobeSlot.Top, price = 200_000_000 },
            new Def { id = "outfit_jacket_fur_brown", source = "jacket_fur_brown", name = "Brown Fur Jacket", slot = WardrobeSlot.Top, price = 1_500_000_000, fullFigures = "jacket_fur_brown_poses" },
            new Def { id = "outfit_pants_gold_foil", source = "pants_gold_foil", name = "Gold Foil Flares", slot = WardrobeSlot.Pants, price = 10_000_000_000, fullFigures = "pants_gold_foil_poses" },
        };

        /// <summary>Rough regions of the tee (torso + sleeves) and of the pants per pose, in pose sprite px (top-left origin).</summary>
        struct Regions { public Vector2[] top, pants; }

        static Vector2[] P(params float[] xy)
        {
            var v = new Vector2[xy.Length / 2];
            for (int i = 0; i < v.Length; i++) v[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            return v;
        }

        static readonly Dictionary<CharacterPose, Regions> PoseRegions = new Dictionary<CharacterPose, Regions>
        {
            { CharacterPose.StandIdle, new Regions { top = P(20, 92, 240, 92, 240, 300, 20, 300), pants = P(25, 296, 235, 296, 245, 655, 15, 655) } },
            { CharacterPose.StandSide, new Regions { top = P(15, 88, 160, 88, 160, 315, 15, 315), pants = P(15, 310, 165, 310, 170, 612, 5, 612) } },
            { CharacterPose.StandArmsCrossed, new Regions { top = P(5, 84, 200, 84, 200, 298, 5, 298), pants = P(10, 294, 200, 294, 200, 652, 5, 652) } },
            { CharacterPose.StandHeadTouch, new Regions { top = P(5, 88, 250, 88, 250, 300, 5, 300), pants = P(15, 296, 240, 296, 245, 662, 15, 662) } },
            { CharacterPose.StandBack, new Regions { top = P(5, 88, 205, 88, 205, 300, 5, 300), pants = P(15, 296, 200, 296, 200, 642, 10, 642) } },
            { CharacterPose.SitRelaxed, new Regions { top = P(30, 95, 165, 90, 210, 120, 210, 190, 125, 210, 30, 210), pants = P(0, 210, 205, 210, 220, 125, 344, 125, 344, 320, 0, 320) } },
            { CharacterPose.SitThinking, new Regions { top = P(55, 100, 195, 100, 210, 130, 200, 200, 125, 205, 110, 165, 55, 160), pants = P(0, 185, 293, 185, 293, 318, 0, 318) } },
            { CharacterPose.Crouch, new Regions { top = P(50, 95, 250, 95, 280, 150, 260, 180, 40, 180, 30, 150), pants = P(0, 192, 319, 192, 319, 298, 0, 298) } },
        };

        [MenuItem("Beat Factory/Art/Build Store Items")]
        public static StoreCatalog Build()
        {
            Directory.CreateDirectory(Items);
            Directory.CreateDirectory(Outfits);
            Directory.CreateDirectory(ItemDir);
            RemoveFromNox();
            var made = new List<CharacterOutfitItem>();
            foreach (var d in Defs)
            {
                string src = Src + "/" + d.source + ".png";
                if (!File.Exists(src)) { Debug.LogWarning("Store picture missing: " + src); continue; }
                var cut = Cutout(src, d.darkBackground, out int cw, out int ch);
                string thumbPath = Items + "/" + d.id + ".png";
                SavePng(Downscale(cut, cw, ch, 512, out int tw, out int th), tw, th, thumbPath);
                ImportSprite(thumbPath);

                var poses = new List<CharacterOutfitItem.PoseSprite>();
                bool full = d.fullFigures != null && Directory.Exists(Src + "/" + d.fullFigures);
                if (full)
                    foreach (CharacterPose pose in System.Enum.GetValues(typeof(CharacterPose)))
                    {
                        string figPath = Src + "/" + d.fullFigures + "/" + pose + ".png";
                        string posePath = MainCharacterPoseArt.PosePath(pose);
                        if (!File.Exists(figPath) || !File.Exists(posePath)) continue;
                        string outPath = Outfits + "/" + d.id + "_full_" + pose + ".png";
                        FullFigure(figPath, posePath, outPath);
                        ImportSprite(outPath);
                        poses.Add(new CharacterOutfitItem.PoseSprite { pose = pose, sprite = AssetDatabase.LoadAssetAtPath<Sprite>(outPath) });
                    }
                if (!full) foreach (var pr in PoseRegions)
                {
                    string posePath = MainCharacterPoseArt.PosePath(pr.Key);
                    if (!File.Exists(posePath)) continue;
                    string outPath = Outfits + "/" + d.id + "_" + pr.Key + ".png";
                    if (!Overlay(posePath, pr.Value, d.slot == WardrobeSlot.Top, cut, cw, ch, outPath)) continue;
                    ImportSprite(outPath);
                    poses.Add(new CharacterOutfitItem.PoseSprite { pose = pr.Key, sprite = AssetDatabase.LoadAssetAtPath<Sprite>(outPath) });
                }

                string itemPath = ItemDir + "/" + d.id + ".asset";
                var item = AssetDatabase.LoadAssetAtPath<CharacterOutfitItem>(itemPath);
                if (item == null)
                {
                    item = ScriptableObject.CreateInstance<CharacterOutfitItem>();
                    AssetDatabase.CreateAsset(item, itemPath);
                }
                item.id = d.id;
                item.slot = d.slot;
                item.englishName = d.name;
                item.price = d.price;
                item.thumbnail = AssetDatabase.LoadAssetAtPath<Sprite>(thumbPath);
                item.poses = poses.ToArray();
                item.fullFigure = full;
                EditorUtility.SetDirty(item);
                made.Add(item);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<StoreCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StoreCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.items = made.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        /// <summary>Makes sure the store items exist (cheap when nothing changed).</summary>
        public static StoreCatalog Ensure()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StoreCatalog>(CatalogPath);
            bool ok = catalog != null && catalog.items != null && catalog.items.Length == Defs.Length;
            if (ok) foreach (var i in catalog.items) if (i == null || i.poses.Length == 0) ok = false;
            return ok ? catalog : Build();
        }

        /// <summary>The first version put these clothes on NØX: take them off his rig and delete those generated files.</summary>
        static void RemoveFromNox()
        {
            const string rigPath = BeatFactorySceneBuilder.Root + "/Data/ArtistRigs/nox.asset";
            var rig = AssetDatabase.LoadAssetAtPath<ArtistRig>(rigPath);
            if (rig != null && System.Array.Exists(rig.wardrobe, w => w != null && w.id != null && w.id.StartsWith("store_")))
            {
                rig.wardrobe = System.Array.FindAll(rig.wardrobe, w => w != null && (w.id == null || !w.id.StartsWith("store_")));
                EditorUtility.SetDirty(rig);
            }
            const string wardrobeDir = BeatFactorySceneBuilder.Root + "/Data/Wardrobe/nox";
            foreach (var f in Directory.Exists(wardrobeDir) ? Directory.GetFiles(wardrobeDir, "store_*.asset") : new string[0])
                AssetDatabase.DeleteAsset(f.Replace('\\', '/'));
            const string fitted = BeatFactorySceneBuilder.Root + "/Art/Store/fitted";
            if (AssetDatabase.IsValidFolder(fitted)) AssetDatabase.DeleteAsset(fitted);
            foreach (var f in Directory.Exists(Items) ? Directory.GetFiles(Items, "store_*.png") : new string[0])
                AssetDatabase.DeleteAsset(f.Replace('\\', '/'));
            const string oldCatalog = BeatFactorySceneBuilder.Root + "/Data/Store/StoreCatalog.asset";
            var c = AssetDatabase.LoadAssetAtPath<StoreCatalog>(oldCatalog);
            if (c != null && c.items != null && System.Array.Exists(c.items, i => i == null)) { c.items = new CharacterOutfitItem[0]; EditorUtility.SetDirty(c); }
        }

        // ------------------------------------------------------------------ pixels (rows top-down)

        static Color32[] Load(string path, out int w, out int h)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(File.ReadAllBytes(path));
            w = t.width; h = t.height;
            var px = t.GetPixels32();
            Object.DestroyImmediate(t);
            var o = new Color32[w * h];
            for (int y = 0; y < h; y++) System.Array.Copy(px, (h - 1 - y) * w, o, y * w, w);
            return o;
        }

        static void SavePng(Color32[] topDown, int w, int h, string path)
        {
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++) System.Array.Copy(topDown, y * w, px, (h - 1 - y) * w, w);
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.SetPixels32(px);
            t.Apply();
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
        }

        /// <summary>Background (connected to the border, near white / near black) → transparent; trimmed.</summary>
        static Color32[] Cutout(string path, bool dark, out int cw, out int ch)
        {
            var px = Load(path, out int w, out int h);
            bool Bg(Color32 c)
            {
                int mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), mn = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                return dark ? mx < 34 : (mn > 226 && mx - mn < 22);
            }
            var bg = new bool[w * h];
            var st = new Stack<int>();
            for (int x = 0; x < w; x++) { st.Push(x); st.Push((h - 1) * w + x); }
            for (int y = 0; y < h; y++) { st.Push(y * w); st.Push(y * w + w - 1); }
            while (st.Count > 0)
            {
                int i = st.Pop();
                if (bg[i] || !Bg(px[i])) continue;
                bg[i] = true;
                int x = i % w, y = i / w;
                if (x > 0) st.Push(i - 1); if (x < w - 1) st.Push(i + 1);
                if (y > 0) st.Push(i - w); if (y < h - 1) st.Push(i + w);
            }
            int x0 = w, y0 = h, x1 = -1, y1 = -1;
            for (int i = 0; i < px.Length; i++)
            {
                if (bg[i]) { px[i] = new Color32(0, 0, 0, 0); continue; }
                int x = i % w, y = i / w;
                int n = 0;
                if (x > 0 && bg[i - 1]) n++; if (x < w - 1 && bg[i + 1]) n++;
                if (y > 0 && bg[i - w]) n++; if (y < h - 1 && bg[i + w]) n++;
                if (n > 0) px[i].a = (byte)(n >= 2 ? 120 : 200);
                x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
            }
            cw = x1 - x0 + 1; ch = y1 - y0 + 1;
            var o = new Color32[cw * ch];
            for (int y = 0; y < ch; y++) System.Array.Copy(px, (y0 + y) * w + x0, o, y * cw, cw);
            return o;
        }

        static Color32[] Downscale(Color32[] src, int w, int h, int max, out int nw, out int nh)
        {
            float s = Mathf.Min(1f, max / (float)Mathf.Max(w, h));
            nw = Mathf.Max(1, Mathf.RoundToInt(w * s)); nh = Mathf.Max(1, Mathf.RoundToInt(h * s));
            var o = new Color32[nw * nh];
            for (int y = 0; y < nh; y++)
                for (int x = 0; x < nw; x++)
                    o[y * nw + x] = Sample(src, w, h, (x + 0.5f) / nw, (y + 0.5f) / nh);
            return o;
        }

        static Color32 Sample(Color32[] src, int w, int h, float u, float v)
        {
            float fx = Mathf.Clamp(u * w - 0.5f, 0, w - 1), fy = Mathf.Clamp(v * h - 0.5f, 0, h - 1);
            int ix = (int)fx, iy = (int)fy;
            float tx = fx - ix, ty = fy - iy;
            int ix1 = Mathf.Min(ix + 1, w - 1), iy1 = Mathf.Min(iy + 1, h - 1);
            Color a = src[iy * w + ix], b = src[iy * w + ix1], c = src[iy1 * w + ix], d = src[iy1 * w + ix1];
            return Color.Lerp(Color.Lerp(a, b, tx), Color.Lerp(c, d, tx), ty);
        }

        static float Lum(Color32 c) => (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;

        /// <summary>His clothes: dark neutral cloth (not skin, not purple hair, not white chains / sneakers); on tops also the red print.</summary>
        static bool Cloth(Color32 c, bool top)
        {
            if (c.a < 100) return false;
            int r = c.r, g = c.g, b = c.b;
            float l = Lum(c);
            if (top && r > 120 && g < r * 0.45f && b < r * 0.6f) return true;  // red tee print
            if (top && r > g + 40 && b >= g - 5 && l > 0.3f) return true;     // pink strokes of the print
            if (l > 0.42f) return false;                                      // chains, laces, highlights
            bool skin = r > b + 18 && g > b - 6 && g > r * 0.5f && g < r * 0.88f && l > 0.12f;
            if (skin) return false;
            bool purpleHair = b > g + 22 && r > g + 8 && l > 0.08f;
            return !purpleHair;
        }

        static bool Inside(Vector2 p, Vector2[] poly)
        {
            if (poly == null) return false;
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) inside = !inside;
            return inside;
        }

        const float Feather = 10f;

        static float EdgeDistance(Vector2 p, Vector2[] poly)
        {
            float best = float.MaxValue;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                Vector2 a = poly[j], b = poly[i], ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }

        /// <summary>How much of a pixel belongs to the tee region (soft over Feather px inside its edge).</summary>
        static float TopWeight(Vector2 p, Vector2[] top) => Inside(p, top) ? Mathf.Clamp01(EdgeDistance(p, top) / Feather) : 0f;

        /// <summary>
        /// Weight of a pixel for the item: tops = the tee region (soft edges); pants = everything below the middle of the
        /// tee region down to the pants' lowest line, minus the tee (so tee and pants blend into each other, no seam).
        /// </summary>
        static float Weight(Vector2 p, Regions r, bool top)
        {
            float t = TopWeight(p, r.top);
            if (top) return t;
            float topMid = 0f, bottom = 0f, minY = float.MaxValue, maxY = 0f;
            foreach (var v in r.top) { minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y); }
            foreach (var v in r.pants) bottom = Mathf.Max(bottom, v.y);
            topMid = (minY + maxY) * 0.5f;
            if (p.y < topMid) return 0f;
            float down = Mathf.Clamp01((bottom - p.y) / Feather);
            float up = Mathf.Clamp01((p.y - topMid) / Feather);
            return Mathf.Min(down, up) * (1f - t);
        }

        /// <summary>Refills the cloth pixels of the region with the product (mapped over the region's cloth box), shaded by the pose.</summary>
        static bool Overlay(string posePath, Regions regions, bool top, Color32[] cut, int cw, int ch, string outPath)
        {
            var px = Load(posePath, out int w, out int h);
            var mask = new bool[w * h];
            var weight = new float[w * h];
            int x0 = w, y0 = h, x1 = -1, y1 = -1;
            double lumSum = 0; int n = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float wt = Weight(p, regions, top);
                    if (wt <= 0f) continue;
                    int i = y * w + x;
                    if (!Cloth(px[i], top)) continue;
                    mask[i] = true;
                    weight[i] = wt;
                    x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
                    lumSum += Lum(px[i]); n++;
                }
            if (n < 50) return false;
            float mean = (float)(lumSum / n);

            Color avg = Color.black; int an = 0;
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    var c = Sample(cut, cw, ch, (x + 0.5f) / 32f, (y + 0.5f) / 32f);
                    if (c.a < 128) continue;
                    avg += (Color)c; an++;
                }
            avg = an > 0 ? avg / an : new Color(0.1f, 0.1f, 0.1f);

            var o = new Color32[w * h];
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    int i = y * w + x;
                    if (!mask[i]) continue;
                    float u = (x - x0 + 0.5f) / (x1 - x0 + 1), v = (y - y0 + 0.5f) / (y1 - y0 + 1);
                    Color s = Sample(cut, cw, ch, u, v);
                    if (s.a < 0.5f) s = avg;
                    float l = Lum(px[i]);
                    float shade = Mathf.Clamp(Mathf.Lerp(1f, l / Mathf.Max(0.04f, mean), 0.6f), 0.35f, 1.6f);
                    var res = s * shade;
                    // soft edge where the cloth meets skin / chains, so the overlay doesn't look cut out
                    int edge = 0;
                    if (x > 0 && !mask[i - 1]) edge++; if (x < w - 1 && !mask[i + 1]) edge++;
                    if (y > 0 && !mask[i - w]) edge++; if (y < h - 1 && !mask[i + w]) edge++;
                    res.a = px[i].a / 255f * (edge >= 2 ? 0.55f : edge == 1 ? 0.8f : 1f) * weight[i];
                    o[i] = res;
                }
            SavePng(o, w, h, outPath);
            return true;
        }

        /// <summary>
        /// The whole character in the item, drawn separately: background removed (opaque black → transparent, flood fill
        /// from the border), trimmed and scaled to the height of the original pose so it stands on the same spot.
        /// </summary>
        static void FullFigure(string figPath, string posePath, string outPath)
        {
            var px = Load(figPath, out int w, out int h);
            bool opaqueBg = px[0].a > 250 && px[w * h - 1].a > 250;
            if (opaqueBg)
            {
                var bg = new bool[w * h];
                var st = new Stack<int>();
                for (int x = 0; x < w; x++) { st.Push(x); st.Push((h - 1) * w + x); }
                for (int y = 0; y < h; y++) { st.Push(y * w); st.Push(y * w + w - 1); }
                while (st.Count > 0)
                {
                    int i = st.Pop();
                    if (bg[i]) continue;
                    var c = px[i];
                    if (Mathf.Max(c.r, Mathf.Max(c.g, c.b)) > 0) continue;
                    bg[i] = true;
                    int x = i % w, y = i / w;
                    if (x > 0) st.Push(i - 1); if (x < w - 1) st.Push(i + 1);
                    if (y > 0) st.Push(i - w); if (y < h - 1) st.Push(i + w);
                }
                for (int i = 0; i < px.Length; i++)
                {
                    if (bg[i]) { px[i] = new Color32(0, 0, 0, 0); continue; }
                    int x = i % w, y = i / w, n = 0;
                    if (x > 0 && bg[i - 1]) n++; if (x < w - 1 && bg[i + 1]) n++;
                    if (y > 0 && bg[i - w]) n++; if (y < h - 1 && bg[i + w]) n++;
                    if (n > 0) px[i].a = (byte)(n >= 2 ? 110 : 190);
                }
                // stray specks of near-black background left around the figure: drop small loose pieces
                var seen = new bool[w * h];
                var comp = new List<int>();
                for (int s0 = 0; s0 < px.Length; s0++)
                {
                    if (seen[s0] || px[s0].a == 0) continue;
                    comp.Clear();
                    st.Push(s0);
                    seen[s0] = true;
                    while (st.Count > 0)
                    {
                        int i = st.Pop();
                        comp.Add(i);
                        int x = i % w, y = i / w;
                        for (int dy = -1; dy <= 1; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                                int j = ny * w + nx;
                                if (seen[j] || px[j].a == 0) continue;
                                seen[j] = true;
                                st.Push(j);
                            }
                    }
                    if (comp.Count < 600) foreach (int i in comp) px[i] = new Color32(0, 0, 0, 0);
                }
            }
            int x0 = w, y0 = h, x1 = -1, y1 = -1;
            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].a < 20) continue;
                int x = i % w, y = i / w;
                x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
            }
            int cw = x1 - x0 + 1, ch = y1 - y0 + 1;
            var crop = new Color32[cw * ch];
            for (int y = 0; y < ch; y++) System.Array.Copy(px, (y0 + y) * w + x0, crop, y * cw, cw);
            Load(posePath, out int pw, out int ph);
            float s = ph / (float)ch;
            int nw = Mathf.Max(1, Mathf.RoundToInt(cw * s)), nh = ph;
            var o = new Color32[nw * nh];
            for (int y = 0; y < nh; y++)
                for (int x = 0; x < nw; x++)
                {
                    // box-average when shrinking (the source is ~2-3x bigger) so fur and chains don't alias
                    float fx0 = x / s, fy0 = y / s, step = 1f / s;
                    int sx0 = (int)fx0, sy0 = (int)fy0, sx1 = Mathf.Min(cw - 1, (int)(fx0 + step)), sy1 = Mathf.Min(ch - 1, (int)(fy0 + step));
                    float r = 0, g = 0, b = 0, a = 0; int n = 0;
                    for (int yy = sy0; yy <= sy1; yy++)
                        for (int xx = sx0; xx <= sx1; xx++)
                        {
                            var c = crop[yy * cw + xx];
                            float ca = c.a / 255f;
                            r += c.r * ca; g += c.g * ca; b += c.b * ca; a += ca; n++;
                        }
                    o[y * nw + x] = a > 0.001f ? new Color32((byte)(r / a), (byte)(g / a), (byte)(b / a), (byte)(a / n * 255f)) : new Color32(0, 0, 0, 0);
                }
            SavePng(o, nw, nh, outPath);
        }

        static void ImportSprite(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = 1024;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.SaveAndReimport();
        }
    }
}
