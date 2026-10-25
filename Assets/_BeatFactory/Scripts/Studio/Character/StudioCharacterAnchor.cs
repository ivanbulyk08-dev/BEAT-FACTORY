using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// A spot in a painted room where the main character can be (child of the room image). The anchor's position is the
    /// point where the feet (standing) or the seat (sitting) touch: drag it in the Scene view to move the spot.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class StudioCharacterAnchor : MonoBehaviour
    {
        public enum Direction { AsDrawn, Left, Right, Random }

        public enum Layer
        {
            [Tooltip("Same layer as the producer at the desk: furniture occluders of the room stay in front.")]
            WithFurniture,
            [Tooltip("In front of everything in the room (spots closer to the camera than any furniture).")]
            Front,
        }

        [Tooltip("Shown in the Inspector and logs only.")]
        public string label = "Spot";
        [Tooltip("Size of the character here: painting pixels per sprite pixel. Further from the camera = smaller.")]
        public float scale = 1f;
        public Direction facing = Direction.AsDrawn;
        [Tooltip("Poses that look right on this spot (e.g. only sitting poses on a bed).")]
        public CharacterPose[] supportedPoses = { CharacterPose.StandIdle };
        [Tooltip("MAKE BEAT spot: he comes here to work (first supported pose with art). Not used by the random behaviour.")]
        public bool workstation;
        [Tooltip("Sorting: WithFurniture keeps the room's foreground pieces (bed, sofa...) in front of the character.")]
        public Layer sortingLayer = Layer.WithFurniture;
        [Tooltip("Optional foreground pieces that must cover the character on this spot (drawn in front of him).")]
        public RectTransform[] occluders = new RectTransform[0];
        [Tooltip("Room light on the character here (auto: StudioCharacterPlacement → right-click → Auto-light anchors).")]
        public CharacterLighting lighting = new CharacterLighting();

        public bool Supports(CharacterPose pose)
        {
            foreach (var p in supportedPoses)
                if (p == pose) return true;
            return false;
        }

        /// <summary>
        /// Normalized position on the room painting (0..1, bottom-left origin), from where the anchor really is — so dragging
        /// it in the Scene view works. The anchor may sit in a stretched holder inside the room.
        /// </summary>
        public Vector2 RoomUV(RectTransform room)
        {
            var rt = (RectTransform)transform;
            var r = room.rect;
            if (r.width > 1f && r.height > 1f)
            {
                Vector2 local = room.InverseTransformPoint(rt.position);
                return new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
            }
            return rt.anchorMin + (rt.anchorMax - rt.anchorMin) * 0.5f;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            var rt = (RectTransform)transform;
            var p = rt.position;
            float s = rt.lossyScale.x * 30f;
            Gizmos.color = sortingLayer == Layer.Front ? new Color(1f, 0.4f, 0.9f) : new Color(0.3f, 1f, 0.6f);
            Gizmos.DrawLine(p + Vector3.left * s, p + Vector3.right * s);
            Gizmos.DrawLine(p, p + Vector3.up * s * 4f * Mathf.Max(0.2f, scale));
        }
#endif
    }
}
