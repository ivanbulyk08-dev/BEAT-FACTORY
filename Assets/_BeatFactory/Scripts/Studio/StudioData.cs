using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Static definition of a studio level. Assets live in Assets/_BeatFactory/Data/Studios.
    /// The order in GameManager is the upgrade order (first = Bedroom Studio).
    /// </summary>
    [CreateAssetMenu(fileName = "Studio", menuName = "Beat Factory/Studio")]
    public class StudioData : ScriptableObject
    {
        [Tooltip("Unique id used by the save system. Do not change it after release.")]
        public string id;
        public string displayName;

        /// <summary>Localized name (studio.{id}.name).</summary>
        public string DisplayName => Loc.Or("studio." + id + ".name", displayName);
        public string Tagline => Loc.Or("studio." + id + ".tagline", tagline);
        public string Unlock(int i) => Loc.Or("studio." + id + ".unlock" + i, unlocks != null && i < unlocks.Length ? unlocks[i] : "");

        [Header("Upgrade")]
        [Tooltip("Price to move into this studio (ignored for the first studio).")]
        public double upgradeCost = 10_000;

        [Header("Bonuses (not cumulative: the current studio's values are used)")]
        [Tooltip("Production speed multiplier for all producers.")]
        public double speedMultiplier = 1;
        [Tooltip("Income multiplier for all beats.")]
        public double incomeMultiplier = 1;
        [Tooltip("Maximum hours of offline earnings while in this studio.")]
        public double offlineHours = 2;
        [Tooltip("HIT/VIRAL chance multiplier (better gear = better beats).")]
        public double viralChanceMultiplier = 1;

        [Header("Presentation")]
        [Tooltip("One line describing the room.")]
        public string tagline;
        [Tooltip("What this room unlocks (shown on the upgrade screen).")]
        public string[] unlocks;

        [Header("Visuals")]
        [Tooltip("Background color of the studio card.")]
        public Color panelColor = new Color(0.09f, 0.07f, 0.17f);
        [Tooltip("Number of equalizer bars shown in the studio (more = more impressive).")]
        [Range(1, 32)] public int equalizerBars = 6;
    }
}
