using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeatFactory.EditorTools
{
    /// <summary>
    /// Builds the Game scene automatically: camera, GameManager, Canvas and all UI.
    /// Menu: Tools > Beat Factory > Build Game Scene.
    /// Rebuilding overwrites Assets/_BeatFactory/Scenes/Game.unity.
    /// </summary>
    public static partial class BeatFactorySceneBuilder
    {
        public const string Root = "Assets/_BeatFactory";
        public const string ScenePath = Root + "/Scenes/Game.unity";
        public const string ConfigFolder = Root + "/Data/Config";
        public const string EconomyConfigPath = ConfigFolder + "/EconomyConfig.asset";
        public const string BeatQualityConfigPath = ConfigFolder + "/BeatQualityConfig.asset";
        public const string StreamConfigPath = ConfigFolder + "/StreamConfig.asset";
        public const string BoostConfigPath = ConfigFolder + "/BoostConfig.asset";
        public const string AudioConfigPath = ConfigFolder + "/AudioConfig.asset";
        public const string RoundedSpritePath = Root + "/Art/UI/RoundedRect.png";
        public const string CircleSpritePath = Root + "/Art/UI/Circle.png";
        public const string OutlineSpritePath = Root + "/Art/UI/RoundedOutline.png";

        static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);
        static AlbumCardView albumCardPrefab;
        static ArtistCardView artistCardPrefab;
        static StyleItemCard itemCardPrefab;

        [MenuItem("Tools/Beat Factory/Build Game Scene")]
        static void BuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Build Game Scene",
                    "This will (re)create " + ScenePath + ". Any manual changes in that scene will be lost. Continue?",
                    "Build", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildScene();
        }

        [MenuItem("Tools/Beat Factory/Apply Mobile Project Settings")]
        public static void ApplyProjectSettings()
        {
            PlayerSettings.productName = "Beat Factory";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // The game uses no physics: don't simulate it every frame (saves CPU/battery on phones).
            Physics.simulationMode = SimulationMode.Script;
            Physics2D.simulationMode = SimulationMode2D.Script;
            Physics.autoSyncTransforms = false;
            Physics2D.autoSyncTransforms = false;
            Debug.Log("[Beat Factory] Mobile project settings applied (portrait, product name).");
        }

        public static void BuildScene()
        {
            UIFactory.ResetEnglishCache();
            EnsureFolders();
            EnsureAsset<EconomyConfig>(EconomyConfigPath);
            EnsureAsset<BeatQualityConfig>(BeatQualityConfigPath);
            EnsureAsset<StreamConfig>(StreamConfigPath);
            EnsureAsset<BoostConfig>(BoostConfigPath);
            DefaultGameData.EnsureAudio(AudioConfigPath);
            DefaultGameData.MigrateQualityStreamShares(BeatQualityConfigPath);
            SpriteGenerator.EnsureRoundedRect(RoundedSpritePath, 128, 48);
            SpriteGenerator.EnsureRoundedRect(CircleSpritePath, 128, 64);
            SpriteGenerator.EnsureRoundedOutline(OutlineSpritePath, 128, 48, 7);
            DefaultGameData.EnsureProducers();
            DefaultGameData.EnsureGenres();
            DefaultGameData.EnsureStudios();
            DefaultGameData.EnsureArtists();
            Art.ArtGenerator.GenerateAll(overwrite: false);
            DefaultGameData.EnsureAlbums();
            AvatarBuilder.EnsureLibrary(overwrite: false);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load assets AFTER NewScene: opening a scene unloads previously loaded assets.
            var economyConfig = AssetDatabase.LoadAssetAtPath<EconomyConfig>(EconomyConfigPath);
            var qualityConfig = AssetDatabase.LoadAssetAtPath<BeatQualityConfig>(BeatQualityConfigPath);
            var streamConfig = AssetDatabase.LoadAssetAtPath<StreamConfig>(StreamConfigPath);
            var boostConfig = AssetDatabase.LoadAssetAtPath<BoostConfig>(BoostConfigPath);
            var audioConfig = AssetDatabase.LoadAssetAtPath<AudioConfig>(AudioConfigPath);
            var producers = DefaultGameData.LoadProducers();
            var genres = DefaultGameData.LoadGenres();
            var studios = DefaultGameData.LoadStudios();
            var artists = DefaultGameData.LoadArtists();
            var albums = DefaultGameData.LoadAlbums();
            UIFactory.RoundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
            UIFactory.CircleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
            UIFactory.OutlineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OutlineSpritePath);
            UIStyle.Load();

            AssignProducerPortraits(producers);
            AssignArtistPortraits(artists);
            var producerRowPrefab = UIPrefabBuilder.BuildProducerRow();
            var shopRowPrefab = UIPrefabBuilder.BuildShopRow();
            albumCardPrefab = UIPrefabBuilder.BuildAlbumCard();
            artistCardPrefab = UIPrefabBuilder.BuildArtistCard();
            itemCardPrefab = UIPrefabBuilder.BuildStyleItemCard();

            CreateCamera();
            var gameManager = CreateGameManager(economyConfig, qualityConfig, producers, genres, studios, artists);
            var gmSo = new SerializedObject(gameManager);
            UIFactory.SetArray(gmSo.FindProperty("albums"), albums);
            gmSo.ApplyModifiedPropertiesWithoutUndo();
            UIFactory.SetRef(gameManager, "streamConfig", streamConfig);
            UIFactory.SetRef(gameManager, "boostConfig", boostConfig);

            var audio = new GameObject("AudioManager");
            var audioManager = audio.AddComponent<AudioManager>();
            UIFactory.SetRef(audioManager, "config", audioConfig);
            audio.AddComponent<AudioEvents>();

            new GameObject("NotificationManager").AddComponent<NotificationManager>();
            CreateEventSystem();
            CreateUI(producerRowPrefab, shopRowPrefab);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            ApplyProjectSettings();
            Debug.Log("[Beat Factory] Game scene built: " + ScenePath);
        }

        // ------------------------------------------------------------------ scene objects

        static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9.6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Theme.Background;
            go.transform.position = new Vector3(0, 0, -10);
            go.AddComponent<AudioListener>(); // without it no sound is heard
        }

        static GameManager CreateGameManager(EconomyConfig economyConfig, BeatQualityConfig qualityConfig,
            ProducerData[] producers, GenreData[] genres, StudioData[] studios, ArtistData[] artists)
        {
            var go = new GameObject("GameManager");
            var gm = go.AddComponent<GameManager>();
            var so = new SerializedObject(gm);
            so.FindProperty("economyConfig").objectReferenceValue = economyConfig;
            so.FindProperty("beatQualityConfig").objectReferenceValue = qualityConfig;
            UIFactory.SetArray(so.FindProperty("producers"), producers);
            UIFactory.SetArray(so.FindProperty("genres"), genres);
            UIFactory.SetArray(so.FindProperty("studios"), studios);
            UIFactory.SetArray(so.FindProperty("artists"), artists);
            so.ApplyModifiedPropertiesWithoutUndo();
            return gm;
        }

        static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        static void CreateUI(ProducerRow producerRowPrefab, ShopRowView shopRowPrefab)
        {
            var canvasGo = new GameObject("MainCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var background = UIFactory.Image("Background", canvasGo.transform, Theme.Background);
            UIFactory.Stretch(background.rectTransform);
            background.raycastTarget = false;

            var safeArea = UIFactory.Rect("SafeArea", canvasGo.transform);
            UIFactory.Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeArea>();

            var accentLine = CreateTopBar(safeArea, out var streamCounter);
            var boostBar = CreateBoostBar(safeArea);

            // Main content area between the top bar and the bottom navigation. Holds one screen per tab.
            var content = UIFactory.Rect("Content", safeArea);
            UIFactory.Stretch(content, top: Theme.TopBarHeight + Theme.BoostBarHeight, bottom: Theme.BottomNavHeight);

            // Effects layer on top of the screens (floating texts, celebrations). Not clickable.
            // Created now so screens can reference it; moved to the top of the draw order below.
            var fxLayer = UIFactory.Rect("FXLayer", safeArea);
            UIFactory.Stretch(fxLayer);
            var floating = fxLayer.gameObject.AddComponent<FloatingTextSpawner>();
            UIFactory.SetRef(floating, "layer", fxLayer);

            // HOME: studio scene → MAKE BEAT → producer cards
            var home = CreateScreen("HomeScreen", content);
            var beatOrigin = CreateStudioScene(home, background, accentLine, out _);
            float makeBeatTop = Theme.SceneTop + Theme.SceneHeight + 20;
            CreateMakeBeatButton(home, floating, makeBeatTop, beatOrigin);
            CreateProducerList(home, producerRowPrefab, floating,
                top: makeBeatTop + Theme.MakeBeatHeight + 20, bottom: 16, buyModeSelector: null);
            var tutorial = CreateTutorial(home);

            var producersScreen = CreateProducersScreen(content, producerRowPrefab, floating);
            var musicScreen = CreateMusicScreen(content, shopRowPrefab, floating);
            var artistsScreen = CreateArtistsScreen(content, shopRowPrefab);
            var prestigeScreen = CreatePrestigeScreen(content, out var prestigeUi);
            var settingsScreen = CreateSettingsScreen(content, out var settingsUi);

            var navigator = CreateBottomNav(safeArea, new[]
            {
                new NavTabDef("HOME", "icon_studio", home.gameObject, NavBadgeSource.None),
                new NavTabDef("PRODUCERS", "icon_producer", producersScreen.gameObject, NavBadgeSource.Producers),
                new NavTabDef("MUSIC", "icon_music", musicScreen.gameObject, NavBadgeSource.Music),
                new NavTabDef("CAREER", "icon_artist", artistsScreen.gameObject, NavBadgeSource.Artists),
                new NavTabDef("PRESTIGE", "icon_prestige", prestigeScreen.gameObject, NavBadgeSource.Prestige),
                new NavTabDef("SETTINGS", "icon_settings", settingsScreen.gameObject, NavBadgeSource.None),
            });

            // Tutorial step "unlock genres" points at the MUSIC tab.
            UIFactory.SetRef(tutorial, "genreTarget", navigator.transform.Find("Tabs/Tab_MUSIC"));
            UIFactory.SetRef(streamCounter, "navigator", navigator);
            UIFactory.SetRef(prestigeUi, "navigator", navigator);
            UIFactory.SetRef(settingsUi, "navigator", navigator);
            UIFactory.SetRef(settingsUi, "tutorial", tutorial);

            fxLayer.SetAsLastSibling();
            var popup = CreateCelebrationPopup(fxLayer);

            var announcements = fxLayer.gameObject.AddComponent<GameAnnouncements>();
            UIFactory.SetRef(announcements, "popup", popup);

            // Modal popups go on top of everything and dim the whole screen (outside the safe area too).
            var popupLayer = UIFactory.Rect("PopupLayer", canvasGo.transform);
            UIFactory.Stretch(popupLayer);
            CreateWelcomeBackPopup(popupLayer);
            var confirm = CreateConfirmDialog(popupLayer);
            UIFactory.SetRef(prestigeUi, "confirmDialog", confirm);
            UIFactory.SetRef(settingsUi, "confirmDialog", confirm);
            CreateArtistContractPopup(popupLayer);
            CreateAlbumPresentation(popupLayer);
            CreateStudioUpgradePopup(popupLayer);
            CreateStyleScreen(popupLayer, home.Find("StudioScene/StyleButton")?.GetComponent<Button>());
            CreateConcertUI(popupLayer);
            var boostsPopup = CreateBoostsPopup(popupLayer, shopRowPrefab);
            UIFactory.SetRef(boostBar, "popup", boostsPopup);

            // Ads: manager + test-ad overlay (always the top-most layer)
            var fakeAd = CreateFakeAdOverlay(popupLayer);
            var adsGo = new GameObject("AdsManager");
            var ads = adsGo.AddComponent<AdsManager>();
            UIFactory.SetRef(ads, "fakeOverlay", fakeAd);

            // Android Back button: closes the top popup / page, then goes HOME, then to the background
            var back = navigator.gameObject.AddComponent<BackButtonRouter>();
            UIFactory.SetRef(back, "navigator", navigator);
            UIFactory.SetRef(back, "fakeAd", fakeAd);

            // Design system pass: consistent surfaces and typography everywhere.
            UIStyle.ApplySurfaces(canvasGo, UIFactory.RoundedSprite);
            UIStyle.ApplyTypography(canvasGo);
        }

        static FakeAdOverlay CreateFakeAdOverlay(RectTransform layer)
        {
            var holder = UIFactory.Rect("FakeAd", layer);
            UIFactory.Stretch(holder);

            // Fully opaque: in Linear color space even 3% transparency shows the game through noticeably.
            var overlay = UIFactory.Image("Overlay", holder, new Color(0.02f, 0.02f, 0.04f, 1f));
            UIFactory.Stretch(overlay.rectTransform);
            overlay.raycastTarget = true; // blocks the game, like a real ad
            var ort = overlay.rectTransform;

            var title = UIFactory.Text("Title", ort, "TEST REWARDED AD", 64, Theme.AccentFame, TextAlignmentOptions.Center,
                new Vector2(0, 0.62f), new Vector2(1, 0.75f), new Vector2(40, 0), new Vector2(-40, 0));
            title.fontStyle = FontStyles.Bold;
            var placement = UIFactory.Text("Placement", ort, "", 32, Theme.TextDim, TextAlignmentOptions.Center,
                new Vector2(0, 0.52f), new Vector2(1, 0.62f), new Vector2(60, 0), new Vector2(-60, 0));
            placement.textWrappingMode = TextWrappingModes.Normal;
            var countdown = UIFactory.Text("Countdown", ort, "3", 160, Theme.TextMain, TextAlignmentOptions.Center,
                new Vector2(0, 0.36f), new Vector2(1, 0.52f), new Vector2(40, 0), new Vector2(-40, 0));
            countdown.fontStyle = FontStyles.Bold;

            var close = UIFactory.Button("CloseButton", ort, Theme.AccentMoney, "@ui.close", 52, Theme.Background, out _);
            var crt = (RectTransform)close.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.25f);
            crt.sizeDelta = new Vector2(520, 150);

            var skip = UIFactory.Button("SkipButton", ort, Theme.PanelLight, "@ads.skip_no_reward", 30, Theme.TextDim, out _);
            var srt = (RectTransform)skip.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(1, 1);
            srt.sizeDelta = new Vector2(340, 90);
            srt.anchoredPosition = new Vector2(-40, -80);

            var comp = holder.gameObject.AddComponent<FakeAdOverlay>();
            UIFactory.SetRef(comp, "root", overlay.gameObject);
            UIFactory.SetRef(comp, "titleText", title);
            UIFactory.SetRef(comp, "placementText", placement);
            UIFactory.SetRef(comp, "countdownText", countdown);
            UIFactory.SetRef(comp, "closeButton", close);
            UIFactory.SetRef(comp, "skipButton", skip);
            return comp;
        }

        static ConfirmDialog CreateConfirmDialog(RectTransform layer)
        {
            var panel = CreateModal("ConfirmDialog", layer, new Vector2(900, 720), Theme.AccentFame, out var modal);

            var title = UIFactory.Text("Title", panel, "@ui.confirm_title", 64, Theme.TextMain, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -150), new Vector2(-40, -50));
            title.fontStyle = FontStyles.Bold;
            var message = UIFactory.Text("Message", panel, "", 36, Theme.TextDim, TextAlignmentOptions.Top,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(60, 230), new Vector2(-60, -170));
            message.richText = true;
            message.textWrappingMode = TextWrappingModes.Normal;

            var cancel = UIFactory.Button("CancelButton", panel, Theme.PanelLight, "@ui.cancel", 40, Theme.TextMain, out _);
            var crt = (RectTransform)cancel.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0, 0);
            crt.pivot = new Vector2(0, 0);
            crt.sizeDelta = new Vector2(370, 140);
            crt.anchoredPosition = new Vector2(50, 50);

            var confirm = UIFactory.Button("ConfirmButton", panel, Theme.AccentFame, "CONFIRM", 40, Theme.Background, out var confirmLabel);
            UIFactory.SetClick(confirm, ButtonPunch.ClickStyle.Strong);
            var cfrt = (RectTransform)confirm.transform;
            cfrt.anchorMin = cfrt.anchorMax = new Vector2(1, 0);
            cfrt.pivot = new Vector2(1, 0);
            cfrt.sizeDelta = new Vector2(370, 140);
            cfrt.anchoredPosition = new Vector2(-50, 50);

            var dialog = modal.gameObject.AddComponent<ConfirmDialog>();
            UIFactory.SetRef(dialog, "window", modal);
            UIFactory.SetRef(dialog, "titleText", title);
            UIFactory.SetRef(dialog, "messageText", message);
            UIFactory.SetRef(dialog, "confirmButton", confirm);
            UIFactory.SetRef(dialog, "confirmLabel", confirmLabel);
            UIFactory.SetRef(dialog, "confirmImage", confirm.targetGraphic);
            UIFactory.SetRef(dialog, "cancelButton", cancel);
            return dialog;
        }

        /// <summary>Creates root (ModalWindow) > Window (CanvasGroup) > Dim + panel. Returns the panel.</summary>
        static RectTransform CreateModal(string name, RectTransform layer, Vector2 panelSize, Color glow, out ModalWindow modal)
        {
            var root = UIFactory.Rect(name, layer);
            UIFactory.Stretch(root);

            var window = UIFactory.Rect("Window", root);
            UIFactory.Stretch(window);
            var group = window.gameObject.AddComponent<CanvasGroup>();

            var dim = UIFactory.Image("Dim", window, new Color(0.01f, 0.01f, 0.03f, 0.88f));
            UIFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true; // blocks clicks on the game behind

            var panel = UIFactory.GlowPanel("Panel", window, panelSize, glow);

            modal = root.gameObject.AddComponent<ModalWindow>();
            UIFactory.SetRef(modal, "window", window.gameObject);
            UIFactory.SetRef(modal, "group", group);
            UIFactory.SetRef(modal, "panel", panel);
            return panel;
        }

        static void CreateWelcomeBackPopup(RectTransform layer)
        {
            var panel = CreateModal("WelcomeBackPopup", layer, new Vector2(920, 1000), Theme.AccentMoney, out var modal);

            var title = UIFactory.Text("Title", panel, "@offline.title", 84, Theme.AccentMoney, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -170), new Vector2(-40, -60));
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 3;

            var away = UIFactory.Text("AwayText", panel, "You were away for 2h 35m", 42, Theme.TextMain, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -250), new Vector2(-40, -180));
            away.richText = true;

            UIFactory.Text("MadeLabel", panel, "@offline.made", 36, Theme.TextDim, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -350), new Vector2(-40, -290));

            var amount = UIFactory.Text("AmountText", panel, "$124,500", 130, Theme.AccentMoney, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -510), new Vector2(-40, -350));
            amount.fontStyle = FontStyles.Bold;

            var note = UIFactory.Text("NoteText", panel, "245 beats made while you were away", 30, Theme.TextDim, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -580), new Vector2(-40, -520));

            var collect = UIFactory.Button("CollectButton", panel, Theme.AccentMoney, "@ui.collect", 64, Theme.Background, out _);
            UIFactory.SetClick(collect, ButtonPunch.ClickStyle.None); // the money sound plays instead
            var crt = (RectTransform)collect.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0);
            crt.pivot = new Vector2(0.5f, 0);
            crt.sizeDelta = new Vector2(640, 170);
            crt.anchoredPosition = new Vector2(0, 190);

            // Shown only when a rewarded ad is available.
            var doubleBtn = UIFactory.Button("DoubleButton", panel, Theme.AccentFame, "@ads.x2_with_ad", 40, Theme.Background, out _);
            UIFactory.SetClick(doubleBtn, ButtonPunch.ClickStyle.Strong);
            var drt = (RectTransform)doubleBtn.transform;
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0);
            drt.pivot = new Vector2(0.5f, 0);
            drt.sizeDelta = new Vector2(480, 110);
            drt.anchoredPosition = new Vector2(0, 50);

            var popup = modal.gameObject.AddComponent<WelcomeBackPopup>();
            UIFactory.SetRef(popup, "window", modal);
            UIFactory.SetRef(popup, "awayText", away);
            UIFactory.SetRef(popup, "amountText", amount);
            UIFactory.SetRef(popup, "noteText", note);
            UIFactory.SetRef(popup, "collectButton", collect);
            UIFactory.SetRef(popup, "doubleButton", doubleBtn);
        }

        /// <summary>Celebration (HIT/VIRAL, unlocks): full-screen flash + popup card. Non-blocking (no raycasts).</summary>
        static CelebrationPopup CreateCelebrationPopup(RectTransform fxLayer)
        {
            var root = UIFactory.Rect("Celebration", fxLayer);
            UIFactory.Stretch(root);

            var flash = UIFactory.Image("ScreenFlash", root, new Color(1, 1, 1, 0));
            UIFactory.Stretch(flash.rectTransform, -200, -200, -400, -400); // covers notches/safe-area edges too
            flash.raycastTarget = false;

            var popup = UIFactory.Rect("Popup", root);
            UIFactory.Stretch(popup);
            var group = popup.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var glow = UIFactory.RoundedImage("Glow", popup, new Color(1, 0.24f, 0.78f, 0.35f));
            glow.raycastTarget = false;
            // Transparent root that scales; children draw the glow border and the card.
            var panel = UIFactory.RoundedImage("Panel", popup, Color.clear);
            panel.raycastTarget = false;
            var prt = panel.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.6f);
            prt.sizeDelta = new Vector2(900, 460);
            // Glow is a child of the panel so it scales with it.
            glow.rectTransform.SetParent(prt, false);
            UIFactory.Stretch(glow.rectTransform, -18, -18, -18, -18);
            glow.rectTransform.SetAsFirstSibling();
            var inner = UIFactory.RoundedImage("Inner", prt, Theme.Panel);
            UIFactory.Stretch(inner.rectTransform);
            inner.raycastTarget = false;

            var header = UIFactory.Text("Header", prt, "VIRAL BEAT!", 96, Theme.AccentPink, TextAlignmentOptions.Center,
                new Vector2(0, 0.62f), new Vector2(1, 1), new Vector2(30, 0), new Vector2(-30, -30));
            header.fontStyle = FontStyles.Bold;
            header.characterSpacing = 4;
            var title = UIFactory.Text("Title", prt, "\"MIDNIGHT\"", 54, Theme.TextMain, TextAlignmentOptions.Center,
                new Vector2(0, 0.38f), new Vector2(1, 0.62f), new Vector2(30, 0), new Vector2(-30, 0));
            title.fontStyle = FontStyles.Italic;
            var reward = UIFactory.Text("Reward", prt, "+$10,000", 88, Theme.AccentMoney, TextAlignmentOptions.Center,
                new Vector2(0, 0), new Vector2(1, 0.38f), new Vector2(30, 30), new Vector2(-30, 0));
            reward.fontStyle = FontStyles.Bold;

            var comp = root.gameObject.AddComponent<CelebrationPopup>();
            UIFactory.SetRef(comp, "group", group);
            UIFactory.SetRef(comp, "panel", prt);
            UIFactory.SetRef(comp, "panelGlow", glow);
            UIFactory.SetRef(comp, "headerText", header);
            UIFactory.SetRef(comp, "titleText", title);
            UIFactory.SetRef(comp, "valueText", reward);
            UIFactory.SetRef(comp, "screenFlash", flash);
            return comp;
        }

        /// <summary>Scrollable producer list. <paramref name="top"/>/<paramref name="bottom"/> are margins inside the parent.</summary>
        static void CreateProducerList(RectTransform parent, ProducerRow rowPrefab, FloatingTextSpawner floating,
            float top, float bottom, BuyModeSelector buyModeSelector)
        {
            var content = CreateScrollList(parent, "ProducerList", top, bottom, out var area);

            var list = area.gameObject.AddComponent<ProducerListUI>();
            UIFactory.SetRef(list, "rowPrefab", rowPrefab);
            UIFactory.SetRef(list, "container", content);
            UIFactory.SetRef(list, "floatingText", floating);
            if (buyModeSelector != null) UIFactory.SetRef(list, "buyModeSelector", buyModeSelector);
            var so = new SerializedObject(list);
            so.FindProperty("moneyColor").colorValue = Theme.AccentMoney;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Vertical ScrollRect with a masked viewport. Returns the content (VerticalLayoutGroup +
        /// ContentSizeFitter) where rows go; <paramref name="area"/> is the ScrollRect object.
        /// </summary>
        public static RectTransform CreateScrollList(RectTransform parent, string name, float top, float bottom, out RectTransform area)
        {
            area = UIFactory.Rect(name, parent);
            UIFactory.Stretch(area, left: 40, right: 40, top: top, bottom: bottom);

            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40;
            scroll.decelerationRate = 0.12f;

            var viewport = UIFactory.Rect("Viewport", area);
            UIFactory.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            // Invisible graphic so empty space between rows can still be dragged.
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;

            var content = UIFactory.Rect("Rows", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        static void CreateStudioCard(RectTransform parent, Image background, Image accentLine)
        {
            var card = UIFactory.RoundedImage("StudioCard", parent, Theme.Panel);
            var rt = card.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(40, -40 - Theme.StudioCardHeight);
            rt.offsetMax = new Vector2(-40, -40);

            // Equalizer first so it draws behind everything else in the card.
            var eqRoot = UIFactory.Rect("Equalizer", rt);
            eqRoot.anchorMin = new Vector2(0, 0);
            eqRoot.anchorMax = new Vector2(1, 0.5f);
            eqRoot.offsetMin = new Vector2(24, 18);
            eqRoot.offsetMax = new Vector2(-24, 0);
            var eq = eqRoot.gameObject.AddComponent<EqualizerVisualizer>();
            UIFactory.SetRef(eq, "container", eqRoot);

            // Left column: studio name, genre, level pips, beat counter.
            const float rightColumn = 440f;
            var studioName = UIFactory.Text("StudioNameText", rt, "BEDROOM STUDIO", 46, Theme.TextMain, TextAlignmentOptions.TopLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -96), new Vector2(-rightColumn, -36));
            studioName.fontStyle = FontStyles.Bold;
            var genreText = UIFactory.Text("GenreText", rt, "LO-FI", 34, Theme.AccentCyan, TextAlignmentOptions.TopLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -146), new Vector2(-rightColumn, -98));
            genreText.richText = true;

            var pips = new Image[7];
            for (int i = 0; i < pips.Length; i++)
            {
                var pip = UIFactory.RoundedImage("Pip" + i, rt, Theme.TextDim);
                pip.pixelsPerUnitMultiplier = 8;
                pip.raycastTarget = false;
                var prt = pip.rectTransform;
                prt.anchorMin = prt.anchorMax = new Vector2(0, 1);
                prt.pivot = new Vector2(0, 1);
                prt.sizeDelta = new Vector2(40, 12);
                prt.anchoredPosition = new Vector2(40 + i * 50, -160);
                pips[i] = pip;
            }

            var count = UIFactory.Text("BeatCountText", rt, "0", 96, Theme.TextMain, TextAlignmentOptions.BottomLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 136), new Vector2(-rightColumn, 240));
            count.fontStyle = FontStyles.Bold;
            count.rectTransform.pivot = new Vector2(0, 0); // bounce from the bottom-left
            count.rectTransform.offsetMin = new Vector2(40, 136);
            count.rectTransform.offsetMax = new Vector2(-rightColumn, 240);
            var label = UIFactory.Text("BeatCountLabel", rt, "BEATS MADE", 28, Theme.TextDim, TextAlignmentOptions.TopLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 100), new Vector2(-rightColumn, 136));
            label.characterSpacing = 8;

            var counter = card.gameObject.AddComponent<BeatCounterUI>();
            UIFactory.SetRef(counter, "countText", count);
            UIFactory.SetRef(counter, "labelText", label);

            CreateArtistStrip(rt);

            // Right column: genre unlock (top) and studio upgrade (bottom).
            var genreBtn = CreateCardButton("GenreUnlockButton", rt, Theme.AccentCyan, "UNLOCK HIP-HOP", "$1,000", -24, out var gLabel, out var gCost);
            var genreComp = genreBtn.gameObject.AddComponent<GenreUnlockButton>();
            UIFactory.SetRef(genreComp, "buttonImage", genreBtn);
            UIFactory.SetRef(genreComp, "labelText", gLabel);
            UIFactory.SetRef(genreComp, "costText", gCost);

            var studioBtn = CreateCardButton("StudioUpgradeButton", rt, Theme.TextMain, "UPGRADE STUDIO", "$10K",
                -24 - Theme.CardButtonHeight - 12, out var sLabel, out var sCost);
            var studioComp = studioBtn.gameObject.AddComponent<StudioUpgradeButton>();
            UIFactory.SetRef(studioComp, "labelText", sLabel);
            UIFactory.SetRef(studioComp, "costText", sCost);

            var theme = card.gameObject.AddComponent<GenreThemeApplier>();
            UIFactory.SetRef(theme, "background", background);
            UIFactory.SetRef(theme, "genreText", genreText);
            var themeSo = new SerializedObject(theme);
            UIFactory.SetArray(themeSo.FindProperty("accentGraphics"), new Object[] { accentLine });
            themeSo.ApplyModifiedPropertiesWithoutUndo();

            var studioUi = card.gameObject.AddComponent<StudioCardUI>();
            UIFactory.SetRef(studioUi, "cardImage", card);
            UIFactory.SetRef(studioUi, "studioNameText", studioName);
            UIFactory.SetRef(studioUi, "equalizer", eq);
            var studioSo = new SerializedObject(studioUi);
            UIFactory.SetArray(studioSo.FindProperty("levelPips"), pips);
            studioSo.FindProperty("pipOnColor").colorValue = Theme.AccentCyan;
            studioSo.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Two-line button for the studio card's right column ("LABEL" / "$COST").</summary>
        static Image CreateCardButton(string name, RectTransform card, Color color, string labelText, string costText,
            float y, out TextMeshProUGUI label, out TextMeshProUGUI cost)
        {
            var img = UIFactory.RoundedImage(name, card, color);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(380, Theme.CardButtonHeight);
            rt.anchoredPosition = new Vector2(-24, y);

            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.38f, 0.36f, 0.45f, 1f); // opaque so the equalizer doesn't show through
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            img.gameObject.AddComponent<ButtonPunch>();

            label = UIFactory.Text("Label", rt, labelText, 26, Theme.Background, TextAlignmentOptions.Bottom,
                new Vector2(0, 0.52f), new Vector2(1, 1), new Vector2(14, 0), new Vector2(-14, -14));
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 2;
            cost = UIFactory.Text("Cost", rt, costText, 40, Theme.Background, TextAlignmentOptions.Top,
                new Vector2(0, 0), new Vector2(1, 0.52f), new Vector2(14, 12), new Vector2(-14, 0));
            cost.fontStyle = FontStyles.Bold;
            return img;
        }

        static void CreateMakeBeatButton(RectTransform parent, FloatingTextSpawner floating, float top, RectTransform beatOrigin)
        {
            // Soft neon glow behind the button.
            var glow = UIFactory.Image("MakeBeatGlow", parent, new Color(Theme.AccentPink.r, Theme.AccentPink.g, Theme.AccentPink.b, 0.3f));
            glow.sprite = UIStyle.SoftGlowSprite;
            glow.type = Image.Type.Sliced;
            glow.raycastTarget = false;
            var grt = glow.rectTransform;
            grt.anchorMin = new Vector2(0, 1);
            grt.anchorMax = new Vector2(1, 1);
            grt.pivot = new Vector2(0.5f, 0.5f);
            grt.offsetMin = new Vector2(0, -top - Theme.MakeBeatHeight - 40);
            grt.offsetMax = new Vector2(0, -top + 40);
            glow.gameObject.AddComponent<PulseGlow>();

            var image = UIFactory.RoundedImage("MakeBeatButton", parent, Theme.AccentPink);
            var rt = image.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(40, -top - Theme.MakeBeatHeight);
            rt.offsetMax = new Vector2(-40, -top);

            var noteIcon = UIFactory.Image("Icon", rt, Color.white);
            noteIcon.sprite = LoadSprite(Art.ArtGenerator.Icons + "/icon_beat.png");
            noteIcon.raycastTarget = false;
            var nrt = noteIcon.rectTransform;
            nrt.anchorMin = nrt.anchorMax = new Vector2(0, 0.5f);
            nrt.sizeDelta = new Vector2(96, 96);
            nrt.anchoredPosition = new Vector2(110, 10);

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            button.colors = colors;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            var punch = image.gameObject.AddComponent<ButtonPunch>();
            var punchSo = new SerializedObject(punch);
            punchSo.FindProperty("clickStyle").enumValueIndex = (int)ButtonPunch.ClickStyle.None; // MAKE BEAT plays the beat sound instead
            punchSo.ApplyModifiedPropertiesWithoutUndo();

            var title = UIFactory.Text("Label", rt, "@home.make_beat", 76, Color.white, TextAlignmentOptions.Center,
                new Vector2(0, 0.38f), new Vector2(1, 1), new Vector2(150, 0), new Vector2(-40, -16));
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 6;
            var reward = UIFactory.Text("RewardText", rt, "+$5 per beat", 36, new Color(1, 1, 1, 0.85f), TextAlignmentOptions.Top,
                new Vector2(0, 0), new Vector2(1, 0.38f), new Vector2(150, 16), new Vector2(-40, 0));

            var makeBeat = image.gameObject.AddComponent<MakeBeatButton>();
            UIFactory.SetRef(makeBeat, "rewardText", reward);
            UIFactory.SetRef(makeBeat, "floatingText", floating);
            UIFactory.SetRef(makeBeat, "spawnPoint", beatOrigin != null ? beatOrigin : rt);
            var so = new SerializedObject(makeBeat);
            so.FindProperty("floatingTextColor").colorValue = Theme.AccentMoney;
            so.FindProperty("verticalOffset").floatValue = 20f;
            so.FindProperty("spread").floatValue = 90f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Small tinted icon in the top bar. pos = top-left corner offset from the anchor.</summary>
        static void TopIcon(RectTransform bar, string icon, Vector2 anchor, Vector2 pos, Color color)
        {
            var img = UIFactory.Image(icon, bar, color);
            img.sprite = LoadSprite(Art.ArtGenerator.Icons + "/" + icon + ".png");
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0, anchor.y >= 1 ? 1 : 0);
            rt.sizeDelta = new Vector2(34, 34);
            rt.anchoredPosition = pos;
        }

        static Image CreateTopBar(RectTransform parent, out StreamCounterHUD streamCounter)
        {
            var bar = UIFactory.Image("TopBar", parent, Theme.Panel);
            UIStyle.GradientBar(bar);
            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, Theme.TopBarHeight);
            rt.anchoredPosition = Vector2.zero;

            var accent = UIFactory.Image("AccentLine", rt, Theme.AccentMoney);
            accent.rectTransform.anchorMin = new Vector2(0, 0);
            accent.rectTransform.anchorMax = new Vector2(1, 0);
            accent.rectTransform.pivot = new Vector2(0.5f, 0);
            accent.rectTransform.sizeDelta = new Vector2(0, 4);
            accent.raycastTarget = false;

            // Money (left, big) with income per second under it.
            TopIcon(rt, "icon_money", new Vector2(0, 1), new Vector2(40, -22), Theme.AccentMoney);
            UIFactory.Text("MoneyLabel", rt, "@hud.money", 30, Theme.TextDim, TextAlignmentOptions.BottomLeft,
                new Vector2(0, 0.72f), new Vector2(0.68f, 1f), new Vector2(84, 0), new Vector2(0, -18));
            var moneyText = UIFactory.Text("MoneyText", rt, "$0", 96, Theme.AccentMoney, TextAlignmentOptions.Left,
                new Vector2(0, 0.3f), new Vector2(0.68f, 0.72f), new Vector2(40, 0), new Vector2(0, 0));
            moneyText.fontStyle = FontStyles.Bold;
            // Punch animation scales from the left. Changing the pivot moves the rect, so restore the offsets.
            moneyText.rectTransform.pivot = new Vector2(0, 0.5f);
            moneyText.rectTransform.offsetMin = new Vector2(40, 0);
            moneyText.rectTransform.offsetMax = Vector2.zero;
            var incomeText = UIFactory.Text("IncomeText", rt, "", 34, Theme.AccentMoney * new Color(1, 1, 1, 0.7f), TextAlignmentOptions.TopLeft,
                new Vector2(0, 0f), new Vector2(0.68f, 0.3f), new Vector2(40, 10), new Vector2(0, 0));

            // Beats made (middle of the bottom row)
            TopIcon(rt, "icon_beat", new Vector2(0.38f, 0), new Vector2(0, 30), Theme.AccentCyan);
            var beatsText = UIFactory.Text("BeatsText", rt, "0", 34, Theme.TextMain, TextAlignmentOptions.Left,
                new Vector2(0.38f, 0), new Vector2(0.66f, 0.3f), new Vector2(44, 10), new Vector2(0, 0));
            beatsText.fontStyle = FontStyles.Bold;
            var beatsLabel = UIFactory.Text("BeatsLabel", rt, "BEATS", 20, Theme.TextDim, TextAlignmentOptions.BottomLeft,
                new Vector2(0.38f, 0.22f), new Vector2(0.66f, 0.42f), new Vector2(44, 0), new Vector2(0, 0));
            var counter = bar.gameObject.AddComponent<BeatCounterUI>();
            UIFactory.SetRef(counter, "countText", beatsText);
            UIFactory.SetRef(counter, "labelText", beatsLabel);

            // Fame (right)
            TopIcon(rt, "icon_fame", new Vector2(1, 1), new Vector2(-170, -22), Theme.AccentFame);
            UIFactory.Text("FameLabel", rt, "@hud.fame", 30, Theme.TextDim, TextAlignmentOptions.BottomRight,
                new Vector2(0.68f, 0.72f), new Vector2(1, 1f), new Vector2(0, 0), new Vector2(-40, -18));
            var fameText = UIFactory.Text("FameText", rt, "0", 60, Theme.AccentFame, TextAlignmentOptions.Right,
                new Vector2(0.68f, 0.3f), new Vector2(1, 0.72f), new Vector2(0, 0), new Vector2(-40, 0));
            fameText.fontStyle = FontStyles.Bold;

            var hud = bar.gameObject.AddComponent<CurrencyHUD>();
            UIFactory.SetRef(hud, "moneyText", moneyText);
            UIFactory.SetRef(hud, "fameText", fameText);
            UIFactory.SetRef(hud, "incomeText", incomeText);

            // Streams pill (bottom right). Tapping it opens the MUSIC tab.
            var pill = UIFactory.Button("StreamsPill", rt, Theme.PanelLight, "► 0 streams", 28, Theme.AccentCyan, out var pillText);
            var prt = (RectTransform)pill.transform;
            prt.anchorMin = prt.anchorMax = new Vector2(1, 0);
            prt.pivot = new Vector2(1, 0);
            prt.sizeDelta = new Vector2(340, 58);
            prt.anchoredPosition = new Vector2(-32, 18);
            pillText.fontStyle = FontStyles.Bold;
            streamCounter = pill.gameObject.AddComponent<StreamCounterHUD>();
            UIFactory.SetRef(streamCounter, "text", pillText);
            UIFactory.SetRef(streamCounter, "button", pill);
            return accent;
        }

        // ------------------------------------------------------------------ helpers

        static void EnsureFolders()
        {
            // Icons moved from Art/Icons to Art/UI/Icons (MoveAsset keeps references).
            EnsureFolder(Root + "/Art/UI");
            if (AssetDatabase.IsValidFolder(Root + "/Art/Icons") && !AssetDatabase.IsValidFolder(Root + "/Art/UI/Icons"))
                AssetDatabase.MoveAsset(Root + "/Art/Icons", Root + "/Art/UI/Icons");

            string[] folders =
            {
                Root + "/Scenes", Root + "/Data/Config", Root + "/Data/Producers", Root + "/Data/Genres",
                Root + "/Data/Studios", Root + "/Data/Artists", Root + "/Prefabs", Root + "/Audio",
                Root + "/Art/Environments/Basement", Root + "/Art/Environments/BedroomStudio",
                Root + "/Art/Environments/ProfessionalStudio", Root + "/Art/Environments/ModernStudio",
                Root + "/Art/Props/Computer", Root + "/Art/Props/Monitor", Root + "/Art/Props/Speakers",
                Root + "/Art/Props/MIDI", Root + "/Art/Props/Microphone", Root + "/Art/Props/Furniture",
                Root + "/Art/Characters/Player", Root + "/Art/Characters/Producers", Root + "/Art/Characters/Portraits",
                Root + "/Art/DAW", Root + "/Art/UI/Icons", Root + "/Art/Effects", Root + "/Animations",
            };
            foreach (var f in folders) EnsureFolder(f);
        }

        public static void EnsureFolderPublic(string path) => EnsureFolder(path);

        /// <summary>Creates a folder (and its parents) through the AssetDatabase so Unity knows about it immediately.</summary>
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        static void AddSceneToBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    /// <summary>Colors and sizes shared by the generated UI.</summary>
    public static class Theme
    {
        public static readonly Color Background = Hex("0D0B1A");
        public static readonly Color Panel = Hex("17132B");
        public static readonly Color PanelLight = Hex("231D40");
        public static readonly Color TextMain = Hex("F2F0FF");
        public static readonly Color TextDim = Hex("8A84B0");
        public static readonly Color AccentMoney = Hex("4DFFA8");
        public static readonly Color AccentFame = Hex("FF8A3D");
        public static readonly Color AccentPink = Hex("FF3DC8");
        public static readonly Color AccentCyan = Hex("3DE1FF");

        public const float TopBarHeight = 240f;
        public const float BoostBarHeight = 80f;
        public const float BottomNavHeight = 180f;
        public const float StudioCardHeight = 420f;
        public const float CardButtonHeight = 150f;
        public const float MakeBeatHeight = 190f;
        public const float SceneTop = 24f;
        public const float SceneHeight = Art.ArtGenerator.CardH; // the studio card shows the whole room (1000 wide)

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }

    /// <summary>Small helpers that create UI objects with sensible defaults.</summary>
    public static class UIFactory
    {
        /// <summary>9-sliced rounded rectangle used for panels and buttons (set by the scene builder).</summary>
        public static Sprite RoundedSprite;
        /// <summary>Plain circle (set by the scene builder).</summary>
        public static Sprite CircleSprite;
        /// <summary>Rounded frame, 9-sliced (set by the scene builder).</summary>
        public static Sprite OutlineSprite;

        public static Image RoundedImage(string name, Transform parent, Color color)
        {
            var img = Image(name, parent, color);
            img.sprite = RoundedSprite;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        /// <summary>Big rounded button with one centered label, a press bounce and no keyboard navigation.</summary>
        public static Button Button(string name, Transform parent, Color color, string text, float fontSize,
            Color textColor, out TextMeshProUGUI label)
        {
            var img = RoundedImage(name, parent, color);
            var button = img.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.38f, 0.36f, 0.45f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            img.gameObject.AddComponent<ButtonPunch>();

            label = Text("Label", img.rectTransform, text, fontSize, textColor, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(16, 8), new Vector2(-16, -8));
            label.fontStyle = FontStyles.Bold;
            return button;
        }

        /// <summary>Card with a colored glow border, used by popups.</summary>
        public static RectTransform GlowPanel(string name, Transform parent, Vector2 size, Color glowColor)
        {
            var root = Rect(name, parent);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = size;
            // Soft neon halo around the card (the opaque center is hidden behind the card).
            var glow = Image("Glow", root, new Color(glowColor.r, glowColor.g, glowColor.b, 0.2f));
            glow.sprite = UIStyle.SoftGlowSprite;
            glow.type = UnityEngine.UI.Image.Type.Sliced;
            Stretch(glow.rectTransform, -50, -50, -50, -50);
            glow.raycastTarget = false;
            var inner = RoundedImage("Inner", root, Theme.Panel);
            Stretch(inner.rectTransform);
            return root;
        }

        /// <summary>Sets which click sound a button plays (None when its action already has a sound).</summary>
        public static void SetClick(Component button, ButtonPunch.ClickStyle style)
        {
            var punch = button.GetComponent<ButtonPunch>();
            if (punch == null) return;
            var so = new SerializedObject(punch);
            so.FindProperty("clickStyle").enumValueIndex = (int)style;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Fills a serialized array of object references.</summary>
        public static void SetArray(SerializedProperty array, Object[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        /// <summary>Assigns a private [SerializeField] reference on a component.</summary>
        public static void SetRef(Component target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[Beat Factory] Field '{field}' not found on {target.GetType().Name}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Image(string name, Transform parent, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = Rect(name, parent);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = Localize(tmp, text);
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            // Long numbers shrink to fit instead of being cut off.
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = size;
            // translations differ in length: shrink first, and only then end with "…" (never a hard cut)
            tmp.fontSizeMin = size * 0.4f;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>
        /// "@key" → fixed localized label: adds a LocalizedText (updates on language change) and returns the English text.
        /// Any other text is a placeholder that the runtime scripts overwrite.
        /// </summary>
        public static string Localize(TMP_Text tmp, string text)
        {
            if (string.IsNullOrEmpty(text) || text[0] != '@') return text;
            string key = text.Substring(1);
            var lt = tmp.gameObject.AddComponent<LocalizedText>();
            var so = new SerializedObject(lt);
            so.FindProperty("key").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
            return EnglishText(key);
        }

        static System.Collections.Generic.Dictionary<string, string> english;

        /// <summary>English text for a key, read straight from the table file (independent of the editor's language).</summary>
        public static string EnglishText(string key)
        {
            if (english == null)
            {
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(BeatFactorySceneBuilder.Root + "/Resources/Localization/en.txt");
                english = asset != null ? LocalizationDatabase.Parse(asset.text) : new System.Collections.Generic.Dictionary<string, string>();
            }
            if (english.TryGetValue(key, out var v)) return v;
            Debug.LogWarning("[Beat Factory] Missing English text for key " + key);
            return key;
        }

        public static void ResetEnglishCache() => english = null;

        /// <summary>Fills the parent, with optional margins in reference pixels.</summary>
        public static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
