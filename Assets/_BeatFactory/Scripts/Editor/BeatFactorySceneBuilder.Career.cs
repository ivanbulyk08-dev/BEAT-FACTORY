using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory.EditorTools
{
    /// <summary>Career UI: artist contracts (and helpers shared by albums / customization).</summary>
    public static partial class BeatFactorySceneBuilder
    {
        static readonly Color Gold = new Color(0.95f, 0.76f, 0.31f);

        /// <summary>Anchored-at-top rect inside a panel (x centered, y from the top).</summary>
        static RectTransform TopRect(string name, RectTransform parent, float y, Vector2 size)
        {
            var rt = UIFactory.Rect(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = size;
            rt.anchoredPosition = new Vector2(0, y);
            return rt;
        }

        static TextMeshProUGUI TopText(string name, RectTransform parent, string text, float y, float height, float size, Color color,
            TMP_FontAsset font, float spacing = 0, float side = 50)
        {
            var t = UIFactory.Text(name, parent, text, size, color, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(side, y - height), new Vector2(-side, y));
            t.font = font;
            t.characterSpacing = spacing;
            return t;
        }

        /// <summary>Two-line premium button: action label on top, price below, divider between.</summary>
        static Button PriceButton(string name, RectTransform parent, Color color, string label, string price, Vector2 size,
            out TextMeshProUGUI labelText, out TextMeshProUGUI priceText)
        {
            var button = UIFactory.Button(name, parent, color, "", 10, Theme.Background, out var unused);
            Object.DestroyImmediate(unused.gameObject);
            var rt = (RectTransform)button.transform;
            rt.sizeDelta = size;
            ((Image)button.targetGraphic).sprite = UIStyle.ButtonSprite;
            labelText = UIFactory.Text("ActionLabel", rt, label, 40, Theme.Background, TextAlignmentOptions.Bottom,
                new Vector2(0, 0.53f), Vector2.one, new Vector2(20, 0), new Vector2(-20, -16));
            labelText.font = UIStyle.Heading;
            labelText.characterSpacing = 5;
            priceText = UIFactory.Text("PriceValue", rt, price, 52, Theme.Background, TextAlignmentOptions.Top,
                Vector2.zero, new Vector2(1, 0.53f), new Vector2(20, 14), new Vector2(-20, 0));
            priceText.font = UIStyle.Display;
            UIPrefabBuilder.AddPriceDivider(rt);
            return button;
        }

        static Button CloseX(RectTransform panel)
        {
            var close = UIFactory.Button("CloseButton", panel, Theme.PanelLight, "X", 44, Theme.TextMain, out var x);
            x.font = UIStyle.Heading;
            var crt = (RectTransform)close.transform;
            ((Image)close.targetGraphic).sprite = UIFactory.CircleSprite;
            ((Image)close.targetGraphic).type = Image.Type.Simple;
            crt.anchorMin = crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(1, 1);
            crt.sizeDelta = new Vector2(96, 96);
            crt.anchoredPosition = new Vector2(-28, -28);
            return close;
        }

        static RectTransform StatRow(RectTransform card, int index, string label, out TextMeshProUGUI value, float rowH = 72)
        {
            var row = UIFactory.Rect("Row_" + label.TrimStart('@'), card);
            row.anchorMin = new Vector2(0, 1);
            row.anchorMax = new Vector2(1, 1);
            row.pivot = new Vector2(0.5f, 1);
            row.offsetMin = new Vector2(36, -24 - (index + 1) * rowH);
            row.offsetMax = new Vector2(-36, -24 - index * rowH);
            var l = UIFactory.Text("Label", row, label, 26, Theme.TextDim, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            l.font = UIStyle.Heading;
            l.characterSpacing = 6;
            value = UIFactory.Text("Value", row, "-", 36, Theme.TextMain, TextAlignmentOptions.Right, Vector2.zero, Vector2.one, new Vector2(260, 0), Vector2.zero);
            value.font = UIStyle.Heading;
            if (index > 0)
            {
                var line = UIFactory.Image("Line", row, new Color(1, 1, 1, 0.06f));
                line.raycastTarget = false;
                line.rectTransform.anchorMin = new Vector2(0, 1);
                line.rectTransform.anchorMax = new Vector2(1, 1);
                line.rectTransform.sizeDelta = new Vector2(0, 2);
            }
            return row;
        }

        static RectTransform AnchoredRect(string name, RectTransform parent, Vector2 anchor, Vector2 size)
        {
            var rt = UIFactory.Rect(name, parent);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            return rt;
        }

        static TextMeshProUGUI CenterText(string name, RectTransform parent, string text, float anchorY, float height, float size, Color color, TMP_FontAsset font, float spacing = 0)
        {
            var t = UIFactory.Text(name, parent, text, size, color, TextAlignmentOptions.Center,
                new Vector2(0, anchorY), new Vector2(1, anchorY), new Vector2(60, -height / 2), new Vector2(-60, height / 2));
            t.font = font;
            t.characterSpacing = spacing;
            return t;
        }

        /// <summary>Full-screen album release ceremony (cover, title, visualizer, rewards).</summary>
        static void CreateAlbumPresentation(RectTransform layer)
        {
            var root = UIFactory.Rect("AlbumRelease", layer);
            UIFactory.Stretch(root);
            var window = UIFactory.Rect("Window", root);
            UIFactory.Stretch(window);
            var group = window.gameObject.AddComponent<CanvasGroup>();

            var dim = UIFactory.Image("Dim", window, new Color(0, 0, 0, 0));
            UIFactory.Stretch(dim.rectTransform, -200, -200, -400, -400);
            dim.raycastTarget = true;

            var spot = UIFactory.Image("Spotlight", window, new Color(1, 0.3f, 0.8f, 0));
            spot.sprite = UIStyle.RadialSprite;
            spot.type = Image.Type.Simple;
            spot.raycastTarget = false;
            var srt = spot.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.64f);
            srt.sizeDelta = new Vector2(980, 980);

            var holder = AnchoredRect("CoverHolder", window, new Vector2(0.5f, 0.64f), new Vector2(560, 560));
            var shadow = UIFactory.Image("CoverShadow", holder, new Color(0, 0, 0, 0.6f));
            shadow.sprite = UIStyle.RadialSprite;
            shadow.type = Image.Type.Simple;
            shadow.raycastTarget = false;
            UIFactory.Stretch(shadow.rectTransform, -50, -50, -30, -70);
            var cover = UIFactory.Image("Cover", holder, Color.white);
            cover.raycastTarget = false;
            UIFactory.Stretch(cover.rectTransform);

            var header = CenterText("Header", window, "FIRST TAPE OUT NOW", 0.86f, 60, 34, Theme.AccentPink, UIStyle.Heading, 10);
            var title = CenterText("TitleBig", window, "BASEMENT TAPES", 0.43f, 140, 116, Theme.TextMain, UIStyle.Display, 4);
            var subtitle = CenterText("Subtitle", window, "", 0.385f, 50, 30, Theme.TextDim, UIStyle.Body);

            // visualizer
            var viz = AnchoredRect("Visualizer", window, new Vector2(0.5f, 0.335f), new Vector2(720, 70));
            var bars = new Object[30];
            for (int i = 0; i < bars.Length; i++)
            {
                var bar = UIFactory.RoundedImage("Bar" + i, viz, i % 3 == 0 ? Theme.AccentPink : i % 3 == 1 ? Theme.AccentCyan : Theme.AccentMoney);
                bar.pixelsPerUnitMultiplier = 8;
                bar.raycastTarget = false;
                var brt = bar.rectTransform;
                brt.anchorMin = new Vector2(i / 30f + 0.004f, 0);
                brt.anchorMax = new Vector2((i + 1) / 30f - 0.004f, 1);
                brt.pivot = new Vector2(0.5f, 0);
                brt.offsetMin = brt.offsetMax = Vector2.zero;
                bars[i] = brt;
            }

            var money = CenterText("RewardMoney", window, "+$0", 0.27f, 100, 84, Theme.AccentMoney, UIStyle.Display, 2);
            var rep = CenterText("RewardRep", window, "", 0.215f, 50, 32, Gold, UIStyle.Heading, 6);
            var poster = CenterText("RewardPoster", window, "", 0.175f, 46, 28, Theme.TextDim, UIStyle.Heading, 6);

            var cont = UIFactory.Button("ContinueButton", window, Theme.AccentPink, "@album.hang_it", 38, Theme.Background, out var cl);
            cl.font = UIStyle.Heading;
            cl.characterSpacing = 4;
            ((Image)cont.targetGraphic).sprite = UIStyle.ButtonSprite;
            UIFactory.SetClick(cont, ButtonPunch.ClickStyle.Strong);
            var crt = (RectTransform)cont.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.085f);
            crt.sizeDelta = new Vector2(760, 150);

            var burst = AnchoredRect("BurstLayer", window, new Vector2(0.5f, 0.64f), Vector2.zero);

            var comp = root.gameObject.AddComponent<AlbumReleasePresentation>();
            UIFactory.SetRef(comp, "window", window.gameObject);
            UIFactory.SetRef(comp, "group", group);
            UIFactory.SetRef(comp, "dim", dim);
            UIFactory.SetRef(comp, "spotlight", spot);
            UIFactory.SetRef(comp, "coverHolder", holder);
            UIFactory.SetRef(comp, "cover", cover);
            UIFactory.SetRef(comp, "headerText", header);
            UIFactory.SetRef(comp, "titleText", title);
            UIFactory.SetRef(comp, "subtitleText", subtitle);
            UIFactory.SetRef(comp, "continueButton", cont);
            UIFactory.SetRef(comp, "burstLayer", burst);
            UIFactory.SetRef(comp, "confettiSprite", UIFactory.RoundedSprite);
            var so = new SerializedObject(comp);
            UIFactory.SetArray(so.FindProperty("bars"), bars);
            UIFactory.SetArray(so.FindProperty("rewardLines"), new Object[] { money, rep, poster });
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>STYLE: premium fashion menu with the character in the center and big item cards.</summary>
        static void CreateStyleScreen(RectTransform layer, Button openButton)
        {
            var panel = CreateModal("StyleScreen", layer, new Vector2(980, 1780), Theme.AccentCyan, out var modal);

            var title = UIFactory.Text("TitleBig", panel, "@style.title", 100, Theme.TextMain, TextAlignmentOptions.TopLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(56, -160), new Vector2(-160, -36));
            title.font = UIStyle.Display;
            title.characterSpacing = 6;
            var sub = UIFactory.Text("Subtitle", panel, "@style.subtitle", 30, Theme.TextDim, TextAlignmentOptions.TopLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(60, -206), new Vector2(-160, -160));
            sub.font = UIStyle.Body;
            var close = CloseX(panel);

            // stage with spotlight, floor shadow and the character
            var stage = UIFactory.RoundedImage("Stage", panel, Theme.Background);
            stage.raycastTarget = false;
            var stRt = stage.rectTransform;
            stRt.anchorMin = stRt.anchorMax = new Vector2(0.5f, 1);
            stRt.pivot = new Vector2(0.5f, 1);
            stRt.sizeDelta = new Vector2(940, 760);
            stRt.anchoredPosition = new Vector2(0, -230);
            var glow = UIFactory.Image("Spotlight", stRt, new Color(Theme.AccentCyan.r, Theme.AccentCyan.g, Theme.AccentCyan.b, 0.22f));
            glow.sprite = UIStyle.RadialSprite;
            glow.type = Image.Type.Simple;
            glow.raycastTarget = false;
            var grt = glow.rectTransform;
            grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.55f);
            grt.sizeDelta = new Vector2(760, 760);
            var floor = UIFactory.Image("Floor", stRt, new Color(0, 0, 0, 0.5f));
            floor.sprite = UIFactory.CircleSprite;
            floor.raycastTarget = false;
            var frt = floor.rectTransform;
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0);
            frt.sizeDelta = new Vector2(520, 70);
            frt.anchoredPosition = new Vector2(0, 70);
            var preview = CreatePlayerRig(stRt, "Preview", new Vector2(470, 56), 2.3f, true);
            var burst = AnchoredRect("BurstLayer", stRt, new Vector2(0.5f, 0.5f), Vector2.zero);

            // category pills
            var tabsRt = UIFactory.Rect("Categories", panel);
            tabsRt.anchorMin = new Vector2(0, 1);
            tabsRt.anchorMax = new Vector2(1, 1);
            tabsRt.pivot = new Vector2(0.5f, 1);
            tabsRt.offsetMin = new Vector2(40, -1100);
            tabsRt.offsetMax = new Vector2(-40, -1010);
            var buttons = new Object[AvatarCatalog.Categories.Length];
            var backs = new Object[buttons.Length];
            var labels = new Object[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                var b = UIFactory.Button("Cat_" + AvatarCatalog.Categories[i], tabsRt, Theme.AccentCyan, "@style.cat_" + AvatarCatalog.Categories[i].ToLowerInvariant(), 24, Theme.Background, out var l);
                l.font = UIStyle.Heading;
                l.characterSpacing = 3;
                var brt = (RectTransform)b.transform;
                float w = 1f / buttons.Length;
                brt.anchorMin = new Vector2(i * w, 0);
                brt.anchorMax = new Vector2((i + 1) * w, 1);
                brt.offsetMin = new Vector2(5, 0);
                brt.offsetMax = new Vector2(-5, 0);
                UIFactory.SetClick(b, ButtonPunch.ClickStyle.Tab);
                buttons[i] = b;
                backs[i] = b.targetGraphic;
                labels[i] = l;
            }
            var tabs = tabsRt.gameObject.AddComponent<SegmentTabs>();
            var tso = new SerializedObject(tabs);
            UIFactory.SetArray(tso.FindProperty("buttons"), buttons);
            UIFactory.SetArray(tso.FindProperty("panels"), new Object[0]);
            UIFactory.SetArray(tso.FindProperty("backgrounds"), backs);
            UIFactory.SetArray(tso.FindProperty("labels"), labels);
            tso.ApplyModifiedPropertiesWithoutUndo();

            // horizontal item strip
            var area = UIFactory.Rect("Items", panel);
            area.anchorMin = new Vector2(0, 1);
            area.anchorMax = new Vector2(1, 1);
            area.pivot = new Vector2(0.5f, 1);
            area.offsetMin = new Vector2(30, -1490);
            area.offsetMax = new Vector2(-30, -1125);
            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.decelerationRate = 0.12f;
            var viewport = UIFactory.Rect("Viewport", area);
            UIFactory.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var row = UIFactory.Rect("Row", viewport);
            row.anchorMin = new Vector2(0, 0);
            row.anchorMax = new Vector2(0, 1);
            row.pivot = new Vector2(0, 0.5f);
            row.offsetMin = row.offsetMax = Vector2.zero;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 18;
            h.padding = new RectOffset(16, 16, 10, 10);
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            var fit = row.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = row;

            var hint = UIFactory.Text("Hint", panel, "", 28, Theme.TextDim, TextAlignmentOptions.Center,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(50, 60), new Vector2(-50, 130));
            hint.font = UIStyle.Body;

            var ui = modal.gameObject.AddComponent<StyleScreenUI>();
            UIFactory.SetRef(ui, "window", modal);
            UIFactory.SetRef(ui, "categories", tabs);
            UIFactory.SetRef(ui, "itemContainer", row);
            UIFactory.SetRef(ui, "itemScroll", scroll);
            UIFactory.SetRef(ui, "cardPrefab", itemCardPrefab);
            UIFactory.SetRef(ui, "library", AssetDatabase.LoadAssetAtPath<AvatarLibrary>(Art.ArtGenerator.LibraryPath));
            UIFactory.SetRef(ui, "previewRig", preview);
            UIFactory.SetRef(ui, "hintText", hint);
            UIFactory.SetRef(ui, "burstLayer", burst);
            UIFactory.SetRef(ui, "confettiSprite", UIFactory.RoundedSprite);
            UIFactory.SetRef(ui, "closeButton", close);
            if (openButton != null) UIFactory.SetRef(ui, "openButton", openButton);
        }

        /// <summary>Room preview inside a rounded, masked frame (crops the room art to its interesting part).</summary>
        static Image RoomPreview(string name, RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var frame = UIFactory.RoundedImage(name, parent, Color.white);
            frame.raycastTarget = false;
            var frt = frame.rectTransform;
            frt.anchorMin = frt.anchorMax = anchor;
            frt.pivot = new Vector2(0.5f, 1);
            frt.sizeDelta = size;
            frt.anchoredPosition = pos;
            frame.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var img = UIFactory.Image("Room", frt, Color.white);
            img.raycastTarget = false;
            var irt = img.rectTransform;
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0);
            irt.pivot = new Vector2(0.5f, 0);
            float h = size.x * Art.ArtGenerator.SceneH / Art.ArtGenerator.SceneW;
            irt.sizeDelta = new Vector2(size.x, h);
            irt.anchoredPosition = new Vector2(0, -size.x * 0.1f);
            return img;
        }

        static void CreateStudioUpgradePopup(RectTransform layer)
        {
            var panel = CreateModal("StudioUpgrade", layer, new Vector2(940, 1760), Gold, out var modal);

            var header = UIFactory.Text("Header", panel, "@studio.popup_header", 30, Theme.TextDim, TextAlignmentOptions.TopLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(50, -86), new Vector2(-160, -44));
            header.font = UIStyle.Heading;
            header.characterSpacing = 10;
            var close = CloseX(panel);

            // current room (small)
            var curPrev = RoomPreview("CurrentPreview", panel, new Vector2(0, 1), new Vector2(50 + 140, -110), new Vector2(280, 158));
            curPrev.transform.parent.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1);
            var curName = UIFactory.Text("CurrentName", panel, "BASEMENT", 44, Theme.TextMain, TextAlignmentOptions.BottomLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(360, -200), new Vector2(-50, -110));
            curName.font = UIStyle.Display;
            curName.characterSpacing = 3;
            var curStats = UIFactory.Text("CurrentStats", panel, "", 26, Theme.TextDim, TextAlignmentOptions.TopLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(360, -260), new Vector2(-50, -206));
            curStats.font = UIStyle.Heading;
            curStats.characterSpacing = 3;

            // next room (big)
            var next = UIFactory.Rect("NextGroup", panel);
            UIFactory.Stretch(next);
            var nextPrev = RoomPreview("NextPreview", next, new Vector2(0.5f, 1), new Vector2(0, -300), new Vector2(840, 470));
            var shade = UIFactory.Image("Shade", nextPrev.transform.parent, new Color(0, 0, 0, 0.6f));
            shade.raycastTarget = false;
            var shRt = shade.rectTransform;
            shRt.anchorMin = new Vector2(0, 0);
            shRt.anchorMax = new Vector2(1, 0.42f);
            shRt.offsetMin = shRt.offsetMax = Vector2.zero;
            var lvl = UIFactory.Text("NextLevel", nextPrev.transform.parent, "NEXT  ·  LEVEL 2", 26, Gold, TextAlignmentOptions.BottomLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(30, 128), new Vector2(-30, 164));
            lvl.font = UIStyle.Heading;
            lvl.characterSpacing = 8;
            var nextName = UIFactory.Text("NextName", nextPrev.transform.parent, "BEDROOM STUDIO", 84, Theme.TextMain, TextAlignmentOptions.BottomLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(28, 50), new Vector2(-28, 132));
            nextName.font = UIStyle.Display;
            nextName.characterSpacing = 3;
            var tagline = UIFactory.Text("NextTagline", nextPrev.transform.parent, "", 26, new Color(1, 1, 1, 0.8f), TextAlignmentOptions.TopLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(30, 14), new Vector2(-30, 50));
            tagline.font = UIStyle.Body;

            var income = UIFactory.Text("IncomeLine", next, "", 34, Theme.AccentMoney, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(54, -840), new Vector2(-50, -786));
            income.font = UIStyle.Heading;
            income.characterSpacing = 3;
            income.richText = true;

            var unlockCard = UIFactory.RoundedImage("UnlocksCard", next, Theme.Background);
            unlockCard.raycastTarget = false;
            var uc = unlockCard.rectTransform;
            uc.anchorMin = new Vector2(0, 1);
            uc.anchorMax = new Vector2(1, 1);
            uc.pivot = new Vector2(0.5f, 1);
            uc.offsetMin = new Vector2(50, -1250);
            uc.offsetMax = new Vector2(-50, -856);
            var ulHead = UIFactory.Text("UnlocksHeader", uc, "@studio.unlocks_header", 24, Theme.TextDim, TextAlignmentOptions.TopLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -64), new Vector2(-30, -24));
            ulHead.font = UIStyle.Heading;
            ulHead.characterSpacing = 10;
            var unlocks = UIFactory.Text("UnlocksList", uc, "", 28, Theme.TextMain, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(36, 24), new Vector2(-30, -70));
            unlocks.font = UIStyle.Body;
            unlocks.richText = true;
            unlocks.textWrappingMode = TextWrappingModes.Normal;
            unlocks.enableAutoSizing = true;
            unlocks.fontSizeMin = 20;
            unlocks.lineSpacing = 6;

            // progress toward the price
            var progress = UIFactory.Text("ProgressAmount", next, "$0 / $5M", 44, Theme.TextMain, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(50, -1340), new Vector2(-50, -1276));
            progress.font = UIStyle.Display;
            progress.characterSpacing = 2;
            var barBg = UIFactory.RoundedImage("ProgressBar", next, Theme.Background);
            barBg.pixelsPerUnitMultiplier = 3;
            barBg.raycastTarget = false;
            var brt = barBg.rectTransform;
            brt.anchorMin = new Vector2(0, 1);
            brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(0.5f, 1);
            brt.offsetMin = new Vector2(60, -1380);
            brt.offsetMax = new Vector2(-60, -1352);
            var fill = UIFactory.RoundedImage("Fill", brt, Gold);
            fill.pixelsPerUnitMultiplier = 3;
            fill.raycastTarget = false;
            var frt = fill.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(0.3f, 1);
            frt.offsetMin = frt.offsetMax = Vector2.zero;

            var button = PriceButton("UpgradeButton", next, Gold, "MOVE IN", "$5M", new Vector2(780, 190), out var bLabel, out var bPrice);
            UIFactory.SetClick(button, ButtonPunch.ClickStyle.None);
            var bt = (RectTransform)button.transform;
            bt.anchorMin = bt.anchorMax = new Vector2(0.5f, 0);
            bt.pivot = new Vector2(0.5f, 0);
            bt.anchoredPosition = new Vector2(0, 70);

            // all rooms owned
            var maxed = UIFactory.Rect("MaxedGroup", panel);
            UIFactory.Stretch(maxed);
            var maxText = UIFactory.Text("MaxText", maxed, "@studio.maxed", 72, Gold, TextAlignmentOptions.Center,
                new Vector2(0, 0.35f), new Vector2(1, 0.65f), new Vector2(40, 0), new Vector2(-40, 0));
            maxText.font = UIStyle.Display;
            maxText.textWrappingMode = TextWrappingModes.Normal;
            maxed.gameObject.SetActive(false);

            var previews = new Object[Art.ArtGenerator.EnvPaths.Length];
            for (int i = 0; i < previews.Length; i++) previews[i] = LoadSprite(Art.ArtGenerator.EnvPaths[i]);

            var comp = modal.gameObject.AddComponent<StudioUpgradePopup>();
            UIFactory.SetRef(comp, "window", modal);
            UIFactory.SetRef(comp, "currentPreview", curPrev);
            UIFactory.SetRef(comp, "currentName", curName);
            UIFactory.SetRef(comp, "currentStats", curStats);
            UIFactory.SetRef(comp, "nextPreview", nextPrev);
            UIFactory.SetRef(comp, "nextLevel", lvl);
            UIFactory.SetRef(comp, "nextName", nextName);
            UIFactory.SetRef(comp, "nextTagline", tagline);
            UIFactory.SetRef(comp, "incomeText", income);
            UIFactory.SetRef(comp, "unlocksText", unlocks);
            UIFactory.SetRef(comp, "progressText", progress);
            UIFactory.SetRef(comp, "progressFill", frt);
            UIFactory.SetRef(comp, "upgradeButton", button);
            UIFactory.SetRef(comp, "upgradeLabel", bLabel);
            UIFactory.SetRef(comp, "upgradePrice", bPrice);
            UIFactory.SetRef(comp, "closeButton", close);
            UIFactory.SetRef(comp, "nextGroup", next.gameObject);
            UIFactory.SetRef(comp, "maxedGroup", maxed.gameObject);
            var so = new SerializedObject(comp);
            UIFactory.SetArray(so.FindProperty("roomPreviews"), previews);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>"NEXT: BEDROOM STUDIO  $1.2M / $5M" + progress bar, under the studio name.</summary>
        static void CreateNextGoalHUD(RectTransform studioCard)
        {
            var bg = UIFactory.RoundedImage("NextGoal", studioCard, new Color(0.05f, 0.04f, 0.1f, 0.82f));
            var rt = bg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(470, 84);
            rt.anchoredPosition = new Vector2(18, -132);
            var button = bg.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            bg.gameObject.AddComponent<ButtonPunch>();

            var glow = UIFactory.Image("ReadyGlow", rt, new Color(Gold.r, Gold.g, Gold.b, 0.3f));
            glow.sprite = UIFactory.OutlineSprite;
            glow.type = Image.Type.Sliced;
            glow.raycastTarget = false;
            UIFactory.Stretch(glow.rectTransform);
            glow.enabled = false;

            var title = UIFactory.Text("GoalTitle", rt, "NEXT: BEDROOM STUDIO", 22, Theme.TextMain, TextAlignmentOptions.TopLeft,
                new Vector2(0, 0.4f), new Vector2(1, 1), new Vector2(20, 0), new Vector2(-16, -10));
            title.font = UIStyle.Heading;
            title.characterSpacing = 4;
            var amount = UIFactory.Text("GoalAmount", rt, "$0 / $5M", 26, Gold, TextAlignmentOptions.TopRight,
                new Vector2(0, 0.4f), new Vector2(1, 1), new Vector2(20, 0), new Vector2(-18, -8));
            amount.font = UIStyle.Display;
            var barBg = UIFactory.RoundedImage("GoalBar", rt, new Color(1, 1, 1, 0.1f));
            barBg.pixelsPerUnitMultiplier = 6;
            barBg.raycastTarget = false;
            var brt = barBg.rectTransform;
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0);
            brt.offsetMin = new Vector2(20, 16);
            brt.offsetMax = new Vector2(-20, 28);
            var fill = UIFactory.RoundedImage("Fill", brt, Theme.AccentCyan);
            fill.pixelsPerUnitMultiplier = 6;
            fill.raycastTarget = false;
            var frt = fill.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(0.2f, 1);
            frt.offsetMin = frt.offsetMax = Vector2.zero;

            var hud = bg.gameObject.AddComponent<NextGoalHUD>();
            UIFactory.SetRef(hud, "button", button);
            UIFactory.SetRef(hud, "titleText", title);
            UIFactory.SetRef(hud, "amountText", amount);
            UIFactory.SetRef(hud, "fill", frt);
            UIFactory.SetRef(hud, "fillImage", fill);
            UIFactory.SetRef(hud, "glow", glow);
        }

        static void CreateArtistContractPopup(RectTransform layer)
        {
            var panel = CreateModal("ArtistContract", layer, new Vector2(900, 1620), Theme.AccentPink, out var modal);
            var root = modal.gameObject;

            // portrait with ring + breathing glow
            var holder = TopRect("PortraitHolder", panel, -50, new Vector2(400, 400));
            var glow = UIFactory.Image("PortraitGlow", holder, new Color(1, 0.3f, 0.8f, 0.35f));
            glow.sprite = UIStyle.RadialSprite;
            glow.type = Image.Type.Simple;
            glow.raycastTarget = false;
            UIFactory.Stretch(glow.rectTransform, -90, -90, -90, -90);
            var portrait = UIFactory.Image("Portrait", holder, Color.white);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            UIFactory.Stretch(portrait.rectTransform);
            var ring = UIFactory.Image("PortraitRing", holder, Theme.AccentPink);
            ring.sprite = UIStyle.RingSprite;
            ring.raycastTarget = false;
            UIFactory.Stretch(ring.rectTransform, -14, -14, -14, -14);

            // role chip, name, style, bio
            var chip = UIFactory.RoundedImage("RoleChip", panel, new Color(1, 0.3f, 0.8f, 0.2f));
            chip.raycastTarget = false;
            var chipRt = chip.rectTransform;
            chipRt.anchorMin = chipRt.anchorMax = new Vector2(0.5f, 1);
            chipRt.pivot = new Vector2(0.5f, 1);
            chipRt.sizeDelta = new Vector2(560, 66);
            chipRt.anchoredPosition = new Vector2(0, -490);
            var role = UIFactory.Text("RoleText", chipRt, "UNDERGROUND RAPPER", 28, Theme.AccentPink, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            role.font = UIStyle.Heading;
            role.characterSpacing = 8;

            var name = TopText("NameBig", panel, "KID OBSIDIAN", -572, 130, 118, Theme.TextMain, UIStyle.Display, 4);
            // popularity pips + monthly listeners
            var pips = new Object[5];
            for (int i = 0; i < 5; i++)
            {
                var pip = UIFactory.RoundedImage("Pip" + i, panel, Theme.AccentPink);
                pip.pixelsPerUnitMultiplier = 6;
                pip.raycastTarget = false;
                var prt = pip.rectTransform;
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1);
                prt.sizeDelta = new Vector2(40, 12);
                prt.anchoredPosition = new Vector2((i - 2) * 50, -712);
                pips[i] = pip;
            }
            var style = TopText("StyleText", panel, "", -730, 44, 30, Theme.TextDim, UIStyle.Body, 2);
            var bio = TopText("BioText", panel, "", -778, 100, 30, new Color(0.85f, 0.83f, 0.95f, 0.85f), UIStyle.Body, 0, 80);
            bio.fontStyle = FontStyles.Italic;
            bio.textWrappingMode = TextWrappingModes.Normal;
            bio.enableAutoSizing = true;
            bio.fontSizeMin = 24;

            // contract terms
            var terms = UIFactory.RoundedImage("TermsCard", panel, Theme.Background);
            var trt = terms.rectTransform;
            trt.anchorMin = new Vector2(0, 1);
            trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1);
            trt.offsetMin = new Vector2(50, -895 - 412);
            trt.offsetMax = new Vector2(-50, -895);
            terms.raycastTarget = false;
            StatRow(trt, 0, "@contract.term_cost", out var cost);
            cost.font = UIStyle.Display;
            cost.color = Gold;
            cost.fontSize = cost.fontSizeMax = 44;
            StatRow(trt, 1, "@contract.term_genre", out var genre);
            StatRow(trt, 2, "@contract.term_beat", out var beat);
            beat.font = UIStyle.Body;
            beat.fontSize = beat.fontSizeMax = 28;
            StatRow(trt, 3, "@contract.term_boost", out var boost);
            boost.color = Theme.AccentMoney;
            StatRow(trt, 4, "@contract.term_requires", out var requires);

            // sign button
            var sign = PriceButton("SignButton", panel, Gold, "SIGN CONTRACT", "$0", new Vector2(800, 190), out var signLabel, out var signCost);
            UIFactory.SetClick(sign, ButtonPunch.ClickStyle.Strong);
            var srt = (RectTransform)sign.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0);
            srt.pivot = new Vector2(0.5f, 0);
            srt.anchoredPosition = new Vector2(0, 70);

            var close = CloseX(panel);

            // SIGNED stamp (over the terms card)
            var stampRt = UIFactory.Rect("Stamp", panel);
            stampRt.anchorMin = stampRt.anchorMax = new Vector2(0.5f, 1);
            stampRt.sizeDelta = new Vector2(620, 190);
            stampRt.anchoredPosition = new Vector2(0, -1100);
            stampRt.localEulerAngles = new Vector3(0, 0, -11);
            var stampGroup = stampRt.gameObject.AddComponent<CanvasGroup>();
            stampGroup.alpha = 0;
            stampGroup.blocksRaycasts = false;
            var frame = UIFactory.Image("Frame", stampRt, Theme.AccentPink);
            frame.sprite = UIFactory.OutlineSprite;
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;
            UIFactory.Stretch(frame.rectTransform);
            var stampText = UIFactory.Text("StampLabel", stampRt, "@contract.signed", 130, Theme.AccentPink, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(20, 10), new Vector2(-20, -10));
            stampText.font = UIStyle.Display;
            stampText.characterSpacing = 14;

            var flash = UIFactory.RoundedImage("FlashOverlay", panel, new Color(1, 1, 1, 0));
            flash.raycastTarget = false;
            UIFactory.Stretch(flash.rectTransform);
            var burst = UIFactory.Rect("BurstLayer", panel);
            burst.anchorMin = burst.anchorMax = new Vector2(0.5f, 0.6f);

            var comp = root.AddComponent<ArtistContractPopup>();
            UIFactory.SetRef(comp, "window", modal);
            UIFactory.SetRef(comp, "portraitHolder", holder);
            UIFactory.SetRef(comp, "portrait", portrait);
            UIFactory.SetRef(comp, "portraitRing", ring);
            UIFactory.SetRef(comp, "portraitGlow", glow);
            UIFactory.SetRef(comp, "roleChip", chip);
            UIFactory.SetRef(comp, "roleText", role);
            UIFactory.SetRef(comp, "nameText", name);
            UIFactory.SetRef(comp, "styleText", style);
            UIFactory.SetRef(comp, "bioText", bio);
            UIFactory.SetRef(comp, "costText", cost);
            UIFactory.SetRef(comp, "genreText", genre);
            UIFactory.SetRef(comp, "boostText", boost);
            UIFactory.SetRef(comp, "requiresText", requires);
            UIFactory.SetRef(comp, "beatText", beat);
            var pso = new SerializedObject(comp);
            UIFactory.SetArray(pso.FindProperty("popularityPips"), pips);
            pso.ApplyModifiedPropertiesWithoutUndo();
            UIFactory.SetRef(comp, "signButton", sign);
            UIFactory.SetRef(comp, "signLabel", signLabel);
            UIFactory.SetRef(comp, "signCost", signCost);
            UIFactory.SetRef(comp, "closeButton", close);
            UIFactory.SetRef(comp, "stamp", stampGroup);
            UIFactory.SetRef(comp, "flash", flash);
            UIFactory.SetRef(comp, "burstLayer", burst);
            UIFactory.SetRef(comp, "confettiSprite", UIFactory.RoundedSprite);
        }
    }
}



