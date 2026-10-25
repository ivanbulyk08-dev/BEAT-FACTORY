using System;
using System.Collections.Generic;

namespace BeatFactory
{
    /// <summary>A detailed artist's chosen outfit as saved: item ids only.</summary>
    [Serializable]
    public class ArtistOutfitSave
    {
        public string artistId;
        public List<string> items = new List<string>();
    }

    /// <summary>
    /// The outfits of detailed artists (DRIP STUDIO). The save keeps item ids per artist; anything missing or unknown
    /// (old saves, removed items) falls back to the rig's default outfit, so loading never fails.
    /// Kept across prestige (style is cosmetic).
    /// </summary>
    public class ArtistWardrobe
    {
        readonly Dictionary<string, Dictionary<WardrobeSlot, string>> outfits = new Dictionary<string, Dictionary<WardrobeSlot, string>>();
        static readonly WardrobeSlot[] Slots = (WardrobeSlot[])Enum.GetValues(typeof(WardrobeSlot));

        readonly HashSet<string> owned = new HashSet<string>();

        /// <summary>STORE purchases (item ids). Kept across prestige, like the outfits.</summary>
        public bool Owns(string itemId) => owned.Contains(itemId);

        /// <summary>Marks a STORE item as bought. Returns false if it was owned already.</summary>
        public bool Grant(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || !owned.Add(itemId)) return false;
            Changed?.Invoke("");
            return true;
        }

        public List<string> GetOwned() => new List<string>(owned);

        /// <summary>Outfit key of the main character in the studio rooms (saved with the artists' outfits).</summary>
        public const string PlayerKey = "__player";

        /// <summary>STORE item id the room character wears in <paramref name="slot"/> (null = his own clothes).</summary>
        public string PlayerWorn(WardrobeSlot slot) =>
            outfits.TryGetValue(PlayerKey, out var o) && o.TryGetValue(slot, out var id) && !string.IsNullOrEmpty(id) ? id : null;

        /// <summary>Puts a bought item on the room character (null = back to his own clothes).</summary>
        public bool WearPlayer(WardrobeSlot slot, string itemId)
        {
            if (itemId != null && !owned.Contains(itemId)) return false;
            if (!outfits.TryGetValue(PlayerKey, out var o)) outfits[PlayerKey] = o = new Dictionary<WardrobeSlot, string>();
            o[slot] = itemId ?? "";
            Changed?.Invoke(PlayerKey);
            return true;
        }

        public void LoadOwned(List<string> ids)
        {
            owned.Clear();
            if (ids != null) foreach (var id in ids) if (!string.IsNullOrEmpty(id)) owned.Add(id);
        }

        /// <summary>Fired when an artist's outfit changes (argument: artist id).</summary>
        public event Action<string> Changed;

        /// <summary>The item worn in <paramref name="slot"/> (null = nothing in an optional slot).</summary>
        public WardrobeItem Worn(ArtistRig rig, WardrobeSlot slot)
        {
            if (rig == null) return null;
            if (outfits.TryGetValue(rig.artistId, out var o) && o.TryGetValue(slot, out var id))
            {
                if (string.IsNullOrEmpty(id)) return ArtistRig.OptionalSlot(slot) ? null : rig.Default(slot);
                var item = rig.Find(id);
                if (item != null && item.slot == slot) return item;
            }
            return rig.Default(slot);
        }

        /// <summary>Everything the artist wears right now, one entry per slot (null for empty optional slots).</summary>
        public Dictionary<WardrobeSlot, WardrobeItem> Resolve(ArtistRig rig)
        {
            var result = new Dictionary<WardrobeSlot, WardrobeItem>();
            foreach (var s in Slots) result[s] = Worn(rig, s);
            return result;
        }

        /// <summary>Wears an item (null = take off an optional slot). Returns false for items of another artist or slot.</summary>
        public bool Wear(ArtistRig rig, WardrobeSlot slot, WardrobeItem item)
        {
            if (rig == null) return false;
            if (item != null && (item.slot != slot || rig.Find(item.id) != item)) return false;
            if (item == null && !ArtistRig.OptionalSlot(slot)) return false;
            if (!outfits.TryGetValue(rig.artistId, out var o)) outfits[rig.artistId] = o = new Dictionary<WardrobeSlot, string>();
            o[slot] = item != null ? item.id : "";
            Changed?.Invoke(rig.artistId);
            return true;
        }

        public List<ArtistOutfitSave> GetSaveData()
        {
            var list = new List<ArtistOutfitSave>();
            foreach (var pair in outfits)
            {
                var s = new ArtistOutfitSave { artistId = pair.Key };
                foreach (var slot in pair.Value) s.items.Add(slot.Key + ":" + slot.Value);
                list.Add(s);
            }
            return list;
        }

        public void LoadState(List<ArtistOutfitSave> list)
        {
            outfits.Clear();
            if (list == null) return;
            foreach (var s in list)
            {
                if (s == null || string.IsNullOrEmpty(s.artistId) || s.items == null) continue;
                var o = new Dictionary<WardrobeSlot, string>();
                foreach (var entry in s.items)
                {
                    int i = entry != null ? entry.IndexOf(':') : -1;
                    if (i <= 0) continue;
                    if (Enum.TryParse(entry.Substring(0, i), out WardrobeSlot slot)) o[slot] = entry.Substring(i + 1);
                }
                outfits[s.artistId] = o;
            }
        }
    }
}
