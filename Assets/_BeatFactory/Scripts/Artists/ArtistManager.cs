using System;
using System.Collections.Generic;

namespace BeatFactory
{
    /// <summary>
    /// Signing artists. Any artist can be signed once the studio is good enough;
    /// every signed artist multiplies all income (bonuses stack).
    /// </summary>
    public class ArtistManager
    {
        readonly List<ArtistData> artists = new List<ArtistData>();
        readonly HashSet<string> signed = new HashSet<string>();
        readonly EconomyManager economy;
        readonly Func<int> getStudioLevel;

        /// <summary>Fired when the player signs an artist (use for celebrations).</summary>
        public event Action<ArtistData> ArtistSigned;

        /// <summary>Fired whenever the signed set changes, including loading a save.</summary>
        public event Action Changed;

        public ArtistManager(IEnumerable<ArtistData> definitions, EconomyManager economy, Func<int> getStudioLevel)
        {
            this.economy = economy;
            this.getStudioLevel = getStudioLevel;
            foreach (var a in definitions)
                if (a != null) artists.Add(a);
        }

        public IReadOnlyList<ArtistData> Artists => artists;
        public int SignedCount => signed.Count;

        public bool IsSigned(ArtistData a) => signed.Contains(a.id);
        public bool MeetsStudioRequirement(ArtistData a) => getStudioLevel() >= a.requiredStudioLevel;
        public bool CanSign(ArtistData a) => !IsSigned(a) && MeetsStudioRequirement(a) && economy.CanAfford(a.cost);

        public bool CanSignAny()
        {
            foreach (var a in artists)
                if (CanSign(a)) return true;
            return false;
        }

        /// <summary>All signed artists' multipliers multiplied together.</summary>
        public double Multiplier
        {
            get
            {
                double m = 1.0;
                foreach (var a in artists)
                    if (signed.Contains(a.id)) m *= a.incomeMultiplier;
                return m;
            }
        }

        public bool TrySign(ArtistData a)
        {
            if (IsSigned(a) || !MeetsStudioRequirement(a)) return false;
            if (!economy.TrySpend(a.cost)) return false;

            signed.Add(a.id);
            Changed?.Invoke();
            ArtistSigned?.Invoke(a);
            return true;
        }

        public void ForceSign(ArtistData a)
        {
            if (!signed.Add(a.id)) return;
            Changed?.Invoke();
            ArtistSigned?.Invoke(a);
        }

        public List<string> GetSignedIds() => new List<string>(signed);

        /// <summary>Restores progress from a save file / resets on prestige (pass null or empty to clear).</summary>
        public void LoadState(IEnumerable<string> signedIds)
        {
            signed.Clear();
            if (signedIds != null)
                foreach (var id in signedIds)
                    if (artists.Exists(a => a.id == id)) signed.Add(id);
            Changed?.Invoke();
        }
    }
}
