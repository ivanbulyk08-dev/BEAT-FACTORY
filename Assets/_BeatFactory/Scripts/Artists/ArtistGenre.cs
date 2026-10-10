using UnityEngine;

namespace BeatFactory
{
    public enum ArtistGenre { Rage, Trap, Underground, Drill, Melodic, RnB, Experimental, HipHop }

    /// <summary>Display label and signature color per artist genre (cards, chips, portraits).</summary>
    public static class ArtistGenres
    {
        public static string Label(ArtistGenre g) => Loc.T("artist_genre." + g.ToString().ToLowerInvariant());

        public static Color Color(ArtistGenre g)
        {
            switch (g)
            {
                case ArtistGenre.Rage: return new Color(1f, 0.17f, 0.24f);
                case ArtistGenre.Trap: return new Color(0.71f, 0.36f, 1f);
                case ArtistGenre.Underground: return new Color(0.66f, 0.64f, 0.78f);
                case ArtistGenre.Drill: return new Color(0.5f, 0.66f, 0.8f);
                case ArtistGenre.Melodic: return new Color(1f, 0.54f, 0.85f);
                case ArtistGenre.RnB: return new Color(1f, 0.7f, 0.54f);
                case ArtistGenre.Experimental: return new Color(0.24f, 0.88f, 1f);
                default: return new Color(0.95f, 0.76f, 0.31f);
            }
        }

        /// <summary>"12.4K monthly listeners".</summary>
        public static string Listeners(double n) => Loc.P("artist.monthly_listeners", n);
    }
}
