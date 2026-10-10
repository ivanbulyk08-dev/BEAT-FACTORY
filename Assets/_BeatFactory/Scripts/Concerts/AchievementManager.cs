using System;
using System.Collections.Generic;

namespace BeatFactory
{
    public class AchievementDef
    {
        public string id;
        public string title;
        public string description;

        public string Title => Loc.Or("achievement." + id + ".title", title);
        public string Description => Loc.Or("achievement." + id + ".desc", description);
    }

    /// <summary>Simple achievement list (saved, kept after prestige). Systems call Unlock(id) when a goal is reached.</summary>
    public class AchievementManager
    {
        public static readonly AchievementDef[] All =
        {
            new AchievementDef { id = "first_show", title = "FIRST SHOW", description = "Perform your first concert." },
            new AchievementDef { id = "sold_out", title = "SOLD OUT", description = "Sell out a venue." },
            new AchievementDef { id = "arena_ready", title = "ARENA READY", description = "Perform your first arena show." },
            new AchievementDef { id = "stadium", title = "STADIUM KING", description = "Perform your first stadium show." },
            new AchievementDef { id = "superstar", title = "SUPERSTAR", description = "Reach 90% popularity with an artist." },
            new AchievementDef { id = "world_tour", title = "WORLD TOUR", description = "Complete a world tour." },
            new AchievementDef { id = "music_empire", title = "MUSIC EMPIRE", description = "Generate $1B from concerts and tours." },
        };

        readonly HashSet<string> unlocked = new HashSet<string>();

        /// <summary>Fired once when an achievement is unlocked.</summary>
        public event Action<AchievementDef> Unlocked;

        public bool IsUnlocked(string id) => unlocked.Contains(id);
        public int UnlockedCount => unlocked.Count;

        public void Unlock(string id)
        {
            if (!unlocked.Add(id)) return;
            var def = Array.Find(All, a => a.id == id);
            if (def != null) Unlocked?.Invoke(def);
        }

        public List<string> GetIds() => new List<string>(unlocked);

        public void LoadState(IEnumerable<string> ids)
        {
            unlocked.Clear();
            if (ids != null) foreach (var id in ids) unlocked.Add(id);
        }
    }
}
