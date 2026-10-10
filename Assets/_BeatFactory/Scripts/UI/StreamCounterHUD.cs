using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Small "► 12.5K streams" counter in the top bar. Tapping it opens the MUSIC tab.
    /// </summary>
    public class StreamCounterHUD : MonoBehaviour
    {
        [SerializeField] TMP_Text text;
        [SerializeField] Button button;
        [SerializeField] ScreenNavigator navigator;
        [SerializeField] string screenName = "MUSIC";
        [SerializeField] float refreshInterval = 0.25f;
        [SerializeField] float punchScale = 1.15f;

        StreamManager streams;
        double lastShown = -1;
        float timer;
        float punch;

        void Start()
        {
            streams = GameManager.Instance.Streams;
            if (button != null) button.onClick.AddListener(() => { if (navigator != null) navigator.Show(screenName); });
            Refresh();
            Loc.Changed += ForceRefresh;
        }

        void OnDestroy() => Loc.Changed -= ForceRefresh;

        void ForceRefresh() { lastShown = -1; Refresh(); }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer >= refreshInterval)
            {
                timer = 0f;
                Refresh();
            }

            if (punch > 0f)
            {
                punch -= Time.unscaledDeltaTime;
                text.transform.localScale = Vector3.one * Mathf.Lerp(1f, punchScale, Mathf.Clamp01(punch / 0.15f));
            }
        }

        void Refresh()
        {
            double s = streams.Streams;
            if (s == lastShown) return;
            if (s > lastShown && lastShown >= 0) punch = 0.15f;
            lastShown = s;
            text.text = "► " + Loc.P("count.streams", s);
        }
    }
}
