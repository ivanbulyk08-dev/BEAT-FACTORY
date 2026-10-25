using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BeatFactory.EditorTools
{
    /// <summary>
    /// Menu: Tools > Beat Factory > Build iOS (No Ads) — exports an Xcode project WITHOUT the Google Mobile Ads
    /// SDK, so it links with an older Xcode (no CocoaPods needed: open Unity-iPhone.xcodeproj directly).
    /// Ads use the placeholder overlay in that build. Project settings are restored afterwards.
    /// </summary>
    public static class IOSNoAdsBuild
    {
        public const string OutputPath = "Builds/iOS-NoAds";
        const string AdMobDefine = "BEATFACTORY_ADMOB";

        static readonly string[] NativeAdsPlugins =
        {
            "Assets/Plugins/iOS/unity-plugin-library.xcframework",
            "Assets/GoogleMobileAds/GoogleMobileAds.iOS.dll",
            "Assets/GoogleMobileAds/GoogleMobileAds.Ump.iOS.dll",
        };

        [MenuItem("Tools/Beat Factory/Build iOS (No Ads)")]
        public static void Build() => Debug.Log(BuildAndReport());

        public static string BuildAndReport()
        {
            var target = NamedBuildTarget.iOS;
            string oldDefines = PlayerSettings.GetScriptingDefineSymbols(target);
            var disabled = new System.Collections.Generic.List<PluginImporter>();
            try
            {
                PlayerSettings.SetScriptingDefineSymbols(target,
                    string.Join(";", oldDefines.Split(';').Where(d => d.Trim() != AdMobDefine)));

                foreach (var path in NativeAdsPlugins)
                {
                    if (AssetImporter.GetAtPath(path) is PluginImporter p && p.GetCompatibleWithPlatform(BuildTarget.iOS))
                    {
                        p.SetCompatibleWithPlatform(BuildTarget.iOS, false);
                        p.SaveAndReimport();
                        disabled.Add(p);
                    }
                }

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = OutputPath,
                    target = BuildTarget.iOS,
                    targetGroup = BuildTargetGroup.iOS,
                    options = BuildOptions.Development,
                });

                // no ads SDK → no pods: the project opens directly in Xcode
                string podfile = Path.Combine(OutputPath, "Podfile");
                if (File.Exists(podfile)) File.Delete(podfile);

                return "[iOS No Ads] " + report.summary.result + ", errors=" + report.summary.totalErrors +
                       " → " + Path.GetFullPath(OutputPath);
            }
            finally
            {
                PlayerSettings.SetScriptingDefineSymbols(target, oldDefines);
                foreach (var p in disabled)
                {
                    p.SetCompatibleWithPlatform(BuildTarget.iOS, true);
                    p.SaveAndReimport();
                }
            }
        }
    }
}
