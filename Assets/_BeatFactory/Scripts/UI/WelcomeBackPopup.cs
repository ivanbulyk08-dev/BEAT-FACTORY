using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// "WELCOME BACK! You were away for 2h 35m. Your producers made $124,500 [COLLECT] [x2 WITH AD]"
    /// </summary>
    public class WelcomeBackPopup : MonoBehaviour
    {
        [SerializeField] ModalWindow window;
        [SerializeField] TMP_Text awayText;
        [SerializeField] TMP_Text amountText;
        [SerializeField] TMP_Text noteText;
        [SerializeField] Button collectButton;
        [Tooltip("\"x2 WITH AD\" button: shown only when a rewarded ad is available.")]
        [SerializeField] Button doubleButton;
        [SerializeField] float doubleMultiplier = 2f;

        OfflineEarningsManager offline;
        double lastShownMoney;

        void Start()
        {
            offline = GameManager.Instance.Offline;

            collectButton.onClick.AddListener(OnCollect);
            window.BackAction = OnCollect; // Android Back = COLLECT (the money is never left behind)
            if (doubleButton != null) doubleButton.onClick.AddListener(OnDouble);

            offline.ReportReady += Show;
            Loc.Changed += OnLanguageChanged;
            if (offline.HasPending) Show(offline.Pending);
        }

        void OnDestroy()
        {
            if (offline != null) offline.ReportReady -= Show;
            Loc.Changed -= OnLanguageChanged;
        }

        // the popup can be open while the player switches language: rewrite its texts
        void OnLanguageChanged()
        {
            if (offline != null && offline.HasPending && window.IsOpen) FillTexts(offline.Pending);
        }

        void Show(OfflineReport report)
        {
            FillTexts(report);
            if (doubleButton != null)
            {
                bool adReady = AdsManager.Instance != null && AdsManager.Instance.IsRewardedAvailable;
                doubleButton.gameObject.SetActive(adReady);
                doubleButton.interactable = adReady;
            }
            window.Show();
        }

        void FillTexts(OfflineReport report)
        {
            awayText.text = Loc.F("offline.away", "time", "<b>" + NumberFormatter.FormatDuration(report.SecondsAway) + "</b>");
            amountText.text = NumberFormatter.FormatMoney(report.Money);
            lastShownMoney = report.Money;
            noteText.text = report.WasCapped
                ? Loc.F("offline.capped", "time", NumberFormatter.FormatDuration(offline.MaxOfflineSeconds))
                : Loc.P("offline.beats", report.Beats);
        }

        void OnCollect()
        {
            offline.Collect();
            window.Hide();
        }

        void OnDouble()
        {
            if (AdsManager.Instance == null) return;
            doubleButton.interactable = false; // no double taps while the ad loads

            AdsManager.Instance.ShowRewarded(AdPlacements.DoubleOffline, rewarded =>
            {
                if (rewarded)
                {
                    // If the money was auto-collected meanwhile (app went to background), pay the bonus half now.
                    if (offline.HasPending) offline.Collect(doubleMultiplier);
                    else GameManager.Instance.Economy.AddMoney(lastShownMoney * (doubleMultiplier - 1));
                    window.Hide();
                }
                else
                {
                    noteText.text = Loc.T("offline.ad_not_finished");
                    doubleButton.gameObject.SetActive(false);
                }
            });
        }
    }
}
