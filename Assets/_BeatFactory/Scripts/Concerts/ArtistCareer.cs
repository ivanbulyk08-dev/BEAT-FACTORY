using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>One finished concert (history screen).</summary>
    [Serializable]
    public class ConcertRecord
    {
        public string name;        // old saves only (English); new records are built from the ids below
        public string tourName;    // stylized tour name (language independent)
        public int tourKind;
        public int shows;
        public int venue;
        public string city;
        public int attendance;
        public int capacity;
        public double revenue;
        public double fans;
        public bool soldOut;
        public bool tour;
        public long utcTicks;

        public float Fill => capacity > 0 ? attendance / (float)capacity : 0f;

        /// <summary>"ARENA · Neon Harbor" / "NIGHT SHIFT TOUR · 5 shows" in the current language.</summary>
        public string DisplayName => ArtistCareer.ShowName(venue, city, tour, tourName, tourKind, shows, name);
    }

    /// <summary>
    /// The live career of a signed artist: fans, popularity, concert experience and history.
    /// Saved as-is. Careers are history: they are kept after a prestige (re-sign the artist to continue).
    /// </summary>
    [Serializable]
    public class ArtistCareer
    {
        public const int HistorySize = 12;
        public static readonly int[] XpForLevel = { 0, 300, 800, 1600, 3000, 5000, 8000, 12000, 18000, 26000 };
        public static readonly string[] Stages = { "UNKNOWN ARTIST", "UNDERGROUND ARTIST", "RISING ARTIST", "ESTABLISHED ARTIST", "ARENA ARTIST", "SUPERSTAR", "GLOBAL ICON" };

        public string id;
        public double fans;
        public float popularity;
        public int concertXp;
        public int concerts;
        public int soldOuts;
        public double totalRevenue;
        public double totalFansGained;
        public int bestVenue = -1;
        public float bestAttendance;
        public string bestConcertName = "";   // old saves only (English)
        public int bestConcertVenue = -1;
        public string bestConcertCity = "";
        public string bestConcertTour = "";
        public int bestConcertTourKind;
        public int bestConcertShows;
        public double bestConcertRevenue;
        public long restUntilTicks;
        public double lastFansGain;
        public int toursCompleted;
        public int worldTours;
        [Tooltip("Newest album this artist was featured on and hasn't toured yet (NEW ALBUM TOUR bonus).")]
        public string newAlbumId = "";
        public string newAlbumTitle = "";
        public List<ConcertRecord> history = new List<ConcertRecord>();

        // ------------------------------------------------------------------ derived stats

        public int ConcertLevel
        {
            get
            {
                int level = 1;
                for (int i = 1; i < XpForLevel.Length; i++) if (concertXp >= XpForLevel[i]) level = i + 1;
                return level;
            }
        }

        public bool MaxConcertLevel => ConcertLevel >= XpForLevel.Length;
        /// <summary>XP inside the current level (for the bar).</summary>
        public int LevelXp => concertXp - XpForLevel[ConcertLevel - 1];
        public int LevelXpNeeded => MaxConcertLevel ? 1 : XpForLevel[ConcertLevel] - XpForLevel[ConcertLevel - 1];

        /// <summary>0 Unknown … 5 Superstar, 6 Global Icon (completed a world tour as a superstar).</summary>
        public int StageIndex
        {
            get
            {
                if (popularity >= 90 && worldTours > 0) return 6;
                if (popularity >= 90) return 5;
                if (popularity >= 65) return 4;
                if (popularity >= 40) return 3;
                if (popularity >= 20) return 2;
                if (popularity >= 10) return 1;
                return 0;
            }
        }

        public string Stage => Loc.Or("career.stage" + StageIndex, Stages[StageIndex]);
        public int CareerLevel => StageIndex + 1;

        /// <summary>0..1 stage show quality: talent (popularity pips), concert experience and the studio's sound.</summary>
        public float PerformanceQuality(ArtistData a, int studioLevel) =>
            Mathf.Clamp(0.5f + 0.04f * a.popularity + 0.035f * (ConcertLevel - 1) + 0.01f * studioLevel + (ExtraPerformance != null ? ExtraPerformance(a) : 0f), 0f, 0.99f);

        /// <summary>Small extras from other systems (artist chemistry): show quality and fan growth.</summary>
        public static Func<ArtistData, float> ExtraPerformance;
        public static Func<ArtistData, float> ExtraFanGrowth;
        public static float FanGrowth(ArtistData a) => 1f + (ExtraFanGrowth != null ? ExtraFanGrowth(a) : 0f);

        /// <summary>Higher concert level: +4% ticket prices and +5% concert revenue per level.</summary>
        public double ConcertIncomeMultiplier => 1.0 + 0.05 * (ConcertLevel - 1);

        public bool IsResting(DateTime now) => restUntilTicks > now.Ticks;
        public double RestSeconds(DateTime now) => Math.Max(0, (restUntilTicks - now.Ticks) / (double)TimeSpan.TicksPerSecond);

        /// <summary>Remembers the best show by ids (the name is built in the player's language when shown).</summary>
        public void SetBestConcert(int venue, string city, string tourName, int tourKind, int shows, string legacyName = "")
        {
            bestConcertVenue = venue;
            bestConcertCity = city ?? "";
            bestConcertTour = tourName ?? "";
            bestConcertTourKind = tourKind;
            bestConcertShows = shows;
            bestConcertName = legacyName ?? "";
        }

        public string BestConcertDisplay => bestConcertVenue >= 0
            ? ShowName(bestConcertVenue, bestConcertCity, !string.IsNullOrEmpty(bestConcertTour), bestConcertTour, bestConcertTourKind, bestConcertShows, bestConcertName)
            : bestConcertName;

        public static string ShowName(int venue, string city, bool tour, string tourName, int tourKind, int shows, string legacy)
        {
            if (tour)
                return !string.IsNullOrEmpty(tourName)
                    ? Tours.FormatTitle(tourName, (TourKind)tourKind) + " · " + Loc.P("count.shows", shows)
                    : legacy ?? "";
            if (venue < 0) return legacy ?? "";
            return Venues.Get((VenueTier)venue).Name + (string.IsNullOrEmpty(city) ? "" : " · " + city);
        }

        public ConcertRecord BiggestShow()
        {
            ConcertRecord best = null;
            foreach (var r in history) if (best == null || r.revenue > best.revenue) best = r;
            return best;
        }

        public void AddRecord(ConcertRecord r)
        {
            history.Insert(0, r);
            if (history.Count > HistorySize) history.RemoveAt(history.Count - 1);
        }

        public static ArtistCareer CreateFor(ArtistData a) => new ArtistCareer
        {
            id = a.id,
            // signing someone nobody knows yet: a small fan base, popularity from their buzz (1–5 pips)
            fans = Math.Max(2_000, Math.Round(a.monthlyListeners * 0.25)),
            popularity = 4 + a.popularity * 4,
        };
    }
}
