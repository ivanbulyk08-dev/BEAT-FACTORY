using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Animated equalizer bars inside the studio. Bars "pump" every time beats are created,
    /// and better studios show more bars. Uses a fixed set of bars (max 32) and one Update.
    /// </summary>
    public class EqualizerVisualizer : MonoBehaviour
    {
        [SerializeField] RectTransform container;
        [SerializeField] int maxBars = 32;
        [SerializeField] float spacing = 10f;
        [SerializeField] float minHeight = 0.08f;
        [Tooltip("How much a beat boosts the bars (0-1).")]
        [SerializeField] float beatKick = 0.45f;
        [SerializeField] float manualBeatKick = 0.7f;
        [SerializeField] float energyDecay = 2.5f;
        [SerializeField] float idleEnergy = 0.15f;
        [SerializeField] float barAlpha = 0.07f;
        [Tooltip("Accent color follows the current genre.")]
        [SerializeField] bool useGenreColor = true;

        RectTransform[] bars;
        Image[] barImages;
        float[] phases;
        int visibleBars = 6;
        float energy;
        float width;
        BeatManager beats;
        GenreManager genres;

        void Awake()
        {
            if (container == null) container = (RectTransform)transform;
            bars = new RectTransform[maxBars];
            barImages = new Image[maxBars];
            phases = new float[maxBars];

            for (int i = 0; i < maxBars; i++)
            {
                var go = new GameObject("Bar", typeof(RectTransform), typeof(Image));
                go.layer = gameObject.layer;
                var rt = (RectTransform)go.transform;
                rt.SetParent(container, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0.5f, 0);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                bars[i] = rt;
                barImages[i] = img;
                phases[i] = Random.Range(0f, 100f);
            }
        }

        void Start()
        {
            var gm = GameManager.Instance;
            beats = gm.Beats;
            genres = gm.Genres;
            beats.BeatsCreated += OnBeatsCreated;
            if (useGenreColor)
            {
                genres.CurrentChanged += OnGenreChanged;
                OnGenreChanged(genres.Current);
            }
            Layout();
        }

        void OnDestroy()
        {
            if (beats != null) beats.BeatsCreated -= OnBeatsCreated;
            if (genres != null) genres.CurrentChanged -= OnGenreChanged;
        }

        public void SetBarCount(int count)
        {
            visibleBars = Mathf.Clamp(count, 1, maxBars);
            if (bars != null) Layout();
        }

        void OnBeatsCreated(BeatResult result)
        {
            float kick = result.Source == BeatSource.Manual ? manualBeatKick : beatKick;
            if (result.Quality >= BeatQuality.Great) kick = 1f;
            energy = Mathf.Min(1f, energy + kick);
        }

        void OnGenreChanged(GenreData genre)
        {
            if (genre == null) return;
            var c = genre.accentColor;
            c.a = barAlpha;
            for (int i = 0; i < barImages.Length; i++) barImages[i].color = c;
        }

        void Layout()
        {
            width = container.rect.width;
            float barWidth = Mathf.Max(4f, (width - spacing * (visibleBars + 1)) / visibleBars);
            for (int i = 0; i < bars.Length; i++)
            {
                bool visible = i < visibleBars;
                bars[i].gameObject.SetActive(visible);
                if (!visible) continue;
                bars[i].sizeDelta = new Vector2(barWidth, 0);
                bars[i].anchoredPosition = new Vector2(spacing + barWidth * 0.5f + i * (barWidth + spacing), 0);
            }
        }

        void OnRectTransformDimensionsChange()
        {
            if (bars != null && container != null && !Mathf.Approximately(container.rect.width, width)) Layout();
        }

        float frameTimer;

        void Update()
        {
            // 30 updates/s is plenty for decoration and halves the UI rebuild cost of the card.
            frameTimer += Time.unscaledDeltaTime;
            if (frameTimer < 1f / 30f) return;
            float dt = frameTimer;
            frameTimer = 0f;

            energy = Mathf.Max(idleEnergy, energy - energyDecay * dt);
            float height = container.rect.height;
            float t = Time.unscaledTime;

            for (int i = 0; i < visibleBars; i++)
            {
                // Each bar wobbles at its own speed; overall height follows the energy.
                float wave = Mathf.PerlinNoise(phases[i], t * 3f);
                float h = Mathf.Lerp(minHeight, 1f, wave * energy + minHeight);
                var size = bars[i].sizeDelta;
                size.y = h * height;
                bars[i].sizeDelta = size;
            }
        }
    }
}
