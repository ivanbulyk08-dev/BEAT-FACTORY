using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// BOOSTS popup: the featured boost card on top, then every boost with its timer, free/ad buttons and INSTANT VIRAL.
    /// </summary>
    public class BoostsPopupUI : MonoBehaviour
    {
        [SerializeField] ModalWindow window;
        [SerializeField] TMP_Text freeStatusText;
        [SerializeField] ShopRowView rowPrefab;
        [SerializeField] RectTransform container;
        [SerializeField] Button closeButton;
        [Tooltip("INSTANT VIRAL BEAT (rewarded ad).")]
        [SerializeField] Button instantViralButton;
        [Tooltip("Featured boost card at the top (CTA, live income preview, active state).")]
        [SerializeField] BoostOfferCard offerCard;
        [SerializeField] Color activeColor = new Color(0.3f, 1f, 0.66f);
        [SerializeField] Color dimColor = new Color(0.54f, 0.52f, 0.69f);
        [SerializeField] float refreshInterval = 0.5f;

        readonly List<ShopRowView> rows = new List<ShopRowView>();
        BoostManager boosts;
        float timer;
        bool built;

        void Start()
        {
            boosts = GameManager.Instance.Boosts;
            closeButton.onClick.AddListener(() => window.Hide());
            if (instantViralButton != null) instantViralButton.onClick.AddListener(OnInstantViral);
            enabled = false;
        }

        static bool AdReady => AdsManager.Instance != null && AdsManager.Instance.IsRewardedAvailable;

        float notReadyUntil;
        TMP_Text viralLabel;

        /// <summary>The player tapped an ad button but the ad is still loading: say so clearly (and keep loading in the background).</summary>
        void ShowNotReady()
        {
            notReadyUntil = Time.unscaledTime + 3f;
            AudioManager.Play(SoundId.Click, 0.8f);
            Refresh();
        }

        void OnInstantViral()
        {
            if (!GameManager.Instance.InstantViralReady) { AudioManager.Play(SoundId.Click, 0.8f); return; }
            if (!AdReady) { ShowNotReady(); return; }
            AdsManager.Instance.ShowRewarded(AdPlacements.InstantViral, rewarded =>
            {
                if (!rewarded) return;
                window.Hide(); // close so the VIRAL celebration is visible
                GameManager.Instance.GiveInstantViral();
            });
        }

        void WatchAdForBoost(BoostType type, ShopRowView row)
        {
            if (!AdReady) { ShowNotReady(); return; }
            AdsManager.Instance.ShowRewarded(AdPlacements.Boost, rewarded =>
            {
                if (!rewarded) { Refresh(); return; } // closed early / failed → no boost
                Debug.Log("[Ads] Boost granted: " + type);
                boosts.Activate(type);
                row.Punch();
                Refresh();
            });
        }

        public void Open()
        {
            if (boosts == null) boosts = GameManager.Instance.Boosts;
            if (!built) Build();
            Refresh();
            if (offerCard != null) offerCard.Refresh();
            window.Show();
            enabled = true;
        }

        void Build()
        {
            built = true;
            foreach (var def in boosts.Config.boosts)
            {
                var row = Instantiate(rowPrefab, container);
                row.name = "Boost_" + def.type;
                rows.Add(row);
            }
        }

        void Update()
        {
            if (!window.IsOpen) { enabled = false; return; }
            timer += Time.unscaledDeltaTime;
            if (timer < refreshInterval) return;
            timer = 0f;
            Refresh();
        }

        void Refresh()
        {
            bool freeReady = boosts.FreeBoostReady;
            freeStatusText.text = Time.unscaledTime < notReadyUntil
                ? Loc.T(Application.internetReachability == NetworkReachability.NotReachable ? "ads.no_internet" : "ads.still_loading")
                : freeReady
                    ? Loc.T("boosts.free_ready")
                    : Loc.F("boosts.next_free", "time", "<b>" + NumberFormatter.FormatClock(boosts.SecondsUntilFreeBoost) + "</b>");

            var defs = boosts.Config.boosts;
            for (int i = 0; i < rows.Count; i++)
            {
                var def = defs[i];
                var row = rows[i];
                bool active = boosts.IsActive(def.type);

                row.SetIcon(def.color, def.ChipText.Split(' ')[0]);
                row.SetTexts(def.DisplayName, def.Description + "  ·  " + NumberFormatter.FormatDuration(def.durationSeconds));
                row.SetLocked(false);

                if (freeReady)
                {
                    var type = def.type;
                    row.ShowButton(Loc.T(active ? "boosts.extend" : "boosts.activate"), Loc.T("ui.free"), def.color, true, () =>
                    {
                        if (boosts.TryActivateFree(type)) row.Punch();
                        Refresh();
                    });
                }
                else if (AdReady)
                {
                    var type = def.type;
                    string label = active ? NumberFormatter.FormatClock(boosts.RemainingSeconds(type)) + " " + Loc.T("ads.plus_ad") : Loc.T("ads.watch_ad");
                    row.ShowButton(label, "+" + NumberFormatter.FormatDuration(def.durationSeconds), def.color, true,
                        () => WatchAdForBoost(type, row));
                }
                else
                {
                    // ad still loading: keep the button so a tap explains what's happening
                    string label = active ? NumberFormatter.FormatClock(boosts.RemainingSeconds(def.type)) : Loc.T("ads.watch_ad");
                    row.ShowButton(label, Loc.T("ui.loading"), dimColor, true, ShowNotReady);
                }
            }

            if (instantViralButton != null)
            {
                // one INSTANT VIRAL ad per cooldown (unlimited ads made progression far too fast)
                var gm = GameManager.Instance;
                bool ready = gm.InstantViralReady;
                instantViralButton.interactable = ready;
                if (viralLabel == null) viralLabel = instantViralButton.GetComponentInChildren<TMP_Text>();
                if (viralLabel != null)
                    viralLabel.text = ready ? Loc.T("boosts.instant_viral")
                        : Loc.F("boosts.instant_viral_wait", "time", NumberFormatter.FormatClock(gm.SecondsUntilInstantViral));
            }
        }
    }
}
