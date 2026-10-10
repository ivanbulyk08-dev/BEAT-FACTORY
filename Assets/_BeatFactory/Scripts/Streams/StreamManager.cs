using System;
using System.Collections.Generic;

namespace BeatFactory
{
    /// <summary>
    /// Streams: GOOD and better beats earn streams on every released platform.
    /// Streams pile up and can be cashed out for money at any time.
    /// </summary>
    public class StreamManager
    {
        readonly StreamConfig config;
        readonly EconomyManager economy;
        readonly HashSet<string> released = new HashSet<string>();

        public double Streams { get; private set; }
        public double LifetimeStreams { get; private set; }

        /// <summary>Fired when the stream count changes. Argument: new total.</summary>
        public event Action<double> StreamsChanged;

        /// <summary>Fired when the player releases on a new platform (use for celebrations).</summary>
        public event Action<StreamPlatform> PlatformReleased;

        /// <summary>Fired whenever the set of platforms changes (incl. loading).</summary>
        public event Action PlatformsChanged;

        /// <summary>Fired after cashing out. Argument: money received.</summary>
        public event Action<double> CashedOut;

        public StreamManager(StreamConfig config, EconomyManager economy, BeatManager beats)
        {
            this.config = config;
            this.economy = economy;
            ReleaseFreePlatforms();
            beats.BeatsCreated += OnBeatsCreated;
        }

        public StreamConfig Config => config;

        /// <summary>Extra stream generation (artist chemistry). Set by GameManager; 1 = none.</summary>
        public Func<double> BonusMultiplier { get; set; }
        public IReadOnlyList<StreamPlatform> Platforms => config.platforms;

        public bool IsReleased(StreamPlatform p) => released.Contains(p.id);

        /// <summary>Sum of the multipliers of all released platforms.</summary>
        public double PlatformMultiplier
        {
            get
            {
                double sum = 0;
                foreach (var p in config.platforms)
                    if (released.Contains(p.id)) sum += p.streamMultiplier;
                return sum;
            }
        }

        /// <summary>Money you'd get by cashing out right now.</summary>
        public double StreamsValue => Streams * config.MoneyPerStream;

        void OnBeatsCreated(BeatResult result)
        {
            if (result.StreamValue <= 0) return;
            double bonus = BonusMultiplier != null ? Math.Max(1.0, BonusMultiplier()) : 1.0;
            double streams = result.StreamValue * PlatformMultiplier * bonus / config.MoneyPerStream;
            if (streams <= 0) return;
            Streams += streams;
            LifetimeStreams += streams;
            StreamsChanged?.Invoke(Streams);
        }

        public double CashOut()
        {
            if (Streams <= 0) return 0;
            double money = StreamsValue;
            Streams = 0;
            economy.AddMoney(money);
            StreamsChanged?.Invoke(Streams);
            CashedOut?.Invoke(money);
            return money;
        }

        public bool CanRelease(StreamPlatform p) => !IsReleased(p) && economy.CanAfford(p.releaseCost);

        public bool CanReleaseAny()
        {
            foreach (var p in config.platforms)
                if (CanRelease(p)) return true;
            return false;
        }

        public bool TryRelease(StreamPlatform p)
        {
            if (IsReleased(p)) return false;
            if (!economy.TrySpend(p.releaseCost)) return false;
            released.Add(p.id);
            PlatformsChanged?.Invoke();
            PlatformReleased?.Invoke(p);
            return true;
        }

        public void AddStreams(double amount)
        {
            if (amount <= 0 || double.IsNaN(amount) || double.IsInfinity(amount)) return;
            Streams += amount;
            LifetimeStreams += amount;
            StreamsChanged?.Invoke(Streams);
        }

        void ReleaseFreePlatforms()
        {
            foreach (var p in config.platforms)
                if (p.releaseCost <= 0) released.Add(p.id);
        }

        public List<string> GetReleasedIds() => new List<string>(released);

        /// <summary>Restores progress from a save file / resets on prestige.</summary>
        public void LoadState(double streams, double lifetimeStreams, IEnumerable<string> releasedIds)
        {
            Streams = Math.Max(0, streams);
            LifetimeStreams = Math.Max(0, lifetimeStreams);
            released.Clear();
            ReleaseFreePlatforms();
            if (releasedIds != null)
                foreach (var id in releasedIds)
                    if (Array.Exists(config.platforms, p => p.id == id)) released.Add(id);
            StreamsChanged?.Invoke(Streams);
            PlatformsChanged?.Invoke();
        }
    }
}
