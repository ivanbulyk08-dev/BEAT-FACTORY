using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory.EditorTools
{
    /// <summary>First-time tutorial banner + highlight frame on the HOME screen.</summary>
    public static partial class BeatFactorySceneBuilder
    {
        static TutorialUI CreateTutorial(RectTransform home)
        {
            // Pulsing frame drawn around the button the player should press (never blocks clicks).
            var frame = UIFactory.Image("TutorialHighlight", home, Theme.AccentCyan);
            frame.sprite = UIFactory.OutlineSprite;
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;
            frame.gameObject.SetActive(false);

            // Banner at the bottom of HOME (over the lower producer cards; the first card stays visible).
            var banner = UIFactory.RoundedImage("TutorialBanner", home, Theme.PanelLight);
            var brt = banner.rectTransform;
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0);
            brt.offsetMin = new Vector2(40, 14);
            brt.offsetMax = new Vector2(-40, 14 + 190);

            var border = UIFactory.Image("Border", brt, Theme.AccentCyan);
            border.sprite = UIFactory.OutlineSprite;
            border.type = Image.Type.Sliced;
            border.raycastTarget = false;
            UIFactory.Stretch(border.rectTransform);

            var message = UIFactory.Text("Message", brt, "Welcome!", 36, Theme.TextMain, TextAlignmentOptions.TopLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(36, 24), new Vector2(-300, -24));
            message.richText = true;
            message.textWrappingMode = TextWrappingModes.Normal;
            message.fontSizeMin = 24;

            var progress = UIFactory.Text("Progress", brt, "", 34, Theme.AccentMoney, TextAlignmentOptions.Right,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(-280, 20), new Vector2(-36, -60));
            progress.fontStyle = FontStyles.Bold;

            var action = UIFactory.Button("ActionButton", brt, Theme.AccentCyan, "@tutorial.lets_go", 34, Theme.Background, out var actionLabel);
            var art = (RectTransform)action.transform;
            art.anchorMin = art.anchorMax = new Vector2(1, 0.5f);
            art.pivot = new Vector2(1, 0.5f);
            art.sizeDelta = new Vector2(240, 110);
            art.anchoredPosition = new Vector2(-28, 0);

            var skip = UIFactory.Button("SkipButton", brt, new Color(0, 0, 0, 0), "@tutorial.skip", 24, Theme.TextDim, out _);
            var srt = (RectTransform)skip.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(1, 1);
            srt.sizeDelta = new Vector2(120, 56);
            srt.anchoredPosition = new Vector2(-12, -8);

            var ui = brt.gameObject.AddComponent<TutorialUI>();
            UIFactory.SetRef(ui, "banner", brt);
            UIFactory.SetRef(ui, "messageText", message);
            UIFactory.SetRef(ui, "progressText", progress);
            UIFactory.SetRef(ui, "actionButton", action);
            UIFactory.SetRef(ui, "actionLabel", actionLabel);
            UIFactory.SetRef(ui, "skipButton", skip);
            UIFactory.SetRef(ui, "highlight", frame.rectTransform);
            UIFactory.SetRef(ui, "makeBeatTarget", home.Find("MakeBeatButton"));
            UIFactory.SetRef(ui, "genreTarget", home.Find("StudioCard/GenreUnlockButton"));
            UIFactory.SetRef(ui, "producerRows", home.Find("ProducerList/Viewport/Rows"));
            return ui;
        }

        static RectTransform CreateSettingsScreen(RectTransform content, out SettingsScreenUI ui)
        {
            var screen = CreateScreen("SettingsScreen", content);
            CreateScreenTitle(screen, "@nav.settings");
            var language = CreateLanguageButton(screen);

            // Switches
            const float rowH = 96f;
            var audio = CreateCard("SwitchesCard", screen, ScreenListTop, rowH * 3 + 30);
            var music = CreateSwitchRow(audio, "Music", 0, rowH, "@settings.music");
            var sound = CreateSwitchRow(audio, "Sound effects", 1, rowH, "@settings.sound");
            var notif = CreateSwitchRow(audio, "Notifications", 2, rowH, "@settings.notifications");

            // Volume sliders
            const float sliderRowH = 90f;
            float volumeTop = ScreenListTop + rowH * 3 + 30 + 20;
            var volume = CreateCard("VolumeCard", screen, volumeTop, sliderRowH * 3 + 30);
            var master = CreateSliderRow(volume, "Master", 0, sliderRowH, "@settings.volume_master");
            var musicVol = CreateSliderRow(volume, "Music", 1, sliderRowH, "@settings.volume_music");
            var sfxVol = CreateSliderRow(volume, "SFX", 2, sliderRowH, "@settings.volume_sfx");

            // Concerts: music on/off + volume, effects quality
            float concertTop = volumeTop + sliderRowH * 3 + 30 + 20;
            var concert = CreateCard("ConcertCard", screen, concertTop, rowH + sliderRowH + rowH + 30);
            var concertMusic = CreateSwitchRow(concert, "Concert Music", 0, rowH, "@settings.concert_music");
            var concertVol = CreateSliderRow(concert, "Concert", 0, sliderRowH, "@settings.volume_concert");
            ((RectTransform)concertVol.transform).anchoredPosition += new Vector2(0, -rowH);
            var concertVolLabel = concert.Find("ConcertLabel") as RectTransform;
            if (concertVolLabel != null) concertVolLabel.anchoredPosition += new Vector2(0, -rowH);
            float qTop = 15 + rowH + sliderRowH;
            UIFactory.Text("QualityLabel", concert, "@settings.concert_effects", 40, Theme.TextMain, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -qTop - rowH), new Vector2(-330, -qTop));
            var quality = UIFactory.Button("QualityButton", concert, Theme.PanelLight, "AUTO", 30, Theme.AccentCyan, out var qualityLabel);
            var qrt = (RectTransform)quality.transform;
            qrt.anchorMin = qrt.anchorMax = new Vector2(1, 1);
            qrt.pivot = new Vector2(1, 0.5f);
            qrt.sizeDelta = new Vector2(290, 78);
            qrt.anchoredPosition = new Vector2(-40, -qTop - rowH * 0.5f);

            // Stats
            float statsTop = concertTop + rowH + sliderRowH + rowH + 30 + 20;
            var stats = CreateCard("StatsCard", screen, statsTop, 260);
            UIFactory.Text("Header", stats, "@settings.stats", 28, Theme.TextDim, TextAlignmentOptions.TopLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -64), new Vector2(-40, -24)).characterSpacing = 6;
            var statsText = UIFactory.Text("StatsText", stats, "", 28, Theme.TextDim, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(40, 14), new Vector2(-40, -62));
            statsText.richText = true;
            statsText.enableAutoSizing = false;
            statsText.lineSpacing = 0;

            // Buttons
            float buttonsTop = statsTop + 260 + 20;
            var replay = UIFactory.Button("ReplayTutorialButton", screen, Theme.PanelLight, "@settings.replay_tutorial", 36, Theme.TextMain, out _);
            PlaceTopButton((RectTransform)replay.transform, buttonsTop);
            var reset = UIFactory.Button("ResetButton", screen, new Color(1f, 0.24f, 0.35f), "@settings.reset_save", 36, Theme.TextMain, out _);
            PlaceTopButton((RectTransform)reset.transform, buttonsTop + 125);

            var version = UIFactory.Text("Version", screen, "Beat Factory v1.0", 26, Theme.TextDim, TextAlignmentOptions.Bottom,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 24), new Vector2(-40, 70));

            // privacy row above the version: policy link (when set) + ad consent settings (EEA/UK only)
            var policy = UIFactory.Button("PrivacyPolicyButton", screen, new Color(0, 0, 0, 0), "@settings.privacy_policy", 26, Theme.AccentCyan, out _);
            var prt = (RectTransform)policy.transform;
            prt.anchorMin = new Vector2(0, 0); prt.anchorMax = new Vector2(0.5f, 0);
            prt.offsetMin = new Vector2(40, 74); prt.offsetMax = new Vector2(-8, 134);
            var consent = UIFactory.Button("PrivacyOptionsButton", screen, new Color(0, 0, 0, 0), "@settings.privacy_options", 26, Theme.AccentCyan, out _);
            var crt2 = (RectTransform)consent.transform;
            crt2.anchorMin = new Vector2(0.5f, 0); crt2.anchorMax = new Vector2(1, 0);
            crt2.offsetMin = new Vector2(8, 74); crt2.offsetMax = new Vector2(-40, 134);

            ui = screen.gameObject.AddComponent<SettingsScreenUI>();
            UIFactory.SetRef(ui, "musicToggle", music);
            UIFactory.SetRef(ui, "soundToggle", sound);
            UIFactory.SetRef(ui, "notificationsToggle", notif);
            UIFactory.SetRef(ui, "masterSlider", master);
            UIFactory.SetRef(ui, "musicSlider", musicVol);
            UIFactory.SetRef(ui, "sfxSlider", sfxVol);
            UIFactory.SetRef(ui, "concertMusicToggle", concertMusic);
            UIFactory.SetRef(ui, "concertMusicSlider", concertVol);
            UIFactory.SetRef(ui, "concertQualityButton", quality);
            UIFactory.SetRef(ui, "concertQualityLabel", qualityLabel);
            UIFactory.SetRef(ui, "replayTutorialButton", replay);
            UIFactory.SetRef(ui, "resetButton", reset);
            UIFactory.SetRef(ui, "statsText", statsText);
            UIFactory.SetRef(ui, "versionText", version);
            UIFactory.SetRef(ui, "languageButton", language);
            UIFactory.SetRef(ui, "privacyPolicyButton", policy);
            UIFactory.SetRef(ui, "privacyOptionsButton", consent);
            return screen;
        }

        /// <summary>Language pill in the SETTINGS title row ("🌐 English") — opens the language picker.</summary>
        static Button CreateLanguageButton(RectTransform screen)
        {
            var b = UIFactory.Button("LanguageButton", screen, Theme.PanelLight, "English", 32, Theme.AccentCyan, out var label);
            label.fontSizeMin = 20;
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(440, 92);
            rt.anchoredPosition = new Vector2(-40, -34);
            return b;
        }

        static void PlaceTopButton(RectTransform rt, float top)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(40, -top - 110);
            rt.offsetMax = new Vector2(-40, -top);
        }

        /// <summary>Label + rounded slider (0..1) with a round handle.</summary>
        static Slider CreateSliderRow(RectTransform card, string label, int index, float rowHeight, string key = null)
        {
            float top = 15 + index * rowHeight;
            UIFactory.Text(label + "Label", card, key ?? label, 36, Theme.TextMain, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -top - rowHeight), new Vector2(260, -top));

            var root = UIFactory.Rect(label + "Slider", card);
            root.anchorMin = new Vector2(0, 1);
            root.anchorMax = new Vector2(1, 1);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = new Vector2(280, -top - rowHeight * 0.5f - 30);
            root.offsetMax = new Vector2(-50, -top - rowHeight * 0.5f + 30);

            var bg = UIFactory.RoundedImage("Background", root, Theme.PanelLight);
            bg.pixelsPerUnitMultiplier = 4;
            var bgrt = bg.rectTransform;
            bgrt.anchorMin = new Vector2(0, 0.5f);
            bgrt.anchorMax = new Vector2(1, 0.5f);
            bgrt.sizeDelta = new Vector2(0, 18);

            var fillArea = UIFactory.Rect("Fill Area", root);
            fillArea.anchorMin = new Vector2(0, 0.5f);
            fillArea.anchorMax = new Vector2(1, 0.5f);
            fillArea.sizeDelta = new Vector2(0, 18);
            var fill = UIFactory.RoundedImage("Fill", fillArea, Theme.AccentCyan);
            fill.pixelsPerUnitMultiplier = 4;
            fill.raycastTarget = false;
            UIFactory.Stretch(fill.rectTransform);

            var handleArea = UIFactory.Rect("Handle Slide Area", root);
            UIFactory.Stretch(handleArea, 26, 26, 0, 0);
            var handle = UIFactory.Image("Handle", handleArea, Color.white);
            handle.sprite = UIFactory.CircleSprite;
            handle.rectTransform.sizeDelta = new Vector2(52, 0);
            handle.rectTransform.anchorMin = new Vector2(0, 0);
            handle.rectTransform.anchorMax = new Vector2(0, 1);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            var nav = slider.navigation;
            nav.mode = Navigation.Mode.None;
            slider.navigation = nav;
            return slider;
        }

        static ToggleSwitch CreateSwitchRow(RectTransform card, string label, int index, float rowHeight, string key = null)
        {
            float top = 15 + index * rowHeight;
            var text = UIFactory.Text(label + "Label", card, key ?? label, 40, Theme.TextMain, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -top - rowHeight), new Vector2(-220, -top));

            var track = UIFactory.RoundedImage(label + "Switch", card, Theme.PanelLight);
            var trt = track.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(1, 0.5f);
            trt.sizeDelta = new Vector2(150, 78);
            trt.anchoredPosition = new Vector2(-40, -top - rowHeight * 0.5f);
            var button = track.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            track.gameObject.AddComponent<ButtonPunch>();

            var knob = UIFactory.Image("Knob", trt, Color.white);
            knob.sprite = UIFactory.CircleSprite;
            knob.raycastTarget = false;
            knob.rectTransform.sizeDelta = new Vector2(64, 64);

            var toggle = track.gameObject.AddComponent<ToggleSwitch>();
            UIFactory.SetRef(toggle, "track", track);
            UIFactory.SetRef(toggle, "knob", knob.rectTransform);
            var so = new UnityEditor.SerializedObject(toggle);
            so.FindProperty("onColor").colorValue = Theme.AccentMoney;
            so.FindProperty("offColor").colorValue = Theme.PanelLight;
            so.ApplyModifiedPropertiesWithoutUndo();
            return toggle;
        }
    }
}
