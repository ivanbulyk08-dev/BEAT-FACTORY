using UnityEngine;

namespace BeatFactory
{
    public enum VenueTier { Club = 0, SmallVenue = 1, ConcertHall = 2, Arena = 3, Stadium = 4 }

    /// <summary>
    /// One concert venue tier. Ticket sales use real-world numbers; "merch, streams &amp; sponsors" scale with the
    /// player's producer income so concerts stay meaningful from the basement to the end game.
    /// </summary>
    public class VenueData
    {
        public VenueTier tier;
        public string name;
        public string description;

        public string Name => Loc.Or("venue." + tier.ToString().ToLowerInvariant() + ".name", name);
        public string Description => Loc.Or("venue." + tier.ToString().ToLowerInvariant() + ".desc", description);
        public int capacity;
        public float ticketMin, ticketMax;
        [Tooltip("Popularity (%) needed to book.")]
        public float requiredPopularity;
        [Tooltip("Fans needed to book.")]
        public double requiredFans;
        [Tooltip("Merch/streams/sponsors: seconds of producer income at a full house.")]
        public float incomeSeconds;
        [Tooltip("Rest time before the artist can perform again.")]
        public float cooldownMinutes;
        public int xp;
        [Tooltip("Popularity gained at a full house.")]
        public float popularityGain;
        [Tooltip("New fans per attendee.")]
        public float fansPerAttendee;
        public Color color;
    }

    /// <summary>Venue table (balance lives here, see ECONOMY_BALANCE.md → Concerts).</summary>
    public static class Venues
    {
        public static readonly VenueData[] All =
        {
            new VenueData { tier = VenueTier.Club, name = "UNDERGROUND CLUB", description = "Dark room, small stage, smoke and the real ones.",
                capacity = 300, ticketMin = 15, ticketMax = 25, requiredPopularity = 0, requiredFans = 0,
                incomeSeconds = 10, cooldownMinutes = 2, xp = 60, popularityGain = 0.8f, fansPerAttendee = 12, color = new Color(0.71f, 0.36f, 1f) },
            new VenueData { tier = VenueTier.SmallVenue, name = "SMALL VENUE", description = "Bigger stage, pro lights and LED screens.",
                capacity = 1_000, ticketMin = 25, ticketMax = 35, requiredPopularity = 20, requiredFans = 25_000,
                incomeSeconds = 25, cooldownMinutes = 5, xp = 140, popularityGain = 1.2f, fansPerAttendee = 10, color = new Color(0.24f, 0.88f, 1f) },
            new VenueData { tier = VenueTier.ConcertHall, name = "CONCERT HALL", description = "A real hall. Balconies. A real lighting rig.",
                capacity = 3_000, ticketMin = 35, ticketMax = 50, requiredPopularity = 40, requiredFans = 150_000,
                incomeSeconds = 60, cooldownMinutes = 10, xp = 300, popularityGain = 1.8f, fansPerAttendee = 9, color = new Color(0.3f, 1f, 0.66f) },
            new VenueData { tier = VenueTier.Arena, name = "ARENA", description = "Giant stage, lasers, smoke — a career milestone.",
                capacity = 15_000, ticketMin = 50, ticketMax = 80, requiredPopularity = 65, requiredFans = 1_000_000,
                incomeSeconds = 150, cooldownMinutes = 20, xp = 600, popularityGain = 2.5f, fansPerAttendee = 8, color = new Color(1f, 0.24f, 0.78f) },
            new VenueData { tier = VenueTier.Stadium, name = "STADIUM", description = "Fireworks. Massive screens. The end game.",
                capacity = 60_000, ticketMin = 70, ticketMax = 120, requiredPopularity = 90, requiredFans = 6_000_000,
                incomeSeconds = 360, cooldownMinutes = 40, xp = 1200, popularityGain = 3.5f, fansPerAttendee = 7, color = new Color(1f, 0.82f, 0.24f) },
        };

        public static VenueData Get(VenueTier t) => All[Mathf.Clamp((int)t, 0, All.Length - 1)];

        /// <summary>Fictional cities of the Beat Factory world (no real places). Proper names: never translated.</summary>
        public static readonly string[] Cities =
        {
            "Neon Harbor", "Vel Marro", "Kastra", "Port Solene", "Ashgrove", "Nova Lisk", "Cyan Bay", "Ironvale",
            "Saint Rue", "Mirov", "Lumen City", "Duskport", "Oslan", "Redmarch", "Tessaro", "Brightwater",
            "Kael Heights", "Halden", "Verdis", "Skyreach", "Northgate", "Solmere", "Arden Falls", "Zenith Bay",
        };
    }
}
