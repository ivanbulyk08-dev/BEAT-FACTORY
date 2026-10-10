using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Room upgrade screen: CURRENT ROOM → NEXT ROOM with previews, price, income multiplier,
    /// everything the next room unlocks and a live progress bar toward the price.
    /// </summary>
    public class StudioUpgradePopup : MonoBehaviour
    {
        [SerializeField] ModalWindow window;
        [SerializeField] Sprite[] roomPreviews;
        [SerializeField] Image currentPreview;
        [SerializeField] TMP_Text currentName;
        [SerializeField] TMP_Text currentStats;
        [SerializeField] Image nextPreview;
        [SerializeField] TMP_Text nextLevel;
        [SerializeField] TMP_Text nextName;
        [SerializeField] TMP_Text nextTagline;
        [SerializeField] TMP_Text incomeText;
        [SerializeField] TMP_Text unlocksText;
        [SerializeField] TMP_Text progressText;
        [SerializeField] RectTransform progressFill;
        [SerializeField] Button upgradeButton;
        [SerializeField] TMP_Text upgradeLabel;
        [SerializeField] TMP_Text upgradePrice;
        [SerializeField] Button closeButton;
        [SerializeField] GameObject nextGroup;
        [SerializeField] GameObject maxedGroup;
        [SerializeField] Color readyColor = new Color(0.95f, 0.76f, 0.31f);
        [SerializeField] Color waitColor = new Color(0.38f, 0.36f, 0.45f);

        GameManager gm;
        float timer;

        public static StudioUpgradePopup Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            upgradeButton.onClick.AddListener(OnUpgrade);
            closeButton.onClick.AddListener(() => window.Hide());
        }

        void Start() => gm = GameManager.Instance;

        public void Open()
        {
            if (gm == null) gm = GameManager.Instance;
            Refresh();
            window.Show();
            enabled = true;
        }

        Sprite Preview(int level) => roomPreviews != null && level >= 0 && level < roomPreviews.Length ? roomPreviews[level] : null;

        void Refresh()
        {
            var studios = gm.Studios;
            var cur = studios.Current;
            currentPreview.sprite = Preview(studios.Level);
            currentName.text = cur != null ? Loc.Upper(cur.DisplayName) : "";
            currentStats.text = cur != null ? Loc.F("common.level_short", "level", studios.Level + 1) + "   ·   " + Loc.F("common.income", "mult", NumberFormatter.FormatMultiplier(cur.incomeMultiplier)) : "";

            var next = studios.Next;
            nextGroup.SetActive(next != null);
            maxedGroup.SetActive(next == null);
            if (next == null) return;

            int nextLv = studios.Level + 1;
            nextPreview.sprite = Preview(nextLv);
            nextLevel.text = Loc.F("studio.next_level", "level", nextLv + 1);
            nextName.text = Loc.Upper(next.DisplayName);
            nextTagline.text = next.Tagline;
            incomeText.text = Loc.F("common.income", "mult", "<b>" + NumberFormatter.FormatMultiplier(next.incomeMultiplier) + "</b>") + "   ·   " +
                              Loc.F("common.speed", "mult", NumberFormatter.FormatMultiplier(next.speedMultiplier));
            unlocksText.text = UnlocksFor(nextLv, next);
            RefreshProgress();
        }

        void RefreshProgress()
        {
            var next = gm.Studios.Next;
            if (next == null) return;
            double money = gm.Economy.Money, cost = next.upgradeCost;
            float p = (float)System.Math.Min(1.0, money / System.Math.Max(1, cost));
            progressFill.anchorMax = new Vector2(p, 1);
            progressText.text = NumberFormatter.FormatMoney(System.Math.Min(money, cost)) + "  /  " + NumberFormatter.FormatMoney(cost);
            bool ready = money >= cost;
            upgradeButton.interactable = ready;
            ((Image)upgradeButton.targetGraphic).color = ready ? readyColor : waitColor;
            upgradeLabel.text = Loc.T(ready ? "studio.move_in" : "studio.saving_up");
            upgradePrice.text = NumberFormatter.FormatMoney(cost);
        }

        /// <summary>Everything the room at <paramref name="level"/> unlocks, as a bullet list.</summary>
        string UnlocksFor(int level, StudioData room)
        {
            var lines = new List<string>();
            foreach (var p in gm.Producers.Producers)
                if (p.Data.requiredStudio == level) lines.Add(Loc.F("studio.unlock_producer", "name", "<b>" + p.Data.DisplayName + "</b>"));
            int artistCount = 0;
            string firstArtist = null;
            foreach (var a in gm.Artists.Artists)
                if (a.requiredStudioLevel == level) { artistCount++; if (firstArtist == null) firstArtist = a.displayName; }
            if (artistCount > 0) lines.Add(Loc.P("studio.unlock_artists", artistCount, "name", "<b>" + firstArtist + "</b>" + (artistCount > 1 ? " +" + (artistCount - 1) : "")));
            foreach (var g in gm.Genres.Genres)
                if (g.requiredStudioLevel == level && g.unlockCost > 0) lines.Add(Loc.F("studio.unlock_genre", "name", "<b>" + g.DisplayName + "</b>"));
            foreach (var al in gm.Albums.Albums)
                if (al.requiredStudio == level) lines.Add(Loc.F("studio.unlock_album", "name", "<b>" + al.title + "</b>"));
            lines.Add(Loc.F("studio.unlock_offline", "time", "<b>" + NumberFormatter.FormatDuration(room.offlineHours * 3600) + "</b>"));
            lines.Add(Loc.F("studio.unlock_viral", "mult", "<b>" + NumberFormatter.FormatMultiplier(room.viralChanceMultiplier) + "</b>"));
            if (room.unlocks != null) for (int i = 0; i < room.unlocks.Length; i++) lines.Add(room.Unlock(i));

            var sb = new StringBuilder();
            foreach (var l in lines) sb.Append("<color=#4DFFA8>●</color>  ").Append(l).Append('\n');
            return sb.ToString().TrimEnd('\n');
        }

        void OnUpgrade()
        {
            if (!gm.Studios.TryUpgrade()) return;
            window.Hide(); // the studio scene plays the move-in show (lights, equipment, celebration)
        }

        void Update()
        {
            if (!window.IsOpen) { enabled = false; return; }
            timer += Time.unscaledDeltaTime;
            if (timer < 0.2f) return;
            timer = 0f;
            RefreshProgress();
        }
    }
}
