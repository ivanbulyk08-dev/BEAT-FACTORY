using System;
using UnityEngine;

namespace BeatFactory
{
    public enum BeatQuality
    {
        Normal = 0,
        Good = 1,
        Great = 2,
        Hit = 3,
        Viral = 4,
    }

    [Serializable]
    public class BeatQualityTier
    {
        public BeatQuality quality;
        public string label;
        /// <summary>Localized label: NORMAL / GOOD / GREAT / HIT / VIRAL.</summary>
        public string Label => Loc.Or("quality." + quality.ToString().ToLowerInvariant(), label);
        [Tooltip("Chance in percent. Chances don't have to add up to exactly 100 — they are used as weights.")]
        [Min(0)] public float chancePercent;
        [Tooltip("Reward multiplier for a beat of this quality.")]
        [Min(0)] public double multiplier = 1;
        [Tooltip("Streams this beat earns, as a share of its BASE money reward (0.5 = streams worth 50% of a normal beat), before platform multipliers. NORMAL beats get none.")]
        [Min(0)] public double streamShare = 0;
        public Color color = Color.white;
    }

    /// <summary>
    /// Beat quality chances and multipliers. Edit the BeatQualityConfig asset in
    /// Assets/_BeatFactory/Data/Config to change the numbers.
    /// </summary>
    [CreateAssetMenu(fileName = "BeatQualityConfig", menuName = "Beat Factory/Beat Quality Config")]
    public class BeatQualityConfig : ScriptableObject
    {
        [Tooltip("Must contain one entry per quality, ordered Normal -> Viral.")]
        public BeatQualityTier[] tiers = CreateDefaultTiers();

        [Tooltip("Batches bigger than this are not rolled beat-by-beat: the average multiplier is used instead (fast, no popups).")]
        [Min(1)] public int maxIndividualRolls = 32;

        [Tooltip("Random titles shown when a HIT or VIRAL beat happens.")]
        public string[] specialBeatTitles =
        {
            "MIDNIGHT", "NEON RAIN", "LOST SIGNAL", "SKYLINE", "AFTERHOURS", "VELVET NIGHTS", "ORBIT",
            "DEEP END", "LOW BATTERY", "CITY LIGHTS", "SLOW MOTION", "NIGHT DRIVE", "SUGAR RUSH",
            "BLUE HOUR", "STATIC", "ZERO GRAVITY", "AFTERGLOW", "WILDFIRE", "ECHOES", "BASEMENT TAPES",
            "HEADPHONES ON", "808 DREAMS", "PIXEL HEART", "LATE CHECKOUT", "RED LIGHT", "SATELLITE",
        };

        public static BeatQualityTier[] CreateDefaultTiers() => new[]
        {
            new BeatQualityTier { quality = BeatQuality.Normal, label = "NORMAL", chancePercent = 70f,  multiplier = 1,   streamShare = 0,   color = new Color(0.30f, 1.00f, 0.66f) },
            new BeatQualityTier { quality = BeatQuality.Good,   label = "GOOD",   chancePercent = 20f,  multiplier = 2,   streamShare = 0.5, color = new Color(0.24f, 0.88f, 1.00f) },
            new BeatQualityTier { quality = BeatQuality.Great,  label = "GREAT",  chancePercent = 8f,   multiplier = 5,   streamShare = 1,   color = new Color(0.71f, 0.36f, 1.00f) },
            new BeatQualityTier { quality = BeatQuality.Hit,    label = "HIT",    chancePercent = 1.8f, multiplier = 20,  streamShare = 2,   color = new Color(1.00f, 0.54f, 0.24f) },
            new BeatQualityTier { quality = BeatQuality.Viral,  label = "VIRAL",  chancePercent = 0.2f, multiplier = 100, streamShare = 5,   color = new Color(1.00f, 0.24f, 0.78f) },
        };

        public BeatQualityTier GetTier(BeatQuality quality)
        {
            foreach (var t in tiers)
                if (t.quality == quality) return t;
            return tiers.Length > 0 ? tiers[0] : new BeatQualityTier { label = quality.ToString().ToUpper(), multiplier = 1 };
        }

        public string GetRandomTitle(System.Random random) =>
            specialBeatTitles == null || specialBeatTitles.Length == 0
                ? "UNTITLED"
                : specialBeatTitles[random.Next(specialBeatTitles.Length)];
    }
}
