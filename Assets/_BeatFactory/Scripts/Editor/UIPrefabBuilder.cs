using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory.EditorTools
{
    /// <summary>
    /// Creates UI prefabs (e.g. the producer row) in Assets/_BeatFactory/Prefabs.
    /// Prefabs are regenerated on every scene build.
    /// </summary>
    public static class UIPrefabBuilder
    {
        public const string ProducerRowPath = BeatFactorySceneBuilder.Root + "/Prefabs/ProducerRow.prefab";

        public const float ProducerRowHeight = 180f;

        public const string ShopRowPath = BeatFactorySceneBuilder.Root + "/Prefabs/ShopRow.prefab";
        public const float ShopRowHeight = 160f;

        /// <summary>Generic row: icon, title, subtitle and a buy button OR a status text.</summary>
        public static ShopRowView BuildShopRow()
        {
            var root = UIFactory.RoundedImage("ShopRow", null, Theme.Panel);
            var rt = root.rectTransform;
            rt.sizeDelta = new Vector2(1000, ShopRowHeight);
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = layout.minHeight = ShopRowHeight;
            var group = root.gameObject.AddComponent<CanvasGroup>();

            var iconBg = UIFactory.RoundedImage("Icon", rt, Color.white);
            var irt = iconBg.rectTransform;
            irt.anchorMin = irt.anchorMax = new Vector2(0, 0.5f);
            irt.sizeDelta = new Vector2(104, 104);
            irt.anchoredPosition = new Vector2(28 + 52, 0);
            iconBg.raycastTarget = false;
            var letters = UIFactory.Text("Letters", irt, "LO", 44, Theme.Background, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            letters.fontStyle = FontStyles.Bold;

            const float textLeft = 160f, textRight = 320f;
            var title = UIFactory.Text("Title", rt, "Title", 40, Theme.TextMain, TextAlignmentOptions.BottomLeft,
                new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(textLeft, 0), new Vector2(-textRight, -20));
            title.fontStyle = FontStyles.Bold;
            var subtitle = UIFactory.Text("Subtitle", rt, "Income ×1", 30, Theme.TextDim, TextAlignmentOptions.TopLeft,
                new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(textLeft, 20), new Vector2(-textRight, -6));

            var button = UIFactory.Button("Button", rt, Theme.AccentCyan, "", 26, Theme.Background, out var unused);
            UIFactory.SetClick(button, ButtonPunch.ClickStyle.None); // unlock/purchase sounds play instead
            Object.DestroyImmediate(unused.gameObject); // this button uses two lines instead
            var brt = (RectTransform)button.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(1, 0.5f);
            brt.pivot = new Vector2(1, 0.5f);
            brt.sizeDelta = new Vector2(270, 116);
            brt.anchoredPosition = new Vector2(-22, 0);
            var label = UIFactory.Text("Label", brt, "UNLOCK", 26, Theme.Background, TextAlignmentOptions.Bottom,
                new Vector2(0, 0.52f), new Vector2(1, 1), new Vector2(10, 0), new Vector2(-10, -10));
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 3;
            var cost = UIFactory.Text("Cost", brt, "$1,000", 38, Theme.Background, TextAlignmentOptions.Top,
                new Vector2(0, 0), new Vector2(1, 0.52f), new Vector2(10, 10), new Vector2(-10, 0));
            cost.fontStyle = FontStyles.Bold;

            var status = UIFactory.Text("Status", rt, "ACTIVE", 32, Theme.AccentMoney, TextAlignmentOptions.Right,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(-textRight + 20, 0), new Vector2(-30, 0));
            status.fontStyle = FontStyles.Bold;
            status.characterSpacing = 3;

            // Portrait/artwork slot inside the icon (artists, albums).
            iconBg.sprite = UIFactory.CircleSprite;
            iconBg.type = Image.Type.Simple;
            var iconImage = UIFactory.Image("Sprite", irt, Color.white);
            UIFactory.Stretch(iconImage.rectTransform, 4, 4, 4, 4);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.enabled = false;

            AddPriceDivider((RectTransform)button.transform);
            var flash = AddFlash(rt);
            root.gameObject.AddComponent<CardAppear>();

            // The whole row can be tapped (enabled only by screens that use it, e.g. "view artist").
            var rowButton = root.gameObject.AddComponent<Button>();
            rowButton.transition = Selectable.Transition.None;
            var rnav = rowButton.navigation;
            rnav.mode = Navigation.Mode.None;
            rowButton.navigation = rnav;

            var view = root.gameObject.AddComponent<ShopRowView>();
            UIFactory.SetRef(view, "iconImage", iconImage);
            UIFactory.SetRef(view, "flash", flash);
            UIFactory.SetRef(view, "rowButton", rowButton);
            UIFactory.SetRef(view, "canvasGroup", group);
            UIFactory.SetRef(view, "iconBackground", iconBg);
            UIFactory.SetRef(view, "iconLetters", letters);
            UIFactory.SetRef(view, "titleText", title);
            UIFactory.SetRef(view, "subtitleText", subtitle);
            UIFactory.SetRef(view, "button", button);
            UIFactory.SetRef(view, "buttonImage", button.targetGraphic);
            UIFactory.SetRef(view, "buttonLabel", label);
            UIFactory.SetRef(view, "buttonCost", cost);
            UIFactory.SetRef(view, "statusText", status);

            UIStyle.ApplySurfaces(root.gameObject, UIFactory.RoundedSprite);
            UIStyle.ApplyTypography(root.gameObject);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, ShopRowPath);
            Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<ShopRowView>();
        }

        public const string AlbumCardPath = BeatFactorySceneBuilder.Root + "/Prefabs/AlbumCard.prefab";
        public const string StyleItemPath = BeatFactorySceneBuilder.Root + "/Prefabs/StyleItemCard.prefab";
        public const float AlbumCardHeight = 360f;

        static TextMeshProUGUI T(string name, RectTransform parent, string text, float size, Color color, TMP_FontAsset font,
            TextAlignmentOptions align, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var t = UIFactory.Text(name, parent, text, size, color, align, aMin, aMax, oMin, oMax);
            t.font = font;
            return t;
        }

        /// <summary>Album card: cover left, title + genre, requirement checklist, progress, reward, RELEASE.</summary>
        public static AlbumCardView BuildAlbumCard()
        {
            var root = UIFactory.RoundedImage("AlbumCard", null, Theme.Panel);
            root.sprite = UIStyle.PanelSprite;
            var rt = root.rectTransform;
            rt.sizeDelta = new Vector2(1000, AlbumCardHeight);
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = le.minHeight = AlbumCardHeight;
            var group = root.gameObject.AddComponent<CanvasGroup>();
            root.gameObject.AddComponent<CardAppear>();

            // cover with a thin dark frame
            var frame = UIFactory.RoundedImage("CoverFrame", rt, new Color(0, 0, 0, 0.6f));
            frame.pixelsPerUnitMultiplier = 4;
            frame.raycastTarget = false;
            var frt = frame.rectTransform;
            frt.anchorMin = frt.anchorMax = new Vector2(0, 0.5f);
            frt.pivot = new Vector2(0, 0.5f);
            frt.sizeDelta = new Vector2(268, 268);
            frt.anchoredPosition = new Vector2(34, 4);
            var cover = UIFactory.Image("Cover", frt, Color.white);
            cover.raycastTarget = false;
            UIFactory.Stretch(cover.rectTransform, 8, 8, 8, 8);

            const float left = 330, right = 34;
            var title = T("Title", rt, "BASEMENT TAPES", 58, Theme.TextMain, UIStyle.Display, TextAlignmentOptions.TopLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(left, -100), new Vector2(-right, -30));
            title.characterSpacing = 2;
            var kind = T("Kind", rt, "FIRST TAPE · UNDERGROUND", 24, Theme.TextDim, UIStyle.Heading, TextAlignmentOptions.TopLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(left, -138), new Vector2(-right, -102));
            kind.characterSpacing = 4;
            kind.richText = true;
            var reqs = T("Requirements", rt, "", 26, Theme.TextDim, UIStyle.Body, TextAlignmentOptions.TopLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(left, -262), new Vector2(-right, -150));
            reqs.richText = true;
            reqs.textWrappingMode = TextWrappingModes.NoWrap;
            reqs.enableAutoSizing = true;
            reqs.fontSizeMin = 16;
            reqs.lineSpacing = 0;

            var reward = T("RewardLine", rt, "", 24, new Color(0.95f, 0.76f, 0.31f), UIStyle.Body, TextAlignmentOptions.BottomLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(left, 48), new Vector2(-290, 84));
            reward.richText = true;

            var barBg = UIFactory.RoundedImage("ProgressBar", rt, Theme.Background);
            barBg.pixelsPerUnitMultiplier = 4;
            barBg.raycastTarget = false;
            var brt = barBg.rectTransform;
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0);
            brt.offsetMin = new Vector2(left, 28);
            brt.offsetMax = new Vector2(-290, 42);
            var fill = UIFactory.RoundedImage("Fill", brt, Theme.AccentPink);
            fill.pixelsPerUnitMultiplier = 4;
            fill.raycastTarget = false;
            var fillRt = fill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0.4f, 1);
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

            var button = UIFactory.Button("ReleaseButton", rt, Theme.AccentPink, "", 10, Theme.Background, out var unused);
            Object.DestroyImmediate(unused.gameObject);
            ((Image)button.targetGraphic).sprite = UIStyle.ButtonSprite;
            UIFactory.SetClick(button, ButtonPunch.ClickStyle.Strong);
            var btn = (RectTransform)button.transform;
            btn.anchorMin = btn.anchorMax = new Vector2(1, 0);
            btn.pivot = new Vector2(1, 0);
            btn.sizeDelta = new Vector2(236, 112);
            btn.anchoredPosition = new Vector2(-right, 24);
            var label = T("ActionLabel", btn, "RELEASE", 30, Theme.Background, UIStyle.Heading, TextAlignmentOptions.Bottom,
                new Vector2(0, 0.53f), Vector2.one, new Vector2(10, 0), new Vector2(-10, -12));
            label.characterSpacing = 4;
            var value = T("PriceValue", btn, "40%", 40, Theme.Background, UIStyle.Display, TextAlignmentOptions.Top,
                Vector2.zero, new Vector2(1, 0.53f), new Vector2(10, 10), new Vector2(-10, 0));
            AddPriceDivider(btn);

            var badge = UIFactory.RoundedImage("ReleasedBadge", rt, new Color(1, 0.24f, 0.78f, 0.18f));
            badge.raycastTarget = false;
            var bdt = badge.rectTransform;
            bdt.anchorMin = bdt.anchorMax = new Vector2(1, 0);
            bdt.pivot = new Vector2(1, 0);
            bdt.sizeDelta = new Vector2(236, 64);
            bdt.anchoredPosition = new Vector2(-right, 30);
            var badgeText = T("BadgeLabel", bdt, "@album.released_badge", 28, Theme.AccentPink, UIStyle.Heading, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            badgeText.characterSpacing = 6;

            var flash = AddFlash(rt);

            var view = root.gameObject.AddComponent<AlbumCardView>();
            UIFactory.SetRef(view, "group", group);
            UIFactory.SetRef(view, "cover", cover);
            UIFactory.SetRef(view, "titleText", title);
            UIFactory.SetRef(view, "kindText", kind);
            UIFactory.SetRef(view, "requirementsText", reqs);
            UIFactory.SetRef(view, "progressFill", fillRt);
            UIFactory.SetRef(view, "progressImage", fill);
            UIFactory.SetRef(view, "rewardText", reward);
            UIFactory.SetRef(view, "releaseButton", button);
            UIFactory.SetRef(view, "releaseLabel", label);
            UIFactory.SetRef(view, "releaseValue", value);
            UIFactory.SetRef(view, "releasedBadge", badge.gameObject);
            UIFactory.SetRef(view, "flash", flash);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, AlbumCardPath);
            Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<AlbumCardView>();
        }

        public const string ArtistCardPath = BeatFactorySceneBuilder.Root + "/Prefabs/ArtistCard.prefab";
        public const float ArtistCardHeight = 230f;

        /// <summary>Premium artist card: portrait + genre ring, big name, genre chip, popularity pips, contract price.</summary>
        public static ArtistCardView BuildArtistCard()
        {
            var root = UIFactory.RoundedImage("ArtistCard", null, Theme.Panel);
            root.sprite = UIStyle.PanelSprite;
            var rt = root.rectTransform;
            rt.sizeDelta = new Vector2(1000, ArtistCardHeight);
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = le.minHeight = ArtistCardHeight;
            var group = root.gameObject.AddComponent<CanvasGroup>();
            root.gameObject.AddComponent<CardAppear>();
            var cardButton = root.gameObject.AddComponent<Button>();
            cardButton.transition = Selectable.Transition.None;
            var nav = cardButton.navigation;
            nav.mode = Navigation.Mode.None;
            cardButton.navigation = nav;

            // portrait with a genre ring
            var holder = UIFactory.Rect("PortraitHolder", rt);
            holder.anchorMin = holder.anchorMax = new Vector2(0, 0.5f);
            holder.sizeDelta = new Vector2(168, 168);
            holder.anchoredPosition = new Vector2(30 + 84, 2);
            var portrait = UIFactory.Image("Portrait", holder, Color.white);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            UIFactory.Stretch(portrait.rectTransform);
            var ring = UIFactory.Image("GenreRing", holder, Color.white);
            ring.sprite = UIStyle.RingSprite;
            ring.raycastTarget = false;
            UIFactory.Stretch(ring.rectTransform, -8, -8, -8, -8);

            const float left = 228, right = 280;
            var name = T("StageName", rt, "VEXXO", 64, Theme.TextMain, UIStyle.Display, TextAlignmentOptions.BottomLeft,
                new Vector2(0, 0.52f), new Vector2(1, 1), new Vector2(left, 0), new Vector2(-right, -18));
            name.characterSpacing = 3;
            name.enableAutoSizing = true;
            name.fontSizeMin = 36;

            var chip = UIFactory.RoundedImage("GenreChip", rt, new Color(1, 0.2f, 0.3f, 0.16f));
            chip.pixelsPerUnitMultiplier = 2;
            chip.raycastTarget = false;
            var crt = chip.rectTransform;
            crt.anchorMin = new Vector2(0, 0.52f);
            crt.anchorMax = new Vector2(1, 0.52f);
            crt.pivot = new Vector2(0, 1);
            crt.offsetMin = new Vector2(left - 6, -46);
            crt.offsetMax = new Vector2(-right, -6);
            var genre = T("GenreLabel", crt, "RAGE", 22, Theme.AccentPink, UIStyle.Heading, TextAlignmentOptions.Left,
                Vector2.zero, Vector2.one, new Vector2(14, 0), new Vector2(-8, 0));
            genre.characterSpacing = 6;
            genre.richText = true;

            // popularity pips + monthly listeners
            var pips = new Object[5];
            for (int i = 0; i < 5; i++)
            {
                var pip = UIFactory.RoundedImage("Pip" + i, rt, Theme.AccentPink);
                pip.pixelsPerUnitMultiplier = 6;
                pip.raycastTarget = false;
                var prt = pip.rectTransform;
                prt.anchorMin = prt.anchorMax = new Vector2(0, 0);
                prt.pivot = new Vector2(0, 0);
                prt.sizeDelta = new Vector2(22, 10);
                prt.anchoredPosition = new Vector2(left + i * 27, 34);
                pips[i] = pip;
            }
            var listeners = T("Listeners", rt, "12.4K monthly listeners", 22, Theme.TextDim, UIStyle.Body, TextAlignmentOptions.BottomLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(left + 148, 22), new Vector2(-36, 52));
            listeners.enableAutoSizing = false;

            // right side: CONTRACT button / SIGNED badge / requirement
            var button = UIFactory.Button("ContractButton", rt, new Color(0.95f, 0.76f, 0.31f), "", 10, Theme.Background, out var unused);
            Object.DestroyImmediate(unused.gameObject);
            ((Image)button.targetGraphic).sprite = UIStyle.ButtonSprite;
            UIFactory.SetClick(button, ButtonPunch.ClickStyle.Soft);
            var brt = (RectTransform)button.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(1, 0.5f);
            brt.pivot = new Vector2(1, 0.5f);
            brt.sizeDelta = new Vector2(236, 118);
            brt.anchoredPosition = new Vector2(-30, 16);
            var label = T("ActionLabel", brt, "CONTRACT", 26, Theme.Background, UIStyle.Heading, TextAlignmentOptions.Bottom,
                new Vector2(0, 0.53f), Vector2.one, new Vector2(10, 0), new Vector2(-10, -14));
            label.characterSpacing = 5;
            var price = T("PriceValue", brt, "$15K", 44, Theme.Background, UIStyle.Display, TextAlignmentOptions.Top,
                Vector2.zero, new Vector2(1, 0.53f), new Vector2(10, 12), new Vector2(-10, 0));
            AddPriceDivider(brt);

            var badge = UIFactory.RoundedImage("SignedBadge", rt, new Color(0.3f, 1f, 0.66f, 0.16f));
            badge.raycastTarget = false;
            var bdt = badge.rectTransform;
            bdt.anchorMin = bdt.anchorMax = new Vector2(1, 0.5f);
            bdt.pivot = new Vector2(1, 0.5f);
            bdt.sizeDelta = new Vector2(220, 70);
            bdt.anchoredPosition = new Vector2(-36, 0);
            var badgeText = T("BadgeLabel", bdt, "@contract.signed", 30, Theme.AccentMoney, UIStyle.Heading, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            badgeText.characterSpacing = 8;

            var locked = T("LockedText", rt, "", 24, Theme.TextDim, UIStyle.Heading, TextAlignmentOptions.Right,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(-260, 0), new Vector2(-36, 0));
            locked.richText = true;
            locked.characterSpacing = 2;
            locked.textWrappingMode = TextWrappingModes.Normal;

            var flash = AddFlash(rt);

            var view = root.gameObject.AddComponent<ArtistCardView>();
            UIFactory.SetRef(view, "group", group);
            UIFactory.SetRef(view, "cardButton", cardButton);
            UIFactory.SetRef(view, "portrait", portrait);
            UIFactory.SetRef(view, "ring", ring);
            UIFactory.SetRef(view, "nameText", name);
            UIFactory.SetRef(view, "genreChip", chip);
            UIFactory.SetRef(view, "genreText", genre);
            UIFactory.SetRef(view, "listenersText", listeners);
            UIFactory.SetRef(view, "contractButton", button);
            UIFactory.SetRef(view, "contractLabel", label);
            UIFactory.SetRef(view, "contractPrice", price);
            UIFactory.SetRef(view, "signedBadge", badge.gameObject);
            UIFactory.SetRef(view, "lockedText", locked);
            UIFactory.SetRef(view, "flash", flash);
            var so = new SerializedObject(view);
            UIFactory.SetArray(so.FindProperty("popularityPips"), pips);
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, ArtistCardPath);
            Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<ArtistCardView>();
        }

        /// <summary>STYLE menu card: thumbnail, name, tag (TOP/PANTS), equipped frame + badge, lock overlay.</summary>
        public static StyleItemCard BuildStyleItemCard()
        {
            var root = UIFactory.RoundedImage("StyleItemCard", null, Theme.PanelLight);
            root.sprite = UIStyle.PanelSprite;
            var rt = root.rectTransform;
            rt.sizeDelta = new Vector2(260, 330);
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = 260;
            le.preferredHeight = le.minHeight = 330;
            root.gameObject.AddComponent<CardAppear>();
            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            var stage = UIFactory.RoundedImage("ThumbStage", rt, new Color(0, 0, 0, 0.35f));
            stage.pixelsPerUnitMultiplier = 2;
            stage.raycastTarget = false;
            var srt = stage.rectTransform;
            srt.anchorMin = new Vector2(0, 1);
            srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(0.5f, 1);
            srt.offsetMin = new Vector2(30, -244);
            srt.offsetMax = new Vector2(-30, -30);
            var thumb = UIFactory.Image("Thumb", srt, Color.white);
            thumb.preserveAspect = true;
            thumb.raycastTarget = false;
            UIFactory.Stretch(thumb.rectTransform, 10, 10, 10, 10);

            var name = T("Name", rt, "Item", 28, Theme.TextMain, UIStyle.Heading, TextAlignmentOptions.Center,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(26, 34), new Vector2(-26, 84));
            name.characterSpacing = 1;
            var tag = T("Tag", rt, "", 18, Theme.TextDim, UIStyle.Heading, TextAlignmentOptions.Center,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(26, 16), new Vector2(-26, 40));
            tag.characterSpacing = 6;

            var frame = UIFactory.Image("SelectedFrame", rt, Theme.AccentCyan);
            frame.sprite = UIFactory.OutlineSprite;
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;
            UIFactory.Stretch(frame.rectTransform, 12, 12, 12, 16);

            var badge = UIFactory.Image("EquippedBadge", rt, Theme.AccentCyan);
            badge.sprite = UIFactory.CircleSprite;
            badge.raycastTarget = false;
            var bdt = badge.rectTransform;
            bdt.anchorMin = bdt.anchorMax = new Vector2(1, 1);
            bdt.sizeDelta = new Vector2(54, 54);
            bdt.anchoredPosition = new Vector2(-40, -40);
            var on = T("OnLabel", bdt, "ON", 22, Theme.Background, UIStyle.Heading, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var lockImg = UIFactory.RoundedImage("LockOverlay", rt, new Color(0.03f, 0.02f, 0.07f, 0.9f));
            lockImg.raycastTarget = false;
            UIFactory.Stretch(lockImg.rectTransform, 14, 14, 14, 18);
            var lockTitle = T("LockTitle", lockImg.rectTransform, "@ui.locked", 30, Theme.AccentFame, UIStyle.Heading, TextAlignmentOptions.Center,
                new Vector2(0, 0.6f), new Vector2(1, 0.78f), new Vector2(10, 0), new Vector2(-10, 0));
            lockTitle.characterSpacing = 6;
            var lockText = T("LockText", lockImg.rectTransform, "", 20, Theme.TextMain, UIStyle.Body, TextAlignmentOptions.Top,
                new Vector2(0, 0.36f), new Vector2(1, 0.58f), new Vector2(16, 0), new Vector2(-16, 0));
            lockText.textWrappingMode = TextWrappingModes.Normal;

            var card = root.gameObject.AddComponent<StyleItemCard>();
            UIFactory.SetRef(card, "button", button);
            UIFactory.SetRef(card, "thumbnail", thumb);
            UIFactory.SetRef(card, "nameText", name);
            UIFactory.SetRef(card, "tagText", tag);
            UIFactory.SetRef(card, "selectedFrame", frame);
            UIFactory.SetRef(card, "equippedBadge", badge.gameObject);
            UIFactory.SetRef(card, "lockOverlay", lockImg.gameObject);
            UIFactory.SetRef(card, "lockText", lockText);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, StyleItemPath);
            Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<StyleItemCard>();
        }

        /// <summary>Thin line separating the action ("UPGRADE") from the price on two-line buttons.</summary>
        public static void AddPriceDivider(RectTransform button)
        {
            var line = UIFactory.Image("Divider", button, new Color(0, 0, 0, 0.18f));
            line.raycastTarget = false;
            var lrt = line.rectTransform;
            lrt.anchorMin = new Vector2(0.18f, 0.53f);
            lrt.anchorMax = new Vector2(0.82f, 0.53f);
            lrt.sizeDelta = new Vector2(0, 2);
        }

        /// <summary>White overlay that glows briefly (upgrade / purchase feedback).</summary>
        public static CardFlash AddFlash(RectTransform card)
        {
            var img = UIFactory.Image("FlashHighlight", card, new Color(1, 1, 1, 0));
            img.sprite = UIFactory.RoundedSprite;
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            UIFactory.Stretch(img.rectTransform, 14, 14, 14, 18);
            return img.gameObject.AddComponent<CardFlash>();
        }

        public static ProducerRow BuildProducerRow()
        {
            var root = UIFactory.RoundedImage("ProducerRow", null, Theme.Panel);
            var rt = root.rectTransform;
            rt.sizeDelta = new Vector2(1000, ProducerRowHeight);
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = ProducerRowHeight;
            layout.minHeight = ProducerRowHeight;
            var group = root.gameObject.AddComponent<CanvasGroup>();

            // Icon (left)
            var iconBg = UIFactory.RoundedImage("Icon", rt, Color.white);
            var irt = iconBg.rectTransform;
            irt.anchorMin = irt.anchorMax = new Vector2(0, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.sizeDelta = new Vector2(120, 120);
            irt.anchoredPosition = new Vector2(30 + 60, 0);
            iconBg.raycastTarget = false;

            var iconImage = UIFactory.Image("Sprite", irt, Color.white);
            UIFactory.Stretch(iconImage.rectTransform, 12, 12, 12, 12);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.enabled = false;

            var letters = UIFactory.Text("Letters", irt, "BP", 48, Theme.Background, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            letters.fontStyle = FontStyles.Bold;

            // Texts (middle)
            const float textLeft = 180f, textRight = 330f;
            var name = UIFactory.Text("NameText", rt, "Producer", 40, Theme.TextMain, TextAlignmentOptions.BottomLeft,
                new Vector2(0, 0.55f), new Vector2(1, 1), new Vector2(textLeft, 0), new Vector2(-textRight, -22));
            name.fontStyle = FontStyles.Bold;
            var info = UIFactory.Text("InfoText", rt, "$5 every 5s", 32, Theme.TextDim, TextAlignmentOptions.TopLeft,
                new Vector2(0, 0.25f), new Vector2(1, 0.55f), new Vector2(textLeft, 0), new Vector2(-textRight, -6));
            info.richText = true;

            // Progress bar
            var barBg = UIFactory.RoundedImage("ProgressBar", rt, Theme.Background);
            var brt = barBg.rectTransform;
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0);
            brt.offsetMin = new Vector2(textLeft, 30);
            brt.offsetMax = new Vector2(-textRight, 50);
            barBg.pixelsPerUnitMultiplier = 4; // smaller corner radius for a thin bar
            barBg.raycastTarget = false;

            var fill = UIFactory.RoundedImage("Fill", brt, Theme.AccentMoney);
            var frt = fill.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(0, 1);
            frt.offsetMin = frt.offsetMax = Vector2.zero;
            fill.pixelsPerUnitMultiplier = 4;
            fill.raycastTarget = false;

            // Button (right)
            var btnImg = UIFactory.RoundedImage("LevelButton", rt, Theme.AccentMoney);
            var btnRt = btnImg.rectTransform;
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(1, 0.5f);
            btnRt.pivot = new Vector2(1, 0.5f);
            btnRt.sizeDelta = new Vector2(280, 130);
            btnRt.anchoredPosition = new Vector2(-24, 0);
            var button = btnImg.gameObject.AddComponent<Button>();
            button.targetGraphic = btnImg;
            var colors = button.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.28f, 0.26f, 0.38f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            btnImg.gameObject.AddComponent<ButtonPunch>();
            UIFactory.SetClick(button, ButtonPunch.ClickStyle.None); // hire/upgrade sounds play instead

            var label = UIFactory.Text("Label", btnRt, "HIRE", 28, Theme.Background, TextAlignmentOptions.Bottom,
                new Vector2(0, 0.55f), new Vector2(1, 1), new Vector2(10, 0), new Vector2(-10, -14));
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 4;
            var cost = UIFactory.Text("Cost", btnRt, "$15", 42, Theme.Background, TextAlignmentOptions.Top,
                new Vector2(0, 0), new Vector2(1, 0.55f), new Vector2(10, 12), new Vector2(-10, 0));
            cost.fontStyle = FontStyles.Bold;

            iconBg.sprite = UIFactory.CircleSprite;
            iconBg.type = Image.Type.Simple;
            AddPriceDivider(btnRt);
            var flash = AddFlash(rt);
            root.gameObject.AddComponent<CardAppear>();

            var row = root.gameObject.AddComponent<ProducerRow>();
            UIFactory.SetRef(row, "flash", flash);
            UIFactory.SetRef(row, "canvasGroup", group);
            UIFactory.SetRef(row, "iconBackground", iconBg);
            UIFactory.SetRef(row, "iconImage", iconImage);
            UIFactory.SetRef(row, "iconLetters", letters);
            UIFactory.SetRef(row, "nameText", name);
            UIFactory.SetRef(row, "infoText", info);
            UIFactory.SetRef(row, "progressFill", frt);
            UIFactory.SetRef(row, "button", button);
            UIFactory.SetRef(row, "buttonLabel", label);
            UIFactory.SetRef(row, "costText", cost);
            var so = new SerializedObject(row);
            so.FindProperty("levelColor").colorValue = Theme.AccentCyan;
            so.ApplyModifiedPropertiesWithoutUndo();

            UIStyle.ApplySurfaces(root.gameObject, UIFactory.RoundedSprite);
            UIStyle.ApplyTypography(root.gameObject);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, ProducerRowPath);
            Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<ProducerRow>();
        }
    }
}
