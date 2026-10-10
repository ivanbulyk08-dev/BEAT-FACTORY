using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace BeatFactory.EditorTools
{
    /// <summary>
    /// Menu: Tools > Beat Factory > Build > ...
    /// Applies store-ready Android/iOS settings, generates the app icon and builds an APK/AAB.
    /// Change CompanyName / BundleId to your own before publishing.
    /// </summary>
    public static class MobileBuildSetup
    {
        public const string CompanyName = "Beat Factory Studio";
        public const string ProductName = "Beat Factory";
        public const string BundleId = "com.beatfactorystudio.beatfactory";
        public const string Version = "1.0.0";
        public const int BuildNumber = 1;
        const string IconPath = BeatFactorySceneBuilder.Root + "/Art/AppIcon.png";
        const string ScenePath = BeatFactorySceneBuilder.ScenePath;

        [MenuItem("Tools/Beat Factory/Build/Apply Mobile Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);

            // Portrait only
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Android: Google Play requires 64-bit (IL2CPP + ARM64) and App Bundles
            PlayerSettings.Android.bundleVersionCode = BuildNumber;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // lowest allowed by Unity 6.6
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);

            // iOS
            PlayerSettings.iOS.buildNumber = BuildNumber.ToString();
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Low);

            // Look & performance
            PlayerSettings.SplashScreen.backgroundColor = Theme.Background;
            PlayerSettings.runInBackground = false; // phones: pause in background (offline earnings handle it)
            PlayerSettings.stripEngineCode = true;

            // Only the game scene goes into the build
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            GenerateIcon();
            AssetDatabase.SaveAssets();
            Debug.Log("[Beat Factory] Mobile settings applied: " + BundleId + " v" + Version);
        }

        [MenuItem("Tools/Beat Factory/Build/Build Android APK (test)")]
        static void BuildApk() => BuildAndroid(appBundle: false);

        [MenuItem("Tools/Beat Factory/Build/Build Android AAB (Google Play)")]
        static void BuildAab() => BuildAndroid(appBundle: true);

        static void BuildAndroid(bool appBundle)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                EditorUtility.DisplayDialog("Android not installed",
                    "Install 'Android Build Support' (with OpenJDK and SDK & NDK) in Unity Hub > Installs > 6000.6.3f1 > Add modules.", "OK");
                return;
            }

            Apply();
            EditorUserBuildSettings.buildAppBundle = appBundle;
            Directory.CreateDirectory("Builds/Android");
            string path = "Builds/Android/BeatFactory_" + Version + (appBundle ? ".aab" : ".apk");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });
            Debug.Log("[Beat Factory] Android build: " + report.summary.result + " -> " + path +
                      " (" + report.summary.totalSize / (1024 * 1024) + " MB)");
        }

        /// <summary>Draws a 1024×1024 neon equalizer icon (placeholder until you have real art).</summary>
        static void GenerateIcon()
        {
            const int size = 1024;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            Color bgTop = new Color(0.16f, 0.07f, 0.30f), bgBottom = new Color(0.04f, 0.03f, 0.10f);
            Color pink = Theme.AccentPink, cyan = Theme.AccentCyan;

            int[] heights = { 300, 520, 700, 460, 620, 380 };
            int barW = 96, gap = 34, total = heights.Length * barW + (heights.Length - 1) * gap;
            int x0 = (size - total) / 2, baseY = 250;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Color c = Color.Lerp(bgBottom, bgTop, y / (float)size);

                // soft glow in the middle
                float dx = (x - size * 0.5f) / size, dy = (y - size * 0.45f) / size;
                c += pink * Mathf.Clamp01(0.35f - Mathf.Sqrt(dx * dx + dy * dy)) * 0.6f;

                // equalizer bars with a pink→cyan gradient
                int rel = x - x0;
                if (rel >= 0 && rel < total)
                {
                    int bar = rel / (barW + gap);
                    int inBar = rel % (barW + gap);
                    if (bar < heights.Length && inBar < barW && y >= baseY && y < baseY + heights[bar])
                    {
                        float t = (y - baseY) / 700f;
                        c = Color.Lerp(pink, cyan, t);
                    }
                }
                // floor line
                if (y >= baseY - 40 && y < baseY - 16 && x >= x0 && x < x0 + total) c = cyan;

                px[y * size + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(IconPath));
            File.WriteAllBytes(IconPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(IconPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }
    }
}
