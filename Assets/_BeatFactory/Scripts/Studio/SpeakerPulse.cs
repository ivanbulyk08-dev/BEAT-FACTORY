using UnityEngine;

namespace BeatFactory
{
    /// <summary>Speaker cones pump to the music tempo and kick harder on every beat.</summary>
    public class SpeakerPulse : MonoBehaviour
    {
        [SerializeField] RectTransform[] cones;
        [SerializeField] float bpm = 85f;
        [SerializeField] float idleAmount = 0.04f;
        [SerializeField] float kickAmount = 0.18f;

        BeatManager beats;
        float kick;

        void Start()
        {
            beats = GameManager.Instance.Beats;
            beats.BeatsCreated += OnBeat;
        }

        void OnDestroy()
        {
            if (beats != null) beats.BeatsCreated -= OnBeat;
        }

        void OnBeat(BeatResult r)
        {
            if (r.Source != BeatSource.Offline) kick = 1f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            kick = Mathf.Max(0f, kick - dt * 5f);
            // sharp "thump" on every beat of the loop
            float phase = Mathf.Repeat(Time.unscaledTime * bpm / 60f, 1f);
            float thump = Mathf.Exp(-phase * 8f);
            float s = 1f + thump * idleAmount + kick * kickAmount;
            for (int i = 0; i < cones.Length; i++) cones[i].localScale = new Vector3(s, s, 1f);
        }
    }
}
