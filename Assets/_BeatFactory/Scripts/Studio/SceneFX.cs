using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Lightweight particle effects for the studio scene made of pooled UI Images:
    /// music notes, sparkles, confetti, plus a camera-like shake. Nothing is created during play.
    /// </summary>
    public class SceneFX : MonoBehaviour
    {
        [SerializeField] RectTransform layer;
        [SerializeField] RectTransform shakeTarget;
        [SerializeField] Sprite noteSprite;
        [SerializeField] Sprite sparkleSprite;
        [SerializeField] Sprite confettiSprite;
        [SerializeField] int poolSize = 70;
        [SerializeField] Color[] confettiColors =
        {
            new Color(1f, 0.24f, 0.78f), new Color(0.24f, 0.88f, 1f), new Color(1f, 0.82f, 0.24f), new Color(0.3f, 1f, 0.66f),
        };

        struct Particle
        {
            public RectTransform rt;
            public Image img;
            public Vector2 pos, vel;
            public float rot, rotVel, life, maxLife, size, gravity;
            public Color color;
            public bool active, fadeIn;
        }

        Particle[] particles;
        int active;
        int next;
        Vector2 shakeBase;
        float shakeTime, shakeDuration, shakeStrength;

        void Awake()
        {
            if (layer == null) layer = (RectTransform)transform;
            particles = new Particle[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject("P", typeof(RectTransform), typeof(Image));
                go.layer = gameObject.layer;
                var rt = (RectTransform)go.transform;
                rt.SetParent(layer, false);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                img.enabled = false;
                particles[i] = new Particle { rt = rt, img = img };
            }
            if (shakeTarget != null) shakeBase = shakeTarget.anchoredPosition;
            enabled = false;
        }

        Vector2 ToLocal(Vector3 world) => (Vector2)layer.InverseTransformPoint(world);

        void Spawn(Sprite sprite, Vector2 pos, Vector2 vel, float size, Color color, float life, float gravity, float rotVel)
        {
            ref var p = ref particles[next];
            next = (next + 1) % particles.Length;
            if (!p.active) active++;
            p.active = true;
            p.pos = pos;
            p.vel = vel;
            p.size = size;
            p.color = color;
            p.life = p.maxLife = life;
            p.gravity = gravity;
            p.rot = Random.Range(-20f, 20f);
            p.rotVel = rotVel;
            p.img.sprite = sprite;
            p.img.enabled = true;
            p.rt.sizeDelta = new Vector2(size, sprite == confettiSprite ? size * 0.5f : size);
            p.rt.SetAsLastSibling();
            enabled = true;
        }

        /// <summary>Music notes floating up from a point (e.g. the monitor).</summary>
        public void Notes(Vector3 worldPos, int count, Color color)
        {
            var origin = ToLocal(worldPos);
            for (int i = 0; i < count; i++)
                Spawn(noteSprite, origin + new Vector2(Random.Range(-30f, 30f), Random.Range(-10f, 10f)),
                    new Vector2(Random.Range(-40f, 40f), Random.Range(90f, 150f)), Random.Range(34f, 48f), color,
                    Random.Range(0.9f, 1.3f), -20f, Random.Range(-60f, 60f));
        }

        /// <summary>Sparkles bursting out of a point (upgrades, good beats).</summary>
        public void Sparkles(Vector3 worldPos, int count, Color color, float speed = 220f)
        {
            var origin = ToLocal(worldPos);
            for (int i = 0; i < count; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Spawn(sparkleSprite, origin, dir * Random.Range(speed * 0.4f, speed), Random.Range(18f, 36f), color,
                    Random.Range(0.45f, 0.8f), 0f, Random.Range(-200f, 200f));
            }
        }

        /// <summary>Confetti falling over the whole scene (VIRAL).</summary>
        public void Confetti(int count)
        {
            var r = layer.rect;
            for (int i = 0; i < count; i++)
            {
                var c = confettiColors[Random.Range(0, confettiColors.Length)];
                Spawn(confettiSprite, new Vector2(Random.Range(r.xMin, r.xMax), r.yMax + Random.Range(0f, 80f)),
                    new Vector2(Random.Range(-60f, 60f), Random.Range(-260f, -120f)), Random.Range(18f, 30f), c,
                    Random.Range(1.4f, 2.2f), -60f, Random.Range(-360f, 360f));
            }
        }

        public void Shake(float strength, float duration)
        {
            if (shakeTarget == null) return;
            if (shakeTime <= 0f) shakeBase = shakeTarget.anchoredPosition;
            shakeStrength = strength;
            shakeDuration = shakeTime = duration;
            enabled = true;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                if (!p.active) continue;
                p.life -= dt;
                if (p.life <= 0f)
                {
                    p.active = false;
                    p.img.enabled = false;
                    active--;
                    continue;
                }
                p.vel.y += p.gravity * dt;
                p.pos += p.vel * dt;
                p.rot += p.rotVel * dt;
                float t = p.life / p.maxLife;
                var c = p.color;
                c.a = Mathf.Clamp01(t * 2.5f);
                p.img.color = c;
                p.rt.anchoredPosition = p.pos;
                p.rt.localEulerAngles = new Vector3(0, 0, p.rot);
                float pop = t > 0.85f ? Mathf.Lerp(1f, 0.3f, (t - 0.85f) / 0.15f) : 1f;
                p.rt.localScale = new Vector3(pop, pop, 1f);
            }

            if (shakeTime > 0f)
            {
                shakeTime -= dt;
                float k = Mathf.Clamp01(shakeTime / shakeDuration);
                shakeTarget.anchoredPosition = shakeBase + Random.insideUnitCircle * shakeStrength * k;
                if (shakeTime <= 0f) shakeTarget.anchoredPosition = shakeBase;
            }

            if (active <= 0 && shakeTime <= 0f)
            {
                active = 0;
                enabled = false;
            }
        }
    }
}
