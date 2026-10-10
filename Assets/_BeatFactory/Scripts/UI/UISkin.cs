using TMPro;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// The design system for UI that is built from code at runtime (concerts, tours): fonts, surface sprites, colors.
    /// Created and filled by the scene builder (Data/UISkin.asset).
    /// </summary>
    [CreateAssetMenu(fileName = "UISkin", menuName = "Beat Factory/UI Skin")]
    public class UISkin : ScriptableObject
    {
        [Header("Fonts")]
        public TMP_FontAsset display;
        public TMP_FontAsset heading;
        public TMP_FontAsset body;

        [Header("Sprites")]
        public Sprite rounded;
        public Sprite circle;
        public Sprite outline;
        public Sprite panel;
        public Sprite button;
        public Sprite softGlow;
        public Sprite radial;
        public Sprite ring;
        public Sprite barGradient;
        public Sprite confetti;

        [Header("Colors")]
        public Color background = new Color(0.05f, 0.043f, 0.1f);
        public Color card = new Color(0.09f, 0.075f, 0.17f);
        public Color cardLight = new Color(0.137f, 0.114f, 0.25f);
        public Color text = new Color(0.95f, 0.94f, 1f);
        public Color textDim = new Color(0.54f, 0.52f, 0.69f);
        public Color money = new Color(0.3f, 1f, 0.66f);
        public Color gold = new Color(0.95f, 0.76f, 0.31f);
        public Color pink = new Color(1f, 0.24f, 0.78f);
        public Color cyan = new Color(0.24f, 0.88f, 1f);
        public Color fame = new Color(1f, 0.54f, 0.24f);
        public Color locked = new Color(0.38f, 0.36f, 0.45f);
    }
}
