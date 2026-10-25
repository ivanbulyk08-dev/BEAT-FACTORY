using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// The detailed artist on stage: body parts + DRIP STUDIO clothing layers from an <see cref="ArtistRig"/>, on a simple
    /// 2D skeleton (torso, head, upper arm / forearm / hand ×2, legs, shoes). All layers are flat UI images in a fixed draw
    /// order (<see cref="ArtistRig.DrawOrder"/>); every frame the bones are solved (forward kinematics) and each layer is
    /// placed on its bone, so any jacket, pants or hairstyle drawn on the template follows the same motion.
    /// Units = template pixels, origin between the feet.
    /// </summary>
    public class LayeredPerformer
    {
        public struct Pose
        {
            public float bob, torso, head;
            public float upperNear, foreNear, handNear, upperFar, foreFar, handFar;
            public float legNear, legFar;
            public int expression;
            public HandPose nearHand;
        }

        class Layer
        {
            public Image img;
            public RectTransform rt;
            public RigPart part;
            public WardrobeSlot? slot;
            public ArtistRig.Bone bone;
            public Vector2 offset; // pivot joint relative to the bone joint
        }

        readonly ArtistRig rig;
        readonly List<Layer> layers = new List<Layer>();
        readonly Vector2[] bonePos = new Vector2[13];
        readonly float[] boneAngle = new float[13];
        Dictionary<WardrobeSlot, WardrobeItem> outfit;
        int expression = -1;
        HandPose hand = (HandPose)(-1);

        public ArtistRig Rig => rig;

        /// <summary>Near-hand grip point after the last <see cref="Apply"/> (template units, from the feet).</summary>
        public Vector2 NearHandGrip { get; private set; }

        /// <summary>Height of the template character in its own units (for scaling against the old rig).</summary>
        public float CharacterHeight => Mathf.Max(1f, rig.canvasSize.y - rig.Joint(RigJoint.Feet).y);

        LayeredPerformer(ArtistRig rig) { this.rig = rig; }

        public static LayeredPerformer Build(RectTransform back, RectTransform front, ArtistRig rig, Dictionary<WardrobeSlot, WardrobeItem> outfit)
        {
            var lp = new LayeredPerformer(rig);
            var order = ArtistRig.DrawOrder;
            for (int i = 0; i < order.Length; i++)
            {
                var (part, slot) = order[i];
                var parent = i < ArtistRig.FrontGroupStart ? back : front;
                var img = UIKit.Image(part + (slot.HasValue ? "_" + slot.Value : ""), parent, Color.white);
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                var bone = ArtistRig.BoneOf(part);
                lp.layers.Add(new Layer
                {
                    img = img, rt = rt, part = part, slot = slot, bone = bone,
                    offset = rig.Joint(ArtistRig.PivotJoint(part)) - rig.Joint(ArtistRig.BoneJoint(bone)),
                });
            }
            lp.Dress(outfit);
            return lp;
        }

        /// <summary>Puts on an outfit (one item per slot, null = nothing in that optional slot).</summary>
        public void Dress(Dictionary<WardrobeSlot, WardrobeItem> worn)
        {
            outfit = worn ?? new Dictionary<WardrobeSlot, WardrobeItem>();
            expression = -1;
            hand = (HandPose)(-1);
            foreach (var l in layers)
            {
                if (l.slot.HasValue)
                {
                    outfit.TryGetValue(l.slot.Value, out var item);
                    SetSprite(l, item != null ? item.Get(l.part) : null);
                }
                else if (l.part != RigPart.Head && l.part != RigPart.HandNear) SetSprite(l, rig.Body(l.part));
            }
        }

        static void SetSprite(Layer l, Sprite s)
        {
            if (l.img.sprite == s && l.img.enabled == (s != null)) return;
            l.img.sprite = s;
            l.img.enabled = s != null;
            if (s == null) return;
            var r = s.rect;
            l.rt.sizeDelta = r.size;
            l.rt.pivot = new Vector2(s.pivot.x / r.width, s.pivot.y / r.height);
        }

        Vector2 J(RigJoint j) => rig.Joint(j) - rig.Joint(RigJoint.Feet);

        static Vector2 Rot(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        void Solve(ArtistRig.Bone b, float local, Vector2 extra)
        {
            int i = (int)b;
            if (b == ArtistRig.Bone.Root)
            {
                bonePos[i] = extra;
                boneAngle[i] = local;
                return;
            }
            var p = ArtistRig.Parent(b);
            int pi = (int)p;
            var rel = J(ArtistRig.BoneJoint(b)) - J(ArtistRig.BoneJoint(p));
            bonePos[i] = bonePos[pi] + Rot(rel, boneAngle[pi]) + extra;
            boneAngle[i] = boneAngle[pi] + local;
        }

        /// <summary>Solves the skeleton for this pose and places every layer.</summary>
        public void Apply(Pose p)
        {
            Solve(ArtistRig.Bone.Root, 0f, Vector2.zero);
            Solve(ArtistRig.Bone.Torso, p.torso, new Vector2(0, p.bob));
            Solve(ArtistRig.Bone.Head, p.head, Vector2.zero);
            Solve(ArtistRig.Bone.UpperArmFar, p.upperFar, Vector2.zero);
            Solve(ArtistRig.Bone.ForearmFar, p.foreFar, Vector2.zero);
            Solve(ArtistRig.Bone.HandFar, p.handFar, Vector2.zero);
            Solve(ArtistRig.Bone.UpperArmNear, p.upperNear, Vector2.zero);
            Solve(ArtistRig.Bone.ForearmNear, p.foreNear, Vector2.zero);
            Solve(ArtistRig.Bone.HandNear, p.handNear, Vector2.zero);
            Solve(ArtistRig.Bone.LegNear, p.legNear, Vector2.zero);
            Solve(ArtistRig.Bone.ShoeNear, -p.legNear, Vector2.zero); // shoes stay flat on the floor
            Solve(ArtistRig.Bone.LegFar, p.legFar, Vector2.zero);
            Solve(ArtistRig.Bone.ShoeFar, -p.legFar, Vector2.zero);

            bool newExpr = p.expression != expression, newHand = p.nearHand != hand;
            expression = p.expression;
            hand = p.nearHand;
            foreach (var l in layers)
            {
                if (l.slot == null && l.part == RigPart.Head && newExpr) SetSprite(l, rig.Head(expression));
                if (l.slot == null && l.part == RigPart.HandNear && newHand) SetSprite(l, rig.NearHand(hand));
                if (!l.img.enabled) continue;
                int b = (int)l.bone;
                l.rt.anchoredPosition = bonePos[b] + Rot(l.offset, boneAngle[b]);
                l.rt.localEulerAngles = new Vector3(0, 0, boneAngle[b]);
            }
            // the mic sits in the palm, a bit below the wrist
            int h = (int)ArtistRig.Bone.HandNear;
            NearHandGrip = bonePos[h] + Rot(new Vector2(0, -0.06f * CharacterHeight), boneAngle[h]);
        }
    }
}
