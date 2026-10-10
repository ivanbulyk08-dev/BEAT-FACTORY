using System;

namespace BeatFactory
{
    /// <summary>
    /// RESTART LABEL: resets the run (money, producers, genres, studio, artists, streams)
    /// in exchange for Fame Points, which give a permanent income bonus.
    /// </summary>
    public class PrestigeManager
    {
        readonly GameManager game;
        readonly EconomyConfig config;

        /// <summary>How many times the player restarted the label.</summary>
        public int Count { get; private set; }

        /// <summary>Fired after a restart. Argument: Fame gained.</summary>
        public event Action<double> Prestiged;

        public PrestigeManager(GameManager game, EconomyConfig config)
        {
            this.game = game;
            this.config = config;
        }

        public double MinRunEarnings => config.prestigeMinRunEarnings;

        /// <summary>Fame you would get by restarting now.</summary>
        public double PendingFame
        {
            get
            {
                double earned = game.Economy.RunEarnings;
                if (earned <= 0) return 0;
                return Math.Floor(Math.Pow(earned / config.fameUnitEarnings, config.fameExponent));
            }
        }

        /// <summary>0..1 progress towards the first possible restart of this run.</summary>
        public double UnlockProgress => MinRunEarnings <= 0 ? 1 : Math.Min(1, game.Economy.RunEarnings / MinRunEarnings);

        public bool CanPrestige => game.Economy.RunEarnings >= MinRunEarnings && PendingFame >= 1;

        /// <summary>Income multiplier after restarting now (for the "x2.5 → x4" preview).</summary>
        public double MultiplierAfterPrestige =>
            1.0 + (game.Economy.FamePoints + PendingFame) * config.incomeBonusPerFamePoint;

        public bool TryPrestige()
        {
            if (!CanPrestige) return false;

            // Money waiting in the Welcome Back popup still counts towards this run's Fame.
            game.Offline.Collect();
            double fame = PendingFame;

            Count++;
            game.Economy.ResetForPrestige();
            game.Producers.ResetAll();
            game.Studios.LoadState(0);
            game.Genres.LoadState(1);
            game.Artists.LoadState(null);
            game.Streams.LoadState(0, game.Streams.LifetimeStreams, null);
            game.Economy.AddFame(fame);

            Prestiged?.Invoke(fame);
            return true;
        }

        public void LoadState(int count) => Count = Math.Max(0, count);
    }
}
