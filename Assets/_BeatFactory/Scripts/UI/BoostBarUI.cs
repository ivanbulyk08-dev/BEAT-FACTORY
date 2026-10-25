using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Strip under the top bar: a BOOSTS button (with a dot when a free boost is ready)
    /// and one chip per active boost with its countdown. Tapping anything opens the boosts popup.
    /// </summary>
    public class BoostBarUI : MonoBehaviour
    {
        [SerializeField] Button boostsButton;
        [SerializeField] GameObject freeDot;
        [Tooltip("Label of the BOOSTS button: shows the featured boost (for example ×3 CASH) from BoostConfig.")]
        [SerializeField] TMP_Text boostsLabel;
        [SerializeField] BoostsPopupUI popup;
        [Tooltip("One chip per boost type, in BoostType order (Money, Speed, Producer, Viral).")]
        [SerializeField] GameObject[] chips;
        [SerializeField] TMP_Text[] chipTexts;
        [SerializeField] Image[] chipImages;
        [SerializeField] float refreshInterval = 0.5f;

        BoostManager boosts;
        float timer;

        void Start()
        {
            boosts = GameManager.Instance.Boosts;
            boostsButton.onClick.AddListener(() => popup.Open());
            foreach (var chip in chips)
            {
                var b = chip.GetComponent<Button>();
                if (b != null) b.onClick.AddListener(() => popup.Open());
            }
            boosts.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (boosts != null) boosts.Changed -= Refresh;
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
            for (int i = 0; i < chips.Length; i++)
            {
                var type = (BoostType)i;
                bool active = boosts.IsActive(type);
                if (chips[i].activeSelf != active) chips[i].SetActive(active);
                if (!active) continue;

                var def = boosts.Config.Get(type);
                chipTexts[i].text = def.ChipText + "  " + NumberFormatter.FormatClock(boosts.RemainingSeconds(type));
                chipImages[i].color = def.color;
            }

            if (boostsLabel != null)
            {
                var featured = boosts.Config.Get(BoostOfferCard.Featured(GameManager.Instance));
                if (featured != null) boostsLabel.text = Loc.F("boost_offer.button", "mult", NumberFormatter.FormatMultiplier(featured.multiplier));
            }

            bool free = boosts.FreeBoostReady;
            if (freeDot.activeSelf != free) freeDot.SetActive(free);
        }
    }
}
