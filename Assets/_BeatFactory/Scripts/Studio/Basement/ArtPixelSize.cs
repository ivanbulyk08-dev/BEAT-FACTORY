using UnityEngine;

namespace BeatFactory
{
    /// <summary>Keeps an overlay at a fixed size in painting pixels, whatever size the (aspect-fitted) painting is drawn at.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class ArtPixelSize : MonoBehaviour
    {
        public Vector2 sizeInArtPixels = new Vector2(100, 100);
        public Vector2 artSize = new Vector2(864, 1536);
        RectTransform rt, parent;
        Vector2 lastParent;

        void OnEnable()
        {
            rt = (RectTransform)transform;
            parent = rt.parent as RectTransform;
            lastParent = Vector2.zero;
        }

        void LateUpdate()
        {
            if (parent == null) return;
            var size = parent.rect.size;
            if (size == lastParent) return;
            lastParent = size;
            rt.sizeDelta = new Vector2(sizeInArtPixels.x * size.x / artSize.x, sizeInArtPixels.y * size.y / artSize.y);
        }
    }
}
