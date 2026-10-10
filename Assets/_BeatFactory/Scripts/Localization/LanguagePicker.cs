using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Settings → Language: the 7 languages by their native names. Switching is instant (no restart).
    /// Development builds also get the Localization Test Mode: NEXT LANGUAGE cycles through all languages
    /// so every screen can be checked for clipping, wrapping and missing glyphs.
    /// </summary>
    public class LanguagePicker : MonoBehaviour
    {
        [SerializeField] UISkin skin;

        public static LanguagePicker Instance { get; private set; }
        public bool IsOpen => root != null && root.gameObject.activeSelf;

        RectTransform root, panel;
        readonly System.Collections.Generic.List<(Image bg, TextMeshProUGUI label, Language id)> rows =
            new System.Collections.Generic.List<(Image, TextMeshProUGUI, Language)>();

        void Awake()
        {
            Instance = this;
            UIKit.Skin = skin;
            Build();
            root.gameObject.SetActive(false);
            Loc.Changed += Refresh;
        }

        void OnDestroy() => Loc.Changed -= Refresh;

        void Build()
        {
            var s = skin;
            root = UIKit.Rect("LanguagePicker", transform);
            UIKit.Stretch(root);
            var dim = UIKit.Image("Dim", root, new Color(0.01f, 0.01f, 0.03f, 0.88f));
            UIKit.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(Hide);

            bool dev = Debug.isDebugBuild || Application.isEditor;
            int n = LanguageInfo.All.Length;
            float rowH = 118, h = 230 + n * (rowH + 14) + (dev ? 130 : 0);
            panel = UIKit.Rect("Panel", root);
            UIKit.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, h));
            var bg = UIKit.Card("Bg", panel, s.card);
            UIKit.Stretch(bg.rectTransform);

            var title = UIKit.Text("Title", panel, "", 56, s.text, s.display, TextAlignmentOptions.Center, 4);
            UIKit.TopRow(title.rectTransform, 40, 80, 40, 40);
            title.gameObject.AddComponent<LocalizedText>().Setup("settings.language");

            for (int i = 0; i < n; i++)
            {
                var info = LanguageInfo.All[i];
                var b = UIKit.Button("Lang_" + info.code, panel, s.cardLight, info.nativeName, 42, out var label);
                UIKit.TopRow((RectTransform)b.transform, 150 + i * (rowH + 14), rowH, 50, 50);
                var id = info.id;
                b.onClick.AddListener(() => Choose(id));
                rows.Add(((Image)b.targetGraphic, label, id));
            }

            if (dev)
            {
                var next = UIKit.Button("TestNext", panel, s.fame, "", 30, out var nl);
                nl.gameObject.AddComponent<LocalizedText>().Setup("settings.language_test");
                UIKit.TopRow((RectTransform)next.transform, 160 + n * (rowH + 14), 96, 50, 50);
                next.onClick.AddListener(NextLanguage);
            }
        }

        public void Show()
        {
            UIKit.Skin = skin;
            Refresh();
            root.gameObject.SetActive(true);
            transform.SetAsLastSibling();
            AudioManager.Play(SoundId.Click);
        }

        public void Hide() => root.gameObject.SetActive(false);

        void Choose(Language id)
        {
            Loc.SetLanguage(id);
            var gm = GameManager.Instance;
            if (gm != null) { gm.Settings.language = Loc.Info.code; gm.Settings.NotifyChanged(); }
            AudioManager.Play(SoundId.Tab);
            Hide();
        }

        /// <summary>Localization Test Mode: next language in the list (development builds only).</summary>
        public static void NextLanguage()
        {
            var next = (Language)(((int)Loc.Current + 1) % LanguageInfo.All.Length);
            Loc.SetLanguage(next);
            var gm = GameManager.Instance;
            if (gm != null) { gm.Settings.language = Loc.Info.code; gm.Settings.NotifyChanged(); }
            Debug.Log("[Localization Test Mode] " + Loc.Info.code + " — " + Loc.Info.nativeName);
        }

        void Refresh()
        {
            foreach (var r in rows)
            {
                bool on = r.id == Loc.Current;
                r.bg.color = on ? skin.cyan : skin.cardLight;
                r.label.color = on ? skin.background : skin.text;
            }
        }
    }
}
