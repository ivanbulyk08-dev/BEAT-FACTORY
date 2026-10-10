using System;
using System.Collections.Generic;

namespace BeatFactory
{
    /// <summary>Where "now" comes from. Local device time for now; a server clock can be plugged in later.</summary>
    public interface ITimeSource
    {
        DateTime UtcNow { get; }
    }

    public class DeviceTime : ITimeSource
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    /// <summary>A challenge template: which stat grows, how the target scales with the player's progress, the reward.</summary>
    public class ChallengeTemplate
    {
        public string id;
        public string titleFormat;   // "Make {0} beats"
        public StatId stat;
        public Func<GameManager, double> target;
        public Func<GameManager, bool> available;
        public GoalReward reward;
        public Func<double, string> format;
    }

    /// <summary>Saved state of one active challenge. Progress = stat now − stat when the period started.</summary>
    [Serializable]
    public class ChallengeState
    {
        public string templateId;
        public double baseline;
        public double target;
        public bool completed;
        public bool claimed;
    }

    [Serializable]
    public class ChallengeSetSave
    {
        public long periodKey = -1;
        public List<ChallengeState> items = new List<ChallengeState>();
    }

    /// <summary>
    /// Daily (3) and weekly (4) challenges. Daily resets at local midnight, weekly on Monday 00:00 local time.
    /// Progress is validated through the real gameplay stats (no manual counters): a challenge only grows when the
    /// underlying system's number grows. Moving the device clock backwards never triggers a new set.
    /// No push notifications; nothing is lost by skipping days (it's a bonus, not a chore).
    /// </summary>
    public class ChallengeManager
    {
        public const int DailyCount = 3, WeeklyCount = 4;

        readonly GameManager game;
        readonly StatsTracker stats;
        readonly ITimeSource time;
        ChallengeSetSave daily = new ChallengeSetSave(), weekly = new ChallengeSetSave();
        long lastSeenTicks;
        float timer;

        public event Action<string, bool> Completed; // title, isWeekly
        public event Action Changed;

        public ChallengeManager(GameManager game, StatsTracker stats, ITimeSource time)
        {
            this.game = game;
            this.stats = stats;
            this.time = time;
        }

        // ------------------------------------------------------------------ templates (targets scale with progression)

        static double Income(GameManager g) => Math.Max(1, g.Producers.IncomePerSecond / Math.Max(1.0, g.Boosts.ProducerIncomeFactor));
        // ~streams per second while playing (streams follow producer income; based on the current run, not lifetime totals)
        static double StreamRate(GameManager g) => Income(g) * 0.08 * Math.Max(0.5, g.Streams.PlatformMultiplier);
        static double Nice(double v)
        {
            if (v < 10) return Math.Ceiling(v);
            double mag = Math.Pow(10, Math.Floor(Math.Log10(v)));
            return Math.Ceiling(v / (mag / 2)) * (mag / 2);
        }
        static string Num(double v) => NumberFormatter.Format(v);
        static string Cash(double v) => NumberFormatter.FormatMoney(v);
        static bool HasArtist(GameManager g) => g.Artists.SignedCount > 0;
        static bool CanSignMore(GameManager g) => g.Artists.SignedCount < g.Artists.Artists.Count;

        public static readonly ChallengeTemplate[] DailyPool =
        {
            new ChallengeTemplate { id = "d_beats", titleFormat = "Make {0} beats", stat = StatId.BeatsMade, format = Num,
                target = g => Nice(150 + 60 * g.Studios.Level), reward = GoalReward.Money(300) },
            new ChallengeTemplate { id = "d_money", titleFormat = "Earn {0}", stat = StatId.MoneyEarned, format = Cash,
                target = g => Nice(Math.Max(1e5, Income(g) * 900)), reward = GoalReward.Money(300) },
            new ChallengeTemplate { id = "d_upgrades", titleFormat = "Upgrade producers {0} times", stat = StatId.ProducerUpgrades, format = Num,
                target = g => 3 + g.Studios.Level / 2, reward = GoalReward.Boost(BoostType.Producer, 300) },
            new ChallengeTemplate { id = "d_fans", titleFormat = "Gain {0} fans", stat = StatId.TotalFans, format = Num, available = HasArtist,
                target = g => Nice(Math.Max(2000, g.Stats.Value(StatId.TotalFans) * 0.02)), reward = GoalReward.Money(300) },
            new ChallengeTemplate { id = "d_concert", titleFormat = "Complete {0} concert", stat = StatId.Concerts, format = Num, available = HasArtist,
                target = g => 1, reward = GoalReward.Fans(5) },
            new ChallengeTemplate { id = "d_ad", titleFormat = "Watch {0} ad for a boost", stat = StatId.AdsWatched, format = Num,
                target = g => 1, reward = GoalReward.Boost(BoostType.Money, 300) },
            new ChallengeTemplate { id = "d_hit", titleFormat = "Make {0} HIT beats", stat = StatId.HitBeats, format = Num,
                target = g => 2 + g.Studios.Level / 3, reward = GoalReward.Boost(BoostType.Viral, 180) },
            new ChallengeTemplate { id = "d_streams", titleFormat = "Generate {0} streams", stat = StatId.Streams, format = Num,
                target = g => Nice(Math.Max(1e4, StreamRate(g) * 1800)), reward = GoalReward.Money(300) },
        };

        public static readonly ChallengeTemplate[] WeeklyPool =
        {
            new ChallengeTemplate { id = "w_money", titleFormat = "Earn {0}", stat = StatId.MoneyEarned, format = Cash,
                target = g => Nice(Math.Max(1e7, Income(g) * 14400)), reward = GoalReward.Money(1800) },
            new ChallengeTemplate { id = "w_beats", titleFormat = "Make {0} beats", stat = StatId.BeatsMade, format = Num,
                target = g => Nice(1500 + 600 * g.Studios.Level), reward = GoalReward.Boost(BoostType.Money, 900) },
            new ChallengeTemplate { id = "w_streams", titleFormat = "Reach {0} new streams", stat = StatId.Streams, format = Num,
                target = g => Nice(Math.Max(1e5, StreamRate(g) * 14400)), reward = GoalReward.Money(1800) },
            new ChallengeTemplate { id = "w_concerts", titleFormat = "Complete {0} concerts", stat = StatId.Concerts, format = Num, available = HasArtist,
                target = g => 5, reward = GoalReward.Fans(15) },
            new ChallengeTemplate { id = "w_sign", titleFormat = "Sign {0} artists", stat = StatId.ArtistsSignedLifetime, format = Num, available = CanSignMore,
                target = g => Math.Min(2, g.Artists.Artists.Count - g.Artists.SignedCount), reward = GoalReward.Money(1800) },
            new ChallengeTemplate { id = "w_album", titleFormat = "Release {0} album", stat = StatId.AlbumsReleased, format = Num, available = g => g.Albums.Next != null,
                target = g => 1, reward = GoalReward.Fame(1) },
            new ChallengeTemplate { id = "w_viral", titleFormat = "Make {0} VIRAL beats", stat = StatId.ViralBeats, format = Num,
                target = g => 3, reward = GoalReward.Boost(BoostType.Viral, 600) },
        };

        public static ChallengeTemplate Find(string id) =>
            Array.Find(DailyPool, t => t.id == id) ?? Array.Find(WeeklyPool, t => t.id == id);

        // ------------------------------------------------------------------ periods

        /// <summary>Local calendar day number.</summary>
        static long DayKey(DateTime utc) => (long)(utc.ToLocalTime().Date - new DateTime(2000, 1, 1)).TotalDays;

        /// <summary>Local week number (weeks start on Monday).</summary>
        static long WeekKey(DateTime utc)
        {
            var d = utc.ToLocalTime().Date;
            int offset = ((int)d.DayOfWeek + 6) % 7; // Monday = 0
            return (long)(d.AddDays(-offset) - new DateTime(2000, 1, 3)).TotalDays / 7; // 2000-01-03 was a Monday
        }

        public TimeSpan TimeToDailyReset => time.UtcNow.ToLocalTime().Date.AddDays(1) - time.UtcNow.ToLocalTime();
        public TimeSpan TimeToWeeklyReset
        {
            get
            {
                var now = time.UtcNow.ToLocalTime();
                int offset = ((int)now.DayOfWeek + 6) % 7;
                return now.Date.AddDays(7 - offset) - now;
            }
        }

        public IReadOnlyList<ChallengeState> Daily => daily.items;
        public IReadOnlyList<ChallengeState> Weekly => weekly.items;

        /// <summary>Creates new sets when a new day/week started. Clock moved backwards → keep the current set.</summary>
        public void Refresh()
        {
            var now = time.UtcNow;
            if (lastSeenTicks > 0 && now.Ticks < lastSeenTicks - TimeSpan.TicksPerHour)
                return; // device clock went back: ignore (no free resets)
            lastSeenTicks = Math.Max(lastSeenTicks, now.Ticks);

            long dk = DayKey(now), wk = WeekKey(now);
            bool changed = false;
            if (daily.periodKey != dk && dk > daily.periodKey) { daily = Generate(DailyPool, DailyCount, dk, 17); changed = true; }
            if (weekly.periodKey != wk && wk > weekly.periodKey) { weekly = Generate(WeeklyPool, WeeklyCount, wk, 91); changed = true; }
            if (changed) Changed?.Invoke();
        }

        ChallengeSetSave Generate(ChallengeTemplate[] pool, int count, long key, int salt)
        {
            var set = new ChallengeSetSave { periodKey = key };
            var rnd = new Random((int)(key * 7919 + salt));
            var candidates = new List<ChallengeTemplate>();
            foreach (var t in pool) if (t.available == null || t.available(game)) candidates.Add(t);
            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                int k = rnd.Next(candidates.Count);
                var t = candidates[k];
                candidates.RemoveAt(k);
                set.items.Add(new ChallengeState { templateId = t.id, baseline = stats.Value(t.stat), target = Math.Max(1, t.target(game)) });
            }
            return set;
        }

        /// <summary>"Make 120 beats".</summary>
        public static string Title(ChallengeState c)
        {
            var t = Find(c.templateId);
            if (t == null) return "";
            string value = t.format != null ? t.format(c.target) : NumberFormatter.Format(c.target);
            // plural-aware text in the player's language: challenge.{id}.one/.few/.many/.other, {value} = formatted target
            return Loc.P("challenge." + t.id, c.target, "value", value);
        }

        public double Progress(ChallengeState c)
        {
            var t = Find(c.templateId);
            return t == null ? 0 : Math.Max(0, Math.Min(c.target, stats.Value(t.stat) - c.baseline));
        }

        public float Progress01(ChallengeState c) => (float)(Progress(c) / Math.Max(1, c.target));

        public int ClaimableCount
        {
            get
            {
                int n = 0;
                foreach (var c in daily.items) if (c.completed && !c.claimed) n++;
                foreach (var c in weekly.items) if (c.completed && !c.claimed) n++;
                return n;
            }
        }

        public void Tick(float dt)
        {
            timer += dt;
            if (timer < 1f) return;
            timer = 0;
            Refresh();
            CheckSet(daily, false);
            CheckSet(weekly, true);
        }

        void CheckSet(ChallengeSetSave set, bool isWeekly)
        {
            foreach (var c in set.items)
            {
                if (c.completed || Progress(c) < c.target) continue;
                c.completed = true;
                Completed?.Invoke(Title(c), isWeekly);
                Changed?.Invoke();
            }
        }

        public string Claim(ChallengeState c)
        {
            if (c == null || !c.completed || c.claimed) return null;
            var t = Find(c.templateId);
            if (t == null) return null;
            c.claimed = true;
            string text = t.reward.Grant(game);
            Changed?.Invoke();
            return text;
        }

        // ------------------------------------------------------------------ save

        public void Save(GoalsSave s)
        {
            s.daily = daily;
            s.weekly = weekly;
            s.lastSeenUtcTicks = lastSeenTicks;
        }

        public void Load(GoalsSave s)
        {
            daily = s?.daily ?? new ChallengeSetSave();
            weekly = s?.weekly ?? new ChallengeSetSave();
            if (daily.items == null) daily.items = new List<ChallengeState>();
            if (weekly.items == null) weekly.items = new List<ChallengeState>();
            daily.items.RemoveAll(c => Find(c.templateId) == null);
            weekly.items.RemoveAll(c => Find(c.templateId) == null);
            lastSeenTicks = s?.lastSeenUtcTicks ?? 0;
        }

        /// <summary>Debug: force new sets (pretend a new day and week started).</summary>
        public void DebugForceReset()
        {
            daily.periodKey = -1;
            weekly.periodKey = -1;
            lastSeenTicks = 0;
            Refresh();
        }
    }

    /// <summary>All saved data of missions + challenges + the stats counters.</summary>
    [Serializable]
    public class GoalsSave
    {
        public List<StatEntry> stats = new List<StatEntry>();
        public List<string> missionsCompleted = new List<string>();
        public List<string> missionsClaimed = new List<string>();
        public ChallengeSetSave daily = new ChallengeSetSave();
        public ChallengeSetSave weekly = new ChallengeSetSave();
        public long lastSeenUtcTicks;
    }
}
