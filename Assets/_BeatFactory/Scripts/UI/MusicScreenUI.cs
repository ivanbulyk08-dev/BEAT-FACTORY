using System.Collections.Generic;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// MUSIC tab: full list of genres and studios with their bonuses, status and buy buttons.
    /// </summary>
    public class MusicScreenUI : MonoBehaviour
    {
        [SerializeField] ShopRowView rowPrefab;
        [SerializeField] RectTransform genreContainer;
        [SerializeField] RectTransform studioContainer;

        [Header("Colors")]
        [SerializeField] Color activeColor = new Color(0.3f, 1f, 0.66f);
        [SerializeField] Color ownedColor = new Color(0.54f, 0.52f, 0.69f);
        [SerializeField] Color lockedColor = new Color(0.54f, 0.52f, 0.69f);
        [SerializeField] Color studioIconColor = new Color(0.24f, 0.88f, 1f);
        [SerializeField] Color studioButtonColor = new Color(0.95f, 0.94f, 1f);

        readonly List<ShopRowView> genreRows = new List<ShopRowView>();
        readonly List<ShopRowView> studioRows = new List<ShopRowView>();
        GenreManager genres;
        StudioManager studios;
        EconomyManager economy;
        bool started;

        void Start()
        {
            var gm = GameManager.Instance;
            genres = gm.Genres;
            studios = gm.Studios;
            economy = gm.Economy;

            for (int i = 0; i < genres.Genres.Count; i++)
            {
                var row = Instantiate(rowPrefab, genreContainer);
                row.name = "Genre_" + genres.Genres[i].id;
                genreRows.Add(row);
            }
            for (int i = 0; i < studios.Count; i++)
            {
                var row = Instantiate(rowPrefab, studioContainer);
                row.name = "Studio_" + studios.Get(i).id;
                studioRows.Add(row);
            }

            genres.CurrentChanged += OnChanged;
            studios.CurrentChanged += OnChanged;
            economy.MoneyChanged += OnMoneyChanged;
            started = true;
            Loc.Changed += RefreshAll;
            RefreshAll();
        }

        void OnEnable()
        {
            if (started) RefreshAll();
        }

        void OnDestroy()
        {
            if (genres != null) genres.CurrentChanged -= OnChanged;
            if (studios != null) studios.CurrentChanged -= OnChanged;
            if (economy != null) economy.MoneyChanged -= OnMoneyChanged;
            Loc.Changed -= RefreshAll;
        }

        void OnChanged<T>(T _) => RefreshAll();

        void OnMoneyChanged(double _)
        {
            if (!isActiveAndEnabled) return;
            int nextGenre = genres.UnlockedCount;
            if (nextGenre < genreRows.Count) genreRows[nextGenre].SetInteractable(genres.CanUnlockNext());
            int nextStudio = studios.Level + 1;
            if (nextStudio < studioRows.Count) studioRows[nextStudio].SetInteractable(studios.CanUpgrade());
        }

        void RefreshAll()
        {
            for (int i = 0; i < genreRows.Count; i++) RefreshGenre(i);
            for (int i = 0; i < studioRows.Count; i++) RefreshStudio(i);
        }

        void RefreshGenre(int i)
        {
            var g = genres.Genres[i];
            var row = genreRows[i];
            string letters = g.displayName.Length >= 2 ? g.displayName.Substring(0, 2).ToUpperInvariant() : g.displayName;
            row.SetIcon(g.accentColor, letters);
            row.SetIconSprite(LogoGenerator.Get("genre_" + g.id, g.accentColor, i), Color.clear);
            row.SetTexts(g.DisplayName, Loc.F("common.income", "mult", NumberFormatter.FormatMultiplier(g.incomeMultiplier)));

            int unlocked = genres.UnlockedCount;
            if (i == unlocked - 1)
            {
                row.SetLocked(false);
                row.ShowStatus(Loc.T("shop.active"), activeColor);
            }
            else if (i < unlocked)
            {
                row.SetLocked(false);
                row.ShowStatus(Loc.T("shop.unlocked"), ownedColor);
            }
            else if (i == unlocked)
            {
                row.SetLocked(false);
                if (genres.MeetsStudioRequirement(g))
                {
                    row.ShowButton(Loc.T("shop.unlock"), NumberFormatter.FormatMoney(g.unlockCost), g.accentColor,
                        genres.CanUnlockNext(), () => { if (genres.TryUnlockNext()) row.Punch(); });
                }
                else
                {
                    var studio = studios.Get(g.requiredStudioLevel);
                    row.ShowStatus(Loc.F("common.needs", "what", studio != null ? studio.DisplayName : Loc.T("common.better_studio")), lockedColor);
                }
            }
            else
            {
                row.SetLocked(true);
                row.ShowStatus(Loc.T("ui.locked"), lockedColor);
            }
        }

        void RefreshStudio(int i)
        {
            var s = studios.Get(i);
            var row = studioRows[i];
            row.SetIcon(studioIconColor, (i + 1).ToString());
            row.SetIconSprite(LogoGenerator.Get("studio_" + s.id, Color.HSVToRGB(Mathf.Repeat(0.52f + i * 0.09f, 1f), 0.7f, 1f), i), Color.clear);
            row.SetTexts(s.DisplayName,
                Loc.F("common.income", "mult", NumberFormatter.FormatMultiplier(s.incomeMultiplier)) + "   " +
                Loc.F("common.speed", "mult", NumberFormatter.FormatMultiplier(s.speedMultiplier)));

            int level = studios.Level;
            if (i == level)
            {
                row.SetLocked(false);
                row.ShowStatus(Loc.T("shop.current"), activeColor);
            }
            else if (i < level)
            {
                row.SetLocked(false);
                row.ShowStatus(Loc.T("shop.owned"), ownedColor);
            }
            else if (i == level + 1)
            {
                row.SetLocked(false);
                row.ShowButton(Loc.T("shop.upgrade"), NumberFormatter.FormatMoney(s.upgradeCost), studioButtonColor,
                    studios.CanUpgrade(), () => { if (studios.TryUpgrade()) row.Punch(); });
            }
            else
            {
                row.SetLocked(true);
                row.ShowStatus(Loc.T("ui.locked"), lockedColor);
            }
        }
    }
}
