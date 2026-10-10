using UnityEngine;
using static BeatFactory.EditorTools.Art.SdfPainter;
using Sdf = System.Func<UnityEngine.Vector2, float>;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// Studio rooms (1000×560) and producer stations (220×200).
    /// Layout shared with the scene builder: monitor screens (DAW overlays), speaker cones, LED strips.
    /// </summary>
    public static partial class ArtGenerator
    {
        /// <summary>DAW screens per tier (room coords, bottom-left origin). First = main monitor.</summary>
        // Room order (= studio level): 0 Basement, 1 Bedroom, 2 Small Pro, 3 Professional, 4 Modern,
        // 5 Luxury, 6 Major, 7 Elite, 8 Record Label, 9 Mega Complex.
        public static readonly Rect[][] TierScreens =
        {
            new[] { new Rect(200, 240, 170, 106) },
            new[] { new Rect(188, 236, 194, 120) },
            new[] { new Rect(186, 236, 204, 126) },
            new[] { new Rect(178, 232, 214, 132), new Rect(56, 262, 104, 66) },
            new[] { new Rect(164, 230, 240, 148), new Rect(40, 258, 112, 70) },
            new[] { new Rect(150, 236, 270, 124), new Rect(36, 258, 100, 64) },
            new[] { new Rect(166, 232, 240, 140), new Rect(36, 258, 112, 70) },
            new[] { new Rect(160, 230, 250, 150), new Rect(30, 262, 112, 70) },
            new[] { new Rect(164, 230, 240, 148), new Rect(36, 258, 112, 70) },
            new[] { new Rect(150, 228, 270, 158), new Rect(28, 256, 112, 72) },
        };

        /// <summary>Speaker cones per tier (pulse with the music).</summary>
        public static readonly Rect[][] TierCones =
        {
            new[] { new Rect(157, 214, 26, 26), new Rect(385, 214, 26, 26) },
            new[] { new Rect(128, 216, 34, 34), new Rect(390, 216, 34, 34) },
            new[] { new Rect(130, 214, 36, 36), new Rect(404, 214, 36, 36) },
            new[] { new Rect(127, 214, 38, 38), new Rect(400, 214, 38, 38) },
            new[] { new Rect(52, 372, 44, 44), new Rect(572, 372, 44, 44) },
            new[] { new Rect(440, 214, 34, 34), new Rect(98, 196, 34, 34) },
            new[] { new Rect(560, 384, 46, 46), new Rect(900, 384, 46, 46) },
            new[] { new Rect(560, 380, 44, 44), new Rect(52, 380, 44, 44) },
            new[] { new Rect(434, 214, 36, 36), new Rect(110, 196, 34, 34) },
            new[] { new Rect(560, 430, 48, 48), new Rect(900, 430, 48, 48) },
        };

        /// <summary>RGB/LED strip per tier (y, xFrom, xTo) — the glow on top is animated.</summary>
        public static readonly Vector3[] TierLed =
        {
            new Vector3(468, 90, 760),
            new Vector3(520, 0, 1000),
            new Vector3(524, 0, 1000),
            new Vector3(530, 0, 1000),
            new Vector3(536, 0, 1000),
            new Vector3(538, 0, 1000),
            new Vector3(536, 0, 1000),
            new Vector3(540, 0, 1000),
            new Vector3(534, 0, 1000),
            new Vector3(544, 0, 1000),
        };

        /// <summary>
        /// Rooms painted with a mirrored floor plan (desk on the right). Painters draw in the normal layout;
        /// the builder uses <see cref="ScreensFor"/>/<see cref="ConesFor"/>/<see cref="MirrorX"/> for final positions.
        /// </summary>
        public static readonly bool[] TierMirrored = { false, false, true, false, false, true, false, false, true, false };

        public static float MirrorX(int tier, float x, float width = 0) => TierMirrored[tier] ? SceneW - x - width : x;
        static Rect MirrorRect(int tier, Rect r) => new Rect(MirrorX(tier, r.x, r.width), r.y, r.width, r.height);
        public static Rect[] ScreensFor(int tier) => System.Array.ConvertAll(TierScreens[tier], r => MirrorRect(tier, r));
        public static Rect[] ConesFor(int tier) => System.Array.ConvertAll(TierCones[tier], r => MirrorRect(tier, r));
        public static Vector3 LedFor(int tier)
        {
            var l = TierLed[tier];
            return TierMirrored[tier] ? new Vector3(l.x, SceneW - l.z, SceneW - l.y) : l;
        }

        /// <summary>Front row of producer stations (Beginner, Melody, Drum), room x of the station center.</summary>
        public static readonly float[] StationCenters = { 170, 500, 830 };

        /// <summary>Laptop screen inside a station sprite (station sprite is drawn at (centerX − 110, 14)).</summary>
        public static readonly Rect StationScreen = new Rect(24, 106, 92, 58);

        static readonly Color Bezel = Hex("0E0D14");

        // ------------------------------------------------------------------ bounded helpers (rooms are big)

        static RectInt Bnd(float x, float y, float w, float h, float pad = 3) =>
            new RectInt(Mathf.FloorToInt(x - pad), Mathf.FloorToInt(y - pad), Mathf.CeilToInt(w + pad * 2) + 1, Mathf.CeilToInt(h + pad * 2) + 1);

        static void R(SdfPainter p, float x, float y, float w, float h, float r, Color c) => p.Fill(RRect(x, y, w, h, r), c, Bnd(x, y, w, h));
        static void RG(SdfPainter p, float x, float y, float w, float h, float r, Color bottom, Color top) =>
            p.Fill(RRect(x, y, w, h, r), VGradient(y, bottom, y + h, top), Bnd(x, y, w, h));
        static void C(SdfPainter p, float x, float y, float r, Color c) => p.Fill(Circle(new Vector2(x, y), r), c, Bnd(x - r, y - r, r * 2, r * 2));
        static void Rn(SdfPainter p, float x, float y, float r, float th, Color c) => p.Fill(Ring(new Vector2(x, y), r, th), c, Bnd(x - r, y - r, r * 2, r * 2, th + 2));

        static void L(SdfPainter p, float x1, float y1, float x2, float y2, float r, Color c) =>
            p.Fill(Capsule(new Vector2(x1, y1), new Vector2(x2, y2), r), c,
                Bnd(Mathf.Min(x1, x2), Mathf.Min(y1, y2), Mathf.Abs(x2 - x1), Mathf.Abs(y2 - y1), r + 2));

        static void Pg(SdfPainter p, Color c, params Vector2[] v)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var q in v) { x0 = Mathf.Min(x0, q.x); y0 = Mathf.Min(y0, q.y); x1 = Mathf.Max(x1, q.x); y1 = Mathf.Max(y1, q.y); }
            p.Fill(Polygon(v), c, Bnd(x0, y0, x1 - x0, y1 - y0));
        }

        static void Sh(SdfPainter p, Sdf s, Color c, float x, float y, float w, float h) => p.Fill(s, c, Bnd(x, y, w, h));

        /// <summary>Soft radial light (alpha falls off to the edge).</summary>
        static void Light(SdfPainter p, float x, float y, float radius, Color c)
        {
            var center = new Vector2(x, y);
            p.Fill(Circle(center, radius), q =>
            {
                float k = 1f - Mathf.Clamp01((q - center).magnitude / radius);
                return new Color(c.r, c.g, c.b, c.a * k * k);
            }, Bnd(x - radius, y - radius, radius * 2, radius * 2));
        }

        /// <summary>Soft ellipse light (e.g. a lamp cone on a wall or floor).</summary>
        static void LightEllipse(SdfPainter p, float x, float y, float rx, float ry, Color c)
        {
            p.Fill(Ellipse(new Vector2(x, y), new Vector2(rx, ry)), q =>
            {
                float k = 1f - Mathf.Clamp01(new Vector2((q.x - x) / rx, (q.y - y) / ry).magnitude);
                return new Color(c.r, c.g, c.b, c.a * k * k);
            }, Bnd(x - rx, y - ry, rx * 2, ry * 2));
        }

        static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

        // ------------------------------------------------------------------ shared props

        static void Monitor(SdfPainter p, Rect s, float deskTop, bool thickBezel)
        {
            float b = thickBezel ? 9 : 5;
            float cx = s.center.x;
            R(p, cx - 7, deskTop, 14, s.y - deskTop, 2, Hex("1A1822"));
            R(p, cx - 32, deskTop - 2, 64, 7, 3, Hex("1A1822"));
            R(p, s.x - b, s.y - b, s.width + b * 2, s.height + b * 2, 6, Bezel);
            R(p, s.x, s.y, s.width, s.height, 2, Hex("0B0A14"));
            if (thickBezel) C(p, s.xMax + 3, s.y - 5, 1.6f, Hex("4DFFA8"));
        }

        static void MidiKeys(SdfPainter p, float x, float y, float w, int keys, Color body, bool pads)
        {
            R(p, x, y, w, 22, 4, body);
            R(p, x + 2, y + 18, w - 4, 3, 1, A(Color.white, 0.08f));
            float kx = x + 6, kw = (w - 12) / keys;
            if (pads)
            {
                for (int i = 0; i < 4; i++) R(p, x + 6 + i * 9, y + 13, 7, 6, 1.5f, i % 2 == 0 ? Hex("FF3DC8") : Hex("3DE1FF"));
                kx = x + 44;
                kw = (w - 50) / keys;
            }
            else
            {
                for (int i = 0; i < 3; i++) C(p, x + 10 + i * 9, y + 16, 2.6f, Hex("3A3646"));
                kx = x + 36;
                kw = (w - 42) / keys;
            }
            for (int i = 0; i < keys; i++) R(p, kx + i * kw + 0.5f, y + 2, kw - 1, 11, 1, Hex("EDEBF2"));
            for (int i = 0; i < keys - 1; i++)
            {
                int n = i % 7;
                if (n == 2 || n == 6) continue;
                R(p, kx + (i + 1) * kw - kw * 0.3f, y + 6, kw * 0.6f, 7, 0.8f, Hex("17151F"));
            }
        }

        static void PcKeyboard(SdfPainter p, float x, float y, float w, Color body, bool rgb)
        {
            R(p, x, y, w, 9, 3, body);
            for (int row = 0; row < 2; row++)
            for (float kx = x + 3; kx < x + w - 5; kx += 6)
                R(p, kx, y + 1.5f + row * 3.5f, 4.5f, 2.5f, 0.8f, rgb ? Color.HSVToRGB(Mathf.Repeat(kx / 120f, 1f), 0.7f, 1f) : Hex("3A3646"));
            R(p, x + w + 8, y, 12, 8, 4, body);
        }

        static void SpeakerBox(SdfPainter p, Rect cone, float pad, Color body, Color trim)
        {
            float x = cone.x - pad, w = cone.width + pad * 2;
            float y = cone.y - pad - 4, h = cone.height + pad * 2 + cone.height * 0.7f + 6;
            R(p, x, y, w, h, 5, body);
            R(p, x + 2, y + h - 4, w - 4, 2, 1, A(Color.white, 0.07f));
            Rn(p, cone.center.x, cone.center.y, cone.width * 0.5f + 2, 2, trim);
            float tw = cone.width * 0.3f;
            C(p, cone.center.x, cone.yMax + pad + tw * 0.8f, tw, Hex("0E0C14"));
            Rn(p, cone.center.x, cone.yMax + pad + tw * 0.8f, tw, 1.5f, trim);
        }

        static void Poster(SdfPainter p, float x, float y, float w, float h, float angle, Color bg, Color a, Color b, int design)
        {
            var c = new Vector2(x + w / 2, y + h / 2);
            Sh(p, Box(c + new Vector2(3, -3), new Vector2(w / 2, h / 2), 2, angle), A(Color.black, 0.35f), x - 8, y - 8, w + 16, h + 16);
            Sh(p, Box(c, new Vector2(w / 2, h / 2), 2, angle), bg, x - 8, y - 8, w + 16, h + 16);
            float s = Mathf.Min(w, h);
            switch (design)
            {
                case 0: // sun + equalizer
                    C(p, c.x, c.y + h * 0.14f, s * 0.26f, a);
                    for (int i = 0; i < 5; i++)
                    {
                        float bh = s * (0.12f + 0.1f * ((i * 3) % 4));
                        R(p, c.x - w * 0.34f + i * w * 0.15f, y + h * 0.12f, w * 0.1f, bh, 1, b);
                    }
                    break;
                case 1: // cassette
                    R(p, c.x - w * 0.36f, c.y - h * 0.18f, w * 0.72f, h * 0.42f, 4, a);
                    R(p, c.x - w * 0.24f, c.y - h * 0.04f, w * 0.48f, h * 0.16f, 3, b);
                    C(p, c.x - w * 0.12f, c.y + h * 0.04f, s * 0.06f, a);
                    C(p, c.x + w * 0.12f, c.y + h * 0.04f, s * 0.06f, a);
                    R(p, c.x - w * 0.3f, y + h * 0.8f, w * 0.6f, h * 0.06f, 2, b);
                    break;
                case 2: // vinyl
                    C(p, c.x, c.y, s * 0.34f, Hex("0F0E14"));
                    Rn(p, c.x, c.y, s * 0.24f, 1, A(Color.white, 0.12f));
                    Rn(p, c.x, c.y, s * 0.29f, 1, A(Color.white, 0.1f));
                    C(p, c.x, c.y, s * 0.11f, a);
                    C(p, c.x, c.y, s * 0.025f, b);
                    break;
                default: // waveform
                    for (int i = 0; i < 9; i++)
                    {
                        float bh = h * (0.1f + 0.5f * Mathf.Abs(Mathf.Sin(i * 1.3f)));
                        R(p, x + w * 0.1f + i * w * 0.09f, c.y - bh / 2, w * 0.05f, bh, 1, i % 2 == 0 ? a : b);
                    }
                    break;
            }
            // tape corners
            foreach (var corner in new[] { new Vector2(x + 4, y + h - 2), new Vector2(x + w - 4, y + h - 2) })
                Sh(p, Box(corner, new Vector2(9, 4), 1, 35), Hex("E8E2C8", 0.75f), corner.x - 12, corner.y - 12, 24, 24);
        }

        public enum Album { DarkTrap, FuturisticHipHop, UndergroundRap, Electronic, Ambient, Experimental, OldSchool, Distorted, ColorfulSynth }

        /// <summary>
        /// Original fictional album cover (square, bottom-left x,y, size s). frame: 0 taped poster, 1 black frame, 2 gold frame.
        /// Title/artist are abstract "type" bars — no real albums, names or logos.
        /// </summary>
        static void AlbumCover(SdfPainter p, float x, float y, float s, Album design, int frame)
        {
            if (frame > 0)
            {
                R(p, x - 7 + 3, y - 7 - 3, s + 14, s + 14, 2, A(Color.black, 0.35f));
                R(p, x - 7, y - 7, s + 14, s + 14, 2, frame == 2 ? Hex("C9A23A") : Hex("0C0B10"));
                if (frame == 2) R(p, x - 4, y - 4, s + 8, s + 8, 1, Hex("8A6A1E"));
            }
            else if (frame == 0) R(p, x + 3, y - 3, s, s, 1, A(Color.black, 0.35f));

            float cx = x + s / 2, cy = y + s / 2;
            var rnd = new System.Random((int)design * 31 + (int)x);
            Color white = Hex("F4F1EA");
            switch (design)
            {
                case Album.DarkTrap:
                    R(p, x, y, s, s, 0, Hex("0B0A0E"));
                    Light(p, cx, cy + s * 0.08f, s * 0.42f, Hex("FF2A3D", 0.45f));
                    C(p, cx, cy + s * 0.08f, s * 0.2f, Hex("C8102E"));
                    for (int i = 0; i < 7; i++) Rn(p, x + s * 0.12f + i * s * 0.12f, y + s * 0.3f + Mathf.Sin(i) * 2, s * 0.05f, 1.6f, Hex("8A8E9A"));
                    R(p, x + s * 0.1f, y + s * 0.84f, s * 0.5f, s * 0.06f, 1, white);
                    break;
                case Album.FuturisticHipHop:
                    RG(p, x, y, s, s, 0, Hex("1A0B3A"), Hex("6C3BD9"));
                    C(p, cx, cy + s * 0.05f, s * 0.24f, Hex("3DE1FF"));
                    C(p, cx - s * 0.06f, cy + s * 0.11f, s * 0.08f, A(Color.white, 0.6f));
                    R(p, x, y, s, s * 0.38f, 0, Hex("120828"));
                    for (int i = 0; i < 5; i++) R(p, x, y + s * 0.06f + i * s * 0.065f, s, 1.2f, 0, Hex("FF3DC8", 0.8f));
                    for (int i = 0; i < 7; i++) L(p, cx, y + s * 0.38f, x + i * s / 6f, y, 0.6f, Hex("FF3DC8", 0.6f));
                    R(p, x + s * 0.08f, y + s * 0.86f, s * 0.6f, s * 0.05f, 1, white);
                    break;
                case Album.UndergroundRap:
                    R(p, x, y, s, s, 0, Hex("6E6C72"));
                    for (int i = 0; i < 40; i++) C(p, x + (float)rnd.NextDouble() * s, y + (float)rnd.NextDouble() * s, 1 + (float)rnd.NextDouble() * 2, A(Color.black, 0.18f));
                    for (int i = 0; i < 6; i++)
                        L(p, x + s * (0.15f + i * 0.12f), y + s * (0.45f + 0.15f * Mathf.Sin(i * 2.1f)), x + s * (0.25f + i * 0.12f), y + s * (0.55f + 0.12f * Mathf.Cos(i * 1.7f)), s * 0.03f, Hex("FF3DC8"));
                    L(p, x + s * 0.3f, y + s * 0.42f, x + s * 0.3f, y + s * 0.3f, 1, Hex("FF3DC8"));
                    R(p, x + s * 0.1f, y + s * 0.1f, s * 0.8f, s * 0.12f, 0, Hex("0B0A0E"));
                    R(p, x + s * 0.16f, y + s * 0.14f, s * 0.4f, s * 0.04f, 0, white);
                    break;
                case Album.Electronic:
                    R(p, x, y, s, s, 0, Hex("061A3A"));
                    for (int i = 6; i > 0; i--) Rn(p, cx + i * 1.5f, cy - i, s * 0.06f * i, 1.4f, i % 2 == 0 ? Hex("3DE1FF") : Hex("4DFFA8"));
                    R(p, x + s * 0.1f, y + s * 0.08f, s * 0.35f, s * 0.05f, 1, Hex("3DE1FF"));
                    break;
                case Album.Ambient:
                    RG(p, x, y, s, s, 0, Hex("F2C6D8"), Hex("BFD8F2"));
                    Light(p, x + s * 0.35f, y + s * 0.6f, s * 0.35f, Hex("FFFFFF", 0.7f));
                    Light(p, x + s * 0.65f, y + s * 0.35f, s * 0.3f, Hex("B48CFF", 0.5f));
                    R(p, x + s * 0.1f, y + s * 0.1f, s * 0.3f, s * 0.03f, 1, Hex("5A5468"));
                    break;
                case Album.Experimental:
                    R(p, x, y, s, s, 0, Hex("EDE8DC"));
                    C(p, cx, cy, s * 0.28f, Hex("1A1626"));
                    for (int i = 0; i < 7; i++)
                    {
                        float by = y + s * (0.15f + i * 0.11f), off = (float)(rnd.NextDouble() - 0.5) * s * 0.25f;
                        R(p, x + s * 0.2f + off, by, s * 0.6f, s * 0.05f, 0, i % 3 == 0 ? Hex("FF3D5A") : i % 3 == 1 ? Hex("3DE1FF") : Hex("1A1626", 0.6f));
                    }
                    break;
                case Album.OldSchool:
                    R(p, x, y, s, s, 0, Hex("FFD23D"));
                    R(p, x + s * 0.18f, y + s * 0.28f, s * 0.64f, s * 0.36f, 4, Hex("15131B"));
                    C(p, x + s * 0.32f, y + s * 0.44f, s * 0.1f, Hex("8A8E9A"));
                    C(p, x + s * 0.68f, y + s * 0.44f, s * 0.1f, Hex("8A8E9A"));
                    R(p, x + s * 0.43f, y + s * 0.5f, s * 0.14f, s * 0.07f, 1, Hex("FF3D5A"));
                    L(p, x + s * 0.25f, y + s * 0.64f, x + s * 0.35f, y + s * 0.76f, 1.5f, Hex("15131B"));
                    L(p, x + s * 0.75f, y + s * 0.64f, x + s * 0.65f, y + s * 0.76f, 1.5f, Hex("15131B"));
                    L(p, x + s * 0.35f, y + s * 0.76f, x + s * 0.65f, y + s * 0.76f, 1.5f, Hex("15131B"));
                    R(p, x + s * 0.12f, y + s * 0.08f, s * 0.76f, s * 0.1f, 1, Hex("FF3D5A"));
                    break;
                case Album.Distorted:
                    R(p, x, y, s, s, 0, Hex("140A0E"));
                    C(p, cx, cy + s * 0.1f, s * 0.17f, Hex("2E1A22"));
                    p.Fill(Ellipse(new Vector2(cx, y), new Vector2(s * 0.32f, s * 0.3f)), Hex("2E1A22"), Bnd(x, y, s, s * 0.35f));
                    for (float yy = y + 2; yy < y + s; yy += 4) R(p, x, yy, s, 1.2f, 0, Hex("FF2A3D", 0.25f));
                    R(p, x + s * 0.3f, cy + s * 0.12f, s * 0.4f, s * 0.05f, 0, Hex("FF2A3D"));
                    break;
                default: // ColorfulSynth
                    string[] cols = { "2A0E4A", "6C3BD9", "FF3DC8", "FF8A3D", "FFD23D" };
                    for (int i = 0; i < cols.Length; i++) R(p, x, y + i * s / 5f, s, s / 5f + 0.5f, 0, Hex(cols[i]));
                    Pg(p, Hex("0B0A14"), new Vector2(x + s * 0.2f, y), new Vector2(cx, y + s * 0.55f), new Vector2(x + s * 0.8f, y));
                    R(p, x + s * 0.1f, y + s * 0.86f, s * 0.45f, s * 0.05f, 1, white);
                    break;
            }
            if (frame == 0)
                foreach (var corner in new[] { new Vector2(x + 4, y + s - 2), new Vector2(x + s - 4, y + s - 2) })
                    Sh(p, Box(corner, new Vector2(9, 4), 1, 35), Hex("E8E2C8", 0.75f), corner.x - 12, corner.y - 12, 24, 24);
        }

        public static string AlbumCoverPath(string id) => $"{ArtRoot}/Albums/cover_{id}.png";

        /// <summary>Standalone album cover (384×384) for an album asset, styled by genre.</summary>
        public static SdfPainter AlbumCoverArt(AlbumGenre genre, int seed)
        {
            var p = new SdfPainter(384, 384);
            Album design = genre switch
            {
                AlbumGenre.Underground => Album.UndergroundRap,
                AlbumGenre.Trap => Album.DarkTrap,
                AlbumGenre.Rage => Album.Experimental,
                AlbumGenre.Drill => Album.Distorted,
                AlbumGenre.RnB => Album.Ambient,
                _ => Album.Electronic,
            };
            AlbumCover(p, 0, 0, 384, design, -1);
            // fictional "parental advisory"-free label strip + catalog number blocks (no real marks)
            R(p, 16, 16, 90, 22, 3, A(Color.black, 0.55f));
            for (int i = 0; i < 3; i++) R(p, 22 + i * 26, 22, 18, 10, 2, A(Color.white, 0.7f - i * 0.15f));
            var rnd = new System.Random(seed);
            for (int i = 0; i < 30; i++) C(p, (float)rnd.NextDouble() * 384, (float)rnd.NextDouble() * 384, 0.8f, A(Color.white, 0.08f));
            return p;
        }

        /// <summary>Spray-paint graffiti tag (abstract strokes with drips).</summary>
        static void Graffiti(SdfPainter p, float x, float y, float w, Color c)
        {
            float px = x, py = y;
            for (int i = 0; i < 10; i++)
            {
                float nx = x + w * (i + 1) / 10f, ny = y + 18 * Mathf.Sin(i * 1.9f) + (i % 3 == 0 ? 10 : 0);
                L(p, px, py, nx, ny, 4.2f, c);
                if (i % 3 == 1) L(p, nx, ny - 2, nx, ny - 10 - i, 1.3f, A(c, 0.8f)); // drip
                px = nx; py = ny;
            }
            L(p, x - 4, y - 14, x + w + 6, y - 10, 2.5f, A(Color.white, 0.8f)); // underline
        }

        static void Bottle(SdfPainter p, float x, float y, Color c)
        {
            R(p, x, y, 10, 22, 3, c);
            R(p, x + 3, y + 20, 4, 9, 1, c);
            R(p, x + 2, y + 6, 6, 8, 1, A(Color.white, 0.5f));
        }

        static void Sticker(SdfPainter p, float x, float y, float r, Color c, int kind)
        {
            switch (kind % 3)
            {
                case 0: C(p, x, y, r, c); C(p, x, y, r * 0.45f, A(Color.white, 0.85f)); break;
                case 1: Sh(p, Star(new Vector2(x, y), r, r * 0.45f), c, x - r, y - r, r * 2, r * 2); break;
                default: Sh(p, Box(new Vector2(x, y), new Vector2(r, r * 0.6f), r * 0.3f, 12), c, x - r - 3, y - r - 3, r * 2 + 6, r * 2 + 6); break;
            }
        }

        static void Plant(SdfPainter p, float x, float y, float s, Color pot)
        {
            R(p, x - 10 * s, y, 20 * s, 18 * s, 4 * s, pot);
            Color leaf = Hex("2FA36B"), leaf2 = Hex("1F7A51");
            for (int i = 0; i < 7; i++)
            {
                float a = (40 + i * 17) * Mathf.Deg2Rad;
                var tip = new Vector2(x + Mathf.Cos(a) * 30 * s, y + 16 * s + Mathf.Sin(a) * 34 * s);
                L(p, x, y + 16 * s, tip.x, tip.y, 4.5f * s, i % 2 == 0 ? leaf : leaf2);
            }
        }

        static void Desk(SdfPainter p, float x0, float x1, float top, Color wood, Color legs)
        {
            R(p, x0, top - 12, x1 - x0, 12, 3, wood);
            R(p, x0, top - 3, x1 - x0, 3, 1, A(Color.white, 0.1f));
            R(p, x0 + 8, 40, 9, top - 52, 2, legs);
            R(p, x1 - 17, 40, 9, top - 52, 2, legs);
            R(p, x0 + 8, 40, x1 - x0 - 16, 5, 2, A(legs, 0.6f));
        }

        static void Floor(SdfPainter p, float horizon, Color near, Color far) =>
            p.Fill(RRect(0, 0, SceneW, horizon, 0), VGradient(0, near, horizon, far), new RectInt(0, 0, SceneW, (int)horizon + 2));

        static void FloorShadow(SdfPainter p, float x, float y, float rx, float ry, float alpha) =>
            LightEllipse(p, x, y, rx, ry, new Color(0, 0, 0, alpha));

        // ================================================================== ROOMS

        static SdfPainter Environment(int tier)
        {
            var room = new SdfPainter(SceneW, DrawH);
            switch (tier)
            {
                case 0: Basement(room); break;
                case 1: Bedroom(room); break;
                case 2: SmallPro(room); break;
                case 3: Professional(room); break;
                case 4: Modern(room); break;
                case 5: Luxury(room); break;
                case 6: Major(room); break;
                case 7: Elite(room); break;
                case 8: RecordLabel(room); break;
                default: Mega(room); break;
            }
            if (TierMirrored[tier])
            {
                // mirrored floor plan: desk on the right, lounge/window on the left
                var flipped = new SdfPainter(SceneW, DrawH);
                for (int y = 0; y < DrawH; y++)
                    for (int x = 0; x < SceneW; x++)
                        flipped.SetPixel(SceneW - 1 - x, y, room.GetPixel(x, y));
                room = flipped;
            }

            // Lift the room and extend the floor toward the viewer (darker in front).
            var p = new SdfPainter(SceneW, SceneH);
            p.Blit(room, 0, Lift);
            // average of the bottom row: details on the floor's edge (veins, cables) must not stretch into streaks
            Color avg = Color.black;
            for (int x = 0; x < SceneW; x++) avg += room.GetPixel(x, 1);
            avg /= SceneW;
            for (int y = 0; y < Lift; y++)
            {
                float k = Mathf.Lerp(0.55f, 1f, y / (float)Lift);
                for (int x = 0; x < SceneW; x++)
                {
                    var c = Color.Lerp(avg, room.GetPixel(x, 1), 0.35f);
                    p.SetPixel(x, y, new Color(c.r * k, c.g * k, c.b * k, 1f));
                }
            }
            // extend the ceiling upward (only visible on narrow phones)
            for (int y = Lift + DrawH; y < SceneH; y++)
            {
                float k = Mathf.Lerp(1f, 0.6f, (y - Lift - DrawH) / (float)(SceneH - Lift - DrawH));
                for (int x = 0; x < SceneW; x++)
                {
                    var c = room.GetPixel(x, DrawH - 1);
                    p.SetPixel(x, y, new Color(c.r * k, c.g * k, c.b * k, 1f));
                }
            }
            // soft shadows where the producer stations stand
            foreach (float cx in StationCenters) FloorShadow(p, cx - 10, 16, 120, 14, 0.35f);
            return p;
        }

        /// <summary>Tier 0: underground basement — concrete, exposed pipes and cables, cheap gear, rough but cool.</summary>
        static void Basement(SdfPainter p)
        {
            var rnd = new System.Random(7);
            const float horizon = 150;
            p.VerticalGradient(Hex("23242B"), Hex("191A20"));

            // cinder block wall
            for (int row = 0; row * 40 + horizon < 500; row++)
            {
                float y = horizon + row * 40;
                float off = row % 2 == 0 ? 0 : -45;
                for (float x = off; x < SceneW; x += 90)
                {
                    float tint = (float)rnd.NextDouble() * 0.05f;
                    R(p, x + 2, y + 2, 86, 36, 2, new Color(0.17f + tint, 0.175f + tint, 0.2f + tint));
                    if (rnd.NextDouble() < 0.18) C(p, x + 20 + (float)rnd.NextDouble() * 50, y + 10 + (float)rnd.NextDouble() * 20, 1.5f + (float)rnd.NextDouble() * 2, A(Color.black, 0.25f));
                }
            }
            // damp stains
            LightEllipse(p, 120, 200, 90, 60, Hex("0E1210", 0.35f));
            LightEllipse(p, 900, 330, 80, 120, Hex("0E1210", 0.3f));

            // ceiling: beam, pipes, cables
            R(p, 0, 500, SceneW, 60, 0, Hex("121218"));
            R(p, 0, 496, SceneW, 8, 0, Hex("2A2A33"));
            L(p, 0, 520, 1000, 520, 9, Hex("4A4E58"));
            L(p, 0, 520, 1000, 520, 3, A(Color.white, 0.08f));
            L(p, 0, 542, 1000, 542, 6, Hex("6B4A2E"));
            for (float x = 80; x < 1000; x += 220) R(p, x, 510, 14, 22, 2, Hex("5A5E68"));
            foreach (var (a, b, sag, col) in new[] { (60f, 300f, 34f, "15151A"), (260f, 560f, 26f, "2A1F3D"), (540f, 980f, 40f, "15151A") })
            {
                var c = new Vector2((a + b) / 2, 496 - sag * 0.2f);
                float r = Mathf.Abs(b - a) * 0.75f;
                p.Fill(Intersect(Ring(new Vector2(c.x, 496 + r - sag), r, 3), RRect(a, 440, b - a, 60)), Hex(col), Bnd(a, 440, b - a, 60));
            }
            // cable down the wall to the desk
            L(p, 470, 496, 470, 260, 2.5f, Hex("15151A"));
            L(p, 478, 496, 478, 300, 2.5f, Hex("2A1F3D"));
            R(p, 462, 300, 24, 16, 3, Hex("D9D6CC"));

            // small high window (street at night)
            RG(p, 780, 420, 140, 62, 3, Hex("1B2A4A"), Hex("0E1428"));
            for (int i = 0; i < 6; i++) R(p, 790 + i * 22, 424, 14, 6 + (i * 7) % 18, 1, Hex("FFD27A", 0.35f));
            for (float x = 800; x < 920; x += 24) R(p, x, 420, 4, 62, 1, Hex("2A2A33"));
            R(p, 774, 414, 152, 8, 2, Hex("33343D"));
            LightEllipse(p, 850, 300, 120, 170, Hex("6FA8FF", 0.08f));

            // bare bulb
            L(p, 640, 496, 640, 452, 1.4f, Hex("15151A"));
            R(p, 634, 446, 12, 8, 2, Hex("3A3A40"));
            C(p, 640, 438, 9, Hex("FFE6A8"));
            Light(p, 640, 438, 210, Hex("FFC870", 0.22f));

            // posters + stickers
            AlbumCover(p, 556, 312, 86, Album.UndergroundRap, 0);
            AlbumCover(p, 662, 322, 72, Album.DarkTrap, 0);
            AlbumCover(p, 26, 330, 70, Album.Distorted, 0);
            Graffiti(p, 24, 268, 104, Hex("FF3DC8"));
            Graffiti(p, 560, 430, 150, Hex("3DE1FF", 0.85f));
            Sticker(p, 520, 380, 9, Hex("FFD23D"), 1);
            Sticker(p, 744, 300, 8, Hex("3DE1FF"), 0);
            Sticker(p, 126, 300, 9, Hex("FF3DC8"), 2);

            // LED strip (glow is animated in the scene)
            R(p, 90, 465, 670, 5, 2, Hex("FF9BE6"));

            // floor: concrete + old rug
            Floor(p, horizon, Hex("121216"), Hex("1E1F25"));
            R(p, 0, horizon - 3, SceneW, 4, 0, Hex("0C0C10"));
            LightEllipse(p, 300, 90, 120, 26, Hex("000000", 0.25f));
            p.Fill(Ellipse(new Vector2(330, 70), new Vector2(230, 42)), Hex("3A2550"), Bnd(100, 28, 460, 84));
            p.Fill(Intersect(Ellipse(new Vector2(330, 70), new Vector2(216, 34)), q => -Mathf.Abs(q.y - 70) + 3), Hex("FF3DC8", 0.25f), Bnd(100, 28, 460, 84));
            // extension cord on the floor
            L(p, 140, 40, 300, 30, 2, Hex("D9D6CC"));
            R(p, 296, 24, 40, 14, 3, Hex("E8E4D8"));
            C(p, 308, 31, 2, Hex("2A2A33"));
            C(p, 322, 31, 2, Hex("2A2A33"));

            // cardboard boxes (back right)
            R(p, 800, 140, 90, 70, 3, Hex("7A5A3A"));
            R(p, 800, 172, 90, 6, 0, Hex("C9B48A", 0.6f));
            R(p, 892, 140, 80, 110, 3, Hex("6B4E32"));
            R(p, 820, 210, 64, 52, 3, Hex("8A6A46"));
            R(p, 820, 232, 64, 5, 0, Hex("C9B48A", 0.6f));
            // flight case with a hoodie thrown over it, bottles and cans on the box
            R(p, 892, 250, 80, 38, 3, Hex("17161C"));
            foreach (var cx in new[] { 894f, 962f }) { R(p, cx, 250, 8, 8, 1, Hex("8A8E9A")); R(p, cx, 280, 8, 8, 1, Hex("8A8E9A")); }
            R(p, 920, 265, 24, 6, 2, Hex("8A8E9A"));
            p.Fill(Union(Ellipse(new Vector2(944, 290), new Vector2(30, 8)), Capsule(new Vector2(962, 288), new Vector2(970, 258), 7)), Hex("5A3A7A"), Bnd(900, 248, 84, 54));
            Bottle(p, 828, 262, Hex("2FA36B", 0.9f));
            Bottle(p, 844, 262, Hex("8A5A2E", 0.9f));
            R(p, 862, 262, 10, 15, 2, Hex("FF3DC8"));

            // old desk + gear
            Desk(p, 130, 480, 198, Hex("5A4330"), Hex("2A2018"));
            R(p, 140, 160, 60, 26, 2, Hex("4A3628"));                // drawer
            R(p, 164, 170, 12, 3, 1, Hex("1A1410"));
            R(p, 410, 60, 52, 120, 4, Hex("1A1922"));                // cheap PC tower under the desk
            C(p, 436, 160, 3, Hex("4DFFA8"));
            R(p, 418, 120, 36, 3, 1, Hex("2A2834"));
            R(p, 418, 112, 36, 3, 1, Hex("2A2834"));
            var s = TierScreens[0][0];
            Light(p, s.center.x, s.center.y, 210, Hex("7A5CFF", 0.16f));
            Monitor(p, s, 198, true);
            foreach (var cone in TierCones[0]) SpeakerBox(p, cone, 7, Hex("1C1B22"), Hex("33313D"));
            MidiKeys(p, 196, 198, 120, 15, Hex("2A2834"), false);
            PcKeyboard(p, 324, 198, 64, Hex("2C2A33"), false);
            // headphones hanging on a nail
            C(p, 440, 352, 2, Hex("8A8A90"));
            p.Fill(Arc(new Vector2(440, 330), 18, 4, 0, 180), Hex("2A2A33"), Bnd(418, 320, 44, 34));
            R(p, 418, 310, 10, 22, 4, Hex("2A2A33"));
            R(p, 452, 310, 10, 22, 4, Hex("2A2A33"));
            // cheap USB mic on a tripod
            L(p, 146, 198, 140, 210, 1.5f, Hex("2A2A33"));
            L(p, 146, 198, 152, 210, 1.5f, Hex("2A2A33"));
            L(p, 146, 210, 146, 226, 2, Hex("2A2A33"));
            p.Fill(Capsule(new Vector2(146, 230), new Vector2(146, 250), 7), Hex("3A3946"), Bnd(136, 220, 20, 40));
            for (float y = 236; y < 252; y += 4) L(p, 140, y, 152, y, 0.7f, Hex("5A5966"));
            // clutter: energy can, papers, sticky notes
            R(p, 290, 198, 10, 16, 2, Hex("3DE1FF"));
            R(p, 291, 210, 8, 2, 1, Hex("D9DEE8"));
            Sh(p, Box(new Vector2(240, 380), new Vector2(9, 9), 1, 6), Hex("FFD23D"), 228, 368, 24, 24);
            Sh(p, Box(new Vector2(262, 384), new Vector2(9, 9), 1, -8), Hex("FF8AD8"), 250, 372, 24, 24);
            Sh(p, Box(new Vector2(380, 384), new Vector2(9, 9), 1, 4), Hex("4DFFA8"), 368, 372, 24, 24);
        }

        /// <summary>Tier 1: bedroom studio — purple walls, city window, bed, fairy lights.</summary>
        static void Bedroom(SdfPainter p)
        {
            const float horizon = 150;
            p.VerticalGradient(Hex("2B1F48"), Hex("1C1433"));
            // wall panels
            for (float x = 0; x < SceneW; x += 125) R(p, x, horizon, 2, 360, 0, A(Color.black, 0.12f));
            R(p, 0, 510, SceneW, 50, 0, Hex("150F27"));

            // window with city
            RG(p, 620, 290, 170, 170, 4, Hex("3A2A6E"), Hex("0E0A24"));
            var rnd = new System.Random(3);
            for (float x = 626; x < 780; x += 18)
            {
                float bh = 30 + (float)rnd.NextDouble() * 90;
                R(p, x, 290, 16, bh, 0, Hex("120D26"));
                for (float wy = 298; wy < 290 + bh - 6; wy += 10)
                    if (rnd.NextDouble() < 0.4) R(p, x + 4, wy, 4, 4, 0, Hex("FFD27A", 0.8f));
            }
            C(p, 750, 430, 12, Hex("F4EED8"));
            R(p, 702, 290, 6, 170, 0, Hex("2A1F48"));
            R(p, 620, 372, 170, 6, 0, Hex("2A1F48"));
            R(p, 612, 284, 186, 8, 2, Hex("3A2C5E"));
            RG(p, 586, 280, 36, 196, 6, Hex("6A2C66"), Hex("8A3A86"));
            RG(p, 788, 280, 36, 196, 6, Hex("6A2C66"), Hex("8A3A86"));

            // posters + shelf
            AlbumCover(p, 52, 368, 92, Album.ColorfulSynth, 0);
            AlbumCover(p, 468, 386, 74, Album.FuturisticHipHop, 0);
            AlbumCover(p, 170, 404, 54, Album.Ambient, 0);
            // acoustic foam panels above the desk
            foreach (var (px, py) in new[] { (396f, 300f), (396f, 350f) })
            {
                R(p, px, py, 44, 44, 3, Hex("2A1F48"));
                for (int k = 0; k < 4; k++) R(p, px + 3 + k * 10, py + 3, 8, 38, 2, Hex("3A2C62"));
            }
            // hoodie hanging on a hook
            C(p, 560, 372, 3, Hex("D9DEE8"));
            Pg(p, Hex("2EA56F"), new Vector2(540, 370), new Vector2(580, 370), new Vector2(584, 300), new Vector2(536, 300));
            p.Fill(Ellipse(new Vector2(560, 370), new Vector2(14, 6)), Hex("1F7A51"), Bnd(544, 362, 32, 16));
            L(p, 556, 362, 554, 336, 1.2f, Hex("F4F4F6"));
            L(p, 564, 362, 566, 340, 1.2f, Hex("F4F4F6"));
            R(p, 860, 380, 120, 8, 2, Hex("5A3F2E"));
            for (int i = 0; i < 6; i++) R(p, 866 + i * 9, 388, 7, 34 + (i % 3) * 4, 1, Color.HSVToRGB(0.75f + i * 0.04f, 0.6f, 0.7f));
            Plant(p, 950, 388, 0.8f, Hex("D9D6CC"));

            // fairy lights
            for (float x = 20; x < 980; x += 26)
            {
                float y = 490 - 14 * Mathf.Sin(Mathf.Repeat(x / 240f, 1f) * Mathf.PI);
                Light(p, x, y, 12, Hex("FFD27A", 0.5f));
                C(p, x, y, 3, Hex("FFF1C8"));
            }

            // floor: wood + rug + bed
            Floor(p, horizon, Hex("2A1C1E"), Hex("3B2A2E"));
            for (float y = 18; y < horizon; y += 22) R(p, 0, y, SceneW, 1.5f, 0, A(Color.black, 0.2f));
            p.Fill(Ellipse(new Vector2(320, 70), new Vector2(240, 44)), Hex("4A2C7A"), Bnd(80, 26, 480, 88));
            p.Fill(Ellipse(new Vector2(320, 70), new Vector2(200, 34)), Hex("5C3A92"), Bnd(80, 26, 480, 88));
            R(p, 780, 120, 220, 70, 8, Hex("3A2C5E"));
            R(p, 780, 176, 220, 30, 10, Hex("D9D3EA"));
            R(p, 800, 196, 70, 30, 12, Hex("F4F1FA"));
            R(p, 960, 120, 40, 140, 6, Hex("2A1F48"));

            // desk + gear
            Desk(p, 120, 486, 198, Hex("E4DED2"), Hex("B8B2A6"));
            var s = TierScreens[1][0];
            Light(p, s.center.x, s.center.y, 240, Hex("9B5CFF", 0.2f));
            Monitor(p, s, 198, false);
            foreach (var cone in TierCones[1]) SpeakerBox(p, cone, 8, Hex("17151F"), Hex("FF3DC8"));
            MidiKeys(p, 182, 198, 150, 21, Hex("1C1A26"), true);
            PcKeyboard(p, 338, 198, 62, Hex("E4E0EA"), true);
            // boom arm mic with pop filter
            L(p, 112, 200, 112, 300, 3, Hex("2A2834"));
            L(p, 112, 300, 160, 340, 3, Hex("2A2834"));
            L(p, 160, 340, 170, 310, 3, Hex("2A2834"));
            p.Fill(Capsule(new Vector2(170, 290), new Vector2(170, 306), 8), Hex("3A3946"), Bnd(160, 280, 20, 36));
            Rn(p, 186, 296, 12, 2, Hex("2A2834"));
            C(p, 186, 296, 11, Hex("0E0C14", 0.5f));
            // lamp
            R(p, 450, 198, 22, 6, 2, Hex("2A2834"));
            L(p, 461, 204, 461, 240, 2, Hex("2A2834"));
            Pg(p, Hex("FF6FB5"), new Vector2(446, 240), new Vector2(476, 240), new Vector2(468, 262), new Vector2(454, 262));
            Light(p, 461, 236, 90, Hex("FF6FB5", 0.25f));
        }

        /// <summary>Tier 2: professional studio — acoustic foam, console, vocal booth, warm spots.</summary>
        static void Professional(SdfPainter p)
        {
            const float horizon = 150;
            p.VerticalGradient(Hex("201A24"), Hex("15111A"));

            // acoustic foam panels
            for (float x = 10; x < 990; x += 96)
            for (float y = 250; y < 500; y += 96)
            {
                if (x > 610 && y < 480) continue; // booth window area
                R(p, x, y, 88, 88, 4, Hex("2A2030"));
                for (int k = 0; k < 4; k++)
                {
                    bool vertical = ((int)(x / 96) + (int)(y / 96)) % 2 == 0;
                    if (vertical) R(p, x + 6 + k * 21, y + 6, 14, 76, 3, Hex("33283A"));
                    else R(p, x + 6, y + 6 + k * 21, 76, 14, 3, Hex("33283A"));
                }
            }
            // wood slat diffuser band
            for (float x = 0; x < SceneW; x += 14) R(p, x, horizon + 4, 10, 92, 1, Hex("5A3F2E"));
            R(p, 0, 500, SceneW, 60, 0, Hex("0F0C12"));
            foreach (float x in new[] { 160f, 420f, 700f, 900f })
            {
                R(p, x - 10, 508, 20, 10, 3, Hex("2A2834"));
                LightEllipse(p, x, 420, 90, 110, Hex("FFC870", 0.12f));
            }

            // vocal booth (window)
            RG(p, 630, 240, 340, 236, 6, Hex("0E2238"), Hex("17344F"));
            for (float x = 650; x < 960; x += 40) R(p, x, 250, 30, 200, 3, Hex("123049", 0.8f));
            L(p, 800, 250, 800, 380, 3, Hex("1A1A22"));
            L(p, 780, 252, 820, 252, 3, Hex("1A1A22"));
            p.Fill(Capsule(new Vector2(800, 392), new Vector2(800, 420), 12), Hex("C9CCD6"), Bnd(780, 370, 40, 70));
            for (float y = 396; y < 424; y += 5) L(p, 789, y, 811, y, 0.8f, Hex("8A8E9A"));
            Rn(p, 782, 404, 16, 2, Hex("1A1A22"));
            LightEllipse(p, 800, 420, 120, 80, Hex("3DE1FF", 0.14f));
            R(p, 622, 232, 356, 10, 3, Hex("2A2030"));
            R(p, 622, 474, 356, 10, 3, Hex("2A2030"));
            R(p, 622, 232, 10, 252, 3, Hex("2A2030"));
            R(p, 968, 232, 10, 252, 3, Hex("2A2030"));
            Pg(p, A(Color.white, 0.05f), new Vector2(660, 476), new Vector2(720, 476), new Vector2(660, 300));
            // framed fictional album artwork on the foam wall
            AlbumCover(p, 40, 392, 68, Album.OldSchool, 2);
            AlbumCover(p, 140, 402, 58, Album.Electronic, 1);
            AlbumCover(p, 436, 384, 68, Album.Experimental, 1);
            AlbumCover(p, 530, 398, 58, Album.DarkTrap, 2);
            C(p, 952, 460, 5, Hex("FF3D5A"));
            Light(p, 952, 460, 20, Hex("FF3D5A", 0.5f));

            // floor + rug
            Floor(p, horizon, Hex("16121A"), Hex("231C26"));
            p.Fill(RRect(80, 24, 520, 80, 10), Hex("3A1F2E"), Bnd(80, 24, 520, 80));
            p.Fill(RRect(96, 34, 488, 60, 8), Hex("4A2838"), Bnd(96, 34, 488, 60));

            // console desk + rack
            Desk(p, 100, 500, 198, Hex("2A2532"), Hex("15131A"));
            RG(p, 120, 186, 360, 12, 2, Hex("3A3446"), Hex("4A4458"));
            R(p, 130, 60, 110, 120, 4, Hex("15131A"));
            for (int i = 0; i < 5; i++)
            {
                R(p, 136, 66 + i * 22, 98, 18, 2, Hex("23202B"));
                for (int k = 0; k < 6; k++) C(p, 146 + k * 8, 75 + i * 22, 1.6f, k < 3 + i % 3 ? Hex("4DFFA8") : Hex("3A3646"));
                R(p, 200, 72 + i * 22, 26, 6, 2, Hex("0B0A10"));
            }
            var s = TierScreens[3];
            Light(p, s[0].center.x, s[0].center.y, 260, Hex("7A5CFF", 0.18f));
            Monitor(p, s[0], 198, false);
            Monitor(p, s[1], 198, false);
            foreach (var cone in TierCones[3]) SpeakerBox(p, cone, 8, Hex("1A1822"), Hex("FFD23D"));
            MidiKeys(p, 170, 198, 170, 24, Hex("1C1A26"), true);
            PcKeyboard(p, 346, 198, 60, Hex("2A2834"), true);
            // couch at the back right (below the booth window)
            R(p, 640, 140, 300, 60, 14, Hex("5A2C3E"));
            R(p, 640, 190, 300, 40, 14, Hex("6A3448"));
            Plant(p, 590, 150, 1.2f, Hex("3A3446"));
        }

        /// <summary>Tier 3: modern recording studio — neon lines, glass, gold records, big screens.</summary>
        static void Modern(SdfPainter p)
        {
            const float horizon = 150;
            p.VerticalGradient(Hex("10132A"), Hex("0A0B1A"));
            // panels with neon seams
            for (float x = 0; x < SceneW; x += 160)
            {
                RG(p, x + 4, horizon + 4, 152, 360, 6, Hex("141833"), Hex("1A1F40"));
                R(p, x, horizon, 3, 370, 1, Hex("3DE1FF", 0.5f));
                LightEllipse(p, x + 1, 340, 14, 200, Hex("3DE1FF", 0.18f));
            }
            R(p, 0, 516, SceneW, 44, 0, Hex("07070F"));
            for (float x = 60; x < SceneW; x += 180) { R(p, x, 512, 100, 6, 3, Hex("E8F6FF")); LightEllipse(p, x + 50, 500, 110, 40, Hex("BFE8FF", 0.2f)); }

            // gold & platinum records
            float[] recX = { 230, 330, 430 };
            for (int i = 0; i < 3; i++)
            {
                R(p, recX[i] - 40, 400, 80, 96, 3, Hex("0E0D16"));
                C(p, recX[i], 456, 28, i == 1 ? Hex("D9DEE8") : Hex("F2C14E"));
                Rn(p, recX[i], 456, 20, 1, A(Color.black, 0.25f));
                C(p, recX[i], 456, 9, Hex("6C3BD9"));
                R(p, recX[i] - 26, 410, 52, 10, 2, Hex("F2C14E", 0.6f));
            }
            AlbumCover(p, 484, 304, 60, Album.Ambient, 1);
            // floor-to-ceiling window: city at sunset (the first "view" of the career)
            RG(p, 620, 200, 370, 300, 4, Hex("FF8A5C"), Hex("5A2A7A"));
            LightEllipse(p, 800, 240, 260, 90, Hex("FFC36B", 0.5f));
            C(p, 800, 262, 46, Hex("FFD08A"));
            var rnd = new System.Random(55);
            for (float x = 624; x < 986; x += 18 + (float)rnd.NextDouble() * 16)
            {
                float w = 16 + (float)rnd.NextDouble() * 22, h = 30 + (float)rnd.NextDouble() * 150;
                R(p, x, 200, Mathf.Min(w, 986 - x), h, 0, Hex("2A1636"));
                for (float wy = 208; wy < 200 + h - 6; wy += 9)
                    for (float wx = x + 3; wx < x + w - 4 && wx < 982; wx += 7)
                        if (rnd.NextDouble() < 0.25) R(p, wx, wy, 3, 4, 0, Hex("FFC36B", 0.8f));
            }
            for (float x = 620; x <= 990; x += 92) R(p, x, 200, 5, 300, 0, Hex("0A0B1A"));
            R(p, 616, 494, 378, 8, 0, Hex("0A0B1A"));
            Pg(p, A(Color.white, 0.07f), new Vector2(650, 494), new Vector2(720, 494), new Vector2(650, 300));
            Light(p, 800, 300, 340, Hex("FF8A5C", 0.12f));

            // floor: polished + reflections
            Floor(p, horizon, Hex("0B0B16"), Hex("15162B"));
            LightEllipse(p, 290, 120, 260, 30, Hex("7A5CFF", 0.12f));
            LightEllipse(p, 800, 120, 220, 40, Hex("FF8A5C", 0.18f));
            for (float x = 0; x < SceneW; x += 160) LightEllipse(p, x + 1, 120, 10, 40, Hex("3DE1FF", 0.12f));

            // sleek desk with integrated console
            Desk(p, 100, 510, 198, Hex("E8E6EE"), Hex("2A2834"));
            RG(p, 114, 186, 380, 12, 2, Hex("1A1822"), Hex("2A2834"));
            for (int i = 0; i < 16; i++) R(p, 124 + i * 23, 189, 3, 6, 1, i % 4 == 0 ? Hex("FF3DC8") : Hex("3DE1FF"));
            R(p, 120, 60, 140, 110, 6, Hex("15131A"));
            for (int i = 0; i < 4; i++) R(p, 128, 70 + i * 24, 124, 16, 3, Hex("23202B"));
            for (int i = 0; i < 4; i++) R(p, 140, 76 + i * 24, 60, 3, 1, Hex("3DE1FF", 0.8f));
            var s = TierScreens[4];
            Light(p, s[0].center.x, s[0].center.y, 280, Hex("3DE1FF", 0.14f));
            Monitor(p, s[0], 198, false);
            Monitor(p, s[1], 198, false);
            // wall-mounted speakers
            foreach (var cone in TierCones[4]) SpeakerBox(p, cone, 10, Hex("0E0D16"), Hex("3DE1FF"));
            MidiKeys(p, 168, 198, 180, 26, Hex("16151E"), true);
            PcKeyboard(p, 356, 198, 60, Hex("16151E"), true);
            Plant(p, 590, 150, 1.4f, Hex("E8E6EE"));
            Plant(p, 970, 150, 1.2f, Hex("E8E6EE"));
        }

        // ================================================================== PRODUCER STATIONS (220×200)

        static void Laptop(SdfPainter p, Color body, Color sticker)
        {
            var s = StationScreen;
            R(p, s.x - 4, s.y - 4, s.width + 8, s.height + 8, 4, body);
            R(p, s.x, s.y, s.width, s.height, 1, Hex("0B0A14"));
            Pg(p, body, new Vector2(12, 92), new Vector2(128, 92), new Vector2(122, 101), new Vector2(18, 101));
            R(p, 40, 95, 60, 2.5f, 1, A(Color.black, 0.3f));
            if (sticker.a > 0) Sticker(p, s.x + 8, s.y - 1, 3, sticker, 1);
        }

        static SdfPainter Station(Color top, Color legs)
        {
            var p = new SdfPainter(220, 200);
            FloorShadow(p, 90, 10, 100, 12, 0.35f);
            R(p, 4, 80, 148, 10, 3, top);
            R(p, 4, 87, 148, 3, 1, A(Color.white, 0.1f));
            R(p, 12, 6, 7, 76, 2, legs);
            R(p, 136, 6, 7, 76, 2, legs);
            return p;
        }

        static SdfPainter StationBeginner()
        {
            var p = Station(Hex("B8B2A6"), Hex("5A5A62"));
            Laptop(p, Hex("9AA0AC"), Hex("FFD23D"));
            R(p, 128, 92, 9, 15, 2, Hex("E63946"));
            R(p, 129, 103, 7, 2, 1, Hex("F4F4F6"));
            R(p, 2, 92, 10, 3, 1, Hex("F4F4F6"));
            L(p, 70, 92, 60, 40, 1.4f, Hex("2A2A33"));
            L(p, 60, 40, 90, 10, 1.4f, Hex("2A2A33"));
            return p;
        }

        static SdfPainter StationMelody()
        {
            var p = Station(Hex("F2EEF6"), Hex("D9D3EA"));
            Laptop(p, Hex("F4D6E4"), Hex("B48CFF"));
            // synth on the shelf below
            R(p, 18, 44, 120, 6, 2, Hex("D9D3EA"));
            R(p, 24, 50, 108, 20, 4, Hex("2A2240"));
            for (int i = 0; i < 12; i++) R(p, 52 + i * 6.5f, 52, 5.5f, 9, 1, Hex("F4F1FA"));
            for (int i = 0; i < 3; i++) C(p, 32 + i * 7, 64, 2.4f, i == 1 ? Hex("FF6FB5") : Hex("2EC4B6"));
            Plant(p, 140, 92, 0.6f, Hex("FF6FB5"));
            Light(p, 70, 140, 80, Hex("FF6FB5", 0.12f));
            return p;
        }

        static SdfPainter StationDrum()
        {
            var p = Station(Hex("1F1E26"), Hex("101014"));
            Laptop(p, Hex("2A2A33"), Hex("FF8A3D"));
            // pad controller on the shelf
            R(p, 18, 44, 120, 6, 2, Hex("2A2A33"));
            R(p, 34, 50, 64, 26, 4, Hex("16161A"));
            for (int r = 0; r < 2; r++)
            for (int i = 0; i < 4; i++)
                R(p, 38 + i * 15, 54 + r * 10, 12, 8, 2, (i + r) % 3 == 0 ? Hex("FF8A3D") : (i + r) % 3 == 1 ? Hex("FF3DC8") : Hex("3DE1FF"));
            // subwoofer
            R(p, 104, 8, 34, 36, 4, Hex("16161A"));
            C(p, 121, 26, 12, Hex("0E0C14"));
            Rn(p, 121, 26, 12, 2, Hex("FF8A3D"));
            R(p, 130, 92, 9, 15, 2, Hex("4DFFA8"));
            // ashtray with a resting cigarette (small adult-only background detail)
            p.Fill(Ellipse(new Vector2(121, 46), new Vector2(11, 3.5f)), Hex("5A5E68"), Bnd(108, 40, 26, 12));
            L(p, 116, 48, 130, 50, 1.3f, Hex("F4F1EA"));
            C(p, 130.5f, 50, 1.4f, Hex("FF6A2A"));
            for (int i = 0; i < 3; i++) p.Fill(Arc(new Vector2(132 + (i % 2) * 4, 56 + i * 9), 5, 1.2f, 90 + i * 60, 250 + i * 60), Hex("C9C6CF", 0.35f - i * 0.08f), Bnd(124, 48, 20, 36));
            return p;
        }
    }
}
