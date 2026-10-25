using System;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// A STORE item for the main character in the studio rooms: a top or pants drawn as one transparent overlay per pose
    /// (same size as the pose sprite, laid over it). Bought with money; the id is what the save stores.
    /// </summary>
    [CreateAssetMenu(fileName = "Outfit", menuName = "Beat Factory/Character Outfit Item")]
    public class CharacterOutfitItem : ScriptableObject
    {
        [Serializable]
        public class PoseSprite
        {
            public CharacterPose pose;
            public Sprite sprite;
        }

        [Tooltip("Unique, stored in the save. Do not rename after release.")]
        public string id;
        [Tooltip("Top or Pants (other slots are not used for the room character).")]
        public WardrobeSlot slot = WardrobeSlot.Top;
        public string englishName;
        [Tooltip("Product picture shown in the STORE.")]
        public Sprite thumbnail;
        [Min(0)] public double price;
        [Tooltip("The pose sprites are the whole character drawn in this item (they replace his body; other clothes are hidden).")]
        public bool fullFigure;
        public PoseSprite[] poses = new PoseSprite[0];

        public string DisplayName => Loc.Or("store.item." + id, englishName);

        public Sprite For(CharacterPose pose)
        {
            foreach (var p in poses) if (p != null && p.pose == pose) return p.sprite;
            return null;
        }
    }
}
