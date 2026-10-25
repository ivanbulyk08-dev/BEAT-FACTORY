using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using static BeatFactory.EditorTools.Art.SdfPainter;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// PLACEHOLDER parts for a detailed artist (simple flat shapes, NOT production art) in exactly the file layout the
    /// real sprites must follow, so the layered rig, the wardrobe and the import pipeline can be tested before the art
    /// arrives. Writes PLACEHOLDER.txt; real PNGs with the same names replace them (then delete PLACEHOLDER.txt).
    /// </summary>
    public static class ArtistRigPlaceholders
    {
        const int CW = 800, CH = 1050;

        // template joints (canvas pixels, bottom-left origin)
        static readonly Dictionary<RigJoint, Vector2> J = new Dictionary<RigJoint, Vector2>
        {
            { RigJoint.Feet, new Vector2(400, 20) }, { RigJoint.Hips, new Vector2(400, 500) }, { RigJoint.Neck, new Vector2(392, 790) },
            { RigJoint.ShoulderNear, new Vector2(330, 760) }, { RigJoint.ElbowNear, new Vector2(322, 610) }, { RigJoint.WristNear, new Vector2(322, 470) },
            { RigJoint.ShoulderFar, new Vector2(462, 764) }, { RigJoint.ElbowFar, new Vector2(470, 612) }, { RigJoint.WristFar, new Vector2(470, 472) },
            { RigJoint.HipNear, new Vector2(362, 500) }, { RigJoint.AnkleNear, new Vector2(352, 70) },
            { RigJoint.HipFar, new Vector2(440, 500) }, { RigJoint.AnkleFar, new Vector2(448, 70) },
        };

        static readonly Color Skin = Hex("4A2C1E"), SkinShade = Hex("3A2217");

        [MenuItem("Tools/Beat Factory/Art/Generate NØX Placeholder Parts")]
        static void Menu()
        {
            Generate("nox");
            Debug.Log(ArtistRigImporter.Import("nox"));
        }

        public static void Generate(string id)
        {
            string folder = ArtistRigImporter.Folder(id);
            Directory.CreateDirectory(folder);
            var rig = new StringBuilder();
            rig.AppendLine("# Detailed artist rig (see Art/Artists/README.md). Canvas = the shared character template.");
            rig.AppendLine($"canvas {CW} {CH}");
            foreach (var j in J) rig.AppendLine($"joint {j.Key} {F(j.Value.x)} {F(j.Value.y)}");
            void Part(string name, System.Action<SdfPainter> draw)
            {
                var p = new SdfPainter(CW, CH);
                draw(p);
                var offset = SaveTrimmed(p, folder + "/" + id + "_" + name + ".png");
                rig.AppendLine($"offset {id}_{name}.png {offset.x} {offset.y}");
            }

            // ---- body
            Part("neck", p => p.Fill(RRect(372, 752, 40, 70, 12), Skin));
            Part("torso_base", p => p.Fill(Polygon(new Vector2(318, 778), new Vector2(472, 782), new Vector2(456, 480), new Vector2(344, 480)), Skin));
            Part("upperarm_near", p => p.Fill(Capsule(J[RigJoint.ShoulderNear], J[RigJoint.ElbowNear], 25), Skin));
            Part("forearm_near", p => p.Fill(Capsule(J[RigJoint.ElbowNear], J[RigJoint.WristNear], 21), Skin));
            Part("upperarm_far", p => p.Fill(Capsule(J[RigJoint.ShoulderFar], J[RigJoint.ElbowFar], 24), SkinShade));
            Part("forearm_far", p => p.Fill(Capsule(J[RigJoint.ElbowFar], J[RigJoint.WristFar], 20), SkinShade));
            Part("hand_far", p => p.Fill(Ellipse(J[RigJoint.WristFar] + new Vector2(0, -30), new Vector2(22, 30)), SkinShade));
            Part("leg_near", p => p.Fill(Capsule(J[RigJoint.HipNear], J[RigJoint.AnkleNear], 30), Skin));
            Part("leg_far", p => p.Fill(Capsule(J[RigJoint.HipFar], J[RigJoint.AnkleFar], 28), SkinShade));
            var wrist = J[RigJoint.WristNear];
            Part("hand_mic", p => p.Fill(Circle(wrist + new Vector2(0, -32), 27), Skin));
            Part("hand_open", p =>
            {
                p.Fill(Ellipse(wrist + new Vector2(0, -30), new Vector2(24, 32)), Skin);
                for (int i = 0; i < 4; i++) p.Fill(Capsule(wrist + new Vector2(-15 + i * 10, -50), wrist + new Vector2(-18 + i * 12, -78), 5), Skin);
            });
            Part("hand_point", p =>
            {
                p.Fill(Circle(wrist + new Vector2(0, -30), 25), Skin);
                p.Fill(Capsule(wrist + new Vector2(-10, -40), wrist + new Vector2(-38, -82), 6), Skin);
            });
            Part("hand_fist", p => p.Fill(Circle(wrist + new Vector2(0, -30), 26), Skin));

            // ---- heads (face only; hair is a wardrobe item)
            foreach (var expr in new[] { "idle", "rap", "smile", "proud", "blink" })
                Part("head_" + expr, p => Head(p, expr));

            // ---- wardrobe (2 per slot)
            Hair("hair_locs_black", Hex("15111A"), Hex("2A1F33"));
            Hair("hair_locs_red", Hex("1A0F12"), Hex("C0172E"));
            Tee("top_tee_cross_black", Hex("121016"), Hex("D8D8E0"));
            Tee("top_tee_skull_white", Hex("E8E6EE"), Hex("1A1820"));
            Jacket("jacket_leather_black", Hex("16141B"), Hex("3A3644"), false);
            Jacket("jacket_puffer_purple", Hex("4E22A8"), Hex("7A4BE0"), true);
            Pants("pants_baggy_black_cargo", Hex("141218"), Hex("2A2632"), false);
            Pants("pants_baggy_denim_ripped", Hex("34507E"), Hex("8AA4CC"), true);
            Shoes("shoes_chunky_black", Hex("15141A"), Hex("E8E6EE"));
            Shoes("shoes_chunky_white", Hex("EDEBF2"), Hex("C0172E"));
            Part("chain_silver_cross_torso", p =>
            {
                var n = J[RigJoint.Neck];
                p.Fill(Arc(n + new Vector2(0, 0), 58, 6, 200, 340), Hex("C9CBD6"));
                p.Fill(RRect(n.x - 5, n.y - 128, 10, 54, 3), Hex("E4E6EE"));
                p.Fill(RRect(n.x - 18, n.y - 106, 36, 10, 3), Hex("E4E6EE"));
            });
            Part("glasses_shades_black_head", p =>
            {
                var c = J[RigJoint.Neck] + new Vector2(0, 98);
                p.Fill(RRect(c.x - 58, c.y - 10, 50, 28, 10), Hex("0B0A0F"));
                p.Fill(RRect(c.x + 6, c.y - 10, 50, 28, 10), Hex("0B0A0F"));
                p.Fill(RRect(c.x - 10, c.y + 8, 20, 5), Hex("0B0A0F"));
            });
            Part("rings_silver_hand", p =>
            {
                p.Fill(Ring(wrist + new Vector2(-12, -44), 6, 3), Hex("D8DAE4"));
                p.Fill(Ring(wrist + new Vector2(6, -46), 6, 3), Hex("D8DAE4"));
            });

            rig.AppendLine("default hair locs_black");
            rig.AppendLine("default top tee_cross_black");
            rig.AppendLine("default jacket leather_black");
            rig.AppendLine("default pants baggy_black_cargo");
            rig.AppendLine("default shoes chunky_black");
            rig.AppendLine("default chain silver_cross");
            rig.AppendLine("default rings silver");
            rig.AppendLine("item hair_locs_black | Black Locs | 0 | 0");
            rig.AppendLine("item hair_locs_red | Red Locs | 2 | 2000000");
            rig.AppendLine("item top_tee_cross_black | Cross Tee | 0 | 0");
            rig.AppendLine("item top_tee_skull_white | Skull Tee | 1 | 500000");
            rig.AppendLine("item jacket_leather_black | Leather Jacket | 0 | 0");
            rig.AppendLine("item jacket_puffer_purple | Purple Puffer | 3 | 30000000");
            rig.AppendLine("item pants_baggy_black_cargo | Black Cargo | 0 | 0");
            rig.AppendLine("item pants_baggy_denim_ripped | Ripped Denim | 1 | 750000");
            rig.AppendLine("item shoes_chunky_black | Chunky Black | 0 | 0");
            rig.AppendLine("item shoes_chunky_white | Chunky White | 2 | 4000000");
            rig.AppendLine("item chain_silver_cross | Silver Cross | 0 | 0");
            rig.AppendLine("item glasses_shades_black | Black Shades | 1 | 300000");
            rig.AppendLine("item rings_silver | Silver Rings | 0 | 0");
            File.WriteAllText(folder + "/rig.txt", rig.ToString());
            File.WriteAllText(folder + "/PLACEHOLDER.txt", "These parts are generated PLACEHOLDERS (flat shapes), not production art.\nReplace the PNGs with the real sprites (same names), then delete this file and run Tools > Beat Factory > Art > Import Artist Rig.\n");
            AssetDatabase.Refresh();

            // ---- local helpers (closures over Part)
            void Hair(string name, Color dark, Color tip)
            {
                var head = J[RigJoint.Neck] + new Vector2(0, 92);
                Part(name + "_back", p =>
                {
                    var rnd = new System.Random(7);
                    for (int i = 0; i < 22; i++)
                    {
                        float a = Mathf.Lerp(-80, 80, i / 21f) * Mathf.Deg2Rad;
                        var top = head + new Vector2(Mathf.Sin(a) * 66, 40 + Mathf.Cos(a) * 46);
                        var end = top + new Vector2(Mathf.Sin(a) * 30, -150 - (float)rnd.NextDouble() * 60);
                        p.Fill(Capsule(top, end, 9), q => Color.Lerp(tip, dark, Mathf.Clamp01((q.y - end.y) / 70f)));
                    }
                });
                Part(name + "_front", p =>
                {
                    p.Fill(Ellipse(head + new Vector2(0, 52), new Vector2(72, 44)), dark);
                    for (int i = 0; i < 5; i++)
                    {
                        var top = head + new Vector2(-50 + i * 25, 60);
                        p.Fill(Capsule(top, top + new Vector2(-8 + i * 4, -70 - (i % 2) * 20), 8), q => Color.Lerp(tip, dark, Mathf.Clamp01((q.y - top.y + 90) / 60f)));
                    }
                });
            }

            void Tee(string name, Color cloth, Color print)
            {
                Part(name + "_torso", p =>
                {
                    p.Fill(Polygon(new Vector2(312, 784), new Vector2(478, 788), new Vector2(462, 470), new Vector2(338, 470)), cloth);
                    p.Fill(RRect(392, 620, 10, 70, 3), print);
                    p.Fill(RRect(374, 660, 46, 10, 3), print);
                });
                Part(name + "_upperarm_near", p => p.Fill(Capsule(J[RigJoint.ShoulderNear], Vector2.Lerp(J[RigJoint.ShoulderNear], J[RigJoint.ElbowNear], 0.45f), 30), cloth));
                Part(name + "_upperarm_far", p => p.Fill(Capsule(J[RigJoint.ShoulderFar], Vector2.Lerp(J[RigJoint.ShoulderFar], J[RigJoint.ElbowFar], 0.45f), 29), cloth * 0.8f + new Color(0, 0, 0, 0.2f)));
            }

            void Jacket(string name, Color cloth, Color edge, bool puffy)
            {
                float w = puffy ? 14 : 0;
                Part(name + "_torso", p =>
                {
                    p.Fill(Polygon(new Vector2(306 - w, 790), new Vector2(386, 790), new Vector2(382, 452), new Vector2(330 - w, 452)), cloth);
                    p.Fill(Polygon(new Vector2(404, 790), new Vector2(486 + w, 794), new Vector2(470 + w, 452), new Vector2(410, 452)), cloth);
                    p.Fill(RRect(380, 452, 8, 336), edge);
                    p.Fill(RRect(404, 452, 8, 336), edge);
                    if (puffy) for (int y = 480; y < 780; y += 60) p.Fill(RRect(300, y, 190, 4), edge);
                });
                Part(name + "_upperarm_near", p => p.Fill(Capsule(J[RigJoint.ShoulderNear], J[RigJoint.ElbowNear], 34 + w), cloth));
                Part(name + "_upperarm_far", p => p.Fill(Capsule(J[RigJoint.ShoulderFar], J[RigJoint.ElbowFar], 33 + w), cloth * 0.85f + new Color(0, 0, 0, 0.15f)));
                Part(name + "_forearm_near", p => p.Fill(Capsule(J[RigJoint.ElbowNear], J[RigJoint.WristNear] + new Vector2(0, 14), 30 + w), cloth));
                Part(name + "_forearm_far", p => p.Fill(Capsule(J[RigJoint.ElbowFar], J[RigJoint.WristFar] + new Vector2(0, 14), 29 + w), cloth * 0.85f + new Color(0, 0, 0, 0.15f)));
                Part(name + "_collar", p =>
                {
                    var n = J[RigJoint.Neck];
                    p.Fill(Polygon(n + new Vector2(-58, -24), n + new Vector2(-14, -10), n + new Vector2(-30, -70)), edge);
                    p.Fill(Polygon(n + new Vector2(58, -20), n + new Vector2(16, -10), n + new Vector2(32, -70)), edge);
                });
            }

            void Pants(string name, Color cloth, Color detail, bool ripped)
            {
                Part(name + "_waist", p => { p.Fill(RRect(334, 452, 132, 64, 10), cloth); p.Fill(RRect(334, 500, 132, 10), detail); });
                foreach (var side in new[] { "near", "far" })
                {
                    var hip = J[side == "near" ? RigJoint.HipNear : RigJoint.HipFar];
                    var ankle = J[side == "near" ? RigJoint.AnkleNear : RigJoint.AnkleFar];
                    var c = side == "near" ? cloth : cloth * 0.85f + new Color(0, 0, 0, 0.15f);
                    Part(name + "_leg_" + side, p =>
                    {
                        p.Fill(Polygon(hip + new Vector2(-40, 10), hip + new Vector2(40, 10), ankle + new Vector2(54, 4), ankle + new Vector2(-54, 4)), c);
                        p.Fill(RRect(ankle.x - 30, ankle.y + 200, 60, 70, 6), detail * 0.9f + new Color(0, 0, 0, 0.1f));
                        if (ripped) { p.Fill(Ellipse(ankle + new Vector2(-6, 290), new Vector2(18, 7)), detail); p.Fill(Ellipse(ankle + new Vector2(10, 150), new Vector2(14, 6)), detail); }
                    });
                }
            }

            void Shoes(string name, Color body, Color sole)
            {
                foreach (var side in new[] { "near", "far" })
                {
                    var ankle = J[side == "near" ? RigJoint.AnkleNear : RigJoint.AnkleFar];
                    Part(name + "_" + side, p =>
                    {
                        p.Fill(RRect(ankle.x - 70, ankle.y - 52, 128, 70, 26), body);
                        p.Fill(RRect(ankle.x - 72, ankle.y - 56, 132, 18, 8), sole);
                    });
                }
            }
        }

        static void Head(SdfPainter p, string expr)
        {
            var c = J[RigJoint.Neck] + new Vector2(0, 92);
            p.Fill(Ellipse(c, new Vector2(60, 76)), Skin);
            p.Fill(Ellipse(c + new Vector2(62, 2), new Vector2(10, 16)), SkinShade); // ear
            var eyeL = c + new Vector2(-24, 10);
            var eyeR = c + new Vector2(20, 10);
            if (expr == "blink")
            {
                p.Fill(RRect(eyeL.x - 11, eyeL.y - 2, 22, 4, 2), Hex("1A1010"));
                p.Fill(RRect(eyeR.x - 11, eyeR.y - 2, 22, 4, 2), Hex("1A1010"));
            }
            else
            {
                float open = expr == "proud" ? 0.6f : 1f;
                foreach (var e in new[] { eyeL, eyeR })
                {
                    p.Fill(Ellipse(e, new Vector2(11, 8 * open)), Hex("F2EEE8"));
                    p.Fill(Circle(e + new Vector2(-2, 0), 5 * open), Hex("1A1010"));
                }
            }
            p.Fill(RRect(eyeL.x - 13, eyeL.y + 14, 26, 5, 2), Hex("120B0A"));
            p.Fill(RRect(eyeR.x - 13, eyeR.y + 14, 26, 5, 2), Hex("120B0A"));
            var m = c + new Vector2(-2, -38);
            switch (expr)
            {
                case "rap": p.Fill(Ellipse(m, new Vector2(14, 10)), Hex("2A0E10")); break;
                case "smile": case "proud": p.Fill(Arc(m + new Vector2(0, 10), 16, 4, 200, 340), Hex("2A0E10")); break;
                default: p.Fill(RRect(m.x - 12, m.y - 2, 24, 4, 2), Hex("2A0E10")); break;
            }
        }

        static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        /// <summary>Saves only the drawn area (trimmed PNG) and returns where it sits on the canvas.</summary>
        static Vector2Int SaveTrimmed(SdfPainter p, string path)
        {
            int x0 = p.W, y0 = p.H, x1 = -1, y1 = -1;
            for (int y = 0; y < p.H; y++)
                for (int x = 0; x < p.W; x++)
                    if (p.GetPixel(x, y).a > 0.003f) { if (x < x0) x0 = x; if (y < y0) y0 = y; if (x > x1) x1 = x; if (y > y1) y1 = y; }
            if (x1 < 0) { x0 = y0 = 0; x1 = y1 = 1; }
            x0 = Mathf.Max(0, x0 - 2); y0 = Mathf.Max(0, y0 - 2);
            x1 = Mathf.Min(p.W - 1, x1 + 2); y1 = Mathf.Min(p.H - 1, y1 + 2);
            var t = new SdfPainter(x1 - x0 + 1, y1 - y0 + 1);
            for (int y = 0; y < t.H; y++)
                for (int x = 0; x < t.W; x++)
                    t.SetPixel(x, y, p.GetPixel(x0 + x, y0 + y));
            t.Save(path);
            return new Vector2Int(x0, y0);
        }
    }
}
