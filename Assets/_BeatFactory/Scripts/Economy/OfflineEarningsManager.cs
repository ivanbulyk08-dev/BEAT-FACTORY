using System;

namespace BeatFactory
{
    /// <summary>What the producers earned while the player was away.</summary>
    public readonly struct OfflineReport
    {
        /// <summary>Real time away.</summary>
        public readonly double SecondsAway;
        /// <summary>Time that was actually paid (limited by maxOfflineHours).</summary>
        public readonly double SecondsCounted;
        public readonly double Money;
        public readonly long Beats;

        public OfflineReport(double secondsAway, double secondsCounted, double money, long beats)
        {
            SecondsAway = secondsAway;
            SecondsCounted = secondsCounted;
            Money = money;
            Beats = beats;
        }

        public bool WasCapped => SecondsAway > SecondsCounted;
    }

    /// <summary>
    /// Calculates offline income from the producers' average income per second
    /// (already includes genre, studio, fame and average beat quality) and keeps it
    /// until the player presses COLLECT.
    /// </summary>
    public class OfflineEarningsManager
    {
        readonly EconomyConfig config;
        readonly ProducerManager producers;
        readonly BeatManager beats;
        readonly Func<double> boostFactor;

        OfflineReport pending;

        public bool HasPending { get; private set; }
        public OfflineReport Pending => pending;

        /// <summary>Fired when there is something to collect (show the Welcome Back popup).</summary>
        public event Action<OfflineReport> ReportReady;

        /// <summary>Fired after collecting. Argument: money actually received.</summary>
        public event Action<double> Collected;

        /// <param name="boostFactor">Current boost multiplier on producer income. Offline time is paid WITHOUT boosts.</param>
        public OfflineEarningsManager(EconomyConfig config, ProducerManager producers, BeatManager beats, Func<double> boostFactor = null)
        {
            this.config = config;
            this.producers = producers;
            this.beats = beats;
            this.boostFactor = boostFactor;
        }

        /// <summary>Optional: offline hours from the current studio (better rooms keep producers working longer).</summary>
        public Func<double> MaxHoursOverride { get; set; }

        public double MaxOfflineSeconds
        {
            get
            {
                double h = MaxHoursOverride != null ? MaxHoursOverride() : 0;
                return (h > 0 ? h : config.maxOfflineHours) * 3600.0;
            }
        }

        public OfflineReport Calculate(double secondsAway)
        {
            secondsAway = Math.Max(0, secondsAway);
            double counted = Math.Min(secondsAway, MaxOfflineSeconds);
            double boost = boostFactor != null ? Math.Max(1.0, boostFactor()) : 1.0;
            double money = producers.IncomePerSecond / boost * counted * config.offlineEfficiency;
            long beatCount = (long)(producers.BeatsPerSecond * counted);
            return new OfflineReport(secondsAway, counted, money, beatCount);
        }

        /// <summary>Call when the player comes back. Creates a pending report if it's worth showing.</summary>
        public void Process(double secondsAway)
        {
            if (secondsAway < config.minOfflineSeconds) return;

            var report = Calculate(secondsAway);
            if (report.Money <= 0) return;

            // If an old report wasn't collected yet, give it now so nothing is lost.
            if (HasPending) Collect();

            pending = report;
            HasPending = true;
            ReportReady?.Invoke(report);
        }

        /// <summary>Gives the pending money to the player. Multiplier is for "x2 with ad" (Phase 17).</summary>
        public void Collect(double multiplier = 1.0)
        {
            if (!HasPending) return;
            HasPending = false;

            double money = pending.Money * Math.Max(1.0, multiplier);
            beats.AddPrecomputed(pending.Beats, money, BeatSource.Offline);
            Collected?.Invoke(money);
        }
    }
}
