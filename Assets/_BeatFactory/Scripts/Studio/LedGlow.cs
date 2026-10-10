using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>LED / RGB strip glow: slowly cycles colors (or keeps one color) and breathes with the music.</summary>
    [RequireComponent(typeof(Image))]
    public class LedGlow : MonoBehaviour
    {
        [SerializeField] Color color = new Color(1f, 0.3f, 0.85f, 0.5f);
        [Tooltip("RGB mode: hue cycles over time.")]
        [SerializeField] bool cycle = true;
        [SerializeField] float cycleSeconds = 12f;
        [SerializeField] float bpm = 85f;
        [SerializeField] float breathe = 0.25f;

        Image image;
        float time;

        void Awake() => image = GetComponent<Image>();

        void Update()
        {
            time += Time.unscaledDeltaTime;
            var c = color;
            if (cycle)
            {
                Color.RGBToHSV(color, out float h, out float s, out float v);
                c = Color.HSVToRGB(Mathf.Repeat(h + time / cycleSeconds, 1f), s, v);
            }
            float beat = Mathf.Pow(1f - Mathf.Repeat(time * bpm / 60f, 1f), 2f);
            c.a = color.a * (1f - breathe + breathe * beat);
            image.color = c;
        }
    }
}
