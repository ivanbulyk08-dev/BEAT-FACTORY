using UnityEngine;
using static BeatFactory.EditorTools.Art.SdfPainter;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// Rooms 3 and 6–10 of the career: Small Pro, Luxury, Major, Elite, Record Label and Mega Complex.
    /// Each has its own layout, furniture, lighting and wall art (shared layout rules: floor line y=150,
    /// main desk x≈120–490, screens from TierScreens, producer front row below y≈95).
    /// </summary>
    public static partial class ArtGenerator
    {
        const float Horizon = 150;

        static void Beam(SdfPainter p, float x0, float y0, float x1, float x2, float yBottom, Color c)
        {
            // light beam from a ceiling point (x0,y0) widening down to [x1,x2] at yBottom
            var v = new[] { new Vector2(x0 - 6, y0), new Vector2(x0 + 6, y0), new Vector2(x2, yBottom), new Vector2(x1, yBottom) };
            p.Fill(Polygon(v), q => new Color(c.r, c.g, c.b, c.a * Mathf.Clamp01((q.y - yBottom) / (y0 - yBottom))),
                Bnd(Mathf.Min(x1, x0 - 6), yBottom, Mathf.Max(x2, x0 + 6) - Mathf.Min(x1, x0 - 6), y0 - yBottom));
        }

        static void StandSpeaker(SdfPainter p, float x, float y, float w, float h, Color body, Color trim)
        {
            R(p, x, y, w, h, 6, body);
            C(p, x + w / 2, y + h * 0.32f, w * 0.34f, Hex("0E0C14"));
            Rn(p, x + w / 2, y + h * 0.32f, w * 0.34f, 2, trim);
            C(p, x + w / 2, y + h * 0.75f, w * 0.16f, Hex("0E0C14"));
            Rn(p, x + w / 2, y + h * 0.75f, w * 0.16f, 1.5f, trim);
        }

        static void Sofa(SdfPainter p, float x, float y, float w, Color c, bool tufted)
        {
            R(p, x, y, w, 50, 14, Color.Lerp(c, Color.black, 0.25f));
            R(p, x + 6, y + 44, w - 12, 44, 16, c);
            R(p, x - 10, y + 10, 26, 62, 12, Color.Lerp(c, Color.black, 0.1f));
            R(p, x + w - 16, y + 10, 26, 62, 12, Color.Lerp(c, Color.black, 0.1f));
            if (tufted)
                for (float tx = x + 24; tx < x + w - 16; tx += 22)
                for (float ty = y + 56; ty < y + 84; ty += 14)
                    C(p, tx, ty, 1.6f, A(Color.black, 0.35f));
            R(p, x + 10, y - 10, 6, 12, 2, Hex("C9A23A"));
            R(p, x + w - 16, y - 10, 6, 12, 2, Hex("C9A23A"));
        }

        // ------------------------------------------------------------------ Level 3 — Small Pro Studio
        static void SmallPro(SdfPainter p)
        {
            p.VerticalGradient(Hex("1E2433"), Hex("161B28"));
            // a few acoustic panels + corner bass traps (not a full foam wall yet)
            foreach (var (x, y) in new[] { (40f, 300f), (130f, 300f), (440f, 300f), (530f, 300f), (40f, 400f), (530f, 400f) })
            {
                R(p, x, y, 80, 80, 4, Hex("2A3346"));
                for (int k = 0; k < 4; k++) R(p, x + 5, y + 5 + k * 19, 70, 14, 3, Hex("33405A"));
            }
            Pg(p, Hex("12161F"), new Vector2(0, 150), new Vector2(60, 150), new Vector2(0, 500));
            Pg(p, Hex("12161F"), new Vector2(1000, 150), new Vector2(940, 150), new Vector2(1000, 500));
            R(p, 0, 506, SceneW, 54, 0, Hex("10141D"));
            // vinyl shelf, clock, framed art
            R(p, 650, 380, 160, 8, 2, Hex("6B4E32"));
            for (int i = 0; i < 9; i++) R(p, 656 + i * 16, 388, 12, 40 - (i % 3) * 4, 1, Color.HSVToRGB(0.55f + i * 0.05f, 0.5f, 0.6f));
            C(p, 880, 440, 26, Hex("E8E6EE"));
            Rn(p, 880, 440, 26, 3, Hex("2A2834"));
            L(p, 880, 440, 880, 458, 2, Hex("2A2834"));
            L(p, 880, 440, 894, 440, 2, Hex("2A2834"));
            AlbumCover(p, 660, 250, 70, Album.FuturisticHipHop, 1);
            AlbumCover(p, 760, 262, 56, Album.Ambient, 1);
            // floor: light oak + rug
            Floor(p, Horizon, Hex("3A2C22"), Hex("5A4432"));
            for (float y = 14; y < Horizon; y += 20) R(p, 0, y, SceneW, 1.5f, 0, A(Color.black, 0.18f));
            p.Fill(RRect(110, 30, 420, 80, 16), Hex("2A3346"), Bnd(110, 30, 420, 80));
            // desk + gear
            Desk(p, 110, 490, 198, Hex("4A3628"), Hex("2A1E16"));
            R(p, 410, 60, 60, 110, 4, Hex("16161C"));
            for (int i = 0; i < 4; i++) { R(p, 416, 70 + i * 24, 48, 16, 2, Hex("23232B")); C(p, 424, 78 + i * 24, 2, Hex("4DFFA8")); }
            var s = TierScreens[2][0];
            Light(p, s.center.x, s.center.y, 240, Hex("3DE1FF", 0.14f));
            Monitor(p, s, 198, false);
            foreach (var cone in TierCones[2]) SpeakerBox(p, cone, 8, Hex("1A1D26"), Hex("FFD23D"));
            MidiKeys(p, 176, 198, 160, 22, Hex("1C1A26"), true);
            PcKeyboard(p, 346, 198, 60, Hex("2A2834"), false);
            // boom mic with pop filter
            L(p, 104, 200, 104, 320, 3, Hex("2A2834"));
            L(p, 104, 320, 156, 352, 3, Hex("2A2834"));
            p.Fill(Capsule(new Vector2(160, 316), new Vector2(160, 340), 9), Hex("C9CCD6"), Bnd(148, 300, 24, 52));
            Rn(p, 176, 326, 13, 2, Hex("2A2834"));
            // warm desk lamp
            L(p, 470, 198, 478, 250, 2, Hex("2A2834"));
            Pg(p, Hex("FFB347"), new Vector2(466, 250), new Vector2(492, 250), new Vector2(486, 268), new Vector2(472, 268));
            Light(p, 478, 248, 110, Hex("FFB347", 0.22f));
            Plant(p, 590, 150, 1.1f, Hex("3A3446"));
        }

        // ------------------------------------------------------------------ Level 6 — Luxury Studio
        static void Luxury(SdfPainter p)
        {
            p.VerticalGradient(Hex("24160F"), Hex("1A100A"));
            // walnut slat wall with gold trims
            for (float x = 0; x < SceneW; x += 18) R(p, x, Horizon, 14, 360, 1, Hex("3A2618"));
            for (float x = 0; x < SceneW; x += 250) R(p, x, Horizon, 3, 360, 0, Hex("C9A23A"));
            R(p, 0, 505, SceneW, 4, 0, Hex("C9A23A"));
            R(p, 0, 509, SceneW, 51, 0, Hex("140C08"));
            // chandelier
            L(p, 720, 509, 720, 470, 1.5f, Hex("C9A23A"));
            p.Fill(Ellipse(new Vector2(720, 456), new Vector2(46, 16)), Hex("C9A23A"), Bnd(670, 436, 100, 40));
            for (int i = 0; i < 9; i++) L(p, 680 + i * 10, 450, 680 + i * 10, 432 - (i % 3) * 6, 1.4f, Hex("FFF1C8", 0.9f));
            Light(p, 720, 450, 220, Hex("FFC870", 0.25f));
            // art: gold records + abstract painting
            foreach (var x in new[] { 560f, 640f })
            {
                R(p, x - 30, 360, 60, 74, 2, Hex("0E0A08"));
                C(p, x, 400, 22, Hex("F2C14E"));
                Rn(p, x, 400, 15, 1, A(Color.black, 0.3f));
                C(p, x, 400, 6, Hex("8A1E2E"));
            }
            R(p, 790, 330, 150, 110, 2, Hex("C9A23A"));
            RG(p, 796, 336, 138, 98, 0, Hex("3A1A2E"), Hex("E8A86A"));
            C(p, 840, 390, 26, Hex("F4E1C8", 0.8f));
            Pg(p, Hex("1A0E14", 0.8f), new Vector2(796, 336), new Vector2(934, 336), new Vector2(934, 370), new Vector2(860, 352));
            // black marble floor with gold veins + persian-style rug
            Floor(p, Horizon, Hex("0E0C0E"), Hex("26222A"));
            var rnd = new System.Random(11);
            for (int i = 0; i < 12; i++)
            {
                float x = (float)rnd.NextDouble() * 1000, y = (float)rnd.NextDouble() * 140;
                L(p, x, y, x + 60 + (float)rnd.NextDouble() * 80, y + 10 - (float)rnd.NextDouble() * 20, 0.8f, Hex("C9A23A", 0.35f));
            }
            p.Fill(RRect(100, 24, 440, 90, 8), Hex("5A1A22"), Bnd(100, 24, 440, 90));
            p.Fill(RRect(116, 34, 408, 70, 6), Hex("7A2A2E"), Bnd(116, 34, 408, 70));
            for (float x = 140; x < 520; x += 40) Pg(p, Hex("C9A23A", 0.5f), new Vector2(x, 69), new Vector2(x + 12, 82), new Vector2(x + 24, 69), new Vector2(x + 12, 56));
            // chesterfield sofa + bar cart
            Sofa(p, 640, 150, 290, Hex("5A2E1A"), true);
            R(p, 950, 150, 46, 90, 4, Hex("C9A23A"));
            R(p, 954, 196, 38, 4, 1, Hex("8A6A1E"));
            Bottle(p, 958, 200, Hex("2FA36B", 0.9f));
            Bottle(p, 974, 200, Hex("C9A23A", 0.9f));
            // wide walnut desk, curved ultra-wide + side screen, floor-standing wood speakers
            Desk(p, 100, 500, 198, Hex("4A2C18"), Hex("2A1A10"));
            R(p, 100, 194, 400, 3, 1, Hex("C9A23A"));
            var s = TierScreens[5];
            Light(p, s[0].center.x, s[0].center.y, 280, Hex("FFC870", 0.14f));
            Monitor(p, s[0], 198, false);
            Monitor(p, s[1], 198, false);
            foreach (var cone in TierCones[5]) SpeakerBox(p, cone, 8, Hex("3A2618"), Hex("C9A23A"));
            MidiKeys(p, 168, 198, 170, 24, Hex("1A1210"), true);
            PcKeyboard(p, 350, 198, 62, Hex("1A1210"), false);
            Plant(p, 590, 150, 1.4f, Hex("C9A23A"));
        }

        // ------------------------------------------------------------------ Level 7 — Major Studio
        static void Major(SdfPainter p)
        {
            p.VerticalGradient(Hex("141A22"), Hex("0E1218"));
            // wooden ceiling slats + track spots
            for (float x = 0; x < SceneW; x += 24) R(p, x, 500, 18, 60, 2, Hex("3A2C20"));
            for (float x = 120; x < SceneW; x += 160)
            {
                R(p, x - 8, 488, 16, 14, 3, Hex("2A2834"));
                Beam(p, x, 488, x - 60, x + 60, 160, Hex("BFE8FF", 0.08f));
            }
            // glass partition: second production room (left) and live room with drum kit (right)
            RG(p, 470, 220, 500, 260, 4, Hex("0E2030"), Hex("163448"));
            R(p, 720, 220, 8, 260, 0, Hex("2A2834"));
            // room B: small desk with 2 monitors
            R(p, 500, 250, 190, 8, 2, Hex("2A2834"));
            R(p, 520, 262, 70, 46, 3, Hex("0B0A14"));
            R(p, 600, 262, 70, 46, 3, Hex("0B0A14"));
            R(p, 524, 266, 62, 38, 2, Hex("3D7BFF", 0.4f));
            R(p, 604, 266, 62, 38, 2, Hex("B65CFF", 0.4f));
            // live room: drum kit silhouette + amp
            C(p, 830, 270, 30, Hex("1C2A38"));
            C(p, 790, 300, 16, Hex("1C2A38"));
            C(p, 870, 300, 16, Hex("1C2A38"));
            L(p, 760, 330, 790, 330, 2, Hex("C9A23A"));
            L(p, 880, 340, 920, 336, 2, Hex("C9A23A"));
            R(p, 900, 236, 50, 60, 4, Hex("1A1A22"));
            LightEllipse(p, 840, 300, 140, 90, Hex("3DE1FF", 0.12f));
            Pg(p, A(Color.white, 0.05f), new Vector2(480, 478), new Vector2(560, 478), new Vector2(480, 300));
            // concrete pillars + patch bay wall
            R(p, 440, Horizon, 26, 360, 2, Hex("2A2E36"));
            R(p, 966, Horizon, 26, 360, 2, Hex("2A2E36"));
            R(p, 40, 380, 120, 110, 4, Hex("16161C"));
            for (int r = 0; r < 6; r++) for (int c2 = 0; c2 < 10; c2++) C(p, 52 + c2 * 11, 392 + r * 16, 2.4f, c2 % 3 == 0 ? Hex("FF3DC8") : Hex("3A3646"));
            // floor + big console desk
            Floor(p, Horizon, Hex("15181E"), Hex("262B33"));
            p.Fill(RRect(80, 26, 500, 90, 10), Hex("1E2A38"), Bnd(80, 26, 500, 90));
            Desk(p, 90, 520, 198, Hex("2A2532"), Hex("15131A"));
            RG(p, 100, 184, 410, 14, 2, Hex("3A3446"), Hex("4A4458"));
            for (int i = 0; i < 28; i++) R(p, 106 + i * 14.5f, 188, 3, 7, 1, i % 5 == 0 ? Hex("FF3DC8") : Hex("8A84B0"));
            var s = TierScreens[6];
            Light(p, s[0].center.x, s[0].center.y, 260, Hex("3D7BFF", 0.15f));
            Monitor(p, s[0], 198, false);
            Monitor(p, s[1], 198, false);
            foreach (var cone in TierCones[6]) SpeakerBox(p, cone, 12, Hex("0E0D16"), Hex("3D7BFF"));
            MidiKeys(p, 178, 198, 170, 26, Hex("16151E"), true);
            AlbumCover(p, 300, 410, 62, Album.Electronic, 2);
            AlbumCover(p, 200, 420, 52, Album.DarkTrap, 2);
        }

        // ------------------------------------------------------------------ Level 8 — Elite Studio
        static void Elite(SdfPainter p)
        {
            p.VerticalGradient(Hex("0C0814"), Hex("08060E"));
            // huge LED video wall behind the desk
            RG(p, 120, 210, 560, 300, 6, Hex("3A0E5A"), Hex("1A0A3A"));
            for (int i = 0; i < 70; i++)
            {
                float x = 130 + i * 7.8f;
                float h = 30 + 90 * Mathf.Abs(Mathf.Sin(i * 0.35f) * Mathf.Cos(i * 0.12f));
                R(p, x, 360 - h / 2, 4, h, 2, Color.HSVToRGB(0.78f + 0.12f * Mathf.Sin(i * 0.1f), 0.7f, 1f));
            }
            for (float y = 214; y < 508; y += 6) R(p, 120, y, 560, 1, 0, A(Color.black, 0.25f)); // LED pixel rows
            Light(p, 400, 360, 380, Hex("B65CFF", 0.18f));
            // light beams from the ceiling
            Beam(p, 60, 560, 0, 220, 150, Hex("FF3DC8", 0.12f));
            Beam(p, 940, 560, 760, 1000, 150, Hex("3DE1FF", 0.12f));
            Beam(p, 760, 560, 640, 900, 150, Hex("B65CFF", 0.1f));
            // artist lounge: curved white sofa with purple underglow + glass booth
            R(p, 720, 214, 250, 270, 6, Hex("161026", 0.9f));
            Rn(p, 845, 350, 40, 2, Hex("B65CFF", 0.6f));
            p.Fill(Capsule(new Vector2(845, 380), new Vector2(845, 400), 12), Hex("C9CCD6"), Bnd(830, 360, 30, 60));
            Pg(p, A(Color.white, 0.06f), new Vector2(730, 480), new Vector2(790, 480), new Vector2(730, 300));
            LightEllipse(p, 840, 160, 170, 24, Hex("B65CFF", 0.5f));
            Sofa(p, 700, 150, 270, Hex("E8E6EE"), false);
            // glossy black floor with neon reflections
            Floor(p, Horizon, Hex("05040A"), Hex("120E1E"));
            LightEllipse(p, 400, 120, 300, 40, Hex("B65CFF", 0.2f));
            LightEllipse(p, 110, 110, 90, 30, Hex("FF3DC8", 0.15f));
            LightEllipse(p, 870, 110, 110, 30, Hex("3DE1FF", 0.15f));
            // floating desk with neon edge
            Desk(p, 100, 520, 198, Hex("0E0C14"), Hex("0E0C14"));
            R(p, 100, 184, 420, 3, 1, Hex("FF3DC8"));
            LightEllipse(p, 310, 186, 230, 14, Hex("FF3DC8", 0.4f));
            var s = TierScreens[7];
            Monitor(p, s[0], 198, false);
            Monitor(p, s[1], 198, false);
            foreach (var cone in TierCones[7]) SpeakerBox(p, cone, 10, Hex("0E0C14"), Hex("B65CFF"));
            MidiKeys(p, 168, 198, 190, 26, Hex("0E0C14"), true);
            PcKeyboard(p, 368, 198, 60, Hex("0E0C14"), true);
        }

        // ------------------------------------------------------------------ Level 9 — Record Label Studio
        static void RecordLabel(SdfPainter p)
        {
            // Bright corporate HQ in daylight: cream walls, white oak, glass offices, a skyline by day.
            p.VerticalGradient(Hex("D8CDBA"), Hex("EDE5D6"));
            R(p, 0, 506, SceneW, 54, 0, Hex("F6F1E8"));
            for (float x = 60; x < SceneW; x += 180) { R(p, x, 520, 90, 8, 4, Hex("FFFFFF")); Light(p, x + 45, 520, 120, Hex("FFF6E0", 0.25f)); }
            // panoramic window: city by day
            RG(p, 640, 200, 360, 300, 0, Hex("CFE6F5"), Hex("7FB4E0"));
            var rnd = new System.Random(21);
            for (float x = 640; x < 1000; x += 22 + (float)rnd.NextDouble() * 18)
            {
                float w = 18 + (float)rnd.NextDouble() * 24, h = 50 + (float)rnd.NextDouble() * 200;
                var bc = Color.Lerp(Hex("8FA6BE"), Hex("B9C9D8"), (float)rnd.NextDouble());
                R(p, x, 200, w, h, 0, bc);
                for (float wy = 208; wy < 200 + h - 8; wy += 10)
                    R(p, x + 3, wy, w - 6, 3, 0, A(Color.white, 0.35f));
            }
            for (float x = 640; x <= 1000; x += 120) R(p, x, 200, 6, 300, 0, Hex("F2EEE6"));
            R(p, 640, 194, 360, 8, 0, Hex("F2EEE6"));
            R(p, 640, 496, 360, 10, 0, Hex("F2EEE6"));
            Pg(p, A(Color.white, 0.25f), new Vector2(660, 496), new Vector2(740, 496), new Vector2(660, 300));
            Pg(p, A(Color.white, 0.18f), new Vector2(860, 496), new Vector2(900, 496), new Vector2(800, 300), new Vector2(800, 330));
            // album display grid (platinum + gold records in white frames) — the label's history
            for (int r = 0; r < 3; r++)
                for (int i = 0; i < 2; i++)
                {
                    float x = 440 + i * 88, y = 270 + r * 76;
                    R(p, x, y, 80, 66, 2, Hex("FFFFFF"));
                    R(p, x + 4, y + 4, 72, 58, 1, Hex("1A1A22"));
                    bool plat = (i + r) % 2 == 0;
                    C(p, x + 40, y + 36, 20, plat ? Hex("E4E8F0") : Hex("F2C14E"));
                    Rn(p, x + 40, y + 36, 13, 1, A(Color.black, 0.25f));
                    C(p, x + 40, y + 36, 5, plat ? Hex("3D7BFF") : Hex("8A1E2E"));
                    R(p, x + 12, y + 8, 56, 5, 1, A(Color.white, 0.6f));
                }
            // glass office (meeting room) behind the window bay
            R(p, 640, 150, 340, 50, 0, Hex("E8E0D2"));
            p.Fill(Ellipse(new Vector2(800, 196), new Vector2(150, 22)), Hex("FFFFFF", 0.7f), Bnd(640, 170, 320, 50));
            for (int i = 0; i < 4; i++) R(p, 690 + i * 70, 150, 36, 60, 12, Hex("2A2A33"));
            // white oak floor
            Floor(p, Horizon, Hex("B89A74"), Hex("D8C2A0"));
            for (float y = 16; y < Horizon; y += 22) R(p, 0, y, SceneW, 1.5f, 0, A(Color.black, 0.08f));
            p.Fill(RRect(80, 26, 470, 86, 10), Hex("EFE8DA"), Bnd(80, 26, 470, 86));
            // executive desk: white lacquer with steel edge
            Desk(p, 90, 520, 198, Hex("F4F1EA"), Hex("9A9AA6"));
            R(p, 90, 194, 430, 3, 1, Hex("9A9AA6"));
            var s = TierScreens[8];
            Monitor(p, s[0], 198, false);
            Monitor(p, s[1], 198, false);
            foreach (var cone in TierCones[8]) SpeakerBox(p, cone, 8, Hex("F4F1EA"), Hex("2A2A33"));
            MidiKeys(p, 168, 198, 180, 26, Hex("2A2A33"), true);
            Plant(p, 600, 150, 1.6f, Hex("FFFFFF"));
            Plant(p, 40, 150, 1.3f, Hex("FFFFFF"));
        }

        // ------------------------------------------------------------------ Level 10 — Mega Music Complex
        static void Mega(SdfPainter p)
        {
            p.VerticalGradient(Hex("080610"), Hex("05040A"));
            // control-room window over an arena stage
            RG(p, 20, 200, 960, 330, 6, Hex("140A28"), Hex("05040C"));
            // stage + giant LED screens
            R(p, 300, 220, 640, 22, 2, Hex("2A2834"));
            RG(p, 470, 260, 300, 150, 4, Hex("FF3DC8"), Hex("3D7BFF"));
            for (int i = 0; i < 30; i++) R(p, 478 + i * 9.6f, 300, 5, 20 + 70 * Mathf.Abs(Mathf.Sin(i * 0.5f)), 2, A(Color.white, 0.5f));
            RG(p, 330, 280, 110, 90, 4, Hex("B65CFF"), Hex("3DE1FF"));
            RG(p, 800, 280, 110, 90, 4, Hex("B65CFF"), Hex("3DE1FF"));
            // lighting truss + beams
            R(p, 300, 470, 640, 10, 2, Hex("3A3646"));
            for (float x = 320; x < 940; x += 22) L(p, x, 470, x + 11, 480, 1, Hex("5A5766"));
            Color[] beamColors = { Hex("FF3DC8", 0.12f), Hex("3DE1FF", 0.12f), Hex("4DFFA8", 0.1f), Hex("FFD23D", 0.1f) };
            for (int i = 0; i < 8; i++)
            {
                float x = 340 + i * 80;
                Beam(p, x, 468, x - 120 + i * 20, x + 40 + i * 10, 230, beamColors[i % 4]);
                C(p, x, 466, 5, A(Color.white, 0.9f));
            }
            // crowd silhouettes
            var rnd = new System.Random(3);
            for (float x = 30; x < 980; x += 9)
            {
                float y = 204 + (float)rnd.NextDouble() * 10;
                C(p, x, y + 10, 6, Hex("0A0812"));
                R(p, x - 6, 200, 12, y - 200 + 6, 3, Hex("0A0812"));
                if (rnd.NextDouble() < 0.12) L(p, x, y + 10, x + 4, y + 30, 1.6f, Hex("0A0812"));
            }
            for (int i = 0; i < 40; i++) R(p, 300 + (float)rnd.NextDouble() * 640, 240 + (float)rnd.NextDouble() * 220, 4, 2, 1, Color.HSVToRGB((float)rnd.NextDouble(), 0.6f, 1f));
            // window frame + reflections
            for (float x = 20; x <= 980; x += 240) R(p, x, 196, 6, 340, 0, Hex("1A1824"));
            R(p, 20, 196, 966, 8, 0, Hex("1A1824"));
            Pg(p, A(Color.white, 0.05f), new Vector2(40, 520), new Vector2(120, 520), new Vector2(40, 260));
            R(p, 0, 536, SceneW, 24, 0, Hex("05040A"));
            // floor + massive console
            Floor(p, Horizon, Hex("06050C"), Hex("14121E"));
            LightEllipse(p, 500, 130, 460, 40, Hex("B65CFF", 0.15f));
            Desk(p, 80, 540, 198, Hex("16151E"), Hex("0E0C14"));
            RG(p, 90, 182, 440, 16, 2, Hex("2A2834"), Hex("3A3646"));
            for (int i = 0; i < 36; i++) R(p, 96 + i * 12, 186, 3, 9, 1, Color.HSVToRGB(0.75f + 0.25f * (i / 36f), 0.7f, 1f));
            var s = TierScreens[9];
            Monitor(p, s[0], 198, false);
            Monitor(p, s[1], 198, false);
            foreach (var cone in TierCones[9]) SpeakerBox(p, cone, 12, Hex("0E0C14"), Hex("FF3DC8"));
            MidiKeys(p, 160, 198, 200, 28, Hex("0E0C14"), true);
        }
    }
}
