using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// The big MAKE BEAT button: creates a beat, shows "+$5" floating up and bounces.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MakeBeatButton : MonoBehaviour
    {
        [SerializeField] TMP_Text rewardText;
        [SerializeField] FloatingTextSpawner floatingText;
        [SerializeField] RectTransform spawnPoint;
        [SerializeField] Color floatingTextColor = new Color(0.3f, 1f, 0.66f);
        [Tooltip("Random horizontal spread of the floating text, in UI units.")]
        [SerializeField] float spread = 160f;
        [Tooltip("How far above the spawn point the text appears, in UI units.")]
        [SerializeField] float verticalOffset = 150f;

        BeatManager beats;
        MultiplierStack income;
        ButtonPunch punch;

        void Start()
        {
            beats = GameManager.Instance.Beats;
            income = GameManager.Instance.Income;
            punch = GetComponent<ButtonPunch>();

            GetComponent<Button>().onClick.AddListener(OnClick);
            income.Changed += RefreshRewardText;
            GameManager.Instance.Producers.ProducerChanged += OnProducerChanged;
            RefreshRewardText();
            Loc.Changed += RefreshRewardText;
        }

        void OnDestroy()
        {
            Loc.Changed -= RefreshRewardText;
            if (income != null) income.Changed -= RefreshRewardText;
            if (GameManager.Instance != null) GameManager.Instance.Producers.ProducerChanged -= OnProducerChanged;
        }

        // Manual beats include a share of producer income, so hiring/upgrading changes the reward.
        void OnProducerChanged(Producer _) => RefreshRewardText();

        void OnClick()
        {
            var result = beats.MakeManualBeat();

            if (floatingText != null)
            {
                var origin = spawnPoint != null ? spawnPoint : (RectTransform)transform;
                var offset = new Vector2(Random.Range(-spread, spread), verticalOffset);
                var qualityConfig = beats.QualityConfig;
                floatingText.Spawn(BeatFeedback.RewardText(result, qualityConfig),
                    BeatFeedback.RewardColor(result, qualityConfig, floatingTextColor),
                    origin.position, offset, BeatFeedback.SizeMultiplier(result));
            }

            if (punch != null) punch.Punch();
        }

        void RefreshRewardText()
        {
            if (rewardText != null)
                rewardText.text = Loc.F("home.per_beat", "money", "+" + NumberFormatter.FormatMoney(beats.ManualBeatReward));
        }
    }
}
