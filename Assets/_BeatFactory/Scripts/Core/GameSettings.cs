using System;

namespace BeatFactory
{
    /// <summary>Player settings. Saved together with the game progress.</summary>
    [Serializable]
    public class GameSettings
    {
        public bool musicOn = true;
        public bool soundOn = true;
        public bool notificationsOn = true;

        // 0..1 sliders
        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;

        // Concerts
        public bool concertMusicOn = true;
        public float concertMusicVolume = 1f;
        /// <summary>Concert effects quality: -1 auto (by device), 0 low, 1 medium, 2 high.</summary>
        public int concertQuality = -1;

        /// <summary>Selected language code ("en", "ru", "pt-BR" ...). Empty in old saves → device language.</summary>
        public string language = "";

        /// <summary>Effective concert quality (auto picks by device memory / cores).</summary>
        public int ConcertQualityLevel
        {
            get
            {
                if (concertQuality >= 0) return Math.Min(2, concertQuality);
                int mem = UnityEngine.SystemInfo.systemMemorySize, cores = UnityEngine.SystemInfo.processorCount;
                if (mem > 0 && mem < 3000 || cores <= 4) return 0;
                if (mem < 6000) return 1;
                return 2;
            }
        }

        /// <summary>Fired by whoever changes a setting (call NotifyChanged after editing).</summary>
        [field: NonSerialized] public event Action Changed;

        public void NotifyChanged() => Changed?.Invoke();

        public void CopyFrom(GameSettings other)
        {
            if (other == null) return;
            musicOn = other.musicOn;
            soundOn = other.soundOn;
            notificationsOn = other.notificationsOn;
            masterVolume = Clamp01(other.masterVolume);
            musicVolume = Clamp01(other.musicVolume);
            sfxVolume = Clamp01(other.sfxVolume);
            concertMusicOn = other.concertMusicOn;
            concertMusicVolume = Clamp01(other.concertMusicVolume);
            concertQuality = Math.Max(-1, Math.Min(2, other.concertQuality));
            language = other.language ?? "";
            if (language.Length > 0) Loc.SetLanguage(language);
            else language = Loc.Info.code;
            NotifyChanged();
        }

        static float Clamp01(float v) => float.IsNaN(v) ? 1f : Math.Max(0f, Math.Min(1f, v));
    }
}
