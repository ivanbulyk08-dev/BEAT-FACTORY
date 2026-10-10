using System;

namespace BeatFactory
{
    public enum BeatSource
    {
        Manual,
        Producer,
        Offline,
    }

    /// <summary>Information about beats that were just created (used by UI for feedback).</summary>
    public readonly struct BeatResult
    {
        public readonly double Reward;
        public readonly int Count;
        public readonly BeatSource Source;
        /// <summary>Best quality in this batch.</summary>
        public readonly BeatQuality Quality;
        /// <summary>Song title for HIT/VIRAL beats, otherwise null.</summary>
        public readonly string Title;
        /// <summary>Money value of the streams these beats earn (before platform multipliers). 0 for NORMAL beats.</summary>
        public readonly double StreamValue;

        public BeatResult(double reward, int count, BeatSource source, BeatQuality quality, string title, double streamValue = 0)
        {
            Reward = reward;
            Count = count;
            Source = source;
            Quality = quality;
            Title = title;
            StreamValue = streamValue;
        }

        public bool IsSpecial => Quality >= BeatQuality.Hit;
    }

    /// <summary>
    /// Creates beats (manually or by producers), rolls their quality, applies income multipliers and pays the reward.
    /// Every beat in the game goes through RegisterBeats, so multipliers (fame, genre, studio,
    /// artists, boosts) only need to be added in one place.
    /// </summary>
    public class BeatManager
    {
        readonly EconomyManager economy;
        readonly EconomyConfig config;
        readonly MultiplierStack income;

        public BeatQualityConfig QualityConfig { get; }
        public BeatQualityRoller Quality { get; }

        public long TotalBeats { get; private set; }
        public long ManualBeats { get; private set; }
        public long HitBeats { get; private set; }
        public long ViralBeats { get; private set; }

        /// <summary>Fired after beats were created and paid.</summary>
        public event Action<BeatResult> BeatsCreated;

        /// <summary>Fired when TotalBeats changes. Argument: new total.</summary>
        public event Action<long> BeatCountChanged;

        public BeatManager(EconomyManager economy, EconomyConfig config, BeatQualityConfig qualityConfig, MultiplierStack income)
        {
            this.economy = economy;
            this.config = config;
            this.income = income;
            QualityConfig = qualityConfig;
            Quality = new BeatQualityRoller(qualityConfig);
        }

        /// <summary>Multiplier applied to every beat (fame x genre x studio x artists x boosts).</summary>
        public double GlobalMultiplier => income.Total;

        /// <summary>Producers' base income per second (set by GameManager) — part of it is added to every manual beat.</summary>
        public Func<double> ProducerBaseIncome { get; set; }

        /// <summary>Base value of one manual beat before multipliers.</summary>
        public double ManualBeatBase =>
            config.manualBeatBaseReward + config.manualBeatProducerShare * (ProducerBaseIncome?.Invoke() ?? 0);

        /// <summary>Money for one NORMAL press of MAKE BEAT.</summary>
        public double ManualBeatReward => ManualBeatBase * GlobalMultiplier;

        public BeatResult MakeManualBeat()
        {
            ManualBeats++;
            return RegisterBeats(ManualBeatBase, 1, BeatSource.Manual);
        }

        /// <summary>
        /// Creates <paramref name="count"/> beats worth <paramref name="baseRewardPerBeat"/> each (before multipliers),
        /// rolls their quality and gives the money to the player.
        /// </summary>
        public BeatResult RegisterBeats(double baseRewardPerBeat, int count, BeatSource source) =>
            RegisterBeats(baseRewardPerBeat, count, source, null);

        /// <summary>One beat of a guaranteed quality (e.g. INSTANT VIRAL reward).</summary>
        public BeatResult CreateGuaranteedBeat(double baseReward, BeatQuality quality, BeatSource source) =>
            RegisterBeats(baseReward, 1, source, quality);

        BeatResult RegisterBeats(double baseRewardPerBeat, int count, BeatSource source, BeatQuality? forcedQuality)
        {
            if (count <= 0) return new BeatResult(0, 0, source, BeatQuality.Normal, null);

            double qualitySum;
            double streamSum; // sum of streamShare for these beats (share of the BASE reward)
            var best = BeatQuality.Normal;

            if (count <= QualityConfig.maxIndividualRolls || forcedQuality.HasValue)
            {
                qualitySum = 0;
                streamSum = 0;
                for (int i = 0; i < count; i++)
                {
                    var q = forcedQuality ?? Quality.Roll();
                    var tier = QualityConfig.GetTier(q);
                    qualitySum += tier.multiplier;
                    streamSum += tier.streamShare;
                    if (q > best) best = q;
                    if (q == BeatQuality.Hit) HitBeats++;
                    else if (q == BeatQuality.Viral) ViralBeats++;
                }
            }
            else
            {
                // Huge batches (e.g. offline): use the average so it's fast and fair.
                qualitySum = count * Quality.AverageMultiplier;
                streamSum = count * Quality.AverageStreamShare;
            }

            double multiplier = baseRewardPerBeat * GlobalMultiplier;
            double reward = qualitySum * multiplier;
            double streamValue = streamSum * multiplier;
            TotalBeats += count;
            economy.AddMoney(reward);

            string title = best >= BeatQuality.Hit ? QualityConfig.GetRandomTitle(Quality.Random) : null;
            var result = new BeatResult(reward, count, source, best, title, streamValue);
            BeatCountChanged?.Invoke(TotalBeats);
            BeatsCreated?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Adds beats whose reward was already calculated elsewhere (offline earnings).
        /// No quality roll and no multipliers are applied here.
        /// </summary>
        public void AddPrecomputed(long count, double reward, BeatSource source)
        {
            if (count < 0) count = 0;
            TotalBeats += count;
            economy.AddMoney(reward);
            BeatCountChanged?.Invoke(TotalBeats);
            BeatsCreated?.Invoke(new BeatResult(reward, (int)Math.Min(count, int.MaxValue), source, BeatQuality.Normal, null));
        }

        /// <summary>Restores values from a save file. Used by the SaveManager.</summary>
        public void LoadState(long totalBeats, long manualBeats, long hitBeats, long viralBeats)
        {
            TotalBeats = Math.Max(0, totalBeats);
            ManualBeats = Math.Max(0, manualBeats);
            HitBeats = Math.Max(0, hitBeats);
            ViralBeats = Math.Max(0, viralBeats);
            BeatCountChanged?.Invoke(TotalBeats);
        }
    }
}
