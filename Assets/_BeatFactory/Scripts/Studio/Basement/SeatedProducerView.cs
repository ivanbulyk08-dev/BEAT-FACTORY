using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// The producer at the basement desk (two layers: body + head). Idle: slow breathing and a light head nod on the
    /// beat of the studio music; every MAKE BEAT gives a stronger nod that eases back. Lightweight: two transforms per frame.
    /// </summary>
    public class SeatedProducerView : MonoBehaviour
    {
        [SerializeField] RectTransform body;
        [SerializeField] RectTransform head;
        [SerializeField] float bpm = 92f;
        [SerializeField] float idleNod = 2.2f, beatNod = 6f;

        float t, hype;

        void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.Beats.BeatsCreated += OnBeat;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.Beats.BeatsCreated -= OnBeat;
        }

        void OnBeat(BeatResult r)
        {
            if (r.Source == BeatSource.Manual) hype = 1f;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            t += dt;
            hype = Mathf.MoveTowards(hype, 0f, dt * 1.2f);
            float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(Mathf.PI * t * bpm / 60f)), 3f);
            if (head != null) head.localEulerAngles = new Vector3(0, 0, -(idleNod + beatNod * hype) * beat * Mathf.Sign(head.localScale.x)); // mirrored rigs nod forward too
            if (body != null) body.localScale = new Vector3(Mathf.Sign(body.localScale.x), 1f + 0.006f * Mathf.Sin(t * 1.6f), 1f);
        }
    }
}
