using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Static definition of a music genre. Assets live in Assets/_BeatFactory/Data/Genres.
    /// </summary>
    [CreateAssetMenu(fileName = "Genre", menuName = "Beat Factory/Genre")]
    public class GenreData : ScriptableObject
    {
        [Tooltip("Unique id used by the save system. Do not change it after release.")]
        public string id;
        public string displayName;

        /// <summary>Localized name (genre.{id}.name), English asset text as fallback.</summary>
        public string DisplayName => Loc.Or("genre." + id + ".name", displayName);

        [Header("Unlock")]
        public double unlockCost = 1000;
        [Tooltip("Studio level needed before this genre can be unlocked (0 = Bedroom Studio).")]
        [Min(0)] public int requiredStudioLevel = 0;

        [Header("Effect")]
        [Tooltip("Income multiplier for ALL beats while this is your best genre.")]
        public double incomeMultiplier = 1;

        [Header("Visual Theme")]
        public Color accentColor = Color.cyan;
        public Color backgroundColor = new Color(0.05f, 0.04f, 0.1f);
    }
}
