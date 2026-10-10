using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>Segmented control: one button per panel, the selected segment is highlighted (e.g. ARTISTS | ALBUMS).</summary>
    public class SegmentTabs : MonoBehaviour
    {
        [SerializeField] Button[] buttons;
        [SerializeField] GameObject[] panels;
        [SerializeField] Image[] backgrounds;
        [SerializeField] TMP_Text[] labels;
        [SerializeField] Color selectedColor = new Color(0.24f, 0.88f, 1f);
        [SerializeField] Color normalColor = new Color(1f, 1f, 1f, 0.06f);
        [SerializeField] Color selectedText = new Color(0.05f, 0.04f, 0.1f);
        [SerializeField] Color normalText = new Color(0.54f, 0.52f, 0.69f);

        public int Selected { get; private set; }
        public event Action<int> SelectionChanged;

        void Awake()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                int k = i;
                buttons[i].onClick.AddListener(() => Select(k));
            }
            Select(0);
        }

        public void Select(int index)
        {
            Selected = index;
            for (int i = 0; i < buttons.Length; i++)
            {
                bool on = i == index;
                if (panels != null && i < panels.Length && panels[i] != null) panels[i].SetActive(on);
                if (backgrounds != null && i < backgrounds.Length) backgrounds[i].color = on ? selectedColor : normalColor;
                if (labels != null && i < labels.Length) labels[i].color = on ? selectedText : normalText;
            }
            SelectionChanged?.Invoke(index);
        }
    }
}
