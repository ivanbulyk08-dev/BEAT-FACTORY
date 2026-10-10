using System;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Reusable modal window: dims the screen, blocks input behind it and pops the panel in/out.
    /// Put it on an always-active root; "window" is the child that gets shown/hidden.
    /// </summary>
    public class ModalWindow : MonoBehaviour
    {
        [SerializeField] GameObject window;
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform panel;
        [SerializeField] float showDuration = 0.3f;
        [SerializeField] float hideDuration = 0.18f;

        float time;
        bool opening;
        bool animating;

        public bool IsOpen { get; private set; }

        /// <summary>Open modals in the order they were opened (the Android Back button closes the last one).</summary>
        public static readonly System.Collections.Generic.List<ModalWindow> OpenStack = new System.Collections.Generic.List<ModalWindow>();

        /// <summary>Optional custom Back behaviour (e.g. COLLECT on the Welcome Back popup). Default: Hide().</summary>
        public Action BackAction;

        public void Back() { if (BackAction != null) BackAction(); else Hide(); }

        public event Action Opened;
        public event Action Closed;

        void Awake()
        {
            window.SetActive(false);
            enabled = false;
        }

        public void Show()
        {
            IsOpen = true;
            OpenStack.Remove(this);
            OpenStack.Add(this);
            opening = true;
            window.SetActive(true);
            window.transform.SetAsLastSibling();
            group.blocksRaycasts = true;
            group.interactable = true;
            time = 0f;
            animating = enabled = true;
            Apply(0f);
            Opened?.Invoke();
        }

        public void Hide()
        {
            if (!IsOpen) return;
            IsOpen = false;
            OpenStack.Remove(this);
            opening = false;
            group.interactable = false;
            time = 0f;
            animating = enabled = true;
        }

        void OnDestroy() => OpenStack.Remove(this);

        void Update()
        {
            if (!animating) { enabled = false; return; }
            time += Time.unscaledDeltaTime;
            float duration = opening ? showDuration : hideDuration;
            float p = Mathf.Clamp01(time / duration);
            Apply(p);

            if (p >= 1f)
            {
                animating = enabled = false;
                if (!opening)
                {
                    group.blocksRaycasts = false;
                    window.SetActive(false);
                    Closed?.Invoke();
                }
            }
        }

        /// <summary>Shrinks the panel uniformly when the screen is narrower/shorter than its design size (tall phones).</summary>
        float FitScale()
        {
            var parent = panel.parent as RectTransform;
            if (parent == null) return 1f;
            var r = parent.rect;
            float fx = (r.width - 40f) / Mathf.Max(1f, panel.sizeDelta.x);
            float fy = (r.height - 40f) / Mathf.Max(1f, panel.sizeDelta.y);
            return Mathf.Clamp(Mathf.Min(fx, fy), 0.5f, 1f);
        }

        void Apply(float p)
        {
            float fit = FitScale();
            if (opening)
            {
                group.alpha = Mathf.Clamp01(p * 2f);
                float s = Mathf.LerpUnclamped(0.85f, 1f, EaseOutBack(p)) * fit;
                panel.localScale = new Vector3(s, s, 1f);
            }
            else
            {
                group.alpha = 1f - p;
                float s = Mathf.Lerp(1f, 0.9f, p) * fit;
                panel.localScale = new Vector3(s, s, 1f);
            }
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
        }
    }
}
