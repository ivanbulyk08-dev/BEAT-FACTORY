using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// STYLE menu (premium fashion menu): the character stands in the center, categories as pills,
    /// big item cards below. Selecting an item changes the character immediately with a small animation.
    /// </summary>
    public class StyleScreenUI : MonoBehaviour
    {
        [SerializeField] ModalWindow window;
        [SerializeField] SegmentTabs categories;
        [SerializeField] RectTransform itemContainer;
        [SerializeField] ScrollRect itemScroll;
        [SerializeField] StyleItemCard cardPrefab;
        [SerializeField] AvatarLibrary library;
        [SerializeField] CharacterRig previewRig;
        [SerializeField] TMP_Text hintText;
        [SerializeField] RectTransform burstLayer;
        [SerializeField] Sprite confettiSprite;
        [SerializeField] Button closeButton;
        [SerializeField] Button openButton;

        readonly List<StyleItemCard> cards = new List<StyleItemCard>();
        AppearanceManager appearance;
        StudioManager studios;
        int category;

        void Start()
        {
            var gm = GameManager.Instance;
            appearance = gm.Appearance;
            studios = gm.Studios;
            categories.SelectionChanged += ShowCategory;
            appearance.Changed += RefreshStates;
            closeButton.onClick.AddListener(window.Hide);
            if (openButton != null) openButton.onClick.AddListener(Open);
        }

        void OnDestroy()
        {
            if (appearance != null) appearance.Changed -= RefreshStates;
        }

        public void Open()
        {
            window.Show();
            ShowCategory(categories.Selected);
        }

        void ShowCategory(int index)
        {
            category = index;
            foreach (var c in cards) Destroy(c.gameObject);
            cards.Clear();
            foreach (var item in AvatarCatalog.Items(index))
            {
                var card = Instantiate(cardPrefab, itemContainer);
                string tag = item.Slot == AvatarSlot.Top ? Loc.T("style.tag_top") : item.Slot == AvatarSlot.Pants ? Loc.T("style.tag_pants") : "";
                card.Bind(item, library.Thumb(item.Slot, item.Value), tag, OnCard);
                cards.Add(card);
            }
            if (itemScroll != null) itemScroll.horizontalNormalizedPosition = 0f;
            RefreshStates();
            hintText.text = Loc.T(index == 2 || index == 3 ? "style.hint_take_off" : "style.hint_premium");
        }

        void RefreshStates()
        {
            foreach (var c in cards)
            {
                bool unlocked = appearance.IsUnlocked(c.Item);
                string lockLabel = unlocked ? "" : Loc.Upper(studios.Get(c.Item.UnlockTier)?.DisplayName ?? Loc.T("common.better_studio"));
                c.SetState(appearance.IsEquipped(c.Item), unlocked, lockLabel);
            }
        }

        void OnCard(StyleItemCard card)
        {
            if (!appearance.IsUnlocked(card.Item))
            {
                card.Shake();
                hintText.text = Loc.F("common.unlocks_at", "studio", studios.Get(card.Item.UnlockTier)?.DisplayName ?? Loc.T("common.better_studio"));
                AudioManager.Play(SoundId.Click);
                return;
            }
            if (!appearance.Toggle(card.Item)) return;
            card.Pop();
            if (previewRig != null) previewRig.Upgrade();
            if (burstLayer != null) UIBurst.Spawn(burstLayer, Vector2.zero, 14, confettiSprite);
            AudioManager.Play(SoundId.ClickStrong);
        }
    }
}
