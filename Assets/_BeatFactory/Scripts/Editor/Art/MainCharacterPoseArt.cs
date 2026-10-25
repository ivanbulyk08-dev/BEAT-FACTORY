using System.IO;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// Main character poses: cuts the user's pose sheet (Art/Reference/main_character_pose_sheet.png, 1024×1536, checkerboard
    /// painted into the pixels — no real alpha) into one transparent PNG per figure with <see cref="PoseSheetSplitter"/>,
    /// and keeps the pose library asset (Data/Characters/MainCharacterPoses.asset) pointing at them.
    /// Sprites the user assigned by hand in the library are never replaced.
    /// </summary>
    public static class MainCharacterPoseArt
    {
        public const string Sheet = BeatFactorySceneBuilder.Root + "/Art/Reference/main_character_pose_sheet.png";
        public const string OutDir = BeatFactorySceneBuilder.Root + "/Art/Characters/main_character/poses";
        public const string LibraryPath = BeatFactorySceneBuilder.Root + "/Data/Characters/MainCharacterPoses.asset";

        /// <summary>Enclosed checkerboard holes (sheet px, top-left): under raised arms, between knee and body.</summary>
        static readonly int[] Holes = { 741, 92, 514, 940, 588, 1007, 533, 1202, 905, 892, 911, 982, 553, 904 };

        /// <summary>One seed per figure (torso), plus extra seeds where figures touch so each keeps its own sneakers / hair.</summary>
        static readonly PoseSheetSplitter.Seed[] Seeds =
        {
            new PoseSheetSplitter.Seed("StandIdle", 120, 250),
            new PoseSheetSplitter.Seed("StandSide", 320, 250),
            new PoseSheetSplitter.Seed("StandBack", 505, 250),
            new PoseSheetSplitter.Seed("StandHeadTouch", 690, 250),
            new PoseSheetSplitter.Seed("StandArmsCrossed", 930, 300),
            new PoseSheetSplitter.Seed("Crouch", 160, 950),
            new PoseSheetSplitter.Seed("LeanBack", 430, 950),
            new PoseSheetSplitter.Seed("SitThinking", 860, 950),
            new PoseSheetSplitter.Seed("SitFloor", 150, 1300),
            new PoseSheetSplitter.Seed("SitRelaxed", 480, 1300),
            new PoseSheetSplitter.Seed("SitPhone", 800, 1330),
            new PoseSheetSplitter.Seed("SitRelaxed", 440, 1430),
            new PoseSheetSplitter.Seed("SitPhone", 790, 1195),
            new PoseSheetSplitter.Seed("SitFloor", 410, 1480),
            new PoseSheetSplitter.Seed("SitPhone", 852, 1196),
            new PoseSheetSplitter.Seed("SitThinking", 868, 1188),
            new PoseSheetSplitter.Seed("SitFloor", 330, 1460),
            new PoseSheetSplitter.Seed("SitThinking", 905, 1165),
            new PoseSheetSplitter.Seed("SitThinking", 965, 1170),
        };

        struct Def
        {
            public CharacterPose pose;
            public CharacterActivity activity;
            public Vector2 pivot;
            public CharacterFacing facing;
            public Def(CharacterPose p, CharacterActivity a, float px, float py, CharacterFacing f) { pose = p; activity = a; pivot = new Vector2(px, py); facing = f; }
        }

        /// <summary>Activity, ground point (feet for standing, seat for sitting; 0..1 from bottom-left) and drawing direction.</summary>
        static readonly Def[] Defs =
        {
            new Def(CharacterPose.StandIdle, CharacterActivity.Standing, 0.5f, 0.03f, CharacterFacing.Front),
            new Def(CharacterPose.StandSide, CharacterActivity.Standing, 0.5f, 0.03f, CharacterFacing.Right),
            new Def(CharacterPose.StandArmsCrossed, CharacterActivity.Standing, 0.5f, 0.03f, CharacterFacing.Front),
            new Def(CharacterPose.SitFloor, CharacterActivity.Sitting, 0.2f, 0.25f, CharacterFacing.Right),
            new Def(CharacterPose.SitThinking, CharacterActivity.Sitting, 0.3f, 0.12f, CharacterFacing.Right),
            new Def(CharacterPose.Crouch, CharacterActivity.Sitting, 0.5f, 0.05f, CharacterFacing.Front),
            new Def(CharacterPose.SitRelaxed, CharacterActivity.Relaxing, 0.25f, 0.2f, CharacterFacing.Right),
            new Def(CharacterPose.LeanBack, CharacterActivity.Relaxing, 0.4f, 0.1f, CharacterFacing.Right),
            new Def(CharacterPose.StandHeadTouch, CharacterActivity.Relaxing, 0.5f, 0.03f, CharacterFacing.Front),
            new Def(CharacterPose.SitPhone, CharacterActivity.Special, 0.25f, 0.25f, CharacterFacing.Right),
            new Def(CharacterPose.StandBack, CharacterActivity.Special, 0.5f, 0.03f, CharacterFacing.Front),
            // SitChair: no art yet (the desk uses the existing seated-producer layers)
            new Def(CharacterPose.SitChair, CharacterActivity.Sitting, 0.5f, 0.3f, CharacterFacing.Front),
        };

        /// <summary>Poses taken out of the game (not cut, removed from the library).</summary>
        public static readonly CharacterPose[] Removed = { CharacterPose.SitFloor, CharacterPose.SitPhone, CharacterPose.LeanBack };

        static bool IsRemoved(string name) => System.Array.Exists(Removed, p => p.ToString() == name);

        public static string PosePath(CharacterPose pose) => OutDir + "/" + pose + ".png";

        [MenuItem("Beat Factory/Art/Cut Main Character Poses")]
        public static void CutMenu()
        {
            Cut();
            EnsureLibrary();
        }

        /// <summary>Writes one PNG per figure of the sheet (overwrites earlier cuts).</summary>
        public static void Cut()
        {
            if (!File.Exists(Sheet)) { Debug.LogWarning("Pose sheet missing: " + Sheet); return; }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(Sheet));
            int w = tex.width, h = tex.height;
            var px = tex.GetPixels32();
            Object.DestroyImmediate(tex);
            var rgba = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = px[(h - 1 - y) * w + x]; // Unity rows are bottom-up
                    int i = (y * w + x) * 4;
                    rgba[i] = c.r; rgba[i + 1] = c.g; rgba[i + 2] = c.b; rgba[i + 3] = 255;
                }
            var bg = PoseSheetSplitter.BackgroundMask(rgba, w, h, 200, 14, Holes);
            var owner = PoseSheetSplitter.Assign(bg, w, h, Seeds);
            var names = PoseSheetSplitter.FigureNames(Seeds);
            Directory.CreateDirectory(OutDir);
            for (int f = 0; f < names.Count; f++)
            {
                if (IsRemoved(names[f])) continue;
                var fig = PoseSheetSplitter.Extract(rgba, owner, w, h, f, names[f], 2);
                if (fig == null) continue;
                var outTex = new Texture2D(fig.w, fig.h, TextureFormat.RGBA32, false);
                var o = new Color32[fig.w * fig.h];
                for (int y = 0; y < fig.h; y++)
                    for (int x = 0; x < fig.w; x++)
                    {
                        int i = (y * fig.w + x) * 4;
                        o[(fig.h - 1 - y) * fig.w + x] = new Color32(fig.rgba[i], fig.rgba[i + 1], fig.rgba[i + 2], fig.rgba[i + 3]);
                    }
                outTex.SetPixels32(o);
                outTex.Apply();
                File.WriteAllBytes(OutDir + "/" + names[f] + ".png", outTex.EncodeToPNG());
                Object.DestroyImmediate(outTex);
            }
            AssetDatabase.Refresh();
            foreach (var n in names) if (!IsRemoved(n)) ImportSprite(OutDir + "/" + n + ".png");
        }

        static void ImportSprite(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.maxTextureSize = 1024;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.SaveAndReimport();
        }

        /// <summary>Creates the pose library if needed and fills every entry that has no sprite yet.</summary>
        public static CharacterPoseLibrary EnsureLibrary()
        {
            if (!File.Exists(PosePath(CharacterPose.StandIdle))) Cut();
            var lib = AssetDatabase.LoadAssetAtPath<CharacterPoseLibrary>(LibraryPath);
            if (lib == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath));
                lib = ScriptableObject.CreateInstance<CharacterPoseLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }
            lib.poses.RemoveAll(e => e == null || System.Array.IndexOf(Removed, e.pose) >= 0);
            foreach (var d in Defs)
            {
                if (System.Array.IndexOf(Removed, d.pose) >= 0) continue;
                var e = lib.Get(d.pose);
                if (e == null)
                {
                    e = new CharacterPoseLibrary.Entry { pose = d.pose, activity = d.activity, groundPivot = d.pivot, facing = d.facing };
                    lib.poses.Add(e);
                }
                if (e.sprite == null) e.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PosePath(d.pose)); // stays null when there is no art
            }
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            return lib;
        }
    }
}
