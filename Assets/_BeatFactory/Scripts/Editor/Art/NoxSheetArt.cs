using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// Builds NØX's layered parts from the user's character sheet (Art/Reference/nox_parts_sheet.png, 1536×1024, painted
    /// checkerboard instead of real transparency). The body, jacket, chains, pants and shoes come from the full front figure
    /// (best resolution, coherent proportions; the hanging arms are erased and the jacket/pants behind them filled in),
    /// expressions from the HEADS panel (cut at the chin), arms and hands from BODY PARTS, extra items from the JACKETS,
    /// PANTS and HEAD ACCESSORIES panels. Everything is mapped onto one 800×1050 template (figure × 2.5).
    /// LIMITS: the sheet is low resolution (the figure is ~400 px tall), so parts are upscaled 1.7–3.6× and look soft in
    /// close-ups; hair, top, pants and shoes of the default look are painted into the body (their slots hold empty items).
    /// </summary>
    public static class NoxSheetArt
    {
        const string Sheet = BeatFactorySceneBuilder.Root + "/Art/Reference/nox_parts_sheet.png";
        static string Out => ArtistRigImporter.Folder("nox");
        const float S = 2.5f;                                    // figure → template
        static Vector2 T(float x, float y) => new Vector2((x - 76) * S + 400, (455 - y) * S + 20); // sheet (top-left origin) → template

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        [MenuItem("Tools/Beat Factory/Art/Build NØX From Character Sheet")]
        public static void Menu() => Debug.Log(Build());

        public static string Build()
        {
            var sheet = SheetCutter.LoadSheet(Sheet);
            var log = new StringBuilder("[NØX sheet]\n");
            Directory.CreateDirectory(Out);
            // remove the old placeholder parts (they can be regenerated from the menu)
            foreach (var f in Directory.GetFiles(Out, "*.png")) AssetDatabase.DeleteAsset(f.Replace('\\', '/'));
            if (File.Exists(Out + "/PLACEHOLDER.txt")) File.Delete(Out + "/PLACEHOLDER.txt");

            var rig = new StringBuilder();
            rig.AppendLine("# NØX, cut from Art/Reference/nox_parts_sheet.png by Tools > Beat Factory > Art > Build NØX From Character Sheet");
            rig.AppendLine("canvas 800 1050");
            var joints = new Dictionary<RigJoint, Vector2>();
            var placed = new List<(string file, Vector2 offset, SheetCutter.Cut cut)>();

            // ---- joints: figure landmarks; arms follow the lengths of the arm pieces (so the pieces connect)
            const float armS = 1.7f;
            joints[RigJoint.Feet] = T(76, 455);
            joints[RigJoint.Hips] = T(76, 237);
            joints[RigJoint.Neck] = T(80, 141);
            joints[RigJoint.ShoulderNear] = T(57, 133);
            joints[RigJoint.ElbowNear] = joints[RigJoint.ShoulderNear] + new Vector2(0, -91 * armS);
            joints[RigJoint.WristNear] = joints[RigJoint.ElbowNear] + new Vector2(2, -86 * armS);
            joints[RigJoint.ShoulderFar] = T(127, 135);
            joints[RigJoint.ElbowFar] = joints[RigJoint.ShoulderFar] + new Vector2(0, -91 * armS);
            joints[RigJoint.WristFar] = joints[RigJoint.ElbowFar] + new Vector2(3, -88 * armS);
            joints[RigJoint.HipNear] = T(58, 245);
            joints[RigJoint.AnkleNear] = T(55, 425);
            joints[RigJoint.HipFar] = T(96, 245);
            joints[RigJoint.AnkleFar] = T(118, 425);
            foreach (var j in joints) rig.AppendLine($"joint {j.Key} {F(j.Value.x)} {F(j.Value.y)}");

            // place a cut so that sheet point `anchor` lands on template point `target`
            void Place(string name, SheetCutter.Cut c, float s, Vector2 anchor, Vector2 target)
            {
                if (c == null) { log.AppendLine("EMPTY cut " + name); return; }
                float hOrig = c.h / s;
                var inImage = new Vector2((anchor.x - c.sheetTopLeft.x) * s, (c.sheetTopLeft.y + hOrig - anchor.y) * s);
                var offset = target - inImage;
                string file = "nox_" + name + ".png";
                SheetCutter.Save(c, Out + "/" + file);
                rig.AppendLine($"offset {file} {F(offset.x)} {F(offset.y)}");
                placed.Add((file, offset, c));
            }
            SheetCutter.Cut CutFig(RectInt box, float minX, float maxX, params Vector2[][] erase) => Fill(Erase(SheetCutter.CutOut(sheet, "fig", box, 1f), erase), S, minX, maxX);

            // ---- body from the front figure (jacket, chains, pants and shoes painted in)
            var torso = CutFig(new RectInt(22, 112, 126, 138), 52, 120,
                Poly((24, 150), (64, 140), (66, 250), (24, 250)),               // near arm
                Poly((112, 145), (152, 150), (152, 250), (108, 250)),           // far arm
                Poly((62, 108), (104, 108), (104, 141), (62, 141)));            // face (the head layer draws it)
            Place("torso_base", torso, S, new Vector2(76, 237), T(76, 237));
            var legNear = CutFig(new RectInt(14, 236, 66, 220), 34, 66, Poly((10, 236), (64, 236), (64, 294), (10, 294)));
            Place("leg_near", legNear, S, new Vector2(58, 245), T(58, 245));
            var legFar = CutFig(new RectInt(72, 236, 78, 220), 104, 126, Poly((106, 236), (154, 236), (154, 298), (106, 298)));
            Place("leg_far", legFar, S, new Vector2(96, 245), T(96, 245));

            // ---- expressions (HEADS panel, cut at the chin), scale = figure face size
            const float headS = 2.0f;
            (string name, RectInt box, Vector2 chin)[] heads =
            {
                ("idle", new RectInt(549, 48, 104, 101), new Vector2(615, 144)),
                ("rap", new RectInt(662, 48, 107, 101), new Vector2(710, 145)),
                ("smile", new RectInt(778, 48, 107, 103), new Vector2(835, 147)),
                ("proud", new RectInt(894, 48, 99, 99), new Vector2(940, 142)),
                ("blink", new RectInt(549, 210, 110, 96), new Vector2(610, 302)),
                ("hype", new RectInt(780, 210, 135, 103), new Vector2(850, 309)),
            };
            foreach (var h in heads)
                Place("head_" + h.name, CleanHead(SheetCutter.CutOut(sheet, h.name, h.box, headS), h.chin, headS), headS, h.chin, joints[RigJoint.Neck]);

            // ---- arms and hands (BODY PARTS panel)
            Place("upperarm_near", SheetCutter.CutOut(sheet, "uan", new RectInt(238, 518, 40, 108), armS), armS, new Vector2(257, 524), joints[RigJoint.ShoulderNear]);
            Place("forearm_near", SheetCutter.CutOut(sheet, "fan", new RectInt(370, 518, 40, 108), armS), armS, new Vector2(390, 526), joints[RigJoint.ElbowNear]);
            Place("upperarm_far", SheetCutter.CutOut(sheet, "uaf", new RectInt(305, 518, 42, 108), armS), armS, new Vector2(326, 524), joints[RigJoint.ShoulderFar]);
            Place("forearm_far", SheetCutter.CutOut(sheet, "faf", new RectInt(433, 518, 44, 108), armS), armS, new Vector2(452, 524), joints[RigJoint.ElbowFar]);
            Place("hand_mic", SheetCutter.CutOut(sheet, "hm", new RectInt(12, 684, 60, 70), armS), armS, new Vector2(52, 692), joints[RigJoint.WristNear]);
            Place("hand_open", SheetCutter.CutOut(sheet, "ho", new RectInt(72, 684, 52, 78), armS), armS, new Vector2(100, 697), joints[RigJoint.WristNear]);
            Place("hand_point", SheetCutter.CutOut(sheet, "hp", new RectInt(130, 684, 54, 78), armS), armS, new Vector2(170, 692), joints[RigJoint.WristNear]);
            Place("hand_fist", SheetCutter.CutOut(sheet, "hf", new RectInt(195, 690, 54, 64), armS), armS, new Vector2(232, 702), joints[RigJoint.WristNear]);
            Place("hand_far", SheetCutter.CutOut(sheet, "hfar", new RectInt(262, 692, 46, 66), armS), armS, new Vector2(285, 702), joints[RigJoint.WristFar]);

            // ---- default outfit is painted into the body: these slots hold empty (transparent) items
            foreach (var empty in new[] { "neck", "hair_baked_back", "hair_baked_front", "top_baked_torso", "pants_baked_waist",
                         "pants_baked_leg_near", "pants_baked_leg_far", "shoes_baked_near", "shoes_baked_far" })
                SheetCutter.Save(new SheetCutter.Cut { name = empty, w = 4, h = 4, px = new Color32[16] }, Out + "/nox_" + empty + ".png");

            // ---- DRIP STUDIO extras
            // purple fur jacket: body + sleeves split at the elbow
            const float jS = 1.7f;
            var jacketBox = new RectInt(744, 426, 112, 168);
            var jacketTorso = SheetCutter.CutOut(sheet, "jt", jacketBox, 1f);
            jacketTorso = Erase(jacketTorso, Poly((740, 456), (771, 456), (771, 600), (740, 600)), Poly((829, 456), (860, 456), (860, 600), (829, 600)));
            for (int i = 0; i < jacketTorso.px.Length; i++) if (jacketTorso.px[i].a == 1) jacketTorso.px[i] = new Color32(0, 0, 0, 0);
            Place("jacket_fur_purple_torso", SheetCutter.Scale(jacketTorso, 2.45f), 2.45f, new Vector2(799, 445), joints[RigJoint.Neck]);
            Place("jacket_fur_purple_upperarm_near", SheetCutter.CutOut(sheet, "jun", new RectInt(742, 450, 37, 72), jS), jS, new Vector2(762, 456), joints[RigJoint.ShoulderNear]);
            Place("jacket_fur_purple_forearm_near", SheetCutter.CutOut(sheet, "jfn", new RectInt(742, 518, 37, 76), jS), jS, new Vector2(760, 521), joints[RigJoint.ElbowNear]);
            Place("jacket_fur_purple_upperarm_far", SheetCutter.CutOut(sheet, "juf", new RectInt(816, 450, 40, 72), jS), jS, new Vector2(835, 456), joints[RigJoint.ShoulderFar]);
            Place("jacket_fur_purple_forearm_far", SheetCutter.CutOut(sheet, "jff", new RectInt(816, 518, 40, 76), jS), jS, new Vector2(838, 521), joints[RigJoint.ElbowFar]);

            // ripped denim (with its sneakers), split into legs
            const float pS = 3.6f;
            Place("pants_denim_ripped_waist", SheetCutter.Scale(SheetCutter.CutOut(sheet, "pw", new RectInt(640, 646, 72, 16), 1f), pS), pS, new Vector2(676, 650), joints[RigJoint.Hips]);
            Place("pants_denim_ripped_leg_near", SheetCutter.CutOut(sheet, "pln", new RectInt(638, 650, 40, 150), pS), pS, new Vector2(657, 654), joints[RigJoint.HipNear]);
            Place("pants_denim_ripped_leg_far", SheetCutter.CutOut(sheet, "plf", new RectInt(674, 650, 40, 150), pS), pS, new Vector2(695, 654), joints[RigJoint.HipFar]);
            foreach (var side in new[] { "near", "far" })
                SheetCutter.Save(new SheetCutter.Cut { name = side, w = 4, h = 4, px = new Color32[16] }, Out + "/nox_shoes_with_denim_" + side + ".png");

            // shades (black, purple)
            var eyes = joints[RigJoint.Neck] + new Vector2(-10, 66);
            Place("glasses_shades_black_head", SheetCutter.CutOut(sheet, "gb", new RectInt(1238, 214, 94, 48), 1.2f), 1.2f, new Vector2(1285, 238), eyes);
            Place("glasses_shades_purple_head", SheetCutter.CutOut(sheet, "gp", new RectInt(1340, 214, 92, 48), 1.2f), 1.2f, new Vector2(1386, 238), eyes);

            rig.AppendLine("default hair baked");
            rig.AppendLine("default top baked");
            rig.AppendLine("default pants baked");
            rig.AppendLine("default shoes baked");
            rig.AppendLine("item hair_baked | Signature Locs | 0 | 0");
            rig.AppendLine("item top_baked | Black Tee | 0 | 0");
            rig.AppendLine("item pants_baked | Black Cargo + Chunky Sneakers | 0 | 0");
            rig.AppendLine("item shoes_baked | (with the pants) | 0 | 0");
            rig.AppendLine("item shoes_with_denim | (with the pants) | 0 | 0");
            rig.AppendLine("item jacket_fur_purple | Purple Fur Jacket | 3 | 30000000");
            rig.AppendLine("item pants_denim_ripped | Ripped Denim | 2 | 4000000");
            rig.AppendLine("item glasses_shades_black | Black Shades | 1 | 300000");
            rig.AppendLine("item glasses_shades_purple | Purple Shades | 4 | 1500000000");
            File.WriteAllText(Out + "/rig.txt", rig.ToString());
            File.WriteAllText(Out + "/SOURCE.txt", "Cut from Art/Reference/nox_parts_sheet.png (low resolution: upscaled 1.7-3.6x).\nReplace with production PNGs (same names) for full quality, then run Import Artist Rig.\n");
            AssetDatabase.Refresh();

            // debug: the default look assembled in the rest pose
            Composite(placed.Where(p => !p.file.Contains("jacket_") && !p.file.Contains("pants_denim") && !p.file.Contains("glasses")
                && !p.file.Contains("head_") || p.file == "nox_head_idle.png").ToList(), joints, Out + "/../nox_rest_pose_preview.png");
            log.AppendLine($"{placed.Count} parts cut; preview: Art/Artists/nox_rest_pose_preview.png");
            Object.DestroyImmediate(sheet);
            return log.ToString();
        }

        /// <summary>
        /// The HEADS panel shows portraits with shoulders: keep only the face, hair and neck column near the chin, and fade
        /// the bottom edge so the head blends into the collar of the body instead of showing a cut line.
        /// </summary>
        static SheetCutter.Cut CleanHead(SheetCutter.Cut c, Vector2 chin, float s)
        {
            float hOrig = c.h / s;
            float chinX = (chin.x - c.sheetTopLeft.x) * s;
            float chinY = (c.sheetTopLeft.y + hOrig - chin.y) * s; // from the bottom
            for (int y = 0; y < c.h; y++)
                for (int x = 0; x < c.w; x++)
                {
                    int i = y * c.w + x;
                    var p = c.px[i];
                    if (p.a == 0) continue;
                    float above = (y - chinY) / s;        // source px above the chin
                    float side = Mathf.Abs(x - chinX) / s;
                    // shoulders: below eye level and far from the face column
                    float allowed = above > 22 ? 999 : Mathf.Lerp(20, 34, Mathf.Clamp01((above + 4) / 26f));
                    float a = Mathf.Clamp01((allowed - side) / 4f);
                    a *= Mathf.Clamp01((y / s) / 6f);       // soft bottom edge
                    c.px[i].a = (byte)(p.a * a);
                }
            return c;
        }

        static string F(float v) => v.ToString("0.#", Inv);
        static Vector2[] Poly(params (float x, float y)[] pts) => pts.Select(p => new Vector2(p.x, p.y)).ToArray();

        static bool Inside(Vector2 p, Vector2[] poly)
        {
            bool c = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) c = !c;
            return c;
        }

        /// <summary>Marks sheet-space polygons as holes (alpha 1 = "to be filled"); returns the same cut.</summary>
        static SheetCutter.Cut Erase(SheetCutter.Cut c, params Vector2[][] polys)
        {
            if (c == null) return null;
            for (int y = 0; y < c.h; y++)
                for (int x = 0; x < c.w; x++)
                {
                    var sheetP = new Vector2(c.sheetTopLeft.x + x + 0.5f, c.sheetTopLeft.y + (c.h - 1 - y) + 0.5f);
                    foreach (var poly in polys)
                        if (Inside(sheetP, poly)) { c.px[y * c.w + x] = new Color32(0, 0, 0, 1); break; }
                }
            return c;
        }

        /// <summary>
        /// Fills the holes left by erased arms/hands with the clothing next to them (row by row, from the nearest kept pixel
        /// toward the body centre), removes holes outside the clothing, then scales.
        /// </summary>
        static SheetCutter.Cut Fill(SheetCutter.Cut c, float scale, float minX, float maxX)
        {
            if (c == null) return null;
            float centre = (minX + maxX) * 0.5f;
            for (int y = 0; y < c.h; y++)
                for (int x = 0; x < c.w; x++)
                {
                    int i = y * c.w + x;
                    if (c.px[i].a != 1) continue;
                    float sx = c.sheetTopLeft.x + x + 0.5f;
                    c.px[i] = new Color32(0, 0, 0, 0);
                    if (sx < minX || sx > maxX) continue; // outside the clothing: it was background behind the arm
                    int step = sx < centre ? 1 : -1, dist = 0;
                    for (int k = x + step; k >= 0 && k < c.w; k += step)
                    {
                        dist++;
                        var p = c.px[y * c.w + k];
                        if (p.a > 200)
                        {
                            // mirror the cloth next to the hole (keeps its texture instead of smearing one pixel)
                            int m = Mathf.Clamp(k + step * dist, 0, c.w - 1);
                            var q = c.px[y * c.w + m];
                            if (q.a < 200) q = p;
                            float dark = Mathf.Clamp01(0.95f - dist * 0.004f);
                            c.px[i] = new Color32((byte)(q.r * dark), (byte)(q.g * dark), (byte)(q.b * dark), 255);
                            break;
                        }
                    }
                }
            return Mathf.Approximately(scale, 1f) ? c : SheetCutter.Scale(c, scale);
        }

        /// <summary>Rest-pose preview of the parts on the template (for checking the alignment by eye).</summary>
        static void Composite(List<(string file, Vector2 offset, SheetCutter.Cut cut)> parts, Dictionary<RigJoint, Vector2> joints, string path)
        {
            const int W = 800, H = 1050;
            var px = new Color[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(0.16f, 0.36f, 0.2f, 1);
            string[] order = { "leg_far", "upperarm_far", "forearm_far", "hand_far", "leg_near", "torso_base", "head_idle", "upperarm_near", "forearm_near", "hand_mic" };
            foreach (var key in order)
            {
                var p = parts.FirstOrDefault(x => x.file == "nox_" + key + ".png");
                if (p.cut == null) continue;
                for (int y = 0; y < p.cut.h; y++)
                    for (int x = 0; x < p.cut.w; x++)
                    {
                        int tx = Mathf.RoundToInt(p.offset.x) + x, ty = Mathf.RoundToInt(p.offset.y) + y;
                        if (tx < 0 || ty < 0 || tx >= W || ty >= H) continue;
                        var s = (Color)p.cut.px[y * p.cut.w + x];
                        var d = px[ty * W + tx];
                        px[ty * W + tx] = Color.Lerp(d, new Color(s.r, s.g, s.b, 1), s.a);
                    }
            }
            foreach (var j in joints.Values)
                for (int dy = -3; dy <= 3; dy++)
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        int x = Mathf.RoundToInt(j.x) + dx, y = Mathf.RoundToInt(j.y) + dy;
                        if (x >= 0 && y >= 0 && x < W && y < H) px[y * W + x] = Color.red;
                    }
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            t.SetPixels(px);
            t.Apply();
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
        }
    }
}
