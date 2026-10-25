using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory.EditorTools
{
    /// <summary>Boost strip under the top bar and the BOOSTS popup.</summary>
    public static partial class BeatFactorySceneBuilder
    {
        /// <summary>Premium boost accents: violet neon, electric blue, gold.</summary>
        static readonly Color Violet = new Color(0.54f, 0.24f, 1f);
        static readonly Color ElectricBlue = new Color(0.18f, 0.48f, 1f);

        static BoostBarUI CreateBoostBar(RectTransform safeArea)
        {
            var bar = UIFactory.Rect("BoostBar", safeArea);
            bar.anchorMin = new Vector2(0, 1);
            bar.anchorMax = new Vector2(1, 1);
            bar.pivot = new Vector2(0.5f, 1);
            bar.sizeDelta = new Vector2(0, Theme.BoostBarHeight);
            bar.anchoredPosition = new Vector2(0, -Theme.TopBarHeight);

            var h = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(40, 40, 14, 6);
            h.spacing = 12;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;

            // BOOSTS button: violet neon pill with a bolt and the featured boost ("×3 CASH"), "free boost ready" dot
            var boostsRoot = UIFactory.Image("BoostsButton", bar, new Color(0, 0, 0, 0));
            var le = boostsRoot.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 250;
            le.preferredHeight = 58;
            var bGlow = UIFactory.Image("Glow", boostsRoot.transform, new Color(Violet.r, Violet.g, Violet.b, 0.55f));
            bGlow.sprite = UIStyle.SoftGlowSprite;
            bGlow.type = Image.Type.Sliced;
            bGlow.raycastTarget = false;
            UIFactory.Stretch(bGlow.rectTransform, -22, -22, -22, -22);
            var glowPulse = bGlow.gameObject.AddComponent<PulseGlow>();
            var gso = new SerializedObject(glowPulse);
            gso.FindProperty("minAlpha").floatValue = 0.25f;
            gso.FindProperty("maxAlpha").floatValue = 0.6f;
            gso.FindProperty("bpm").floatValue = 30f;
            gso.FindProperty("scaleAmount").floatValue = 0.02f;
            gso.ApplyModifiedPropertiesWithoutUndo();
            var face = UIFactory.Image("Face", boostsRoot.transform, Color.white);
            face.sprite = UIStyle.PremiumButtonSprite;
            UIFactory.Stretch(face.rectTransform);
            var boosts = boostsRoot.gameObject.AddComponent<Button>();
            boosts.targetGraphic = face;
            var bnav = boosts.navigation;
            bnav.mode = Navigation.Mode.None;
            boosts.navigation = bnav;
            boostsRoot.gameObject.AddComponent<ButtonPunch>();
            var bolt = UIFactory.Image("BoltIcon", boostsRoot.transform, Gold);
            bolt.sprite = UIStyle.BoltSprite;
            bolt.raycastTarget = false;
            var boltRt = bolt.rectTransform;
            boltRt.anchorMin = boltRt.anchorMax = new Vector2(0, 0.5f);
            boltRt.sizeDelta = new Vector2(40, 40);
            boltRt.anchoredPosition = new Vector2(34, 0);
            var boostsLabel = UIFactory.Text("Label", boostsRoot.transform, "×3 CASH", 28, Theme.TextMain, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(52, 4), new Vector2(-12, -4));
            boostsLabel.fontStyle = FontStyles.Bold;
            boostsLabel.characterSpacing = 3;
            var dot = UIFactory.Image("FreeDot", boosts.transform, Theme.AccentPink);
            dot.sprite = UIFactory.CircleSprite;
            dot.raycastTarget = false;
            var drt = dot.rectTransform;
            drt.anchorMin = drt.anchorMax = new Vector2(1, 1);
            drt.sizeDelta = new Vector2(24, 24);
            drt.anchoredPosition = new Vector2(-6, -6);
            dot.gameObject.SetActive(false);

            // GOALS button (missions + challenges) with a "rewards to claim" badge
            var goals = UIFactory.Button("GoalsButton", bar, Theme.PanelLight, "@goals.button", 28, Gold, out var goalsLabel);
            goalsLabel.characterSpacing = 4;
            var gle = goals.gameObject.AddComponent<LayoutElement>();
            gle.preferredWidth = 180;
            gle.preferredHeight = 58;
            var badge = UIFactory.Image("Badge", goals.transform, Theme.AccentPink);
            badge.sprite = UIFactory.CircleSprite;
            badge.raycastTarget = false;
            var brt = badge.rectTransform;
            brt.anchorMin = brt.anchorMax = new Vector2(1, 1);
            brt.sizeDelta = new Vector2(38, 38);
            brt.anchoredPosition = new Vector2(-4, -4);
            var badgeText = UIFactory.Text("Count", badge.transform, "1", 22, Theme.TextMain, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            badgeText.raycastTarget = false;
            badge.gameObject.SetActive(false);
            var goalsUI = goals.gameObject.AddComponent<GoalsButton>();
            UIFactory.SetRef(goalsUI, "button", goals);
            UIFactory.SetRef(goalsUI, "badge", badge.gameObject);
            UIFactory.SetRef(goalsUI, "badgeText", badgeText);

            // One chip per boost type (shown only while active)
            const int chipCount = 4;
            var chips = new Object[chipCount];
            var texts = new Object[chipCount];
            var images = new Object[chipCount];
            for (int i = 0; i < chipCount; i++)
            {
                var chip = UIFactory.Button("Chip" + i, bar, Theme.AccentMoney, "2X $  4:59", 24, Theme.Background, out var chipText);
                var cle = chip.gameObject.AddComponent<LayoutElement>();
                cle.preferredWidth = 172;
                cle.preferredHeight = 58;
                chipText.margin = Vector4.zero;
                ((RectTransform)chipText.transform).offsetMin = new Vector2(6, 4);
                ((RectTransform)chipText.transform).offsetMax = new Vector2(-6, -4);
                chip.gameObject.SetActive(false);
                chips[i] = chip.gameObject;
                texts[i] = chipText;
                images[i] = chip.targetGraphic;
            }

            var ui = bar.gameObject.AddComponent<BoostBarUI>();
            UIFactory.SetRef(ui, "boostsButton", boosts);
            UIFactory.SetRef(ui, "freeDot", dot.gameObject);
            UIFactory.SetRef(ui, "boostsLabel", boostsLabel);
            var so = new SerializedObject(ui);
            UIFactory.SetArray(so.FindProperty("chips"), chips);
            UIFactory.SetArray(so.FindProperty("chipTexts"), texts);
            UIFactory.SetArray(so.FindProperty("chipImages"), images);
            so.ApplyModifiedPropertiesWithoutUndo();
            return ui;
        }

        static BoostsPopupUI CreateBoostsPopup(RectTransform layer, ShopRowView rowPrefab)
        {
            var panel = CreateModal("BoostsPopup", layer, new Vector2(960, 1660), Violet, out var modal);

            var title = UIFactory.Text("Title", panel, "@boosts.title", 64, Theme.TextMain, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -128), new Vector2(-40, -36));
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 6;
            var status = UIFactory.Text("FreeStatus", panel, "Next free boost in 19:59", 32, Theme.TextDim, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -184), new Vector2(-40, -128));
            status.richText = true;

            var card = CreateBoostOfferCard(panel);

            const float listTop = 668, row = 160, gap = 12;
            var list = UIFactory.Rect("List", panel);
            list.anchorMin = new Vector2(0, 1);
            list.anchorMax = new Vector2(1, 1);
            list.pivot = new Vector2(0.5f, 1);
            list.offsetMin = new Vector2(30, -listTop - 4 * row - 3 * gap);
            list.offsetMax = new Vector2(-30, -listTop);
            var vlg = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = gap;
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var viral = UIFactory.Button("InstantViralButton", panel, Theme.AccentPink, "@boosts.instant_viral", 36, Theme.Background, out _);
            var vrt = (RectTransform)viral.transform;
            vrt.anchorMin = vrt.anchorMax = new Vector2(0.5f, 0);
            vrt.pivot = new Vector2(0.5f, 0);
            vrt.sizeDelta = new Vector2(760, 110);
            vrt.anchoredPosition = new Vector2(0, 188);

            var close = UIFactory.Button("CloseButton", panel, Theme.PanelLight, "@ui.close", 40, Theme.TextMain, out _);
            var crt = (RectTransform)close.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0);
            crt.pivot = new Vector2(0.5f, 0);
            crt.sizeDelta = new Vector2(420, 120);
            crt.anchoredPosition = new Vector2(0, 44);

            var ui = modal.gameObject.AddComponent<BoostsPopupUI>();
            UIFactory.SetRef(ui, "window", modal);
            UIFactory.SetRef(ui, "freeStatusText", status);
            UIFactory.SetRef(ui, "rowPrefab", rowPrefab);
            UIFactory.SetRef(ui, "container", list);
            UIFactory.SetRef(ui, "closeButton", close);
            UIFactory.SetRef(ui, "instantViralButton", viral);
            UIFactory.SetRef(ui, "offerCard", card);
            var so = new SerializedObject(ui);
            so.FindProperty("activeColor").colorValue = Theme.AccentMoney;
            so.FindProperty("dimColor").colorValue = Theme.TextDim;
            so.ApplyModifiedPropertiesWithoutUndo();
            return ui;
        }

        /// <summary>
        /// Featured boost card: violet neon card, gold bolt badge, "×3 CASH", the real boost description and duration,
        /// live "income now → with boost", a blue → violet CTA with a shimmer, and an active state (timer + progress bar).
        /// </summary>
        static BoostOfferCard CreateBoostOfferCard(RectTransform panel)
        {
            var card = UIFactory.Rect("BoostOfferCard", panel);
            card.anchorMin = new Vector2(0, 1);
            card.anchorMax = new Vector2(1, 1);
            card.pivot = new Vector2(0.5f, 1);
            card.offsetMin = new Vector2(40, -644);
            card.offsetMax = new Vector2(-40, -204);

            var glow = UIFactory.Image("Glow", card, new Color(Violet.r, Violet.g, Violet.b, 0.5f));
            glow.sprite = UIStyle.SoftGlowSprite;
            glow.type = Image.Type.Sliced;
            glow.raycastTarget = false;
            UIFactory.Stretch(glow.rectTransform, -44, -44, -44, -44);
            var back = UIFactory.RoundedImage("CardBack", card, new Color(0.11f, 0.06f, 0.21f));
            UIFactory.Stretch(back.rectTransform);
            var rim = UIFactory.Image("RimGlow", card, new Color(Violet.r, Violet.g, Violet.b, 0.75f));
            rim.sprite = UIFactory.OutlineSprite;
            rim.type = Image.Type.Sliced;
            rim.raycastTarget = false;
            UIFactory.Stretch(rim.rectTransform);

            // gold bolt badge
            var badge = UIFactory.Image("BoltBadgeIcon", card, new Color(0.17f, 0.08f, 0.35f));
            badge.sprite = UIFactory.CircleSprite;
            badge.raycastTarget = false;
            var brt = badge.rectTransform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 1);
            brt.sizeDelta = new Vector2(132, 132);
            brt.anchoredPosition = new Vector2(98, -94);
            var ring = UIFactory.Image("RingIcon", brt, Gold);
            ring.sprite = UIStyle.RingSprite;
            ring.raycastTarget = false;
            UIFactory.Stretch(ring.rectTransform);
            var bolt = UIFactory.Image("BoltIcon", brt, Gold);
            bolt.sprite = UIStyle.BoltSprite;
            bolt.raycastTarget = false;
            UIFactory.Stretch(bolt.rectTransform, 26, 26, 22, 30);

            var titleText = UIFactory.Text("OfferTitleBig", card, "×3 CASH", 80, Theme.TextMain, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(190, -116), new Vector2(-30, -22));
            titleText.richText = true;
            var detail = UIFactory.Text("OfferDetail", card, "Producers earn ×3  ·  10m", 32, Theme.TextDim, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(190, -162), new Vector2(-30, -116));
            var preview = UIFactory.Text("OfferPreviewAmount", card, "$1.2K/min  →  $3.6K/min", 48, Theme.TextMain, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -244), new Vector2(-30, -176));
            preview.richText = true;

            // CTA: blue → violet, shimmer sweep clipped to the rounded shape
            var cta = UIFactory.Button("OfferCta", card, Color.white, "ACTIVATE ×3 BOOST", 46, Theme.TextMain, out var ctaLabel);
            var ctaImg = (Image)cta.targetGraphic;
            ctaImg.sprite = UIStyle.PremiumButtonSprite;
            ctaImg.type = Image.Type.Simple;
            var ctaRt = (RectTransform)cta.transform;
            ctaRt.anchorMin = ctaRt.anchorMax = new Vector2(0.5f, 0);
            ctaRt.pivot = new Vector2(0.5f, 0);
            ctaRt.sizeDelta = new Vector2(780, 134);
            ctaRt.anchoredPosition = new Vector2(0, 62);
            cta.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            UIFactory.SetClick(cta, ButtonPunch.ClickStyle.Strong);
            ctaLabel.characterSpacing = 3;
            var shine = UIFactory.Image("ShineHighlight", ctaRt, new Color(1, 1, 1, 0.32f));
            shine.sprite = UIStyle.ShineSprite;
            shine.raycastTarget = false;
            var srt = shine.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0, 0.5f);
            srt.sizeDelta = new Vector2(110, 240);
            srt.anchoredPosition = new Vector2(-110, 0);
            srt.localRotation = Quaternion.Euler(0, 0, -18);
            ctaLabel.transform.SetAsLastSibling();

            // active state: same place as the CTA
            var active = UIFactory.RoundedImage("ActiveBack", card, new Color(0.05f, 0.16f, 0.12f));
            var art = active.rectTransform;
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 0);
            art.pivot = new Vector2(0.5f, 0);
            art.sizeDelta = new Vector2(780, 134);
            art.anchoredPosition = new Vector2(0, 62);
            var activeText = UIFactory.Text("ActiveTimer", art, "BOOST ACTIVE  9:59", 44, Theme.AccentMoney, TextAlignmentOptions.Center,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 44), new Vector2(-20, -10));
            activeText.fontStyle = FontStyles.Bold;
            var track = UIFactory.RoundedImage("ActiveBarTrack", art, new Color(1, 1, 1, 0.08f));
            var trt = track.rectTransform;
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(1, 0);
            trt.offsetMin = new Vector2(40, 24);
            trt.offsetMax = new Vector2(-40, 44);
            var fill = UIFactory.Image("ActiveFill", trt, Theme.AccentMoney);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0.7f;
            fill.raycastTarget = false;
            UIFactory.Stretch(fill.rectTransform);
            active.gameObject.SetActive(false);

            var note = UIFactory.Text("OfferNote", card, "Watch one optional ad to activate", 28, Theme.TextDim, TextAlignmentOptions.Center,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(30, 10), new Vector2(-30, 56));
            note.richText = true;

            var flash = UIFactory.RoundedImage("FlashHighlight", card, new Color(1f, 0.92f, 0.7f, 0.45f));
            flash.raycastTarget = false;
            UIFactory.Stretch(flash.rectTransform);

            var ui = card.gameObject.AddComponent<BoostOfferCard>();
            UIFactory.SetRef(ui, "titleText", titleText);
            UIFactory.SetRef(ui, "detailText", detail);
            UIFactory.SetRef(ui, "previewText", preview);
            UIFactory.SetRef(ui, "ctaButton", cta);
            UIFactory.SetRef(ui, "ctaText", ctaLabel);
            UIFactory.SetRef(ui, "noteText", note);
            UIFactory.SetRef(ui, "activePanel", active.gameObject);
            UIFactory.SetRef(ui, "activeTimerText", activeText);
            UIFactory.SetRef(ui, "activeFill", fill);
            UIFactory.SetRef(ui, "glow", glow);
            UIFactory.SetRef(ui, "shine", srt);
            UIFactory.SetRef(ui, "flash", flash);
            UIFactory.SetRef(ui, "icon", brt);
            return ui;
        }
    }
}
