using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Plays sound effects from a pool of AudioSources (sounds never cut each other off) and the looping music.
    /// Volume = clip volume × category (SFX/UI/Production) × master, × the player's Master/SFX/Music sliders.
    /// Use: AudioManager.Play(SoundId.Beat).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] AudioConfig config;
        [Tooltip("How many sounds can play at the same time.")]
        [SerializeField] int voices = 12;

        AudioSource[] sfxSources;
        AudioSource musicSource;
        int nextVoice;
        float[] lastPlayed;
        int[] lastVariant;
        GameSettings settings;
        float musicTarget;
        float duck = 1f, duckTimer;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            sfxSources = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                sfxSources[i] = s;
            }

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0f;
            musicSource.priority = 0;

            int count = System.Enum.GetValues(typeof(SoundId)).Length + 4;
            lastPlayed = new float[count];
            lastVariant = new int[count];
            for (int i = 0; i < count; i++) { lastPlayed[i] = -999f; lastVariant[i] = -1; }
        }

        void Start()
        {
            settings = GameManager.Instance != null ? GameManager.Instance.Settings : new GameSettings();
            settings.Changed += ApplyMusicSetting;

            if (config != null && config.music != null)
            {
                musicSource.clip = config.music;
                musicSource.Play();
            }
            ApplyMusicSetting();
        }

        void OnDestroy()
        {
            if (settings != null) settings.Changed -= ApplyMusicSetting;
            if (Instance == this) Instance = null;
        }

        /// <summary>Plays a sound effect if sound is enabled. Safe to call even without an AudioManager.</summary>
        public static void Play(SoundId id, float pitch = 1f) => Instance?.PlayInternal(id, pitch);

        void PlayInternal(SoundId id, float pitch)
        {
            if (config == null || (settings != null && !settings.soundOn)) return;
            var entry = config.Get(id);
            if (entry == null) return;

            var clip = PickClip(entry, (int)id);
            if (clip == null) return;

            int index = (int)id;
            float now = Time.unscaledTime;
            if (index < lastPlayed.Length)
            {
                if (now - lastPlayed[index] < entry.minInterval) return;
                lastPlayed[index] = now;
            }

            float player = settings != null ? settings.masterVolume * settings.sfxVolume : 1f;
            float volume = entry.volume * config.CategoryVolume(entry.category) * config.masterVolume * player;
            if (volume <= 0.001f) return;

            var source = sfxSources[nextVoice];
            nextVoice = (nextVoice + 1) % sfxSources.Length;
            source.pitch = pitch * (1f + Random.Range(-entry.pitchVariation, entry.pitchVariation));
            source.PlayOneShot(clip, volume);

            if (entry.duckMusic > 0f) Duck(1f - entry.duckMusic, clip.length);
        }

        AudioClip PickClip(SoundEntry entry, int index)
        {
            var v = entry.variants;
            if (v == null || v.Length == 0) return entry.clip;
            if (v.Length == 1) return v[0];

            // Random, but never the same variation twice in a row.
            int pick = Random.Range(0, v.Length);
            if (index < lastVariant.Length && pick == lastVariant[index]) pick = (pick + 1 + Random.Range(0, v.Length - 1)) % v.Length;
            if (index < lastVariant.Length) lastVariant[index] = pick;
            return v[pick];
        }

        /// <summary>Lowers the music to <paramref name="level"/> (0..1) for a moment, then brings it back.</summary>
        public void Duck(float level, float seconds)
        {
            duck = Mathf.Min(duck, Mathf.Clamp01(level));
            duckTimer = Mathf.Max(duckTimer, seconds);
            enabled = true;
        }

        bool held;

        /// <summary>Keeps the game music silent while something else plays music (concerts), then brings it back.</summary>
        public void HoldDuck(bool on)
        {
            held = on;
            enabled = true;
        }

        void ApplyMusicSetting()
        {
            bool on = settings == null || settings.musicOn;
            float player = settings != null ? settings.masterVolume * settings.musicVolume : 1f;
            musicTarget = on && config != null ? config.musicVolume * config.masterVolume * player : 0f;
            enabled = true; // fade in Update
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (duckTimer > 0f) duckTimer -= dt;
            else duck = Mathf.MoveTowards(duck, 1f, dt * 1.5f);

            float target = held ? 0f : musicTarget * duck;
            float speed = config != null && config.musicFadeSeconds > 0 ? Mathf.Max(0.05f, config.musicVolume) / config.musicFadeSeconds : 10f;
            if (duckTimer > 0f) speed *= 4f; // duck quickly
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, target, speed * dt);
            if (Mathf.Approximately(musicSource.volume, target) && duck >= 1f && duckTimer <= 0f && !held) enabled = false;
        }
    }
}
