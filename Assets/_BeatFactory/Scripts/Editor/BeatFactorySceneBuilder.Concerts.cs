using BeatFactory.EditorTools.Art;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools
{
    /// <summary>Concerts &amp; tours UI: the screens build themselves from the UISkin at runtime; here we create the roots and assets.</summary>
    public static partial class BeatFactorySceneBuilder
    {
        public const string UISkinPath = ConfigFolder + "/UISkin.asset";

        static UISkin EnsureUISkin()
        {
            var skin = AssetDatabase.LoadAssetAtPath<UISkin>(UISkinPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<UISkin>();
                AssetDatabase.CreateAsset(skin, UISkinPath);
            }
            skin.display = UIStyle.Display;
            skin.heading = UIStyle.Heading;
            skin.body = UIStyle.Body;
            skin.rounded = UIFactory.RoundedSprite;
            skin.circle = UIFactory.CircleSprite;
            skin.outline = UIFactory.OutlineSprite;
            skin.panel = UIStyle.PanelSprite;
            skin.button = UIStyle.ButtonSprite;
            skin.softGlow = UIStyle.SoftGlowSprite;
            skin.radial = UIStyle.RadialSprite;
            skin.ring = UIStyle.RingSprite;
            skin.barGradient = UIStyle.BarGradientSprite;
            skin.confetti = LoadSprite(ArtGenerator.Fx + "/fx_confetti.png");
            skin.background = Theme.Background;
            skin.card = Theme.Panel;
            skin.cardLight = Theme.PanelLight;
            skin.text = Theme.TextMain;
            skin.textDim = Theme.TextDim;
            skin.money = Theme.AccentMoney;
            skin.pink = Theme.AccentPink;
            skin.cyan = Theme.AccentCyan;
            skin.fame = Theme.AccentFame;
            skin.gold = Gold;
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            return skin;
        }

        // ---- painted venues (Dark Rage set, 941×1672, Art/Venues/painted/venue_N.png): positions in painting px, top-left origin
        const string PaintedVenueDir = Root + "/Art/Venues/painted";
        const float PaintedW = 941f, PaintedH = 1672f;

        struct PaintedVenue
        {
            public Rect screen;          // LED screen (x, y, w, h)
            public float feetY, edgeY, trussY, artistPx;
            public Rect[] panels;        // side LED screens drawn over (none: the painting has its own)
        }

        struct PaintedAngle
        {
            public string file; public int slot;
            public Rect screen;            // painting px; width 0 = no screen visible
            public Vector2 feet; public float artistPx; // painting px; 0 = no artist
        }

        /// <summary>
        /// Extra camera angles per venue (Art/Venues/painted/venue_T_angleN.png). Slots: 0 after the drop = side of the stage ·
        /// 1 build-up = from the back of the venue · 2 chorus = from the stage over the crowd · 3 big moment = low at the barrier.
        /// </summary>
        static readonly PaintedAngle[][] PaintedAngles =
        {
            new PaintedAngle[0],
            new[]
            {
                new PaintedAngle { file = "venue_1_angle1", slot = 0, feet = new Vector2(520, 930), artistPx = 250 },
                new PaintedAngle { file = "venue_1_angle3", slot = 1, screen = new Rect(320, 315, 300, 147), feet = new Vector2(470, 598), artistPx = 82 },
                new PaintedAngle { file = "venue_1_angle0", slot = 2 },
                new PaintedAngle { file = "venue_1_angle2", slot = 3, screen = new Rect(232, 362, 488, 266), feet = new Vector2(470, 885), artistPx = 400 },
            },
            new[]
            {
                new PaintedAngle { file = "venue_2_angle1", slot = 0, feet = new Vector2(430, 1000), artistPx = 260 },
                new PaintedAngle { file = "venue_2_angle3", slot = 1, screen = new Rect(322, 393, 293, 134), feet = new Vector2(450, 735), artistPx = 72 },
                new PaintedAngle { file = "venue_2_angle0", slot = 2 },
                new PaintedAngle { file = "venue_2_angle2", slot = 3, screen = new Rect(222, 432, 475, 226), feet = new Vector2(470, 1030), artistPx = 390 },
            },
            new[]
            {
                new PaintedAngle { file = "venue_3_angle1", slot = 0, feet = new Vector2(430, 1090), artistPx = 270 },
                new PaintedAngle { file = "venue_3_angle3", slot = 1, screen = new Rect(342, 372, 276, 104), feet = new Vector2(470, 598), artistPx = 58 },
                new PaintedAngle { file = "venue_3_angle0", slot = 2, screen = new Rect(320, 627, 300, 124), feet = new Vector2(470, 888), artistPx = 75 },
                new PaintedAngle { file = "venue_3_angle2", slot = 3, screen = new Rect(122, 545, 698, 262), feet = new Vector2(470, 1070), artistPx = 380 },
            },
            new[]
            {
                new PaintedAngle { file = "venue_4_angle1", slot = 0, feet = new Vector2(430, 1140), artistPx = 270 },
                new PaintedAngle { file = "venue_4_angle3", slot = 1, screen = new Rect(380, 505, 196, 102), feet = new Vector2(470, 698), artistPx = 52 },
                new PaintedAngle { file = "venue_4_angle0", slot = 2 },
                new PaintedAngle { file = "venue_4_angle2", slot = 3, screen = new Rect(198, 628, 546, 270), feet = new Vector2(470, 1105), artistPx = 380 },
            },
        };

        static readonly PaintedVenue[] PaintedVenues =
        {
            new PaintedVenue { screen = new Rect(202, 402, 533, 272), feetY = 935, edgeY = 985, trussY = 262, artistPx = 235, panels = new Rect[0] }, // Club
            new PaintedVenue { screen = new Rect(215, 405, 513, 267), feetY = 880, edgeY = 945, trussY = 140, artistPx = 215, panels = new Rect[0] }, // Small Venue
            new PaintedVenue { screen = new Rect(224, 397, 494, 253), feetY = 960, edgeY = 1000, trussY = 230, artistPx = 160, panels = new Rect[0] }, // Concert Hall
            new PaintedVenue { screen = new Rect(258, 452, 430, 216), feetY = 945, edgeY = 1020, trussY = 230, artistPx = 145, panels = new Rect[0] }, // Arena
            new PaintedVenue { screen = new Rect(272, 507, 398, 214), feetY = 955, edgeY = 1030, trussY = 350, artistPx = 130, panels = new Rect[0] }, // Stadium
        };

        /// <summary>
        /// Phone screens painted in the crowd (below the stage edge): small, very bright violet-white spots. The painting is
        /// scanned in 12 px cells; the brightest cells, at least 34 px apart, become glint positions (design space).
        /// </summary>
        static Vector2[] FindPaintedPhones(string path, float edgePx)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(System.IO.File.ReadAllBytes(path))) return new Vector2[0];
            int w = tex.width, h = tex.height;
            var px = tex.GetPixels32();
            Object.DestroyImmediate(tex);
            const int cell = 12;
            var found = new System.Collections.Generic.List<(Vector2 p, int score)>();
            for (int cy = (int)edgePx + 10; cy + cell <= h - 20; cy += cell)
                for (int cx = 0; cx + cell <= w; cx += cell)
                {
                    int n = 0;
                    for (int y = cy; y < cy + cell; y++)
                        for (int x = cx; x < cx + cell; x++)
                        {
                            var c = px[(h - 1 - y) * w + x];
                            if (c.b > 225 && c.r > 190 && c.g > 150) n++; // glowing screen: near white with a violet cast
                        }
                    if (n >= 10) found.Add((new Vector2(cx + cell / 2f, cy + cell / 2f), n));
                }
            found.Sort((a, b) => b.score.CompareTo(a.score));
            var picked = new System.Collections.Generic.List<Vector2>();
            foreach (var f in found)
            {
                bool far = true;
                foreach (var q in picked) if ((q - f.p).sqrMagnitude < 34f * 34f) { far = false; break; }
                if (far) picked.Add(f.p);
                if (picked.Count >= 48) break;
            }
            return picked.ConvertAll(q => new Vector2(q.x * 800f / PaintedW, VenueY(q.y))).ToArray();
        }

        static void SetV2(SerializedProperty p, Vector2[] v)
        {
            v = v ?? new Vector2[0];
            p.arraySize = v.Length;
            for (int k = 0; k < v.Length; k++) p.GetArrayElementAtIndex(k).vector2Value = v[k];
        }

        /// <summary>
        /// Light fixtures painted above <paramref name="maxYPx"/>: tiny near-white hot spots (lens of a spot / moving head).
        /// Scanned in 14 px cells; the brightest cells at least 48 px apart become pulse + beam origins (design space).
        /// </summary>
        static Vector2[] FindPaintedLights(string path, float maxYPx)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!System.IO.File.Exists(path) || !tex.LoadImage(System.IO.File.ReadAllBytes(path))) return new Vector2[0];
            int w = tex.width, h = tex.height;
            var px = tex.GetPixels32();
            Object.DestroyImmediate(tex);
            const int cell = 14;
            var found = new System.Collections.Generic.List<(Vector2 p, int score)>();
            for (int cy = 10; cy + cell <= Mathf.Min(h, maxYPx); cy += cell)
                for (int cx = 0; cx + cell <= w; cx += cell)
                {
                    int n = 0;
                    for (int y = cy; y < cy + cell; y++)
                        for (int x = cx; x < cx + cell; x++)
                        {
                            var c = px[(h - 1 - y) * w + x];
                            if (c.r > 230 && c.g > 215 && c.b > 240) n++;
                        }
                    if (n >= 8 && n <= 150) found.Add((new Vector2(cx + cell / 2f, cy + cell / 2f), n));
                }
            found.Sort((a, b) => b.score.CompareTo(a.score));
            var picked = new System.Collections.Generic.List<Vector2>();
            foreach (var f in found)
            {
                bool far = true;
                foreach (var q in picked) if ((q - f.p).sqrMagnitude < 48f * 48f) { far = false; break; }
                if (far) picked.Add(f.p);
                if (picked.Count >= 24) break;
            }
            return picked.ConvertAll(q => new Vector2(q.x * 800f / PaintedW, VenueY(q.y))).ToArray();
        }

        /// <summary>Painting y (top-left origin) → concert design y (800×1400, bottom-left origin).</summary>
        static float VenueY(float py) => (PaintedH - py) * 1400f / PaintedH;

        /// <summary>Painting rect (top-left origin) → design rect as (x, y-bottom, w, h).</summary>
        static Vector4 VenueRect(Rect r) =>
            new Vector4(r.x * 800f / PaintedW, VenueY(r.y + r.height), r.width * 800f / PaintedW, r.height * 1400f / PaintedH);

        static void CreateConcertUI(RectTransform layer)
        {
            var skin = EnsureUISkin();
            int n = ArtGenerator.VenueScreens.Length;
            var venues = new Sprite[n];
            var crowds = new Sprite[n];
            var rects = new Vector4[n];
            var layouts = new VenueLayout[n];
            for (int i = 0; i < n; i++)
            {
                venues[i] = LoadSprite(ArtGenerator.VenuePath(i));
                crowds[i] = LoadSprite(ArtGenerator.CrowdPath(i));
                var r = ArtGenerator.VenueScreens[i];
                rects[i] = new Vector4(r.x, r.y, r.width, r.height);
                // painted venue (fans drawn in) replaces the generated one when its art exists
                var p = PaintedVenues[i];
                var sprite = ImportArt(PaintedVenueDir + "/venue_" + i + ".png", 2048);
                if (sprite == null) continue;
                venues[i] = sprite;
                rects[i] = VenueRect(p.screen);
                layouts[i] = new VenueLayout
                {
                    paintedCrowd = true,
                    stageY = VenueY(p.feetY), edgeY = VenueY(p.edgeY), trussY = VenueY(p.trussY),
                    artistHeight = p.artistPx * 1400f / PaintedH,
                    panels = System.Array.ConvertAll(p.panels, q => { var v = VenueRect(q); return new Vector4(v.x + v.z / 2, v.y + v.w / 2, v.z, v.w); }),
                    phones = FindPaintedPhones(PaintedVenueDir + "/venue_" + i + ".png", p.edgeY),
                    lights = FindPaintedLights(PaintedVenueDir + "/venue_" + i + ".png", p.edgeY - 20),
                };
                var angles = new System.Collections.Generic.List<VenueAngle>();
                foreach (var a in PaintedAngles[i])
                {
                    var sp = ImportArt(PaintedVenueDir + "/" + a.file + ".png", 2048);
                    if (sp == null) continue;
                    angles.Add(new VenueAngle
                    {
                        sprite = sp, slot = a.slot,
                        screen = a.screen.width > 0 ? VenueRect(a.screen) : Vector4.zero,
                        artist = new Vector2(a.feet.x * 800f / PaintedW, VenueY(a.feet.y)),
                        artistHeight = a.artistPx * 1400f / PaintedH,
                        lights = FindPaintedLights(PaintedVenueDir + "/" + a.file + ".png", a.artistPx > 0 ? a.feet.y - 20 : PaintedH * 0.45f),
                    });
                }
                layouts[i].angles = angles.ToArray();
            }

            RectTransform Root(string name)
            {
                var rt = UIFactory.Rect(name, layer);
                UIFactory.Stretch(rt);
                return rt;
            }

            // order = draw order: career page < concert show < tour results
            var goalsRoot = Root("GoalsScreen");
            var careerRoot = Root("ArtistCareerScreen");
            var showRoot = Root("ConcertShow");
            var tourRoot = Root("TourComplete");

            var show = showRoot.gameObject.AddComponent<ConcertShowUI>();
            var so = new SerializedObject(show);
            so.FindProperty("skin").objectReferenceValue = skin;
            UIFactory.SetArray(so.FindProperty("venueSprites"), venues);
            UIFactory.SetArray(so.FindProperty("crowdSprites"), crowds);
            var rp = so.FindProperty("screenRects");
            rp.arraySize = n;
            for (int i = 0; i < n; i++) rp.GetArrayElementAtIndex(i).vector4Value = rects[i];
            // front-row fan cut-outs (from the user's green-screen sheet)
            int fanCount = System.IO.File.Exists(FanSheetArt.FanPath(0)) ? 0 : FanSheetArt.Cut();
            var fanList = new System.Collections.Generic.List<Object>();
            for (int i = 0; System.IO.File.Exists(FanSheetArt.FanPath(i)); i++) fanList.Add(LoadSprite(FanSheetArt.FanPath(i)));
            UIFactory.SetArray(so.FindProperty("fanSprites"), fanList.ToArray());
            var lp = so.FindProperty("venueLayouts");
            lp.arraySize = n;
            for (int i = 0; i < n; i++)
            {
                var e = lp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("paintedCrowd").boolValue = layouts[i].paintedCrowd;
                e.FindPropertyRelative("stageY").floatValue = layouts[i].stageY;
                e.FindPropertyRelative("edgeY").floatValue = layouts[i].edgeY;
                e.FindPropertyRelative("artistHeight").floatValue = layouts[i].artistHeight;
                e.FindPropertyRelative("trussY").floatValue = layouts[i].trussY;
                var panels = e.FindPropertyRelative("panels");
                var src = layouts[i].panels ?? new Vector4[0];
                panels.arraySize = src.Length;
                for (int k = 0; k < src.Length; k++) panels.GetArrayElementAtIndex(k).vector4Value = src[k];
                var ap = e.FindPropertyRelative("angles");
                var aa = layouts[i].angles ?? new VenueAngle[0];
                ap.arraySize = aa.Length;
                for (int k = 0; k < aa.Length; k++)
                {
                    var q = ap.GetArrayElementAtIndex(k);
                    q.FindPropertyRelative("sprite").objectReferenceValue = aa[k].sprite;
                    q.FindPropertyRelative("screen").vector4Value = aa[k].screen;
                    q.FindPropertyRelative("artist").vector2Value = aa[k].artist;
                    q.FindPropertyRelative("artistHeight").floatValue = aa[k].artistHeight;
                    q.FindPropertyRelative("slot").intValue = aa[k].slot;
                    SetV2(q.FindPropertyRelative("lights"), aa[k].lights);
                }
                SetV2(e.FindPropertyRelative("lights"), layouts[i].lights);
                var phones = e.FindPropertyRelative("phones");
                var ph = layouts[i].phones ?? new Vector2[0];
                phones.arraySize = ph.Length;
                for (int k = 0; k < ph.Length; k++) phones.GetArrayElementAtIndex(k).vector2Value = ph[k];
            }
            so.FindProperty("beamSprite").objectReferenceValue = LoadSprite(ArtGenerator.BeamPath);
            so.FindProperty("glowSprite").objectReferenceValue = UIStyle.RadialSprite;
            so.FindProperty("crowdAtlas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtGenerator.CrowdAtlasPath);
            so.FindProperty("micSprite").objectReferenceValue = LoadSprite(ArtGenerator.MicPath);
            so.FindProperty("laserCoreSprite").objectReferenceValue = LoadSprite(ArtGenerator.LaserCorePath);
            so.FindProperty("laserGlowSprite").objectReferenceValue = LoadSprite(ArtGenerator.LaserGlowPath);
            so.FindProperty("flameSprite").objectReferenceValue = LoadSprite(ArtGenerator.FlamePath);
            so.FindProperty("sparkSprite").objectReferenceValue = LoadSprite(ArtGenerator.SparkPath);
            ArtGenerator.GenerateStadiumAerial(false);
            so.FindProperty("stadiumAerialSprite").objectReferenceValue = LoadSprite(ArtGenerator.StadiumAerialPath);
            so.ApplyModifiedPropertiesWithoutUndo();

            var career = careerRoot.gameObject.AddComponent<ArtistCareerScreen>();
            UIFactory.SetRef(career, "skin", skin);
            UIFactory.SetRef(career, "show", show);

            var goals = goalsRoot.gameObject.AddComponent<GoalsScreen>();
            UIFactory.SetRef(goals, "skin", skin);

            var languageRoot = Root("LanguagePicker");
            var picker = languageRoot.gameObject.AddComponent<LanguagePicker>();
            UIFactory.SetRef(picker, "skin", skin);

            var tour = tourRoot.gameObject.AddComponent<TourCompletePopup>();
            UIFactory.SetRef(tour, "skin", skin);
            UIFactory.SetRef(tour, "show", show);
        }
    }
}
