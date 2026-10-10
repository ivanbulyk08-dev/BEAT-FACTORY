using UnityEngine;

namespace BeatFactory
{
    public enum AlbumGenre { Underground, Trap, Rage, Drill, RnB, Electronic }

    /// <summary>
    /// A fictional album / tape the player can release once the requirements are met.
    /// Assets live in Assets/_BeatFactory/Data/Albums (order = release order).
    /// </summary>
    [CreateAssetMenu(fileName = "Album", menuName = "Beat Factory/Album")]
    public class AlbumData : ScriptableObject
    {
        [Tooltip("Unique id used by the save system. Do not change it after release.")]
        public string id;
        /// <summary>Stylized album title: kept untranslated (like the artist names).</summary>
        public string title;
        [Tooltip("Short line under the title (e.g. FIRST TAPE, DEBUT ALBUM).")]
        public string kind = "MIXTAPE";
        [TextArea] public string tagline;

        public string Kind => Loc.Or("album." + id + ".kind", kind);
        public string Tagline => Loc.Or("album." + id + ".tagline", tagline);
        public AlbumGenre genre;
        public Color accent = Color.white;
        public Sprite cover;

        [Header("Requirements")]
        [Tooltip("Total beats made (all time).")]
        public long requiredBeats = 25;
        public int requiredProducers = 1;
        [Tooltip("Studio level (0 = Basement).")]
        public int requiredStudio;
        public int requiredArtists;

        [Header("Rewards")]
        [Tooltip("Money reward = this many seconds of your current income (minimum 100).")]
        public double rewardIncomeSeconds = 120;
        [Tooltip("Reputation: permanent income multiplier (stacks with other albums, kept after prestige).")]
        public double reputationMultiplier = 1.1;
    }
}
