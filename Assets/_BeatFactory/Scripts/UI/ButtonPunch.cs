using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Makes a button feel "squishy": shrinks while held, bounces back on release.
    /// Add it to any Button. Call Punch() from code for a success bounce.
    /// </summary>
    public class ButtonPunch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] float pressedScale = 0.92f;
        [SerializeField] float punchScale = 1.08f;
        [SerializeField] float speed = 18f;
        public enum ClickStyle { None, Soft, Strong, Tab }

        [Tooltip("Sound when pressed. None for buttons whose action already plays a sound (MAKE BEAT, buy buttons).")]
        [SerializeField] ClickStyle clickStyle = ClickStyle.Soft;

        Selectable selectable;
        Vector3 baseScale;
        float current = 1f;
        float target = 1f;

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            baseScale = transform.localScale;
            enabled = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (selectable != null && !selectable.IsInteractable()) return;
            SetTarget(pressedScale);
            switch (clickStyle)
            {
                case ClickStyle.Soft: AudioManager.Play(SoundId.Click); break;
                case ClickStyle.Strong: AudioManager.Play(SoundId.ClickStrong); break;
                case ClickStyle.Tab: AudioManager.Play(SoundId.Tab); break;
            }
        }

        public void OnPointerUp(PointerEventData eventData) => SetTarget(1f);

        public void OnPointerExit(PointerEventData eventData)
        {
            if (target != 1f) SetTarget(1f);
        }

        /// <summary>Quick grow-and-settle bounce (e.g. after a successful purchase).</summary>
        public void Punch()
        {
            current = punchScale;
            SetTarget(1f);
        }

        void SetTarget(float value)
        {
            target = value;
            enabled = true;
        }

        void Update()
        {
            current = Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
            if (Mathf.Abs(current - target) < 0.001f)
            {
                current = target;
                enabled = false;
            }
            transform.localScale = baseScale * current;
        }
    }
}
