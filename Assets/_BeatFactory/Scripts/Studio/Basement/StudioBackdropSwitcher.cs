using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Shows the painted room that matches the player's studio level: the room with the highest level that is not above the
    /// current one (studios without art yet keep showing the latest room that has art). Switches live on studio upgrades.
    /// </summary>
    public class StudioBackdropSwitcher : MonoBehaviour
    {
        [Tooltip("Painted rooms, in order.")]
        [SerializeField] GameObject[] rooms = new GameObject[0];
        [Tooltip("Studio level (0 = Basement) each room belongs to.")]
        [SerializeField] int[] levels = new int[0];

        StudioManager studios;
        GameObject shown;

        public int RoomCount => rooms.Length;
        public GameObject RoomAt(int i) => rooms[i];
        public int LevelOf(int i) => i < levels.Length ? levels[i] : i;

        /// <summary>Raised when another room becomes visible (the main character moves into it).</summary>
        public event System.Action<GameObject> RoomShown;

        void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            studios = gm.Studios;
            studios.CurrentChanged += OnChanged;
            Apply(studios.Level);
        }

        void OnDestroy()
        {
            if (studios != null) studios.CurrentChanged -= OnChanged;
        }

        void OnChanged(StudioData _) => Apply(studios.Level);

        /// <summary>Index of the room for a studio level.</summary>
        public static int RoomFor(int level, int[] roomLevels)
        {
            int best = 0;
            for (int i = 0; i < roomLevels.Length; i++)
                if (roomLevels[i] <= level && roomLevels[i] >= roomLevels[best]) best = i;
            return best;
        }

        public void Apply(int level)
        {
            int show = RoomFor(level, levels);
            for (int i = 0; i < rooms.Length; i++)
                if (rooms[i] != null && rooms[i].activeSelf != (i == show)) rooms[i].SetActive(i == show);
            var room = show < rooms.Length ? rooms[show] : null;
            if (room == shown) return;
            shown = room;
            RoomShown?.Invoke(room);
        }
    }
}
