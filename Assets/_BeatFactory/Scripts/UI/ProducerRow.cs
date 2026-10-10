using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// One line in the producer list: icon, name, level, income, progress bar and HIRE/UPGRADE button.
    /// Created at runtime by ProducerListUI from the ProducerRow prefab.
    /// </summary>
    public class ProducerRow : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] Image iconBackground;
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text iconLetters;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text infoText;
        [SerializeField] RectTransform progressFill;
        [SerializeField] Button button;
        [SerializeField] TMP_Text buttonLabel;
        [SerializeField] TMP_Text costText;
        [Tooltip("Optional: glows briefly when the producer levels up.")]
        [SerializeField] CardFlash flash;

        [Header("Look")]
        [SerializeField] Color levelColor = new Color(0.24f, 0.88f, 1f);
        [SerializeField] float lockedAlpha = 0.45f;
        [SerializeField] float iconPulseScale = 1.15f;
        [SerializeField] float iconPulseDuration = 0.15f;
        [Tooltip("Producers faster than this don't spawn floating text (avoids spam).")]
        [SerializeField] float minCycleForFloatingText = 0.75f;

        Producer producer;
        ProducerManager manager;
        FloatingTextSpawner floatingText;
        ButtonPunch buttonPunch;
        Color moneyColor;
        string levelColorHex;
        float iconPulse;
        BuyMode buyMode = BuyMode.One;
        int shownAmount = -1;
        int lastLevel = -1;

        public void Bind(Producer producer, ProducerManager manager, FloatingTextSpawner floatingText, Color moneyColor)
        {
            this.producer = producer;
            this.manager = manager;
            this.floatingText = floatingText;
            this.moneyColor = moneyColor;
            levelColorHex = ColorUtility.ToHtmlStringRGB(levelColor);
            buttonPunch = button.GetComponent<ButtonPunch>();

            var data = producer.Data;
            nameText.text = data.DisplayName;
            iconBackground.color = data.color;
            if (data.icon != null)
            {
                iconImage.sprite = data.icon;
                iconImage.enabled = true;
                iconLetters.text = "";
            }
            else
            {
                iconImage.enabled = false;
                iconLetters.text = data.shortName;
            }

            button.onClick.AddListener(OnButtonClicked);
            Loc.Changed += OnLanguageChanged;
            Refresh();
        }

        void OnDestroy() => Loc.Changed -= OnLanguageChanged;

        void OnLanguageChanged()
        {
            nameText.text = producer.Data.DisplayName;
            Refresh();
        }

        /// <summary>Updates all texts (call after level/multiplier changes).</summary>
        public void Refresh()
        {
            if (lastLevel >= 0 && producer.Level > lastLevel && flash != null && isActiveAndEnabled) flash.Flash();
            lastLevel = producer.Level;
            bool unlocked = manager.IsUnlocked(producer);
            canvasGroup.alpha = unlocked ? 1f : lockedAlpha;
            button.gameObject.SetActive(unlocked);

            if (!unlocked)
            {
                if (producer.Index > 0 && !manager.Producers[producer.Index - 1].IsHired)
                    infoText.text = Loc.F("producer.hire_first", "name", manager.Producers[producer.Index - 1].Data.DisplayName);
                else
                {
                    var studio = GameManager.Instance.Studios.Get(producer.Data.requiredStudio);
                    infoText.text = "<color=#FF8A3D>" + Loc.F("common.unlocks_at", "studio", studio != null ? studio.DisplayName : Loc.T("common.better_studio")) + "</color>";
                }
            }
            else if (!producer.IsHired)
            {
                var income = producer.IncomeAtLevel(1) * manager.ProducerIncomeMultiplier * GameManager.Instance.Beats.GlobalMultiplier;
                infoText.text = Loc.F("producer.per_sec_not_hired", "money", NumberFormatter.FormatMoney(income / manager.GetCycleSeconds(producer)));
            }
            else
            {
                double perSecond = manager.GetIncomePerBeat(producer) / manager.GetCycleSeconds(producer);
                infoText.text = "<color=#" + levelColorHex + ">" + Loc.F("common.level_short", "level", producer.Level) + "</color>   " +
                                Loc.F("common.per_sec", "money", NumberFormatter.FormatMoney(perSecond));
            }

            shownAmount = -1; // force the button texts to update
            RefreshButton();
        }

        public void SetBuyMode(BuyMode mode)
        {
            buyMode = mode;
            shownAmount = -1;
            RefreshButton();
        }

        /// <summary>
        /// Called when money changes. Only rebuilds the texts when the amount to buy changed (MAX mode),
        /// otherwise it just toggles the button.
        /// </summary>
        public void RefreshButton()
        {
            if (producer.IsMaxLevel)
            {
                if (shownAmount != 0)
                {
                    buttonLabel.text = Loc.T("ui.max");
                    costText.text = Loc.F("common.level_short", "level", producer.Level);
                    shownAmount = 0;
                }
                button.interactable = false;
                return;
            }

            int amount = manager.GetBuyAmount(producer, buyMode);
            if (amount != shownAmount)
            {
                shownAmount = amount;
                string verb = Loc.T(producer.IsHired ? "producer.upgrade" : "producer.hire");
                buttonLabel.text = buyMode == BuyMode.One ? verb : verb + " x" + amount;
                costText.text = NumberFormatter.FormatMoney(producer.CostOfLevels(amount));
            }
            button.interactable = manager.CanBuy(producer, amount);
        }

        /// <summary>Called every frame by ProducerListUI.</summary>
        public void Tick(float deltaTime)
        {
            float progress = manager.GetCycleSeconds(producer) < 0.25 && producer.IsHired ? 1f : producer.Progress;
            var max = progressFill.anchorMax;
            if (!Mathf.Approximately(max.x, progress))
            {
                max.x = progress;
                progressFill.anchorMax = max;
            }

            if (iconPulse > 0f)
            {
                iconPulse -= deltaTime;
                float t = Mathf.Clamp01(iconPulse / iconPulseDuration);
                iconBackground.transform.localScale = Vector3.one * Mathf.Lerp(1f, iconPulseScale, t);
            }
        }

        public void OnProduced(BeatResult result)
        {
            iconPulse = iconPulseDuration;

            // Fast producers only show text for good beats to avoid spam.
            bool fast = manager.GetCycleSeconds(producer) < minCycleForFloatingText;
            if (floatingText != null && (!fast || result.Quality >= BeatQuality.Great))
            {
                var qualityConfig = GameManager.Instance.Beats.QualityConfig;
                floatingText.Spawn(BeatFeedback.RewardText(result, qualityConfig),
                    BeatFeedback.RewardColor(result, qualityConfig, moneyColor),
                    iconBackground.transform.position, new Vector2(0f, 60f), 0.6f * BeatFeedback.SizeMultiplier(result));
            }
        }

        void OnButtonClicked()
        {
            int amount = manager.GetBuyAmount(producer, buyMode);
            if (manager.TryBuyLevels(producer, amount) && buttonPunch != null)
                buttonPunch.Punch();
        }

        static string FormatSeconds(double seconds)
        {
            return seconds >= 10 ? Mathf.RoundToInt((float)seconds) + "s" : seconds.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "s";
        }
    }
}
