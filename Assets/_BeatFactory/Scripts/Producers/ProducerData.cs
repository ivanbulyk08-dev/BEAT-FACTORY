using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Static definition of one producer type (name, prices, income).
    /// Assets live in Assets/_BeatFactory/Data/Producers — select one to edit its numbers.
    /// </summary>
    [CreateAssetMenu(fileName = "Producer", menuName = "Beat Factory/Producer")]
    public class ProducerData : ScriptableObject
    {
        [Tooltip("Unique id used by the save system. Do not change it after release.")]
        public string id;
        public string displayName;

        /// <summary>Localized name (producer.{id}.name).</summary>
        public string DisplayName => Loc.Or("producer." + id + ".name", displayName);
        [Tooltip("1-2 letters shown on the placeholder icon.")]
        public string shortName;
        public Color color = Color.white;
        [Tooltip("Optional icon. If empty, a colored square with the short name is shown.")]
        public Sprite icon;

        [Header("Prices")]
        [Tooltip("Price to hire (get level 1).")]
        public double hireCost = 15;
        [Tooltip("Price of the first upgrade (level 1 -> 2). Next upgrades: upgradeBaseCost * growth ^ (level - 1).")]
        public double upgradeBaseCost = 50;
        [Tooltip("Cost growth per level. 0 = use EconomyConfig.upgradeCostGrowth.")]
        public double costGrowth = 0;

        [Header("Production")]
        [Tooltip("Money per beat at level 1 (before multipliers). Grows linearly with level.")]
        public double baseIncome = 5;
        [Tooltip("Seconds to produce one beat.")]
        [Min(0.1f)] public float cycleSeconds = 5f;
        [Tooltip("0 = no level limit.")]
        [Min(0)] public int maxLevel = 0;
        [Tooltip("Studio level needed before this producer can be hired (0 = Basement).")]
        [Min(0)] public int requiredStudio = 0;
    }
}
