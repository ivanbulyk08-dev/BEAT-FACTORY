using BeatFactory.EditorTools.Art;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory.EditorTools
{
    /// <summary>The animated studio scene on the HOME screen (room, characters, producers, effects).</summary>
    public static partial class BeatFactorySceneBuilder
    {
        public static Sprite LoadSprite(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogWarning("[Beat Factory] Missing sprite: " + path);
            return s;
        }

        static Image Img(string name, Transform parent, Sprite sprite, float x, float y, float w, float h, Color? color = null)
        {
            var img = UIFactory.Image(name, parent, color ?? Color.white);
            img.sprite = sprite;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            return img;
        }

        /// <summary>Builds the studio card. Returns the point beats/notes come from (main monitor).</summary>
        static RectTransform CreateStudioScene(RectTransform home, Image background, Image accentLine, out TMP_Text genreTextOut)
        {
            // Rounded card that masks the room
            var card = UIFactory.RoundedImage("StudioScene", home, Theme.Panel);
            var crt = card.rectTransform;
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.offsetMin = new Vector2(40, -Theme.SceneTop - Theme.SceneHeight);
            crt.offsetMax = new Vector2(-40, -Theme.SceneTop);
            card.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            // Fixed-size room scaled to cover the card, anchored at the bottom: the full width (all stations) stays
            // visible and only the extra ceiling is cropped.
            var room = UIFactory.Rect("Room", crt);
            room.anchorMin = room.anchorMax = new Vector2(0.5f, 0f);
            room.pivot = new Vector2(0.5f, 0f);
            room.sizeDelta = new Vector2(ArtGenerator.SceneW, ArtGenerator.SceneH);
            var fit = card.gameObject.AddComponent<FitToParent>();
            UIFactory.SetRef(fit, "child", room);
            var fitSo = new SerializedObject(fit);
            fitSo.FindProperty("designSize").vector2Value = new Vector2(ArtGenerator.SceneW, ArtGenerator.SceneH);
            fitSo.ApplyModifiedPropertiesWithoutUndo();
            const float lift = ArtGenerator.Lift;
            Rect Up(Rect r) => new Rect(r.x, r.y + lift, r.width, r.height);

            var tiers = new Sprite[ArtGenerator.EnvPaths.Length];
            for (int i = 0; i < tiers.Length; i++) tiers[i] = LoadSprite(ArtGenerator.EnvPaths[i]);
            var back = Img("BackgroundBack", room, tiers[0], 0, 0, ArtGenerator.SceneW, ArtGenerator.SceneH);
            var front = Img("BackgroundFront", room, tiers[0], 0, 0, ArtGenerator.SceneW, ArtGenerator.SceneH);
            back.gameObject.SetActive(false);

            var glow = Img("LampGlow", room, LoadSprite(ArtGenerator.Fx + "/fx_glow.png"), 40, 120 + lift, 560, 420, new Color(1, 0.4f, 0.8f, 0.25f));

            // Animated extras per tier: DAW screens, speaker cones, LED strip
            string[] tierNames =
            {
                "Tier0_Basement", "Tier1_Bedroom", "Tier2_SmallPro", "Tier3_Professional", "Tier4_Modern",
                "Tier5_Luxury", "Tier6_Major", "Tier7_Elite", "Tier8_RecordLabel", "Tier9_Mega",
            };
            Color[] ledColors =
            {
                new Color(1f, 0.3f, 0.85f, 0.55f), new Color(1f, 0.4f, 0.8f, 0.45f), new Color(0.3f, 0.85f, 1f, 0.35f),
                new Color(1f, 0.75f, 0.4f, 0.35f), new Color(0.3f, 0.9f, 1f, 0.5f), new Color(1f, 0.78f, 0.35f, 0.4f),
                new Color(0.35f, 0.55f, 1f, 0.4f), new Color(0.75f, 0.35f, 1f, 0.55f), new Color(1f, 0.9f, 0.8f, 0.3f),
                new Color(1f, 0.3f, 0.8f, 0.6f),
            };
            bool[] ledCycle = { true, false, false, false, true, false, false, true, false, true };
            var overlays = new GameObject[tierNames.Length];
            for (int i = 0; i < overlays.Length; i++)
            {
                overlays[i] = CreateTierOverlay(room, tierNames[i],
                    System.Array.ConvertAll(ArtGenerator.ScreensFor(i), Up), System.Array.ConvertAll(ArtGenerator.ConesFor(i), Up),
                    ArtGenerator.LedFor(i) + new Vector3(lift, 0, 0), ledColors[i], ledCycle[i]);
                CreateRoomSigns(overlays[i].transform, i, lift);
            }

            // Each room places the desk differently: the producer sits beside the main monitor, facing it.
            var charPositions = new Vector2[tierNames.Length];
            var beatOrigins = new Vector2[tierNames.Length];
            var facing = new bool[tierNames.Length];
            for (int i = 0; i < tierNames.Length; i++)
            {
                var scr = ArtGenerator.ScreensFor(i)[0];
                facing[i] = ArtGenerator.TierMirrored[i];
                charPositions[i] = new Vector2(facing[i] ? scr.xMin - 100 : scr.xMax + 100, 60 + lift);
                beatOrigins[i] = scr.center + new Vector2(0, lift);
            }

            // Characters: main sits beside the monitor (facing it); producers stand in a full-width front row.
            CreateAlbumWall(room, lift);
            CreateArtistPosterWall(room, lift);
            var main = CreatePlayerRig(room, "MainCharacter", charPositions[0], 0.9f, true);
            var sc = ArtGenerator.StationCenters;
            var stations = new[]
            {
                CreateStation(room, 0, "beginner", sc[0], 0.2f),
                CreateStation(room, 1, "melody", sc[1], 0.45f),
                CreateStation(room, 2, "drum", sc[2], 0.7f),
            };

            // Lights-off overlay for the studio upgrade show (above characters, below effects)
            var blackout = UIFactory.Image("Blackout", room, new Color(0, 0, 0, 0));
            blackout.raycastTarget = false;
            UIFactory.Stretch(blackout.rectTransform);
            blackout.gameObject.SetActive(false);

            var origin = UIFactory.Rect("BeatOrigin", room);
            origin.anchorMin = origin.anchorMax = Vector2.zero;
            origin.anchoredPosition = beatOrigins[0];
            origin.sizeDelta = new Vector2(10, 10);

            // "+$5 BEAT CREATED" rises from here: right of the monitor, above the producers.
            var floatOrigin = UIFactory.Rect("FloatOrigin", room);
            floatOrigin.anchorMin = floatOrigin.anchorMax = Vector2.zero;
            floatOrigin.anchoredPosition = new Vector2(600, 300 + lift);
            floatOrigin.sizeDelta = new Vector2(10, 10);

            // Effects on top of the room
            var fxLayer = UIFactory.Rect("FX", room);
            UIFactory.Stretch(fxLayer);
            var fx = fxLayer.gameObject.AddComponent<SceneFX>();
            UIFactory.SetRef(fx, "layer", fxLayer);
            UIFactory.SetRef(fx, "shakeTarget", room);
            UIFactory.SetRef(fx, "noteSprite", LoadSprite(ArtGenerator.Fx + "/fx_note.png"));
            UIFactory.SetRef(fx, "sparkleSprite", LoadSprite(ArtGenerator.Fx + "/fx_sparkle.png"));
            UIFactory.SetRef(fx, "confettiSprite", LoadSprite(ArtGenerator.Fx + "/fx_confetti.png"));

            // Label: studio name + genre (top-left pill)
            var pill = UIFactory.RoundedImage("StudioLabel", crt, new Color(0.05f, 0.04f, 0.1f, 0.72f));
            var prt = pill.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0, 1);
            prt.pivot = new Vector2(0, 1);
            prt.sizeDelta = new Vector2(430, 104);
            prt.anchoredPosition = new Vector2(18, -18);
            pill.raycastTarget = false;
            var studioName = UIFactory.Text("StudioNameText", prt, "BASEMENT STUDIO", 34, Theme.TextMain, TextAlignmentOptions.TopLeft,
                new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(22, 0), new Vector2(-16, -12));
            studioName.fontStyle = FontStyles.Bold;
            studioName.richText = true;
            studioName.textWrappingMode = TextWrappingModes.NoWrap;
            studioName.enableAutoSizing = true;
            studioName.fontSizeMin = 18;
            studioName.fontSizeMax = 34;
            var genreText = UIFactory.Text("GenreText", prt, "LO-FI", 28, Theme.AccentCyan, TextAlignmentOptions.TopLeft,
                new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(22, 8), new Vector2(-16, 0));
            genreText.richText = true;
            genreTextOut = genreText;

            var theme = card.gameObject.AddComponent<GenreThemeApplier>();
            UIFactory.SetRef(theme, "background", background);
            UIFactory.SetRef(theme, "genreText", genreText);
            var themeSo = new SerializedObject(theme);
            UIFactory.SetArray(themeSo.FindProperty("accentGraphics"), new Object[] { accentLine });
            themeSo.ApplyModifiedPropertiesWithoutUndo();

            // Signed artists hang out in the top-right corner
            var stripHolder = UIFactory.Rect("Artists", crt);
            stripHolder.anchorMin = stripHolder.anchorMax = new Vector2(1, 1);
            stripHolder.pivot = new Vector2(1, 1);
            stripHolder.sizeDelta = new Vector2(420, 90);
            stripHolder.anchoredPosition = new Vector2(-14, -14);
            stripHolder.localScale = new Vector3(0.8f, 0.8f, 1f);
            CreateArtistStrip(stripHolder);
            var strip = stripHolder.Find("ArtistStrip") as RectTransform;
            strip.anchoredPosition = new Vector2(30, 8);

            CreateNextGoalHUD(crt);
            var banner = CreateUnlockBanner(crt, out var bannerTitle, out var bannerName, out var bannerSub);

            // STYLE button (opens the customization menu)
            var styleBtn = UIFactory.Button("StyleButton", crt, new Color(0.05f, 0.04f, 0.1f, 0.78f), "@style.button", 30, Theme.TextMain, out var styleLabel);
            styleLabel.font = UIStyle.Heading;
            styleLabel.characterSpacing = 6;
            styleLabel.alignment = TextAlignmentOptions.Right;
            styleLabel.margin = new Vector4(0, 0, 10, 0);
            var sbrt = (RectTransform)styleBtn.transform;
            sbrt.anchorMin = sbrt.anchorMax = new Vector2(1, 1);
            sbrt.pivot = new Vector2(1, 1);
            sbrt.sizeDelta = new Vector2(210, 78);
            sbrt.anchoredPosition = new Vector2(-18, -104);
            var hanger = UIFactory.Image("StyleIcon", sbrt, Theme.AccentCyan);
            hanger.sprite = LoadSprite(ArtGenerator.Icons + "/icon_producer.png");
            hanger.raycastTarget = false;
            hanger.rectTransform.anchorMin = hanger.rectTransform.anchorMax = new Vector2(0, 0.5f);
            hanger.rectTransform.sizeDelta = new Vector2(40, 40);
            hanger.rectTransform.anchoredPosition = new Vector2(42, 0);

            var view = card.gameObject.AddComponent<StudioSceneView>();
            UIFactory.SetRef(view, "backgroundFront", front);
            UIFactory.SetRef(view, "backgroundBack", back);
            UIFactory.SetRef(view, "lampGlow", glow);
            UIFactory.SetRef(view, "mainCharacter", main);
            UIFactory.SetRef(view, "fx", fx);
            UIFactory.SetRef(view, "beatOrigin", origin);
            UIFactory.SetRef(view, "studioNameText", studioName);
            UIFactory.SetRef(view, "blackout", blackout);
            UIFactory.SetRef(view, "room", room);
            UIFactory.SetRef(view, "banner", banner);
            UIFactory.SetRef(view, "bannerTitle", bannerTitle);
            UIFactory.SetRef(view, "bannerName", bannerName);
            UIFactory.SetRef(view, "bannerSub", bannerSub);
            var so = new SerializedObject(view);
            var cp = so.FindProperty("tierCharacterPositions");
            var bo = so.FindProperty("tierBeatOrigins");
            var fr = so.FindProperty("tierFacingRight");
            cp.arraySize = bo.arraySize = fr.arraySize = tierNames.Length;
            for (int i = 0; i < tierNames.Length; i++)
            {
                cp.GetArrayElementAtIndex(i).vector2Value = charPositions[i];
                bo.GetArrayElementAtIndex(i).vector2Value = beatOrigins[i];
                fr.GetArrayElementAtIndex(i).boolValue = facing[i];
            }
            UIFactory.SetArray(so.FindProperty("tierBackgrounds"), tiers);
            // The player's look now comes from the STYLE menu (PlayerAvatar), not per-tier outfits.
            UIFactory.SetArray(so.FindProperty("playerBodies"), new Object[0]);
            UIFactory.SetArray(so.FindProperty("playerArms"), new Object[0]);
            UIFactory.SetArray(so.FindProperty("tierOverlays"), overlays);
            UIFactory.SetArray(so.FindProperty("producerStations"), new Object[] { stations[0], stations[1], stations[2] });
            Color[] glows =
            {
                new Color(1f, 0.8f, 0.45f, 0.16f), new Color(1f, 0.45f, 0.85f, 0.2f), new Color(0.4f, 0.8f, 1f, 0.15f),
                new Color(1f, 0.8f, 0.45f, 0.18f), new Color(0.35f, 0.85f, 1f, 0.2f), new Color(1f, 0.75f, 0.35f, 0.22f),
                new Color(0.4f, 0.6f, 1f, 0.2f), new Color(0.75f, 0.35f, 1f, 0.25f), new Color(0.5f, 0.6f, 1f, 0.18f),
                new Color(1f, 0.35f, 0.8f, 0.25f),
            };
            var glowColors = so.FindProperty("tierGlowColors");
            glowColors.arraySize = glows.Length;
            for (int i = 0; i < glows.Length; i++) glowColors.GetArrayElementAtIndex(i).colorValue = glows[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return floatOrigin;
        }

        static GameObject CreateTierOverlay(RectTransform room, string name, Rect[] screens, Rect[] cones, Vector3 led, Color ledColor, bool ledCycle)
        {
            var group = UIFactory.Rect(name, room);
            UIFactory.Stretch(group);

            // LED strip glow (y, xFrom, xTo)
            var glow = Img("LedGlow", group, LoadSprite(ArtGenerator.Fx + "/fx_glow.png"), led.y, led.x - 42, led.z - led.y, 84, ledColor);
            var ledFx = glow.gameObject.AddComponent<LedGlow>();
            var lso = new SerializedObject(ledFx);
            lso.FindProperty("color").colorValue = ledColor;
            lso.FindProperty("cycle").boolValue = ledCycle;
            lso.ApplyModifiedPropertiesWithoutUndo();

            for (int i = 0; i < screens.Length; i++)
                CreateDawScreen(group, "Screen" + i, screens[i], i == 0, -1, false);

            var coneSprite = LoadSprite(ArtGenerator.SpeakerConePath);
            var coneObjects = new Object[cones.Length];
            for (int i = 0; i < cones.Length; i++)
            {
                var c = cones[i];
                var img = Img("Cone" + i, group, coneSprite, c.x + c.width * 0.5f, c.y + c.height * 0.5f, c.width, c.height);
                img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                coneObjects[i] = img.rectTransform;
            }
            var pulse = group.gameObject.AddComponent<SpeakerPulse>();
            var pso = new SerializedObject(pulse);
            UIFactory.SetArray(pso.FindProperty("cones"), coneObjects);
            pso.ApplyModifiedPropertiesWithoutUndo();
            return group.gameObject;
        }

        /// <summary>Animated BEATLAB DAW on a monitor/laptop screen.</summary>
        static DawScreen CreateDawScreen(RectTransform parent, string name, Rect r, bool listenManual, int producerIndex, bool alwaysPlaying)
        {
            var screen = UIFactory.Rect(name, parent);
            screen.anchorMin = screen.anchorMax = Vector2.zero;
            screen.pivot = new Vector2(0.5f, 0.5f);
            screen.anchoredPosition = r.center;
            screen.sizeDelta = r.size;
            var daw = screen.gameObject.AddComponent<DawScreen>();
            UIFactory.SetRef(daw, "background", LoadSprite(ArtGenerator.DawBackgroundPath));
            var so = new SerializedObject(daw);
            so.FindProperty("listenManual").boolValue = listenManual;
            so.FindProperty("producerIndex").intValue = producerIndex;
            so.FindProperty("alwaysPlaying").boolValue = alwaysPlaying;
            so.ApplyModifiedPropertiesWithoutUndo();
            return daw;
        }

        static GameObject CreateStation(RectTransform room, int index, string characterId, float centerX, float phase)
        {
            var root = UIFactory.Rect("Station_" + characterId, room);
            UIFactory.Stretch(root);
            float x0 = centerX - 110, y0 = 14;
            Img("Desk", root, LoadSprite(ArtGenerator.StationPath(characterId)), x0, y0, 220, 200);
            var s = ArtGenerator.StationScreen;
            CreateDawScreen(root, "Laptop", new Rect(x0 + s.x, y0 + s.y, s.width, s.height), false, index, true);
            var rig = CreateRig(root, characterId, "Producer_" + characterId, new Vector2(centerX + 50, y0), 0.62f, true, phase);

            // Personality: beginner is excited, melody creative/focused, drum energetic.
            var so = new SerializedObject(rig);
            var (a, b) = characterId == "beginner" ? (CharacterRig.Face.Excited, CharacterRig.Face.Idle)
                : characterId == "melody" ? (CharacterRig.Face.Focus, CharacterRig.Face.Proud)
                : (CharacterRig.Face.Focus, CharacterRig.Face.Excited);
            so.FindProperty("workFaceA").enumValueIndex = (int)a;
            so.FindProperty("workFaceB").enumValueIndex = (int)b;
            so.FindProperty("typingSpeed").floatValue = 12f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root.gameObject;
        }

        /// <summary>
        /// Seated character (3/4 view, facing left): far arm, body (with chair), head (expression sprites), near arm.
        /// Root is 200×260, pivot at the floor.
        /// </summary>
        static CharacterRig CreateRig(RectTransform parent, string id, string name, Vector2 position, float scale, bool alwaysWorking, float phase)
        {
            var root = UIFactory.Rect(name, parent);
            root.anchorMin = root.anchorMax = Vector2.zero;
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(200, 260);
            root.anchoredPosition = position;
            root.localScale = new Vector3(scale, scale, 1f);

            Sprite S(string part) => LoadSprite(ArtGenerator.CharPart(id, part));

            Image Part(string partName, Sprite sprite, Vector2 size, Vector2 pivot, Vector2 pos, float rot, Color tint)
            {
                var img = UIFactory.Image(partName, root, tint);
                img.sprite = sprite;
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = pivot;
                rt.sizeDelta = size;
                rt.anchoredPosition = pos;
                rt.localEulerAngles = new Vector3(0, 0, rot);
                return img;
            }

            // Arms reach left to the keyboard (-97°); the far arm is darker for depth.
            var armPivot = new Vector2(0.5f, 126f / 140f);
            var armFar = Part("ArmFar", S("arm"), new Vector2(56, 140), armPivot, new Vector2(18, 144), -94f, new Color(0.72f, 0.72f, 0.78f));
            var body = Part("Body", S("body"), new Vector2(200, 190), new Vector2(0.5f, 0f), Vector2.zero, 0f, Color.white);
            // Head pivot sits at the neck (88, 24 in the 160 px head art).
            var head = Part("Head", S("head_idle"), new Vector2(120, 120), new Vector2(88f / 160f, 24f / 160f), new Vector2(-1, 166), 0f, Color.white);
            var armNear = Part("ArmNear", S("arm"), new Vector2(56, 140), armPivot, new Vector2(-8, 140), -97f, Color.white);

            var rig = root.gameObject.AddComponent<CharacterRig>();
            UIFactory.SetRef(rig, "body", body.rectTransform);
            UIFactory.SetRef(rig, "head", head.rectTransform);
            UIFactory.SetRef(rig, "armL", armFar.rectTransform);
            UIFactory.SetRef(rig, "armR", armNear.rectTransform);
            UIFactory.SetRef(rig, "faceImage", head);
            var so = new SerializedObject(rig);
            var faces = new Object[ArtGenerator.Expressions.Length];
            for (int i = 0; i < faces.Length; i++) faces[i] = S("head_" + ArtGenerator.Expressions[i]);
            UIFactory.SetArray(so.FindProperty("faces"), faces);
            so.FindProperty("alwaysWorking").boolValue = alwaysWorking;
            so.FindProperty("phaseOffset").floatValue = phase;
            so.FindProperty("cheerAngleL").floatValue = 155f;
            so.FindProperty("cheerAngleR").floatValue = -150f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return rig;
        }

        /// <summary>
        /// Layered, customizable player rig (same proportions as CreateRig): far arm, body layers, head layers, near arm.
        /// A PlayerAvatar applies the saved appearance; a CharacterRig animates it.
        /// </summary>
        public static CharacterRig CreatePlayerRig(RectTransform parent, string name, Vector2 position, float scale, bool followGame)
        {
            var lib = AssetDatabase.LoadAssetAtPath<AvatarLibrary>(ArtGenerator.LibraryPath);
            var root = UIFactory.Rect(name, parent);
            root.anchorMin = root.anchorMax = Vector2.zero;
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(200, 260);
            root.anchoredPosition = position;
            root.localScale = new Vector3(scale, scale, 1f);

            Image Layer(string n, Transform p, Color tint)
            {
                var img = UIFactory.Image(n, p, tint);
                img.raycastTarget = false;
                UIFactory.Stretch(img.rectTransform);
                return img;
            }

            RectTransform Part(string n, Vector2 size, Vector2 pivot, Vector2 pos, float rot)
            {
                var rt = UIFactory.Rect(n, root);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = pivot;
                rt.sizeDelta = size;
                rt.anchoredPosition = pos;
                rt.localEulerAngles = new Vector3(0, 0, rot);
                return rt;
            }

            var armPivot = new Vector2(0.5f, 126f / 140f);
            var farTint = new Color(0.72f, 0.72f, 0.78f);
            PlayerAvatar.ArmLayers Arm(RectTransform arm, Color tint) => new PlayerAvatar.ArmLayers
            {
                sleeve = Layer("Sleeve", arm, tint), hand = Layer("Hand", arm, tint), tattooArm = Layer("TattooArm", arm, tint),
                tattooHand = Layer("TattooHand", arm, tint), watch = Layer("Watch", arm, tint), rings = Layer("Rings", arm, tint),
            };

            var armFar = Part("ArmFar", new Vector2(56, 140), armPivot, new Vector2(18, 144), -94f);
            var farLayers = Arm(armFar, farTint);
            var body = Part("Body", new Vector2(200, 190), new Vector2(0.5f, 0f), Vector2.zero, 0f);
            var bodyBase = Layer("Base", body, Color.white);
            bodyBase.sprite = lib != null ? lib.bodyBase : null;
            var pantsFar = Layer("PantsFar", body, Color.white);
            var shoeFar = Layer("ShoeFar", body, Color.white);
            var pantsNear = Layer("PantsNear", body, Color.white);
            var shoeNear = Layer("ShoeNear", body, Color.white);
            var hem = Layer("Hem", body, Color.white);
            var top = Layer("Top", body, Color.white);
            var chain = Layer("Chain", body, Color.white);
            var tatNeck = Layer("TattooNeck", body, Color.white);

            var head = Part("Head", new Vector2(120, 120), new Vector2(88f / 160f, 24f / 160f), new Vector2(-1, 166), 0f);
            var hairBack = Layer("HairBack", head, Color.white);
            var face = Layer("Face", head, Color.white);
            if (lib != null && lib.faces.Length > 0) face.sprite = lib.faces[0];
            var hairFront = Layer("HairFront", head, Color.white);
            var tatFace = Layer("TattooFace", head, Color.white);
            var headwear = Layer("Headwear", head, Color.white);
            var phones = Layer("Headphones", head, Color.white);
            var glasses = Layer("Glasses", head, Color.white);
            var earrings = Layer("Earrings", head, Color.white);

            var armNear = Part("ArmNear", new Vector2(56, 140), armPivot, new Vector2(-8, 140), -97f);
            var nearLayers = Arm(armNear, Color.white);

            var avatar = root.gameObject.AddComponent<PlayerAvatar>();
            var aso = new SerializedObject(avatar);
            aso.FindProperty("library").objectReferenceValue = lib;
            aso.FindProperty("followGame").boolValue = followGame;
            void R(string field, Object v) => aso.FindProperty(field).objectReferenceValue = v;
            R("hairBack", hairBack); R("hairFront", hairFront); R("headwear", headwear); R("headphones", phones);
            R("glasses", glasses); R("earrings", earrings); R("tattooFace", tatFace);
            R("pantsFar", pantsFar); R("shoeFar", shoeFar); R("pantsNear", pantsNear); R("shoeNear", shoeNear);
            R("pantsHem", hem); R("top", top); R("chain", chain); R("tattooNeck", tatNeck);
            void A(string arm, PlayerAvatar.ArmLayers l)
            {
                R(arm + ".sleeve", l.sleeve); R(arm + ".hand", l.hand); R(arm + ".tattooArm", l.tattooArm);
                R(arm + ".tattooHand", l.tattooHand); R(arm + ".watch", l.watch); R(arm + ".rings", l.rings);
            }
            A("armFar", farLayers);
            A("armNear", nearLayers);
            aso.ApplyModifiedPropertiesWithoutUndo();
            // Show the default look in the editor too.
            avatar.Apply(new PlayerAppearance());

            var rig = root.gameObject.AddComponent<CharacterRig>();
            UIFactory.SetRef(rig, "body", body);
            UIFactory.SetRef(rig, "head", head);
            UIFactory.SetRef(rig, "armL", armFar);
            UIFactory.SetRef(rig, "armR", armNear);
            UIFactory.SetRef(rig, "faceImage", face);
            var so = new SerializedObject(rig);
            if (lib != null) UIFactory.SetArray(so.FindProperty("faces"), lib.faces);
            so.FindProperty("alwaysWorking").boolValue = false;
            so.FindProperty("cheerAngleL").floatValue = 155f;
            so.FindProperty("cheerAngleR").floatValue = -150f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return rig;
        }

        /// <summary>Poster slots per studio tier (room draw coordinates: x, y, size). Covers hang here when albums are released.</summary>
        static readonly Vector3[][] PosterSlots =
        {
            new[] { new Vector3(236, 402, 58), new Vector3(306, 408, 52), new Vector3(756, 300, 56), new Vector3(150, 320, 54) },
            new[] { new Vector3(240, 392, 60), new Vector3(316, 398, 54), new Vector3(880, 436, 56), new Vector3(474, 300, 54) },
            new[] { new Vector3(240, 400, 60), new Vector3(320, 404, 54), new Vector3(840, 270, 52) },
            new[] { new Vector3(250, 392, 62), new Vector3(330, 396, 56), new Vector3(560, 300, 50) },
            new[] { new Vector3(556, 296, 48), new Vector3(150, 470, 44), new Vector3(612, 470, 44) },
            new[] { new Vector3(250, 400, 60), new Vector3(330, 404, 54), new Vector3(470, 420, 50) },
            new[] { new Vector3(110, 300, 50), new Vector3(380, 420, 50), new Vector3(30, 440, 44) },
            new[] { new Vector3(40, 400, 56), new Vector3(30, 470, 44), new Vector3(700, 520, 30) },
            new[] { new Vector3(236, 392, 52), new Vector3(300, 392, 52), new Vector3(364, 392, 52) },
            new[] { new Vector3(160, 480, 40), new Vector3(220, 480, 40), new Vector3(280, 480, 40) },
        };

        static void CreateAlbumWall(RectTransform room, float lift)
        {
            var wallRoot = UIFactory.Rect("AlbumWall", room);
            UIFactory.Stretch(wallRoot);
            var wall = wallRoot.gameObject.AddComponent<AlbumWall>();
            var so = new SerializedObject(wall);
            var tiers = so.FindProperty("tiers");
            tiers.arraySize = PosterSlots.Length;
            var rnd = new System.Random(5);
            for (int t = 0; t < PosterSlots.Length; t++)
            {
                var group = UIFactory.Rect("Tier" + t, wallRoot);
                UIFactory.Stretch(group);
                var covers = new Object[PosterSlots[t].Length];
                for (int i = 0; i < PosterSlots[t].Length; i++)
                {
                    var s = PosterSlots[t][i];
                    // frame holder (scales when a new album is hung)
                    var holder = UIFactory.Rect("Poster" + i, group);
                    holder.anchorMin = holder.anchorMax = Vector2.zero;
                    holder.pivot = new Vector2(0.5f, 0.5f);
                    holder.sizeDelta = new Vector2(s.z, s.z);
                    holder.anchoredPosition = new Vector2(ArtGenerator.MirrorX(t, s.x, s.z) + s.z / 2, s.y + s.z / 2 + lift);
                    if (t == 0) holder.localEulerAngles = new Vector3(0, 0, (float)(rnd.NextDouble() * 12 - 6));
                    float pad = t == 0 ? 0 : 6;
                    if (t > 0)
                    {
                        var frame = UIFactory.Image("Frame", holder, t == 1 ? new Color(0.05f, 0.05f, 0.07f) : Gold);
                        frame.raycastTarget = false;
                        UIFactory.Stretch(frame.rectTransform, -pad, -pad, -pad, -pad);
                    }
                    var cover = UIFactory.Image("Cover", holder, Color.white);
                    cover.raycastTarget = false;
                    UIFactory.Stretch(cover.rectTransform);
                    if (t == 0)
                    {
                        foreach (var side in new[] { -1f, 1f })
                        {
                            var tape = UIFactory.Image("Tape", holder, new Color(0.91f, 0.89f, 0.78f, 0.8f));
                            tape.raycastTarget = false;
                            var trt = tape.rectTransform;
                            trt.anchorMin = trt.anchorMax = new Vector2(side < 0 ? 0 : 1, 1);
                            trt.sizeDelta = new Vector2(20, 8);
                            trt.localEulerAngles = new Vector3(0, 0, 35 * side);
                        }
                    }
                    holder.gameObject.SetActive(false);
                    covers[i] = cover;
                }
                var tier = tiers.GetArrayElementAtIndex(t);
                tier.FindPropertyRelative("root").objectReferenceValue = group.gameObject;
                var coverProp = tier.FindPropertyRelative("covers");
                UIFactory.SetArray(coverProp, covers);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Promo-poster spots for signed artists per room (draw coordinates before mirroring: x, y = bottom-left).
        /// Basement has none (nobody signed yet); later rooms add more and the label rooms add painted plaque walls.
        /// </summary>
        static readonly Vector2[][] ArtistPosterSlots =
        {
            new Vector2[0],
            new[] { new Vector2(590, 300) },
            new[] { new Vector2(870, 250), new Vector2(930, 250) },
            new[] { new Vector2(410, 305), new Vector2(480, 305) },
            new[] { new Vector2(100, 336) },
            new[] { new Vector2(450, 296), new Vector2(700, 262), new Vector2(940, 300) },
            new[] { new Vector2(490, 390), new Vector2(560, 390), new Vector2(630, 390), new Vector2(870, 400) },
            new[] { new Vector2(735, 400), new Vector2(900, 400) },
            new[] { new Vector2(20, 384), new Vector2(84, 384), new Vector2(148, 384) },
            new[] { new Vector2(30, 392), new Vector2(95, 392), new Vector2(160, 392), new Vector2(225, 392) },
        };

        static void CreateArtistPosterWall(RectTransform room, float lift)
        {
            const float W = 50, H = 70;
            var wallRoot = UIFactory.Rect("ArtistPosters", room);
            UIFactory.Stretch(wallRoot);
            var wall = wallRoot.gameObject.AddComponent<ArtistPosterWall>();
            var so = new SerializedObject(wall);
            var tiers = so.FindProperty("tiers");
            tiers.arraySize = ArtistPosterSlots.Length;
            var rnd = new System.Random(9);
            for (int t = 0; t < ArtistPosterSlots.Length; t++)
            {
                var group = UIFactory.Rect("Tier" + t, wallRoot);
                UIFactory.Stretch(group);
                var tier = tiers.GetArrayElementAtIndex(t);
                tier.FindPropertyRelative("root").objectReferenceValue = group.gameObject;
                var posters = tier.FindPropertyRelative("posters");
                posters.arraySize = ArtistPosterSlots[t].Length;
                // frames get classier with the room: taped paper → black → gold → platinum
                Color frameColor = t <= 2 ? new Color(0.92f, 0.9f, 0.84f) : t <= 4 ? new Color(0.06f, 0.06f, 0.08f) : t <= 7 ? Gold : new Color(0.85f, 0.88f, 0.94f);
                for (int i = 0; i < ArtistPosterSlots[t].Length; i++)
                {
                    var s = ArtistPosterSlots[t][i];
                    var holder = UIFactory.Rect("ArtistPoster" + i, group);
                    holder.anchorMin = holder.anchorMax = Vector2.zero;
                    holder.pivot = new Vector2(0.5f, 0.5f);
                    holder.sizeDelta = new Vector2(W, H);
                    holder.anchoredPosition = new Vector2(ArtGenerator.MirrorX(t, s.x, W) + W / 2, s.y + H / 2 + lift);
                    if (t <= 2) holder.localEulerAngles = new Vector3(0, 0, (float)(rnd.NextDouble() * 8 - 4));
                    var frame = UIFactory.Image("Frame", holder, frameColor);
                    frame.raycastTarget = false;
                    UIFactory.Stretch(frame.rectTransform);
                    var portrait = UIFactory.Image("Portrait", holder, Color.white);
                    portrait.raycastTarget = false;
                    var prt = portrait.rectTransform;
                    prt.anchorMin = new Vector2(0, 1);
                    prt.anchorMax = new Vector2(1, 1);
                    prt.pivot = new Vector2(0.5f, 1);
                    prt.offsetMin = new Vector2(3, -3 - (W - 6));
                    prt.offsetMax = new Vector2(-3, -3);
                    var strip = UIFactory.Image("GenreStrip", holder, Color.magenta);
                    strip.raycastTarget = false;
                    var srt = strip.rectTransform;
                    srt.anchorMin = new Vector2(0, 0);
                    srt.anchorMax = new Vector2(1, 0);
                    srt.pivot = new Vector2(0.5f, 0);
                    srt.offsetMin = new Vector2(3, 3);
                    srt.offsetMax = new Vector2(-3, H - W - 3);
                    var name = UIFactory.Text("Name", srt, "ARTIST", 9, Color.white, TextAlignmentOptions.Center,
                        Vector2.zero, Vector2.one, new Vector2(1, 0), new Vector2(-1, 0));
                    name.font = UIStyle.Display;
                    name.enableAutoSizing = true;
                    name.fontSizeMin = 5;
                    name.fontSizeMax = 10;
                    name.textWrappingMode = TextWrappingModes.NoWrap;
                    name.raycastTarget = false;
                    if (t <= 2)
                    {
                        var tape = UIFactory.Image("Tape", holder, new Color(0.91f, 0.89f, 0.78f, 0.8f));
                        tape.raycastTarget = false;
                        tape.rectTransform.anchorMin = tape.rectTransform.anchorMax = new Vector2(0.5f, 1);
                        tape.rectTransform.sizeDelta = new Vector2(22, 8);
                    }
                    holder.gameObject.SetActive(false);
                    var el = posters.GetArrayElementAtIndex(i);
                    el.FindPropertyRelative("root").objectReferenceValue = holder.gameObject;
                    el.FindPropertyRelative("portrait").objectReferenceValue = portrait;
                    el.FindPropertyRelative("strip").objectReferenceValue = strip;
                    el.FindPropertyRelative("nameText").objectReferenceValue = name;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Neon / branded signs that make the bigger rooms recognizable (they pop in during the move-in show).</summary>
        static void CreateRoomSigns(Transform overlay, int tier, float lift)
        {
            void Sign(string text, float cx, float cy, float w, float h, Color color, bool neon, float spacing)
            {
                var rt = UIFactory.Rect("Sign_" + text.Replace(" ", ""), overlay);
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(w, h);
                rt.anchoredPosition = new Vector2(ArtGenerator.MirrorX(tier, cx), cy + lift);
                if (neon)
                {
                    var glow = Img("Glow", rt, LoadSprite(ArtGenerator.Fx + "/fx_glow.png"), -w * 0.15f, -h * 0.6f, w * 1.3f, h * 2.2f,
                        new Color(color.r, color.g, color.b, 0.35f));
                    glow.gameObject.AddComponent<PulseGlow>();
                }
                var label = UIFactory.Text("Text", rt, text, h * 0.8f, color, TextAlignmentOptions.Center,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                label.font = UIStyle.Display;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.enableAutoSizing = true;
                label.fontSizeMin = 8;
                label.fontSizeMax = h * 0.9f;
                label.characterSpacing = spacing;
                label.raycastTarget = false;
            }

            switch (tier)
            {
                case 4: Sign("BEAT FACTORY", 805, 455, 300, 40, new Color(1f, 0.24f, 0.78f), true, 8); break;                  // neon on the sunset window
                case 7: Sign("BEAT FACTORY", 400, 462, 520, 64, new Color(0.85f, 0.95f, 1f), true, 14); break;              // giant logo on the LED wall
                case 8: Sign("BF RECORDS", 220, 482, 380, 38, new Color(0.16f, 0.16f, 0.2f), false, 22); break;             // corporate HQ logo
                case 9: Sign("BEAT FACTORY", 620, 335, 280, 44, Color.white, true, 6); break;                                // on the arena stage screen
            }
        }

        static CanvasGroup CreateUnlockBanner(RectTransform card, out TMP_Text title, out TMP_Text name, out TMP_Text sub)
        {
            var panel = UIFactory.RoundedImage("UnlockBanner", card, new Color(0.04f, 0.03f, 0.08f, 0.88f));
            panel.raycastTarget = false;
            var rt = panel.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.55f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(780, 230);
            var line = UIFactory.Image("Accent", rt, Gold);
            line.raycastTarget = false;
            line.rectTransform.anchorMin = new Vector2(0.1f, 1);
            line.rectTransform.anchorMax = new Vector2(0.9f, 1);
            line.rectTransform.sizeDelta = new Vector2(0, 5);
            line.rectTransform.anchoredPosition = new Vector2(0, -3);
            title = UIFactory.Text("Title", rt, "NEW STUDIO UNLOCKED", 30, Gold, TextAlignmentOptions.Center,
                new Vector2(0, 0.7f), new Vector2(1, 1), new Vector2(20, 0), new Vector2(-20, -10));
            title.font = UIStyle.Heading;
            title.characterSpacing = 10;
            name = UIFactory.Text("Name", rt, "STUDIO", 70, Color.white, TextAlignmentOptions.Center,
                new Vector2(0, 0.28f), new Vector2(1, 0.74f), new Vector2(20, 0), new Vector2(-20, 0));
            name.font = UIStyle.Display;
            name.enableAutoSizing = true;
            name.fontSizeMin = 30;
            name.fontSizeMax = 72;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            sub = UIFactory.Text("Sub", rt, "", 28, Theme.AccentCyan, TextAlignmentOptions.Center,
                new Vector2(0, 0), new Vector2(1, 0.3f), new Vector2(20, 10), new Vector2(-20, 0));
            sub.font = UIStyle.Body;
            foreach (var t in new[] { title, name, sub }) t.raycastTarget = false;
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            panel.gameObject.SetActive(false);
            return group;
        }

        /// <summary>Gives artists their generated portrait (only if no avatar was set by hand).</summary>
        static void AssignArtistPortraits(ArtistData[] artists)
        {
            foreach (var a in artists)
            {
                if (a.avatar == null) a.avatar = LoadSprite(ArtGenerator.ArtistPortraitPath(a.id));
                AssignVisualIdentity(a);
                EditorUtility.SetDirty(a);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Fills the artist's visual identity from the same character definition as the portrait (single source of truth):
        /// description, stage palette, the stage performer parts and the fans' sign texts.
        /// </summary>
        static void AssignVisualIdentity(ArtistData a)
        {
            var spec = System.Array.Find(ArtGenerator.Roster, s => s.id == a.id);
            if (spec == null) return;
            var look = ArtGenerator.StageLook(spec);
            var v = a.visual ?? (a.visual = new ArtistVisualIdentity());
            v.skin = look.skin;
            v.hair = look.hair;
            v.hairTip = look.hairTip;
            v.hairStyle = look.hairStyle.ToString();
            v.facialHair = look.beard ? "beard" : look.stubble ? "stubble" : "none";
            var acc = new System.Collections.Generic.List<string>();
            if (look.chainCuban) acc.Add("cuban chain"); if (look.chainThin) acc.Add("thin chain");
            if (look.sunglasses) acc.Add("shades"); if (look.glasses) acc.Add("glasses");
            if (look.earHoop) acc.Add("hoops"); if (look.earStud) acc.Add("studs"); if (look.septum) acc.Add("septum");
            if (look.cap.a > 0) acc.Add("cap"); if (look.beanie.a > 0) acc.Add("beanie");
            v.accessories = string.Join(", ", acc);
            var tat = new System.Collections.Generic.List<string>();
            if (look.faceDots) tat.Add("face dots"); if (look.faceStar) tat.Add("face star"); if (look.neckTattoo) tat.Add("neck");
            if (look.armTattoo) tat.Add("sleeve"); if (look.handTattoo) tat.Add("hand");
            v.tattoos = tat.Count > 0 ? string.Join(", ", tat) : "none";
            v.outfit = look.top + " · " + look.pantsStyle + " · " + look.sneaker;
            v.clothing = look.cloth;
            v.accent = look.accent;
            v.palette = new[] { ArtistGenres.Color(a.genre), look.accent, look.cloth };
            var heads = new Sprite[ArtGenerator.StageExpressions.Length];
            for (int i = 0; i < heads.Length; i++) heads[i] = LoadSprite(ArtGenerator.PerformerPath(a.id, "head_" + ArtGenerator.StageExpressions[i]));
            v.stageHeads = heads;
            v.stageTorso = LoadSprite(ArtGenerator.PerformerPath(a.id, "torso"));
            v.stageArm = LoadSprite(ArtGenerator.PerformerPath(a.id, "arm"));
            v.stageLeg = LoadSprite(ArtGenerator.PerformerPath(a.id, "leg"));
            // detailed layered art (DRIP STUDIO), if this artist has been imported
            v.rig = AssetDatabase.LoadAssetAtPath<ArtistRig>(ArtistRigImporter.RigAssetPath(a.id));
            string n = a.displayName.ToUpperInvariant();
            string phrase = Tours.TitleName(a, TourKind.Mini, null);
            v.signs = new[] { n, "WE LOVE YOU", n + " 4 LIFE", phrase, "#1 FAN" };
        }

        /// <summary>Gives the first producers their portrait (only if no icon was set by hand).</summary>
        static void AssignProducerPortraits(ProducerData[] producers)
        {
            string[] ids = { "beginner", "melody", "drum", "sample", "sound", "hitmaker", "superstar" };
            for (int i = 0; i < producers.Length && i < ids.Length; i++)
            {
                if (producers[i].icon != null) continue;
                producers[i].icon = LoadSprite(ArtGenerator.PortraitPath(ids[i]));
                EditorUtility.SetDirty(producers[i]);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
