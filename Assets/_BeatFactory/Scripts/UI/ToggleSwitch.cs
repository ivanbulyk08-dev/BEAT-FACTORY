using System;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>iOS-style on/off switch with a sliding knob.</summary>
    [RequireComponent(typeof(Button))]
    public class ToggleSwitch : MonoBehaviour
    {
        [SerializeField] Image track;
        [SerializeField] RectTransform knob;
        [SerializeField] Color onColor = new Color(0.3f, 1f, 0.66f);
        [SerializeField] Color offColor = new Color(0.25f, 0.23f, 0.35f);
        [SerializeField] float speed = 14f;

        float position; // 0 = off, 1 = on
        float travel;

        public bool IsOn { get; private set; }
        public event Action<bool> Changed;

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => SetIsOn(!IsOn, true));
            travel = (((RectTransform)transform).rect.width - knob.rect.width) * 0.5f - 6f;
        }

        public void SetIsOn(bool on, bool notify)
        {
            IsOn = on;
            enabled = true;
            if (notify) Changed?.Invoke(on);
        }

        /// <summary>Jump to the state without animation (used when the screen opens).</summary>
        public void SetImmediate(bool on)
        {
            IsOn = on;
            position = on ? 1f : 0f;
            Apply();
        }

        void Update()
        {
            position = Mathf.MoveTowards(position, IsOn ? 1f : 0f, speed * Time.unscaledDeltaTime);
            Apply();
            if (Mathf.Approximately(position, IsOn ? 1f : 0f)) enabled = false;
        }

        void Apply()
        {
            knob.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, position), 0f);
            track.color = Color.Lerp(offColor, onColor, position);
        }
    }
}
