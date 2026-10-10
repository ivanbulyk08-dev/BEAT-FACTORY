using System.Collections.Generic;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Builds the scrollable producer list (one ProducerRow per producer) and keeps it up to date.
    /// One Update() drives the progress bars of all rows. Money changes are batched
    /// (max ~10 refreshes per second) because late game money changes many times per second.
    /// </summary>
    public class ProducerListUI : MonoBehaviour
    {
        [SerializeField] ProducerRow rowPrefab;
        [SerializeField] RectTransform container;
        [SerializeField] FloatingTextSpawner floatingText;
        [SerializeField] Color moneyColor = new Color(0.3f, 1f, 0.66f);
        [Tooltip("Optional x1/x10/MAX selector. Without it the list always buys one level.")]
        [SerializeField] BuyModeSelector buyModeSelector;
        [SerializeField] float buttonRefreshInterval = 0.1f;

        readonly List<ProducerRow> rows = new List<ProducerRow>();
        ProducerManager producers;
        EconomyManager economy;
        MultiplierStack income;
        MultiplierStack speed;
        bool started;
        bool buttonsDirty;
        float refreshTimer;

        void Start()
        {
            producers = GameManager.Instance.Producers;
            economy = GameManager.Instance.Economy;

            foreach (var p in producers.Producers)
            {
                var row = Instantiate(rowPrefab, container);
                row.name = "Row_" + p.Data.id;
                row.Bind(p, producers, floatingText, moneyColor);
                rows.Add(row);
            }

            income = GameManager.Instance.Income;
            speed = GameManager.Instance.Speed;
            economy.MoneyChanged += OnMoneyChanged;
            income.Changed += RefreshAll;
            speed.Changed += RefreshAll;
            producers.ProducerChanged += OnProducerChanged;
            producers.ProducerProduced += OnProducerProduced;

            if (buyModeSelector != null)
            {
                buyModeSelector.Changed += OnBuyModeChanged;
                OnBuyModeChanged(buyModeSelector.Mode);
            }
            started = true;
        }

        void OnEnable()
        {
            // The list may have been hidden (other tab) while things changed.
            if (started) RefreshAll();
        }

        void OnDestroy()
        {
            if (economy != null) economy.MoneyChanged -= OnMoneyChanged;
            if (income != null) income.Changed -= RefreshAll;
            if (speed != null) speed.Changed -= RefreshAll;
            if (producers != null)
            {
                producers.ProducerChanged -= OnProducerChanged;
                producers.ProducerProduced -= OnProducerProduced;
            }
            if (buyModeSelector != null) buyModeSelector.Changed -= OnBuyModeChanged;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < rows.Count; i++) rows[i].Tick(dt);

            refreshTimer += dt;
            if (buttonsDirty && refreshTimer >= buttonRefreshInterval)
            {
                buttonsDirty = false;
                refreshTimer = 0f;
                for (int i = 0; i < rows.Count; i++) rows[i].RefreshButton();
            }
        }

        void OnMoneyChanged(double _) => buttonsDirty = true;

        void OnBuyModeChanged(BuyMode mode)
        {
            for (int i = 0; i < rows.Count; i++) rows[i].SetBuyMode(mode);
        }

        void OnProducerChanged(Producer p)
        {
            rows[p.Index].Refresh();
            // Hiring a producer unlocks the next one.
            if (p.Index + 1 < rows.Count) rows[p.Index + 1].Refresh();
        }

        void OnProducerProduced(Producer p, BeatResult result)
        {
            if (isActiveAndEnabled) rows[p.Index].OnProduced(result);
        }

        public void RefreshAll()
        {
            for (int i = 0; i < rows.Count; i++) rows[i].Refresh();
        }
    }
}
