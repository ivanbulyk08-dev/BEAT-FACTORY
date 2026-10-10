using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>CAREER > ARTISTS: the fictional scene — one premium card per artist, sorted by price.</summary>
    public class ArtistsScreenUI : MonoBehaviour
    {
        [SerializeField] ArtistCardView cardPrefab;
        [SerializeField] RectTransform container;
        [SerializeField] TMP_Text totalBonusText;

        readonly List<ArtistCardView> cards = new List<ArtistCardView>();
        ArtistManager artists;
        StudioManager studios;
        EconomyManager economy;
        bool started;

        void Start()
        {
            var gm = GameManager.Instance;
            artists = gm.Artists;
            studios = gm.Studios;
            economy = gm.Economy;

            foreach (var a in artists.Artists)
            {
                var card = Instantiate(cardPrefab, container);
                card.name = "Artist_" + a.id;
                card.Bind(a, Open);
                cards.Add(card);
            }

            artists.Changed += RefreshAll;
            artists.ArtistSigned += OnSigned;
            studios.CurrentChanged += OnStudioChanged;
            economy.MoneyChanged += OnMoneyChanged;
            gm.Concerts.Changed += OnCareersChanged;
            gm.Tours.TourStarted += _ => RefreshAll();
            gm.Tours.TourCollected += _ => RefreshAll();
            started = true;
            Loc.Changed += OnLanguageChanged;
            RefreshAll();
        }

        void OnLanguageChanged()
        {
            foreach (var c in cards) c.ApplyTexts();
            RefreshAll();
        }

        void OnEnable()
        {
            if (started) RefreshAll();
        }

        void OnDestroy()
        {
            if (artists != null)
            {
                artists.Changed -= RefreshAll;
                artists.ArtistSigned -= OnSigned;
            }
            Loc.Changed -= OnLanguageChanged;
            if (studios != null) studios.CurrentChanged -= OnStudioChanged;
            if (economy != null) economy.MoneyChanged -= OnMoneyChanged;
            if (GameManager.Instance != null) GameManager.Instance.Concerts.Changed -= OnCareersChanged;
        }

        static void Open(ArtistData a)
        {
            var gm = GameManager.Instance;
            if (gm.Artists.IsSigned(a) && ArtistCareerScreen.Instance != null) ArtistCareerScreen.Instance.Show(a);
            else if (ArtistContractPopup.Instance != null) ArtistContractPopup.Instance.Show(a);
        }

        void OnStudioChanged(StudioData _) => RefreshAll();

        float moneyTimer;
        void OnMoneyChanged(double _)
        {
            // affordability colors only; throttle (money changes every frame late game)
            if (!isActiveAndEnabled || Time.unscaledTime - moneyTimer < 0.5f) return;
            moneyTimer = Time.unscaledTime;
            RefreshAll();
        }

        float careerTimer;
        void OnCareersChanged()
        {
            if (!isActiveAndEnabled || Time.unscaledTime - careerTimer < 1f) return;
            careerTimer = Time.unscaledTime;
            RefreshAll();
        }

        void OnSigned(ArtistData a)
        {
            foreach (var c in cards) if (c.Artist == a) c.Flash();
        }

        void RefreshAll()
        {
            if (totalBonusText != null)
                totalBonusText.text = artists.SignedCount > 0
                    ? Loc.F("artists.summary", "signed", artists.SignedCount, "total", artists.Artists.Count, "mult", "<b>" + NumberFormatter.FormatMultiplier(artists.Multiplier) + "</b>")
                    : Loc.T("artists.summary_empty");

            foreach (var c in cards)
            {
                var a = c.Artist;
                bool unlocked = artists.MeetsStudioRequirement(a);
                var studio = studios.Get(a.requiredStudioLevel);
                c.SetState(artists.IsSigned(a), unlocked, economy.CanAfford(a.cost),
                    Loc.F("artists.unlocks_at", "studio", "<b>" + Loc.Upper(studio != null ? studio.DisplayName : Loc.T("common.better_studio")) + "</b>"));
            }
        }
    }
}
