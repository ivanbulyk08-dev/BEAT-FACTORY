using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// The featured boost at the top of the BOOSTS window: shows the real multiplier and duration from BoostConfig,
    /// the player's income now → with the boost (live, including the combined-boost cap), and one clear CTA.
    /// The boost is granted only when the rewarded ad reports completion (or for free when the free boost is ready).
    /// While the boost runs, the CTA turns into BOOST ACTIVE with a countdown and a progress bar.
    /// Animations (breathing glow, shimmer, press/activation feedback) only run while the window is open.
    /// </summary>
    public class BoostOfferCard : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text detailText;
        [SerializeField] TMP_Text previewText;
        [SerializeField] Button ctaButton;
        [SerializeField] TMP_Text ctaText;
        [SerializeField] TMP_Text noteText;
        [SerializeField] GameObject activePanel;
        [SerializeField] TMP_Text activeTimerText;
        [SerializeField] Image activeFill;
        [Header("Animation")]
        [SerializeField] Image glow;
        [SerializeField] RectTransform shine;
        [SerializeField] Image flash;
        [SerializeField] RectTransform icon;
        [SerializeField] float refreshInterval = 0.25f;

        GameManager game;
        BoostManager boosts;
        BoostType featured;
        bool waitingForAd;
        float notePriorityUntil;
        string priorityNote;
        float refreshTimer, time, flashTime = -1f, iconPunch;
        double activeTotal;
        Color glowBase, flashBase;
        Vector2 shineStart;

        /// <summary>The boost the card (and the top-bar button) advertises: PRODUCER BOOST once producers earn, else 2X MONEY.</summary>
        public static BoostType Featured(GameManager gm) =>
            gm.Producers.IncomePerSecond > 0 ? BoostType.Producer : BoostType.Money;

        void Awake()
        {
            if (glow != null) glowBase = glow.color;
            if (flash != null) { flashBase = flash.color; SetAlpha(flash, 0); }
            if (shine != null) shineStart = shine.anchoredPosition;
        }

        void Start()
        {
            game = GameManager.Instance;
            boosts = game.Boosts;
            ctaButton.onClick.AddListener(OnCta);
            boosts.BoostStarted += OnBoostStarted;
            Loc.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (boosts != null) boosts.BoostStarted -= OnBoostStarted;
            Loc.Changed -= Refresh;
        }

        void OnEnable() => Refresh();

        // ------------------------------------------------------------------ actions

        void OnCta()
        {
            if (game == null || waitingForAd || boosts.IsActive(featured)) return;

            if (boosts.FreeBoostReady)
            {
                boosts.TryActivateFree(featured);
                Refresh();
                return;
            }

            var ads = AdsManager.Instance;
            if (ads == null || !ads.IsRewardedAvailable)
            {
                // never fake it: tell the player why nothing happened (the ad keeps loading in the background)
                Note(Loc.T(Application.internetReachability == NetworkReachability.NotReachable ? "ads.no_internet" : "ads.still_loading"));
                AudioManager.Play(SoundId.Click, 0.8f);
                return;
            }

            waitingForAd = true; // repeated taps are ignored until the ad answers
            var type = featured;
            Refresh();
            ads.ShowRewarded(AdPlacements.Boost, rewarded =>
            {
                waitingForAd = false;
                if (rewarded) boosts.Activate(type); // only after the SDK confirmed the reward
                else Note(Loc.T("boost_offer.not_completed"));
                Refresh();
            });
        }

        void OnBoostStarted(BoostDefinition def, float seconds)
        {
            if (def == null || def.type != featured) return;
            activeTotal = 0; // re-measured from the new remaining time
            flashTime = 0f;
            iconPunch = 1f;
            Refresh();
        }

        void Note(string text)
        {
            priorityNote = text;
            notePriorityUntil = Time.unscaledTime + 3.5f;
            Refresh();
        }

        // ------------------------------------------------------------------ display

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;
            refreshTimer += dt;
            if (refreshTimer >= refreshInterval)
            {
                refreshTimer = 0;
                Refresh();
            }

            // breathing neon glow (~4 s cycle)
            float breath = 0.5f + 0.5f * Mathf.Sin(time * 1.6f);
            if (glow != null)
            {
                var c = glowBase;
                c.a = glowBase.a * Mathf.Lerp(0.55f, 1f, breath);
                glow.color = c;
            }

            // shimmer sweep across the CTA every 3.5 s
            if (shine != null && shine.gameObject.activeInHierarchy)
            {
                float k = Mathf.Repeat(time, 3.5f) / 1.1f;
                float width = ((RectTransform)shine.parent).rect.width + shine.rect.width * 2;
                shine.anchoredPosition = shineStart + new Vector2(k <= 1f ? Mathf.SmoothStep(0, 1, k) * width : width, 0);
            }

            // activation flash + icon pop
            if (flash != null && flashTime >= 0f)
            {
                flashTime += dt;
                SetAlpha(flash, flashBase.a * Mathf.Clamp01(1f - flashTime / 0.45f));
                if (flashTime > 0.45f) flashTime = -1f;
            }
            if (icon != null)
            {
                iconPunch = Mathf.MoveTowards(iconPunch, 0f, dt * 2.5f);
                float s = 1f + 0.035f * breath + 0.3f * iconPunch * iconPunch;
                icon.localScale = new Vector3(s, s, 1);
            }
        }

        public void Refresh()
        {
            if (game == null || boosts == null) return;
            featured = Featured(game);
            var def = boosts.Config.Get(featured);
            if (def == null) return;

            string mult = NumberFormatter.FormatMultiplier(def.multiplier);
            titleText.text = Loc.F("boost_offer.title", "mult", "<color=#FFC94D>" + mult + "</color>");
            detailText.text = def.Description + "  ·  " + NumberFormatter.FormatDuration(def.durationSeconds);
            // while it runs, show what the player earns now (the boosted income); otherwise now → with the boost
            previewText.text = boosts.IsActive(featured) && game.Producers.IncomePerSecond > 0
                ? "<color=#4DFFA8>" + PerMin(game.Producers.IncomePerSecond * 60) + "</color>"
                : Preview(featured);

            bool active = boosts.IsActive(featured);
            if (activePanel.activeSelf != active) activePanel.SetActive(active);
            if (ctaButton.gameObject.activeSelf == active) ctaButton.gameObject.SetActive(!active);

            if (active)
            {
                double left = boosts.RemainingSeconds(featured);
                if (left > activeTotal) activeTotal = left; // first frame or extended
                activeTimerText.text = Loc.T("boost_offer.active") + "  " + NumberFormatter.FormatClock(left);
                activeFill.fillAmount = activeTotal > 0 ? (float)(left / activeTotal) : 0f;
            }
            else
            {
                activeTotal = 0;
                bool free = boosts.FreeBoostReady;
                ctaText.text = Loc.F(free ? "boost_offer.cta_free" : "boost_offer.cta", "mult", mult);
                ctaButton.interactable = !waitingForAd;
            }

            string note;
            if (Time.unscaledTime < notePriorityUntil) note = priorityNote;
            else if (waitingForAd) note = Loc.T("boost_offer.opening_ad");
            else if (active) note = Loc.T("boost_offer.active_note");
            else if (boosts.FreeBoostReady) note = Loc.T("boost_offer.free_note");
            else note = Loc.T("boost_offer.ad_note");
            noteText.text = note;
        }

        /// <summary>"$1.2K/min → $3.6K/min" with the real numbers (cap and running boosts included); per beat before producers earn.</summary>
        string Preview(BoostType type)
        {
            double perSecond = game.Producers.IncomePerSecond;
            if (perSecond <= 0)
            {
                double beat = game.Beats.ManualBeatReward;
                double after = boosts.IsActive(BoostType.Money) || type != BoostType.Money ? beat : beat * boosts.Config.Get(BoostType.Money).multiplier;
                return Loc.F("home.per_beat", "money", NumberFormatter.FormatMoney(beat) + "  →  <color=#4DFFA8>" + NumberFormatter.FormatMoney(after) + "</color>");
            }
            double current = boosts.ProducerIncomeFactor;
            double withBoost = boosts.PreviewProducerFactor(type);
            double now = perSecond * 60, boosted = perSecond / current * withBoost * 60;
            return PerMin(now) + "  →  <color=#4DFFA8>" + PerMin(boosted) + "</color>";
        }

        static string PerMin(double v) => Loc.F("common.per_min", "money", NumberFormatter.FormatMoney(v));

        static void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            c.a = a;
            g.color = c;
        }
    }
}
