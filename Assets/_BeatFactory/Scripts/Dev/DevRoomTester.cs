#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// DEVELOPER MODE (editor and Development builds only — not compiled into release builds).
    /// A small "DEV" button on the left edge opens a panel to test the studio rooms without touching progress or the save:
    /// view any room, put the main character on any spot in any pose, send him to the desk, speed up his behaviour and hide
    /// the UI to look at the room. "My studio" goes back to the room of the real studio level.
    /// Keyboard (editor): F1 = panel, ←/→ = room, ↑/↓ = spot, Space = next pose, H = hide UI.
    /// </summary>
    public class DevRoomTester : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<DevRoomTester>() != null) return;
            var go = new GameObject("[DEV] Room Tester");
            DontDestroyOnLoad(go);
            go.AddComponent<DevRoomTester>();
        }

        bool open, uiHidden, fast;
        int room = -1, spot, pose;
        Vector2 savedDelay;
        StudioBackdropSwitcher switcher;
        MainCharacterController character;
        CanvasGroup uiGroup;
        bool addedGroup;
        GUIStyle button, label, box;
        readonly List<CharacterPoseLibrary.Entry> poses = new List<CharacterPoseLibrary.Entry>();

        bool Find()
        {
            if (switcher == null) switcher = FindFirstObjectByType<StudioBackdropSwitcher>(FindObjectsInactive.Include);
            if (character == null) character = FindFirstObjectByType<MainCharacterController>(FindObjectsInactive.Include);
            return switcher != null;
        }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (!Application.isEditor || kb == null) return;
            if (kb.f1Key.wasPressedThisFrame) open = !open;
            if (!open || !Find()) return;
            if (kb.rightArrowKey.wasPressedThisFrame) ShowRoom((CurrentRoomIndex() + 1) % switcher.RoomCount);
            if (kb.leftArrowKey.wasPressedThisFrame) ShowRoom((CurrentRoomIndex() + switcher.RoomCount - 1) % switcher.RoomCount);
            if (kb.upArrowKey.wasPressedThisFrame) StepSpot(1);
            if (kb.downArrowKey.wasPressedThisFrame) StepSpot(-1);
            if (kb.spaceKey.wasPressedThisFrame) StepPose(1);
            if (kb.hKey.wasPressedThisFrame) SetUiHidden(!uiHidden);
        }

        int CurrentRoomIndex()
        {
            for (int i = 0; i < switcher.RoomCount; i++)
                if (switcher.RoomAt(i) != null && switcher.RoomAt(i).activeSelf) return i;
            return 0;
        }

        void ShowRoom(int i)
        {
            room = i;
            spot = pose = 0;
            switcher.Apply(switcher.LevelOf(i)); // visual only: the studio level and the save are not changed
        }

        void BackToMyStudio()
        {
            room = -1;
            var gm = GameManager.Instance;
            if (gm != null) switcher.Apply(gm.Studios.Level);
        }

        List<StudioCharacterAnchor> Spots() =>
            character != null && character.CurrentRoom != null ? character.CurrentRoom.Anchors : new List<StudioCharacterAnchor>();

        void StepSpot(int dir)
        {
            var spots = Spots();
            if (spots.Count == 0) return;
            spot = (spot + dir + spots.Count) % spots.Count;
            pose = 0;
            ShowSpot();
        }

        void StepPose(int dir)
        {
            var spots = Spots();
            if (spots.Count == 0) return;
            CollectPoses(spots[Mathf.Clamp(spot, 0, spots.Count - 1)]);
            if (poses.Count == 0) return;
            pose = (pose + dir + poses.Count) % poses.Count;
            ShowSpot();
        }

        void ShowSpot()
        {
            var spots = Spots();
            if (spots.Count == 0 || character == null) return;
            var a = spots[Mathf.Clamp(spot, 0, spots.Count - 1)];
            CollectPoses(a);
            if (poses.Count == 0) return;
            character.ForceChoice(a, poses[Mathf.Clamp(pose, 0, poses.Count - 1)]);
        }

        void CollectPoses(StudioCharacterAnchor a)
        {
            poses.Clear();
            if (character == null || character.Library == null) return;
            foreach (var p in a.supportedPoses)
                if (character.Library.Has(p)) poses.Add(character.Library.Get(p));
        }

        void SetFast(bool on)
        {
            if (character == null || fast == on) return;
            fast = on;
            if (on) { savedDelay = character.changeDelay; character.changeDelay = new Vector2(3f, 5f); character.ForceRandom(); }
            else character.changeDelay = savedDelay;
        }

        /// <summary>Hides the HUD (everything in the safe area) so only the room is visible; nothing is disabled.</summary>
        void SetUiHidden(bool hide)
        {
            if (uiGroup == null)
            {
                var safe = GameObject.Find("MainCanvas/SafeArea");
                if (safe == null) return;
                uiGroup = safe.GetComponent<CanvasGroup>();
                if (uiGroup == null) { uiGroup = safe.AddComponent<CanvasGroup>(); addedGroup = true; }
            }
            uiHidden = hide;
            uiGroup.alpha = hide ? 0f : 1f;
            uiGroup.blocksRaycasts = !hide;
            if (!hide && addedGroup) { Destroy(uiGroup); uiGroup = null; addedGroup = false; }
        }

        void Styles(float s)
        {
            if (button != null) return;
            button = new GUIStyle(GUI.skin.button) { fontSize = 26, fontStyle = FontStyle.Bold };
            label = new GUIStyle(GUI.skin.label) { fontSize = 24, wordWrap = true };
            label.normal.textColor = Color.white;
            box = new GUIStyle(GUI.skin.box);
            var bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0.05f, 0.03f, 0.1f, 0.88f));
            bg.Apply();
            box.normal.background = bg;
        }

        void OnGUI()
        {
            // a 1080-wide virtual screen, scaled to the real one
            float s = Screen.width / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float vh = Screen.height / s;
            Styles(s);

            if (GUI.Button(new Rect(0, vh * 0.42f, 90, 70), open ? "X" : "DEV", button)) open = !open;
            if (!open || !Find()) return;

            var area = new Rect(100, vh * 0.30f, 880, 0);
            area.height = Application.isEditor ? 380 : 345;
            GUI.Box(area, GUIContent.none, box);
            GUILayout.BeginArea(new Rect(area.x + 16, area.y + 12, area.width - 32, area.height - 24));

            int cur = CurrentRoomIndex();
            GUILayout.Label("DEVELOPER MODE - rooms (progress and save are not changed)", label);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < switcher.RoomCount; i++)
            {
                var go = switcher.RoomAt(i);
                string name = (i + 1) + " " + (go != null ? go.name : "?");
                GUI.color = i == cur ? new Color(1f, 0.6f, 1f) : Color.white;
                if (GUILayout.Button(name, button, GUILayout.Height(64))) ShowRoom(i);
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            var spots = Spots();
            string where = character == null ? "no character"
                : character.IsWorking ? "at the desk (working)"
                : character.CurrentAnchor != null ? character.CurrentAnchor.label + " / " + character.CurrentPose.pose : "-";
            GUILayout.Label("Character: " + where + "   (" + spots.Count + " spots in this room)", label);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("< Spot", button, GUILayout.Height(70))) StepSpot(-1);
            if (GUILayout.Button("Spot >", button, GUILayout.Height(70))) StepSpot(1);
            if (GUILayout.Button("Next pose", button, GUILayout.Height(70))) StepPose(1);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Random", button, GUILayout.Height(70)) && character != null) character.ForceRandom();
            if (GUILayout.Button("At the desk", button, GUILayout.Height(70)) && character != null) character.ForceWork();
            if (GUILayout.Button(fast ? "Fast: ON" : "Fast: off", button, GUILayout.Height(70))) SetFast(!fast);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(uiHidden ? "Show UI" : "Hide UI", button, GUILayout.Height(70))) SetUiHidden(!uiHidden);
            GUI.color = room >= 0 ? new Color(1f, 0.85f, 0.4f) : Color.white;
            if (GUILayout.Button("My studio", button, GUILayout.Height(70))) BackToMyStudio();
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            if (Application.isEditor) GUILayout.Label("Keys: F1 panel, Left/Right room, Up/Down spot, Space pose, H hide UI", label);
            GUILayout.EndArea();
        }
    }
}
#endif
