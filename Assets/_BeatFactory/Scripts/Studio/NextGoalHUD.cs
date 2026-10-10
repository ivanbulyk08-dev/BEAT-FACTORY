using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// "NEXT GOAL" widget on the studio: the next room, a progress bar and $have / $price.
    /// Pulses when the room is affordable. Tapping it opens the room upgrade screen.
    /// </summary>
    public class NextGoalHUD : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text amountText;
        [SerializeField] RectTransform fill;
        [SerializeField] Image fillImage;
        [SerializeField] Image glow;
        [SerializeField] Color progressColor = new Color(0.24f, 0.88f, 1f);
        [SerializeField] Color readyColor = new Color(0.95f, 0.76f, 0.31f);

        GameManager gm;
        float timer, pulse;

        void Start()
        {
            gm = GameManager.Instance;
            button.onClick.AddListener(() => { if (StudioUpgradePopup.Instance != null) StudioUpgradePopup.Instance.Open(); });
            Refresh();
        }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            pulse += Time.unscaledDeltaTime;
            if (timer >= 0.25f)
            {
                timer = 0f;
                Refresh();
            }
            if (glow != null && glow.enabled)
            {
                var c = glow.color;
                c.a = 0.25f + 0.25f * Mathf.Sin(pulse * 5f);
                glow.color = c;
            }
        }

        void Refresh()
        {
            var next = gm.Studios.Next;
            if (next == null)
            {
                titleText.text = Loc.T("studio.top_room");
                amountText.text = "";
                fill.anchorMax = new Vector2(1, 1);
                fillImage.color = readyColor;
                if (glow != null) glow.enabled = false;
                return;
            }
            double money = gm.Economy.Money, cost = next.upgradeCost;
            bool ready = money >= cost;
            titleText.text = Loc.F(ready ? "studio.goal_ready" : "studio.goal_next", "studio", Loc.Upper(next.DisplayName));
            amountText.text = NumberFormatter.FormatMoney(System.Math.Min(money, cost)) + " / " + NumberFormatter.FormatMoney(cost);
            fill.anchorMax = new Vector2((float)System.Math.Min(1.0, money / cost), 1);
            fillImage.color = ready ? readyColor : progressColor;
            if (glow != null) glow.enabled = ready;
        }
    }
}
