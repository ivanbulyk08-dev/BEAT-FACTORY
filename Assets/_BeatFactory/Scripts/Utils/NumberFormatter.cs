using System;
using System.Text;

namespace BeatFactory
{
    /// <summary>
    /// Turns big numbers into short readable text:
    /// 1,000 / 10K / 250K / 1.5M / 123.5B / 5.4T / 1.2Qa ... then aa, ab, ac ...
    /// Separators, money and time units follow the selected language (1 000 / 1,5M / 1,5M $ / 2ч 35м);
    /// the values themselves never change.
    /// </summary>
    public static class NumberFormatter
    {
        static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };

        /// <summary>Numbers below this are shown in full with group separators (e.g. 9,999).</summary>
        const double ShortFormThreshold = 10_000;

        public static string Format(double value)
        {
            if (double.IsNaN(value)) return "0";
            if (double.IsInfinity(value)) return value > 0 ? "∞" : "-∞";

            string sign = value < 0 ? "-" : "";
            value = Math.Abs(value);

            if (value < ShortFormThreshold)
                return sign + Math.Floor(value).ToString("#,0", Loc.Numbers);

            int tier = (int)Math.Floor(Math.Log10(value) / 3);
            double scaled = value / Math.Pow(10, tier * 3);

            // Guard against floating point edge cases (e.g. 999.99999 or 1000.0000001).
            if (scaled >= 1000) { tier++; scaled /= 1000; }
            else if (scaled < 1) { tier--; scaled *= 1000; }

            // Round DOWN so we never display more money than the player actually has.
            scaled = Math.Floor(scaled * 10) / 10;

            return sign + scaled.ToString("0.#", Loc.Numbers) + GetSuffix(tier);
        }

        /// <summary>Whole number with group separators: 12,345 / 12 345 / 12.345.</summary>
        public static string Group(double value) => Math.Floor(value).ToString("#,0", Loc.Numbers);

        /// <summary>Percent with the language's decimal separator: "12%", "4,5%" (value already in %).</summary>
        public static string Percent(double value, string format = "0") => value.ToString(format, Loc.Numbers) + "%";

        /// <summary>Plain decimal ("0.#") with the language's separator.</summary>
        public static string Decimal(double value, string format = "0.#") => value.ToString(format, Loc.Numbers);

        public static string FormatMoney(double value) =>
            value < 0 ? "-" + Money(MoneyAmount(-value)) : Money(MoneyAmount(value));

        /// <summary>Small amounts keep one decimal ("$0.8", "$12.5") so early beats never show as "$0"; rounded down.</summary>
        static string MoneyAmount(double value) =>
            value < 100 && !double.IsNaN(value) ? (Math.Floor(value * 10) / 10).ToString("0.#", Loc.Numbers) : Format(value);

        /// <summary>Puts the currency sign where the language expects it ("$5K" / "5K $").</summary>
        public static string Money(string amount) => Loc.T("num.money").Replace("{0}", amount);

        /// <summary>×1, ×2.5, ×1.2K</summary>
        public static string FormatMultiplier(double value)
        {
            if (value < 100) return "×" + (Math.Floor(value * 100) / 100).ToString("0.##", Loc.Numbers);
            return "×" + Format(value);
        }

        /// <summary>"1d 3h", "2h 35m", "4m 10s", "12s" (units localized)</summary>
        public static string FormatDuration(TimeSpan time)
        {
            if (time.TotalSeconds < 1) return Unit("time.s", 0);
            if (time.TotalDays >= 1) return time.Hours > 0 ? Pair(Unit("time.d", (int)time.TotalDays), Unit("time.h", time.Hours)) : Unit("time.d", (int)time.TotalDays);
            if (time.TotalHours >= 1) return time.Minutes > 0 ? Pair(Unit("time.h", (int)time.TotalHours), Unit("time.m", time.Minutes)) : Unit("time.h", (int)time.TotalHours);
            if (time.TotalMinutes >= 1) return time.Seconds > 0 ? Pair(Unit("time.m", (int)time.TotalMinutes), Unit("time.s", time.Seconds)) : Unit("time.m", (int)time.TotalMinutes);
            return Unit("time.s", time.Seconds);
        }

        static string Unit(string key, int v) => Loc.T(key).Replace("{0}", v.ToString());
        static string Pair(string a, string b) => Loc.T("time.join").Replace("{0}", a).Replace("{1}", b);

        /// <summary>Countdown clock: "4:07", "1:02:03".</summary>
        public static string FormatClock(double seconds)
        {
            int s = (int)Math.Ceiling(Math.Max(0, seconds));
            int h = s / 3600, m = (s / 60) % 60, sec = s % 60;
            return h > 0 ? $"{h}:{m:00}:{sec:00}" : $"{m}:{sec:00}";
        }

        public static string FormatDuration(double seconds) =>
            FormatDuration(TimeSpan.FromSeconds(Math.Max(0, seconds)));

        static string GetSuffix(int tier)
        {
            if (tier < Suffixes.Length) return Suffixes[tier];

            // After "Dc" continue with aa, ab ... az, ba ... zz.
            int index = tier - Suffixes.Length;
            var sb = new StringBuilder(2);
            sb.Append((char)('a' + (index / 26) % 26));
            sb.Append((char)('a' + index % 26));
            return sb.ToString();
        }
    }
}
