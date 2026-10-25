using System.IO;
using BeatFactory.EditorTools.Art;
using UnityEngine;
using static BeatFactory.EditorTools.Art.SdfPainter;

namespace BeatFactory.EditorTools
{
    /// <summary>
    /// STORE tab (bottom navigation): clothes for NØX bought with money and worn on stage (StoreScreenUI + StoreCatalog).
    /// </summary>
    public static partial class BeatFactorySceneBuilder
    {
        const string StoreIconName = "icon_store";

        static RectTransform CreateStoreScreen(RectTransform content)
        {
            EnsureStoreIcon();
            var screen = CreateScreen("StoreScreen", content);
            CreateScreenTitle(screen, "@nav.store");

            // clothes grid (built at runtime from the catalog)
            var area = UIFactory.Rect("StoreArea", screen);
            UIFactory.Stretch(area);
            area.offsetMax = new Vector2(0, -ScreenListTop);
            var ui = screen.gameObject.AddComponent<StoreScreenUI>();
            UIFactory.SetRef(ui, "catalog", Art.StoreItemArt.Ensure());
            UIFactory.SetRef(ui, "skin", EnsureUISkin());
            UIFactory.SetRef(ui, "listArea", area);
            return screen;
        }

        /// <summary>Shopping-bag icon in the style of the other nav icons (white, tinted by the nav).</summary>
        static void EnsureStoreIcon()
        {
            string path = ArtGenerator.Icons + "/" + StoreIconName + ".png";
            if (File.Exists(path)) return;
            var p = new SdfPainter(128, 128);
            p.Fill(RRect(22, 10, 84, 82, 14), Color.white);                          // bag
            p.Fill(Ring(new Vector2(64, 92), 20, 7), Color.white, new RectInt(0, 92, 128, 36)); // handle (top half)
            p.Fill(Circle(new Vector2(46, 70), 5), new Color(0, 0, 0, 0));
            p.Save(path);
        }
    }
}
