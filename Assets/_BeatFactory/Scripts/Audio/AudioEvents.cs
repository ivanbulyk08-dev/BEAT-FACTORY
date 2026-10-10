using System.Collections.Generic;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Connects gameplay events to sounds, so gameplay code never has to know about audio.
    /// Button clicks are played by ButtonPunch (style per button).
    /// </summary>
    public class AudioEvents : MonoBehaviour
    {
        [Tooltip("Identity sound per producer index (Beginner, Melody, Drum, Sample, Sound Designer, Hitmaker, Superstar).")]
        [SerializeField] SoundId[] producerSounds =
        {
            SoundId.ProducerBeginner, SoundId.ProducerMelody, SoundId.ProducerDrums,
            SoundId.ProducerDrums, SoundId.ProducerMelody, SoundId.ProducerMelody, SoundId.ProducerDrums,
        };

        readonly Dictionary<Producer, int> lastLevels = new Dictionary<Producer, int>();
        GameManager gm;

        void Start()
        {
            gm = GameManager.Instance;
            foreach (var p in gm.Producers.Producers) lastLevels[p] = p.Level;

            gm.Beats.BeatsCreated += OnBeats;
            gm.Producers.ProducerChanged += OnProducerChanged;
            gm.Genres.GenreUnlocked += OnUnlock;
            gm.Studios.StudioUpgraded += OnStudio;
            gm.Artists.ArtistSigned += OnUnlock;
            gm.Streams.PlatformReleased += OnUnlock;
            gm.Streams.CashedOut += OnCashOut;
            gm.Offline.Collected += OnCollect;
            gm.Prestige.Prestiged += OnPrestige;
            gm.Boosts.BoostStarted += OnBoost;
        }

        void OnDestroy()
        {
            if (gm == null) return;
            gm.Beats.BeatsCreated -= OnBeats;
            gm.Producers.ProducerChanged -= OnProducerChanged;
            gm.Genres.GenreUnlocked -= OnUnlock;
            gm.Studios.StudioUpgraded -= OnStudio;
            gm.Artists.ArtistSigned -= OnUnlock;
            gm.Streams.PlatformReleased -= OnUnlock;
            gm.Streams.CashedOut -= OnCashOut;
            gm.Offline.Collected -= OnCollect;
            gm.Prestige.Prestiged -= OnPrestige;
            gm.Boosts.BoostStarted -= OnBoost;
        }

        void OnBeats(BeatResult r)
        {
            if (r.Source == BeatSource.Offline) return;
            switch (r.Quality)
            {
                case BeatQuality.Viral:
                    lastViralTime = Time.unscaledTime;
                    AudioManager.Play(SoundId.Viral);
                    return;
                case BeatQuality.Hit: AudioManager.Play(SoundId.Hit); return;
            }
            // Producers make beats all the time: only YOUR beats (and great producer beats) make a sound.
            if (r.Source == BeatSource.Manual)
                AudioManager.Play(r.Quality >= BeatQuality.Good ? SoundId.BeatGood : SoundId.Beat);
            else if (r.Quality == BeatQuality.Great)
                AudioManager.Play(SoundId.BeatGood);
        }

        void OnProducerChanged(Producer p)
        {
            lastLevels.TryGetValue(p, out int before);
            lastLevels[p] = p.Level;
            if (p.Level <= before) return; // reset (prestige) or load

            if (before == 0)
            {
                var id = p.Index < producerSounds.Length ? producerSounds[p.Index] : SoundId.Hire;
                AudioManager.Play(id);
            }
            else
            {
                AudioManager.Play(SoundId.Upgrade);
            }
        }

        void OnUnlock<T>(T _) => AudioManager.Play(SoundId.Unlock);
        void OnStudio(StudioData _) => AudioManager.Play(SoundId.StudioTransition);
        void OnCashOut(double _) => AudioManager.Play(SoundId.CashOut);
        void OnCollect(double _) => AudioManager.Play(SoundId.Collect);
        void OnPrestige(double _) => AudioManager.Play(SoundId.Prestige);
        float lastViralTime = -99f;

        void OnBoost(BoostDefinition b, float s)
        {
            // A VIRAL beat gives a free boost — the VIRAL sound already covers it.
            if (Time.unscaledTime - lastViralTime < 1f) return;
            AudioManager.Play(SoundId.Boost);
        }
    }
}
