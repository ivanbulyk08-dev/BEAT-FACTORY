using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeatFactory
{
    public enum ConcertEvent { SoldOut, ViralMoment, CrowdGoesCrazy, Encore, LowEnergy }

    /// <summary>Everything that affects a show (taken from the game when booking).</summary>
    public struct ConcertContext
    {
        public ArtistData artist;
        public ArtistCareer career;
        public int studioLevel;
        /// <summary>Producer income per second without temporary boosts (merch/streams scale with it).</summary>
        public double incomePerSecond;
        public bool recentViral;
    }

    /// <summary>What the booking card promises.</summary>
    public struct ConcertQuote
    {
        public VenueData venue;
        public int capacity;
        public float ticketPrice;
        public float expectedFill;
        public int expectedAttendance;
        public double ticketRevenue;
        public double extraRevenue;
        public double TotalRevenue => ticketRevenue + extraRevenue;
        public double bookingCost;
        public double fans;
        public float popularity;
        public int xp;
        public bool unlocked;
        public string lockReason;
        /// <summary>One-line requirement without "to unlock" (used inside the tour lock text).</summary>
        public string lockShort;
    }

    /// <summary>What actually happened on the night.</summary>
    public class ConcertResult
    {
        public ArtistData artist;
        public VenueData venue;
        public string city;
        public string showName;
        public int capacity;
        public int attendance;
        public float fill;
        public float ticketPrice;
        public double ticketRevenue;
        public double extraRevenue;
        public double Revenue => ticketRevenue + extraRevenue;
        public double bookingCost;
        public double fans;
        public float popularity;
        public int xp;
        public bool soldOut;
        /// <summary>0..1: how hard the crowd went (attendance, show quality, popularity and the night's events).</summary>
        public float crowdEnergy;
        public float popularityBefore;
        public int levelBefore, levelAfter;
        public int stageBefore, stageAfter;
        public readonly List<ConcertEvent> events = new List<ConcertEvent>();

        /// <summary>LOW / GOOD / GREAT ATTENDANCE / SOLD OUT.</summary>
        public string AttendanceLabel => Loc.T(soldOut ? "concert.sold_out" : fill >= 0.85f ? "concert.attendance_great" : fill >= 0.6f ? "concert.attendance_good" : "concert.attendance_low");
    }

    /// <summary>Attendance: popularity, fans, venue size, experience, the new album, recent viral beats and luck.</summary>
    public static class AttendanceCalculator
    {
        /// <summary>Share of the fan base that buys a ticket for one city.</summary>
        public static double Reach(float popularity) => 0.025 + Mathf.Clamp(popularity, 0, 100) / 100.0 * 0.035;

        /// <summary>At a venue's minimum requirements: club ~35%, small ~50%, hall ~80%, arena ~95%, stadium sells out.</summary>
        public static float ExpectedFill(ConcertContext c, VenueData v, float capacityScale = 1f)
        {
            double capacity = v.capacity * capacityScale;
            double demand = c.career.fans * Reach(c.career.popularity) / capacity;
            double fill = 1.0 - Math.Exp(-0.9 * demand);
            fill += 0.02 * (c.career.ConcertLevel - 1);
            if (!string.IsNullOrEmpty(c.career.newAlbumId)) fill += 0.06;
            if (c.recentViral) fill += 0.05;
            return Mathf.Clamp((float)fill, 0.15f, 1f);
        }

        /// <summary>The real night: luck and the show quality move attendance around the expectation.</summary>
        public static float RollFill(float expected, float performance, System.Random rnd)
        {
            float f = expected + (float)(rnd.NextDouble() * 0.18 - 0.1) + (performance - 0.7f) * 0.1f;
            if (f >= 0.985f) return 1f;
            return Mathf.Clamp(f, 0.1f, 0.99f);
        }
    }

    /// <summary>Prices, revenue, fans, popularity and XP for concerts (balance: ECONOMY_BALANCE.md → Concerts).</summary>
    public static class ConcertRewardCalculator
    {
        public const double BookingShare = 0.35;

        /// <summary>Popularity can't grow past what your studio's sound supports: better rooms → bigger careers.</summary>
        public static float PopularityCap(int studioLevel) => Mathf.Min(100f, 30f + 7.5f * Mathf.Max(0, studioLevel));

        public static float TicketPrice(ConcertContext c, VenueData v)
        {
            float t = Mathf.Lerp(v.ticketMin, v.ticketMax, Mathf.Clamp01(c.career.popularity / 100f));
            return Mathf.Round(t * (1f + 0.04f * (c.career.ConcertLevel - 1)));
        }

        static double Extra(ConcertContext c, VenueData v, float fill, float capacityScale)
        {
            float perf = c.career.PerformanceQuality(c.artist, c.studioLevel);
            return Math.Max(0, c.incomePerSecond) * v.incomeSeconds * capacityScale * fill * (0.8 + 0.4 * perf) * c.career.ConcertIncomeMultiplier;
        }

        public static double Fans(ConcertContext c, VenueData v, int attendance, float fill)
        {
            float perf = c.career.PerformanceQuality(c.artist, c.studioLevel);
            double f = attendance * v.fansPerAttendee * (0.8 + 0.4 * perf) + c.career.fans * 0.02 * fill;
            return Math.Round(f * (1 + 0.1 * c.studioLevel) * ArtistCareer.FanGrowth(c.artist));
        }

        public static float Popularity(ConcertContext c, VenueData v, float fill) =>
            v.popularityGain * fill * (1f + 0.05f * c.studioLevel);

        public static int Xp(VenueData v, float fill) => Mathf.RoundToInt(v.xp * (0.5f + 0.5f * fill));

        public static ConcertQuote Quote(ConcertContext c, VenueData v, float capacityScale = 1f)
        {
            var q = new ConcertQuote { venue = v, capacity = Mathf.RoundToInt(v.capacity * capacityScale) };
            q.ticketPrice = TicketPrice(c, v);
            q.expectedFill = AttendanceCalculator.ExpectedFill(c, v, capacityScale);
            q.expectedAttendance = Mathf.RoundToInt(q.capacity * q.expectedFill);
            q.ticketRevenue = q.expectedAttendance * (double)q.ticketPrice;
            q.extraRevenue = Extra(c, v, q.expectedFill, capacityScale);
            q.bookingCost = RoundPrice(q.TotalRevenue * BookingShare);
            q.fans = Fans(c, v, q.expectedAttendance, q.expectedFill);
            q.popularity = Popularity(c, v, q.expectedFill);
            q.xp = Xp(v, q.expectedFill);

            q.unlocked = true;
            if (c.career.popularity < v.requiredPopularity)
            {
                q.unlocked = false;
                float cap = PopularityCap(c.studioLevel);
                string need = NumberFormatter.Percent(v.requiredPopularity);
                q.lockShort = Loc.F("concert.lock_popularity_short", "value", need);
                q.lockReason = cap < v.requiredPopularity
                    ? q.lockShort + "\n<size=80%>" + Loc.F("concert.lock_studio_cap", "value", NumberFormatter.Percent(cap)) + "</size>"
                    : Loc.F("concert.lock_popularity", "value", need);
            }
            else if (c.career.fans < v.requiredFans)
            {
                q.unlocked = false;
                q.lockShort = Loc.P("concert.lock_fans_short", v.requiredFans);
                q.lockReason = Loc.P("concert.lock_fans", v.requiredFans);
            }
            return q;
        }

        /// <summary>Plays the show: attendance roll, random events, rewards. Does not change any state.</summary>
        public static ConcertResult Perform(ConcertContext c, ConcertQuote q, System.Random rnd)
        {
            var v = q.venue;
            var r = new ConcertResult { artist = c.artist, venue = v, capacity = q.capacity, ticketPrice = q.ticketPrice, bookingCost = q.bookingCost };
            float perf = c.career.PerformanceQuality(c.artist, c.studioLevel);
            r.fill = AttendanceCalculator.RollFill(q.expectedFill, perf, rnd);
            r.soldOut = r.fill >= 1f;
            r.attendance = r.soldOut ? q.capacity : Mathf.RoundToInt(q.capacity * r.fill);

            double revenueMult = 1, fansMult = 1;
            float popBonus = 0, xpMult = 1;
            if (r.soldOut) { r.events.Add(ConcertEvent.SoldOut); revenueMult += 0.2; }
            double viralChance = 0.05 + 0.01 * (c.career.ConcertLevel - 1) + (c.recentViral ? 0.05 : 0) + (r.fill > 0.9f ? 0.03 : 0);
            if (rnd.NextDouble() < viralChance) { r.events.Add(ConcertEvent.ViralMoment); fansMult += 0.15; popBonus += 3; revenueMult += 0.3; }
            if (r.fill >= 0.75f && rnd.NextDouble() < 0.3) { r.events.Add(ConcertEvent.CrowdGoesCrazy); fansMult += 0.05; popBonus += 1; }
            if (perf >= 0.8f && rnd.NextDouble() < 0.15) { r.events.Add(ConcertEvent.Encore); revenueMult += 0.1; xpMult += 0.2f; }
            if (r.fill < 0.6f) { r.events.Add(ConcertEvent.LowEnergy); revenueMult -= 0.1; }

            r.ticketRevenue = Math.Round(r.attendance * (double)q.ticketPrice * revenueMult);
            r.extraRevenue = Math.Round(Extra(c, v, r.fill, q.capacity / (float)v.capacity) * revenueMult);
            r.fans = Math.Round(Fans(c, v, r.attendance, r.fill) * fansMult);
            r.popularity = Popularity(c, v, r.fill) + popBonus;
            r.xp = Mathf.RoundToInt(Xp(v, r.fill) * xpMult);
            float energy = 0.35f * r.fill + 0.3f * perf + 0.25f * Mathf.Clamp01(c.career.popularity / 90f);
            foreach (var e in r.events)
                energy += e == ConcertEvent.ViralMoment ? 0.1f : e == ConcertEvent.CrowdGoesCrazy ? 0.08f : e == ConcertEvent.SoldOut ? 0.05f : e == ConcertEvent.Encore ? 0.05f : -0.1f;
            r.crowdEnergy = Mathf.Clamp(energy + (float)(rnd.NextDouble() * 0.06 - 0.03), 0.2f, 0.99f);
            r.popularityBefore = c.career.popularity;
            return r;
        }

        /// <summary>Two significant digits ($24,000 not $23,871) — reads like a real price.</summary>
        public static double RoundPrice(double raw)
        {
            if (raw < 100) return Math.Ceiling(raw);
            double mag = Math.Pow(10, Math.Floor(Math.Log10(raw)) - 1);
            return Math.Round(raw / mag) * mag;
        }

        public static string EventTitle(ConcertEvent e) => Loc.T("concert.event." + e.ToString().ToLowerInvariant() + ".title");

        public static string EventText(ConcertEvent e) => Loc.T("concert.event." + e.ToString().ToLowerInvariant() + ".text");

        public static Color EventColor(ConcertEvent e)
        {
            switch (e)
            {
                case ConcertEvent.SoldOut: return new Color(1f, 0.54f, 0.24f);
                case ConcertEvent.ViralMoment: return new Color(1f, 0.24f, 0.78f);
                case ConcertEvent.CrowdGoesCrazy: return new Color(1f, 0.82f, 0.24f);
                case ConcertEvent.Encore: return new Color(0.3f, 1f, 0.66f);
                default: return new Color(0.55f, 0.52f, 0.7f);
            }
        }
    }
}
