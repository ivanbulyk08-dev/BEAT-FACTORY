using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Button in the studio card that unlocks the next genre.
    /// Shows the price, or what is still required (a better studio). Hides when all genres are unlocked.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class GenreUnlockButton : MonoBehaviour
    {
        [SerializeField] Image buttonImage;
        [SerializeField] TMP_Text labelText;
        [SerializeField] TMP_Text costText;

        GenreManager genres;
        StudioManager studios;
        EconomyManager economy;
        Button button;
        ButtonPunch punch;
        float costFontSize;

        void Start()
        {
            costFontSize = costText.fontSizeMax;
            var gm = GameManager.Instance;
            genres = gm.Genres;
            studios = gm.Studios;
            economy = gm.Economy;
            button = GetComponent<Button>();
            punch = GetComponent<ButtonPunch>();

            button.onClick.AddListener(OnClick);
            genres.CurrentChanged += OnGenreChanged;
            studios.CurrentChanged += OnStudioChanged;
            economy.MoneyChanged += OnMoneyChanged;
            Loc.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (genres != null) genres.CurrentChanged -= OnGenreChanged;
            if (studios != null) studios.CurrentChanged -= OnStudioChanged;
            if (economy != null) economy.MoneyChanged -= OnMoneyChanged;
            Loc.Changed -= Refresh;
        }

        void OnStudioChanged(StudioData _) => Refresh();

        void OnClick()
        {
            if (genres.TryUnlockNext() && punch != null) punch.Punch();
        }

        void OnGenreChanged(GenreData _) => Refresh();

        void OnMoneyChanged(double _) => button.interactable = genres.CanUnlockNext();

        string RequiredStudioName(GenreData genre)
        {
            var studio = studios.Get(genre.requiredStudioLevel);
            return studio != null ? studio.DisplayName : Loc.F("common.level", "level", genre.requiredStudioLevel + 1);
        }

        /// <summary>Call when something affecting requirements changes (e.g. studio level).</summary>
        public void Refresh()
        {
            var next = genres.Next;
            if (next == null)
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);

            labelText.text = Loc.F("genre.unlock", "genre", Loc.Upper(next.DisplayName));
            bool studioOk = genres.MeetsStudioRequirement(next);
            costText.text = studioOk
                ? NumberFormatter.FormatMoney(next.unlockCost)
                : Loc.F("common.needs", "what", RequiredStudioName(next));
            // Requirement text is longer, so show it smaller than a price.
            costText.fontSizeMax = studioOk ? costFontSize : costFontSize * 0.7f;
            buttonImage.color = next.accentColor;
            button.interactable = genres.CanUnlockNext();
        }
    }
}
