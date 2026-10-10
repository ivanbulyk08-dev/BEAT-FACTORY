using System;
using System.Collections.Generic;

namespace BeatFactory
{
    public enum TourKind { Mini = 0, Regional = 1, National = 2, World = 3, Mega = 4 }

    /// <summary>Tour type: a run of shows at one venue size that plays out in real time (also while the game is closed).</summary>
    public class TourData
    {
        public TourKind kind;
        public string name;
        public string audience;

        public string Name => Loc.Or("tour." + kind.ToString().ToLowerInvariant() + ".name", name);
        public string Audience => Loc.Or("tour." + kind.ToString().ToLowerInvariant() + ".audience", audience);
        public int shows;
        public float hours;
        public VenueTier venue;
        /// <summary>Mega tours play bigger stadium setups.</summary>
        public float capacityScale = 1f;
        public int requiredConcertLevel;
        public float requiredPopularity;
        public int requiredStudio;
        /// <summary>Upfront production cost as a share of the expected revenue.</summary>
        public float costShare = 0.3f;
    }

    public static class Tours
    {
        public static readonly TourData[] All =
        {
            new TourData { kind = TourKind.Mini, name = "MINI TOUR", audience = "For rising artists", shows = 3, hours = 0.33f, venue = VenueTier.SmallVenue, requiredConcertLevel = 2 },
            new TourData { kind = TourKind.Regional, name = "REGIONAL TOUR", audience = "For established artists", shows = 5, hours = 1f, venue = VenueTier.ConcertHall, requiredConcertLevel = 3 },
            new TourData { kind = TourKind.National, name = "NATIONAL TOUR", audience = "For major artists", shows = 8, hours = 3f, venue = VenueTier.Arena, requiredConcertLevel = 5 },
            new TourData { kind = TourKind.World, name = "WORLD TOUR", audience = "For superstars", shows = 12, hours = 8f, venue = VenueTier.Stadium, requiredConcertLevel = 7 },
            new TourData { kind = TourKind.Mega, name = "MEGA TOUR", audience = "The biggest tour ever", shows = 20, hours = 12f, venue = VenueTier.Stadium, capacityScale = 1.5f,
                requiredConcertLevel = 9, requiredPopularity = 95, requiredStudio = 9 },
        };

        public static TourData Get(TourKind k) => All[(int)k];

        static readonly Dictionary<ArtistGenre, string[]> Titles = new Dictionary<ArtistGenre, string[]>
        {
            { ArtistGenre.Rage, new[] { "NO MERCY", "RED NOISE", "OVERDRIVE", "CHAOS THEORY", "BLOOD MOON" } },
            { ArtistGenre.Trap, new[] { "AFTER HOURS", "BLACK CARD", "NIGHT SHIFT", "PENTHOUSE", "LOW LIGHTS" } },
            { ArtistGenre.Underground, new[] { "BASEMENT TAPES", "CONCRETE DREAMS", "B-SIDES", "DUST & VINYL", "UNDERCITY" } },
            { ArtistGenre.Drill, new[] { "COLD STREETS", "NORTH SIDE", "GREY SKIES", "NO SLEEP", "BLOCK PARTY" } },
            { ArtistGenre.Melodic, new[] { "STARLIGHT", "FADED HEARTS", "PAPER WINGS", "AURORA", "CLOUD NINE" } },
            { ArtistGenre.RnB, new[] { "VELVET NIGHTS", "SLOW BURN", "SILK ROAD", "AFTERGLOW", "HONEY MOON" } },
            { ArtistGenre.Experimental, new[] { "GLITCH ERA", "STATIC BLOOM", "NEW SIGNAL", "HYPERSPACE", "PRISM" } },
            { ArtistGenre.HipHop, new[] { "GOLDEN ERA", "CITY LIGHTS", "RUN IT BACK", "CROWN", "THE COME UP" } },
        };

        /// <summary>Stylized tour name without the "TOUR" word ("NIGHT SHIFT"): language independent, saved.</summary>
        public static string TitleName(ArtistData a, TourKind kind, string albumTitle)
        {
            if (!string.IsNullOrEmpty(albumTitle)) return albumTitle.ToUpperInvariant();
            var list = Titles.TryGetValue(a.genre, out var t) ? t : Titles[ArtistGenre.HipHop];
            int h = Math.Abs((a.id ?? "").GetHashCode());
            return list[(h + (int)kind) % list.Length];
        }

        /// <summary>"NIGHT SHIFT TOUR" / "NIGHT SHIFT WORLD TOUR" in the current language.</summary>
        public static string FormatTitle(string name, TourKind kind) =>
            Loc.F(kind == TourKind.World ? "tour.title_world" : kind == TourKind.Mega ? "tour.title_mega" : "tour.title", "name", name);

        /// <summary>"NIGHT SHIFT TOUR" — every artist has their own tour titles.</summary>
        public static string Title(ArtistData a, TourKind kind, string albumTitle) => FormatTitle(TitleName(a, kind, albumTitle), kind);
    }

    /// <summary>A tour in progress (saved). Rewards are fixed when the tour starts and paid when it is collected.</summary>
    [Serializable]
    public class ActiveTour
    {
        public string artistId;
        public int kind;
        public string title;      // old saves: English title (used only when titleName is empty)
        public string titleName;  // stylized name without "TOUR" (language independent)
        public long startUtcTicks;
        public long endUtcTicks;
        public int shows;
        public List<string> cities = new List<string>();
        public double revenue;
        public double fans;
        public float popularity;
        public int xp;
        public int attendance;
        public int capacity;
        public bool albumBonus;

        public TourKind Kind => (TourKind)kind;
        public string Title => !string.IsNullOrEmpty(titleName) ? Tours.FormatTitle(titleName, Kind) : title;
        public double TotalSeconds => (endUtcTicks - startUtcTicks) / (double)TimeSpan.TicksPerSecond;
        public double Progress(DateTime now) => TotalSeconds <= 0 ? 1 : Math.Min(1, Math.Max(0, (now.Ticks - startUtcTicks) / (double)TimeSpan.TicksPerSecond / TotalSeconds));
        public int ShowsDone(DateTime now) => Math.Min(shows, (int)Math.Floor(Progress(now) * shows + 1e-6));
        public bool IsFinished(DateTime now) => now.Ticks >= endUtcTicks;
        public double SecondsLeft(DateTime now) => Math.Max(0, (endUtcTicks - now.Ticks) / (double)TimeSpan.TicksPerSecond);
    }
}
