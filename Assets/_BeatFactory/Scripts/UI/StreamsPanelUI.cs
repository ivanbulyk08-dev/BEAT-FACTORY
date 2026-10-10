using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// STREAMING section of the MUSIC screen: stream counter, CASH OUT button and the platforms list.
    /// The counter is refreshed a few times per second at most.
    /// </summary>
    public class StreamsPanelUI : MonoBehaviour
    {
        [SerializeField] TMP_Text streamsText;
        [SerializeField] TMP_Text valueText;
        [SerializeField] Button cashOutButton;
        [SerializeField] TMP_Text cashOutLabel;
        [SerializeField] ShopRowView rowPrefab;
        [SerializeField] RectTransform platformContainer;
        [SerializeField] FloatingTextSpawner floatingText;
        [SerializeField] Color moneyColor = new Color(0.3f, 1f, 0.66f);
        [SerializeField] Color liveColor = new Color(0.3f, 1f, 0.66f);
        [SerializeField] float refreshInterval = 0.2f;

        readonly List<ShopRowView> rows = new List<ShopRowView>();
        StreamManager streams;
        EconomyManager economy;
        ButtonPunch punch;
        bool dirty = true;
        bool started;
        float timer;

        void Start()
        {
            var gm = GameManager.Instance;
            streams = gm.Streams;
            economy = gm.Economy;
            punch = cashOutButton.GetComponent<ButtonPunch>();

            foreach (var p in streams.Platforms)
            {
                var row = Instantiate(rowPrefab, platformContainer);
                row.name = "Platform_" + p.id;
                rows.Add(row);
            }

            cashOutButton.onClick.AddListener(OnCashOut);
            streams.StreamsChanged += OnStreamsChanged;
            streams.PlatformsChanged += RefreshPlatforms;
            economy.MoneyChanged += OnMoneyChanged;
            started = true;
            Loc.Changed += OnLanguageChanged;
            RefreshPlatforms();
            RefreshCounter();
        }

        void OnEnable()
        {
            if (!started) return;
            RefreshPlatforms();
            RefreshCounter();
        }

        void OnDestroy()
        {
            if (streams != null)
            {
                streams.StreamsChanged -= OnStreamsChanged;
                streams.PlatformsChanged -= RefreshPlatforms;
            }
            if (economy != null) economy.MoneyChanged -= OnMoneyChanged;
            Loc.Changed -= OnLanguageChanged;
        }

        void OnLanguageChanged() { RefreshPlatforms(); RefreshCounter(); }

        void OnStreamsChanged(double _) => dirty = true;

        void OnMoneyChanged(double _)
        {
            if (!isActiveAndEnabled) return;
            for (int i = 0; i < rows.Count; i++)
            {
                var p = streams.Platforms[i];
                if (!streams.IsReleased(p)) rows[i].SetInteractable(streams.CanRelease(p));
            }
        }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (!dirty || timer < refreshInterval) return;
            timer = 0f;
            RefreshCounter();
        }

        void RefreshCounter()
        {
            dirty = false;
            streamsText.text = NumberFormatter.Format(streams.Streams);
            valueText.text = Loc.F("music.streams_info", "money", "<b>" + NumberFormatter.FormatMoney(streams.StreamsValue) + "</b>",
                "rate", NumberFormatter.FormatMoney(streams.Config.MoneyPerStream), "mult", NumberFormatter.FormatMultiplier(streams.PlatformMultiplier));
            cashOutButton.interactable = streams.StreamsValue >= 0.01;
        }

        void OnCashOut()
        {
            double money = streams.CashOut();
            if (money <= 0) return;
            if (punch != null) punch.Punch();
            if (floatingText != null)
                floatingText.Spawn("+" + NumberFormatter.FormatMoney(money), moneyColor, cashOutButton.transform.position, new Vector2(0, 90), 1.2f);
            RefreshCounter();
        }

        void RefreshPlatforms()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var p = streams.Platforms[i];
                var row = rows[i];
                row.SetIcon(p.color, p.shortName);
                row.SetIconSprite(LogoGenerator.Get("platform_" + p.id, p.color, i), Color.clear);
                row.SetTexts(p.displayName, Loc.F("common.streams_mult", "mult", NumberFormatter.FormatMultiplier(p.streamMultiplier)));
                row.SetLocked(false);
                if (streams.IsReleased(p))
                    row.ShowStatus(Loc.T("music.live"), liveColor);
                else
                    row.ShowButton(Loc.T("music.release"), NumberFormatter.FormatMoney(p.releaseCost), p.color, streams.CanRelease(p),
                        () => { if (streams.TryRelease(p)) row.Punch(); });
            }
            dirty = true;
        }
    }
}
