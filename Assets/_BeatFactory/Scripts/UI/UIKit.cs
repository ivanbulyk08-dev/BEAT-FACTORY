using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Small runtime UI toolkit (same look as the scene builder's UI): rounded cards, glossy buttons, bars, texts and
    /// scroll lists. Used by screens that are built from code (concerts, tours).
    /// </summary>
    public static class UIKit
    {
        public static UISkin Skin;

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Places a rect by anchor + pivot + position + size.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Full-width row anchored to the top of the parent: y = distance from the top, h = height, side margins.</summary>
        public static RectTransform TopRow(RectTransform rt, float y, float h, float left = 0, float right = 0)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(left, -y - h);
            rt.offsetMax = new Vector2(-right, -y);
            return rt;
        }

        public static Image Image(string name, Transform parent, Color color, Sprite sprite = null, bool sliced = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Rounded surface (shadow + subtle gradient panel sprite).</summary>
        public static Image Card(string name, Transform parent, Color color)
        {
            var img = Image(name, parent, color, Skin.panel != null ? Skin.panel : Skin.rounded, true);
            return img;
        }

        public static Image Rounded(string name, Transform parent, Color color) => Image(name, parent, color, Skin.rounded, true);

        public static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color,
            TMP_FontAsset font = null, TextAlignmentOptions align = TextAlignmentOptions.Center, float spacing = 0)
        {
            var rt = Rect(name, parent);
            Stretch(rt);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font != null ? font : Skin.body;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.characterSpacing = spacing;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = size * 0.4f;
            t.overflowMode = TextOverflowModes.Ellipsis; // shrink first, "…" only as a last resort
            t.richText = true;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Glossy button with one label (or a label + a value line when value != null).</summary>
        public static Button Button(string name, Transform parent, Color color, string label, float size, out TextMeshProUGUI labelText,
            string value, out TextMeshProUGUI valueText)
        {
            var img = Image(name, parent, color, Skin.button != null ? Skin.button : Skin.rounded, true);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.6f, 1f);
            colors.fadeDuration = 0.08f;
            b.colors = colors;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            img.gameObject.AddComponent<ButtonPunch>();

            var dark = new Color(0.05f, 0.043f, 0.1f);
            if (value == null)
            {
                labelText = Text("Label", img.rectTransform, label, size, dark, Skin.heading, TextAlignmentOptions.Center, 4);
                Stretch(labelText.rectTransform, 14, 14, 6, 6);
                valueText = null;
            }
            else
            {
                labelText = Text("Label", img.rectTransform, label, size * 0.62f, dark, Skin.heading, TextAlignmentOptions.Bottom, 4);
                labelText.rectTransform.anchorMin = new Vector2(0, 0.5f);
                labelText.rectTransform.offsetMin = new Vector2(12, 0);
                labelText.rectTransform.offsetMax = new Vector2(-12, -8);
                valueText = Text("Value", img.rectTransform, value, size, dark, Skin.display, TextAlignmentOptions.Top);
                valueText.rectTransform.anchorMax = new Vector2(1, 0.5f);
                valueText.rectTransform.offsetMin = new Vector2(12, 8);
                valueText.rectTransform.offsetMax = new Vector2(-12, 0);
            }
            return b;
        }

        public static Button Button(string name, Transform parent, Color color, string label, float size, out TextMeshProUGUI labelText) =>
            Button(name, parent, color, label, size, out labelText, null, out _);

        /// <summary>Progress bar: dark track + gradient fill (fillAmount). Returns the fill image.</summary>
        public static Image Bar(string name, Transform parent, Color color, out Image track)
        {
            track = Rounded(name, parent, new Color(1, 1, 1, 0.08f));
            var fill = Image("Fill", track.rectTransform, color, Skin.barGradient != null ? Skin.barGradient : Skin.rounded);
            Stretch(fill.rectTransform, 3, 3, 3, 3);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillAmount = 0.5f;
            return fill;
        }

        /// <summary>Soft round glow behind something (radial sprite).</summary>
        public static Image Glow(string name, Transform parent, Color color, float pad)
        {
            var g = Image(name, parent, color, Skin.radial);
            Stretch(g.rectTransform, -pad, -pad, -pad, -pad);
            return g;
        }

        /// <summary>Vertical scroll list filling the parent; children are stacked by a VerticalLayoutGroup.</summary>
        public static RectTransform ScrollList(string name, RectTransform parent, float spacing, RectOffset padding)
        {
            var view = Rect(name, parent);
            Stretch(view);
            var bg = view.gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.001f);
            view.gameObject.AddComponent<RectMask2D>();
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40;
            var content = Rect("Content", view);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = view;
            return content;
        }

        /// <summary>A block inside a scroll list with a fixed height.</summary>
        public static RectTransform Block(string name, RectTransform list, float height)
        {
            var rt = Rect(name, list);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return rt;
        }

        public static void SetHeight(RectTransform block, float height)
        {
            var le = block.GetComponent<LayoutElement>();
            if (le != null) le.preferredHeight = le.minHeight = height;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.Destroy(t.GetChild(i).gameObject);
        }

        public static string Pct(float v) => NumberFormatter.Percent(v, v < 10 ? "0.0" : "0");
    }
}
