using System;
using UnityEngine;
using static BeatFactory.EditorTools.Art.SdfPainter;
using Sdf = System.Func<UnityEngine.Vector2, float>;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// Characters: 3/4 view facing left (toward the monitor), seated.
    /// Parts per character: body (200×190, with chair), arm (56×140, pivot at the shoulder),
    /// 6 head expressions (160×160, pivot at the neck) and a portrait (256×256).
    /// </summary>
    public static partial class ArtGenerator
    {
        public enum HairStyle { Twists, Messy, Bob, LocsCap, DreadsHalfUp, DreadsTied, Braids, LongStraight, CurlyFade }
        public enum TopStyle { Hoodie, Jacket, Tee, Bomber, Puffer, Tracksuit }
        /// <summary>Original sneaker designs inspired by general categories (no real logos or exact replicas).</summary>
        public enum SneakerStyle { LowRetro, HighTop, Minimal, Chunky }
        public enum PantsStyle { Jogger, Denim, Wide, Cargo }

        public class Look
        {
            public string id;
            public Color skin, skinShade, lip, eye = Hex("3B2314"), brow;
            public Color hair, hairTip;
            public HairStyle hairStyle;
            public TopStyle top;
            public PantsStyle pantsStyle;
            public SneakerStyle sneaker;
            public Color cloth, clothShade, accent, pants, pantsShade, shoe, shoeAccent, sole = Hex("F4F4F6");
            public Color phones, phonesAccent, metal = Hex("F2C14E"), cap, beanie;
            public bool phonesOnHead, phonesOnNeck;
            public float jaw = 1f, noseW = 1f, browThick = 3f;
            public bool squareJaw, lipFull, lashes, beard, stubble, glasses, sunglasses, septum;
            public bool chainThin, chainCuban, earHoop, earStud, noseRing, faceStar, faceDots, browSlit;
            public bool neckTattoo, armTattoo, handTattoo, watchSmart, watchGold, bracelets, rings;
            public string portraitExpression = "smile";
            public Color portraitBg;

            public Look Clone() => (Look)MemberwiseClone();
        }

        /// <summary>
        /// The player's outfit grows with the studio (visual story): worn grey hoodie + beat-up low-tops in the basement →
        /// purple hoodie + high-tops → satin bomber + chain → black luxury puffer, gold chain and chunky designer sneakers.
        /// </summary>
        public static Look PlayerTier(int tier)
        {
            var l = Cast[0].Clone();
            switch (tier)
            {
                case 0:
                    l.top = TopStyle.Hoodie; l.cloth = Hex("4A4752"); l.clothShade = Hex("34313B"); l.accent = Hex("8A84B0");
                    l.pantsStyle = PantsStyle.Denim; l.pants = Hex("2E3A52"); l.pantsShade = Hex("222B3D");
                    l.sneaker = SneakerStyle.LowRetro; l.shoe = Hex("C9C6CF"); l.shoeAccent = Hex("5A5766"); l.sole = Hex("D9D2C2");
                    l.chainThin = false; l.watchSmart = false;
                    break;
                case 2:
                    l.top = TopStyle.Bomber; l.cloth = Hex("1E1B2A"); l.clothShade = Hex("13111C"); l.accent = Hex("B65CFF");
                    l.pantsStyle = PantsStyle.Cargo; l.pants = Hex("2A2834"); l.pantsShade = Hex("1C1A24");
                    l.sneaker = SneakerStyle.HighTop; l.shoe = Hex("F2F2F5"); l.shoeAccent = Hex("9B5CFF");
                    break;
                case 3:
                    l.top = TopStyle.Puffer; l.cloth = Hex("17161D"); l.clothShade = Hex("0D0C12"); l.accent = Hex("F2C14E");
                    l.pantsStyle = PantsStyle.Wide; l.pants = Hex("3A3646"); l.pantsShade = Hex("2A2734");
                    l.sneaker = SneakerStyle.Chunky; l.shoe = Hex("121117"); l.shoeAccent = Hex("F4F4F6"); l.sole = Hex("F4F4F6");
                    l.chainThin = false; l.chainCuban = true; l.metal = Hex("F2C14E"); l.watchSmart = false; l.watchGold = true; l.rings = true;
                    break;
            }
            return l;
        }

        public static string ArtistPortraitPath(string id) => $"{Chars}/Artists/artist_{id}.png";

        /// <summary>Portrait-only producers (cards 4–7): more hairstyles, skin tones and fashion identities.</summary>
        public static readonly Look[] ExtraProducers =
        {
            new Look // Sample Producer: long straight hair with teal ends, beanie, septum, graphic tee
            {
                id = "sample",
                skin = Hex("E7B98E"), skinShade = Hex("C7956A"), lip = Hex("B86B62"), eye = Hex("24160E"), brow = Hex("1A1210"),
                hair = Hex("15131C"), hairTip = Hex("2EC4B6"), hairStyle = HairStyle.LongStraight, beanie = Hex("FF8A3D"),
                top = TopStyle.Tee, cloth = Hex("2A2A33"), clothShade = Hex("1C1C24"), accent = Hex("2EC4B6"),
                jaw = 0.95f, noseW = 0.9f, browThick = 2.4f, lashes = true, septum = true, earStud = true, metal = Hex("D9DEE8"),
                chainThin = true, portraitExpression = "smile", portraitBg = Hex("3A2410"),
            },
            new Look // Sound Designer: long box braids with gold cuffs, glasses, technical jacket
            {
                id = "sound",
                skin = Hex("6B4430"), skinShade = Hex("4E2F20"), lip = Hex("4A2620"), eye = Hex("1E120C"), brow = Hex("120A08"),
                hair = Hex("120D0C"), hairTip = Hex("3A2A24"), hairStyle = HairStyle.Braids,
                top = TopStyle.Jacket, cloth = Hex("7C4DFF"), clothShade = Hex("5634B8"), accent = Hex("3DE1FF"),
                jaw = 0.94f, noseW = 1.15f, browThick = 2.5f, lashes = true, lipFull = true, glasses = true,
                earHoop = true, faceStar = false, portraitExpression = "focus", portraitBg = Hex("2A1A50"),
            },
            new Look // Hitmaker: curly top with a fade, beard, tracksuit, gold chain
            {
                id = "hitmaker",
                skin = Hex("B57A52"), skinShade = Hex("94603E"), lip = Hex("7A4232"), eye = Hex("2A180E"), brow = Hex("140C08"),
                hair = Hex("1A1210"), hairTip = Hex("3A2A22"), hairStyle = HairStyle.CurlyFade,
                top = TopStyle.Tracksuit, cloth = Hex("C2183A"), clothShade = Hex("8E1029"), accent = Hex("F4F4F6"),
                jaw = 1.05f, noseW = 1.1f, browThick = 3.4f, squareJaw = true, beard = true, chainCuban = true, earStud = true,
                portraitExpression = "proud", portraitBg = Hex("3A0E18"),
            },
            new Look // Superstar: long tied-back dreads with gold tips, sunglasses, chains, face tattoo
            {
                id = "superstar",
                skin = Hex("3E2618"), skinShade = Hex("2C1A10"), lip = Hex("2A1610"), eye = Hex("140C08"), brow = Hex("080504"),
                hair = Hex("100B09"), hairTip = Hex("F2C14E"), hairStyle = HairStyle.DreadsTied,
                top = TopStyle.Puffer, cloth = Hex("F4F1FA"), clothShade = Hex("C9C4D6"), accent = Hex("F2C14E"),
                jaw = 1.05f, noseW = 1.3f, browThick = 3.4f, lipFull = true, stubble = true, sunglasses = true,
                chainCuban = true, chainThin = true, earStud = true, faceDots = true, neckTattoo = true,
                portraitExpression = "proud", portraitBg = Hex("3A2E08"),
            },
        };

        /// <summary>Main player + the first three producers. Diverse skin tones, all at the same detail level.</summary>
        public static readonly Look[] Cast =
        {
            new Look // Player: streetwear producer, purple hoodie, big headphones
            {
                id = "player",
                skin = Hex("8D5A3B"), skinShade = Hex("6B4129"), lip = Hex("5C3022"), brow = Hex("140C08"),
                hair = Hex("17110F"), hairTip = Hex("9B5CFF"), hairStyle = HairStyle.DreadsHalfUp,
                pantsStyle = PantsStyle.Jogger, sneaker = SneakerStyle.HighTop,
                top = TopStyle.Hoodie, cloth = Hex("6C3BD9"), clothShade = Hex("4A2799"), accent = Hex("FF3DC8"),
                pants = Hex("22202C"), pantsShade = Hex("17151F"), shoe = Hex("F2F2F5"), shoeAccent = Hex("3DE1FF"),
                phones = Hex("1B1B22"), phonesAccent = Hex("3DE1FF"), phonesOnHead = true, metal = Hex("D9DEE8"),
                jaw = 1f, noseW = 1.2f, browThick = 3.2f, lipFull = true, stubble = true,
                chainThin = true, handTattoo = true, watchSmart = true,
                portraitExpression = "smile", portraitBg = Hex("2A1A55"),
            },
            new Look // Beginner: young, simple green hoodie, round glasses, cheap headphones, excited
            {
                id = "beginner",
                skin = Hex("F1C9A3"), skinShade = Hex("D6A57E"), lip = Hex("C77B6F"), eye = Hex("2E1C12"), brow = Hex("2A1C14"),
                hair = Hex("231813"), hairTip = Hex("4A3428"), hairStyle = HairStyle.Messy,
                pantsStyle = PantsStyle.Denim, sneaker = SneakerStyle.LowRetro,
                top = TopStyle.Hoodie, cloth = Hex("2EA56F"), clothShade = Hex("1F7A51"), accent = Hex("F4F4F6"),
                pants = Hex("3A5A8C"), pantsShade = Hex("2B4469"), shoe = Hex("E63946"), shoeAccent = Hex("F4F4F6"),
                phones = Hex("6B6F7A"), phonesAccent = Hex("A9AEB8"), phonesOnHead = true,
                jaw = 1.08f, noseW = 0.9f, browThick = 2.6f, glasses = true,
                portraitExpression = "excited", portraitBg = Hex("163A2C"),
            },
            new Look // Melody: synth aesthetic, lilac→pink bob with a bun, hoops, subtle tattoos
            {
                id = "melody",
                skin = Hex("D29E73"), skinShade = Hex("B07B52"), lip = Hex("B0575A"), eye = Hex("5A3A20"), brow = Hex("3A2416"),
                hair = Hex("B48CFF"), hairTip = Hex("FF7BC8"), hairStyle = HairStyle.Bob,
                pantsStyle = PantsStyle.Wide, sneaker = SneakerStyle.Minimal,
                top = TopStyle.Jacket, cloth = Hex("2EC4B6"), clothShade = Hex("1E8C83"), accent = Hex("FF6FB5"),
                pants = Hex("1E1B2A"), pantsShade = Hex("15131E"), shoe = Hex("F7F0FA"), shoeAccent = Hex("FF6FB5"),
                phones = Hex("F4F1FA"), phonesAccent = Hex("FF6FB5"), phonesOnNeck = true,
                jaw = 0.92f, noseW = 0.95f, browThick = 2.3f, lashes = true, lipFull = true,
                chainThin = true, earHoop = true, noseRing = true, faceStar = true, handTattoo = true, bracelets = true, rings = true,
                portraitExpression = "proud", portraitBg = Hex("3A1A4A"),
            },
            new Look // Drum: heavy streetwear, backwards cap, locs, beard, tattoo sleeves, gold chains
            {
                id = "drum",
                skin = Hex("5A3826"), skinShade = Hex("41281B"), lip = Hex("2F1A13"), eye = Hex("1E120C"), brow = Hex("0C0806"),
                hair = Hex("120D0B"), hairTip = Hex("2E2420"), hairStyle = HairStyle.LocsCap, cap = Hex("D7263D"),
                pantsStyle = PantsStyle.Cargo, sneaker = SneakerStyle.Chunky,
                top = TopStyle.Tee, cloth = Hex("1B1B20"), clothShade = Hex("101014"), accent = Hex("FF8A3D"),
                pants = Hex("4A4F2E"), pantsShade = Hex("363A21"), shoe = Hex("16161A"), shoeAccent = Hex("FF8A3D"), sole = Hex("FF8A3D"),
                phones = Hex("16161A"), phonesAccent = Hex("FF8A3D"), phonesOnNeck = true,
                jaw = 1.1f, noseW = 1.3f, browThick = 3.8f, squareJaw = true, lipFull = true, beard = true,
                chainCuban = true, earStud = true, faceDots = true, browSlit = true,
                neckTattoo = true, armTattoo = true, handTattoo = true, watchGold = true, rings = true,
                portraitExpression = "proud", portraitBg = Hex("3A1E10"),
            },
        };

        static readonly Color Ink = Hex("120E16", 0.78f);
        static readonly Color EyeWhite = Hex("F6F1EA");

        /// <summary>Coordinate transform so the same drawing code serves heads (1×) and portraits (scaled).</summary>
        struct Tf
        {
            public Vector2 o;
            public float s;
            public Tf(Vector2 o, float s) { this.o = o; this.s = s; }
            public Vector2 P(float x, float y) => o + new Vector2(x, y) * s;
        }

        // ================================================================== HEAD

        static SdfPainter Head(Look c, string expr)
        {
            var p = new SdfPainter(160, 160);
            DrawHead(p, c, expr, new Tf(Vector2.zero, 1f));
            return p;
        }

        /// <summary>Which layer of the head to draw (All = complete head for portraits / producers).</summary>
        public enum HeadPart { All, HairBack, Face, HairFront, Accessories }

        static void DrawHead(SdfPainter p, Look c, string expr, Tf t, bool neck = true, HeadPart part = HeadPart.All)
        {
            bool draw = true;
            bool On(HeadPart x) => part == HeadPart.All || part == x;
            // --- shape helpers in head space (160×160, facing left)
            Sdf Ci(float x, float y, float r) => Circle(t.P(x, y), r * t.s);
            Sdf El(float x, float y, float rx, float ry) => Ellipse(t.P(x, y), new Vector2(rx, ry) * t.s);
            Sdf Ca(float x1, float y1, float x2, float y2, float r) => Capsule(t.P(x1, y1), t.P(x2, y2), r * t.s);
            Sdf Bx(float x, float y, float hx, float hy, float r, float a = 0) => Box(t.P(x, y), new Vector2(hx, hy) * t.s, r * t.s, a);
            Sdf Ar(float x, float y, float r, float th, float a0, float a1) => Arc(t.P(x, y), r * t.s, th * t.s, a0, a1);
            Sdf Rg(float x, float y, float r, float th) => Ring(t.P(x, y), r * t.s, th * t.s);
            Sdf Above(float y) { float yy = t.P(0, y).y; return q => yy - q.y; }
            Sdf Below(float y) { float yy = t.P(0, y).y; return q => q.y - yy; }
            Sdf LeftOf(float x) { float xx = t.P(x, 0).x; return q => q.x - xx; }
            Sdf Po(params float[] xy)
            {
                var v = new Vector2[xy.Length / 2];
                for (int i = 0; i < v.Length; i++) v[i] = t.P(xy[i * 2], xy[i * 2 + 1]);
                return Polygon(v);
            }
            void F(Sdf s, Color col) { if (draw) p.Fill(s, col); }
            void FG(Sdf s, Func<Vector2, Color> col) { if (draw) p.Fill(s, col); }
            Func<Vector2, Color> Grad(float y0, Color c0, float y1, Color c1) => VGradient(t.P(0, y0).y, c0, t.P(0, y1).y, c1);

            bool happy = expr == "smile" || expr == "excited";

            // --- face shape
            Sdf cranium = Ci(84, 92, 42);
            Sdf jaw = c.squareJaw ? Bx(78, 60, 31 * c.jaw, 27, 15) : El(76, 64, 33 * c.jaw, 34);
            Sdf face = SmoothUnion(SmoothUnion(cranium, jaw, 12), Ci(64, 42, 14), 10);

            // --- behind the head: back hair, locs
            draw = On(HeadPart.HairBack);
            if (c.hairStyle == HairStyle.Bob)
            {
                FG(Intersect(El(92, 74, 50, 54), Above(30)), Grad(30, c.hairTip, 128, c.hair));
                F(Ci(104, 140, 15), c.hair);
                F(Ar(104, 140, 15, 3, 200, 340), c.hairTip);
            }
            if (c.hairStyle == HairStyle.LocsCap)
            {
                float[] locs = { 100, 84, 106, 36, 110, 86, 120, 40, 120, 88, 132, 48, 128, 90, 140, 58 };
                for (int i = 0; i < locs.Length; i += 4)
                {
                    F(Ca(locs[i], locs[i + 1], locs[i + 2], locs[i + 3], 5), c.hair);
                    F(Ca(locs[i] + 1.5f, locs[i + 1] - 4, locs[i + 2] + 1.5f, locs[i + 3] + 4, 1.4f), new Color(c.hairTip.r, c.hairTip.g, c.hairTip.b, 0.6f));
                    F(Bx(locs[i + 2] - (locs[i + 2] - locs[i]) * 0.25f, locs[i + 3] + 10, 5.5f, 2.2f, 1.5f), c.metal);
                }
            }

            // Natural dread: slightly curved, bumpy texture, highlight, optional colored tip.
            Color hairDark = Color.Lerp(c.hair, Color.black, 0.45f);
            Color hairLight = Color.Lerp(c.hair, Color.white, 0.22f);
            void Dread(float x1, float y1, float x2, float y2, float r, float bend, bool tip)
            {
                float mx = (x1 + x2) / 2 + bend, my = (y1 + y2) / 2;
                F(Ca(x1, y1, mx, my, r), c.hair);
                F(Ca(mx, my, x2, y2, r * 0.92f), c.hair);
                for (int k = 1; k < 9; k++)
                {
                    float u = k / 9f;
                    float px = u < 0.5f ? Mathf.Lerp(x1, mx, u * 2) : Mathf.Lerp(mx, x2, u * 2 - 1);
                    float py = u < 0.5f ? Mathf.Lerp(y1, my, u * 2) : Mathf.Lerp(my, y2, u * 2 - 1);
                    F(Ca(px - r * 0.8f, py + (k % 2 == 0 ? 0.8f : -0.8f), px + r * 0.8f, py, 0.6f), new Color(hairDark.r, hairDark.g, hairDark.b, 0.7f));
                }
                F(Ca(x1 - r * 0.35f, y1 - 2, mx - r * 0.35f, my, 0.9f), new Color(hairLight.r, hairLight.g, hairLight.b, 0.6f));
                if (tip) F(Ca(Mathf.Lerp(mx, x2, 0.62f), Mathf.Lerp(my, y2, 0.62f), x2, y2, r * 0.95f), c.hairTip);
            }
            void Braid(float x1, float y1, float x2, float y2, float r, bool cuff)
            {
                F(Ca(x1, y1, x2, y2, r), c.hair);
                float len = Mathf.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                int n = Mathf.Max(3, (int)(len / 4.5f));
                for (int k = 1; k < n; k++)
                {
                    float u = k / (float)n;
                    float px = Mathf.Lerp(x1, x2, u), py = Mathf.Lerp(y1, y2, u);
                    float side = k % 2 == 0 ? 1 : -1;
                    F(Ca(px - r * 0.9f * side, py + 1.4f, px + r * 0.2f * side, py - 0.6f, 0.55f), new Color(hairLight.r, hairLight.g, hairLight.b, 0.55f));
                }
                if (cuff) F(Ca(Mathf.Lerp(x1, x2, 0.8f), Mathf.Lerp(y1, y2, 0.8f), Mathf.Lerp(x1, x2, 0.86f), Mathf.Lerp(y1, y2, 0.86f), r * 1.15f), c.metal);
            }

            switch (c.hairStyle)
            {
                case HairStyle.DreadsHalfUp:
                    Dread(52, 100, 38, 34, 4.4f, -4, true);
                    Dread(60, 104, 48, 26, 4.4f, -3, true);
                    for (int i = 0; i < 9; i++)
                        Dread(96 + i * 3.6f, 100 - i * 5, 104 + i * 4.2f, 14 + (i % 3) * 6, 4.6f, 3 + i * 0.5f, true);
                    break;
                case HairStyle.DreadsTied:
                    for (int i = 0; i < 7; i++)
                        Dread(124, 96, 128 + i * 4.5f, 12 + (i % 3) * 7, 4.6f, 6 + i, true);
                    break;
                case HairStyle.Braids:
                    for (int i = 0; i < 6; i++) Braid(46 + i * 4, 100 - i * 2, 34 + i * 3, 8 + (i % 2) * 6, 3.2f, i % 3 == 1);
                    for (int i = 0; i < 10; i++) Braid(94 + i * 3.6f, 102 - i * 5, 100 + i * 4.4f, 4 + (i % 3) * 5, 3.2f, i % 3 == 0);
                    break;
                case HairStyle.LongStraight:
                    FG(Intersect(Union(El(90, 82, 52, 56), Bx(104, 40, 34, 28, 10)), Above(12)), Grad(12, c.hairTip, 110, c.hair));
                    for (int i = 0; i < 6; i++) F(Ca(100 + i * 6, 96, 102 + i * 6.5f, 18, 0.8f), new Color(hairDark.r, hairDark.g, hairDark.b, 0.5f));
                    break;
            }

            // --- neck, face, shading, ear
            draw = On(HeadPart.Face);
            if (neck) F(Ca(88, 36, 91, 12, 14), c.skinShade);
            F(face, c.skin);
            F(Intersect(face, Ci(146, 58, 50)), new Color(c.skinShade.r, c.skinShade.g, c.skinShade.b, 0.6f));
            F(Intersect(face, El(80, 30, 40, 10)), new Color(c.skinShade.r, c.skinShade.g, c.skinShade.b, 0.35f));
            // highlights (stronger on deeper skin so the face keeps its shape)
            float lum = c.skin.r * 0.3f + c.skin.g * 0.59f + c.skin.b * 0.11f;
            float hl = Mathf.Lerp(0.2f, 0.08f, Mathf.InverseLerp(0.25f, 0.75f, lum));
            F(El(64, 100, 16, 9), new Color(1, 0.95f, 0.9f, hl));
            F(El(54, 62, 7, 5), new Color(1, 0.95f, 0.9f, hl * 0.8f));
            F(Ca(44.5f, 96, 44, 60, 1.6f), new Color(1, 0.95f, 0.9f, hl * 0.9f));
            F(El(106, 68, 8, 12), c.skin);
            F(El(107, 68, 4, 7), c.skinShade);

            // --- facial hair (under mouth/nose)
            if (c.beard)
            {
                var beard = Intersect(face, Union(Union(El(66, 38, 26, 17), Bx(96, 50, 13, 22, 8)), Ca(46, 44, 90, 48, 7)));
                F(beard, new Color(c.hair.r, c.hair.g, c.hair.b, 0.93f));
                F(Ca(98, 56, 100, 78, 5), c.hair);
                F(Ca(55, 52, 71, 52.5f, 2.6f), c.hair);
            }
            else if (c.stubble)
            {
                F(Intersect(face, Union(El(64, 39, 21, 13), Ca(56, 52, 70, 52, 2.6f))), new Color(c.hair.r, c.hair.g, c.hair.b, 0.25f));
            }

            // --- face tattoos (own layer when drawing layered heads)
            void FaceTattoos()
            {
                if (c.faceStar) F(Star(t.P(93, 62), 3.8f * t.s, 1.6f * t.s), Hex("2B2240", 0.8f));
                if (c.faceDots)
                {
                    F(Ci(91, 63, 1.4f), Ink);
                    F(Ci(94.5f, 61, 1.4f), Ink);
                    F(Ci(98, 59, 1.4f), Ink);
                }
            }
            if (part == HeadPart.All) FaceTattoos();

            // --- eyes
            Vector2 gaze = expr == "focus" ? new Vector2(-2.6f, -0.6f) : expr == "excited" ? new Vector2(-1f, 0.6f) : new Vector2(-1.6f, 0);
            Color lash = Hex("140C0A");
            void Eye(float ex, float ey, float rx, float ry, bool near)
            {
                if (expr == "smile")
                {
                    F(Ar(ex, ey - 3, rx * 0.95f, 2.7f, 25, 155), lash);
                    F(Ar(ex, ey - 6, rx * 0.7f, 1.2f, 210, 330), new Color(c.skinShade.r, c.skinShade.g, c.skinShade.b, 0.7f));
                    return;
                }
                if (expr == "blink")
                {
                    F(Ar(ex, ey + 4, rx, 2.4f, 208, 332), lash);
                    return;
                }
                float k = expr == "excited" ? 1.18f : 1f;
                Sdf white = El(ex, ey, rx * k, ry * k);
                float cut = expr == "focus" ? ey + ry * 0.3f : expr == "mean" ? ey + ry * 0.1f : expr == "proud" ? ey + ry * 0.45f : float.NaN;
                if (!float.IsNaN(cut)) white = Intersect(white, Below(cut));
                F(white, EyeWhite);
                float ir = (near ? 5.2f : 4.2f) * k;
                float ix = ex + gaze.x, iy = ey + gaze.y;
                F(Intersect(Ci(ix, iy, ir), white), c.eye);
                F(Intersect(Ci(ix, iy, ir * 0.48f), white), Hex("0D0807"));
                F(Intersect(Ci(ix + ir * 0.35f, iy + ir * 0.4f, ir * (expr == "excited" ? 0.36f : 0.27f)), white), Color.white);
                if (float.IsNaN(cut))
                    F(Ar(ex, ey - ry * 0.15f * k, rx * k * 1.02f, 2.5f, 15, 165), lash);
                else
                    F(Ca(ex - rx - 0.5f, cut + (near ? 0.6f : -0.6f), ex + rx + 0.5f, cut + (near ? -0.6f : 0.6f), 1.5f), lash);
                F(Ar(ex, ey + ry * 0.3f, rx, 1.1f, 205, 335), new Color(c.skinShade.r, c.skinShade.g, c.skinShade.b, 0.55f));
                if (c.lashes)
                {
                    float topY = float.IsNaN(cut) ? ey + 2 : cut;
                    if (near) F(Ca(ex + rx, topY, ex + rx + 4.5f, topY + 3.5f, 1.2f), lash);
                    else F(Ca(ex - rx, topY, ex - rx - 3.5f, topY + 3.5f, 1.1f), lash);
                }
            }
            Eye(54, 74, 6.2f, 6.6f, false);
            Eye(82, 74, 8.6f, 7f, true);

            // --- brows
            float up = expr == "excited" ? 5f : expr == "smile" ? 3f : expr == "blink" ? 1f : 0f;
            float inner = expr == "focus" ? -3f : expr == "mean" ? -5.5f : expr == "excited" ? 1.5f : 0f;
            float nearUp = expr == "proud" ? 5f : 0f, farUp = expr == "proud" ? -1f : 0f;
            Sdf farBrow = Ca(46, 87 + up + farUp + (inner < 0 ? 1 : 0), 61, 89 + up + farUp + inner, c.browThick);
            Sdf nearBrow = Ca(73, 89 + up + nearUp + inner, 94, 87 + up + nearUp + (inner < 0 ? 1 : 0) + (nearUp > 0 ? 2 : 0), c.browThick);
            if (c.browSlit) nearBrow = Subtract(nearBrow, Ca(86, 82, 84, 98, 1.3f));
            F(farBrow, c.brow);
            F(nearBrow, c.brow);

            // --- nose
            float nw = c.noseW;
            F(Ca(68, 80, 64, 64, 1.5f), new Color(c.skinShade.r, c.skinShade.g, c.skinShade.b, 0.5f));
            F(Intersect(El(62, 61.5f, 5.5f * nw, 4.2f), Below(60.5f)), new Color(c.skinShade.r, c.skinShade.g, c.skinShade.b, 0.85f));
            F(El(60.5f, 59.8f, 1.9f * nw, 1.2f), Hex("2A1410", 0.6f));
            F(Ci(61, 64.5f, 1.7f), new Color(1, 1, 1, 0.22f));
            if (c.noseRing) F(Intersect(Rg(59.5f, 58.3f, 2.6f, 1.1f), Below(59)), c.metal);

            // --- mouth
            Color lipLine = Color.Lerp(c.lip, Color.black, 0.35f);
            Color lowerLip = new Color(c.lip.r, c.lip.g, c.lip.b, c.lipFull ? 0.55f : 0.35f);
            switch (expr)
            {
                case "excited":
                {
                    Sdf m = Intersect(El(63, 46, 9, 8.5f), Below(48.5f));
                    F(m, Hex("3A1218"));
                    F(Intersect(m, Above(45.5f)), Hex("F7F3EE"));
                    F(Intersect(m, Ci(65, 36, 6)), Hex("E5626F"));
                    break;
                }
                case "smile":
                {
                    Sdf m = Intersect(El(63, 47.5f, 10.5f, 6.5f), Below(48.5f));
                    F(m, Hex("3A1218"));
                    F(Intersect(m, Above(45)), Hex("F7F3EE"));
                    F(Ca(52.5f, 48.5f, 51, 50.5f, 1.3f), lipLine);
                    F(Ca(73.5f, 48.5f, 75, 50.5f, 1.3f), lipLine);
                    F(Ca(59, 39.5f, 67, 39.5f, c.lipFull ? 2.6f : 2f), lowerLip);
                    break;
                }
                case "mean":
                    F(Ar(63, 40.5f, 7.5f, 2.1f, 55, 125), lipLine);
                    F(Ca(59.5f, 41.5f, 66.5f, 41.5f, c.lipFull ? 2.6f : 2f), lowerLip);
                    break;
                case "focus":
                    F(Ca(58, 46.2f, 67.5f, 45.6f, 1.4f), lipLine);
                    F(Ca(59.5f, 42, 66.5f, 42, c.lipFull ? 2.6f : 2f), lowerLip);
                    break;
                case "proud":
                    F(Ca(57, 45, 69, 48, 1.5f), lipLine);
                    F(Ca(69, 48, 71.5f, 50.5f, 1.3f), lipLine);
                    F(Ca(59.5f, 41.5f, 67, 41.5f, c.lipFull ? 2.6f : 2f), lowerLip);
                    break;
                default: // idle / blink: relaxed half smile
                    F(Ar(63, 52, 8, 2.2f, 232, 308), lipLine);
                    F(Ca(59.5f, 41.2f, 66.5f, 41.2f, c.lipFull ? 2.6f : 2f), lowerLip);
                    break;
            }
            if (happy)
            {
                F(El(84, 58, 8, 4.5f), Hex("FF6F8A", 0.14f));
                F(El(52, 58, 5, 3.5f), Hex("FF6F8A", 0.12f));
            }

            // --- hair (front)
            draw = On(HeadPart.HairFront);
            Sdf cap =Subtract(Ci(84, 96, 47), Union(El(62, 76, 26, 28), Below(78)));
            switch (c.hairStyle)
            {
                case HairStyle.Twists:
                {
                    F(cap, c.hair);
                    F(Intersect(cap, Below(100)), new Color(c.skin.r, c.skin.g, c.skin.b, 0.25f)); // faded sides
                    for (int ring = 0; ring < 2; ring++)
                    {
                        float r0 = ring == 0 ? 38 : 28, r1 = ring == 0 ? 53 : 45;
                        for (float a = ring == 0 ? 28 : 48; a <= (ring == 0 ? 168 : 150); a += ring == 0 ? 10 : 14)
                        {
                            float rad = a * Mathf.Deg2Rad, jitter = Mathf.Sin(a * 7.3f) * 0.12f;
                            var dir = new Vector2(Mathf.Cos(rad + jitter), Mathf.Sin(rad + jitter));
                            var b0 = new Vector2(84, 94) + dir * r0;
                            var b1 = new Vector2(84, 94) + dir * r1;
                            F(Ca(b0.x, b0.y, b1.x, b1.y, 5.6f), c.hair);
                            var mid = Vector2.Lerp(b0, b1, 0.7f);
                            F(Ci(mid.x - 1.2f, mid.y + 1.2f, 1.7f), new Color(c.hairTip.r, c.hairTip.g, c.hairTip.b, 0.9f));
                        }
                    }
                    break;
                }
                case HairStyle.Messy:
                {
                    F(cap, c.hair);
                    F(Po(40, 106, 60, 110, 44, 86), c.hair);
                    F(Po(54, 110, 76, 110, 62, 90), c.hair);
                    F(Po(70, 112, 90, 108, 82, 94), c.hair);
                    F(Po(70, 136, 86, 140, 66, 150), c.hair);
                    F(Po(92, 138, 108, 132, 104, 150), c.hair);
                    F(Po(110, 128, 124, 116, 130, 132), c.hair);
                    F(Ca(64, 126, 96, 134, 2), new Color(c.hairTip.r, c.hairTip.g, c.hairTip.b, 0.8f));
                    F(Ca(100, 124, 116, 112, 1.6f), new Color(c.hairTip.r, c.hairTip.g, c.hairTip.b, 0.7f));
                    break;
                }
                case HairStyle.Bob:
                {
                    var grad = Grad(30, c.hairTip, 128, c.hair);
                    FG(cap, grad);
                    FG(Intersect(Intersect(Ci(84, 96, 48), Above(89)), LeftOf(98)), grad);
                    FG(Ca(102, 92, 108, 40, 8), grad);
                    F(Ca(62, 128, 94, 136, 2.2f), new Color(1, 1, 1, 0.3f));
                    F(Ca(48, 96, 92, 96, 1.2f), new Color(0, 0, 0, 0.12f));
                    break;
                }
                case HairStyle.LocsCap:
                {
                    F(Intersect(Subtract(Ci(84, 96, 47), El(62, 76, 26, 28)), Intersect(Below(96), Above(78))), c.hair);
                    Sdf dome = Intersect(Ci(84, 96, 48), Above(93));
                    FG(dome, Grad(93, Color.Lerp(c.cap, Color.black, 0.25f), 144, c.cap));
                    F(Po(118, 104, 154, 96, 152, 88, 118, 93), Color.Lerp(c.cap, Color.black, 0.3f));
                    F(Intersect(El(54, 93, 9, 8), Above(93)), c.hair);
                    F(Ca(45, 94.5f, 63, 94.5f, 1.6f), Hex("1A1A1A"));
                    F(Ca(84, 143, 68, 100, 1.1f), new Color(0, 0, 0, 0.3f));
                    F(Ci(84, 144, 3), Color.Lerp(c.cap, Color.black, 0.3f));
                    F(Ca(56, 128, 80, 138, 2.2f), new Color(1, 1, 1, 0.18f));
                    break;
                }
                case HairStyle.DreadsHalfUp:
                case HairStyle.DreadsTied:
                {
                    // roots: dark cap with parted sections running back
                    F(cap, c.hair);
                    var parting = new Color(c.skinShade.r, c.skinShade.g, c.skinShade.b, 0.45f);
                    Vector2 target = c.hairStyle == HairStyle.DreadsTied ? new Vector2(124, 96) : new Vector2(112, 140);
                    for (int i = 0; i < 6; i++)
                    {
                        float sx = 44 + i * 9, sy = 104 + i * 2;
                        F(Intersect(Ca(sx, sy, Mathf.Lerp(sx, target.x, 0.75f), Mathf.Lerp(sy, target.y, 0.75f), 0.8f), cap), parting);
                    }
                    F(Ca(58, 128, 86, 138, 2f), new Color(hairLight.r, hairLight.g, hairLight.b, 0.6f));
                    if (c.hairStyle == HairStyle.DreadsTied)
                    {
                        F(Rg(124, 96, 6, 2.6f), c.metal);
                        Dread(98, 92, 104, 40, 4.2f, 2, true); // one loose dread in front of the ear
                    }
                    break;
                }
                case HairStyle.Braids:
                {
                    F(cap, c.hair);
                    for (int i = 0; i < 7; i++)
                    {
                        float sx = 42 + i * 8, sy = 102 + i * 3;
                        F(Intersect(Ca(sx, sy, 120, 92 + i * 2, 1f), cap), new Color(hairLight.r, hairLight.g, hairLight.b, 0.5f));
                    }
                    Braid(98, 92, 102, 22, 3.4f, true);
                    Braid(104, 90, 110, 26, 3.4f, false);
                    break;
                }
                case HairStyle.LongStraight:
                {
                    FG(cap, Grad(60, c.hairTip, 140, c.hair));
                    F(Po(38, 104, 74, 110, 56, 82, 42, 88), c.hair);
                    FG(Ca(100, 96, 104, 26, 7.5f), Grad(26, c.hairTip, 96, c.hair));
                    break;
                }
                case HairStyle.CurlyFade:
                {
                    F(Intersect(cap, Below(104)), new Color(c.hair.r, c.hair.g, c.hair.b, 0.45f));
                    for (float y = 104; y <= 146; y += 7.5f)
                    for (float x = 42; x <= 128; x += 8.5f)
                    {
                        float jx = x + Mathf.Sin(y * 1.7f + x) * 2.5f, jy = y + Mathf.Cos(x * 1.3f) * 2f;
                        if ((new Vector2(jx, jy) - new Vector2(84, 98)).magnitude > 50) continue;
                        if (jy < 106 && jx < 92) continue; // keep the forehead clear
                        F(Ci(jx, jy, 6.5f), c.hair);
                        F(Ar(jx, jy, 3.5f, 1.2f, 40, 200), new Color(hairLight.r, hairLight.g, hairLight.b, 0.7f));
                    }
                    break;
                }
            }

            // --- accessories on the head
            draw = On(HeadPart.Accessories);
            if (part == HeadPart.Accessories) FaceTattoos();
            if (c.cap.a > 0 && c.hairStyle != HairStyle.LocsCap)
            {
                // backwards cap (customization accessory)
                Sdf dome = Intersect(Ci(84, 96, 49), Above(93));
                FG(dome, Grad(93, Color.Lerp(c.cap, Color.black, 0.25f), 145, c.cap));
                F(Po(118, 104, 154, 96, 152, 88, 118, 93), Color.Lerp(c.cap, Color.black, 0.3f));
                F(Ca(45, 94.5f, 63, 94.5f, 1.6f), Hex("1A1A1A"));
                F(Ca(84, 145, 68, 100, 1.1f), new Color(0, 0, 0, 0.3f));
                F(Ci(84, 145, 3), Color.Lerp(c.cap, Color.black, 0.3f));
                F(Ca(56, 128, 80, 138, 2.2f), new Color(1, 1, 1, 0.18f));
            }
            if (c.beanie.a > 0)
            {
                Sdf dome = Intersect(Ci(84, 98, 50), Above(100));
                FG(dome, Grad(100, Color.Lerp(c.beanie, Color.black, 0.2f), 150, c.beanie));
                for (int i = 0; i < 12; i++) F(Intersect(Ca(36 + i * 8, 100, 40 + i * 7, 150, 0.8f), dome), new Color(0, 0, 0, 0.15f));
                F(Intersect(Ci(84, 98, 51), Intersect(Above(97), Below(110))), Color.Lerp(c.beanie, Color.black, 0.25f));
                for (int i = 0; i < 14; i++) F(Intersect(Ca(34 + i * 7.5f, 98, 34 + i * 7.5f, 109, 1f), Ci(84, 98, 51)), new Color(0, 0, 0, 0.18f));
            }
            if (c.sunglasses)
            {
                Color lens = Hex("0E0D14", 0.96f);
                F(Bx(82, 75, 11.5f, 7.5f, 4), lens);
                F(Bx(55, 75, 7.5f, 7, 3.5f), lens);
                F(Ca(63, 78, 70.5f, 78, 1.3f), c.metal);
                F(Ca(93.5f, 78, 104, 76, 1.5f), c.metal);
                F(Ca(75, 79, 80, 78, 1f), new Color(1, 1, 1, 0.4f));
            }
            if (c.septum) F(Intersect(Rg(61, 57.2f, 2.4f, 1f), Below(58)), c.metal);
            if (c.glasses)
            {
                Color frame = Hex("1E1E24");
                F(Ci(82, 74, 10.5f), new Color(1, 1, 1, 0.1f));
                F(Ci(55, 74, 8), new Color(1, 1, 1, 0.1f));
                F(Rg(82, 74, 10.5f, 2), frame);
                F(Rg(55, 74, 8, 1.8f), frame);
                F(Ca(63, 76, 71.5f, 76, 1.2f), frame);
                F(Ca(92.5f, 76, 104, 74, 1.4f), frame);
                F(Ca(77, 79, 80, 81, 1f), new Color(1, 1, 1, 0.5f));
            }
            if (c.phonesOnHead)
            {
                F(Ar(86, 92, 54, 7, 30, 150), c.phones);
                F(Ar(86, 92, 49.5f, 2.5f, 45, 135), c.phonesAccent);
                F(Bx(106, 68, 11, 16, 7), c.phones);
                F(Bx(106, 68, 6.5f, 10.5f, 5), c.phonesAccent);
                F(Bx(106, 68, 4, 8, 3), c.phones);
                F(Ca(97, 78, 97, 60, 1.2f), new Color(1, 1, 1, 0.18f));
            }
            draw = On(HeadPart.HairFront) && c.beanie.a <= 0 && c.cap.a <= 0;
            if (c.hairStyle == HairStyle.DreadsHalfUp)
            {
                // half-up top knot: dreads gathered on the crown, tails falling back
                Dread(84, 128, 108, 144, 4.2f, 0, false);
                Dread(100, 124, 112, 142, 4.2f, 0, false);
                F(Ci(112, 144, 11), c.hair);
                for (int k = 0; k < 4; k++) F(Ar(112, 144, 7 - k * 1.5f, 1f, 30 + k * 40, 200 + k * 40), new Color(hairDark.r, hairDark.g, hairDark.b, 0.8f));
                F(Ca(102, 136, 120, 138, 2.4f), c.metal);
                Dread(118, 146, 140, 118, 4.2f, 4, true);
                Dread(120, 140, 146, 104, 4.2f, 5, true);
            }
            draw = On(HeadPart.Accessories);
            if (c.earHoop)
            {
                F(Rg(106.5f, 50, 5.5f, 1.8f), c.metal);
                F(Ci(109, 64, 1.6f), c.metal);
                F(Ci(111, 73, 1.4f), c.metal);
            }
            if (c.earStud)
            {
                F(Ci(106, 57, 2.8f), c.metal);
                F(Ci(106, 57, 1.8f), Hex("E8F6FF"));
            }
        }

        static Sdf Star(Vector2 c, float rOuter, float rInner)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = (90 + i * 36) * Mathf.Deg2Rad;
                float r = i % 2 == 0 ? rOuter : rInner;
                pts[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return Polygon(pts);
        }

        // ================================================================== LEGS & SNEAKERS

        static Color Dark(Color c, float k) => Color.Lerp(c, Color.black, k);
        static Color Al(Color c, float a) => new Color(c.r, c.g, c.b, a);

        /// <summary>Seated leg in the character's pants style (far leg is darker).</summary>
        static void Leg(SdfPainter p, Look c, bool far)
        {
            Vector2 hip = far ? new Vector2(118, 76) : new Vector2(112, 70);
            Vector2 knee = far ? new Vector2(66, 80) : new Vector2(54, 72);
            Vector2 ankle = far ? new Vector2(64, 30) : new Vector2(48, 28);
            float rt = far ? 16 : 18, rs = far ? 13 : 14;
            Color col = far ? c.pantsShade : c.pants, shade = far ? Dark(c.pantsShade, 0.25f) : c.pantsShade;
            if (c.pantsStyle == PantsStyle.Denim || c.pantsStyle == PantsStyle.Cargo) rs += 1.5f;

            p.Fill(Capsule(hip, knee, rt), col);
            p.Fill(Capsule(knee, ankle, rs), col);
            p.Fill(Intersect(Capsule(knee, ankle, rs), RRect(knee.x + 2, 0, 30, 100)), Al(shade, 0.6f)); // back of the shin
            p.Fill(Capsule(knee + new Vector2(-4, -10), knee + new Vector2(2, -22), 1.2f), Al(shade, 0.8f)); // fold
            if (!far) p.Fill(Capsule(new Vector2(96, 84), new Vector2(62, 86), 3), Al(Color.white, 0.07f));

            switch (c.pantsStyle)
            {
                case PantsStyle.Jogger:
                    p.Fill(RRect(ankle.x - 13, ankle.y - 4, 26, 9, 4), shade);
                    for (int i = 0; i < 4; i++) p.Fill(Capsule(new Vector2(ankle.x - 10 + i * 6, ankle.y - 3), new Vector2(ankle.x - 10 + i * 6, ankle.y + 4), 0.6f), Al(Color.black, 0.2f));
                    if (!far) p.Fill(Capsule(hip + new Vector2(0, 12), knee + new Vector2(4, 14), 1.4f), Al(c.accent, 0.8f));
                    break;
                case PantsStyle.Denim:
                    if (!far)
                    {
                        // outer seam stitching + knee fade + rolled cuff
                        for (float u = 0.05f; u < 0.95f; u += 0.08f)
                        {
                            var q = Vector2.Lerp(knee + new Vector2(10, 4), ankle + new Vector2(11, 4), u);
                            p.Fill(Capsule(q, q + new Vector2(-0.4f, -3), 0.5f), Al(Hex("E8C27A"), 0.7f));
                        }
                        p.Fill(Ellipse(knee + new Vector2(-6, 2), new Vector2(10, 8)), Al(Color.white, 0.12f));
                    }
                    p.Fill(RRect(ankle.x - 14, ankle.y - 4, 29, 8, 3), Color.Lerp(col, Color.white, 0.25f));
                    break;
                case PantsStyle.Cargo:
                    if (!far)
                    {
                        p.Fill(RRect(74, 62, 26, 18, 3), shade);
                        p.Fill(RRect(74, 74, 26, 6, 2), Dark(shade, 0.2f));
                        p.Fill(Circle(new Vector2(87, 77), 1.4f), Hex("D9DEE8"));
                        p.Fill(Capsule(new Vector2(70, 60), new Vector2(66, 44), 1.2f), Al(c.accent, 0.9f)); // strap
                    }
                    p.Fill(RRect(ankle.x - 13, ankle.y - 3, 27, 7, 3), shade);
                    p.Fill(Capsule(new Vector2(ankle.x + 12, ankle.y), new Vector2(ankle.x + 15, ankle.y - 8), 0.8f), c.accent); // bungee
                    break;
            }
        }

        /// <summary>Wide trousers: hem falling over the sneaker.</summary>
        static void Hem(SdfPainter p, Look c, bool far)
        {
            Vector2 ankle = far ? new Vector2(64, 30) : new Vector2(48, 28);
            Color col = far ? c.pantsShade : c.pants;
            p.Fill(Polygon(ankle + new Vector2(-14, 22), ankle + new Vector2(16, 22), ankle + new Vector2(20, -12), ankle + new Vector2(-20, -12)), col);
            p.Fill(Capsule(ankle + new Vector2(-4, 18), ankle + new Vector2(-8, -10), 1f), Al(Dark(col, 0.3f), 0.7f));
            p.Fill(Capsule(ankle + new Vector2(8, 18), ankle + new Vector2(12, -10), 1f), Al(Dark(col, 0.3f), 0.7f));
        }

        /// <summary>
        /// Original sneakers (facing left, toe at tx, ground gy, length L). Inspired by general categories only:
        /// retro low-top, basketball high-top, luxury minimal, chunky futuristic high-top. No real logos.
        /// </summary>
        static void Sneaker(SdfPainter p, Look c, float tx, float gy, float L, bool far)
        {
            float k = far ? 0.3f : 0f;
            Color shoe = Dark(c.shoe, k), acc = Dark(c.shoeAccent, k), sole = Dark(c.sole, k);
            Color line = Al(Dark(c.shoe, 0.45f + k), 0.8f), lace = Al(Dark(Color.white, k), 0.9f);
            void F(Sdf s, Color col) => p.Fill(s, col);
            Vector2 V(float x, float y) => new Vector2(tx + x, gy + y);
            Sdf Po(params Vector2[] v) { var poly = Polygon(v); return q => poly(q) - 2.5f; }

            switch (c.sneaker)
            {
                case SneakerStyle.LowRetro:
                    F(Po(V(2, 7), V(L - 2, 7), V(L - 3, 22), V(L - 22, 23), V(16, 17), V(4, 13)), shoe);
                    F(Po(V(2, 7), V(18, 7), V(16, 15), V(5, 12)), acc);                       // toe overlay
                    F(Po(V(L - 15, 7), V(L - 2, 7), V(L - 3, 21), V(L - 14, 20)), acc);       // heel overlay
                    F(Capsule(V(18, 11), V(L - 15, 16), 2.4f), acc);                          // side wave
                    F(Capsule(V(L - 24, 22), V(L - 5, 21), 2.8f), Dark(shoe, 0.2f));          // padded collar
                    for (int i = 0; i < 3; i++) F(Capsule(V(17 + i * 4, 18 + i * 1.2f), V(20 + i * 4, 19.5f + i * 1.2f), 0.9f), lace);
                    F(RRect(tx, gy, L, 8, 4), sole);
                    F(Capsule(V(3, 3), V(L - 3, 3), 0.6f), Al(Dark(sole, 0.3f), 0.8f));
                    break;
                case SneakerStyle.HighTop:
                    F(Po(V(2, 7), V(L - 2, 7), V(L - 1, 34), V(L - 18, 36), V(22, 20), V(5, 14)), shoe);
                    F(Po(V(2, 7), V(17, 7), V(15, 15), V(5, 12)), acc);                       // toe cap
                    for (int i = 0; i < 4; i++) F(Circle(V(7 + i * 2.5f, 11), 0.6f), line);    // perforations
                    F(Arc(V(L * 0.55f, 4), 14, 3.2f, 35, 145), acc);                         // original side swoop
                    F(Po(V(L - 13, 7), V(L - 1, 7), V(L, 30), V(L - 10, 30)), Dark(acc, 0.1f)); // heel panel
                    F(Capsule(V(L - 24, 27), V(L - 3, 25), 3.2f), acc);                      // ankle strap
                    F(Circle(V(L - 6, 25.5f), 1.4f), Hex("D9DEE8"));
                    F(Capsule(V(L - 20, 36), V(L - 2, 35), 3f), Dark(shoe, 0.15f));          // padded collar
                    for (int i = 0; i < 4; i++) F(Capsule(V(20 + i * 4, 20 + i * 3), V(24 + i * 4, 21.5f + i * 3), 0.9f), lace);
                    F(RRect(tx, gy, L, 9, 4), sole);
                    F(Capsule(V(3, 5), V(L - 3, 5), 0.6f), Al(Dark(sole, 0.25f), 0.8f));
                    break;
                case SneakerStyle.Minimal:
                    F(Union(Ellipse(V(13, 10), new Vector2(13, 5.5f)), RRect(tx + 10, gy + 5, L - 14, 13, 6)), shoe);
                    F(Capsule(V(16, 14), V(L - 12, 15), 0.5f), line);                        // clean stitch line
                    F(RRect(tx + L - 9, gy + 7, 5, 10, 2), acc);                             // heel tab
                    for (int i = 0; i < 4; i++) F(Circle(V(L - 16 + i * 1.6f, 11), 0.45f), Al(Dark(shoe, 0.5f), 0.9f)); // tiny stitches
                    for (int i = 0; i < 3; i++) F(Capsule(V(16 + i * 4, 16.5f), V(19 + i * 4, 17), 0.6f), Al(Dark(shoe, 0.15f), 1f));
                    F(RRect(tx + 1, gy, L - 2, 6, 3), sole);
                    break;
                case SneakerStyle.Chunky:
                    F(Po(V(2, 11), V(L - 1, 11), V(L, 38), V(L - 20, 40), V(20, 24), V(4, 18)), shoe);
                    F(Po(V(2, 11), V(20, 11), V(18, 20), V(5, 17)), Dark(Color.Lerp(shoe, Color.white, 0.35f), k)); // geometric toe
                    F(Po(V(22, 13), V(34, 13), V(30, 24), V(24, 22)), Al(Color.Lerp(shoe, Color.white, 0.25f), 1f));
                    F(Capsule(V(L - 24, 34), V(L - 27, 48), 4f), acc);                        // oversized tongue
                    F(Capsule(V(22, 26), V(L - 18, 34), 1.4f), lace);                         // wrap lace
                    F(Capsule(V(26, 22), V(L - 14, 28), 1.4f), lace);
                    F(Capsule(V(L - 2, 14), V(L - 1, 36), 0.9f), Hex("D9DEE8"));              // heel zip
                    F(RRect(tx - 2, gy, L + 4, 13, 5), sole);                                 // exaggerated sole
                    for (int i = 0; i < 7; i++) F(RRect(tx + 2 + i * (L / 7f), gy + 1, 2, 9, 1), Al(Dark(sole, 0.2f), 0.9f));
                    F(Capsule(V(-1, 12), V(L + 1, 12), 0.8f), Al(Dark(sole, 0.3f), 1f));
                    break;
            }
        }

        // ================================================================== BODY (seated, with chair)

        [Flags]
        public enum BodyPart
        {
            Base = 1, PantsFar = 2, ShoeFar = 4, PantsNear = 8, ShoeNear = 16, Hem = 32, Top = 64, Chains = 128, NeckTattoo = 256,
            All = 511,
        }

        static SdfPainter Body(Look c) => Body(c, BodyPart.All);

        static SdfPainter Body(Look c, BodyPart parts)
        {
            var p = new SdfPainter(200, 190);
            bool draw = true;
            bool Has(BodyPart x) => (parts & x) != 0;
            void F(Sdf s, Color col) { if (draw) p.Fill(s, col); }
            void FG(Sdf s, Func<Vector2, Color> col) { if (draw) p.Fill(s, col); }
            Vector2 V(float x, float y) => new Vector2(x, y);
            Color A(Color col, float a) => new Color(col.r, col.g, col.b, a);

            // chair
            draw = Has(BodyPart.Base);
            Color chair = Hex("24212E"), metal = Hex("3A3646");
            F(RRect(128, 64, 40, 92, 16), chair);
            F(RRect(133, 72, 6, 76, 3), A(Color.white, 0.06f));
            F(RRect(70, 54, 100, 14, 7), Hex("2C2838"));
            F(RRect(116, 16, 8, 40, 2), metal);
            F(Capsule(V(80, 14), V(160, 14), 4), metal);
            foreach (float x in new[] { 82f, 120f, 158f }) F(Circle(V(x, 8), 6), Hex("15131B"));

            // far leg (darker), then near leg; wide pants fall over the shoes
            if (Has(BodyPart.PantsFar)) Leg(p, c, far: true);
            if (Has(BodyPart.ShoeFar)) Sneaker(p, c, 38, 9, 46, far: true);
            if (Has(BodyPart.Hem) && c.pantsStyle == PantsStyle.Wide) Hem(p, c, true);
            if (Has(BodyPart.PantsNear)) Leg(p, c, far: false);
            if (Has(BodyPart.ShoeNear)) Sneaker(p, c, 16, 4, 52, far: false);
            if (Has(BodyPart.Hem) && c.pantsStyle == PantsStyle.Wide) Hem(p, c, false);

            // neck (under the collar)
            F(Capsule(V(100, 146), V(98, 178), 12), c.skin);
            F(Intersect(Capsule(V(100, 146), V(98, 178), 12), RRect(102, 140, 20, 40)), A(c.skinShade, 0.7f));
            draw = Has(BodyPart.NeckTattoo);
            if (c.neckTattoo)
            {
                F(Ring(V(103, 164), 3.6f, 1.3f), Ink);
                F(Capsule(V(99, 160), V(95, 156), 1f), Ink);
                F(Capsule(V(106, 160), V(109, 156), 1f), Ink);
                F(Capsule(V(93, 170), V(100, 172), 0.9f), Ink);
                F(Star(V(96, 151), 2.6f, 1.1f), Ink);
            }

            // torso
            draw = Has(BodyPart.Top);
            if (c.top == TopStyle.Hoodie) F(Ellipse(V(128, 148), V(24, 15)), c.clothShade);
            var poly = Polygon(V(86, 64), V(144, 64), V(148, 136), V(130, 150), V(94, 150), V(80, 138));
            Sdf torso = q => poly(q) - 6f;
            FG(torso, VGradient(64, c.clothShade, 154, c.cloth));
            F(Intersect(torso, RRect(136, 50, 30, 120)), A(c.clothShade, 0.55f));

            switch (c.top)
            {
                case TopStyle.Hoodie:
                    F(RRect(80, 58, 70, 10, 5), c.clothShade);
                    F(RRect(84, 72, 46, 26, 9), A(c.clothShade, 0.55f));
                    F(Capsule(V(88, 97), V(126, 97), 1.2f), A(Color.black, 0.15f));
                    F(Arc(V(102, 152), 14, 4, 190, 350), c.clothShade);
                    F(Capsule(V(95, 148), V(93, 120), 1.6f), c.accent);
                    F(Capsule(V(104, 148), V(103, 124), 1.6f), c.accent);
                    F(Capsule(V(93, 121), V(93, 116), 2.1f), Hex("D9DEE8"));
                    F(Capsule(V(103, 125), V(103, 120), 2.1f), Hex("D9DEE8"));
                    if (c.id == "player")
                    {
                        F(RRect(114, 108, 4, 10, 2), c.accent);
                        F(RRect(120, 108, 4, 17, 2), c.accent);
                        F(RRect(126, 108, 4, 7, 2), c.accent);
                        F(Ring(V(122, 116), 12, 1.6f), A(c.accent, 0.7f));
                    }
                    else
                    {
                        F(Star(V(122, 114), 7, 3), A(c.accent, 0.85f));
                    }
                    break;
                case TopStyle.Jacket:
                    for (int y = 80; y <= 136; y += 14) F(Capsule(V(84, y), V(146, y + 2), 1f), A(c.clothShade, 0.45f));
                    F(Polygon(V(86, 152), V(101, 152), V(97, 64), V(84, 64)), Hex("1A1626"));
                    F(Capsule(V(101, 152), V(97, 64), 1.2f), Hex("D9DEE8"));
                    F(Ellipse(V(108, 150), V(26, 8)), c.cloth);
                    F(Capsule(V(86, 147), V(128, 149), 1f), A(c.clothShade, 0.6f));
                    break;
                case TopStyle.Tee:
                    F(RRect(80, 58, 72, 12, 5), c.cloth);
                    F(Arc(V(100, 152), 12, 4, 190, 350), c.clothShade);
                    F(Circle(V(120, 106), 16), A(c.accent, 0.95f));
                    F(Polygon(V(122, 120), V(112, 104), V(119, 104), V(116, 92), V(128, 109), V(121, 109)), c.cloth);
                    F(RRect(104, 80, 32, 4, 2), c.accent);
                    F(RRect(110, 74, 20, 3, 1.5f), A(c.accent, 0.7f));
                    break;
                case TopStyle.Bomber:
                    F(Intersect(torso, Capsule(V(96, 70), V(108, 146), 7)), A(Color.white, 0.07f)); // satin sheen
                    F(RRect(80, 58, 70, 11, 4), c.clothShade);
                    F(RRect(80, 61, 70, 2, 1), c.accent);
                    F(RRect(80, 65, 70, 2, 1), c.accent);
                    F(Arc(V(101, 151), 13, 5, 185, 355), c.clothShade);
                    F(Arc(V(101, 151), 13, 1.4f, 185, 355), c.accent);
                    F(Capsule(V(99, 146), V(95, 64), 1.1f), Hex("D9DEE8"));
                    F(Circle(V(124, 128), 6), c.accent);
                    F(Star(V(124, 128), 4, 1.8f), c.cloth);
                    break;
                case TopStyle.Puffer:
                    for (float y = 74; y <= 140; y += 16)
                    {
                        F(Intersect(torso, Capsule(V(78, y), V(152, y + 1), 1.1f)), A(Color.black, 0.35f));
                        F(Intersect(torso, Capsule(V(84, y + 7), V(146, y + 8), 2.2f)), A(Color.white, 0.08f));
                    }
                    F(Ellipse(V(106, 150), V(27, 10)), c.cloth);
                    F(Ellipse(V(106, 152), V(20, 5)), c.clothShade);
                    F(Capsule(V(99, 148), V(95, 64), 1.6f), c.accent);
                    break;
                case TopStyle.Tracksuit:
                    F(Capsule(V(99, 150), V(95, 64), 1.1f), Hex("D9DEE8"));
                    F(Capsule(V(88, 146), V(84, 64), 1.4f), c.accent);
                    F(Capsule(V(92, 146), V(88, 64), 1.4f), c.accent);
                    F(Arc(V(101, 151), 13, 5, 185, 355), c.clothShade);
                    break;
            }

            // headphones around the neck
            if (c.phonesOnNeck)
            {
                F(Arc(V(100, 152), 20, 6, 195, 345), c.phones);
                F(Box(V(80, 150), V(7, 10), 5, -20), c.phones);
                F(Box(V(80, 150), V(4, 6), 3, -20), c.phonesAccent);
                F(Box(V(121, 152), V(7, 10), 5, 20), c.phones);
            }

            // chains
            draw = Has(BodyPart.Chains);
            if (c.chainThin)
            {
                F(Arc(V(100, 150), 16, 1.8f, 215, 325), c.metal);
                if (c.id == "melody") F(Star(V(100, 131), 4, 1.8f), c.metal);
                else F(RRect(96, 124, 8, 10, 2), c.metal);
            }
            if (c.chainCuban)
            {
                for (float a = 205; a <= 335; a += 9)
                {
                    var q = V(100, 150) + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * 17;
                    F(Circle(q, 3), c.metal);
                    F(Circle(q + V(-0.8f, 0.8f), 1.1f), Hex("FFF1B0"));
                }
                F(Arc(V(100, 150), 27, 1.6f, 232, 308), c.metal);
                F(Circle(V(100, 121), 6.5f), c.metal);
                F(Circle(V(100, 121), 3.6f), Hex("1A1A1A"));
                F(Circle(V(99, 122), 1.2f), c.metal);
            }
            return p;
        }

        // ================================================================== ARM (hangs from the shoulder, hand at the bottom)

        [Flags]
        public enum ArmPart { Sleeve = 1, Hand = 2, TatArm = 4, TatHand = 8, Watch = 16, Rings = 32, All = 63 }

        static SdfPainter Arm(Look c) => Arm(c, ArmPart.All);

        static SdfPainter Arm(Look c, ArmPart parts)
        {
            var p = new SdfPainter(56, 140);
            bool draw = true;
            bool Has(ArmPart x) => (parts & x) != 0;
            void F(Sdf s, Color col) { if (draw) p.Fill(s, col); }
            void FG(Sdf s, Func<Vector2, Color> col) { if (draw) p.Fill(s, col); }
            Vector2 V(float x, float y) => new Vector2(x, y);
            Color A(Color col, float a) => new Color(col.r, col.g, col.b, a);

            draw = Has(ArmPart.Sleeve);
            switch (c.top)
            {
                case TopStyle.Hoodie:
                    FG(Capsule(V(28, 126), V(28, 46), 14), VGradient(46, c.clothShade, 126, c.cloth));
                    F(Intersect(Capsule(V(28, 126), V(28, 46), 14), RRect(33, 30, 20, 110)), A(c.clothShade, 0.5f));
                    F(RRect(15, 34, 26, 14, 6), c.clothShade);
                    break;
                case TopStyle.Jacket:
                    FG(Capsule(V(28, 124), V(28, 48), 16), VGradient(48, c.clothShade, 124, c.cloth));
                    foreach (float y in new[] { 66f, 86f, 106f }) F(Capsule(V(14, y), V(42, y + 1), 1f), A(c.clothShade, 0.5f));
                    F(RRect(15, 36, 26, 13, 6), c.clothShade);
                    break;
                case TopStyle.Tee:
                    var forearm = Capsule(V(28, 104), V(28, 34), 11);
                    F(forearm, c.skin);
                    F(Intersect(forearm, RRect(32, 20, 20, 100)), A(c.skinShade, 0.6f));
                    draw = Has(ArmPart.TatArm);
                    if (c.armTattoo)
                    {
                        Sdf In(Sdf s) => Intersect(s, forearm);
                        F(In(Capsule(V(14, 88), V(42, 88), 1.2f)), Ink);
                        F(In(Capsule(V(14, 84), V(42, 84), 0.8f)), Ink);
                        F(In(Ring(V(27, 70), 6, 1.4f)), Ink);
                        F(In(Ring(V(27, 70), 2.5f, 1.2f)), Ink);
                        F(In(Capsule(V(21, 62), V(17, 56), 1f)), Ink);
                        F(In(Capsule(V(33, 62), V(37, 56), 1f)), Ink);
                        F(In(Star(V(25, 50), 4.5f, 2f)), Ink);
                        F(In(Capsule(V(31, 46), V(36, 52), 0.8f)), Ink);
                        F(In(Capsule(V(18, 40), V(38, 42), 1.1f)), Ink);
                    }
                    draw = Has(ArmPart.Sleeve);
                    FG(Capsule(V(28, 126), V(28, 100), 18), VGradient(96, c.clothShade, 126, c.cloth));
                    F(RRect(10, 94, 36, 8, 4), c.clothShade);
                    break;
                case TopStyle.Bomber:
                    FG(Capsule(V(28, 126), V(28, 46), 15), VGradient(46, c.clothShade, 126, c.cloth));
                    F(Capsule(V(22, 120), V(22, 54), 2.5f), A(Color.white, 0.08f));
                    F(RRect(14, 33, 28, 14, 5), c.clothShade);
                    F(RRect(14, 37, 28, 2, 1), c.accent);
                    F(RRect(14, 41, 28, 2, 1), c.accent);
                    break;
                case TopStyle.Puffer:
                    FG(Capsule(V(28, 124), V(28, 48), 17), VGradient(48, c.clothShade, 124, c.cloth));
                    foreach (float y in new[] { 64f, 82f, 100f, 116f }) F(Capsule(V(12, y), V(44, y + 1), 1.1f), A(Color.black, 0.35f));
                    foreach (float y in new[] { 73f, 91f, 108f }) F(Capsule(V(16, y), V(34, y), 2f), A(Color.white, 0.08f));
                    F(RRect(15, 35, 26, 13, 6), c.clothShade);
                    break;
                case TopStyle.Tracksuit:
                    FG(Capsule(V(28, 126), V(28, 46), 14), VGradient(46, c.clothShade, 126, c.cloth));
                    F(Capsule(V(36, 124), V(36, 48), 1.3f), c.accent);
                    F(Capsule(V(39, 120), V(39, 50), 1.3f), c.accent);
                    F(RRect(15, 34, 26, 13, 6), c.clothShade);
                    break;
            }

            // hand
            draw = Has(ArmPart.Hand);
            Color hs = A(c.skinShade, 0.7f);
            F(Ellipse(V(28, 22), V(11, 13)), c.skin);
            F(Capsule(V(20, 14), V(21, 7), 3.6f), c.skin);
            F(Capsule(V(26, 12), V(26.5f, 4.5f), 3.6f), c.skin);
            F(Capsule(V(32, 12), V(32.5f, 5.5f), 3.6f), c.skin);
            F(Capsule(V(37, 15), V(38, 9.5f), 3.2f), c.skin);
            F(Capsule(V(18, 27), V(14, 17), 4.2f), c.skin);
            F(Intersect(Ellipse(V(28, 22), V(11, 13)), RRect(31, 0, 20, 40)), hs);
            F(Capsule(V(21, 15), V(36, 16), 0.8f), A(c.skinShade, 0.5f));

            draw = Has(ArmPart.TatHand);
            if (c.handTattoo)
            {
                if (c.id == "melody")
                    F(Subtract(Circle(V(28, 25), 4), Circle(V(30, 26.5f), 3.4f)), Ink);
                else if (c.id == "drum")
                {
                    F(Star(V(27, 25), 4.5f, 2f), Ink);
                    foreach (float x in new[] { 21f, 26.5f, 32.5f }) F(Circle(V(x, 9.5f), 1f), Ink);
                }
                else
                {
                    F(Ellipse(V(26, 20), V(2.8f, 2.1f)), Ink);
                    F(Capsule(V(28.4f, 20.5f), V(28.4f, 29), 0.9f), Ink);
                    F(Capsule(V(28.4f, 29), V(31.5f, 27), 0.9f), Ink);
                }
            }
            draw = Has(ArmPart.Rings);
            if (c.rings)
            {
                F(Capsule(V(23.5f, 10.5f), V(28.8f, 10.5f), 1.3f), c.metal);
                F(Capsule(V(29.5f, 10.5f), V(35, 10.5f), 1.3f), c.metal);
            }
            draw = Has(ArmPart.Watch);
            if (c.watchSmart)
            {
                F(RRect(15, 36, 26, 10, 3), Hex("141418"));
                F(RRect(19, 37.5f, 18, 7, 2), Hex("3DE1FF", 0.9f));
            }
            if (c.watchGold)
            {
                F(RRect(15, 33, 26, 11, 3), c.metal);
                F(Circle(V(28, 38.5f), 4.2f), Hex("1A1A1A"));
                F(Ring(V(28, 38.5f), 4.6f, 1f), Hex("FFF1B0"));
            }
            if (c.bracelets)
            {
                F(Capsule(V(16, 40), V(40, 40), 1.6f), c.metal);
                for (float x = 17; x <= 39; x += 4) F(Circle(V(x, 44), 1.8f), x % 8 < 4 ? c.accent : Hex("F4F1FA"));
            }
            return p;
        }

        // ================================================================== PORTRAIT (producer cards)

        static SdfPainter Portrait(Look c) => Portrait(c, null, null, c.accent);

        static SdfPainter Portrait(Look c, Action<SdfPainter> background, Action<SdfPainter> overlay, Color ring)
        {
            var p = new SdfPainter(256, 256);
            Vector2 V(float x, float y) => new Vector2(x, y);
            p.Fill(Circle(V(128, 128), 124), VGradient(0, Color.Lerp(c.portraitBg, Color.black, 0.4f), 256, c.portraitBg));
            background?.Invoke(p);
            p.Fill(Ring(V(128, 128), 122, 4), new Color(ring.r, ring.g, ring.b, 0.9f));

            // shoulders + neck
            Sdf shoulders = Intersect(Ellipse(V(140, 6), V(110, 70)), Circle(V(128, 128), 124));
            p.Fill(Capsule(V(138, 60), V(136, 96), 22), c.skin);
            p.Fill(shoulders, VGradient(0, c.clothShade, 76, c.cloth));
            if (c.top == TopStyle.Jacket)
                p.Fill(Polygon(V(118, 70), V(146, 70), V(140, 0), V(122, 0)), Hex("1A1626"));
            if (c.top == TopStyle.Tee)
            {
                p.Fill(Arc(V(138, 70), 22, 6, 195, 345), c.clothShade);
                p.Fill(Circle(V(170, 30), 13), new Color(c.accent.r, c.accent.g, c.accent.b, 0.9f));
            }
            if (c.top == TopStyle.Puffer)
            {
                for (float y = 10; y < 70; y += 18) p.Fill(Intersect(shoulders, Capsule(V(20, y), V(250, y + 2), 1.4f)), new Color(0, 0, 0, 0.3f));
                p.Fill(Ellipse(V(138, 70), V(42, 13)), c.cloth);
                p.Fill(Capsule(V(138, 62), V(136, 0), 2f), c.accent);
            }
            if (c.top == TopStyle.Tracksuit)
            {
                p.Fill(Arc(V(138, 72), 24, 9, 190, 350), c.clothShade);
                p.Fill(Intersect(shoulders, Capsule(V(60, 54), V(30, 0), 2.2f)), c.accent);
                p.Fill(Intersect(shoulders, Capsule(V(66, 56), V(36, 0), 2.2f)), c.accent);
                p.Fill(Capsule(V(138, 62), V(136, 0), 1.4f), Hex("D9DEE8"));
            }
            if (c.top == TopStyle.Bomber)
            {
                p.Fill(Arc(V(138, 72), 24, 8, 190, 350), c.clothShade);
                p.Fill(Arc(V(138, 72), 24, 2, 190, 350), c.accent);
            }
            if (c.top == TopStyle.Hoodie)
            {
                p.Fill(Arc(V(138, 72), 24, 7, 190, 350), c.clothShade);
                p.Fill(Capsule(V(128, 66), V(126, 30), 2.4f), c.accent);
                p.Fill(Capsule(V(146, 66), V(146, 34), 2.4f), c.accent);
            }
            if (c.phonesOnNeck)
            {
                p.Fill(Arc(V(138, 74), 30, 9, 195, 345), c.phones);
                p.Fill(Box(V(108, 70), V(10, 14), 7, -20), c.phones);
                p.Fill(Box(V(108, 70), V(5, 8), 4, -20), c.phonesAccent);
            }
            if (c.chainCuban)
                for (float a = 210; a <= 330; a += 10)
                    p.Fill(Circle(V(138, 72) + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * 26, 4.2f), c.metal);
            if (c.chainThin) p.Fill(Arc(V(138, 72), 24, 2.6f, 215, 325), c.metal);

            DrawHead(p, c, c.portraitExpression, new Tf(new Vector2(128 - 84 * 1.45f, 136 - 80 * 1.45f), 1.45f), neck: false);
            overlay?.Invoke(p);
            return p;
        }
    }
}

