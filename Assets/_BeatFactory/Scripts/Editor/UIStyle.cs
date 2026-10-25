using System.Collections.Generic;
using System.IO;
using BeatFactory.EditorTools.Art;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using static BeatFactory.EditorTools.Art.SdfPainter;

namespace BeatFactory.EditorTools
{
    /// <summary>
    /// The game's design system: typography (Anton display numbers, Oswald headings/buttons, Roboto body),
    /// surface sprites (cards with soft shadow + gradient + edge highlight, glossy buttons, gradient bars),
    /// and two passes that restyle every generated UI object consistently.
    /// Fonts: Anton / Oswald (SIL OFL), Roboto (Apache 2.0) — licenses in Assets/_BeatFactory/Fonts.
    /// </summary>
    public static class UIStyle
    {
        public const string FontFolder = BeatFactorySceneBuilder.Root + "/Fonts";
        public const string SpriteFolder = BeatFactorySceneBuilder.Root + "/Art/UI";

        public static TMP_FontAsset Display, Heading, Body;
        public static Sprite PanelSprite, ButtonSprite, BarGradientSprite, SoftGlowSprite, RingSprite, RadialSprite;
        public static Sprite BoltSprite, ShineSprite, PremiumButtonSprite;

        /// <summary>Round spotlight: alpha falls off smoothly from the center.</summary>
        static SdfPainter DrawRadial()
        {
            var p = new SdfPainter(256, 256);
            var c = new Vector2(128, 128);
            p.Fill(Circle(c, 127), q =>
            {
                float k = 1f - Mathf.Clamp01((q - c).magnitude / 127f);
                return new Color(1, 1, 1, k * k);
            });
            return p;
        }

        // ------------------------------------------------------------------ setup

        public static void Load()
        {
            Display = EnsureFont("Anton.ttf", "Anton SDF.asset");
            Heading = EnsureFont("Oswald-Bold.ttf", "Oswald SDF.asset");
            Body = EnsureFont("Roboto-Bold.ttf", "Roboto SDF.asset");
            PanelSprite = EnsureSprite("ui_panel.png", DrawPanel, new Vector4(64, 64, 64, 64));
            ButtonSprite = EnsureSprite("ui_button.png", DrawButton, new Vector4(48, 48, 48, 48));
            BarGradientSprite = EnsureSprite("ui_bar_gradient.png", DrawBarGradient, Vector4.zero);
            SoftGlowSprite = EnsureSprite("ui_soft_glow.png", DrawSoftGlow, new Vector4(60, 60, 60, 60));
            RingSprite = EnsureSprite("ui_ring.png", DrawRing, Vector4.zero);
            RadialSprite = EnsureSprite("ui_radial.png", DrawRadial, Vector4.zero);
            BoltSprite = EnsureSprite("ui_bolt.png", DrawBolt, Vector4.zero);
            ShineSprite = EnsureSprite("ui_shine.png", DrawShine, Vector4.zero);
            PremiumButtonSprite = EnsureSprite("ui_button_premium.png", DrawPremiumButton, Vector4.zero);
        }

        static TMP_FontAsset EnsureFont(string ttf, string assetName)
        {
            string path = FontFolder + "/" + assetName;
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontFolder + "/" + ttf);
            if (font == null)
            {
                Debug.LogWarning("[Beat Factory] Missing font " + ttf);
                return TMP_Settings.defaultFontAsset;
            }
            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fa.name = Path.GetFileNameWithoutExtension(assetName);
            AssetDatabase.CreateAsset(fa, path);
            fa.atlasTextures[0].name = fa.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            // Symbols the display fonts lack (► ★ × ·) fall back to the TMP default font.
            if (TMP_Settings.defaultFontAsset != null)
                fa.fallbackFontAssetTable = new List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };
            fa.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz|×·");
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        static Sprite EnsureSprite(string file, System.Func<SdfPainter> draw, Vector4 border)
        {
            string path = SpriteFolder + "/" + file;
            if (!File.Exists(path)) draw().Save(path, border);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ------------------------------------------------------------------ surface sprites (white, tinted by Image.color)

        /// <summary>Card: soft drop shadow, subtle vertical gradient, light top edge.</summary>
        static SdfPainter DrawPanel()
        {
            var p = new SdfPainter(192, 192);
            var shadow = RRect(18, 8, 156, 162, 42);
            p.Fill(q => shadow(q) - 10, q =>
            {
                float d = shadow(q);
                float a = Mathf.Clamp01(1f - (d + 10) / 22f);
                return new Color(0, 0, 0, 0.55f * a * a);
            });
            var card = RRect(16, 18, 160, 158, 40);
            p.Fill(card, VGradient(18, new Color(0.80f, 0.80f, 0.84f), 176, new Color(0.93f, 0.93f, 0.96f)));
            p.Fill(Subtract(card, RRect(18, 20, 156, 154, 38)), q => new Color(1, 1, 1, Mathf.Lerp(0.15f, 0.9f, Mathf.InverseLerp(30, 176, q.y))));
            return p;
        }

        /// <summary>Button: glossy gradient, top highlight line, darker bottom lip.</summary>
        static SdfPainter DrawButton()
        {
            var p = new SdfPainter(128, 128);
            var body = RRect(0, 0, 128, 128, 44);
            p.Fill(body, new Color(0.62f, 0.62f, 0.66f));
            p.Fill(RRect(0, 5, 128, 123, 44), VGradient(5, new Color(0.80f, 0.80f, 0.82f), 128, new Color(1f, 1f, 1f)));
            p.Fill(Subtract(RRect(3, 8, 122, 117, 41), RRect(5, 6, 118, 116, 40)), new Color(1, 1, 1, 0.9f));
            return p;
        }

        /// <summary>Vertical gradient strip for bars (top bar / navigation).</summary>
        static SdfPainter DrawBarGradient()
        {
            var p = new SdfPainter(8, 128);
            p.Fill(RRect(-2, -2, 12, 132), VGradient(0, new Color(0.72f, 0.72f, 0.76f), 128, Color.white));
            return p;
        }

        static SdfPainter DrawSoftGlow()
        {
            var p = new SdfPainter(160, 160);
            var core = RRect(40, 40, 80, 80, 30);
            p.Fill(q => core(q) - 38, q =>
            {
                float a = Mathf.Clamp01(1f - (core(q) + 2) / 40f);
                return new Color(1, 1, 1, a * a);
            });
            return p;
        }

        /// <summary>Lightning bolt (white, tinted by Image.color).</summary>
        static SdfPainter DrawBolt()
        {
            var p = new SdfPainter(128, 128);
            p.Fill(Polygon(new Vector2(70, 124), new Vector2(102, 124), new Vector2(78, 78), new Vector2(102, 78),
                new Vector2(42, 4), new Vector2(58, 62), new Vector2(30, 62)), Color.white);
            return p;
        }

        /// <summary>Soft vertical light band for shimmer sweeps.</summary>
        static SdfPainter DrawShine()
        {
            var p = new SdfPainter(64, 128);
            p.Fill(RRect(-4, -4, 72, 136), q =>
            {
                float k = (q.x - 32f) / 13f;
                return new Color(1, 1, 1, Mathf.Exp(-k * k));
            });
            return p;
        }

        /// <summary>Wide CTA (820×140 px, drawn at full size): electric blue → violet, glossy top, darker lip.</summary>
        static SdfPainter DrawPremiumButton()
        {
            var p = new SdfPainter(820, 140);
            var blue = Hex("2E7BFF");
            var violet = Hex("8A3DFF");
            p.Fill(RRect(0, 0, 820, 140, 62), Hex("3B1C8C"));
            p.Fill(RRect(0, 7, 820, 133, 62), q =>
            {
                var c = Color.Lerp(blue, violet, Mathf.Clamp01(q.x / 820f));
                return Color.Lerp(c * 0.82f, c, Mathf.InverseLerp(7, 140, q.y)) + new Color(0, 0, 0, 1);
            });
            p.Fill(Subtract(RRect(4, 11, 812, 125, 58), RRect(6, 9, 808, 124, 57)), new Color(1, 1, 1, 0.55f));
            p.Fill(RRect(30, 92, 760, 34, 17), new Color(1, 1, 1, 0.10f));
            return p;
        }

        static SdfPainter DrawRing()
        {
            var p = new SdfPainter(128, 128);
            p.Fill(Ring(new Vector2(64, 64), 58, 8), Color.white);
            return p;
        }

        // ------------------------------------------------------------------ passes

        static readonly string[] DisplayHints = { "Money", "Amount", "Cost", "Fame", "StreamsText", "Price", "Count", "Number", "Big" };

        /// <summary>
        /// Typography: big numbers → Anton, headings/labels/buttons (bold) → Oswald, everything else → Roboto.
        /// Bold flags are removed where the font itself is bold (no fake bold).
        /// </summary>
        public static void ApplyTypography(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.font != null && t.font != TMP_Settings.defaultFontAsset)
                    continue; // explicitly styled by the builder
                string n = t.gameObject.name;
                bool isLabelName = n.Contains("Label") || n.Contains("Header");
                bool display = !isLabelName && (System.Array.Exists(DisplayHints, h => n.Contains(h)) || t.fontSize >= 84);
                bool bold = (t.fontStyle & FontStyles.Bold) != 0;

                if (display)
                {
                    t.font = Display;
                    t.fontStyle &= ~FontStyles.Bold;
                    t.characterSpacing = Mathf.Max(t.characterSpacing, 1);
                }
                else if (bold || t.fontSize >= 44)
                {
                    t.font = Heading;
                    t.fontStyle &= ~FontStyles.Bold;
                    t.characterSpacing = Mathf.Max(t.characterSpacing, 2);
                }
                else
                {
                    t.font = Body;
                }
            }
        }

        /// <summary>
        /// Surfaces: rounded buttons get the glossy button sprite, large dark rounded panels get the card sprite
        /// (shadow + gradient + edge). Thin bars, pips, icons, glows and masks are left untouched.
        /// </summary>
        public static void ApplySurfaces(GameObject root, Sprite rounded)
        {
            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                if (img.sprite != rounded) continue;
                string n = img.gameObject.name;
                if (n.Contains("Glow") || n.Contains("Indicator") || n.Contains("Fill") || n.Contains("Bar") || n.Contains("Pip")
                    || n.Contains("Dot") || n.Contains("Switch") || n.Contains("Icon") || n.Contains("StudioScene") || n.Contains("Background")
                    || n.Contains("Highlight") || n.Contains("Label") || img.GetComponent<Mask>() != null || img.pixelsPerUnitMultiplier > 1.01f)
                    continue;

                var btn = img.GetComponent<Button>();
                if (btn != null && btn.transition != Selectable.Transition.None)
                {
                    img.sprite = ButtonSprite;
                    img.pixelsPerUnitMultiplier = 1f;
                }
                else
                {
                    Color.RGBToHSV(img.color, out _, out _, out float v);
                    if (v < 0.3f && img.color.a > 0.5f)
                    {
                        img.sprite = PanelSprite;
                        img.pixelsPerUnitMultiplier = 1f;
                    }
                }
            }
        }

        /// <summary>Gradient for a plain bar image (top bar / bottom nav).</summary>
        public static void GradientBar(Image img)
        {
            img.sprite = BarGradientSprite;
            img.type = Image.Type.Simple;
        }
    }
}
