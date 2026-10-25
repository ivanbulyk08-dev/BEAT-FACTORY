using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Weighted random choice of the next activity, pose and anchor. Only combinations that have art and a spot that
    /// supports them are considered, and the current pose + spot is never picked again while there is any alternative.
    /// </summary>
    [Serializable]
    public class CharacterActivitySelector
    {
        [Tooltip("Relative chances (they don't have to add up to 100).")]
        [Min(0)] public float sitting = 40f;
        [Min(0)] public float standing = 30f;
        [Tooltip("Alternative relaxed poses (leaning back, hand in the hair...).")]
        [Min(0)] public float relaxing = 20f;
        [Tooltip("Looking at the phone, looking out of the window...")]
        [Min(0)] public float special = 10f;

        public struct Choice
        {
            public CharacterPoseLibrary.Entry pose;
            public StudioCharacterAnchor anchor;
            public bool IsValid => pose != null && anchor != null;
        }

        readonly List<Choice> options = new List<Choice>();
        readonly List<Choice> ofActivity = new List<Choice>();

        public float Weight(CharacterActivity a)
        {
            switch (a)
            {
                case CharacterActivity.Sitting: return sitting;
                case CharacterActivity.Standing: return standing;
                case CharacterActivity.Relaxing: return relaxing;
                default: return special;
            }
        }

        public Choice Pick(CharacterPoseLibrary library, IList<StudioCharacterAnchor> anchors, Choice current, System.Random rng)
        {
            options.Clear();
            if (library == null || anchors == null) return default;
            foreach (var e in library.poses)
            {
                if (e == null || e.sprite == null) continue;
                foreach (var a in anchors)
                    if (a != null && !a.workstation && a.Supports(e.pose) && !Same(e, a, current))
                        options.Add(new Choice { pose = e, anchor = a });
            }
            if (options.Count == 0) return current; // nothing else is possible: keep what we have

            // 1) activity by weight, among activities that have at least one option
            float total = 0f;
            for (int i = 0; i < 4; i++) if (HasActivity((CharacterActivity)i)) total += Weight((CharacterActivity)i);
            var activity = options[0].pose.activity;
            if (total > 0f)
            {
                double roll = rng.NextDouble() * total;
                for (int i = 0; i < 4; i++)
                {
                    var act = (CharacterActivity)i;
                    if (!HasActivity(act) || Weight(act) <= 0f) continue;
                    roll -= Weight(act);
                    activity = act;
                    if (roll <= 0) break;
                }
            }
            // 2) prefer a different spot than now (the pose alone changing on the same spot is the fallback)
            ofActivity.Clear();
            foreach (var o in options)
                if (o.pose.activity == activity && o.anchor != current.anchor) ofActivity.Add(o);
            if (ofActivity.Count == 0)
                foreach (var o in options)
                    if (o.pose.activity == activity) ofActivity.Add(o);
            return ofActivity[rng.Next(ofActivity.Count)];
        }

        bool HasActivity(CharacterActivity a)
        {
            foreach (var o in options) if (o.pose.activity == a) return true;
            return false;
        }

        static bool Same(CharacterPoseLibrary.Entry e, StudioCharacterAnchor a, Choice current) =>
            current.pose != null && current.pose.pose == e.pose && current.anchor == a;
    }
}
