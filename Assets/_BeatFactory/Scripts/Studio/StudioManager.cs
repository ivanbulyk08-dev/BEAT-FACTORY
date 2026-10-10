using System;
using System.Collections.Generic;

namespace BeatFactory
{
    /// <summary>
    /// Studio progression: Bedroom Studio -> ... -> Music Empire.
    /// The current studio boosts production speed and income, and unlocks later genres.
    /// </summary>
    public class StudioManager
    {
        readonly List<StudioData> studios = new List<StudioData>();
        readonly EconomyManager economy;

        /// <summary>0 = first studio (Bedroom Studio).</summary>
        public int Level { get; private set; }

        /// <summary>Fired when the player buys a studio upgrade (use for celebrations).</summary>
        public event Action<StudioData> StudioUpgraded;

        /// <summary>Fired whenever the current studio changes, including loading a save (use for visuals/UI).</summary>
        public event Action<StudioData> CurrentChanged;

        public StudioManager(IEnumerable<StudioData> definitions, EconomyManager economy)
        {
            this.economy = economy;
            foreach (var s in definitions)
                if (s != null) studios.Add(s);
        }

        public IReadOnlyList<StudioData> Studios => studios;
        public int Count => studios.Count;

        public StudioData Current => Level < studios.Count ? studios[Level] : null;
        public StudioData Next => Level + 1 < studios.Count ? studios[Level + 1] : null;
        public bool IsMaxLevel => Next == null;

        public StudioData Get(int level) => level >= 0 && level < studios.Count ? studios[level] : null;

        public double SpeedMultiplier => Current != null ? Current.speedMultiplier : 1.0;
        public double IncomeMultiplier => Current != null ? Current.incomeMultiplier : 1.0;

        public bool CanUpgrade() => Next != null && economy.CanAfford(Next.upgradeCost);

        public bool TryUpgrade()
        {
            var next = Next;
            if (next == null) return false;
            if (!economy.TrySpend(next.upgradeCost)) return false;

            Level++;
            CurrentChanged?.Invoke(next);
            StudioUpgraded?.Invoke(next);
            return true;
        }

        /// <summary>Upgrades for free (debug / rewards).</summary>
        public void ForceUpgrade()
        {
            var next = Next;
            if (next == null) return;
            Level++;
            CurrentChanged?.Invoke(next);
            StudioUpgraded?.Invoke(next);
        }

        /// <summary>Restores progress from a save file / resets on prestige.</summary>
        public void LoadState(int level)
        {
            Level = Math.Max(0, Math.Min(level, Math.Max(0, studios.Count - 1)));
            CurrentChanged?.Invoke(Current);
        }
    }
}
