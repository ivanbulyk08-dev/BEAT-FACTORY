using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory.EditorTools
{
    /// <summary>Boost strip under the top bar and the BOOSTS popup.</summary>
    public static partial class BeatFactorySceneBuilder
    {
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

            // BOOSTS button with a "free boost ready" dot
            var boosts = UIFactory.Button("BoostsButton", bar, Theme.PanelLight, "@boosts.button", 28, Theme.AccentCyan, out var boostsLabel);
            boostsLabel.characterSpacing = 4;
            var le = boosts.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 200;
            le.preferredHeight = 58;
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
            var so = new SerializedObject(ui);
            UIFactory.SetArray(so.FindProperty("chips"), chips);
            UIFactory.SetArray(so.FindProperty("chipTexts"), texts);
            UIFactory.SetArray(so.FindProperty("chipImages"), images);
            so.ApplyModifiedPropertiesWithoutUndo();
            return ui;
        }

        static BoostsPopupUI CreateBoostsPopup(RectTransform layer, ShopRowView rowPrefab)
        {
            var panel = CreateModal("BoostsPopup", layer, new Vector2(960, 1320), Theme.AccentCyan, out var modal);

            var title = UIFactory.Text("Title", panel, "@boosts.title", 72, Theme.TextMain, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -140), new Vector2(-40, -40));
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 6;
            var status = UIFactory.Text("FreeStatus", panel, "Next free boost in 19:59", 34, Theme.TextDim, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -200), new Vector2(-40, -140));
            status.richText = true;

            var list = UIFactory.Rect("List", panel);
            list.anchorMin = new Vector2(0, 1);
            list.anchorMax = new Vector2(1, 1);
            list.pivot = new Vector2(0.5f, 1);
            list.offsetMin = new Vector2(30, -230 - 4 * 160 - 3 * 14);
            list.offsetMax = new Vector2(-30, -230);
            var vlg = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var viral = UIFactory.Button("InstantViralButton", panel, Theme.AccentPink, "@boosts.instant_viral", 38, Theme.Background, out _);
            var vrt = (RectTransform)viral.transform;
            vrt.anchorMin = vrt.anchorMax = new Vector2(0.5f, 0);
            vrt.pivot = new Vector2(0.5f, 0);
            vrt.sizeDelta = new Vector2(760, 120);
            vrt.anchoredPosition = new Vector2(0, 210);

            var close = UIFactory.Button("CloseButton", panel, Theme.PanelLight, "@ui.close", 40, Theme.TextMain, out _);
            var crt = (RectTransform)close.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0);
            crt.pivot = new Vector2(0.5f, 0);
            crt.sizeDelta = new Vector2(420, 130);
            crt.anchoredPosition = new Vector2(0, 50);

            var ui = modal.gameObject.AddComponent<BoostsPopupUI>();
            UIFactory.SetRef(ui, "window", modal);
            UIFactory.SetRef(ui, "freeStatusText", status);
            UIFactory.SetRef(ui, "rowPrefab", rowPrefab);
            UIFactory.SetRef(ui, "container", list);
            UIFactory.SetRef(ui, "closeButton", close);
            UIFactory.SetRef(ui, "instantViralButton", viral);
            var so = new SerializedObject(ui);
            so.FindProperty("activeColor").colorValue = Theme.AccentMoney;
            so.FindProperty("dimColor").colorValue = Theme.TextDim;
            so.ApplyModifiedPropertiesWithoutUndo();
            return ui;
        }
    }
}
