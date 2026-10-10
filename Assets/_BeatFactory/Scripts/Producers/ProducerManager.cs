using System;
using System.Collections.Generic;

namespace BeatFactory
{
    /// <summary>
    /// Owns all producers: hiring, upgrading and automatic beat production.
    /// GameManager calls Tick() once per frame for ALL producers (one Update instead of one per producer).
    /// </summary>
    public class ProducerManager
    {
        readonly List<Producer> producers = new List<Producer>();
        readonly EconomyManager economy;
        readonly BeatManager beats;
        readonly MultiplierStack speed;
        readonly MultiplierStack producerIncome;

        public IReadOnlyList<Producer> Producers => producers;

        /// <summary>Fired when a producer is hired or upgraded.</summary>
        public event Action<Producer> ProducerChanged;

        /// <summary>Fired when a producer finishes one or more beats.</summary>
        public event Action<Producer, BeatResult> ProducerProduced;

        public ProducerManager(IEnumerable<ProducerData> definitions, EconomyManager economy, BeatManager beats,
            EconomyConfig config, MultiplierStack speed, MultiplierStack producerIncome)
        {
            this.economy = economy;
            this.beats = beats;
            this.speed = speed;
            this.producerIncome = producerIncome;

            int index = 0;
            foreach (var data in definitions)
            {
                if (data == null) continue;
                producers.Add(new Producer(data, index++, config));
            }
        }

        /// <summary>Production speed multiplier for all producers (studio, boosts).</summary>
        public double SpeedMultiplier => speed.Total;

        /// <summary>Current studio level (set by GameManager). Better studios unlock new producers.</summary>
        public Func<int> StudioLevel { get; set; }

        public bool MeetsStudio(Producer p) => p.IsHired || StudioLevel == null || StudioLevel() >= p.Data.requiredStudio;

        /// <summary>A producer can be hired after the previous one is hired AND the studio is good enough.</summary>
        public bool IsUnlocked(Producer p) => (p.Index == 0 || producers[p.Index - 1].IsHired) && MeetsStudio(p);

        public bool CanLevelUp(Producer p) => IsUnlocked(p) && !p.IsMaxLevel && economy.CanAfford(p.NextCost);

        /// <summary>Hires (level 0 -> 1) or upgrades the producer if the player can pay.</summary>
        public bool TryLevelUp(Producer p) => TryBuyLevels(p, 1);

        /// <summary>
        /// How many levels a purchase in this mode would buy right now
        /// (x1 = 1, x10 = 10, MAX = as many as affordable but at least 1 for showing the price).
        /// </summary>
        public int GetBuyAmount(Producer p, BuyMode mode)
        {
            switch (mode)
            {
                case BuyMode.Ten: return Math.Min(10, p.LevelsLeft);
                case BuyMode.Max: return Math.Max(1, Math.Min(p.MaxAffordableLevels(economy.Money), p.LevelsLeft));
                default: return Math.Min(1, p.LevelsLeft);
            }
        }

        public bool CanBuy(Producer p, int count) =>
            count > 0 && IsUnlocked(p) && !p.IsMaxLevel && economy.CanAfford(p.CostOfLevels(count));

        /// <summary>Buys several levels at once (all or nothing).</summary>
        public bool TryBuyLevels(Producer p, int count)
        {
            if (!IsUnlocked(p) || p.IsMaxLevel || count <= 0) return false;
            count = Math.Min(count, p.LevelsLeft);
            if (!economy.TrySpend(p.CostOfLevels(count))) return false;

            p.SetLevel(p.Level + count);
            ProducerChanged?.Invoke(p);
            return true;
        }

        /// <summary>Extra multiplier that only applies to producers (e.g. PRODUCER BOOST).</summary>
        public double ProducerIncomeMultiplier => producerIncome.Total;

        /// <summary>Money per beat for this producer including all multipliers (for UI).</summary>
        public double GetIncomePerBeat(Producer p) => p.BaseIncomePerBeat * ProducerIncomeMultiplier * beats.GlobalMultiplier;

        /// <summary>Effective seconds per beat including speed multipliers (for UI).</summary>
        public double GetCycleSeconds(Producer p) => p.Data.cycleSeconds / SpeedMultiplier;

        /// <summary>
        /// Average total money per second from all producers, including the average beat-quality bonus
        /// (used by HUD and offline earnings).
        /// </summary>
        public double IncomePerSecond
        {
            get
            {
                double sum = 0;
                foreach (var p in producers)
                    if (p.IsHired) sum += p.BaseIncomePerBeat / p.Data.cycleSeconds;
                return sum * SpeedMultiplier * ProducerIncomeMultiplier * beats.GlobalMultiplier * beats.Quality.AverageMultiplier;
            }
        }

        /// <summary>Producer income per second BEFORE global multipliers and beat quality (base for rewards).</summary>
        public double BaseIncomePerSecond
        {
            get
            {
                double sum = 0;
                foreach (var p in producers)
                    if (p.IsHired) sum += p.BaseIncomePerBeat / p.Data.cycleSeconds;
                return sum * SpeedMultiplier * ProducerIncomeMultiplier;
            }
        }

        /// <summary>Total beats per second from all producers (used for offline earnings).</summary>
        public double BeatsPerSecond
        {
            get
            {
                double sum = 0;
                foreach (var p in producers)
                    if (p.IsHired) sum += 1.0 / p.Data.cycleSeconds;
                return sum * SpeedMultiplier;
            }
        }

        public void Tick(float deltaTime)
        {
            float scaled = deltaTime * (float)SpeedMultiplier;
            for (int i = 0; i < producers.Count; i++)
            {
                var p = producers[i];
                int finished = p.AddTime(scaled);
                if (finished <= 0) continue;

                var result = beats.RegisterBeats(p.BaseIncomePerBeat * ProducerIncomeMultiplier, finished, BeatSource.Producer);
                ProducerProduced?.Invoke(p, result);
            }
        }

        public Producer FindById(string id) => producers.Find(p => p.Data.id == id);

        /// <summary>Fires everyone (prestige).</summary>
        public void ResetAll()
        {
            foreach (var p in producers)
            {
                p.SetLevel(0);
                p.SetTimer(0);
                ProducerChanged?.Invoke(p);
            }
        }

        /// <summary>Restores a producer from a save file. Used by the SaveManager.</summary>
        public void LoadProducer(string id, int level, float timer)
        {
            var p = FindById(id);
            if (p == null) return;
            p.SetLevel(level);
            p.SetTimer(timer);
            ProducerChanged?.Invoke(p);
        }
    }
}
