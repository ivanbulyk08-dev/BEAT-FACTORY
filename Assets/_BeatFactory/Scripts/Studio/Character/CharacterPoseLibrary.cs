using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>Every pose the main character can take. Add new values at the end (they are saved by index in assets).</summary>
    public enum CharacterPose
    {
        StandIdle,
        StandSide,
        StandArmsCrossed,
        SitRelaxed,
        SitFloor,
        SitThinking,
        SitPhone,
        SitChair,
        StandHeadTouch,
        StandBack,
        Crouch,
        LeanBack,
    }

    /// <summary>What the character is doing; the random behaviour picks an activity first, then a pose of it.</summary>
    public enum CharacterActivity { Sitting, Standing, Relaxing, Special }

    /// <summary>Which way the drawing looks (used to mirror it to the direction an anchor wants).</summary>
    public enum CharacterFacing { Front, Left, Right }

    /// <summary>
    /// The main character's pose sprites (one transparent PNG each). A pose without a sprite is simply never chosen, so
    /// missing art never shows up as a stretched or wrong picture.
    /// </summary>
    [CreateAssetMenu(menuName = "Beat Factory/Character Pose Library", fileName = "MainCharacterPoses")]
    public class CharacterPoseLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public CharacterPose pose;
            public CharacterActivity activity;
            [Tooltip("Transparent PNG of the whole figure (feet, head and hands inside the image).")]
            public Sprite sprite;
            [Tooltip("Point of the sprite that touches the floor / seat (0..1, bottom-left = 0,0). The anchor position is placed here.")]
            public Vector2 groundPivot = new Vector2(0.5f, 0.02f);
            [Tooltip("Which way the drawing looks; anchors mirror it when they want the other side.")]
            public CharacterFacing facing = CharacterFacing.Front;
        }

        public List<Entry> poses = new List<Entry>();

        public Entry Get(CharacterPose pose)
        {
            foreach (var e in poses)
                if (e != null && e.pose == pose) return e;
            return null;
        }

        /// <summary>True when the pose has art.</summary>
        public bool Has(CharacterPose pose)
        {
            var e = Get(pose);
            return e != null && e.sprite != null;
        }
    }
}
