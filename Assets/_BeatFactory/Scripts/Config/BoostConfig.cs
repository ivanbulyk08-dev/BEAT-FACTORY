using System;
using UnityEngine;

namespace BeatFactory
{
    public enum BoostType
    {
        Money = 0,     // all income ×N
        Speed = 1,     // producer speed ×N
        Producer = 2,  // producer income ×N
        Viral = 3,     // HIT/VIRAL chance ×N
    }

    [Serializable]
    public class BoostDefinition
    {
        public BoostType type;
        public string displayName;
        [Tooltip("Short text for the active-boost chip, e.g. \"2X $\".")]
        public string chipText;
        public string description;

        string Key => "boost." + type.ToString().ToLowerInvariant();
        string Mult => NumberFormatter.FormatMultiplier(multiplier);
        public string DisplayName => Loc.F(Key + ".name", "mult", Mult);
        public string ChipText => Loc.F(Key + ".chip", "mult", Mult);
        public string Description => Loc.F(Key + ".desc", "mult", Mult);
        [Min(1)] public double multiplier = 2;
        [Min(1)] public float durationSeconds = 300;
        public Color color = Color.white;
    }

    /// <summary>Temporary boosts. Edit the BoostConfig asset in Assets/_BeatFactory/Data/Config.</summary>
    [CreateAssetMenu(fileName = "BoostConfig", menuName = "Beat Factory/Boost Config")]
    public class BoostConfig : ScriptableObject
    {
        public BoostDefinition[] boosts =
        {
            new BoostDefinition { type = BoostType.Money,    displayName = "2X MONEY",       chipText = "2X $",     description = "All income ×2",            multiplier = 2, durationSeconds = 300, color = new Color(0.3f, 1f, 0.66f) },
            new BoostDefinition { type = BoostType.Speed,    displayName = "2X SPEED",       chipText = "2X SPD",   description = "Producers work 2× faster", multiplier = 2, durationSeconds = 300, color = new Color(1f, 0.82f, 0.24f) },
            new BoostDefinition { type = BoostType.Producer, displayName = "PRODUCER BOOST", chipText = "3X PROD",  description = "Producers earn ×3",        multiplier = 3, durationSeconds = 600, color = new Color(0.24f, 0.88f, 1f) },
            new BoostDefinition { type = BoostType.Viral,    displayName = "VIRAL BOOST",    chipText = "5X VIRAL", description = "HIT & VIRAL chance ×5",    multiplier = 5, durationSeconds = 180, color = new Color(1f, 0.24f, 0.78f) },
        };

        [Header("Getting boosts")]
        [Tooltip("Minutes between free boosts.")]
        [Min(0)] public float freeBoostCooldownMinutes = 20f;
        [Tooltip("A VIRAL beat gives a 2X MONEY boost for this many seconds (0 = off).")]
        [Min(0)] public float viralBeatMoneyBoostSeconds = 120f;
        [Tooltip("Minutes before another VIRAL beat can give its 2X MONEY boost (busy studios make many VIRAL beats).")]
        [Min(0)] public float viralBeatBoostCooldownMinutes = 10f;
        [Tooltip("MONEY × SPEED × PRODUCER boosts together can't multiply producer income by more than this.")]
        [Min(1)] public double maxCombinedProducerBoost = 4;
        [Tooltip("Stacking the same boost can't go beyond this many minutes.")]
        [Min(1)] public float maxStackMinutes = 60f;

        public BoostDefinition Get(BoostType type)
        {
            foreach (var b in boosts)
                if (b.type == type) return b;
            return null;
        }
    }
}
