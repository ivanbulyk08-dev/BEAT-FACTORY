using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Central place for every balance value in the game.
    /// Select the EconomyConfig asset in Assets/_BeatFactory/Data/Config to tweak numbers in the Inspector.
    /// New sections are added here as new systems are implemented.
    /// </summary>
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "Beat Factory/Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Currency")]
        [Tooltip("Money the player has when starting a fresh game (or after prestige).")]
        [Min(0)] public double startingMoney = 0;

        [Header("Manual Beats")]
        [Tooltip("Money earned by one press of MAKE BEAT before any multipliers.")]
        [Min(0)] public double manualBeatBaseReward = 10;
        [Tooltip("Each MAKE BEAT also earns this share of the producers' base income per second (keeps tapping useful later).")]
        [Min(0)] public double manualBeatProducerShare = 0.04;

        [Header("Upgrades")]
        [Tooltip("Upgrade cost = upgradeBaseCost * growth ^ (level - 1). Steep growth: every level is a goal.")]
        [Min(1)] public double upgradeCostGrowth = 1.5;

        [Header("Producers")]
        [Tooltip("Every N levels a producer gets a milestone bonus. 0 = no milestones.")]
        [Min(0)] public int producerMilestoneEvery = 10;
        [Tooltip("Income multiplier for each milestone reached (x2 at level 10, x4 at 20 ...).")]
        [Min(1)] public double producerMilestoneMultiplier = 2;

        [Header("Offline Earnings")]
        [Tooltip("Maximum hours of offline income the player can collect.")]
        [Min(0)] public float maxOfflineHours = 8f;
        [Tooltip("Shorter absences don't show the Welcome Back popup.")]
        [Min(0)] public float minOfflineSeconds = 60f;
        [Tooltip("Share of normal income earned while away. 1 = 100%.")]
        [Min(0)] public double offlineEfficiency = 0.5;

        [Header("Prestige")]
        [Tooltip("Permanent income bonus per Fame Point. 0.05 = +5% per point.")]
        [Min(0)] public double incomeBonusPerFamePoint = 0.05;
        [Tooltip("Money that must be earned in the current run before RESTART LABEL is possible.")]
        [Min(0)] public double prestigeMinRunEarnings = 10_000_000_000;
        [Tooltip("Fame gained = (run earnings / fameUnit) ^ fameExponent, rounded down.")]
        [Min(1)] public double fameUnitEarnings = 100_000_000;
        [Range(0.1f, 1f)] public float fameExponent = 0.5f;
    }
}
