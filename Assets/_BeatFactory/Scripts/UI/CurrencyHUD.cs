using TMPro;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Top bar that shows Money and Fame. The money number smoothly counts up
    /// and the label "punches" when money is earned. The component disables its own
    /// Update() when nothing is animating, so it costs nothing while idle.
    /// </summary>
    public class CurrencyHUD : MonoBehaviour
    {
        [SerializeField] TMP_Text moneyText;
        [SerializeField] TMP_Text fameText;
        [Tooltip("Optional: shows income per second from producers.")]
        [SerializeField] TMP_Text incomeText;

        [Header("Animation")]
        [Tooltip("How fast the displayed number catches up with the real value (higher = faster).")]
        [SerializeField] float countSpeed = 10f;
        [SerializeField] float punchScale = 1.12f;
        [SerializeField] float punchDuration = 0.15f;

        EconomyManager economy;
        ProducerManager producers;
        MultiplierStack income;
        MultiplierStack speed;
        double displayedMoney;
        double targetMoney;
        float punchTimer;
        float textTimer;
        string lastMoneyString;

        void Start()
        {
            economy = GameManager.Instance.Economy;
            economy.MoneyChanged += OnMoneyChanged;
            economy.MoneyEarned += OnMoneyEarned;
            economy.FameChanged += OnFameChanged;
            producers = GameManager.Instance.Producers;
            producers.ProducerChanged += OnProducerChanged;
            income = GameManager.Instance.Income;
            income.Changed += RefreshIncomeText;
            speed = GameManager.Instance.Speed;
            speed.Changed += RefreshIncomeText;

            displayedMoney = targetMoney = economy.Money;
            RefreshMoneyText();
            OnFameChanged(economy.FamePoints);
            RefreshIncomeText();
            Loc.Changed += OnLanguageChanged;
            enabled = false;
        }

        void OnLanguageChanged()
        {
            lastMoneyString = null;
            RefreshMoneyText();
            OnFameChanged(economy.FamePoints);
            RefreshIncomeText();
        }

        void OnDestroy()
        {
            Loc.Changed -= OnLanguageChanged;
            if (producers != null) producers.ProducerChanged -= OnProducerChanged;
            if (income != null) income.Changed -= RefreshIncomeText;
            if (speed != null) speed.Changed -= RefreshIncomeText;
            if (economy == null) return;
            economy.MoneyChanged -= OnMoneyChanged;
            economy.MoneyEarned -= OnMoneyEarned;
            economy.FameChanged -= OnFameChanged;
        }

        void OnProducerChanged(Producer _) => RefreshIncomeText();

        void RefreshIncomeText()
        {
            if (incomeText == null) return;
            double perSecond = producers.IncomePerSecond;
            string text = perSecond > 0 ? "+" + Loc.F("common.per_s", "money", NumberFormatter.FormatMoney(perSecond)) : "";
            // running boosts: show what they do to producer income right next to it
            double boost = GameManager.Instance != null ? GameManager.Instance.Boosts.ProducerIncomeFactor : 1;
            if (perSecond > 0 && boost > 1.001) text += "  <color=#FFC94D>" + NumberFormatter.FormatMultiplier(boost) + "</color>";
            incomeText.richText = true;
            incomeText.text = text;
        }

        void OnMoneyChanged(double money)
        {
            targetMoney = money;
            // Spending is shown instantly, earning counts up.
            if (money < displayedMoney)
            {
                displayedMoney = money;
                RefreshMoneyText();
            }
            enabled = true;
        }

        void OnMoneyEarned(double amount)
        {
            punchTimer = punchDuration;
            enabled = true;
        }

        void OnFameChanged(double fame)
        {
            if (fameText != null) fameText.text = NumberFormatter.Format(fame);
        }

        void Update()
        {
            bool counting = displayedMoney < targetMoney;
            if (counting)
            {
                double step = (targetMoney - displayedMoney) * Mathf.Clamp01(countSpeed * Time.unscaledDeltaTime);
                displayedMoney += step;
                if (targetMoney - displayedMoney < 0.5 || step <= 0) displayedMoney = targetMoney;

                // Late game money changes every frame: format the text max 20×/s (saves string garbage).
                textTimer -= Time.unscaledDeltaTime;
                if (textTimer <= 0f || displayedMoney >= targetMoney)
                {
                    textTimer = 0.05f;
                    RefreshMoneyText();
                }
            }

            if (punchTimer > 0)
            {
                punchTimer -= Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(punchTimer / punchDuration);
                moneyText.transform.localScale = Vector3.one * Mathf.Lerp(1f, punchScale, t);
            }

            if (!counting && punchTimer <= 0)
            {
                moneyText.transform.localScale = Vector3.one;
                enabled = false;
            }
        }

        void RefreshMoneyText()
        {
            // Only touch TextMeshPro when the visible string really changes (avoids mesh rebuilds).
            string s = NumberFormatter.FormatMoney(displayedMoney);
            if (s == lastMoneyString) return;
            lastMoneyString = s;
            moneyText.text = s;
        }
    }
}
