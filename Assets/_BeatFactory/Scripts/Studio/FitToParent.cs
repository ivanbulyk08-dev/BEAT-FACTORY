using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Scales a fixed-size child (the 1000×560 room) to cover its parent on any screen shape,
    /// so the art never shows empty edges on tall or wide phones.
    /// </summary>
    [ExecuteAlways]
    public class FitToParent : MonoBehaviour
    {
        [SerializeField] RectTransform child;
        [SerializeField] Vector2 designSize = new Vector2(1000, 560);

        void OnEnable() => Fit();
        void OnRectTransformDimensionsChange() => Fit();

        void Fit()
        {
            if (child == null) return;
            var r = ((RectTransform)transform).rect;
            if (r.width <= 0 || r.height <= 0) return;
            float s = Mathf.Max(r.width / designSize.x, r.height / designSize.y);
            child.localScale = new Vector3(s, s, 1f);
        }
    }
}
