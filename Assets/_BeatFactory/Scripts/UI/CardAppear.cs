using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Cards smoothly appear when shown: they scale up from slightly smaller with a small stagger
    /// based on their position in the list. Uses scale only, so layout groups are not disturbed.
    /// </summary>
    public class CardAppear : MonoBehaviour
    {
        [SerializeField] float duration = 0.32f;
        [SerializeField] float stagger = 0.045f;
        [SerializeField] float fromScale = 0.88f;

        float t = -1f;
        float delay;

        void OnEnable()
        {
            delay = Mathf.Min(transform.GetSiblingIndex(), 8) * stagger;
            t = 0f;
            transform.localScale = new Vector3(fromScale, fromScale, 1f);
        }

        void Update()
        {
            if (t < 0f) return;
            if (delay > 0f) { delay -= Time.unscaledDeltaTime; return; }
            t += Time.unscaledDeltaTime / duration;
            float p = Mathf.Clamp01(t);
            // ease-out-back: overshoots a hair, then settles
            const float c1 = 1.4f, c3 = c1 + 1f;
            float e = 1f + c3 * Mathf.Pow(p - 1f, 3) + c1 * Mathf.Pow(p - 1f, 2);
            float s = Mathf.LerpUnclamped(fromScale, 1f, e);
            transform.localScale = new Vector3(s, s, 1f);
            if (p >= 1f)
            {
                transform.localScale = Vector3.one;
                t = -1f;
            }
        }
    }

}
