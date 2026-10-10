using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>x1 / x10 / MAX toggle for buying producer levels.</summary>
    public class BuyModeSelector : MonoBehaviour
    {
        [SerializeField] Button oneButton;
        [SerializeField] Button tenButton;
        [SerializeField] Button maxButton;
        [SerializeField] Color selectedColor = new Color(0.24f, 0.88f, 1f);
        [SerializeField] Color normalColor = new Color(0.14f, 0.11f, 0.25f);
        [SerializeField] Color selectedTextColor = new Color(0.05f, 0.04f, 0.1f);
        [SerializeField] Color normalTextColor = new Color(0.95f, 0.94f, 1f);

        public BuyMode Mode { get; private set; } = BuyMode.One;
        public event Action<BuyMode> Changed;

        void Awake()
        {
            oneButton.onClick.AddListener(() => SetMode(BuyMode.One));
            tenButton.onClick.AddListener(() => SetMode(BuyMode.Ten));
            maxButton.onClick.AddListener(() => SetMode(BuyMode.Max));
            Paint();
        }

        public void SetMode(BuyMode mode)
        {
            if (mode == Mode) return;
            Mode = mode;
            Paint();
            Changed?.Invoke(mode);
        }

        void Paint()
        {
            Paint(oneButton, Mode == BuyMode.One);
            Paint(tenButton, Mode == BuyMode.Ten);
            Paint(maxButton, Mode == BuyMode.Max);
        }

        void Paint(Button b, bool selected)
        {
            ((Image)b.targetGraphic).color = selected ? selectedColor : normalColor;
            var label = b.GetComponentInChildren<TMP_Text>();
            if (label != null) label.color = selected ? selectedTextColor : normalTextColor;
        }
    }
}
