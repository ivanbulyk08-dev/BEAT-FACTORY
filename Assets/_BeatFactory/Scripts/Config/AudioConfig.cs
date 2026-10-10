using System;
using UnityEngine;

namespace BeatFactory
{
    [Serializable]
    public class SoundEntry
    {
        public SoundId id;
        public SoundCategory category = SoundCategory.SFX;
        [Tooltip("One is picked at random each time (never the same twice in a row when there are several).")]
        public AudioClip[] variants = new AudioClip[0];
        [Tooltip("Legacy single clip — used only when Variants is empty.")]
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.6f;
        [Tooltip("Random pitch +/- this amount. Keep small (0–0.04) so it stays in tune.")]
        [Range(0f, 0.2f)] public float pitchVariation = 0.02f;
        [Tooltip("The same sound can't start more often than this (seconds). Prevents noise when lots happens at once.")]
        [Min(0f)] public float minInterval = 0.05f;
        [Tooltip("Lower the music briefly while this plays (0 = no ducking).")]
        [Range(0f, 1f)] public float duckMusic = 0f;
    }

    /// <summary>
    /// All audio clips and the mix. Replace generated clips by dragging your own AudioClips into the variants.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Beat Factory/Audio Config")]
    public class AudioConfig : ScriptableObject
    {
        public SoundEntry[] sounds = new SoundEntry[0];

        [Header("Music")]
        public AudioClip music;
        [Range(0f, 1f)] public float musicVolume = 0.32f;
        [Min(0f)] public float musicFadeSeconds = 1.5f;

        [Header("Mix (category volumes)")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 0.9f;
        [Range(0f, 1f)] public float uiVolume = 0.7f;
        [Range(0f, 1f)] public float productionVolume = 0.85f;

        public SoundEntry Get(SoundId id)
        {
            foreach (var s in sounds)
                if (s.id == id) return s;
            return null;
        }

        public float CategoryVolume(SoundCategory c) =>
            c == SoundCategory.UI ? uiVolume : c == SoundCategory.Production ? productionVolume : sfxVolume;
    }
}
