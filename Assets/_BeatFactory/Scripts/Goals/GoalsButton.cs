using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>GOALS button in the boost bar: opens the Goals screen and shows how many rewards are waiting.</summary>
    public class GoalsButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] GameObject badge;
        [SerializeField] TMP_Text badgeText;

        float timer;

        void Start()
        {
            button.onClick.AddListener(() => { if (GoalsScreen.Instance != null) GoalsScreen.Instance.Show(); });
            Refresh();
        }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < 0.5f) return;
            timer = 0;
            Refresh();
        }

        void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Missions == null) return;
            int n = gm.Missions.ClaimableCount + gm.Challenges.ClaimableCount;
            badge.SetActive(n > 0);
            badgeText.text = n > 9 ? "9+" : n.ToString();
        }
    }
}
