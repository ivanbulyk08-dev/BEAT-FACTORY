using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>Small UI confetti burst (contracts, album releases). Spawned pieces clean themselves up.</summary>
    public class UIBurst : MonoBehaviour
    {
        static readonly Color[] DefaultColors =
        {
            new Color(1f, 0.24f, 0.78f), new Color(0.24f, 0.88f, 1f), new Color(0.3f, 1f, 0.66f), new Color(1f, 0.82f, 0.24f),
        };

        RectTransform[] pieces;
        Vector2[] velocity;
        float[] spin;
        float life = 1.6f, t;

        public static void Spawn(RectTransform parent, Vector2 localPos, int count, Sprite sprite, Color[] colors = null)
        {
            colors = colors ?? DefaultColors;
            var go = new GameObject("Burst", typeof(RectTransform));
            var root = (RectTransform)go.transform;
            root.SetParent(parent, false);
            root.anchoredPosition = localPos;
            var burst = go.AddComponent<UIBurst>();
            burst.pieces = new RectTransform[count];
            burst.velocity = new Vector2[count];
            burst.spin = new float[count];
            for (int i = 0; i < count; i++)
            {
                var p = new GameObject("p", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)p.transform;
                rt.SetParent(root, false);
                rt.sizeDelta = new Vector2(Random.Range(14f, 26f), Random.Range(8f, 14f));
                var img = p.GetComponent<Image>();
                img.sprite = sprite;
                img.color = colors[i % colors.Length];
                img.raycastTarget = false;
                float a = Random.Range(0f, Mathf.PI * 2f);
                burst.pieces[i] = rt;
                burst.velocity[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.8f + 0.6f) * Random.Range(500f, 1100f);
                burst.spin[i] = Random.Range(-720f, 720f);
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            for (int i = 0; i < pieces.Length; i++)
            {
                velocity[i] += Vector2.down * 1800f * dt;
                velocity[i] *= 1f - 1.2f * dt;
                pieces[i].anchoredPosition += velocity[i] * dt;
                pieces[i].localEulerAngles += new Vector3(0, 0, spin[i] * dt);
                pieces[i].localScale = Vector3.one * Mathf.Clamp01((life - t) * 2f);
            }
            if (t >= life) Destroy(gameObject);
        }
    }

}
