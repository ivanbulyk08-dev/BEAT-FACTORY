using System.IO;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools
{
    /// <summary>
    /// Generates simple placeholder sprites (rounded rectangles, circles) as PNG files,
    /// so the game looks clean without any external art.
    /// </summary>
    public static class SpriteGenerator
    {
        /// <summary>Creates a white 9-sliced rounded rectangle PNG if it does not exist yet.</summary>
        public static void EnsureRoundedRect(string assetPath, int size, int radius) =>
            EnsureRoundedShape(assetPath, size, radius, 0);

        /// <summary>Rounded rectangle OUTLINE (frame) with the given line thickness, 9-sliced.</summary>
        public static void EnsureRoundedOutline(string assetPath, int size, int radius, int thickness) =>
            EnsureRoundedShape(assetPath, size, radius, thickness);

        static void EnsureRoundedShape(string assetPath, int size, int radius, int thickness)
        {
            if (File.Exists(assetPath)) return;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float r = radius;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Distance from the pixel center to the nearest corner circle center (0 inside the straight parts).
                float px = x + 0.5f, py = y + 0.5f;
                float cx = Mathf.Clamp(px, r, size - r);
                float cy = Mathf.Clamp(py, r, size - r);
                float d = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                float alpha = Mathf.Clamp01(r - d + 0.5f);
                if (thickness > 0)
                {
                    // Distance to the shape edge, measured inward; keep only a band of "thickness" pixels.
                    float edge = Mathf.Min(Mathf.Min(px, size - px), Mathf.Min(py, size - py));
                    float inside = (cx != px || cy != py) ? r - d : edge;
                    alpha *= Mathf.Clamp01(thickness - inside + 0.5f);
                }
                byte a = (byte)(alpha * 255);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(radius, radius, radius, radius);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
    }
}
