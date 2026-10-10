using System;

namespace BeatFactory
{
    /// <summary>
    /// Runtime state of one producer: its level and production timer.
    /// All formulas for this producer's cost and income live here.
    /// </summary>
    public class Producer
    {
        readonly EconomyConfig config;

        public ProducerData Data { get; }
        public int Index { get; }
        public int Level { get; private set; }

        /// <summary>Seconds of work done on the current beat.</summary>
        public float CycleTimer { get; private set; }

        public Producer(ProducerData data, int index, EconomyConfig config)
        {
            Data = data;
            Index = index;
            this.config = config;
        }

        public bool IsHired => Level > 0;
        public bool IsMaxLevel => Data.maxLevel > 0 && Level >= Data.maxLevel;
        public float Progress => IsHired ? Math.Min(1f, CycleTimer / Data.cycleSeconds) : 0f;

        double Growth => Data.costGrowth > 0 ? Data.costGrowth : config.upgradeCostGrowth;

        /// <summary>Price of the next level (hire price when not hired yet).</summary>
        public double NextCost => Level == 0
            ? Data.hireCost
            : Data.upgradeBaseCost * Math.Pow(Growth, Level - 1);

        /// <summary>Levels that can still be bought (int.MaxValue when there is no max level).</summary>
        public int LevelsLeft => Data.maxLevel > 0 ? Math.Max(0, Data.maxLevel - Level) : int.MaxValue;

        /// <summary>Total price of the next <paramref name="count"/> levels (geometric series, no loops).</summary>
        public double CostOfLevels(int count)
        {
            count = Math.Min(count, LevelsLeft);
            if (count <= 0) return 0;

            double total = 0;
            int level = Level;
            if (level == 0)
            {
                total += Data.hireCost; // hiring is the first "level"
                count--;
                level = 1;
            }
            if (count > 0)
            {
                double g = Growth;
                double first = Data.upgradeBaseCost * Math.Pow(g, level - 1);
                total += Math.Abs(g - 1) < 1e-9 ? first * count : first * (Math.Pow(g, count) - 1) / (g - 1);
            }
            return total;
        }

        /// <summary>How many levels can be bought with <paramref name="money"/> (0 if not even one).</summary>
        public int MaxAffordableLevels(double money)
        {
            if (LevelsLeft <= 0 || money < NextCost) return 0;

            double budget = money;
            int count = 0;
            int level = Level;
            if (level == 0)
            {
                money -= Data.hireCost;
                count = 1;
                level = 1;
            }

            double g = Growth;
            double first = Data.upgradeBaseCost * Math.Pow(g, level - 1);
            int more;
            if (first <= 0) more = 0;
            else if (Math.Abs(g - 1) < 1e-9) more = (int)Math.Min(int.MaxValue / 2, Math.Floor(money / first));
            else more = (int)Math.Min(int.MaxValue / 2, Math.Floor(Math.Log(money * (g - 1) / first + 1) / Math.Log(g)));

            count += Math.Max(0, more);
            count = Math.Min(count, LevelsLeft);
            // Guard against floating point rounding: never report more than we can pay.
            while (count > 0 && CostOfLevels(count) > budget * (1 + 1e-12)) count--;
            return count;
        }

        /// <summary>How many level milestones (every N levels) were reached.</summary>
        public int Milestones => config.producerMilestoneEvery > 0 ? Level / config.producerMilestoneEvery : 0;

        /// <summary>Money per beat before global multipliers.</summary>
        public double BaseIncomePerBeat => IncomeAtLevel(Level);

        public double IncomeAtLevel(int level)
        {
            if (level <= 0) return 0;
            int milestones = config.producerMilestoneEvery > 0 ? level / config.producerMilestoneEvery : 0;
            return Data.baseIncome * level * Math.Pow(config.producerMilestoneMultiplier, milestones);
        }

        /// <summary>Adds work time. Returns how many beats were finished.</summary>
        internal int AddTime(float seconds)
        {
            if (!IsHired) return 0;
            CycleTimer += seconds;
            if (CycleTimer < Data.cycleSeconds) return 0;

            int beats = (int)(CycleTimer / Data.cycleSeconds);
            CycleTimer -= beats * Data.cycleSeconds;
            return beats;
        }

        internal void SetLevel(int level)
        {
            Level = Math.Max(0, Data.maxLevel > 0 ? Math.Min(level, Data.maxLevel) : level);
            if (Level == 0) CycleTimer = 0;
        }

        internal void SetTimer(float timer) => CycleTimer = Math.Max(0f, Math.Min(timer, Data.cycleSeconds));
    }
}
