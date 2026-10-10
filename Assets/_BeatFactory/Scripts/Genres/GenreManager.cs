using System;
using System.Collections.Generic;

namespace BeatFactory
{
    /// <summary>
    /// Genres are unlocked in order. Your newest (best) genre is your current genre:
    /// its multiplier applies to all beats and its colors theme the game.
    /// </summary>
    public class GenreManager
    {
        readonly List<GenreData> genres = new List<GenreData>();
        readonly EconomyManager economy;
        readonly Func<int> getStudioLevel;

        /// <summary>How many genres are unlocked (the first one is always unlocked).</summary>
        public int UnlockedCount { get; private set; } = 1;

        /// <summary>Fired when the player buys a new genre (use for celebrations).</summary>
        public event Action<GenreData> GenreUnlocked;

        /// <summary>Fired whenever the current genre changes, including loading a save (use for theme/UI).</summary>
        public event Action<GenreData> CurrentChanged;

        public GenreManager(IEnumerable<GenreData> definitions, EconomyManager economy, Func<int> getStudioLevel)
        {
            this.economy = economy;
            this.getStudioLevel = getStudioLevel;
            foreach (var g in definitions)
                if (g != null) genres.Add(g);
        }

        public IReadOnlyList<GenreData> Genres => genres;

        public GenreData Current => genres.Count > 0 ? genres[UnlockedCount - 1] : null;

        /// <summary>The next genre to unlock, or null if all are unlocked.</summary>
        public GenreData Next => UnlockedCount < genres.Count ? genres[UnlockedCount] : null;

        public double Multiplier => Current != null ? Current.incomeMultiplier : 1.0;

        public bool IsUnlocked(GenreData g) => genres.IndexOf(g) < UnlockedCount;

        public bool MeetsStudioRequirement(GenreData g) => getStudioLevel() >= g.requiredStudioLevel;

        public bool CanUnlockNext()
        {
            var next = Next;
            return next != null && MeetsStudioRequirement(next) && economy.CanAfford(next.unlockCost);
        }

        public bool TryUnlockNext()
        {
            var next = Next;
            if (next == null || !MeetsStudioRequirement(next)) return false;
            if (!economy.TrySpend(next.unlockCost)) return false;

            UnlockedCount++;
            CurrentChanged?.Invoke(next);
            GenreUnlocked?.Invoke(next);
            return true;
        }

        /// <summary>Unlocks the next genre ignoring price and studio (debug / rewards).</summary>
        public void ForceUnlockNext()
        {
            var next = Next;
            if (next == null) return;
            UnlockedCount++;
            CurrentChanged?.Invoke(next);
            GenreUnlocked?.Invoke(next);
        }

        /// <summary>Restores progress from a save file / resets on prestige.</summary>
        public void LoadState(int unlockedCount)
        {
            UnlockedCount = Math.Max(1, Math.Min(unlockedCount, Math.Max(1, genres.Count)));
            CurrentChanged?.Invoke(Current);
        }
    }
}
