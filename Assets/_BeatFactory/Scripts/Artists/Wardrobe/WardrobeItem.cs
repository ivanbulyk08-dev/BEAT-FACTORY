using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// One DRIP STUDIO item (a jacket, pants, a hairstyle …) for a detailed artist: one sprite per body part it covers,
    /// all drawn on the artist's template so they line up with the body. Ids are what the save stores.
    /// </summary>
    [CreateAssetMenu(fileName = "WardrobeItem", menuName = "Beat Factory/Wardrobe Item")]
    public class WardrobeItem : ScriptableObject
    {
        [Tooltip("Unique per artist, stored in the save. Do not rename after release.")]
        public string id;
        public WardrobeSlot slot;
        [Tooltip("Localization key of the item name (wardrobe.<id>).")]
        public string nameKey;
        public string englishName;
        public Sprite thumbnail;
        public RigPartSprite[] parts = new RigPartSprite[0];

        [Header("Unlock (used by DRIP STUDIO)")]
        [Min(0)] public int requiredStudioLevel;
        [Min(0)] public double price;

        public string DisplayName => Loc.Or(string.IsNullOrEmpty(nameKey) ? "wardrobe." + id : nameKey, englishName);

        public Sprite Get(RigPart part)
        {
            foreach (var p in parts) if (p.part == part) return p.sprite;
            return null;
        }
    }
}
