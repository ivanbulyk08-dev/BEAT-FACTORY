using UnityEngine;

namespace BeatFactory
{
    /// <summary>What the STORE sells: clothes for the main character in the studio rooms (bought with money).</summary>
    [CreateAssetMenu(fileName = "StoreCatalog", menuName = "Beat Factory/Store Catalog")]
    public class StoreCatalog : ScriptableObject
    {
        public CharacterOutfitItem[] items = new CharacterOutfitItem[0];

        public CharacterOutfitItem Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var i in items) if (i != null && i.id == id) return i;
            return null;
        }
    }
}
