using System.Collections;
using System.Threading;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Concert sound: the artist's own original soundtrack (rendered on a worker thread), the crowd bed and pyro/spark SFX.
    /// Also the show's clock: visuals read Time / Beat / kicks / sections from here, so lights hit on the music.
    /// Mix: Concert Music (Settings → Concert Music on/off + volume) · crowd & pyro follow the SFX volume · the game's
    /// background music is ducked while the concert plays. Fades in at the start and out at the end (never cut).
    /// </summary>
    public class ConcertAudio : MonoBehaviour
    {
        AudioSource music, crowd, sfx;
        AudioClip whoosh, crackle;
        ConcertTrack track;
        Thread worker;
        volatile bool ready;
        volatile int generation;
        float fallbackTime;
        bool playing;
        float musicFade = 1f, fadeTarget = 1f, fadeSpeed = 1f;
        GameSettings settings;

        public ConcertTrack Track => ready ? track : null;
        public bool IsReady => ready;
        public ConcertMusicProfile Profile { get; private set; }

        /// <summary>Show time in seconds (music position when playing).</summary>
        public float Time => playing && music != null && music.isPlaying && music.clip != null ? music.time : fallbackTime;
        public float Bpm => Profile != null ? Profile.bpm : 120f;
        public float Beat => Time / (60f / Bpm);

        void Awake()
        {
            music = Source(false);
            crowd = Source(false);
            sfx = Source(false);
            whoosh = Clip("Whoosh", ConcertTrackSynth.Whoosh(7));
            crackle = Clip("Crackle", ConcertTrackSynth.Crackle(11));
        }

        AudioSource Source(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0;
            return s;
        }

        static AudioClip Clip(string name, float[] data)
        {
            var c = AudioClip.Create(name, data.Length, 1, ConcertTrackSynth.Rate, false);
            c.SetData(data, 0);
            return c;
        }

        /// <summary>Starts rendering the artist's soundtrack in the background.</summary>
        public void Prepare(ArtistData artist, float popularity)
        {
            Stop();
            ready = false;
            Profile = ArtistConcertMusic.For(artist);
            var p = Profile;
            float size = Mathf.Clamp01(popularity / 100f);
            int seed = (artist != null ? artist.id.GetHashCode() : 1) ^ System.Environment.TickCount;
            int gen = ++generation;
            track = null;
            worker = new Thread(() =>
            {
                ConcertTrack result = null;
                try { result = ConcertTrackSynth.Render(p, size, seed); }
                catch (System.Exception e) { Debug.LogException(e); }
                if (gen != generation) return; // a newer concert started meanwhile
                track = result;
                ready = result != null;
            }) { IsBackground = true };
            worker.Start();
        }

        float MusicVolume
        {
            get
            {
                if (settings == null && GameManager.Instance != null) settings = GameManager.Instance.Settings;
                if (settings == null) return 0.8f;
                return settings.concertMusicOn ? settings.masterVolume * settings.concertMusicVolume * 0.85f : 0f;
            }
        }

        float SfxVolume
        {
            get
            {
                if (settings == null && GameManager.Instance != null) settings = GameManager.Instance.Settings;
                if (settings == null) return 0.6f;
                return settings.soundOn ? settings.masterVolume * settings.sfxVolume : 0f;
            }
        }

        /// <summary>Waits for the render (slow phones may need a few seconds; max 6 s), then fades the concert in.</summary>
        public IEnumerator Begin()
        {
            for (float t = 0; t < 6f && !ready; t += UnityEngine.Time.unscaledDeltaTime) yield return null;
            fallbackTime = 0;
            playing = true;
            AudioManager.Instance?.HoldDuck(true);
            if (!ready) yield break;
            if (music.clip != null) Destroy(music.clip);
            if (crowd.clip != null) Destroy(crowd.clip);
            music.clip = Clip("ConcertMusic", track.music);
            crowd.clip = Clip("Crowd", track.crowd);
            musicFade = 0f;
            fadeTarget = 1f;
            fadeSpeed = 1f / 0.6f;
            music.Play();
            crowd.Play();
        }

        /// <summary>Music fades out; the crowd keeps cheering a bit longer.</summary>
        public void FadeOut(float seconds)
        {
            fadeTarget = 0f;
            fadeSpeed = 1f / Mathf.Max(0.1f, seconds);
        }

        public void Stop()
        {
            playing = false;
            if (music != null) music.Stop();
            if (crowd != null) crowd.Stop();
            AudioManager.Instance?.HoldDuck(false);
        }

        /// <summary>Jumps the music to a time (skip to the end of the show).</summary>
        public void Seek(float seconds)
        {
            if (music != null && music.clip != null) music.time = Mathf.Clamp(seconds, 0, music.clip.length - 0.05f);
            if (crowd != null && crowd.clip != null) crowd.time = Mathf.Clamp(seconds, 0, crowd.clip.length - 0.05f);
            fallbackTime = seconds;
        }

        public void Pyro(float pitch = 1f)
        {
            sfx.pitch = pitch;
            sfx.PlayOneShot(whoosh, 0.55f * SfxVolume);
        }

        public void Sparks() => sfx.PlayOneShot(crackle, 0.35f * SfxVolume);

        void Update()
        {
            if (!playing) return;
            float dt = UnityEngine.Time.unscaledDeltaTime;
            fallbackTime += dt;
            musicFade = Mathf.MoveTowards(musicFade, fadeTarget, fadeSpeed * dt);
            music.volume = MusicVolume * musicFade;
            crowd.volume = SfxVolume * 0.5f * Mathf.Max(musicFade, 0.6f);
            if (fadeTarget <= 0f && musicFade <= 0f && !crowd.isPlaying) Stop();
        }

        void OnDestroy() => AudioManager.Instance?.HoldDuck(false);
    }
}
