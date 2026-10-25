using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>DRIP STUDIO slots (what can be swapped on a detailed artist).</summary>
    public enum WardrobeSlot { Hair, Top, Jacket, Pants, Shoes, Chain, Glasses, Rings }

    /// <summary>
    /// Body parts of the detailed artist rig. Every sprite (body or clothing) belongs to one part: its pivot is that part's
    /// joint, drawn on the shared character template, so clothes from any item line up with the body.
    /// </summary>
    public enum RigPart
    {
        HairBack, Head, Face, HairFront,
        Neck, Torso, Chest, Collar, Waist,
        UpperArmNear, ForearmNear, HandNear,
        UpperArmFar, ForearmFar, HandFar,
        LegNear, ShoeNear, LegFar, ShoeFar,
    }

    /// <summary>Joints of the template (canvas pixels, bottom-left origin). Each part's sprite pivot sits on one of them.</summary>
    public enum RigJoint { Feet, Hips, Neck, ShoulderNear, ElbowNear, WristNear, ShoulderFar, ElbowFar, WristFar, HipNear, AnkleNear, HipFar, AnkleFar }

    [Serializable]
    public class RigPartSprite
    {
        public RigPart part;
        public Sprite sprite;
    }

    [Serializable]
    public class RigJointPosition
    {
        public RigJoint joint;
        public Vector2 position;
    }

    /// <summary>Near-hand drawings for the different performance poses.</summary>
    public enum HandPose { Mic, Open, Point, Fist }

    /// <summary>
    /// One detailed artist built from separate transparent PNG parts (see ARTIST_ART_PLAN.md): the body under the clothes,
    /// expression heads, hand poses, the joint positions of the shared template and the artist's wardrobe.
    /// Created/updated by Tools > Beat Factory > Art > Import Artist Rig from Assets/_BeatFactory/Art/Artists/&lt;id&gt;/.
    /// Artists without a rig keep the generated flat-style performer.
    /// </summary>
    [CreateAssetMenu(fileName = "ArtistRig", menuName = "Beat Factory/Artist Rig")]
    public class ArtistRig : ScriptableObject
    {
        public string artistId;
        [Tooltip("True while the parts are generated placeholders (not production art).")]
        public bool placeholderArt;
        [Tooltip("Template canvas the parts were drawn on (pixels).")]
        public Vector2 canvasSize = new Vector2(800, 1050);
        public RigJointPosition[] joints = new RigJointPosition[0];

        [Header("Body (under the clothes)")]
        public RigPartSprite[] body = new RigPartSprite[0];
        [Tooltip("Heads in ArtistVisualIdentity expression order: idle, excited (rap / hype), smile, proud, blink.")]
        public Sprite[] heads = new Sprite[0];
        [Tooltip("Near hand per HandPose: mic, open, point, fist.")]
        public Sprite[] nearHands = new Sprite[0];

        [Header("Wardrobe")]
        [Tooltip("Everything this artist can wear (DRIP STUDIO).")]
        public WardrobeItem[] wardrobe = new WardrobeItem[0];
        [Tooltip("Default outfit (one item per slot that is worn by default).")]
        public WardrobeItem[] defaultOutfit = new WardrobeItem[0];

        public Vector2 Joint(RigJoint j)
        {
            foreach (var p in joints) if (p.joint == j) return p.position;
            return Vector2.zero;
        }

        public Sprite Body(RigPart part)
        {
            foreach (var b in body) if (b.part == part) return b.sprite;
            return null;
        }

        public Sprite Head(int expression) =>
            heads == null || heads.Length == 0 ? null : heads[Mathf.Clamp(expression, 0, heads.Length - 1)];

        public Sprite NearHand(HandPose pose)
        {
            if (nearHands == null || nearHands.Length == 0) return Body(RigPart.HandNear);
            var s = nearHands[Mathf.Clamp((int)pose, 0, nearHands.Length - 1)];
            return s != null ? s : nearHands[0];
        }

        public WardrobeItem Find(string itemId)
        {
            foreach (var w in wardrobe) if (w != null && w.id == itemId) return w;
            return null;
        }

        public WardrobeItem Default(WardrobeSlot slot)
        {
            foreach (var w in defaultOutfit) if (w != null && w.slot == slot) return w;
            return null;
        }

        public IEnumerable<WardrobeItem> ItemsFor(WardrobeSlot slot)
        {
            foreach (var w in wardrobe) if (w != null && w.slot == slot) yield return w;
        }

        // ------------------------------------------------------------------ skeleton (shared by every artist rig)

        /// <summary>Which joint each part's sprite pivot sits on.</summary>
        public static RigJoint PivotJoint(RigPart p)
        {
            switch (p)
            {
                case RigPart.HairBack: case RigPart.Head: case RigPart.Face: case RigPart.HairFront:
                case RigPart.Neck: case RigPart.Chest: case RigPart.Collar: return RigJoint.Neck;
                case RigPart.Torso: case RigPart.Waist: return RigJoint.Hips;
                case RigPart.UpperArmNear: return RigJoint.ShoulderNear;
                case RigPart.ForearmNear: return RigJoint.ElbowNear;
                case RigPart.HandNear: return RigJoint.WristNear;
                case RigPart.UpperArmFar: return RigJoint.ShoulderFar;
                case RigPart.ForearmFar: return RigJoint.ElbowFar;
                case RigPart.HandFar: return RigJoint.WristFar;
                case RigPart.LegNear: return RigJoint.HipNear;
                case RigPart.ShoeNear: return RigJoint.AnkleNear;
                case RigPart.LegFar: return RigJoint.HipFar;
                default: return RigJoint.AnkleFar;
            }
        }

        /// <summary>Bones that move a part (the head turns the hair and glasses; the torso carries neck, chain and collar).</summary>
        public enum Bone { Root, Torso, Head, UpperArmNear, ForearmNear, HandNear, UpperArmFar, ForearmFar, HandFar, LegNear, ShoeNear, LegFar, ShoeFar }

        public static Bone BoneOf(RigPart p)
        {
            switch (p)
            {
                case RigPart.HairBack: case RigPart.Head: case RigPart.Face: case RigPart.HairFront: return Bone.Head;
                case RigPart.Neck: case RigPart.Torso: case RigPart.Chest: case RigPart.Collar: case RigPart.Waist: return Bone.Torso;
                case RigPart.UpperArmNear: return Bone.UpperArmNear;
                case RigPart.ForearmNear: return Bone.ForearmNear;
                case RigPart.HandNear: return Bone.HandNear;
                case RigPart.UpperArmFar: return Bone.UpperArmFar;
                case RigPart.ForearmFar: return Bone.ForearmFar;
                case RigPart.HandFar: return Bone.HandFar;
                case RigPart.LegNear: return Bone.LegNear;
                case RigPart.ShoeNear: return Bone.ShoeNear;
                case RigPart.LegFar: return Bone.LegFar;
                default: return Bone.ShoeFar;
            }
        }

        /// <summary>Joint at the origin of each bone, and the bone's parent.</summary>
        public static RigJoint BoneJoint(Bone b)
        {
            switch (b)
            {
                case Bone.Root: return RigJoint.Feet;
                case Bone.Torso: return RigJoint.Hips;
                case Bone.Head: return RigJoint.Neck;
                case Bone.UpperArmNear: return RigJoint.ShoulderNear;
                case Bone.ForearmNear: return RigJoint.ElbowNear;
                case Bone.HandNear: return RigJoint.WristNear;
                case Bone.UpperArmFar: return RigJoint.ShoulderFar;
                case Bone.ForearmFar: return RigJoint.ElbowFar;
                case Bone.HandFar: return RigJoint.WristFar;
                case Bone.LegNear: return RigJoint.HipNear;
                case Bone.ShoeNear: return RigJoint.AnkleNear;
                case Bone.LegFar: return RigJoint.HipFar;
                default: return RigJoint.AnkleFar;
            }
        }

        public static Bone Parent(Bone b)
        {
            switch (b)
            {
                case Bone.Torso: case Bone.LegNear: case Bone.LegFar: return Bone.Root;
                case Bone.Head: case Bone.UpperArmNear: case Bone.UpperArmFar: return Bone.Torso;
                case Bone.ForearmNear: return Bone.UpperArmNear;
                case Bone.HandNear: return Bone.ForearmNear;
                case Bone.ForearmFar: return Bone.UpperArmFar;
                case Bone.HandFar: return Bone.ForearmFar;
                case Bone.ShoeNear: return Bone.LegNear;
                case Bone.ShoeFar: return Bone.LegFar;
                default: return Bone.Root;
            }
        }

        /// <summary>
        /// Draw order, back → front: (part, slot or null for the body). The microphone is drawn between the back group and
        /// the near arm (index <see cref="FrontGroupStart"/>), so the fingers wrap around it.
        /// </summary>
        public static readonly (RigPart part, WardrobeSlot? slot)[] DrawOrder =
        {
            (RigPart.LegFar, null), (RigPart.LegFar, WardrobeSlot.Pants), (RigPart.ShoeFar, WardrobeSlot.Shoes),
            (RigPart.UpperArmFar, null), (RigPart.UpperArmFar, WardrobeSlot.Top), (RigPart.UpperArmFar, WardrobeSlot.Jacket),
            (RigPart.ForearmFar, null), (RigPart.ForearmFar, WardrobeSlot.Jacket), (RigPart.HandFar, null),
            (RigPart.HairBack, WardrobeSlot.Hair),
            (RigPart.LegNear, null), (RigPart.LegNear, WardrobeSlot.Pants), (RigPart.ShoeNear, WardrobeSlot.Shoes),
            (RigPart.Waist, WardrobeSlot.Pants),
            (RigPart.Neck, null),
            (RigPart.Torso, null), (RigPart.Torso, WardrobeSlot.Top), (RigPart.Torso, WardrobeSlot.Jacket),
            (RigPart.Chest, WardrobeSlot.Chain), (RigPart.Collar, WardrobeSlot.Jacket),
            (RigPart.Head, null), (RigPart.Face, WardrobeSlot.Glasses), (RigPart.HairFront, WardrobeSlot.Hair),
            // front group (after the mic)
            (RigPart.UpperArmNear, null), (RigPart.UpperArmNear, WardrobeSlot.Top), (RigPart.UpperArmNear, WardrobeSlot.Jacket),
            (RigPart.ForearmNear, null), (RigPart.ForearmNear, WardrobeSlot.Jacket),
            (RigPart.HandNear, null), (RigPart.HandNear, WardrobeSlot.Rings),
        };

        public const int FrontGroupStart = 23;

        /// <summary>Parts an item of this slot must provide (others are optional).</summary>
        public static RigPart[] RequiredParts(WardrobeSlot slot)
        {
            switch (slot)
            {
                case WardrobeSlot.Hair: return new[] { RigPart.HairBack, RigPart.HairFront };
                case WardrobeSlot.Top: return new[] { RigPart.Torso };
                case WardrobeSlot.Jacket: return new[] { RigPart.Torso, RigPart.UpperArmNear, RigPart.UpperArmFar, RigPart.ForearmNear, RigPart.ForearmFar };
                case WardrobeSlot.Pants: return new[] { RigPart.Waist, RigPart.LegNear, RigPart.LegFar };
                case WardrobeSlot.Shoes: return new[] { RigPart.ShoeNear, RigPart.ShoeFar };
                case WardrobeSlot.Chain: return new[] { RigPart.Chest };
                case WardrobeSlot.Glasses: return new[] { RigPart.Face };
                default: return new[] { RigPart.HandNear };
            }
        }

        /// <summary>Body parts the rig must have.</summary>
        public static readonly RigPart[] RequiredBody =
        {
            RigPart.Neck, RigPart.Torso, RigPart.UpperArmNear, RigPart.ForearmNear, RigPart.UpperArmFar, RigPart.ForearmFar,
            RigPart.HandFar, RigPart.LegNear, RigPart.LegFar,
        };

        public static bool OptionalSlot(WardrobeSlot s) => s == WardrobeSlot.Jacket || s == WardrobeSlot.Chain || s == WardrobeSlot.Glasses || s == WardrobeSlot.Rings;
    }
}
