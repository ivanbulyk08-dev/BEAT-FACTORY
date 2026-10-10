using BeatFactory.EditorTools.Art;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools
{
    /// <summary>Concerts &amp; tours UI: the screens build themselves from the UISkin at runtime; here we create the roots and assets.</summary>
    public static partial class BeatFactorySceneBuilder
    {
        public const string UISkinPath = ConfigFolder + "/UISkin.asset";

        static UISkin EnsureUISkin()
        {
            var skin = AssetDatabase.LoadAssetAtPath<UISkin>(UISkinPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<UISkin>();
                AssetDatabase.CreateAsset(skin, UISkinPath);
            }
            skin.display = UIStyle.Display;
            skin.heading = UIStyle.Heading;
            skin.body = UIStyle.Body;
            skin.rounded = UIFactory.RoundedSprite;
            skin.circle = UIFactory.CircleSprite;
            skin.outline = UIFactory.OutlineSprite;
            skin.panel = UIStyle.PanelSprite;
            skin.button = UIStyle.ButtonSprite;
            skin.softGlow = UIStyle.SoftGlowSprite;
            skin.radial = UIStyle.RadialSprite;
            skin.ring = UIStyle.RingSprite;
            skin.barGradient = UIStyle.BarGradientSprite;
            skin.confetti = LoadSprite(ArtGenerator.Fx + "/fx_confetti.png");
            skin.background = Theme.Background;
            skin.card = Theme.Panel;
            skin.cardLight = Theme.PanelLight;
            skin.text = Theme.TextMain;
            skin.textDim = Theme.TextDim;
            skin.money = Theme.AccentMoney;
            skin.pink = Theme.AccentPink;
            skin.cyan = Theme.AccentCyan;
            skin.fame = Theme.AccentFame;
            skin.gold = Gold;
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            return skin;
        }

        static void CreateConcertUI(RectTransform layer)
        {
            var skin = EnsureUISkin();
            int n = ArtGenerator.VenueScreens.Length;
            var venues = new Sprite[n];
            var crowds = new Sprite[n];
            var rects = new Vector4[n];
            for (int i = 0; i < n; i++)
            {
                venues[i] = LoadSprite(ArtGenerator.VenuePath(i));
                crowds[i] = LoadSprite(ArtGenerator.CrowdPath(i));
                var r = ArtGenerator.VenueScreens[i];
                rects[i] = new Vector4(r.x, r.y, r.width, r.height);
            }

            RectTransform Root(string name)
            {
                var rt = UIFactory.Rect(name, layer);
                UIFactory.Stretch(rt);
                return rt;
            }

            // order = draw order: career page < concert show < tour results
            var goalsRoot = Root("GoalsScreen");
            var careerRoot = Root("ArtistCareerScreen");
            var showRoot = Root("ConcertShow");
            var tourRoot = Root("TourComplete");

            var show = showRoot.gameObject.AddComponent<ConcertShowUI>();
            var so = new SerializedObject(show);
            so.FindProperty("skin").objectReferenceValue = skin;
            UIFactory.SetArray(so.FindProperty("venueSprites"), venues);
            UIFactory.SetArray(so.FindProperty("crowdSprites"), crowds);
            var rp = so.FindProperty("screenRects");
            rp.arraySize = n;
            for (int i = 0; i < n; i++) rp.GetArrayElementAtIndex(i).vector4Value = rects[i];
            so.FindProperty("beamSprite").objectReferenceValue = LoadSprite(ArtGenerator.BeamPath);
            so.FindProperty("glowSprite").objectReferenceValue = UIStyle.RadialSprite;
            so.FindProperty("crowdAtlas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtGenerator.CrowdAtlasPath);
            so.FindProperty("micSprite").objectReferenceValue = LoadSprite(ArtGenerator.MicPath);
            so.FindProperty("laserCoreSprite").objectReferenceValue = LoadSprite(ArtGenerator.LaserCorePath);
            so.FindProperty("laserGlowSprite").objectReferenceValue = LoadSprite(ArtGenerator.LaserGlowPath);
            so.FindProperty("flameSprite").objectReferenceValue = LoadSprite(ArtGenerator.FlamePath);
            so.FindProperty("sparkSprite").objectReferenceValue = LoadSprite(ArtGenerator.SparkPath);
            so.ApplyModifiedPropertiesWithoutUndo();

            var career = careerRoot.gameObject.AddComponent<ArtistCareerScreen>();
            UIFactory.SetRef(career, "skin", skin);
            UIFactory.SetRef(career, "show", show);

            var goals = goalsRoot.gameObject.AddComponent<GoalsScreen>();
            UIFactory.SetRef(goals, "skin", skin);

            var languageRoot = Root("LanguagePicker");
            var picker = languageRoot.gameObject.AddComponent<LanguagePicker>();
            UIFactory.SetRef(picker, "skin", skin);

            var tour = tourRoot.gameObject.AddComponent<TourCompletePopup>();
            UIFactory.SetRef(tour, "skin", skin);
            UIFactory.SetRef(tour, "show", show);
        }
    }
}
