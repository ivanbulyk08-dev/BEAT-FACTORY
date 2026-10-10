using TMPro;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Shows short texts like "+$5" that pop, float upward and fade out.
    /// Uses a fixed pool of text objects (no Instantiate/Destroy during play) and
    /// turns its own Update() off when nothing is flying.
    /// </summary>
    public class FloatingTextSpawner : MonoBehaviour
    {
        [SerializeField] RectTransform layer;
        [SerializeField] int poolSize = 16;
        [SerializeField] float duration = 0.9f;
        [SerializeField] float riseDistance = 220f;
        [SerializeField] float fontSize = 64f;

        class Item
        {
            public RectTransform rect;
            public TextMeshProUGUI text;
            public Vector2 start;
            public Color color;
            public float time;
            public bool active;
        }

        Item[] items;
        int activeCount;
        int nextIndex;

        void Awake()
        {
            if (layer == null) layer = (RectTransform)transform;

            items = new Item[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject("FloatingText", typeof(RectTransform));
                go.layer = gameObject.layer;
                var rt = (RectTransform)go.transform;
                rt.SetParent(layer, false);
                rt.sizeDelta = new Vector2(600, 120);

                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = fontSize;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.raycastTarget = false;
                tmp.enabled = false;

                items[i] = new Item { rect = rt, text = tmp };
            }
            enabled = false;
        }

        /// <summary>
        /// Spawns a floating text at a screen position (for an Overlay canvas: someTransform.position).
        /// <paramref name="localOffset"/> is in UI units, useful for small random spread.
        /// </summary>
        public void Spawn(string message, Color color, Vector3 screenPosition, Vector2 localOffset = default, float sizeMultiplier = 1f)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenPosition, null, out var local);

            // Reuse the next pooled item (the oldest one if all are busy).
            var item = items[nextIndex];
            nextIndex = (nextIndex + 1) % items.Length;
            if (!item.active) activeCount++;

            item.active = true;
            item.time = 0f;
            item.start = local + localOffset;
            item.color = color;
            item.text.text = message;
            item.text.fontSize = fontSize * sizeMultiplier;
            item.text.color = color;
            item.text.enabled = true;
            item.rect.SetAsLastSibling();
            Apply(item, 0f);

            enabled = true;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (!item.active) continue;

                item.time += dt;
                float p = item.time / duration;
                if (p >= 1f)
                {
                    item.active = false;
                    item.text.enabled = false;
                    activeCount--;
                    continue;
                }
                Apply(item, p);
            }

            if (activeCount <= 0)
            {
                activeCount = 0;
                enabled = false;
            }
        }

        void Apply(Item item, float p)
        {
            float rise = 1f - (1f - p) * (1f - p); // ease-out
            item.rect.anchoredPosition = item.start + Vector2.up * (riseDistance * rise);

            // Pop in: 0.6 -> 1.15 -> 1.0 during the first 20%.
            float scale = p < 0.1f ? Mathf.Lerp(0.6f, 1.15f, p / 0.1f)
                        : p < 0.2f ? Mathf.Lerp(1.15f, 1f, (p - 0.1f) / 0.1f)
                        : 1f;
            item.rect.localScale = new Vector3(scale, scale, 1f);

            var c = item.color;
            c.a = p < 0.6f ? 1f : 1f - (p - 0.6f) / 0.4f;
            item.text.color = c;
        }
    }
}
