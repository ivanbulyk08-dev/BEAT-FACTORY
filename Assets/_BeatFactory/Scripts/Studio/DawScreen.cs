using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Animated screen of the fictional "BEATLAB" DAW (original design): transport + BPM, channel list,
    /// timeline with colorful clips and a playhead, mixer meters and a waveform.
    /// Idle: meters and waveform move gently. MAKE BEAT / producer beat: a new clip pops in,
    /// the playhead runs, meters jump. Layout is defined on a 400×250 grid and scales to any screen size.
    /// </summary>
    public class DawScreen : MonoBehaviour
    {
        [SerializeField] Sprite background;
        [SerializeField] TMP_FontAsset font;
        [Tooltip("React to the player's MAKE BEAT taps.")]
        [SerializeField] bool listenManual;
        [Tooltip("React to this producer's beats (-1 = none).")]
        [SerializeField] int producerIndex = -1;
        [Tooltip("Producers' screens keep the transport running.")]
        [SerializeField] bool alwaysPlaying;
        [SerializeField] float bpm = 85f;

        static readonly Color[] LaneColors =
        {
            new Color(1f, 0.24f, 0.78f), new Color(0.24f, 0.88f, 1f), new Color(0.3f, 1f, 0.66f),
            new Color(1f, 0.82f, 0.24f), new Color(0.71f, 0.36f, 1f), new Color(1f, 0.54f, 0.24f),
        };

        const float GridW = 400f, GridH = 250f;
        const float TimelineX = 84f, TimelineW = 312f;
        const int Lanes = 6, Steps = 16, MaxClips = 18;

        class Clip
        {
            public RectTransform rt;
            public Image flash;
            public float age;
        }

        RectTransform root, playhead, bootCover;
        Image playLed, bootImage;
        TMP_Text bootText;
        readonly List<Clip> clips = new List<Clip>();
        readonly List<RectTransform> meters = new List<RectTransform>();
        readonly List<Image> meterImages = new List<Image>();
        readonly List<RectTransform> wave = new List<RectTransform>();
        readonly List<RectTransform> faders = new List<RectTransform>();
        readonly List<RectTransform> notes = new List<RectTransform>();
        readonly List<RectTransform> knobs = new List<RectTransform>();
        RectTransform rollHead;
        int nextNote;
        float[] meterLevel;
        float playPos, active, pulse, boot = -1f, tick, time;
        bool built;
        GameManager gm;

        void Awake() => Build();

        void Start()
        {
            gm = GameManager.Instance;
            if (gm == null) return;
            if (listenManual) gm.Beats.BeatsCreated += OnBeats;
            if (producerIndex >= 0) gm.Producers.ProducerProduced += OnProducerBeat;
        }

        void OnDestroy()
        {
            if (gm == null) return;
            gm.Beats.BeatsCreated -= OnBeats;
            gm.Producers.ProducerProduced -= OnProducerBeat;
        }

        void OnBeats(BeatResult r)
        {
            if (r.Source == BeatSource.Manual) Pulse(true);
        }

        void OnProducerBeat(Producer p, BeatResult r)
        {
            if (p.Index == producerIndex) Pulse(Random.value < 0.6f);
        }

        // ------------------------------------------------------------------ public API

        /// <summary>A beat was made: transport runs, meters jump, optionally a new clip appears.</summary>
        public void Pulse(bool addClip)
        {
            if (!built) return;
            active = 3f;
            pulse = 1f;
            for (int i = 0; i < meterLevel.Length; i++) meterLevel[i] = Mathf.Max(meterLevel[i], 0.7f + Random.value * 0.3f);
            if (addClip && isActiveAndEnabled) AddClip(true);
        }

        /// <summary>Screen turns on (studio upgrade): dark → logo → interface.</summary>
        public void Boot()
        {
            if (!built) return;
            boot = 0f;
            bootCover.gameObject.SetActive(true);
        }

        // ------------------------------------------------------------------ building

        void Build()
        {
            if (built) return;
            built = true;
            root = (RectTransform)transform;

            var bg = Box("Background", 0, 0, GridW, GridH, Color.white);
            bg.GetComponent<Image>().sprite = background;

            // transport
            playLed = Box("PlayLed", 10, 226, 16, 18, new Color(0.3f, 1f, 0.66f, 0f)).GetComponent<Image>();
            Label("Logo", "BEATLAB", 66, 222, 120, 26, new Color(1f, 0.36f, 0.85f), FontStyles.Bold, TextAlignmentOptions.Left);
            Label("Bpm", Mathf.RoundToInt(bpm) + " BPM", 300, 226, 92, 18, new Color(0.3f, 1f, 0.66f), FontStyles.Bold, TextAlignmentOptions.Center);
            Label("Time", "01 : 03 : 12", 190, 226, 100, 18, new Color(0.54f, 0.52f, 0.69f), FontStyles.Normal, TextAlignmentOptions.Center);

            // initial clips
            for (int i = 0; i < 8; i++) AddClip(false, Random.Range(0, Steps - 3));

            // playhead
            playhead = Box("Playhead", TimelineX, 62, 2, 156, new Color(1f, 1f, 1f, 0.85f));

            // track names in the channel rack
            string[] names = { "KICK", "808", "SNARE", "HATS", "KEYS", "VOX" };
            for (int i = 0; i < names.Length; i++)
                Label("Track" + i, names[i], 22, 194 - i * 22 + 3, 56, 12, new Color(0.85f, 0.83f, 0.95f, 0.85f), FontStyles.Bold, TextAlignmentOptions.Left);

            // mixer meters + faders
            meterLevel = new float[6];
            for (int i = 0; i < 6; i++)
            {
                var m = Box("Meter" + i, 6 + i * 22 + 7, 8, 4, 40, new Color(0.3f, 1f, 0.66f));
                m.pivot = new Vector2(0.5f, 0f);
                meters.Add(m);
                meterImages.Add(m.GetComponent<Image>());
                faders.Add(Box("Fader" + i, 6 + i * 22 + 2, 14 + (i * 13) % 26, 14, 4, new Color(0.85f, 0.83f, 0.95f)));
            }

            // piano roll: notes + its own playhead
            for (int i = 0; i < 12; i++)
            {
                var n = Box("Note" + i, 0, 0, 0, 0, LaneColors[i % LaneColors.Length]);
                notes.Add(n);
                PlaceNote(n, Random.Range(0, 16), Random.Range(0, 8), Random.Range(1, 3));
            }
            rollHead = Box("RollHead", 156, 6, 1, 48, new Color(1f, 1f, 1f, 0.7f));

            // synth knobs (rotating indicators)
            for (int k = 0; k < 4; k++)
            {
                var knob = Box("Knob" + k, 284 + k * 30 - 7, 42 - 7, 14, 14, new Color(0, 0, 0, 0));
                var ind = Box("Indicator", 0, 0, 0, 0, Color.white, knob);
                ind.anchorMin = new Vector2(0.44f, 0.5f);
                ind.anchorMax = new Vector2(0.56f, 1f);
                knobs.Add(knob);
            }

            // waveform
            for (int i = 0; i < 24; i++)
            {
                var b = Box("Wave" + i, 271 + i * 5.1f, 16, 3, 2, new Color(0.24f, 0.88f, 1f, 0.9f));
                wave.Add(b);
            }

            // boot cover (studio upgrade)
            bootCover = Box("Boot", 0, 0, GridW, GridH, Color.black);
            bootImage = bootCover.GetComponent<Image>();
            bootText = Label("BootLogo", "BEATLAB", 0, 100, GridW, 50, new Color(1f, 0.36f, 0.85f), FontStyles.Bold, TextAlignmentOptions.Center, bootCover);
            bootCover.gameObject.SetActive(false);
        }

        RectTransform Box(string name, float x, float y, float w, float h, Color c, RectTransform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent != null ? parent : root, false);
            SetGrid(rt, x, y, w, h, parent);
            var img = go.GetComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return rt;
        }

        /// <summary>Places a child by 400×250 grid coordinates (anchors only, so it scales with the screen).</summary>
        static void SetGrid(RectTransform rt, float x, float y, float w, float h, RectTransform parent = null)
        {
            rt.anchorMin = new Vector2(x / GridW, y / GridH);
            rt.anchorMax = new Vector2((x + w) / GridW, (y + h) / GridH);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        TMP_Text Label(string name, string text, float x, float y, float w, float h, Color c, FontStyles style, TextAlignmentOptions align, RectTransform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent != null ? parent : root, false);
            SetGrid(rt, x, y, w, h, parent);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text;
            t.color = c;
            t.fontStyle = style;
            t.alignment = align;
            t.enableAutoSizing = true;
            t.fontSizeMin = 4;
            t.fontSizeMax = 72;
            t.characterSpacing = 2;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Piano roll note: 16 steps × 8 rows inside the roll panel.</summary>
        static void PlaceNote(RectTransform n, int step, int row, int len)
        {
            SetGrid(n, 156 + step * 6.6f + 0.4f, 6 + row * 6 + 0.6f, Mathf.Min(len, 16 - step) * 6.6f - 0.8f, 4.4f);
        }

        void AddClip(bool animate, int step = -1)
        {
            if (step < 0) step = Mathf.Clamp(Mathf.FloorToInt(playPos * Steps), 0, Steps - 2);
            int lane = Random.Range(0, Lanes);
            int len = Mathf.Min(Random.Range(2, 5), Steps - step);
            float stepW = TimelineW / Steps;
            var color = LaneColors[lane];
            var rt = Box("Clip", TimelineX + step * stepW + 0.5f, 194 - lane * 22 + 1, len * stepW - 1, 16, new Color(color.r, color.g, color.b, 0.85f));
            // clip header + note blips
            var header = Box("Header", 0, 0, 0, 0, new Color(1, 1, 1, 0.35f), rt);
            header.anchorMin = new Vector2(0, 0.78f);
            header.anchorMax = Vector2.one;
            for (int n = 0; n < len * 2; n++)
            {
                var blip = Box("Note", 0, 0, 0, 0, new Color(0, 0, 0, 0.35f), rt);
                float bx = (n + 0.2f) / (len * 2f);
                float by = 0.12f + Random.value * 0.45f;
                blip.anchorMin = new Vector2(bx, by);
                blip.anchorMax = new Vector2(bx + 0.6f / (len * 2f), by + 0.14f);
            }
            Clip clip = new Clip { rt = rt, age = animate ? 0f : 1f };
            if (animate)
            {
                // white flash over the new clip + new notes in the piano roll
                clip.flash = Box("Flash", 0, 0, 0, 0, Color.white, rt).GetComponent<Image>();
                clip.flash.rectTransform.anchorMin = Vector2.zero;
                clip.flash.rectTransform.anchorMax = Vector2.one;
                for (int i = 0; i < 3 && notes.Count > 0; i++)
                {
                    var n = notes[nextNote];
                    nextNote = (nextNote + 1) % notes.Count;
                    PlaceNote(n, Mathf.Clamp(step + i, 0, 15), Random.Range(0, 8), Random.Range(1, 3));
                    n.localScale = new Vector3(0.2f, 1f, 1f);
                }
            }
            if (playhead != null) playhead.SetAsLastSibling();
            if (bootCover != null) bootCover.SetAsLastSibling();

            clips.Add(clip);
            if (clips.Count > MaxClips)
            {
                Destroy(clips[0].rt.gameObject);
                clips.RemoveAt(0);
            }
        }

        // ------------------------------------------------------------------ animation

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;
            active = Mathf.Max(0f, active - dt);
            pulse = Mathf.Max(0f, pulse - dt * 2.5f);
            bool playing = alwaysPlaying || active > 0f;

            // playhead: 16 steps = 2 bars
            if (playing) playPos = Mathf.Repeat(playPos + dt * bpm / 60f / 8f, 1f);
            float px = (TimelineX + playPos * TimelineW) / GridW;
            playhead.anchorMin = new Vector2(px, playhead.anchorMin.y);
            playhead.anchorMax = new Vector2(px + 2f / GridW, playhead.anchorMax.y);
            playLed.color = new Color(0.3f, 1f, 0.66f, playing ? 0.35f + 0.25f * Mathf.Abs(Mathf.Sin(time * Mathf.PI * bpm / 60f)) : 0f);

            // new clips pop in
            foreach (var c in clips)
            {
                if (c.age >= 1f) continue;
                c.age = Mathf.Min(1f, c.age + dt * 4f);
                float s = c.age < 0.6f ? Mathf.Lerp(0.2f, 1.15f, c.age / 0.6f) : Mathf.Lerp(1.15f, 1f, (c.age - 0.6f) / 0.4f);
                c.rt.localScale = new Vector3(s, s, 1f);
                if (c.flash != null) c.flash.color = new Color(1, 1, 1, 0.9f * (1f - c.age));
            }
            foreach (var n in notes)
                if (n.localScale.x < 1f) n.localScale = new Vector3(Mathf.Min(1f, n.localScale.x + dt * 4f), 1f, 1f);

            float rx = (156f + playPos * 105.6f) / GridW;
            rollHead.anchorMin = new Vector2(rx, rollHead.anchorMin.y);
            rollHead.anchorMax = new Vector2(rx + 1f / GridW, rollHead.anchorMax.y);
            for (int k = 0; k < knobs.Count; k++)
            {
                float a = Mathf.Sin(time * (0.6f + k * 0.23f) + k) * 120f + pulse * 40f * (k % 2 == 0 ? 1 : -1);
                knobs[k].localEulerAngles = new Vector3(0, 0, a);
            }

            // boot sequence
            if (boot >= 0f)
            {
                boot += dt;
                bootText.alpha = Mathf.Clamp01(boot / 0.3f) * Mathf.Clamp01((1.2f - boot) / 0.3f);
                bootImage.color = new Color(0, 0, 0, Mathf.Clamp01((1.5f - boot) / 0.3f));
                if (boot > 1.5f) { boot = -1f; bootCover.gameObject.SetActive(false); }
            }

            // meters & waveform at ~30 Hz (cheap)
            tick += dt;
            if (tick < 1f / 30f) return;
            float step = tick;
            tick = 0f;

            float beatPhase = Mathf.Repeat(time * bpm / 60f, 1f);
            float kick = playing ? Mathf.Pow(1f - beatPhase, 3f) : 0f;
            for (int i = 0; i < meters.Count; i++)
            {
                float target = playing
                    ? 0.35f + 0.45f * kick * (i % 3 == 0 ? 1f : 0.6f) + Random.value * 0.2f
                    : 0.08f + 0.08f * Mathf.PerlinNoise(time * 1.5f, i * 3.1f);
                meterLevel[i] = Mathf.Max(target, meterLevel[i] - step * 1.6f);
                float lv = Mathf.Clamp01(meterLevel[i]);
                meters[i].localScale = new Vector3(1f, lv, 1f);
                meterImages[i].color = lv > 0.85f ? new Color(1f, 0.24f, 0.55f) : lv > 0.65f ? new Color(1f, 0.82f, 0.24f) : new Color(0.3f, 1f, 0.66f);
                float fy = (10f + 30f * (0.5f + 0.5f * Mathf.Sin(time * 0.4f + i * 1.7f)) * (playing ? 1f : 0.6f)) / GridH;
                faders[i].anchorMin = new Vector2(faders[i].anchorMin.x, fy);
                faders[i].anchorMax = new Vector2(faders[i].anchorMax.x, fy + 4f / GridH);
            }

            float amp = playing ? 0.45f + 0.5f * Mathf.Max(kick, pulse) : 0.12f;
            for (int i = 0; i < wave.Count; i++)
            {
                float n = Mathf.PerlinNoise(i * 0.35f - time * (playing ? 3f : 0.8f), 0.5f);
                float h = Mathf.Lerp(2f, 22f, n * amp);
                var w = wave[i];
                float cy = 16f / GridH, hh = h * 0.5f / GridH;
                w.anchorMin = new Vector2(w.anchorMin.x, cy - hh);
                w.anchorMax = new Vector2(w.anchorMax.x, cy + hh);
            }
        }
    }
}
