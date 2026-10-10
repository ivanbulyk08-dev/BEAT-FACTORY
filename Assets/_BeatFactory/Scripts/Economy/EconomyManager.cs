using System;

namespace BeatFactory
{
    /// <summary>
    /// Owns the player's currencies (Money and Fame Points).
    /// Every system that earns or spends money goes through this class,
    /// so the UI only needs to listen to its events.
    /// </summary>
    public class EconomyManager
    {
        readonly EconomyConfig config;

        /// <summary>Current spendable money.</summary>
        public double Money { get; private set; }

        /// <summary>All money earned during the current run (resets on prestige). Used to calculate Fame.</summary>
        public double RunEarnings { get; private set; }

        /// <summary>All money ever earned, never resets. Used for stats.</summary>
        public double LifetimeEarnings { get; private set; }

        /// <summary>Permanent prestige currency.</summary>
        public double FamePoints { get; private set; }

        /// <summary>Fired whenever Money changes. Argument: new total.</summary>
        public event Action<double> MoneyChanged;

        /// <summary>Fired when money is earned. Argument: amount added (use it for "+$100" popups).</summary>
        public event Action<double> MoneyEarned;

        /// <summary>Fired whenever Fame Points change. Argument: new total.</summary>
        public event Action<double> FameChanged;

        public EconomyManager(EconomyConfig config)
        {
            this.config = config;
            Money = config.startingMoney;
        }

        /// <summary>Permanent multiplier from Fame Points (1 + points * bonus).</summary>
        public double FameMultiplier => 1.0 + FamePoints * config.incomeBonusPerFamePoint;

        public void AddMoney(double amount)
        {
            if (!IsValidAmount(amount)) return;

            // Extremely late game: never overflow to Infinity (it would break saving and math).
            Money = Clamp(Money + amount);
            RunEarnings = Clamp(RunEarnings + amount);
            LifetimeEarnings = Clamp(LifetimeEarnings + amount);

            MoneyEarned?.Invoke(amount);
            MoneyChanged?.Invoke(Money);
        }

        public bool CanAfford(double cost) => cost >= 0 && Money >= cost;

        /// <summary>Spends money if the player has enough. Returns false (and changes nothing) otherwise.</summary>
        public bool TrySpend(double cost)
        {
            if (!IsValidAmount(cost) && cost != 0) return false;
            if (!CanAfford(cost)) return false;

            Money = Math.Max(0, Money - cost);
            MoneyChanged?.Invoke(Money);
            return true;
        }

        public void AddFame(double amount)
        {
            if (!IsValidAmount(amount)) return;

            FamePoints += amount;
            FameChanged?.Invoke(FamePoints);
        }

        /// <summary>Prestige reset: money goes back to the start value, Fame and lifetime stats are kept.</summary>
        public void ResetForPrestige()
        {
            Money = config.startingMoney;
            RunEarnings = 0;
            MoneyChanged?.Invoke(Money);
        }

        /// <summary>Restores values from a save file. Used by the SaveManager.</summary>
        public void LoadState(double money, double runEarnings, double lifetimeEarnings, double famePoints)
        {
            Money = Sanitize(money);
            RunEarnings = Sanitize(runEarnings);
            LifetimeEarnings = Sanitize(lifetimeEarnings);
            FamePoints = Sanitize(famePoints);

            MoneyChanged?.Invoke(Money);
            FameChanged?.Invoke(FamePoints);
        }

        static double Clamp(double value) => double.IsInfinity(value) || value > double.MaxValue ? double.MaxValue : value;

        static bool IsValidAmount(double amount) =>
            amount > 0 && !double.IsNaN(amount) && !double.IsInfinity(amount);

        static double Sanitize(double value) =>
            double.IsNaN(value) || double.IsInfinity(value) || value < 0 ? 0 : value;
    }
}
