using System;
using System.Collections.Generic;

namespace BeatFactory
{
    [Serializable]
    public class BoostSave
    {
        public int type;
        public long endUtcTicks;
    }

    /// <summary>
    /// Temporary boosts with real-time timers (they keep counting down while the game is closed).
    /// Activating an active boost extends it. One free boost every N minutes.
    /// </summary>
    public class BoostManager
    {
        readonly BoostConfig config;
        readonly Dictionary<BoostType, DateTime> ends = new Dictionary<BoostType, DateTime>();
        readonly List<BoostType> expired = new List<BoostType>();
        DateTime nextFreeUtc = DateTime.MinValue;
        float checkTimer;

        /// <summary>Fired when a boost starts or is extended. Arguments: boost, seconds added.</summary>
        public event Action<BoostDefinition, float> BoostStarted;

        /// <summary>Fired whenever the set of active boosts changes (start, extend, expire, load).</summary>
        public event Action Changed;

        public BoostManager(BoostConfig config)
        {
            this.config = config;
        }

        public BoostConfig Config => config;
        static DateTime Now => DateTime.UtcNow;

        public bool IsActive(BoostType type) => ends.TryGetValue(type, out var end) && end > Now;

        public double RemainingSeconds(BoostType type) =>
            ends.TryGetValue(type, out var end) ? Math.Max(0, (end - Now).TotalSeconds) : 0;

        double MultiplierOf(BoostType type)
        {
            if (!IsActive(type)) return 1.0;
            var def = config.Get(type);
            return def != null ? def.multiplier : 1.0;
        }

        public double IncomeMultiplier => MultiplierOf(BoostType.Money);
        public double SpeedMultiplier => MultiplierOf(BoostType.Speed);
        public double ProducerIncomeMultiplier => MultiplierOf(BoostType.Producer);
        public float ViralChanceMultiplier => (float)MultiplierOf(BoostType.Viral);

        /// <summary>How much boosts currently inflate producer income (offline earnings divide this out).</summary>
        public double ProducerIncomeFactor => IncomeMultiplier * SpeedMultiplier * ProducerIncomeMultiplier;

        // ------------------------------------------------------------------ activation

        public void Activate(BoostType type, float seconds)
        {
            var def = config.Get(type);
            if (def == null || seconds <= 0) return;

            var start = IsActive(type) ? ends[type] : Now;
            var end = start.AddSeconds(seconds);
            var cap = Now.AddMinutes(config.maxStackMinutes);
            if (end > cap) end = cap;
            ends[type] = end;

            Changed?.Invoke();
            BoostStarted?.Invoke(def, seconds);
        }

        public void Activate(BoostType type)
        {
            var def = config.Get(type);
            if (def != null) Activate(type, def.durationSeconds);
        }

        public bool FreeBoostReady => Now >= nextFreeUtc;
        public double SecondsUntilFreeBoost => Math.Max(0, (nextFreeUtc - Now).TotalSeconds);

        public bool TryActivateFree(BoostType type)
        {
            if (!FreeBoostReady) return false;
            nextFreeUtc = Now.AddMinutes(config.freeBoostCooldownMinutes);
            Activate(type);
            return true;
        }

        /// <summary>Call regularly (GameManager does it). Detects expired boosts twice per second.</summary>
        public void Tick(float deltaTime)
        {
            checkTimer += deltaTime;
            if (checkTimer < 0.5f) return;
            checkTimer = 0f;

            expired.Clear();
            var now = Now;
            foreach (var pair in ends)
                if (pair.Value <= now) expired.Add(pair.Key);
            if (expired.Count == 0) return;

            foreach (var t in expired) ends.Remove(t);
            Changed?.Invoke();
        }

        // ------------------------------------------------------------------ saving

        public List<BoostSave> GetSaveData()
        {
            var list = new List<BoostSave>();
            foreach (var pair in ends)
                if (pair.Value > Now) list.Add(new BoostSave { type = (int)pair.Key, endUtcTicks = pair.Value.Ticks });
            return list;
        }

        public long NextFreeUtcTicks => nextFreeUtc.Ticks;

        public void LoadState(List<BoostSave> boosts, long nextFreeTicks)
        {
            ends.Clear();
            if (boosts != null)
            {
                var now = Now;
                foreach (var b in boosts)
                {
                    var end = new DateTime(b.endUtcTicks, DateTimeKind.Utc);
                    // Ignore expired boosts and clocks set far into the future.
                    if (end > now && end < now.AddMinutes(config.maxStackMinutes + 1))
                        ends[(BoostType)b.type] = end;
                }
            }

            var next = new DateTime(Math.Max(0, nextFreeTicks), DateTimeKind.Utc);
            // Clock moved back? Don't make the player wait longer than one cooldown.
            var maxNext = Now.AddMinutes(config.freeBoostCooldownMinutes);
            nextFreeUtc = next > maxNext ? maxNext : next;

            Changed?.Invoke();
        }
    }
}
