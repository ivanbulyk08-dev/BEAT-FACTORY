using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// PRESTIGE tab: current Fame and bonus, how much Fame a restart would give,
    /// and the RESTART LABEL button (with confirmation). Shows unlock progress until available.
    /// </summary>
    public class PrestigeScreenUI : MonoBehaviour
    {
        [SerializeField] TMP_Text fameText;
        [SerializeField] TMP_Text bonusText;
        [SerializeField] TMP_Text countText;
        [SerializeField] TMP_Text pendingText;
        [SerializeField] TMP_Text detailText;
        [SerializeField] RectTransform progressFill;
        [SerializeField] GameObject progressBar;
        [SerializeField] Button restartButton;
        [SerializeField] ConfirmDialog confirmDialog;
        [SerializeField] ScreenNavigator navigator;
        [SerializeField] Color confirmColor = new Color(1f, 0.54f, 0.24f);
        [SerializeField] float refreshInterval = 0.25f;

        GameManager gm;
        float timer;

        void Start()
        {
            gm = GameManager.Instance;
            restartButton.onClick.AddListener(OnRestartPressed);
            Refresh();
        }

        void OnEnable()
        {
            if (gm != null) Refresh();
        }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < refreshInterval) return;
            timer = 0f;
            Refresh();
        }

        void Refresh()
        {
            var economy = gm.Economy;
            var prestige = gm.Prestige;
            double bonusPercent = (economy.FameMultiplier - 1.0) * 100.0;

            fameText.text = NumberFormatter.Format(economy.FamePoints);
            bonusText.text = Loc.F("prestige.bonus", "value", "+" + NumberFormatter.Format(bonusPercent) + "%");
            countText.text = prestige.Count == 0 ? Loc.T("prestige.first_run") : Loc.F("prestige.restarts", "count", prestige.Count);

            bool can = prestige.CanPrestige;
            restartButton.interactable = can;
            progressBar.SetActive(!can);

            if (can)
            {
                double pending = prestige.PendingFame;
                pendingText.text = Loc.F("prestige.pending", "fame", "<b>+" + NumberFormatter.Format(pending) + "</b>");
                detailText.text = Loc.F("prestige.detail", "from", NumberFormatter.FormatMultiplier(economy.FameMultiplier),
                                  "to", "<b>" + NumberFormatter.FormatMultiplier(prestige.MultiplierAfterPrestige) + "</b>");
            }
            else
            {
                pendingText.text = Loc.F("prestige.locked", "money", "<b>" + NumberFormatter.FormatMoney(prestige.MinRunEarnings) + "</b>");
                detailText.text = NumberFormatter.FormatMoney(economy.RunEarnings) + " / " + NumberFormatter.FormatMoney(prestige.MinRunEarnings);
                var max = progressFill.anchorMax;
                max.x = (float)prestige.UnlockProgress;
                progressFill.anchorMax = max;
            }
        }

        void OnRestartPressed()
        {
            var prestige = gm.Prestige;
            if (!prestige.CanPrestige) return;

            confirmDialog.Show(Loc.T("prestige.confirm_title"),
                Loc.F("prestige.confirm_message", "fame", "<b>+" + NumberFormatter.Format(prestige.PendingFame) + "</b>",
                    "mult", NumberFormatter.FormatMultiplier(prestige.MultiplierAfterPrestige)),
                Loc.T("prestige.confirm_button"), confirmColor,
                () =>
                {
                    if (!gm.Prestige.TryPrestige()) return;
                    if (navigator != null) navigator.Show("HOME");
                    // A restart is a natural break: the only place an interstitial may appear (rules in AdsManager).
                    AdsManager.Instance?.TryShowInterstitialDelayed(AdPlacements.AfterPrestige, 3f);
                });
        }
    }
}
