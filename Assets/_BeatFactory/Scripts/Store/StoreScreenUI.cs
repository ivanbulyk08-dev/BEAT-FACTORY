using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// STORE tab: a grid of clothes cards (picture, name, price) for the main character in the studio rooms. BUY spends
    /// money; a bought item can be put on (WEAR) and taken off again (tap WORN). Purchases and the outfit are saved by
    /// the wardrobe. Cards are built once; their state refreshes on money / wardrobe changes.
    /// </summary>
    public class StoreScreenUI : MonoBehaviour
    {
        [SerializeField] StoreCatalog catalog;
        [SerializeField] UISkin skin;
        [SerializeField] RectTransform listArea;

        class Card
        {
            public CharacterOutfitItem item;
            public Button button;
            public Image buttonImg;
            public TextMeshProUGUI label, value, status;
        }

        readonly List<Card> cards = new List<Card>();
        GameManager gm;
        bool built, dirty;

        void OnEnable()
        {
            gm = GameManager.Instance;
            if (gm == null || catalog == null) return;
            if (!built) Build();
            gm.Economy.MoneyChanged += OnMoney;
            gm.Wardrobe.Changed += OnWardrobe;
            Refresh();
        }

        void OnDisable()
        {
            if (gm == null) return;
            gm.Economy.MoneyChanged -= OnMoney;
            gm.Wardrobe.Changed -= OnWardrobe;
        }

        void OnMoney(double _) => dirty = true;
        void OnWardrobe(string _) => dirty = true;

        void Update()
        {
            if (!dirty) return;
            dirty = false;
            Refresh();
        }

        void Build()
        {
            built = true;
            if (skin != null) UIKit.Skin = skin;
            var list = UIKit.ScrollList("StoreList", listArea, 18, new RectOffset(24, 24, 10, 220));
            RectTransform row = null;
            for (int i = 0; i < catalog.items.Length; i++)
            {
                var item = catalog.items[i];
                if (item == null) continue;
                if (cards.Count % 2 == 0)
                {
                    row = UIKit.Block("Row", list, 560);
                    var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                    h.spacing = 18;
                    h.childControlWidth = h.childControlHeight = true;
                    h.childForceExpandWidth = h.childForceExpandHeight = true;
                }
                cards.Add(CardFor(item, row));
            }
            // an odd last card keeps half width
            if (cards.Count % 2 == 1) UIKit.Rect("Spacer", row).gameObject.AddComponent<LayoutElement>();
        }

        Card CardFor(CharacterOutfitItem item, RectTransform row)
        {
            var c = new Card { item = item };
            var bg = UIKit.Card("Item_" + item.id, row, UIKit.Skin.card);
            var rt = bg.rectTransform;

            var picBg = UIKit.Rounded("PicBg", rt, UIKit.Skin.cardLight);
            UIKit.Stretch(picBg.rectTransform, 14, 14, 14, 200);
            var pic = UIKit.Image("Picture", picBg.rectTransform, Color.white, item.thumbnail);
            pic.preserveAspect = true;
            UIKit.Stretch(pic.rectTransform, 16, 16, 16, 16);

            var name = UIKit.Text("Name", rt, item.DisplayName, 30, UIKit.Skin.text, UIKit.Skin.heading, TextAlignmentOptions.Center);
            name.rectTransform.anchorMin = new Vector2(0, 0);
            name.rectTransform.anchorMax = new Vector2(1, 0);
            name.rectTransform.offsetMin = new Vector2(16, 140);
            name.rectTransform.offsetMax = new Vector2(-16, 186);

            c.status = UIKit.Text("Status", rt, "", 22, UIKit.Skin.textDim, UIKit.Skin.body, TextAlignmentOptions.Center);
            c.status.rectTransform.anchorMin = new Vector2(0, 0);
            c.status.rectTransform.anchorMax = new Vector2(1, 0);
            c.status.rectTransform.offsetMin = new Vector2(16, 108);
            c.status.rectTransform.offsetMax = new Vector2(-16, 140);

            c.button = UIKit.Button("Buy", rt, UIKit.Skin.money, Loc.T("store.buy"), 34, out c.label, "", out c.value);
            c.buttonImg = (Image)c.button.targetGraphic;
            var brt = (RectTransform)c.button.transform;
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(1, 0);
            brt.offsetMin = new Vector2(16, 14);
            brt.offsetMax = new Vector2(-16, 100);
            c.button.onClick.AddListener(() => OnTap(c));
            return c;
        }

        void Refresh()
        {
            if (gm == null) return;
            foreach (var c in cards)
            {
                bool owned = gm.Wardrobe.Owns(c.item.id);
                bool worn = owned && gm.Wardrobe.PlayerWorn(c.item.slot) == c.item.id;
                if (!owned)
                {
                    bool afford = gm.Economy.CanAfford(c.item.price);
                    c.label.text = Loc.T("store.buy");
                    c.value.gameObject.SetActive(true);
                    c.value.text = NumberFormatter.FormatMoney(c.item.price);
                    c.buttonImg.color = afford ? UIKit.Skin.money : UIKit.Skin.locked;
                    c.button.interactable = afford;
                    c.status.text = Loc.T("store.for_you");
                }
                else
                {
                    c.value.gameObject.SetActive(false);
                    c.label.text = worn ? Loc.T("store.worn") : Loc.T("store.wear");
                    c.buttonImg.color = worn ? UIKit.Skin.cyan : UIKit.Skin.pink;
                    c.button.interactable = true;
                    c.status.text = worn ? Loc.T("store.tap_to_remove") : Loc.T("store.owned");
                }
                // single label centred when there is no value line
                var lrt = c.label.rectTransform;
                lrt.anchorMin = owned ? Vector2.zero : new Vector2(0, 0.5f);
                c.label.alignment = owned ? TextAlignmentOptions.Center : TextAlignmentOptions.Bottom;
                c.label.fontSize = c.label.fontSizeMax = owned ? 34 : 34 * 0.62f;
            }
        }

        void OnTap(Card c)
        {
            if (gm == null) return;
            if (!gm.Wardrobe.Owns(c.item.id))
            {
                if (!gm.Economy.TrySpend(c.item.price)) return;
                gm.Wardrobe.Grant(c.item.id); // the wardrobe change saves the game
                gm.Wardrobe.WearPlayer(c.item.slot, c.item.id); // a new buy goes on right away
            }
            else if (gm.Wardrobe.PlayerWorn(c.item.slot) == c.item.id) gm.Wardrobe.WearPlayer(c.item.slot, null);
            else gm.Wardrobe.WearPlayer(c.item.slot, c.item.id);
            Refresh();
        }
    }
}
