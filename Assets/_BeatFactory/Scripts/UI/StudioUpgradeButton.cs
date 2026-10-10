using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>Button in the studio card that buys the next studio. Hidden at the final studio.</summary>
    [RequireComponent(typeof(Button))]
    public class StudioUpgradeButton : MonoBehaviour
    {
        [SerializeField] TMP_Text labelText;
        [SerializeField] TMP_Text costText;

        StudioManager studios;
        EconomyManager economy;
        Button button;
        ButtonPunch punch;

        void Start()
        {
            var gm = GameManager.Instance;
            studios = gm.Studios;
            economy = gm.Economy;
            button = GetComponent<Button>();
            punch = GetComponent<ButtonPunch>();

            button.onClick.AddListener(OnClick);
            studios.CurrentChanged += OnStudioChanged;
            economy.MoneyChanged += OnMoneyChanged;
            Loc.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (studios != null) studios.CurrentChanged -= OnStudioChanged;
            if (economy != null) economy.MoneyChanged -= OnMoneyChanged;
            Loc.Changed -= Refresh;
        }

        void OnClick()
        {
            if (studios.TryUpgrade() && punch != null) punch.Punch();
        }

        void OnStudioChanged(StudioData _) => Refresh();

        void OnMoneyChanged(double _) => button.interactable = studios.CanUpgrade();

        public void Refresh()
        {
            var next = studios.Next;
            if (next == null)
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);

            labelText.text = Loc.T("studio.upgrade_button");
            costText.text = NumberFormatter.FormatMoney(next.upgradeCost);
            button.interactable = studios.CanUpgrade();
        }
    }
}
