using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>Soft glow that breathes with the music (e.g. behind MAKE BEAT). Cheap: only changes alpha/scale.</summary>
    [RequireComponent(typeof(Graphic))]
    public class PulseGlow : MonoBehaviour
    {
        [SerializeField] float minAlpha = 0.12f;
        [SerializeField] float maxAlpha = 0.38f;
        [SerializeField] float bpm = 85f;
        [SerializeField] float scaleAmount = 0.015f;

        Graphic graphic;
        Color baseColor;
        float time;

        void Awake()
        {
            graphic = GetComponent<Graphic>();
            baseColor = graphic.color;
        }

        void Update()
        {
            time += Time.unscaledDeltaTime;
            float beat = Mathf.Pow(1f - Mathf.Repeat(time * bpm / 60f, 1f), 2.5f);
            var c = baseColor;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, beat);
            graphic.color = c;
            transform.localScale = Vector3.one * (1f + scaleAmount * beat);
        }
    }

}
