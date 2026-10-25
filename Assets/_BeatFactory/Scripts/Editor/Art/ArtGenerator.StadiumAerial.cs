using UnityEditor;
using UnityEngine;
using static BeatFactory.EditorTools.Art.SdfPainter;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// Stadium seen from a drone at night (2048×1120, oblique view, transparent outside the stadium) for the stadium intro.
    /// The far stands look taller than the near ones (the camera is in front of the stadium). Seat rows, aisles and stairs,
    /// ~45,000 fans as coloured dots with phone lights, the pitch full of people in front of the stage, a stage with
    /// LED walls at the far end, roof lights and four floodlight towers. Original procedural art (no photos or footage).
    /// </summary>
    public static partial class ArtGenerator
    {
        public const string StadiumAerialPath = VenueArt + "/stadium_aerial.png";
        public const int AerialW = 2048, AerialH = 1120;

        /// <summary>Key points of the aerial art (pixels, bottom-left origin) used by the intro camera and its overlays.</summary>
        public static readonly Vector2 AerialStage = new Vector2(1024, 752);
        public static readonly Vector2[] AerialFloodlights = { new Vector2(288, 960), new Vector2(1760, 960), new Vector2(238, 454), new Vector2(1810, 454) };

        static readonly Vector2 Cs = new Vector2(1024, 540), Rs = new Vector2(960, 455);   // outer edge of the stands
        static readonly Vector2 Cf = new Vector2(1024, 470), Rf = new Vector2(600, 245);   // pitch

        [MenuItem("Tools/Beat Factory/Art/Generate Stadium Aerial")]
        public static void GenerateStadiumAerial() => GenerateStadiumAerial(true);

        public static void GenerateStadiumAerial(bool overwrite)
        {
            Make(StadiumAerialPath, overwrite, DrawStadiumAerial);
            var imp = (TextureImporter)AssetImporter.GetAtPath(StadiumAerialPath);
            if (imp != null && imp.maxTextureSize < 2048)
            {
                imp.maxTextureSize = 2048;
                imp.SaveAndReimport();
            }
        }

        /// <summary>0 at the pitch edge → 1 at the top of the stands (the stands are a blend of the two ellipses).</summary>
        static float StandsDepth(Vector2 p)
        {
            float lo = 0f, hi = 1f;
            if (InEllipse(p, Cf, Rf)) return -1f;
            if (!InEllipse(p, Cs, Rs)) return 2f;
            for (int i = 0; i < 14; i++)
            {
                float k = (lo + hi) * 0.5f;
                if (InEllipse(p, Vector2.Lerp(Cf, Cs, k), Vector2.Lerp(Rf, Rs, k))) hi = k; else lo = k;
            }
            return (lo + hi) * 0.5f;
        }

        static bool InEllipse(Vector2 p, Vector2 c, Vector2 r)
        {
            float x = (p.x - c.x) / r.x, y = (p.y - c.y) / r.y;
            return x * x + y * y <= 1f;
        }

        static Vector2 OnRing(float k, float angle)
        {
            var c = Vector2.Lerp(Cf, Cs, k);
            var r = Vector2.Lerp(Rf, Rs, k);
            return new Vector2(c.x + Mathf.Cos(angle) * r.x, c.y + Mathf.Sin(angle) * r.y);
        }

        static SdfPainter DrawStadiumAerial()
        {
            var p = new SdfPainter(AerialW, AerialH);
            var rnd = new System.Random(2026);

            // shadow around the building
            p.Fill(Ellipse(Cs, Rs + new Vector2(70, 60)), q =>
            {
                float d = Mathf.Clamp01(((q - Cs) / (Rs + new Vector2(70, 60))).magnitude);
                return new Color(0, 0, 0, 0.55f * (1f - d * d));
            });
            // outer wall + roof rim
            p.Fill(Ellipse(Cs + new Vector2(0, -16), Rs + new Vector2(34, 30)), Hex("15141C"));
            p.Fill(Ellipse(Cs, Rs + new Vector2(26, 24)), Hex("24232E"));

            // stands: dark seats, row lines, tier walkways and stairs
            var seat = Hex("120F1D");
            for (int y = 0; y < AerialH; y++)
                for (int x = 0; x < AerialW; x++)
                {
                    var q = new Vector2(x + 0.5f, y + 0.5f);
                    float k = StandsDepth(q);
                    if (k < 0f || k > 1f) continue;
                    float rows = k * 44f;
                    float line = Mathf.Abs(rows - Mathf.Round(rows));
                    var c = Color.Lerp(seat, Hex("1E1A2E"), 0.5f + 0.5f * Mathf.Sin(rows * 0.7f) * 0.3f);
                    if (line < 0.12f) c = Color.Lerp(c, Color.black, 0.35f);
                    if (Mathf.Abs(k - 0.36f) < 0.012f || Mathf.Abs(k - 0.7f) < 0.012f) c = Hex("2C2A36"); // concourse rings
                    float ang = Mathf.Atan2((q.y - Cs.y) / Rs.y, (q.x - Cs.x) / Rs.x) * Mathf.Rad2Deg;
                    if (Mathf.Abs(Mathf.Repeat(ang, 15f) - 7.5f) > 7.1f) c = Hex("2A2734");                       // stairs
                    p.SetPixel(x, y, new Color(c.r, c.g, c.b, 1f));
                }

            // the crowd in the stands: rows of fans in many colours, a few phone lights
            Color[] shirts =
            {
                Hex("F2F0F5"), Hex("16151C"), Hex("6C3BD9"), Hex("FF3DC8"), Hex("2EA56F"), Hex("C2183A"), Hex("FF8A3D"),
                Hex("3DE1FF"), Hex("F2C14E"), Hex("8A84B0"), Hex("2E3A52"), Hex("B48CFF"), Hex("E8E2D6"),
            };
            Color[] skins = { Hex("3E2618"), Hex("6B4430"), Hex("8D5A3B"), Hex("C68E64"), Hex("E0B48C"), Hex("F1C9A3") };
            const int rowsCount = 44;
            for (int row = 0; row < rowsCount; row++)
            {
                float k = (row + 0.5f) / rowsCount;
                if (Mathf.Abs(k - 0.36f) < 0.02f || Mathf.Abs(k - 0.7f) < 0.02f) continue;
                var r = Vector2.Lerp(Rf, Rs, k);
                float perimeter = Mathf.PI * (3 * (r.x + r.y) - Mathf.Sqrt((3 * r.x + r.y) * (r.x + 3 * r.y)));
                int n = Mathf.RoundToInt(perimeter / 5.2f);
                for (int i = 0; i < n; i++)
                {
                    float a = (i + (float)rnd.NextDouble() * 0.5f) / n * Mathf.PI * 2f;
                    float deg = Mathf.Repeat(a * Mathf.Rad2Deg, 15f);
                    if (deg < 0.6f || deg > 14.4f) continue; // stairs stay empty
                    if (rnd.NextDouble() < 0.06) continue;   // a few empty seats
                    var pos = OnRing(k + ((float)rnd.NextDouble() - 0.5f) * 0.012f, a);
                    // the far stands behind the stage are hidden by the LED walls anyway
                    float depthLight = Mathf.Lerp(0.55f, 1f, pos.y < Cs.y ? 1f : 0.85f);
                    if (rnd.NextDouble() < 0.045)
                    {
                        p.Fill(Circle(pos, 3.2f), new Color(0.85f, 0.92f, 1f, 0.35f), new RectInt((int)pos.x - 5, (int)pos.y - 5, 11, 11));
                        p.Fill(Circle(pos, 1.3f), Color.white, new RectInt((int)pos.x - 3, (int)pos.y - 3, 7, 7));
                        continue;
                    }
                    var shirt = shirts[rnd.Next(shirts.Length)] * (0.55f + 0.35f * (float)rnd.NextDouble()) * depthLight;
                    var skin = skins[rnd.Next(skins.Length)] * 0.8f * depthLight;
                    shirt.a = skin.a = 1f;
                    p.Fill(Ellipse(pos, new Vector2(2.1f, 1.6f)), shirt, new RectInt((int)pos.x - 4, (int)pos.y - 4, 9, 9));
                    p.Fill(Circle(pos + new Vector2(0, 1.6f), 1.15f), skin, new RectInt((int)pos.x - 3, (int)pos.y - 2, 7, 7));
                }
            }

            // roof lights along the rim
            for (int i = 0; i < 96; i++)
            {
                float a = i / 96f * Mathf.PI * 2f;
                var pos = new Vector2(Cs.x + Mathf.Cos(a) * (Rs.x + 14), Cs.y + Mathf.Sin(a) * (Rs.y + 12));
                p.Fill(Circle(pos, 3f), new Color(1f, 0.95f, 0.85f, 0.9f), new RectInt((int)pos.x - 5, (int)pos.y - 5, 11, 11));
            }

            // pitch with mowing stripes, light spill from the floodlights
            p.Fill(Ellipse(Cf, Rf + new Vector2(10, 8)), Hex("0B0B10"));
            p.Fill(Ellipse(Cf, Rf), q =>
            {
                bool dark = Mathf.FloorToInt((q.x - (Cf.x - Rf.x)) / 75f) % 2 == 0;
                var c = dark ? Hex("17532B") : Hex("1E6634");
                float spill = 1f - Mathf.Clamp01(((q - Cf) / Rf).magnitude);
                return Color.Lerp(c, Hex("4E9E5E"), 0.25f * spill);
            });

            // the floor crowd in front of the stage
            var floorC = new Vector2(1024, 600);
            var floorR = new Vector2(430, 125);
            for (int i = 0; i < 9000; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float d = Mathf.Sqrt((float)rnd.NextDouble());
                var pos = floorC + new Vector2(Mathf.Cos(a) * floorR.x, Mathf.Sin(a) * floorR.y) * d;
                if (!InEllipse(pos, Cf, Rf - new Vector2(20, 14))) continue;
                if (rnd.NextDouble() < 0.05)
                {
                    p.Fill(Circle(pos, 1.3f), Color.white, new RectInt((int)pos.x - 3, (int)pos.y - 3, 7, 7));
                    continue;
                }
                var shirt = shirts[rnd.Next(shirts.Length)] * (0.6f + 0.35f * (float)rnd.NextDouble());
                shirt.a = 1f;
                p.Fill(Circle(pos, 1.9f), shirt, new RectInt((int)pos.x - 4, (int)pos.y - 4, 9, 9));
            }

            // stage at the far end: deck, LED wall, side screens, truss with lights, speaker hangs
            var st = AerialStage;
            p.Fill(Polygon(new Vector2(st.x - 270, st.y - 70), new Vector2(st.x + 270, st.y - 70), new Vector2(st.x + 235, st.y - 18), new Vector2(st.x - 235, st.y - 18)), Hex("0E0D14"));
            p.Fill(RRect(st.x - 238, st.y - 22, 476, 10), Hex("2B2A35"));
            p.Fill(RRect(st.x - 206, st.y - 12, 412, 150, 4), Hex("09080E"));
            p.Fill(RRect(st.x - 196, st.y - 4, 392, 134, 3), q =>
            {
                float u = Mathf.InverseLerp(st.x - 196, st.x + 196, q.x);
                var c = Color.Lerp(Hex("5B2BD6"), Hex("FF3DC8"), u);
                if (((int)q.y & 3) == 0) c *= 0.82f;
                c.a = 1f;
                return c;
            });
            foreach (int side in new[] { -1, 1 })
            {
                p.Fill(RRect(st.x + side * 286 - 42, st.y - 6, 84, 112, 3), Hex("09080E"));
                p.Fill(RRect(st.x + side * 286 - 37, st.y - 1, 74, 102, 2), Hex("3D8BFF"));
                p.Fill(RRect(st.x + side * 222 - 12, st.y - 4, 24, 128), Hex("16151E")); // speaker hang
            }
            p.Fill(RRect(st.x - 300, st.y + 138, 600, 14), Hex("3A3946"));
            for (int i = 0; i < 16; i++)
            {
                var pos = new Vector2(st.x - 285 + i * 38, st.y + 134);
                Color[] lamp = { Hex("FF3DC8"), Hex("3DE1FF"), Hex("F2C14E"), Hex("B48CFF") };
                p.Fill(Circle(pos, 4.5f), lamp[i % lamp.Length], new RectInt((int)pos.x - 6, (int)pos.y - 6, 13, 13));
            }

            // floodlight towers
            foreach (var f in AerialFloodlights)
            {
                bool far = f.y > Cs.y;
                float h = far ? 120 : 175;
                var foot = new Vector2(f.x, f.y - h);
                p.Fill(RRect(foot.x - 5, foot.y, 10, h), Hex("2B2A33"));
                p.Fill(RRect(f.x - 46, f.y - 4, 92, 38, 4), Hex("3A3946"));
                for (int r = 0; r < 2; r++)
                    for (int c = 0; c < 4; c++)
                        p.Fill(Circle(new Vector2(f.x - 33 + c * 22, f.y + 6 + r * 16), 7f), new Color(1f, 0.97f, 0.88f, 1f), new RectInt((int)f.x - 50, (int)f.y - 8, 100, 50));
            }
            return p;
        }
    }
}
