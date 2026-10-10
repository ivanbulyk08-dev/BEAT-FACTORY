using System;
using System.Collections.Generic;

namespace BeatFactory
{
    /// <summary>
    /// Albums are the player's career milestones: released in order once the requirements are met.
    /// Each release pays money, adds permanent reputation (income multiplier) and a poster in the studio.
    /// Released albums are career history — they are kept after a prestige.
    /// </summary>
    public class AlbumManager
    {
        public struct Requirement
        {
            public string label;
            public double current, target;
            public bool Met => current >= target;
        }

        readonly List<AlbumData> albums = new List<AlbumData>();
        readonly List<string> released = new List<string>(); // in release order
        readonly GameManager game;

        /// <summary>Fired when an album is released. Arguments: album, money reward.</summary>
        public event Action<AlbumData, double> Released;
        /// <summary>Fired whenever the released set changes (including loading).</summary>
        public event Action Changed;

        public AlbumManager(IEnumerable<AlbumData> definitions, GameManager game)
        {
            this.game = game;
            foreach (var a in definitions)
                if (a != null) albums.Add(a);
        }

        public IReadOnlyList<AlbumData> Albums => albums;
        public int ReleasedCount => released.Count;
        public bool IsReleased(AlbumData a) => released.Contains(a.id);

        /// <summary>Released albums, newest first (for the poster wall).</summary>
        public List<AlbumData> ReleasedNewestFirst()
        {
            var list = new List<AlbumData>();
            for (int i = released.Count - 1; i >= 0; i--)
            {
                var a = albums.Find(x => x.id == released[i]);
                if (a != null) list.Add(a);
            }
            return list;
        }

        /// <summary>The next album to work on (null when all are released).</summary>
        public AlbumData Next => albums.Find(a => !IsReleased(a));

        public double Multiplier
        {
            get
            {
                double m = 1;
                foreach (var a in albums)
                    if (IsReleased(a)) m *= a.reputationMultiplier;
                return m;
            }
        }

        public Requirement[] GetRequirements(AlbumData a)
        {
            int hired = 0;
            foreach (var p in game.Producers.Producers) if (p.IsHired) hired++;
            var list = new List<Requirement>
            {
                new Requirement { label = Loc.T("album.req_beats"), current = game.Beats.TotalBeats, target = a.requiredBeats },
                new Requirement { label = Loc.T("album.req_producers"), current = hired, target = a.requiredProducers },
            };
            if (a.requiredStudio > 0)
                list.Add(new Requirement { label = game.Studios.Get(a.requiredStudio)?.DisplayName ?? Loc.T("album.req_studio"), current = game.Studios.Level, target = a.requiredStudio });
            if (a.requiredArtists > 0)
                list.Add(new Requirement { label = Loc.T("album.req_artists"), current = game.Artists.SignedCount, target = a.requiredArtists });
            return list.ToArray();
        }

        /// <summary>0..1 overall progress (average of the requirements).</summary>
        public float Progress(AlbumData a)
        {
            var reqs = GetRequirements(a);
            float sum = 0;
            foreach (var r in reqs) sum += (float)Math.Min(1.0, r.target <= 0 ? 1 : r.current / r.target);
            return reqs.Length > 0 ? sum / reqs.Length : 1f;
        }

        public bool CanRelease(AlbumData a)
        {
            if (IsReleased(a) || Next != a) return false;
            foreach (var r in GetRequirements(a)) if (!r.Met) return false;
            return true;
        }

        public bool CanReleaseAny() => Next != null && CanRelease(Next);

        public double RewardFor(AlbumData a) =>
            Math.Max(100, game.Producers.IncomePerSecond * a.rewardIncomeSeconds);

        public bool TryRelease(AlbumData a)
        {
            if (!CanRelease(a)) return false;
            double reward = RewardFor(a);
            released.Add(a.id);
            game.Economy.AddMoney(reward);
            Changed?.Invoke();
            Released?.Invoke(a, reward);
            return true;
        }

        public List<string> GetReleasedIds() => new List<string>(released);

        public void LoadState(IEnumerable<string> ids)
        {
            released.Clear();
            if (ids != null)
                foreach (var id in ids)
                    if (albums.Exists(a => a.id == id) && !released.Contains(id)) released.Add(id);
            Changed?.Invoke();
        }
    }
}
