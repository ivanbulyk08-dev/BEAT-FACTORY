using System;
using UnityEngine;

namespace BeatFactory
{
    public enum RewardKind { Money, Boost, Fans, Fame }

    /// <summary>
    /// A mission/challenge reward, always paid in the game's existing currencies/systems:
    /// money (scaled to the player's current income, so it stays meaningful from the basement to the end game),
    /// a timed boost (BoostManager), fans for every signed artist (ConcertManager) or Fame points (permanent prestige bonus).
    /// </summary>
    [Serializable]
    public struct GoalReward
    {
        public RewardKind kind;
        /// <summary>Money: seconds of income · Boost: seconds · Fans: percent · Fame: points.</summary>
        public double amount;
        public BoostType boost;

        public static GoalReward Money(double incomeSeconds) => new GoalReward { kind = RewardKind.Money, amount = incomeSeconds };
        public static GoalReward Boost(BoostType type, double seconds) => new GoalReward { kind = RewardKind.Boost, amount = seconds, boost = type };
        public static GoalReward Fans(double percent) => new GoalReward { kind = RewardKind.Fans, amount = percent };
        public static GoalReward Fame(double points) => new GoalReward { kind = RewardKind.Fame, amount = points };

        /// <summary>Money paid right now for N seconds of income (never less than a few manual beats).</summary>
        /// <summary>All mission / challenge rewards (money, boost time, fans) are scaled by this. 0.5 = half.</summary>
        public const double Scale = 0.5;

        public static double MoneyFor(GameManager gm, double seconds)
        {
            seconds *= Scale;
            double income = gm.Producers.IncomePerSecond / Math.Max(1.0, gm.Boosts.ProducerIncomeFactor);
            double floor = gm.Beats.ManualBeatReward * Math.Max(3, seconds / 10);
            return Math.Max(floor, income * seconds);
        }

        public string Describe(GameManager gm)
        {
            switch (kind)
            {
                case RewardKind.Money: return "+" + NumberFormatter.FormatMoney(MoneyFor(gm, amount));
                case RewardKind.Boost:
                {
                    var def = gm.Boosts.Config.Get(boost);
                    return (def != null ? def.DisplayName : boost.ToString().ToUpperInvariant()) + " " + NumberFormatter.FormatDuration(amount * Scale);
                }
                case RewardKind.Fans: return Loc.F("reward.fans", "value", "+" + NumberFormatter.Percent(amount * Scale));
                default: return Loc.F("reward.fame", "value", "+" + NumberFormatter.Group(amount));
            }
        }

        public Color Color(UISkin s)
        {
            switch (kind)
            {
                case RewardKind.Money: return s.money;
                case RewardKind.Boost: return s.cyan;
                case RewardKind.Fans: return s.pink;
                default: return s.fame;
            }
        }

        /// <summary>Pays the reward through the existing systems. Returns a short text for the reward reveal.</summary>
        public string Grant(GameManager gm)
        {
            string text = Describe(gm);
            switch (kind)
            {
                case RewardKind.Money:
                    gm.Economy.AddMoney(MoneyFor(gm, amount));
                    break;
                case RewardKind.Boost:
                    gm.Boosts.Activate(boost, (float)(amount * Scale));
                    break;
                case RewardKind.Fans:
                    foreach (var a in gm.Concerts.SignedArtists())
                    {
                        var c = gm.Concerts.Career(a);
                        double gain = Math.Round(c.fans * amount * Scale / 100.0);
                        c.fans += gain;
                        c.totalFansGained += gain;
                    }
                    gm.Concerts.NotifyChanged();
                    break;
                case RewardKind.Fame:
                    gm.Economy.AddFame(amount);
                    break;
            }
            return text;
        }
    }
}
