using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// The producer who sits at the desk in studio rooms 3–10: the user's two green-screen pictures
    /// (Art/Reference/desk_producer_left.png = working at a desk on his left, _right = on his right) are chroma-keyed
    /// (soft alpha from the greenness, green spill removed), trimmed and saved to Art/Characters/desk_producer.
    /// <see cref="SeatPivot"/> is where he sits on the chair (normalized, bottom-left origin of the trimmed sprite).
    /// </summary>
    public static class DeskProducerArt
    {
        const string Ref = BeatFactorySceneBuilder.Root + "/Art/Reference/desk_producer_";
        public const string OutDir = BeatFactorySceneBuilder.Root + "/Art/Characters/desk_producer";

        /// <summary>Seat point in the source pictures (px, top-left origin).</summary>
        static readonly Vector2 SeatLeft = new Vector2(700, 930), SeatRight = new Vector2(330, 930);

        public static string SpritePath(bool left) => OutDir + "/" + (left ? "left" : "right") + ".png";

        public struct Result { public Sprite sprite; public Vector2 pivot; }

        public static Result Get(bool left)
        {
            string path = SpritePath(left);
            string pivotPath = path + ".pivot.txt";
            if (!File.Exists(path) || !File.Exists(pivotPath)) Cut(left);
            var parts = File.Exists(pivotPath) ? File.ReadAllText(pivotPath).Split(',') : new[] { "0.5", "0.4" };
            return new Result
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path),
                pivot = new Vector2(float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                                    float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture)),
            };
        }

        static void Cut(bool left)
        {
            string src = Ref + (left ? "left" : "right") + ".png";
            if (!File.Exists(src)) return;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(src));
            int w = tex.width, h = tex.height;
            var px = tex.GetPixels32(); // bottom-up rows
            Object.DestroyImmediate(tex);
            int x0 = w, y0 = h, x1 = -1, y1 = -1;
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                int other = Mathf.Max(c.r, c.b);
                float a = Mathf.Clamp01((110f - (c.g - other)) / 80f);
                byte g = a < 1f ? (byte)Mathf.Min(c.g, other + 10) : c.g;
                px[i] = new Color32(c.r, g, c.b, (byte)(a * 255));
                if (a < 0.1f) continue;
                int x = i % w, y = i / w;
                x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
            }
            int cw = x1 - x0 + 1, ch = y1 - y0 + 1;
            var o = new Color32[cw * ch];
            for (int y = 0; y < ch; y++) System.Array.Copy(px, (y0 + y) * w + x0, o, y * cw, cw);
            var t = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
            t.SetPixels32(o);
            t.Apply();
            Directory.CreateDirectory(OutDir);
            string path = SpritePath(left);
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            // seat: source top-left px → trimmed sprite, normalized, bottom-left origin
            var seat = left ? SeatLeft : SeatRight;
            float sy = (h - 1 - seat.y);
            var pivot = new Vector2((seat.x - x0) / cw, (sy - y0) / ch);
            File.WriteAllText(path + ".pivot.txt", pivot.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + pivot.y.ToString(System.Globalization.CultureInfo.InvariantCulture));
            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.maxTextureSize = 1024;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.SaveAndReimport();
            }
        }

        [MenuItem("Beat Factory/Art/Cut Desk Producer")]
        static void CutMenu()
        {
            foreach (var f in new[] { SpritePath(true), SpritePath(false) })
            {
                if (File.Exists(f + ".pivot.txt")) File.Delete(f + ".pivot.txt");
            }
            Cut(true);
            Cut(false);
        }
    }
}
