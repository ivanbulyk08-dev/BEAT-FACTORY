using UnityEngine;
using UnityEngine.UI;
using static BeatFactory.CharacterPose;
using Dir = BeatFactory.StudioCharacterAnchor.Direction;
using Layer = BeatFactory.StudioCharacterAnchor.Layer;

namespace BeatFactory.EditorTools
{
    /// <summary>
    /// Main character in the painted rooms: one character object + MainCharacterController on the backdrop, and per room a
    /// StudioCharacterPlacement with anchors placed on the furniture of that painting (art px, top-left origin, like ArtUV).
    /// MAKE BEAT "working" view: a workstation anchor (he stands at the desk) or separate desk layers (CreateSeatedProducer).
    /// </summary>
    public static partial class BeatFactorySceneBuilder
    {
        static readonly CharacterPose[] StandPoses = { StandIdle, StandArmsCrossed, StandSide, StandHeadTouch };
        static readonly CharacterPose[] WindowPoses = { StandBack, StandIdle, StandArmsCrossed };
        static readonly CharacterPose[] FloorPoses = { Crouch, SitRelaxed, SitThinking };
        static readonly CharacterPose[] SeatPoses = { SitThinking, SitRelaxed };
        static readonly CharacterPose[] WorkPoses = { StandSide, StandIdle };

        struct Spot
        {
            public string label;
            public Vector2 px;
            public float scale;
            public Dir facing;
            public Layer layer;
            public CharacterPose[] poses;
            public bool work;
            public Spot(string label, float x, float y, float scale, CharacterPose[] poses, Dir facing = Dir.AsDrawn, Layer layer = Layer.WithFurniture, bool work = false)
            {
                this.label = label; px = new Vector2(x, y); this.scale = scale; this.poses = poses; this.facing = facing; this.layer = layer; this.work = work;
            }
        }

        /// <summary>Character setup of a room: the anchors and the desk (working) view. Uses the current ArtW/ArtH.</summary>
        static void CharacterSpots(RectTransform room, GameObject[] workViews, params Spot[] spots)
        {
            var placement = room.gameObject.AddComponent<StudioCharacterPlacement>();
            placement.artSize = new Vector2(ArtW, ArtH);
            placement.workstationViews = workViews ?? new GameObject[0];
            placement.previewLibrary = Art.MainCharacterPoseArt.EnsureLibrary();
            var holder = UIFactory.Rect("CharacterAnchors", room);
            UIFactory.Stretch(holder);
            foreach (var s in spots)
            {
                var a = UIFactory.Rect("Anchor_" + s.label, holder);
                a.anchorMin = a.anchorMax = ArtUV(s.px.x, s.px.y);
                a.sizeDelta = new Vector2(40, 40);
                a.anchoredPosition = Vector2.zero;
                var anchor = a.gameObject.AddComponent<StudioCharacterAnchor>();
                anchor.label = s.label;
                anchor.scale = s.scale;
                anchor.facing = s.facing;
                anchor.sortingLayer = s.layer;
                anchor.supportedPoses = s.poses;
                anchor.workstation = s.work;
            }
        }

        const string CharacterLitMaterialPath = Root + "/Art/Characters/main_character/CharacterLit.mat";

        /// <summary>The single character object and its controller (on the backdrop holder, next to the room switcher).</summary>
        static void CreateMainCharacter(RectTransform holder, StudioBackdropSwitcher switcher)
        {
            // root (placed + faded) → contact shadow, then the lit body on top
            var root = UIFactory.Rect("MainCharacter", holder);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            var shadow = UIFactory.Image("Shadow", root, new Color(0f, 0f, 0f, 0.55f));
            shadow.sprite = UIStyle.RadialSprite;
            shadow.raycastTarget = false;
            var body = UIFactory.Image("Body", root, Color.white);
            body.raycastTarget = false;
            body.preserveAspect = false;
            body.material = CharacterLitMaterial();
            UIFactory.Stretch(body.rectTransform);
            // STORE clothes over the body (same rect, filled per pose)
            Image Clothes(string name)
            {
                var img = UIFactory.Image(name, root, Color.white);
                img.raycastTarget = false;
                img.preserveAspect = false;
                img.enabled = false;
                UIFactory.Stretch(img.rectTransform);
                return img;
            }
            var pants = Clothes("Pants");
            var top = Clothes("Top");
            var view = root.gameObject.AddComponent<MainCharacterView>();
            view.Init(body, shadow);
            UIFactory.SetRef(view, "pantsLayer", pants);
            UIFactory.SetRef(view, "topLayer", top);
            UIFactory.SetRef(view, "store", Art.StoreItemArt.Ensure());

            var controller = holder.gameObject.AddComponent<MainCharacterController>();
            UIFactory.SetRef(controller, "library", Art.MainCharacterPoseArt.EnsureLibrary());
            UIFactory.SetRef(controller, "character", root);
            UIFactory.SetRef(controller, "switcher", switcher);

            // light every spot from its painting and the room's lamps / neon (the rooms are complete now)
            foreach (var p in holder.GetComponentsInChildren<StudioCharacterPlacement>(true)) p.AutoLight();
        }

        static Material CharacterLitMaterial()
        {
            var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(CharacterLitMaterialPath);
            if (mat != null) return mat;
            var shader = Shader.Find("BeatFactory/UI/CharacterLit");
            if (shader == null) return null;
            mat = new Material(shader);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(CharacterLitMaterialPath));
            UnityEditor.AssetDatabase.CreateAsset(mat, CharacterLitMaterialPath);
            return mat;
        }
    }
}
