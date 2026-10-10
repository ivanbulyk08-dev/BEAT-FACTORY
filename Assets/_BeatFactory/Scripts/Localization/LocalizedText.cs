using TMPro;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Static label that shows a localization key and updates itself when the language changes.
    /// Added by the scene builder to every fixed label ("@key" texts); dynamic texts call Loc directly.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] string key;
        [SerializeField] bool upperCase;

        TMP_Text label;

        public string Key
        {
            get => key;
            set { key = value; Refresh(); }
        }

        public void Setup(string k, bool upper = false)
        {
            key = k;
            upperCase = upper;
            Refresh();
        }

        void Awake() => label = GetComponent<TMP_Text>();

        void OnEnable()
        {
            Loc.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => Loc.Changed -= Refresh;

        public void Refresh()
        {
            if (label == null) label = GetComponent<TMP_Text>();
            if (label == null || string.IsNullOrEmpty(key)) return;
            string s = Loc.T(key);
            label.text = upperCase ? Loc.Upper(s) : s;
            label.isRightToLeftText = Loc.Info.rightToLeft;
        }
    }
}
