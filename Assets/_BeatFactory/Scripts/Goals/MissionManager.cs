using System;
using System.Collections.Generic;

namespace BeatFactory
{
    /// <summary>
    /// Career goals. Progress is read from <see cref="StatsTracker"/> (so it follows normal gameplay automatically);
    /// a mission completes once its stat reaches the target and pays its reward when the player taps CLAIM.
    /// Completed/claimed missions stay in the career history (saved; kept after prestige).
    /// </summary>
    public class MissionManager
    {
        readonly GameManager game;
        readonly StatsTracker stats;
        readonly HashSet<string> completed = new HashSet<string>();
        readonly HashSet<string> claimed = new HashSet<string>();
        float timer;

        /// <summary>A mission's target was reached (notification).</summary>
        public event Action<MissionDefinition> Completed;
        public event Action<MissionDefinition, string> Claimed;
        public event Action Changed;

        public MissionManager(GameManager game, StatsTracker stats)
        {
            this.game = game;
            this.stats = stats;
        }

        public IReadOnlyList<MissionDefinition> All => MissionCatalog.All;
        public bool IsCompleted(MissionDefinition m) => completed.Contains(m.id);
        public bool IsClaimed(MissionDefinition m) => claimed.Contains(m.id);
        public double Progress(MissionDefinition m) => Math.Min(m.target, stats.Value(m.stat));
        public float Progress01(MissionDefinition m) => m.target <= 0 ? 1f : (float)Math.Min(1.0, stats.Value(m.stat) / m.target);

        public int ClaimableCount
        {
            get
            {
                int n = 0;
                foreach (var m in MissionCatalog.All) if (completed.Contains(m.id) && !claimed.Contains(m.id)) n++;
                return n;
            }
        }

        /// <summary>Checks targets (cheap; called every second and right after loading without notifications).</summary>
        public void Check(bool notify = true)
        {
            bool any = false;
            foreach (var m in MissionCatalog.All)
            {
                if (completed.Contains(m.id)) continue;
                if (stats.Value(m.stat) < m.target) continue;
                completed.Add(m.id);
                any = true;
                if (notify) Completed?.Invoke(m);
            }
            if (any) Changed?.Invoke();
        }

        public void Tick(float dt)
        {
            timer += dt;
            if (timer < 1f) return;
            timer = 0;
            Check();
        }

        public bool Claim(MissionDefinition m)
        {
            if (!completed.Contains(m.id) || claimed.Contains(m.id)) return false;
            claimed.Add(m.id);
            string text = m.reward.Grant(game);
            Claimed?.Invoke(m, text);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Visible list for a category: claimable first, then the next goals in order (max <paramref name="upcoming"/>).</summary>
        public List<MissionDefinition> Visible(MissionCategory c, int upcoming = 2)
        {
            var list = new List<MissionDefinition>();
            foreach (var m in MissionCatalog.All) if (m.category == c && completed.Contains(m.id) && !claimed.Contains(m.id)) list.Add(m);
            int n = 0;
            foreach (var m in MissionCatalog.All)
            {
                if (m.category != c || completed.Contains(m.id)) continue;
                if (n++ >= upcoming) break;
                list.Add(m);
            }
            return list;
        }

        public List<MissionDefinition> History()
        {
            var list = new List<MissionDefinition>();
            foreach (var m in MissionCatalog.All) if (claimed.Contains(m.id)) list.Add(m);
            return list;
        }

        // ------------------------------------------------------------------ save

        public void Save(GoalsSave s)
        {
            s.missionsCompleted = new List<string>(completed);
            s.missionsClaimed = new List<string>(claimed);
        }

        public void Load(GoalsSave s)
        {
            completed.Clear();
            claimed.Clear();
            if (s?.missionsCompleted != null) foreach (var id in s.missionsCompleted) completed.Add(id);
            if (s?.missionsClaimed != null) foreach (var id in s.missionsClaimed) { claimed.Add(id); completed.Add(id); }
        }
    }
}
