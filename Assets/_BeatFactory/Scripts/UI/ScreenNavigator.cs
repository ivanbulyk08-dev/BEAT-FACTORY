using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Bottom navigation: one tab button per screen. Only the selected screen is active,
    /// so hidden screens cost nothing (their Update() doesn't run).
    /// </summary>
    public class ScreenNavigator : MonoBehaviour
    {
        [Serializable]
        public class Tab
        {
            public string name;
            public Button button;
            public GameObject screen;
            public Graphic[] tintedGraphics;
        }

        [SerializeField] Tab[] tabs;
        [SerializeField] Color selectedColor = new Color(0.24f, 0.88f, 1f);
        [SerializeField] Color normalColor = new Color(0.54f, 0.52f, 0.69f);
        [SerializeField] RectTransform selectionIndicator;
        [SerializeField] float fadeDuration = 0.15f;
        [Tooltip("The painted studio room: shown only on the first tab (HOME), turned off on the others.")]
        [SerializeField] GameObject homeBackground;

        public int Current { get; private set; } = -1;
        public event Action<int> ScreenChanged;

        CanvasGroup fadingGroup;
        float fadeTime;

        void Awake()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                tabs[i].button.onClick.AddListener(() => Show(index));
            }
            Show(0, instant: true);
        }

        public void Show(string tabName)
        {
            for (int i = 0; i < tabs.Length; i++)
                if (tabs[i].name == tabName) { Show(i); return; }
        }

        public void Show(int index) => Show(index, instant: false);

        void Show(int index, bool instant)
        {
            if (index == Current || index < 0 || index >= tabs.Length) return;
            Current = index;

            for (int i = 0; i < tabs.Length; i++)
            {
                bool selected = i == index;
                if (tabs[i].screen != null) tabs[i].screen.SetActive(selected);
                foreach (var g in tabs[i].tintedGraphics)
                    if (g != null) g.color = selected ? selectedColor : normalColor;
            }

            if (homeBackground != null && homeBackground.activeSelf != (index == 0)) homeBackground.SetActive(index == 0);

            if (selectionIndicator != null)
            {
                var target = (RectTransform)tabs[index].button.transform;
                selectionIndicator.SetParent(target, false);
                selectionIndicator.SetAsFirstSibling();
            }

            // Quick fade-in of the new screen.
            var screen = tabs[index].screen;
            fadingGroup = screen != null ? screen.GetComponent<CanvasGroup>() : null;
            if (fadingGroup != null && !instant)
            {
                fadingGroup.alpha = 0f;
                fadeTime = 0f;
                enabled = true;
            }
            else
            {
                if (fadingGroup != null) fadingGroup.alpha = 1f;
                enabled = false;
            }

            ScreenChanged?.Invoke(index);
        }

        void Update()
        {
            fadeTime += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(fadeTime / fadeDuration);
            if (fadingGroup != null) fadingGroup.alpha = p;
            if (p >= 1f) enabled = false;
        }
    }
}
