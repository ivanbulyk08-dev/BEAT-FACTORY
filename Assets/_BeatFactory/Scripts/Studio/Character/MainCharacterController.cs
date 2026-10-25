using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// The main character living in the painted studio. Every 30–90 s he picks a new activity (weighted, see
    /// <see cref="CharacterActivitySelector"/>), fades out, moves to a matching spot of the current room and fades in with the
    /// new pose. MAKE BEAT sends him to the desk (the existing seated-producer layers) for a while — at most once per
    /// cooldown, every further tap just keeps him working. One character object only: it is re-parented into whichever
    /// room the StudioBackdropSwitcher shows. Gameplay is untouched: this only listens to events.
    /// </summary>
    public class MainCharacterController : MonoBehaviour
    {
        [SerializeField] CharacterPoseLibrary library;
        [Tooltip("The character image (Image + CanvasGroup); created by the scene builder.")]
        [SerializeField] RectTransform character;
        [SerializeField] StudioBackdropSwitcher switcher;

        [Header("Random behaviour")]
        public CharacterActivitySelector selector = new CharacterActivitySelector();
        [Tooltip("Seconds between activity changes (random in this range).")]
        public Vector2 changeDelay = new Vector2(30f, 90f);
        public float fadeDuration = 0.2f;

        [Header("MAKE BEAT")]
        [Tooltip("Go to the desk when the player makes beats (only in rooms with a workstation).")]
        public bool workOnMakeBeat = true;
        [Tooltip("How long he keeps working after the last MAKE BEAT tap.")]
        public float workHold = 8f;
        [Tooltip("Minimum seconds between two trips to the desk.")]
        public float workCooldown = 20f;

        CanvasGroup group;
        StudioCharacterPlacement room;
        CharacterActivitySelector.Choice current;
        bool working, workRequested;
        float workUntil, lastWorkEnd = -999f;
        Coroutine life;
        readonly System.Random rng = new System.Random();
        readonly List<Graphic> workGraphics = new List<Graphic>(), found = new List<Graphic>();

        /// <summary>Current pose (null while working at the desk or when the room has no character setup).</summary>
        public CharacterPoseLibrary.Entry CurrentPose => working ? null : current.pose;
        public StudioCharacterAnchor CurrentAnchor => working ? null : current.anchor;
        public bool IsWorking => working;
        public StudioCharacterPlacement CurrentRoom => room;
        public CharacterPoseLibrary Library => library;

        // ---- developer mode (DevRoomTester): show a spot / pose / the desk right now, then carry on as usual

        /// <summary>Puts him on this spot with this pose immediately (no fade); the random behaviour continues from there.</summary>
        public void ForceChoice(StudioCharacterAnchor anchor, CharacterPoseLibrary.Entry pose)
        {
            if (room == null || anchor == null || pose == null || pose.sprite == null) return;
            if (life != null) StopCoroutine(life);
            ShowWorkViews(room, false, 0f);
            working = workRequested = false;
            current = new CharacterActivitySelector.Choice { pose = pose, anchor = anchor };
            Place(current);
            group.alpha = 1f;
            life = isActiveAndEnabled ? StartCoroutine(Life()) : null;
        }

        /// <summary>A new random activity right now.</summary>
        public void ForceRandom()
        {
            if (room == null) return;
            var pick = selector.Pick(library, room.Anchors, working ? default : current, rng);
            if (pick.IsValid) ForceChoice(pick.anchor, pick.pose);
        }

        /// <summary>Sends him to the desk now (rooms with a workstation).</summary>
        public void ForceWork()
        {
            if (room == null || !room.HasWorkstation) return;
            if (life != null) StopCoroutine(life);
            group.alpha = 0f;
            ShowWorkViews(room, true, 1f);
            PlaceAtWork();
            working = true;
            workRequested = false;
            workUntil = Time.unscaledTime + workHold;
            life = isActiveAndEnabled ? StartCoroutine(Life()) : null;
        }

        void Awake()
        {
            if (character != null)
            {
                group = character.GetComponent<CanvasGroup>();
                if (group == null) group = character.gameObject.AddComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                group.interactable = false;
                group.alpha = 0f;
            }
            if (switcher != null) switcher.RoomShown += OnRoomShown;
        }

        void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.Beats.BeatsCreated += OnBeats;
            if (room == null) OnRoomShown(FindActiveRoom());
        }

        void OnDestroy()
        {
            if (switcher != null) switcher.RoomShown -= OnRoomShown;
            if (GameManager.Instance != null) GameManager.Instance.Beats.BeatsCreated -= OnBeats;
        }

        GameObject FindActiveRoom()
        {
            if (switcher == null) return null;
            foreach (Transform t in switcher.transform)
                if (t.gameObject.activeSelf && t.GetComponent<StudioCharacterPlacement>() != null) return t.gameObject;
            return null;
        }

        /// <summary>Studio switched: cancel whatever he was doing and start fresh in the new room.</summary>
        void OnRoomShown(GameObject roomGo)
        {
            var next = roomGo != null ? roomGo.GetComponent<StudioCharacterPlacement>() : null;
            if (next == room && life != null) return;
            if (life != null) StopCoroutine(life);
            life = null;
            // previous room: put its producer back at the desk, so the room looks as before if it is shown without us
            if (room != null) ShowWorkViews(room, true, 1f);
            room = next;
            working = workRequested = false;
            current = default;
            if (group != null) group.alpha = 0f;
            if (room == null || character == null || library == null) return;
            ShowWorkViews(room, false, 0f);
            if (isActiveAndEnabled) life = StartCoroutine(Life());
        }

        void OnEnable()
        {
            // the coroutine dies with the object being disabled (maybe mid-fade); resume fully visible when it comes back
            if (room == null || life != null || !Application.isPlaying) return;
            if (working) WorkAlpha(1f);
            else if (current.IsValid && group != null) group.alpha = 1f;
            life = StartCoroutine(Life());
        }

        void OnDisable() => life = null;

        void OnBeats(BeatResult r)
        {
            if (!workOnMakeBeat || r.Source != BeatSource.Manual || room == null || !room.HasWorkstation) return;
            if (working) { workUntil = Time.unscaledTime + workHold; return; }
            if (Time.unscaledTime - lastWorkEnd >= workCooldown) workRequested = true;
        }

        IEnumerator Life()
        {
            // starting pose (if this room has no usable spot/art he stays at the desk like before)
            if (!current.IsValid && !working)
            {
                var first = selector.Pick(library, room.Anchors, default, rng);
                if (!first.IsValid)
                {
                    working = true;
                    ShowWorkViews(room, true, 1f);
                    yield break;
                }
                current = first;
                Place(current);
                yield return Fade(a => group.alpha = a, 0f, 1f);
            }
            while (true)
            {
                if (working)
                {
                    yield return new WaitUntil(() => Time.unscaledTime >= workUntil);
                    lastWorkEnd = Time.unscaledTime;
                    working = false;
                    var next = selector.Pick(library, room.Anchors, current, rng);
                    if (!next.IsValid) { working = true; workUntil = float.MaxValue; continue; }
                    yield return Fade(WorkAlpha, 1f, 0f);
                    ShowWorkViews(room, false, 0f);
                    current = next;
                    Place(current);
                    yield return Fade(a => group.alpha = a, 0f, 1f);
                    continue;
                }

                float until = Time.unscaledTime + UnityEngine.Random.Range(changeDelay.x, Mathf.Max(changeDelay.x, changeDelay.y));
                workRequested = false;
                yield return new WaitUntil(() => workRequested || Time.unscaledTime >= until);

                if (workRequested && room.HasWorkstation)
                {
                    workRequested = false;
                    yield return Fade(a => group.alpha = a, group.alpha, 0f);
                    ShowWorkViews(room, true, 0f);
                    PlaceAtWork();
                    working = true;
                    workUntil = Time.unscaledTime + workHold;
                    yield return Fade(WorkAlpha, 0f, 1f);
                    continue;
                }

                var pick = selector.Pick(library, room.Anchors, current, rng);
                if (!pick.IsValid || (pick.pose == current.pose && pick.anchor == current.anchor)) continue;
                yield return Fade(a => group.alpha = a, group.alpha, 0f);
                current = pick;
                Place(current);
                yield return Fade(a => group.alpha = a, 0f, 1f);
            }
        }

        /// <summary>Rooms without desk layers: he himself goes to the workstation anchor.</summary>
        void PlaceAtWork()
        {
            if (room.HasWorkViews) return;
            var a = room.WorkAnchor;
            if (a == null) return;
            foreach (var p in a.supportedPoses)
                if (library.Has(p))
                {
                    var c = new CharacterActivitySelector.Choice { pose = library.Get(p), anchor = a };
                    Place(c);
                    current = c;
                    return;
                }
        }

        /// <summary>Visibility of the "working" view: the desk layers, or the character himself at the work anchor.</summary>
        void WorkAlpha(float a)
        {
            if (room.HasWorkViews) SetWorkAlpha(a);
            else group.alpha = a;
        }

        void Place(CharacterActivitySelector.Choice c)
        {
            character.gameObject.SetActive(true);
            room.Place(character, c.pose, c.anchor, StudioCharacterPlacement.Mirror(c.pose, c.anchor, rng));
        }

        IEnumerator Fade(Action<float> set, float from, float to)
        {
            float t = 0f, d = Mathf.Max(0.01f, fadeDuration);
            while (t < d)
            {
                set(Mathf.Lerp(from, to, t / d));
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            set(to);
        }

        /// <summary>Producer-at-the-desk layers on/off (inactive when hidden: no animation cost), at an alpha.</summary>
        void ShowWorkViews(StudioCharacterPlacement r, bool show, float alpha)
        {
            foreach (var v in r.workstationViews)
                if (v != null && v.activeSelf != show) v.SetActive(show);
            SetWorkAlpha(r, alpha);
        }

        void SetWorkAlpha(float a) => SetWorkAlpha(room, a);

        void SetWorkAlpha(StudioCharacterPlacement r, float a)
        {
            workGraphics.Clear();

            foreach (var v in r.workstationViews)
            {
                if (v == null) continue;
                v.GetComponentsInChildren(true, found);
                workGraphics.AddRange(found);
            }
            foreach (var g in workGraphics) g.canvasRenderer.SetAlpha(a);
        }
    }
}
