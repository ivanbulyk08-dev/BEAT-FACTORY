using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Static definition of an artist the player can sign. Assets live in Assets/_BeatFactory/Data/Artists.
    /// Signed artists stack: their multipliers are multiplied together.
    /// </summary>
    [CreateAssetMenu(fileName = "Artist", menuName = "Beat Factory/Artist")]
    public class ArtistData : ScriptableObject
    {
        [Tooltip("Unique id used by the save system. Do not change it after release.")]
        public string id;
        public string displayName;
        [Tooltip("1-2 letters shown on the placeholder avatar.")]
        public string shortName;
        // displayName / shortName are fictional stage names: never translated.
        public Color color = Color.white;
        [Tooltip("Optional avatar. If empty, a colored circle with the short name is shown.")]
        public Sprite avatar;

        [Header("Identity (fictional)")]
        [Tooltip("Archetype shown on the card, e.g. UNDERGROUND RAPPER.")]
        public string role;
        [Tooltip("Music style, e.g. Underground Rap.")]
        public string musicStyle;
        [Tooltip("One-line personality / bio shown on the contract.")]
        [TextArea] public string personality;
        public ArtistGenre genre;
        [Tooltip("The kind of beats this artist wants from you.")]
        public string beatStyle;

        public string Role => Loc.Or("artist." + id + ".role", role);
        public string MusicStyle => Loc.Or("artist." + id + ".style", musicStyle);
        public string Personality => Loc.Or("artist." + id + ".bio", personality);
        public string BeatStyle => Loc.Or("artist." + id + ".beat_style", beatStyle);
        [Tooltip("1-5: shown as popularity pips.")]
        [Range(1, 5)] public int popularity = 1;
        public double monthlyListeners = 1000;

        [Header("Look (single source of truth: portrait, posters, stage performer)")]
        public ArtistVisualIdentity visual = new ArtistVisualIdentity();

        [Header("Signing")]
        public double cost = 50_000;
        [Tooltip("Studio level needed before this artist can be signed (0 = Bedroom Studio).")]
        [Min(0)] public int requiredStudioLevel = 1;

        [Header("Effect")]
        [Tooltip("Income multiplier for ALL beats. Stacks with other signed artists.")]
        public double incomeMultiplier = 2;
    }
}
