using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// Imports a detailed artist from Assets/_BeatFactory/Art/Artists/&lt;id&gt;/ (separate transparent PNGs + rig.txt):
    /// sets the sprite import settings and pivots (each sprite pivots on its joint of the shared template), creates/updates
    /// the ArtistRig and WardrobeItem assets, links the rig to the artist and reports missing or suspicious files.
    /// File names and rig.txt format: Art/Artists/README.md (also in ARTIST_ART_PLAN.md).
    /// </summary>
    public static class ArtistRigImporter
    {
        public const string ArtistsArt = BeatFactorySceneBuilder.Root + "/Art/Artists";
        public const string RigsFolder = BeatFactorySceneBuilder.Root + "/Data/ArtistRigs";
        public const string WardrobeFolder = BeatFactorySceneBuilder.Root + "/Data/Wardrobe";

        public static string Folder(string id) => ArtistsArt + "/" + id;
        public static string RigAssetPath(string id) => RigsFolder + "/" + id + ".asset";

        /// <summary>Heads in ArtistVisualIdentity order (idle, excited, smile, proud, blink): file suffixes, first found wins.</summary>
        static readonly string[][] HeadFiles = { new[] { "idle" }, new[] { "rap", "hype" }, new[] { "smile" }, new[] { "proud" }, new[] { "blink" } };
        static readonly string[] HandFiles = { "hand_mic", "hand_open", "hand_point", "hand_fist" };

        static readonly Dictionary<string, RigPart> BodyFiles = new Dictionary<string, RigPart>
        {
            { "neck", RigPart.Neck }, { "torso_base", RigPart.Torso },
            { "upperarm_near", RigPart.UpperArmNear }, { "forearm_near", RigPart.ForearmNear },
            { "upperarm_far", RigPart.UpperArmFar }, { "forearm_far", RigPart.ForearmFar }, { "hand_far", RigPart.HandFar },
            { "leg_near", RigPart.LegNear }, { "leg_far", RigPart.LegFar },
        };

        /// <summary>Item file suffix → part, per slot.</summary>
        static readonly Dictionary<WardrobeSlot, Dictionary<string, RigPart>> ItemFiles = new Dictionary<WardrobeSlot, Dictionary<string, RigPart>>
        {
            { WardrobeSlot.Hair, new Dictionary<string, RigPart> { { "back", RigPart.HairBack }, { "front", RigPart.HairFront } } },
            { WardrobeSlot.Top, new Dictionary<string, RigPart> { { "torso", RigPart.Torso }, { "upperarm_near", RigPart.UpperArmNear }, { "upperarm_far", RigPart.UpperArmFar } } },
            { WardrobeSlot.Jacket, new Dictionary<string, RigPart> { { "torso", RigPart.Torso }, { "upperarm_near", RigPart.UpperArmNear }, { "upperarm_far", RigPart.UpperArmFar },
                { "forearm_near", RigPart.ForearmNear }, { "forearm_far", RigPart.ForearmFar }, { "collar", RigPart.Collar } } },
            { WardrobeSlot.Pants, new Dictionary<string, RigPart> { { "waist", RigPart.Waist }, { "leg_near", RigPart.LegNear }, { "leg_far", RigPart.LegFar } } },
            { WardrobeSlot.Shoes, new Dictionary<string, RigPart> { { "near", RigPart.ShoeNear }, { "far", RigPart.ShoeFar } } },
            { WardrobeSlot.Chain, new Dictionary<string, RigPart> { { "torso", RigPart.Chest } } },
            { WardrobeSlot.Glasses, new Dictionary<string, RigPart> { { "head", RigPart.Face } } },
            { WardrobeSlot.Rings, new Dictionary<string, RigPart> { { "hand", RigPart.HandNear } } },
        };

        static string SlotName(WardrobeSlot s) => s.ToString().ToLowerInvariant();

        // ------------------------------------------------------------------ rig.txt

        public class RigFile
        {
            public Vector2 canvas = new Vector2(800, 1050);
            public readonly Dictionary<RigJoint, Vector2> joints = new Dictionary<RigJoint, Vector2>();
            public readonly Dictionary<string, Vector2> offsets = new Dictionary<string, Vector2>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<WardrobeSlot, string> defaults = new Dictionary<WardrobeSlot, string>();
            public readonly Dictionary<string, (string name, int studio, double price)> items = new Dictionary<string, (string, int, double)>();
            public readonly List<string> errors = new List<string>();
        }

        /// <summary>
        /// Lines: "canvas W H" · "joint Name X Y" · "offset file.png X Y" (where the PNG's bottom-left sits on the canvas;
        /// omit for full-canvas exports) · "default slot itemId" · "item slot_itemId | English name | studio level | price".
        /// </summary>
        public static RigFile ParseRig(string text)
        {
            var r = new RigFile();
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            int n = 0;
            foreach (var raw in text.Split('\n'))
            {
                n++;
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var w = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    switch (w[0].ToLowerInvariant())
                    {
                        case "canvas": r.canvas = new Vector2(float.Parse(w[1], inv), float.Parse(w[2], inv)); break;
                        case "joint": r.joints[(RigJoint)Enum.Parse(typeof(RigJoint), w[1], true)] = new Vector2(float.Parse(w[2], inv), float.Parse(w[3], inv)); break;
                        case "offset": r.offsets[w[1]] = new Vector2(float.Parse(w[2], inv), float.Parse(w[3], inv)); break;
                        case "default": r.defaults[(WardrobeSlot)Enum.Parse(typeof(WardrobeSlot), w[1], true)] = w[2]; break;
                        case "item":
                        {
                            var parts = line.Substring(4).Split('|').Select(x => x.Trim()).ToArray();
                            r.items[parts[0]] = (parts.Length > 1 ? parts[1] : parts[0], parts.Length > 2 ? int.Parse(parts[2], inv) : 0, parts.Length > 3 ? double.Parse(parts[3], inv) : 0);
                            break;
                        }
                        default: r.errors.Add($"line {n}: unknown '{w[0]}'"); break;
                    }
                }
                catch (Exception e) { r.errors.Add($"line {n}: {e.Message}"); }
            }
            foreach (RigJoint j in Enum.GetValues(typeof(RigJoint)))
                if (!r.joints.ContainsKey(j)) r.errors.Add("missing joint " + j);
            return r;
        }

        // ------------------------------------------------------------------ import

        [MenuItem("Tools/Beat Factory/Art/Import Artist Rig (NØX)")]
        static void ImportNoxMenu() => Debug.Log(Import("nox"));

        /// <summary>Imports one artist folder. Returns the report (also logged by the menu).</summary>
        public static string Import(string id)
        {
            var report = new StringBuilder($"[Artist Rig] {id}\n");
            string folder = Folder(id);
            string rigPath = folder + "/rig.txt";
            if (!File.Exists(rigPath)) return report.Append("ERROR: " + rigPath + " not found.").ToString();
            var rf = ParseRig(File.ReadAllText(rigPath));
            foreach (var e in rf.errors) report.AppendLine("ERROR rig.txt: " + e);
            bool placeholder = File.Exists(folder + "/PLACEHOLDER.txt");
            if (placeholder) report.AppendLine("NOTE: placeholder art (generated), not production sprites.");

            var files = Directory.GetFiles(folder, "*.png").Select(f => f.Replace('\\', '/')).ToList();
            string prefix = id + "_";
            Sprite Load(string name, RigPart? pivotPart)
            {
                string path = folder + "/" + prefix + name + ".png";
                if (!files.Contains(path)) return null;
                ConfigureSprite(path, pivotPart, rf, report);
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            var used = new HashSet<string>();
            string Use(string name) { used.Add(folder + "/" + prefix + name + ".png"); return name; }

            Directory.CreateDirectory(RigsFolder);
            var rig = AssetDatabase.LoadAssetAtPath<ArtistRig>(RigAssetPath(id));
            if (rig == null)
            {
                rig = ScriptableObject.CreateInstance<ArtistRig>();
                AssetDatabase.CreateAsset(rig, RigAssetPath(id));
            }
            rig.artistId = id;
            rig.placeholderArt = placeholder;
            rig.canvasSize = rf.canvas;
            rig.joints = rf.joints.Select(p => new RigJointPosition { joint = p.Key, position = p.Value }).ToArray();

            // body
            var body = new List<RigPartSprite>();
            foreach (var b in BodyFiles)
            {
                var s = Load(Use(b.Key), b.Value);
                if (s != null) body.Add(new RigPartSprite { part = b.Value, sprite = s });
            }
            foreach (var req in ArtistRig.RequiredBody)
                if (!body.Exists(x => x.part == req)) report.AppendLine($"MISSING body part {req} ({prefix}{BodyFiles.First(x => x.Value == req).Key}.png)");
            var heads = new Sprite[HeadFiles.Length];
            for (int i = 0; i < HeadFiles.Length; i++)
            {
                foreach (var suffix in HeadFiles[i])
                {
                    heads[i] = Load(Use("head_" + suffix), RigPart.Head);
                    if (heads[i] != null) break;
                }
                if (heads[i] == null) report.AppendLine($"MISSING head {prefix}head_{HeadFiles[i][0]}.png");
            }
            Use("head_hype"); // optional alternative for "excited"
            var hands = new Sprite[HandFiles.Length];
            for (int i = 0; i < HandFiles.Length; i++)
            {
                hands[i] = Load(Use(HandFiles[i]), RigPart.HandNear);
                if (hands[i] == null && i == 0) report.AppendLine($"MISSING {prefix}hand_mic.png (the near hand holding the mic)");
            }
            if (body.Find(x => x.part == RigPart.HandFar) == null && hands[1] != null)
                body.Add(new RigPartSprite { part = RigPart.HandFar, sprite = hands[1] }); // far hand falls back to the open hand
            rig.body = body.ToArray();
            rig.heads = heads.Where(h => h != null).Count() == heads.Length ? heads : FillHeads(heads);
            rig.nearHands = hands;

            // wardrobe: <id>_<slot>_<item>_<part>.png
            var items = new List<WardrobeItem>();
            string itemFolder = WardrobeFolder + "/" + id;
            Directory.CreateDirectory(itemFolder);
            foreach (var slotPair in ItemFiles)
            {
                string slotPrefix = prefix + SlotName(slotPair.Key) + "_";
                var itemIds = new HashSet<string>();
                foreach (var f in files)
                {
                    string name = Path.GetFileNameWithoutExtension(f);
                    if (!name.StartsWith(slotPrefix)) continue;
                    string rest = name.Substring(slotPrefix.Length);
                    foreach (var suffix in slotPair.Value.Keys.Concat(new[] { "thumb" }))
                        if (rest.EndsWith("_" + suffix)) { itemIds.Add(rest.Substring(0, rest.Length - suffix.Length - 1)); break; }
                }
                foreach (var itemId in itemIds)
                {
                    string fullId = SlotName(slotPair.Key) + "_" + itemId;
                    string assetPath = itemFolder + "/" + fullId + ".asset";
                    var item = AssetDatabase.LoadAssetAtPath<WardrobeItem>(assetPath);
                    if (item == null)
                    {
                        item = ScriptableObject.CreateInstance<WardrobeItem>();
                        AssetDatabase.CreateAsset(item, assetPath);
                    }
                    item.id = fullId;
                    item.slot = slotPair.Key;
                    item.nameKey = "wardrobe." + id + "." + fullId;
                    if (rf.items.TryGetValue(fullId, out var meta))
                    {
                        item.englishName = meta.name;
                        item.requiredStudioLevel = meta.studio;
                        item.price = meta.price;
                    }
                    else item.englishName = itemId.Replace('_', ' ').ToUpperInvariant();
                    var parts = new List<RigPartSprite>();
                    foreach (var pp in slotPair.Value)
                    {
                        var s = Load(Use(SlotName(slotPair.Key) + "_" + itemId + "_" + pp.Key), pp.Value);
                        if (s != null) parts.Add(new RigPartSprite { part = pp.Value, sprite = s });
                    }
                    item.parts = parts.ToArray();
                    foreach (var req in ArtistRig.RequiredParts(slotPair.Key))
                        if (!parts.Exists(x => x.part == req))
                            report.AppendLine($"MISSING {fullId}: {prefix}{SlotName(slotPair.Key)}_{itemId}_{slotPair.Value.First(x => x.Value == req).Key}.png");
                    item.thumbnail = Load(Use(SlotName(slotPair.Key) + "_" + itemId + "_thumb"), null) ?? (parts.Count > 0 ? parts[0].sprite : null);
                    EditorUtility.SetDirty(item);
                    items.Add(item);
                }
            }
            rig.wardrobe = items.ToArray();
            var defaults = new List<WardrobeItem>();
            foreach (var d in rf.defaults)
            {
                var item = items.Find(x => x.slot == d.Key && (x.id == d.Value || x.id == SlotName(d.Key) + "_" + d.Value));
                if (item != null) defaults.Add(item);
                else report.AppendLine($"ERROR default {d.Key} '{d.Value}' not found");
            }
            foreach (WardrobeSlot s in Enum.GetValues(typeof(WardrobeSlot)))
                if (!ArtistRig.OptionalSlot(s) && !defaults.Exists(x => x.slot == s)) report.AppendLine($"MISSING default item for {s}");
            rig.defaultOutfit = defaults.ToArray();
            EditorUtility.SetDirty(rig);

            foreach (var f in files)
                if (!used.Contains(f)) report.AppendLine("UNUSED file (name not recognised): " + Path.GetFileName(f));

            // link to the artist
            var artist = DefaultGameData.LoadArtists().FirstOrDefault(a => a.id == id);
            if (artist != null)
            {
                (artist.visual ?? (artist.visual = new ArtistVisualIdentity())).rig = rig;
                EditorUtility.SetDirty(artist);
            }
            else report.AppendLine("WARNING: no artist with id " + id);
            AssetDatabase.SaveAssets();
            report.AppendLine($"OK: {rig.body.Length} body parts, {heads.Count(h => h != null)}/{heads.Length} heads, {items.Count} wardrobe items, {rig.defaultOutfit.Length} defaults.");
            return report.ToString();
        }

        static Sprite[] FillHeads(Sprite[] heads)
        {
            var first = heads.FirstOrDefault(h => h != null);
            return heads.Select(h => h ?? first).ToArray();
        }

        /// <summary>Sprite settings for character parts, and the pivot on the part's joint.</summary>
        static void ConfigureSprite(string path, RigPart? part, RigFile rf, StringBuilder report)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return;
            imp.GetSourceTextureWidthAndHeight(out int w, out int h);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            if (part.HasValue)
            {
                var joint = rf.joints.TryGetValue(ArtistRig.PivotJoint(part.Value), out var j) ? j : Vector2.zero;
                var offset = rf.offsets.TryGetValue(Path.GetFileName(path), out var o) ? o : Vector2.zero;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2((joint.x - offset.x) / w, (joint.y - offset.y) / h);
                if (w >= rf.canvas.x && h >= rf.canvas.y && !rf.offsets.ContainsKey(Path.GetFileName(path)))
                    report.AppendLine("NOTE full-canvas export (works, but trimmed PNGs + 'offset' lines use much less memory): " + Path.GetFileName(path));
            }
            imp.SetTextureSettings(settings);
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.SaveAndReimport();
        }
    }
}
