using System;
using UnityEngine;
using static BeatFactory.EditorTools.Art.SdfPainter;
using Sdf = System.Func<UnityEngine.Vector2, float>;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// Stage performers and the concert crowd.
    /// Performer parts are drawn from the SAME Look as the artist's portrait (DrawHead / Body / Arm), so the face, skin,
    /// hair, tattoos and clothes on stage match the contract card exactly. Only the standing legs are new (portraits are
    /// cropped at the chest); they are derived deterministically from the artist's id and outfit.
    /// </summary>
    public static partial class ArtGenerator
    {
        public static readonly string[] StageExpressions = { "idle", "excited", "smile", "proud", "blink" };
        public const string CrowdAtlasPath = VenueArt + "/crowd_atlas.png";
        public const string MicPath = Fx + "/fx_mic.png";

        public static string PerformerPath(string id, string part) => $"{Chars}/Artists/Stage/{id}_{part}.png";

        static void GeneratePerformers(bool overwrite)
        {
            foreach (var a in Roster)
            {
                var spec = a;
                var look = StageLook(spec);
                foreach (var e in StageExpressions)
                {
                    var expr = e;
                    Make(PerformerPath(spec.id, "head_" + expr), overwrite, () => StageHead(look, expr));
                }
                Make(PerformerPath(spec.id, "torso"), overwrite, () => StageTorso(look));
                Make(PerformerPath(spec.id, "arm"), overwrite, () => Arm(look));
                Make(PerformerPath(spec.id, "leg"), overwrite, () => StageLeg(look));
            }
            Make(MicPath, overwrite, Mic);
            Make(LaserCorePath, overwrite, () => Laser(8, 1.2f, 0.35f));
            Make(LaserGlowPath, overwrite, () => Laser(32, 7f, 0.12f));
            Make(FlamePath, overwrite, Flame);
            Make(SparkPath, overwrite, Spark);
            Make(CrowdAtlasPath, overwrite, CrowdAtlasArt);
        }

        /// <summary>The artist's Look completed for a full body (pants/shoes are not visible on portraits).</summary>
        public static Look StageLook(ArtistSpec a)
        {
            var l = a.look.Clone();
            int h = Math.Abs(a.id.GetHashCode());
            if (l.pants.a <= 0)
            {
                string[] pants = { "16151C", "2A2834", "2E3A52", "3A3646", "4A4F2E", "C9B79A", "1E1B2A" };
                l.pantsStyle = (PantsStyle)(h % 4);
                l.pants = Hex(pants[(h / 4) % pants.Length]);
                l.pantsShade = Color.Lerp(l.pants, Color.black, 0.3f);
            }
            if (l.shoe.a <= 0)
            {
                l.sneaker = (SneakerStyle)((h / 7) % 4);
                string[] shoes = { "F4F4F6", "16161A", "F4F4F6", "E8E2D6" };
                l.shoe = Hex(shoes[(h / 11) % shoes.Length]);
                l.shoeAccent = l.accent;
                l.sole = l.shoe.r < 0.3f ? Hex("F4F4F6") : Hex("F4F4F6");
            }
            return l;
        }

        /// <summary>Same head as the portrait (DrawHead with the same Look), at 2× resolution for close-ups.</summary>
        static SdfPainter StageHead(Look c, string expr)
        {
            var p = new SdfPainter(320, 320);
            DrawHead(p, c, expr, new Tf(Vector2.zero, 2f));
            return p;
        }

        static void BlendOver(SdfPainter dst, SdfPainter src)
        {
            for (int y = 0; y < src.H; y++)
            for (int x = 0; x < src.W; x++)
            {
                var s = src.GetPixel(x, y);
                if (s.a <= 0) continue;
                var d = dst.GetPixel(x, y);
                float a = s.a, outA = a + d.a * (1 - a);
                dst.SetPixel(x, y, outA <= 0 ? Color.clear : new Color(
                    (s.r * a + d.r * d.a * (1 - a)) / outA, (s.g * a + d.g * d.a * (1 - a)) / outA, (s.b * a + d.b * d.a * (1 - a)) / outA, outA));
            }
        }

        /// <summary>Upper body (same top, chains, tattoos as the seated studio body) with the neck; hip at (115, 64).</summary>
        static SdfPainter StageTorso(Look c)
        {
            var p = new SdfPainter(200, 190);
            p.Fill(Capsule(new Vector2(100, 146), new Vector2(98, 178), 12), c.skin);
            p.Fill(Intersect(Capsule(new Vector2(100, 146), new Vector2(98, 178), 12), RRect(102, 140, 20, 40)), new Color(c.skinShade.r, c.skinShade.g, c.skinShade.b, 0.7f));
            // belt line / hips so the torso meets the legs
            p.Fill(RRect(90, 50, 52, 24, 10), c.pantsShade);
            BlendOver(p, Body(c, BodyPart.Top | BodyPart.Chains | BodyPart.NeckTattoo));
            return p;
        }

        /// <summary>Standing leg (pants + sneaker) 60×150, hip at the top center (30, 146).</summary>
        static SdfPainter StageLeg(Look c)
        {
            var p = new SdfPainter(60, 150);
            Color col = c.pants, shade = c.pantsShade;
            float r = c.pantsStyle == PantsStyle.Wide ? 15 : c.pantsStyle == PantsStyle.Jogger ? 12 : 13;
            p.Fill(Capsule(new Vector2(30, 140), new Vector2(31, 30), r), VGradient(30, shade, 140, col));
            p.Fill(Intersect(Capsule(new Vector2(30, 140), new Vector2(31, 30), r), RRect(33, 0, 30, 150)), new Color(shade.r, shade.g, shade.b, 0.6f));
            p.Fill(Capsule(new Vector2(26, 86), new Vector2(29, 80), 1.1f), new Color(0, 0, 0, 0.25f)); // knee fold
            switch (c.pantsStyle)
            {
                case PantsStyle.Jogger:
                    p.Fill(RRect(19, 26, 24, 9, 4), shade);
                    p.Fill(Capsule(new Vector2(22, 130), new Vector2(23, 40), 1.4f), new Color(c.accent.r, c.accent.g, c.accent.b, 0.85f));
                    break;
                case PantsStyle.Denim:
                    p.Fill(Ellipse(new Vector2(28, 84), new Vector2(8, 10)), new Color(1, 1, 1, 0.12f));
                    p.Fill(RRect(17, 26, 28, 8, 3), Color.Lerp(col, Color.white, 0.25f));
                    break;
                case PantsStyle.Cargo:
                    p.Fill(RRect(32, 70, 14, 20, 3), shade);
                    p.Fill(RRect(18, 26, 26, 7, 3), shade);
                    break;
                case PantsStyle.Wide:
                    p.Fill(Polygon(new Vector2(14, 50), new Vector2(46, 50), new Vector2(50, 18), new Vector2(10, 18)), col);
                    break;
            }
            Sneaker(p, c, 4, 0, 48, far: false);
            if (c.pantsStyle == PantsStyle.Wide) p.Fill(Polygon(new Vector2(14, 44), new Vector2(46, 44), new Vector2(48, 22), new Vector2(12, 22)), col);
            return p;
        }

        public const string LaserCorePath = Fx + "/fx_laser_core.png";
        public const string LaserGlowPath = Fx + "/fx_laser_glow.png";
        public const string FlamePath = Fx + "/fx_flame.png";
        public const string SparkPath = Fx + "/fx_spark.png";

        /// <summary>Laser beam (origin at the bottom): bright center, gaussian halo, fades with distance (perspective).</summary>
        static SdfPainter Laser(int w, float sigma, float farAlpha)
        {
            var p = new SdfPainter(w, 512);
            for (int y = 0; y < 512; y++)
            {
                float along = Mathf.Lerp(1f, farAlpha, Mathf.Pow(y / 511f, 0.8f));
                for (int x = 0; x < w; x++)
                {
                    float d = (x + 0.5f - w * 0.5f) / sigma;
                    float a = Mathf.Exp(-d * d * 0.5f) * along;
                    float core = Mathf.Exp(-d * d * 4f);
                    p.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Max(a, core * along))));
                }
            }
            return p;
        }

        /// <summary>Stage flame (pyro cannon burst): white-hot base → yellow → orange → red tips, soft edges.</summary>
        static SdfPainter Flame()
        {
            var p = new SdfPainter(64, 192);
            var rnd = new System.Random(5);
            for (int y = 0; y < 192; y++)
            for (int x = 0; x < 64; x++)
            {
                float v = y / 191f;
                float width = 26f * Mathf.Sin(Mathf.Clamp01(v * 1.1f + 0.08f) * Mathf.PI) * (1f - v * 0.55f) + 2f;
                float wob = 4f * Mathf.Sin(v * 14f) * v;
                float d = Mathf.Abs(x + 0.5f - 32f - wob) / Mathf.Max(1f, width);
                if (d > 1f) continue;
                float edge = 1f - d;
                float a = Mathf.Clamp01(edge * 1.6f) * Mathf.Clamp01((1f - v) * 1.4f);
                var c = v < 0.18f ? Color.Lerp(Hex("FFFBE8"), Hex("FFE066"), v / 0.18f)
                    : v < 0.5f ? Color.Lerp(Hex("FFE066"), Hex("FF8A1E"), (v - 0.18f) / 0.32f)
                    : Color.Lerp(Hex("FF8A1E"), Hex("C2181E"), (v - 0.5f) / 0.5f);
                c = Color.Lerp(c, Hex("FFFBE8"), Mathf.Clamp01(edge - 0.6f) * (1f - v));
                c.a = a;
                p.SetPixel(x, y, c);
            }
            return p;
        }

        static SdfPainter Spark()
        {
            var p = new SdfPainter(16, 16);
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float d = new Vector2(x - 7.5f, y - 7.5f).magnitude / 7.5f;
                float a = Mathf.Clamp01(1f - d);
                p.SetPixel(x, y, new Color(1f, 0.95f, 0.8f, a * a));
            }
            return p;
        }

        static SdfPainter Mic()
        {
            var p = new SdfPainter(24, 64);
            p.Fill(Capsule(new Vector2(12, 4), new Vector2(12, 40), 4.5f), Hex("1A1A22"));
            p.Fill(Capsule(new Vector2(10.5f, 8), new Vector2(10.5f, 38), 1.2f), new Color(1, 1, 1, 0.15f));
            p.Fill(Circle(new Vector2(12, 50), 9.5f), Hex("C9CCD6"));
            for (int i = -2; i <= 2; i++) p.Fill(Capsule(new Vector2(5, 50 + i * 3.4f), new Vector2(19, 50 + i * 3.4f), 0.6f), Hex("6A6E7A"));
            p.Fill(RRect(6, 38, 12, 4, 1), Hex("2A2A33"));
            return p;
        }

        // ================================================================== CROWD ATLAS (white parts, tinted per fan)

        static SdfPainter CrowdAtlasArt()
        {
            var p = new SdfPainter(CrowdAtlas.Width, CrowdAtlas.Height);
            const float C = CrowdAtlas.Cell;
            Color W(float v, float a = 1f) => new Color(v, v, v, a);
            Vector2 O(int col, int row) => new Vector2(col * C, row * C);
            void Sh(int col, int row, Sdf s, Func<Vector2, Color> color)
            {
                var o = O(col, row);
                p.Fill(q => s(q - o), q => color(q - o), new RectInt((int)o.x, (int)o.y, (int)C, (int)C));
            }
            void S(int col, int row, Sdf s, Color color) => Sh(col, row, s, _ => color);
            // soft side shading: lighter in the middle, darker at the edges (reads as volume after tinting)
            Func<Vector2, Color> Vol(float cx, float half, float top = 64) => q =>
            {
                float k = 1f - Mathf.Clamp01(Mathf.Abs(q.x - cx) / half) * 0.35f;
                k *= Mathf.Lerp(0.82f, 1f, Mathf.Clamp01(q.y / top));
                return W(k);
            };

            // ---- torsos (back view): shoulders at y≈47, neck (32,54)
            Sdf Shoulders(float half, float waist) => Polygon(new Vector2(32 - waist, 0), new Vector2(32 + waist, 0), new Vector2(32 + half, 40), new Vector2(32 + half - 6, 50), new Vector2(32 - half + 6, 50), new Vector2(32 - half, 40));
            Sdf Round(Sdf s, float r) => q => s(q) - r;
            // 0 hoodie with the hood lying on the back
            S(0, 0, Round(Shoulders(22, 18), 3), Color.white);
            Sh(0, 0, Round(Shoulders(22, 18), 3), Vol(32, 26));
            Sh(0, 0, Ellipse(new Vector2(32, 44), new Vector2(11, 8)), _ => W(0.8f));
            S(0, 0, Arc(new Vector2(32, 46), 10, 2, 190, 350), W(0.62f));
            // 1 tee: slimmer, short sleeves
            Sh(1, 0, Union(Round(Shoulders(19, 16), 2), Union(Circle(new Vector2(13, 42), 6), Circle(new Vector2(51, 42), 6))), Vol(32, 24));
            S(1, 0, Capsule(new Vector2(14, 37), new Vector2(18, 35), 1f), W(0.7f));
            S(1, 0, Capsule(new Vector2(50, 37), new Vector2(46, 35), 1f), W(0.7f));
            // 2 jacket: wide, center seam, collar
            Sh(2, 0, Round(Shoulders(23, 20), 2), Vol(32, 27));
            S(2, 0, Capsule(new Vector2(32, 2), new Vector2(32, 44), 0.8f), W(0.62f));
            S(2, 0, RRect(22, 46, 20, 6, 3), W(0.75f));
            // 3 slim top / tank (narrow shoulders, waist)
            Sh(3, 0, Round(Polygon(new Vector2(20, 0), new Vector2(44, 0), new Vector2(42, 22), new Vector2(47, 42), new Vector2(42, 50), new Vector2(22, 50), new Vector2(17, 42), new Vector2(22, 22)), 2), Vol(32, 18));
            // 4 puffer: big with quilting lines
            Sh(4, 0, Round(Shoulders(25, 22), 4), Vol(32, 29));
            for (int i = 0; i < 4; i++) S(4, 0, Intersect(Capsule(new Vector2(4, 10 + i * 11), new Vector2(60, 10 + i * 11), 0.9f), Round(Shoulders(25, 22), 4)), W(0.6f));
            // 5 oversized tee with a back print
            Sh(5, 0, Round(Shoulders(23, 21), 2), Vol(32, 27));
            S(5, 0, RRect(23, 16, 18, 18, 3), W(0.55f));
            S(5, 0, Circle(new Vector2(32, 25), 5), W(0.9f));

            // ---- hair (back view) around the head center (32,30), r 15
            Vector2 hc = CrowdAtlas.HeadCenter;
            Sdf Cap(float r, float cut) => Intersect(Circle(hc, r), q => (hc.y - cut) - q.y);
            Func<Vector2, Color> HairShade = q => W(Mathf.Lerp(0.75f, 1f, Mathf.Clamp01((q.y - 10) / 40f)));
            Sh(0, 1, Cap(16, 8), HairShade);                                                                     // short crop
            Sh(1, 1, Circle(hc + new Vector2(0, 3), 20), HairShade);                                             // afro
            Sh(2, 1, Union(Cap(16.5f, 4), RRect(16, 2, 32, 30, 8)), HairShade);                                  // long straight
            Sh(3, 1, Union(Cap(16, 6), Circle(hc + new Vector2(0, 19), 7)), HairShade);                          // bun
            Sh(4, 1, Cap(16.5f, 6), HairShade);                                                                  // locs / braids
            for (int i = 0; i < 6; i++) Sh(4, 1, Capsule(new Vector2(19 + i * 5.2f, 26), new Vector2(18 + i * 5.6f, 4), 2.3f), HairShade);
            Sh(5, 1, Union(Cap(16, 6), Capsule(hc + new Vector2(0, 10), new Vector2(36, 4), 4.5f)), HairShade); // ponytail
            Sh(6, 1, Cap(15.5f, 2), q => W(0.55f, 0.75f));                                                        // buzz cut
            Sh(7, 1, Union(Cap(16, 6), Union(Circle(hc + new Vector2(-8, 12), 7), Union(Circle(hc + new Vector2(0, 15), 7), Circle(hc + new Vector2(8, 12), 7)))), HairShade); // curly top
            Sh(8, 1, Union(Cap(16.5f, 4), RRect(15, 14, 34, 18, 6)), HairShade);                                 // bob
            Sh(9, 1, Union(Cap(16, 6), Polygon(hc + new Vector2(-14, 8), hc + new Vector2(-8, 22), hc + new Vector2(-2, 12), hc + new Vector2(4, 24), hc + new Vector2(9, 12), hc + new Vector2(15, 20), hc + new Vector2(15, 6))), HairShade); // spiky
            // bare head (back) with ears + neck; head turned (side face)
            Sdf headBack = Union(Union(Circle(hc, CrowdAtlas.HeadRadius), Union(Circle(hc + new Vector2(-15, -1), 4), Circle(hc + new Vector2(15, -1), 4))), Capsule(new Vector2(32, 14), new Vector2(32, 0), 7));
            Sh(CrowdAtlas.HeadBackCol, 1, headBack, Vol(32, 18, 50));
            Sdf headSide = Union(Union(Circle(hc, CrowdAtlas.HeadRadius), Circle(hc + new Vector2(-15, -2), 3.5f)), Capsule(new Vector2(34, 14), new Vector2(34, 0), 7));
            Sh(CrowdAtlas.HeadSideCol, 1, headSide, Vol(34, 18, 50));
            S(CrowdAtlas.HeadSideCol, 1, Circle(hc + new Vector2(-9, 3), 1.6f), W(0.15f));                     // eye
            S(CrowdAtlas.HeadSideCol, 1, Capsule(hc + new Vector2(-12, -7), hc + new Vector2(-7, -8), 0.9f), W(0.35f)); // open mouth (singing)
            S(CrowdAtlas.HeadSideCol, 1, Cap(16, 4), W(0.25f));                                                 // dark hair on the turned head
            // snapback (back view) + beanie
            Sh(CrowdAtlas.CapCol, 1, Intersect(Circle(hc + new Vector2(0, 2), 16.5f), q => (hc.y - 2) - q.y), Vol(32, 18));
            S(CrowdAtlas.CapCol, 1, RRect(26, hc.y - 3, 12, 5, 2), W(0.3f));
            Sh(CrowdAtlas.BeanieCol, 1, Intersect(Circle(hc + new Vector2(0, 3), 17), q => (hc.y - 4) - q.y), Vol(32, 18));
            S(CrowdAtlas.BeanieCol, 1, RRect(15, hc.y - 6, 34, 6, 3), W(0.75f));

            // ---- props
            Sdf arm = Union(Capsule(new Vector2(32, 58), new Vector2(32, 15), 5.5f), Ellipse(new Vector2(32, 10), new Vector2(6.5f, 7.5f)));
            Sh(CrowdAtlas.ArmCol, 2, arm, Vol(32, 7));
            Sh(CrowdAtlas.SleeveCol, 2, Capsule(new Vector2(32, 59), new Vector2(32, 34), 7f), Vol(32, 8));
            S(CrowdAtlas.PhoneCol, 2, RRect(25, 20, 14, 24, 3), W(0.12f));
            S(CrowdAtlas.PhoneCol, 2, RRect(26.5f, 22, 11, 20, 2), W(1f));
            Sh(CrowdAtlas.GlowCol, 2, Circle(new Vector2(32, 32), 31), q => W(1f, Mathf.Pow(1f - Mathf.Clamp01((q - new Vector2(32, 32)).magnitude / 31f), 2f)));
            S(CrowdAtlas.SignCol, 2, RRect(2, 14, 60, 38, 4), W(0.35f));
            S(CrowdAtlas.SignCol, 2, RRect(5, 17, 54, 32, 3), W(1f));
            Sdf far = Union(Circle(new Vector2(32, 40), 10), Ellipse(new Vector2(32, 16), new Vector2(19, 15)));
            Sh(CrowdAtlas.FarCol, 2, far, Vol(32, 20));
            Sh(CrowdAtlas.FarHandsCol, 2, Union(far, Union(Capsule(new Vector2(17, 22), new Vector2(12, 58), 3.2f), Capsule(new Vector2(47, 22), new Vector2(52, 58), 3.2f))), Vol(32, 20));
            Sh(CrowdAtlas.FarWideCol, 2, Union(Union(Circle(new Vector2(20, 40), 9), Ellipse(new Vector2(20, 16), new Vector2(15, 14))), Union(Circle(new Vector2(44, 42), 9.5f), Ellipse(new Vector2(44, 17), new Vector2(16, 15)))), Vol(32, 30));
            S(CrowdAtlas.FlashCol, 2, Star(new Vector2(32, 32), 28, 5), W(1f, 0.9f));
            Sh(CrowdAtlas.FlashCol, 2, Circle(new Vector2(32, 32), 10), q => W(1f, 1f - Mathf.Clamp01((q - new Vector2(32, 32)).magnitude / 10f)));
            S(CrowdAtlas.BarrierCol, 2, RRect(0, 30, 64, 5, 2), W(0.85f));
            S(CrowdAtlas.BarrierCol, 2, RRect(0, 8, 64, 4, 2), W(0.7f));
            for (int i = 0; i < 4; i++) S(CrowdAtlas.BarrierCol, 2, RRect(4 + i * 16, 4, 3, 32, 1), W(0.75f));
            return p;
        }
    }
}
