using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory.EditorTools
{
    /// <summary>Screens (tabs) and the bottom navigation bar.</summary>
    public static partial class BeatFactorySceneBuilder
    {
        public enum NavBadgeSource { None, Music, Producers, Artists, Prestige }

        static RectTransform CreateCard(string name, RectTransform screen, float top, float height)
        {
            var card = UIFactory.RoundedImage(name, screen, Theme.Panel);
            var rt = card.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(40, -top - height);
            rt.offsetMax = new Vector2(-40, -top);
            return rt;
        }

        static RectTransform CreatePrestigeScreen(RectTransform content, out PrestigeScreenUI ui)
        {
            var screen = CreateScreen("PrestigeScreen", content);
            CreateScreenTitle(screen, "@nav.prestige");

            // Fame card
            var fame = CreateCard("FameCard", screen, ScreenListTop, 320);
            UIFactory.Text("FameLabel", fame, "@prestige.fame_points", 30, Theme.TextDim, TextAlignmentOptions.TopLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -80), new Vector2(-40, -36)).characterSpacing = 6;
            var count = UIFactory.Text("CountText", fame, "First run", 30, Theme.TextDim, TextAlignmentOptions.TopRight,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -80), new Vector2(-40, -36));
            var fameText = UIFactory.Text("FameText", fame, "0", 140, Theme.AccentFame, TextAlignmentOptions.Left,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(36, 90), new Vector2(-40, -80));
            fameText.fontStyle = FontStyles.Bold;
            var bonus = UIFactory.Text("BonusText", fame, "+0% income forever", 38, Theme.AccentMoney, TextAlignmentOptions.BottomLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 30), new Vector2(-40, 90));

            // Restart card
            const float restartTop = ScreenListTop + 320 + 30;
            var restart = CreateCard("RestartCard", screen, restartTop, 470);
            var pending = UIFactory.Text("PendingText", restart, "Restart now for +3 Fame", 46, Theme.TextMain, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -110), new Vector2(-30, -36));
            pending.richText = true;
            var detail = UIFactory.Text("DetailText", restart, "", 34, Theme.TextDim, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -170), new Vector2(-30, -116));
            detail.richText = true;

            var barBg = UIFactory.RoundedImage("ProgressBar", restart, Theme.Background);
            barBg.pixelsPerUnitMultiplier = 3;
            var brt = barBg.rectTransform;
            brt.anchorMin = new Vector2(0, 1);
            brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(0.5f, 1);
            brt.offsetMin = new Vector2(60, -228);
            brt.offsetMax = new Vector2(-60, -196);
            var fill = UIFactory.RoundedImage("Fill", brt, Theme.AccentFame);
            fill.pixelsPerUnitMultiplier = 3;
            var frt = fill.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(0, 1);
            frt.offsetMin = frt.offsetMax = Vector2.zero;

            var button = UIFactory.Button("RestartButton", restart, Theme.AccentFame, "@prestige.restart_button", 60, Theme.Background, out var buttonLabel);
            UIFactory.SetClick(button, ButtonPunch.ClickStyle.Strong);
            buttonLabel.characterSpacing = 4;
            var btn = (RectTransform)button.transform;
            btn.anchorMin = btn.anchorMax = new Vector2(0.5f, 0);
            btn.pivot = new Vector2(0.5f, 0);
            btn.sizeDelta = new Vector2(800, 170);
            btn.anchoredPosition = new Vector2(0, 40);

            // What resets / what stays
            var info = CreateCard("InfoCard", screen, restartTop + 470 + 30, 220);
            var infoText = UIFactory.Text("InfoText", info,
                "@prestige.info",
                30, Theme.TextDim, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, new Vector2(40, 30), new Vector2(-40, -30));
            infoText.richText = true;
            infoText.textWrappingMode = TextWrappingModes.Normal;

            ui = screen.gameObject.AddComponent<PrestigeScreenUI>();
            UIFactory.SetRef(ui, "fameText", fameText);
            UIFactory.SetRef(ui, "bonusText", bonus);
            UIFactory.SetRef(ui, "countText", count);
            UIFactory.SetRef(ui, "pendingText", pending);
            UIFactory.SetRef(ui, "detailText", detail);
            UIFactory.SetRef(ui, "progressFill", frt);
            UIFactory.SetRef(ui, "progressBar", barBg.gameObject);
            UIFactory.SetRef(ui, "restartButton", button);
            var so = new SerializedObject(ui);
            so.FindProperty("confirmColor").colorValue = Theme.AccentFame;
            so.ApplyModifiedPropertiesWithoutUndo();
            return screen;
        }

        /// <summary>Five artist avatars along the bottom of the studio card.</summary>
        static void CreateArtistStrip(RectTransform card)
        {
            const int count = 5;
            const float size = 68f, gap = 14f;
            var slots = new Object[count];
            var letters = new Object[count];

            var strip = UIFactory.Rect("ArtistStrip", card);
            strip.anchorMin = strip.anchorMax = new Vector2(0, 0);
            strip.pivot = new Vector2(0, 0);
            strip.sizeDelta = new Vector2(count * size + (count - 1) * gap, size);
            strip.anchoredPosition = new Vector2(40, 22);

            for (int i = 0; i < count; i++)
            {
                var img = UIFactory.Image("Artist" + i, strip, new Color(1, 1, 1, 0.08f));
                img.sprite = UIFactory.CircleSprite;
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
                rt.sizeDelta = new Vector2(size, size);
                rt.anchoredPosition = new Vector2(size * 0.5f + i * (size + gap), 0);
                var t = UIFactory.Text("Letters", rt, "?", 28, new Color(1, 1, 1, 0.25f), TextAlignmentOptions.Center,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                t.fontStyle = FontStyles.Bold;
                slots[i] = img;
                letters[i] = t;
            }

            var ui = strip.gameObject.AddComponent<ArtistStripUI>();
            var so = new SerializedObject(ui);
            UIFactory.SetArray(so.FindProperty("slots"), slots);
            UIFactory.SetArray(so.FindProperty("slotLetters"), letters);
            so.FindProperty("letterColor").colorValue = Theme.Background;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static RectTransform CreateArtistsScreen(RectTransform content, ShopRowView rowPrefab)
        {
            var screen = CreateScreen("ArtistsScreen", content);
            CreateScreenTitle(screen, "@nav.career");

            // ARTISTS | ALBUMS segmented control
            var seg = UIFactory.RoundedImage("Segments", screen, new Color(1, 1, 1, 0.05f));
            seg.raycastTarget = false;
            var segRt = seg.rectTransform;
            segRt.anchorMin = new Vector2(0, 1);
            segRt.anchorMax = new Vector2(1, 1);
            segRt.pivot = new Vector2(0.5f, 1);
            segRt.offsetMin = new Vector2(40, -ScreenListTop - 96);
            segRt.offsetMax = new Vector2(-40, -ScreenListTop);
            var segButtons = new Object[2];
            var segBacks = new Object[2];
            var segLabels = new Object[2];
            string[] segNames = { "ARTISTS", "ALBUMS" };
            for (int i = 0; i < 2; i++)
            {
                var b = UIFactory.Button("Seg_" + segNames[i], segRt, Theme.AccentCyan, "@career.tab_" + segNames[i].ToLowerInvariant(), 32, Theme.Background, out var l);
                l.font = UIStyle.Heading;
                l.characterSpacing = 8;
                var brt = (RectTransform)b.transform;
                brt.anchorMin = new Vector2(i * 0.5f, 0);
                brt.anchorMax = new Vector2(i * 0.5f + 0.5f, 1);
                brt.offsetMin = new Vector2(8, 8);
                brt.offsetMax = new Vector2(-8, -8);
                UIFactory.SetClick(b, ButtonPunch.ClickStyle.Tab);
                segButtons[i] = b;
                segBacks[i] = b.targetGraphic;
                segLabels[i] = l;
            }

            const float panelTop = ScreenListTop + 110;
            var artistsPanel = UIFactory.Rect("ArtistsPanel", screen);
            UIFactory.Stretch(artistsPanel);
            var total = UIFactory.Text("TotalBonus", artistsPanel, "Artist bonus: ×1", 30, Theme.AccentPink, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(44, -panelTop - 56), new Vector2(-40, -panelTop));
            total.richText = true;
            var list = CreateScrollList(artistsPanel, "ArtistList", panelTop + 60, 30, out _);

            var albumsPanel = UIFactory.Rect("AlbumsPanel", screen);
            UIFactory.Stretch(albumsPanel);
            var summary = UIFactory.Text("AlbumSummary", albumsPanel, "", 30, Theme.AccentCyan, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(44, -panelTop - 56), new Vector2(-40, -panelTop));
            summary.richText = true;
            var albumList = CreateScrollList(albumsPanel, "AlbumList", panelTop + 60, 30, out _);
            var albumsUi = albumsPanel.gameObject.AddComponent<AlbumsPanelUI>();
            UIFactory.SetRef(albumsUi, "cardPrefab", albumCardPrefab);
            UIFactory.SetRef(albumsUi, "container", albumList);
            UIFactory.SetRef(albumsUi, "summaryText", summary);

            var tabs = segRt.gameObject.AddComponent<SegmentTabs>();
            var tso = new SerializedObject(tabs);
            UIFactory.SetArray(tso.FindProperty("buttons"), segButtons);
            UIFactory.SetArray(tso.FindProperty("panels"), new Object[] { artistsPanel.gameObject, albumsPanel.gameObject });
            UIFactory.SetArray(tso.FindProperty("backgrounds"), segBacks);
            UIFactory.SetArray(tso.FindProperty("labels"), segLabels);
            tso.ApplyModifiedPropertiesWithoutUndo();

            var ui = screen.gameObject.AddComponent<ArtistsScreenUI>();
            UIFactory.SetRef(ui, "cardPrefab", artistCardPrefab);
            UIFactory.SetRef(ui, "container", list);
            UIFactory.SetRef(ui, "totalBonusText", total);
            return screen;
        }

        public readonly struct NavTabDef
        {
            public readonly string Label, Icon;
            public readonly GameObject Screen;
            public readonly NavBadgeSource Badge;

            public NavTabDef(string label, string icon, GameObject screen, NavBadgeSource badge)
            {
                Label = label;
                Icon = icon;
                Screen = screen;
                Badge = badge;
            }
        }

        /// <summary>Full-size screen container with a CanvasGroup (used for the fade-in).</summary>
        static RectTransform CreateScreen(string name, RectTransform content)
        {
            var screen = UIFactory.Rect(name, content);
            UIFactory.Stretch(screen);
            screen.gameObject.AddComponent<CanvasGroup>();
            return screen;
        }

        static TextMeshProUGUI CreateScreenTitle(RectTransform screen, string title)
        {
            var t = UIFactory.Text("Title", screen, title, 64, Theme.TextMain, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -130), new Vector2(-40, -30));
            t.fontStyle = FontStyles.Bold;
            t.characterSpacing = 4;
            return t;
        }

        const float ScreenListTop = 150f;

        static RectTransform CreateProducersScreen(RectTransform content, ProducerRow rowPrefab, FloatingTextSpawner floating)
        {
            var screen = CreateScreen("ProducersScreen", content);
            CreateScreenTitle(screen, "@nav.producers");

            // x1 / x10 / MAX selector (top right)
            var selectorRoot = UIFactory.Rect("BuyModeSelector", screen);
            selectorRoot.anchorMin = selectorRoot.anchorMax = new Vector2(1, 1);
            selectorRoot.pivot = new Vector2(1, 1);
            selectorRoot.sizeDelta = new Vector2(3 * 130 + 2 * 12, 90);
            selectorRoot.anchoredPosition = new Vector2(-40, -36);
            var h = selectorRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;

            var one = UIFactory.Button("x1", selectorRoot, Theme.PanelLight, "x1", 36, Theme.TextMain, out _);
            var ten = UIFactory.Button("x10", selectorRoot, Theme.PanelLight, "x10", 36, Theme.TextMain, out _);
            var max = UIFactory.Button("MAX", selectorRoot, Theme.PanelLight, "@producers.buy_max", 36, Theme.TextMain, out _);
            var selector = selectorRoot.gameObject.AddComponent<BuyModeSelector>();
            UIFactory.SetRef(selector, "oneButton", one);
            UIFactory.SetRef(selector, "tenButton", ten);
            UIFactory.SetRef(selector, "maxButton", max);
            var so = new SerializedObject(selector);
            so.FindProperty("selectedColor").colorValue = Theme.AccentCyan;
            so.FindProperty("normalColor").colorValue = Theme.PanelLight;
            so.FindProperty("selectedTextColor").colorValue = Theme.Background;
            so.FindProperty("normalTextColor").colorValue = Theme.TextMain;
            so.ApplyModifiedPropertiesWithoutUndo();

            CreateProducerList(screen, rowPrefab, floating, top: ScreenListTop, bottom: 30, buyModeSelector: selector);
            return screen;
        }

        static RectTransform CreateMusicScreen(RectTransform content, ShopRowView rowPrefab, FloatingTextSpawner floating)
        {
            var screen = CreateScreen("MusicScreen", content);
            CreateScreenTitle(screen, "@nav.music");

            var list = CreateScrollList(screen, "MusicList", ScreenListTop, 30, out _);
            CreateSectionHeader(list, "@music.header_streaming");
            CreateStreamsCard(list, rowPrefab, floating);
            CreateSectionHeader(list, "@music.header_genres");
            var genres = CreateSubList(list, "Genres");
            CreateSectionHeader(list, "@music.header_studios");
            var studios = CreateSubList(list, "Studios");

            var ui = screen.gameObject.AddComponent<MusicScreenUI>();
            UIFactory.SetRef(ui, "rowPrefab", rowPrefab);
            UIFactory.SetRef(ui, "genreContainer", genres);
            UIFactory.SetRef(ui, "studioContainer", studios);
            var so = new SerializedObject(ui);
            so.FindProperty("activeColor").colorValue = Theme.AccentMoney;
            so.FindProperty("ownedColor").colorValue = Theme.TextDim;
            so.FindProperty("lockedColor").colorValue = Theme.TextDim;
            so.FindProperty("studioIconColor").colorValue = Theme.AccentCyan;
            so.FindProperty("studioButtonColor").colorValue = Theme.TextMain;
            so.ApplyModifiedPropertiesWithoutUndo();
            return screen;
        }

        /// <summary>Stream counter card with CASH OUT, followed by the platform rows.</summary>
        static void CreateStreamsCard(RectTransform list, ShopRowView rowPrefab, FloatingTextSpawner floating)
        {
            var card = UIFactory.RoundedImage("StreamsCard", list, Theme.Panel);
            var le = card.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = le.minHeight = 250;
            var rt = card.rectTransform;

            var count = UIFactory.Text("StreamsText", rt, "0", 92, Theme.AccentCyan, TextAlignmentOptions.BottomLeft,
                new Vector2(0, 0.42f), new Vector2(1, 1), new Vector2(36, 0), new Vector2(-330, -20));
            count.fontStyle = FontStyles.Bold;
            UIFactory.Text("StreamsLabel", rt, "@music.streams_label", 28, Theme.TextDim, TextAlignmentOptions.TopLeft,
                new Vector2(0, 0.3f), new Vector2(1, 0.42f), new Vector2(40, -6), new Vector2(-330, 6)).characterSpacing = 6;
            var value = UIFactory.Text("ValueText", rt, "Worth $0", 26, Theme.TextDim, TextAlignmentOptions.Left,
                new Vector2(0, 0), new Vector2(1, 0.3f), new Vector2(40, 16), new Vector2(-30, 0));
            value.richText = true;

            var cash = UIFactory.Button("CashOutButton", rt, Theme.AccentMoney, "@music.cash_out", 40, Theme.Background, out var cashLabel);
            UIFactory.SetClick(cash, ButtonPunch.ClickStyle.None); // money sound plays instead
            var crt = (RectTransform)cash.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(1, 1);
            crt.sizeDelta = new Vector2(280, 130);
            crt.anchoredPosition = new Vector2(-24, -24);

            var platforms = CreateSubList(list, "Platforms");

            var ui = card.gameObject.AddComponent<StreamsPanelUI>();
            UIFactory.SetRef(ui, "streamsText", count);
            UIFactory.SetRef(ui, "valueText", value);
            UIFactory.SetRef(ui, "cashOutButton", cash);
            UIFactory.SetRef(ui, "cashOutLabel", cashLabel);
            UIFactory.SetRef(ui, "rowPrefab", rowPrefab);
            UIFactory.SetRef(ui, "platformContainer", platforms);
            UIFactory.SetRef(ui, "floatingText", floating);
            var so = new SerializedObject(ui);
            so.FindProperty("moneyColor").colorValue = Theme.AccentMoney;
            so.FindProperty("liveColor").colorValue = Theme.AccentMoney;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateSectionHeader(RectTransform list, string text)
        {
            var t = UIFactory.Text("Header", list, text, 32, Theme.TextDim, TextAlignmentOptions.BottomLeft,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            t.characterSpacing = 4;
            t.margin = new Vector4(8, 0, 0, 10);
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = le.minHeight = 80;
        }

        /// <summary>Vertical group inside a scroll list (its height follows its rows).</summary>
        static RectTransform CreateSubList(RectTransform list, string name)
        {
            var sub = UIFactory.Rect(name, list);
            var vlg = sub.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            return sub;
        }

        static RectTransform CreateComingSoonScreen(RectTransform content, string name, string title, string body)
        {
            var screen = CreateScreen(name, content);
            CreateScreenTitle(screen, title);

            var card = UIFactory.RoundedImage("Card", screen, Theme.Panel);
            var rt = card.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(40, -ScreenListTop - 360);
            rt.offsetMax = new Vector2(-40, -ScreenListTop);

            var text = UIFactory.Text("Body", rt, body, 40, Theme.TextDim, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(40, 40), new Vector2(-40, -40));
            text.textWrappingMode = TextWrappingModes.Normal;
            return screen;
        }

        static ScreenNavigator CreateBottomNav(RectTransform safeArea, NavTabDef[] defs)
        {
            var bar = UIFactory.Image("BottomNav", safeArea, Theme.Panel);
            UIStyle.GradientBar(bar);
            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(0, Theme.BottomNavHeight);
            rt.anchoredPosition = Vector2.zero;
            ExtendBar(rt, down: true, new Color(Theme.Panel.r * 0.72f, Theme.Panel.g * 0.72f, Theme.Panel.b * 0.72f, 1f));

            var line = UIFactory.Image("TopLine", rt, new Color(1, 1, 1, 0.06f));
            line.rectTransform.anchorMin = new Vector2(0, 1);
            line.rectTransform.anchorMax = new Vector2(1, 1);
            line.rectTransform.pivot = new Vector2(0.5f, 1);
            line.rectTransform.sizeDelta = new Vector2(0, 3);
            line.raycastTarget = false;

            var row = UIFactory.Rect("Tabs", rt);
            UIFactory.Stretch(row, 8, 8, 8, 8);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;

            // Highlight pill that the navigator moves behind the selected tab.
            var indicator = UIFactory.RoundedImage("SelectionIndicator", rt, new Color(Theme.AccentCyan.r, Theme.AccentCyan.g, Theme.AccentCyan.b, 0.12f));
            UIFactory.Stretch(indicator.rectTransform, 6, 6, 6, 6);
            indicator.raycastTarget = false;
            indicator.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var navigator = bar.gameObject.AddComponent<ScreenNavigator>();
            var so = new SerializedObject(navigator);
            var tabs = so.FindProperty("tabs");
            tabs.arraySize = defs.Length;

            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                var tabImg = UIFactory.Image("Tab_" + d.Label, row, Color.clear); // invisible but clickable
                var button = tabImg.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                var nav = button.navigation;
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
                tabImg.gameObject.AddComponent<ButtonPunch>();
                UIFactory.SetClick(tabImg, ButtonPunch.ClickStyle.Tab);

                var icon = UIFactory.Image("Icon", tabImg.rectTransform, Theme.TextDim);
                icon.sprite = LoadSprite(Art.ArtGenerator.Icons + "/" + d.Icon + ".png");
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var irt = icon.rectTransform;
                irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 1);
                irt.pivot = new Vector2(0.5f, 1);
                irt.sizeDelta = new Vector2(62, 62);
                irt.anchoredPosition = new Vector2(0, -22);
                var label = UIFactory.Text("Label", tabImg.rectTransform, "@nav." + d.Label.ToLowerInvariant(), 22, Theme.TextDim, TextAlignmentOptions.Top,
                    new Vector2(0, 0), new Vector2(1, 0.38f), new Vector2(2, 6), new Vector2(-2, 0));
                label.fontStyle = FontStyles.Bold;
                label.fontSizeMin = 14;

                if (d.Badge != NavBadgeSource.None)
                {
                    var dot = UIFactory.RoundedImage("BadgeDot", tabImg.rectTransform, Theme.AccentPink);
                    dot.pixelsPerUnitMultiplier = 4;
                    dot.raycastTarget = false;
                    var drt = dot.rectTransform;
                    drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 1);
                    drt.sizeDelta = new Vector2(22, 22);
                    drt.anchoredPosition = new Vector2(38, -24);
                    dot.gameObject.SetActive(false);

                    var badge = tabImg.gameObject.AddComponent<NavBadge>();
                    UIFactory.SetRef(badge, "dot", dot.gameObject);
                    var bso = new SerializedObject(badge);
                    bso.FindProperty("source").enumValueIndex = d.Badge == NavBadgeSource.Music ? (int)NavBadge.Source.Music
                        : d.Badge == NavBadgeSource.Artists ? (int)NavBadge.Source.Artists
                        : d.Badge == NavBadgeSource.Prestige ? (int)NavBadge.Source.Prestige
                        : (int)NavBadge.Source.Producers;
                    bso.ApplyModifiedPropertiesWithoutUndo();
                }

                var tab = tabs.GetArrayElementAtIndex(i);
                tab.FindPropertyRelative("name").stringValue = d.Label;
                tab.FindPropertyRelative("button").objectReferenceValue = button;
                tab.FindPropertyRelative("screen").objectReferenceValue = d.Screen;
                var tinted = tab.FindPropertyRelative("tintedGraphics");
                UIFactory.SetArray(tinted, new Object[] { icon, label });
            }

            so.FindProperty("selectionIndicator").objectReferenceValue = indicator.rectTransform;
            so.FindProperty("selectedColor").colorValue = Theme.AccentCyan;
            so.FindProperty("normalColor").colorValue = Theme.TextDim;
            so.ApplyModifiedPropertiesWithoutUndo();
            return navigator;
        }
    }
}
