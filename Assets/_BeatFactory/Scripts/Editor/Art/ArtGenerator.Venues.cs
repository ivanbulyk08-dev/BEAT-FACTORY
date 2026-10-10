using UnityEngine;
using static BeatFactory.EditorTools.Art.SdfPainter;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// Concert venues (portrait 800×1400): club → small venue → concert hall → arena → stadium.
    /// Each venue = background (architecture, stage, LED screen, lights) + a separate crowd layer (transparent, 800×560)
    /// that bounces at runtime. The artist's portrait is shown on the LED screen (<see cref="VenueScreens"/>).
    /// </summary>
    public static partial class ArtGenerator
    {
        public const int VenueW = 800, VenueH = 1400, CrowdH = 560;
        public const string VenueArt = ArtRoot + "/Venues";
        public static string VenuePath(int tier) => $"{VenueArt}/venue_{tier}.png";
        public static string CrowdPath(int tier) => $"{VenueArt}/crowd_{tier}.png";
        public const string BeamPath = Fx + "/fx_beam.png";

        /// <summary>Main LED screen per venue (800×1400 coordinates, bottom-left origin).</summary>
        public static readonly Rect[] VenueScreens =
        {
            new Rect(270, 720, 260, 150),
            new Rect(190, 720, 420, 240),
            new Rect(170, 740, 460, 260),
            new Rect(160, 760, 480, 280),
            new Rect(120, 780, 560, 310),
        };

        static void GenerateVenues(bool overwrite)
        {
            for (int i = 0; i < VenueScreens.Length; i++)
            {
                int t = i;
                Make(VenuePath(t), overwrite, () => VenueBackground(t));
                Make(CrowdPath(t), overwrite, () => Crowd(t));
            }
            Make(BeamPath, overwrite, FxBeam);
        }

        static SdfPainter FxBeam()
        {
            var p = new SdfPainter(64, 512);
            p.Fill(Polygon(new Vector2(28, 512), new Vector2(36, 512), new Vector2(64, 0), new Vector2(0, 0)),
                q => new Color(1, 1, 1, Mathf.Pow(q.y / 512f, 1.4f) * (1f - Mathf.Abs(q.x - 32) / 34f)));
            return p;
        }

        static void Truss(SdfPainter p, float x0, float x1, float y, float h)
        {
            R(p, x0, y, x1 - x0, 4, 1, Hex("4A4658"));
            R(p, x0, y + h - 4, x1 - x0, 4, 1, Hex("4A4658"));
            for (float x = x0; x < x1 - 1; x += h)
            {
                L(p, x, y, x + h, y + h, 1.5f, Hex("3A3646"));
                L(p, x, y + h, x + h, y, 1.5f, Hex("3A3646"));
            }
        }

        static void ParCan(SdfPainter p, float x, float y, Color c)
        {
            R(p, x - 9, y - 16, 18, 22, 4, Hex("1A1824"));
            C(p, x, y - 16, 7, c);
            Light(p, x, y - 18, 34, A(c, 0.6f));
        }

        static void Speakers(SdfPainter p, float x, float y, int boxes, float w)
        {
            for (int i = 0; i < boxes; i++)
            {
                float by = y + i * w * 0.75f;
                R(p, x, by, w, w * 0.72f, 4, Hex("121018"));
                C(p, x + w / 2, by + w * 0.36f, w * 0.24f, Hex("1F1C28"));
                Rn(p, x + w / 2, by + w * 0.36f, w * 0.24f, 2, Hex("2E2A3A"));
            }
        }

        static void LedScreen(SdfPainter p, Rect s, Color glow)
        {
            Light(p, s.center.x, s.center.y, Mathf.Max(s.width, s.height) * 0.9f, A(glow, 0.25f));
            R(p, s.x - 10, s.y - 10, s.width + 20, s.height + 20, 6, Hex("0A0910"));
            RG(p, s.x, s.y, s.width, s.height, 3, Hex("1A1430"), Hex("2A1E4A"));
        }

        static void Performer(SdfPainter p, float x, float y, float s)
        {
            // simple silhouette with a mic — the portrait on the screen is the real "face" of the show
            var col = Hex("0B0A10");
            p.Fill(Capsule(new Vector2(x - 8 * s, y), new Vector2(x - 6 * s, y + 46 * s), 7 * s), col, Bnd(x - 30 * s, y - 10, 60 * s, 70 * s));
            p.Fill(Capsule(new Vector2(x + 8 * s, y), new Vector2(x + 6 * s, y + 46 * s), 7 * s), col, Bnd(x - 30 * s, y - 10, 60 * s, 70 * s));
            p.Fill(RRect(x - 18 * s, y + 42 * s, 36 * s, 50 * s, 12 * s), col, Bnd(x - 30 * s, y + 30 * s, 60 * s, 70 * s));
            C(p, x, y + 104 * s, 14 * s, col);
            L(p, x + 14 * s, y + 84 * s, x + 30 * s, y + 104 * s, 5 * s, col);
            C(p, x + 31 * s, y + 107 * s, 5 * s, Hex("3A3646"));
            L(p, x - 14 * s, y + 84 * s, x - 34 * s, y + 118 * s, 5 * s, col);
            Light(p, x, y + 60 * s, 120 * s, Hex("FFFFFF", 0.12f));
        }

        static void Stars(SdfPainter p, int count, float yMin, int seed)
        {
            var rnd = new System.Random(seed);
            for (int i = 0; i < count; i++)
                C(p, (float)rnd.NextDouble() * VenueW, yMin + (float)rnd.NextDouble() * (VenueH - yMin), 0.6f + (float)rnd.NextDouble() * 1.2f,
                    Hex("FFFFFF", 0.3f + (float)rnd.NextDouble() * 0.6f));
        }

        /// <summary>Rows of tiny audience dots on stands (arena/stadium), with phone lights.</summary>
        static void Stands(SdfPainter p, float x0, float x1, float y0, float y1, float slope, int seed, float density)
        {
            var rnd = new System.Random(seed);
            for (float y = y0; y < y1; y += 9)
            {
                float k = (y - y0) / Mathf.Max(1, y1 - y0);
                for (float x = x0; x < x1; x += 7)
                {
                    if (rnd.NextDouble() > density) continue;
                    float yy = y + (x - x0) * slope;
                    var c = Color.Lerp(Hex("2A2240"), Hex("181428"), k);
                    if (rnd.NextDouble() < 0.06) c = Hex("FFFFFF", 0.85f);
                    else if (rnd.NextDouble() < 0.05) c = Color.HSVToRGB((float)rnd.NextDouble(), 0.6f, 1f);
                    C(p, x, yy, 2.2f, c);
                }
            }
        }

        static SdfPainter VenueBackground(int t)
        {
            var p = new SdfPainter(VenueW, VenueH);
            var s = VenueScreens[t];
            switch (t)
            {
                case 0: // UNDERGROUND CLUB: brick, low ceiling, a few par cans, smoke
                    p.VerticalGradient(Hex("120A10"), Hex("1E1018"));
                    for (float y = 520, row = 0; y < 1180; y += 26, row++)
                        for (float x = (row % 2) * -30; x < VenueW; x += 60)
                            R(p, x + 2, y + 2, 56, 22, 2, Hex(row % 3 == 0 ? "2E1A1E" : "2A161A"));
                    R(p, 0, 1180, VenueW, 220, 0, Hex("0A0608"));
                    for (float x = 40; x < VenueW; x += 120) R(p, x, 1180, 14, 220, 0, Hex("140C10"));
                    Truss(p, 120, 680, 1150, 22);
                    foreach (var (x, c) in new[] { (180f, "FF3DC8"), (320f, "B65CFF"), (480f, "3DE1FF"), (620f, "FF3DC8") }) ParCan(p, x, 1150, Hex(c));
                    // neon sign + small screen
                    R(p, 300, 960, 200, 60, 8, Hex("FF3DC8", 0.15f));
                    for (int i = 0; i < 4; i++) R(p, 320 + i * 44, 975, 30, 30, 4, Hex("FF3DC8", 0.85f));
                    Light(p, 400, 990, 160, Hex("FF3DC8", 0.25f));
                    LedScreen(p, s, Hex("B65CFF"));
                    // low stage + amp stacks
                    R(p, 180, 500, 440, 60, 4, Hex("1A1014"));
                    R(p, 180, 556, 440, 6, 2, Hex("3A2028"));
                    Speakers(p, 70, 560, 2, 90);
                    Speakers(p, 640, 560, 2, 90);
                    LightEllipse(p, 400, 700, 420, 260, Hex("B65CFF", 0.12f));
                    break;

                case 1: // SMALL VENUE: black box, truss, LED wall, pro lights
                    p.VerticalGradient(Hex("07060C"), Hex("100C1A"));
                    Truss(p, 60, 740, 1180, 28);
                    Truss(p, 60, 88, 560, 620);
                    Truss(p, 712, 740, 560, 620);
                    for (int i = 0; i < 7; i++) ParCan(p, 110 + i * 97, 1180, Color.HSVToRGB(0.75f + i * 0.04f, 0.7f, 1f));
                    LedScreen(p, s, Hex("3DE1FF"));
                    R(p, 110, 490, 580, 70, 4, Hex("15121C"));
                    R(p, 110, 556, 580, 6, 2, Hex("3DE1FF", 0.6f));
                    Speakers(p, 10, 560, 3, 80);
                    Speakers(p, 710, 560, 3, 80);
                    LightEllipse(p, 400, 620, 380, 90, Hex("3DE1FF", 0.12f));
                    break;

                case 2: // CONCERT HALL: theatre, balconies, red curtains, gold proscenium
                    p.VerticalGradient(Hex("120808"), Hex("1E0E10"));
                    // balconies with audience
                    for (int b = 0; b < 2; b++)
                    {
                        float y = 900 + b * 170;
                        R(p, 0, y, VenueW, 22, 4, Hex("6A1A22"));
                        R(p, 0, y + 18, VenueW, 6, 2, Hex("C9A23A"));
                        Stands(p, 10, 790, y + 26, y + 70, 0, 30 + b, 0.7f);
                    }
                    // proscenium arch
                    p.Fill(Subtract(RRect(60, 470, 680, 760, 0), RRect(100, 470, 600, 700, 120)), Hex("C9A23A"), Bnd(60, 470, 680, 760));
                    p.Fill(Subtract(RRect(70, 470, 660, 750, 0), RRect(100, 470, 600, 700, 120)), Hex("7A1A24"), Bnd(60, 470, 680, 760));
                    // curtains
                    for (int side = 0; side < 2; side++)
                        for (int f = 0; f < 6; f++)
                        {
                            float x = side == 0 ? 100 + f * 14 : 700 - f * 14 - 14;
                            RG(p, x, 470, 16, 690, 6, Hex("5A0E16"), Hex("8A1E2A"));
                        }
                    LedScreen(p, s, Hex("FFC870"));
                    R(p, 90, 470, 620, 90, 4, Hex("3A2416"));
                    for (float x = 90; x < 710; x += 20) R(p, x, 470, 2, 90, 0, Hex("2A180E"));
                    R(p, 90, 556, 620, 6, 2, Hex("C9A23A"));
                    Truss(p, 140, 660, 1130, 24);
                    for (int i = 0; i < 6; i++) ParCan(p, 180 + i * 88, 1130, Hex(i % 2 == 0 ? "FFC870" : "FF8A5C"));
                    LightEllipse(p, 400, 600, 300, 80, Hex("FFC870", 0.15f));
                    break;

                case 3: // ARENA: dome, tiered stands, three screens, lasers rig, huge stage
                    p.VerticalGradient(Hex("05040A"), Hex("0C0A18"));
                    for (int i = 0; i < 9; i++)
                        p.Fill(Ring(new Vector2(400, 300), 900 + i * 60, 2), Hex("1A1830", 0.6f), Bnd(0, 300, VenueW, 1100));
                    Stands(p, 0, 330, 600, 1080, 0.35f, 41, 0.75f);
                    Stands(p, 470, 800, 715, 1195, -0.35f, 42, 0.75f);
                    Truss(p, 30, 770, 1210, 30);
                    Truss(p, 30, 770, 1120, 18);
                    for (int i = 0; i < 9; i++) ParCan(p, 70 + i * 82, 1210, Color.HSVToRGB(0.8f + 0.05f * i % 1, 0.75f, 1f));
                    LedScreen(p, s, Hex("FF3DC8"));
                    LedScreen(p, new Rect(20, 800, 110, 200), Hex("3DE1FF"));
                    LedScreen(p, new Rect(670, 800, 110, 200), Hex("3DE1FF"));
                    R(p, 30, 470, 740, 90, 4, Hex("101018"));
                    R(p, 30, 556, 740, 6, 2, Hex("FF3DC8", 0.8f));
                    for (float x = 60; x < 760; x += 40) R(p, x, 476, 22, 6, 2, Hex("3DE1FF", 0.7f));
                    Speakers(p, 140, 600, 4, 70);
                    Speakers(p, 590, 600, 4, 70);
                    LightEllipse(p, 400, 620, 420, 110, Hex("FF3DC8", 0.15f));
                    break;

                default: // STADIUM: open night sky, floodlight towers, endless stands, mega stage
                    p.VerticalGradient(Hex("05060E"), Hex("0E1430"));
                    Stars(p, 160, 1100, 77);
                    // stadium bowl
                    p.Fill(Ellipse(new Vector2(400, 820), new Vector2(760, 330)), Hex("12101E"), Bnd(0, 490, VenueW, 700));
                    Stands(p, 0, 800, 640, 1090, 0, 51, 0.85f);
                    R(p, 0, 1086, VenueW, 10, 2, Hex("2A2640"));
                    // floodlight towers
                    foreach (float x in new[] { 40f, 760f })
                    {
                        L(p, x, 600, x, 1290, 6, Hex("1A1824"));
                        R(p, x - 50, 1280, 100, 60, 4, Hex("1A1824"));
                        for (int r = 0; r < 2; r++) for (int c = 0; c < 4; c++) C(p, x - 36 + c * 24, 1296 + r * 24, 8, Hex("FFFBE8"));
                        Light(p, x, 1310, 260, Hex("FFF6D8", 0.35f));
                    }
                    // mega stage: roof + screens
                    Pg(p, Hex("16141E"), new Vector2(60, 1180), new Vector2(740, 1180), new Vector2(700, 1230), new Vector2(100, 1230));
                    Truss(p, 60, 740, 1150, 30);
                    for (int i = 0; i < 10; i++) ParCan(p, 90 + i * 69, 1150, Color.HSVToRGB(0.1f * i % 1, 0.6f, 1f));
                    LedScreen(p, s, Hex("FFD23D"));
                    LedScreen(p, new Rect(0, 820, 100, 240), Hex("FF3DC8"));
                    LedScreen(p, new Rect(700, 820, 100, 240), Hex("FF3DC8"));
                    R(p, 0, 450, VenueW, 110, 4, Hex("0E0C16"));
                    R(p, 0, 556, VenueW, 6, 2, Hex("FFD23D", 0.8f));
                    for (float x = 20; x < 780; x += 30) R(p, x, 456, 16, 6, 2, Hex("FFD23D", 0.6f));
                    Speakers(p, 120, 600, 5, 66);
                    Speakers(p, 614, 600, 5, 66);
                    LightEllipse(p, 400, 640, 460, 140, Hex("FFD23D", 0.12f));
                    break;
            }
            return p;
        }

        /// <summary>Crowd silhouettes: more rows, denser and more raised hands the bigger the venue.</summary>
        static SdfPainter Crowd(int t)
        {
            var p = new SdfPainter(VenueW, CrowdH);
            int[] rowsByTier = { 3, 4, 6, 8, 11 };
            float[] density = { 0.55f, 0.72f, 0.86f, 0.95f, 1f };
            float[] hands = { 0.12f, 0.18f, 0.25f, 0.35f, 0.45f };
            int rows = rowsByTier[t];
            var rnd = new System.Random(100 + t);
            float topY = 150 + rows * 34f;
            for (int r = rows - 1; r >= 0; r--)
            {
                float k = r / (float)Mathf.Max(1, rows - 1);          // 0 = front row
                float y = Mathf.Lerp(40, topY, k);
                float size = Mathf.Lerp(34, 14, k);
                var col = Color.Lerp(Hex("06050A"), Hex("1C1830"), k);
                var rim = Color.Lerp(Hex("3A2A5A"), Hex("2A2444"), k);
                // solid mass under this row (bodies of everyone behind the next row)
                p.Fill(RRect(-10, 0, VenueW + 20, Mathf.Max(1, y - size * 0.6f), 0), col, Bnd(0, 0, VenueW, y));
                for (float x = -20 + (float)rnd.NextDouble() * size; x < VenueW + 20; x += size * Mathf.Lerp(1.5f, 1.15f, density[t]))
                {
                    if (rnd.NextDouble() > density[t]) continue;
                    float yy = y + (float)rnd.NextDouble() * size * 0.4f;
                    float bottom = Mathf.Max(0, yy - size * 2.2f);
                    p.Fill(RRect(x - size * 0.9f, bottom, size * 1.8f, yy + size * 0.2f - bottom, size * 0.6f), col, Bnd(x - size, bottom, size * 2, yy + size - bottom));
                    C(p, x, yy + size * 0.9f, size * 0.55f, col);
                    p.Fill(Arc(new Vector2(x, yy + size * 0.9f), size * 0.55f, 1.6f, 40, 140), rim, Bnd(x - size, yy, size * 2, size * 2));
                    if (rnd.NextDouble() < hands[t])
                    {
                        float dir = rnd.NextDouble() < 0.5 ? -1 : 1;
                        L(p, x + dir * size * 0.5f, yy + size * 0.5f, x + dir * size * 0.8f, yy + size * 2.4f, size * 0.16f, col);
                        if (rnd.NextDouble() < 0.4) R(p, x + dir * size * 0.8f - 3, yy + size * 2.4f, 6, 10, 1, Hex("DDEBFF", 0.9f)); // phone
                    }
                }
            }
            // front row darkest fade so the crowd sits in front of the stage
            p.Fill(RRect(0, 0, VenueW, 60, 0), q => new Color(0, 0, 0, 0.6f * (1f - q.y / 60f)), Bnd(0, 0, VenueW, 60));
            return p;
        }
    }
}
