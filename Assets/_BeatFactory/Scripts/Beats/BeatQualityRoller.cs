using System;

namespace BeatFactory
{
    /// <summary>
    /// Picks random beat qualities using the chances from BeatQualityConfig.
    /// HIT and VIRAL chances can be boosted (e.g. by a Viral Boost) with SpecialChanceMultiplier.
    /// </summary>
    public class BeatQualityRoller
    {
        readonly BeatQualityConfig config;
        readonly Random random = new Random();

        /// <summary>Multiplies the chance of HIT and VIRAL beats. 1 = normal.</summary>
        public float SpecialChanceMultiplier { get; set; } = 1f;

        public BeatQualityRoller(BeatQualityConfig config)
        {
            this.config = config;
        }

        public Random Random => random;

        public BeatQuality Roll()
        {
            float total = 0f;
            foreach (var t in config.tiers) total += Weight(t);
            if (total <= 0f) return BeatQuality.Normal;

            float r = (float)random.NextDouble() * total;
            foreach (var t in config.tiers)
            {
                r -= Weight(t);
                if (r < 0f) return t.quality;
            }
            return config.tiers[config.tiers.Length - 1].quality;
        }

        /// <summary>Average reward multiplier of one beat (used for big batches and income per second).</summary>
        public double AverageMultiplier => AverageMultiplierAt(SpecialChanceMultiplier);

        /// <summary>Average reward multiplier for a given HIT/VIRAL chance multiplier (1 = no Viral Boost).</summary>
        public double AverageMultiplierAt(float specialChanceMultiplier)
        {
            double total = 0, weighted = 0;
            foreach (var t in config.tiers)
            {
                double w = t.quality >= BeatQuality.Hit ? t.chancePercent * specialChanceMultiplier : t.chancePercent;
                total += w;
                weighted += w * t.multiplier;
            }
            return total > 0 ? weighted / total : 1;
        }

        /// <summary>Average stream value of one beat, as a share of its base reward.</summary>
        public double AverageStreamShare
        {
            get
            {
                double total = 0, weighted = 0;
                foreach (var t in config.tiers)
                {
                    double w = Weight(t);
                    total += w;
                    weighted += w * t.streamShare;
                }
                return total > 0 ? weighted / total : 0;
            }
        }

        float Weight(BeatQualityTier t) =>
            t.quality >= BeatQuality.Hit ? t.chancePercent * SpecialChanceMultiplier : t.chancePercent;
    }
}
