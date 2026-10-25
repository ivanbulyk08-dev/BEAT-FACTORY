using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Character setup of one painted room (sits on the room image): its anchors (child StudioCharacterAnchor objects) and
    /// the producer-at-the-desk layers used for the "working" activity. Right-click the component → "Preview character
    /// on all anchors" to see every spot with its first pose in the editor.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class StudioCharacterPlacement : MonoBehaviour
    {
        [Tooltip("Size of the room painting in pixels (anchor scales are in these pixels).")]
        public Vector2 artSize = new Vector2(1440, 2560);
        [Tooltip("The producer at the desk (body + head): shown while the character works, hidden otherwise. Empty = no workstation.")]
        public GameObject[] workstationViews = new GameObject[0];
        [Tooltip("Pose library used by the editor preview (the game uses the controller's library).")]
        public CharacterPoseLibrary previewLibrary;

        readonly List<StudioCharacterAnchor> anchors = new List<StudioCharacterAnchor>();
        readonly List<Transform> foreground = new List<Transform>();

        public bool HasWorkstation => HasWorkViews || WorkAnchor != null;

        /// <summary>The room has separate desk layers (the seated producer) rather than a workstation anchor.</summary>
        public bool HasWorkViews
        {
            get
            {
                foreach (var v in workstationViews) if (v != null) return true;
                return false;
            }
        }

        /// <summary>The MAKE BEAT spot, when the character himself works at the desk.</summary>
        public StudioCharacterAnchor WorkAnchor
        {
            get
            {
                foreach (var a in Anchors) if (a.workstation) return a;
                return null;
            }
        }

        /// <summary>Anchors under this room (collected from the children every call, so added/removed spots just work).</summary>
        public List<StudioCharacterAnchor> Anchors
        {
            get
            {
                anchors.Clear();
                GetComponentsInChildren(true, anchors);
                anchors.RemoveAll(a => a == null || !a.enabled);
                return anchors;
            }
        }

        /// <summary>Puts the character image on an anchor: size from the sprite pixels × anchor scale, feet on the anchor.</summary>
        public void Place(RectTransform character, CharacterPoseLibrary.Entry pose, StudioCharacterAnchor anchor, bool mirror)
        {
            var room = (RectTransform)transform;
            if (character.parent != room) character.SetParent(room, false);
            var view = character.GetComponent<MainCharacterView>();
            if (view != null) view.Show(pose, anchor.lighting, mirror);
            else character.GetComponent<Image>().sprite = pose.sprite;
            // size as a fraction of the painting: scales with the room on every screen, no per-frame work
            var sizeUV = new Vector2(pose.sprite.rect.width * anchor.scale / artSize.x, pose.sprite.rect.height * anchor.scale / artSize.y);
            // the pivot is the ground point: it sits on the anchor and mirroring (scale.x = -1) flips around it
            var pivot = pose.groundPivot;
            var min = anchor.RoomUV(room) - Vector2.Scale(pivot, sizeUV);
            character.pivot = pivot;
            character.anchorMin = min;
            character.anchorMax = min + sizeUV;
            character.offsetMin = character.offsetMax = Vector2.zero;
            character.localScale = new Vector3(mirror ? -1f : 1f, 1f, 1f);
            character.localRotation = Quaternion.identity;
            Sort(character, anchor);
        }

        /// <summary>
        /// Sibling order. WithFurniture: just below the room's first foreground piece (StudioForeground: bed, sofa...) and
        /// above everything else, light overlays included. Front: on top of the whole room. Anchor occluders always cover him.
        /// </summary>
        void Sort(RectTransform character, StudioCharacterAnchor anchor)
        {
            int index = -1;
            void Below(Transform t)
            {
                if (t != null && t.parent == transform && (index < 0 || t.GetSiblingIndex() < index)) index = t.GetSiblingIndex();
            }
            if (anchor.occluders != null)
                foreach (var o in anchor.occluders) Below(o);
            if (anchor.sortingLayer == StudioCharacterAnchor.Layer.WithFurniture)
            {
                // foreground pieces go to the top (over the light overlays) so the character can sit between both
                foreground.Clear();
                foreach (Transform child in transform)
                    if (child.GetComponent<StudioForeground>() != null) foreground.Add(child);
                foreach (var f in foreground) f.SetAsLastSibling();
                foreach (var f in foreground) Below(f);
            }
            if (index < 0) { character.SetAsLastSibling(); return; }
            if (character.parent == transform && character.GetSiblingIndex() < index) index--; // moving down the list shifts the target
            character.SetSiblingIndex(index);
        }

        /// <summary>Whether the pose must be mirrored to face the anchor's direction.</summary>
        public static bool Mirror(CharacterPoseLibrary.Entry pose, StudioCharacterAnchor anchor, System.Random rng)
        {
            switch (anchor.facing)
            {
                case StudioCharacterAnchor.Direction.Left: return pose.facing == CharacterFacing.Right;
                case StudioCharacterAnchor.Direction.Right: return pose.facing == CharacterFacing.Left;
                case StudioCharacterAnchor.Direction.Random: return pose.facing != CharacterFacing.Front && rng != null && rng.Next(2) == 0;
                default: return false;
            }
        }

#if UNITY_EDITOR
        const string PreviewName = "__CharacterPreview";

        [ContextMenu("Preview character on all anchors")]
        void PreviewAll()
        {
            ClearPreview();
            if (previewLibrary == null) { Debug.LogWarning("Assign a Preview Library first.", this); return; }
            foreach (var a in Anchors)
            {
                CharacterPoseLibrary.Entry pose = null;
                foreach (var p in a.supportedPoses)
                    if (previewLibrary.Has(p)) { pose = previewLibrary.Get(p); break; }
                if (pose == null) { Debug.LogWarning("No art for any pose of anchor " + a.label, a); continue; }
                var go = new GameObject(PreviewName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.hideFlags = HideFlags.DontSave;
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                img.color = new Color(1f, 1f, 1f, 0.85f);
                Place((RectTransform)go.transform, pose, a, Mirror(pose, a, null));
            }
        }

        /// <summary>
        /// Fills every anchor's lighting from the room: AMBIENT = colour and brightness of the painting where the character
        /// stands; KEY/RIM = direction and colour of the room's lights (StudioLightGlow), weighted by brightness, size and
        /// distance. Run again after moving anchors; hand-tune the values afterwards if needed.
        /// </summary>
        [ContextMenu("Auto-light anchors from the painting")]
        public void AutoLight()
        {
            var roomImage = GetComponent<Image>();
            if (roomImage == null || roomImage.sprite == null) return;
            var path = UnityEditor.AssetDatabase.GetAssetPath(roomImage.sprite.texture);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(System.IO.File.ReadAllBytes(path))) { DestroyImmediate(tex); return; }
            var px = tex.GetPixels32();
            int tw = tex.width, th = tex.height;
            DestroyImmediate(tex);
            float toTex = tw / artSize.x; // the file can be bigger than the art size the anchors use

            var glows = GetComponentsInChildren<StudioLightGlow>(true);
            foreach (var a in Anchors)
            {
                var room = (RectTransform)transform;
                var uv = a.RoomUV(room);
                var foot = new Vector2(uv.x * artSize.x, (1f - uv.y) * artSize.y); // art px, top-left origin
                float h = 620f * a.scale;   // about one figure tall
                var center = foot - new Vector2(0, h * 0.5f);

                // ambient: average of the painting over the figure's area
                double r = 0, g = 0, b = 0; int n = 0;
                for (float y = foot.y - h; y <= foot.y; y += h / 24f)
                    for (float x = center.x - h * 0.22f; x <= center.x + h * 0.22f; x += h / 48f)
                    {
                        int ix = Mathf.Clamp(Mathf.RoundToInt(x * toTex), 0, tw - 1), iy = Mathf.Clamp(th - 1 - Mathf.RoundToInt(y * toTex), 0, th - 1);
                        var c = px[iy * tw + ix];
                        r += c.r; g += c.g; b += c.b; n++;
                    }
                var avg = new Color((float)(r / n / 255.0), (float)(g / n / 255.0), (float)(b / n / 255.0));
                float lum = 0.299f * avg.r + 0.587f * avg.g + 0.114f * avg.b;
                float maxC = Mathf.Max(0.01f, Mathf.Max(avg.r, Mathf.Max(avg.g, avg.b)));
                var hue = new Color(avg.r / maxC, avg.g / maxC, avg.b / maxC);
                float brightness = Mathf.Clamp(0.5f + lum * 1.6f, 0.55f, 1f);
                var light = a.lighting ?? new CharacterLighting();
                light.ambient = Color.Lerp(Color.white, hue, 0.4f) * brightness;
                light.ambient.a = 1f;

                // key + rim: the room's lights, nearer / bigger / brighter ones count more
                Vector2 dir = Vector2.zero; Color col = Color.black; float wsum = 0f;
                foreach (var gl in glows)
                {
                    var grt = (RectTransform)gl.transform;
                    var gp = new Vector2(grt.anchorMin.x * artSize.x, (1f - grt.anchorMin.y) * artSize.y);
                    var size = gl.GetComponent<ArtPixelSize>() is ArtPixelSize ap ? ap.sizeInArtPixels : new Vector2(100, 100);
                    var d = gp - center;
                    d.y = -d.y; // screen: y up
                    float dist = d.magnitude;
                    float w = gl.baseAlpha * Mathf.Sqrt(Mathf.Max(1f, size.x * size.y)) / (dist + h * 0.35f) * (gl.kind == StudioLightGlow.Kind.Lamp ? 1.2f : 1f);
                    var gc = gl.GetComponent<Graphic>().color;
                    dir += w * (dist > 1f ? d / dist : Vector2.up);
                    col += gc * w;
                    wsum += w;
                }
                if (wsum > 0.0001f)
                {
                    col /= wsum;
                    float cm = Mathf.Max(0.01f, Mathf.Max(col.r, Mathf.Max(col.g, col.b)));
                    var rim = new Color(col.r / cm, col.g / cm, col.b / cm, 1f);
                    light.rimColor = rim;
                    light.keyColor = Color.Lerp(Color.white, rim, 0.55f);
                    light.lightDirection = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.up;
                    float focus = dir.magnitude / wsum;                 // 1 = all light from one side
                    float amount = Mathf.Clamp01(wsum / 0.25f);         // how much light there is around
                    light.rimStrength = Mathf.Lerp(0.5f, 1.2f, amount) * Mathf.Lerp(0.6f, 1f, focus);
                    light.rimWidth = 2.5f;
                    light.keyStrength = Mathf.Lerp(0.2f, 0.55f, amount * focus);
                }
                light.floorShade = 0.32f;
                light.desaturate = 0.14f;
                a.lighting = light;
                UnityEditor.EditorUtility.SetDirty(a);
                Debug.Log($"[AutoLight] {name}/{a.label}: lum {lum:0.00} ambient {light.ambient} rim {light.rimColor} x{light.rimStrength:0.00} dir {light.lightDirection} w {wsum:0.000}");
            }
        }

        [ContextMenu("Clear character preview")]
        void ClearPreview()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                if (transform.GetChild(i).name == PreviewName) DestroyImmediate(transform.GetChild(i).gameObject);
        }
#endif
    }
}
