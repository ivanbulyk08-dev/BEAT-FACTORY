using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeatFactory.EditorTools
{
    /// <summary>
    /// Stops a RELEASE build (Development Build unchecked) that would ship with Google's test ads, a missing
    /// real ad unit or no privacy policy link — mistakes that cost revenue or get the app rejected.
    /// Development builds only get warnings, so test APKs keep working.
    /// Menu: Tools > Beat Factory > Release > Check Release Readiness.
    /// </summary>
    public class ReleaseBuildChecks : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            bool development = (report.summary.options & BuildOptions.Development) != 0;
            var problems = Collect(report.summary.platform);
            if (problems.Count == 0) return;

            string text = "Beat Factory release check:\n• " + string.Join("\n• ", problems);
            if (development) { Debug.LogWarning(text + "\n(Development build: allowed.)"); return; }
            throw new BuildFailedException(text + "\n\nFix these, or make a Development Build for testing.");
        }

        [MenuItem("Tools/Beat Factory/Release/Check Release Readiness")]
        static void Menu()
        {
            var problems = Collect(EditorUserBuildSettings.activeBuildTarget);
            string msg = problems.Count == 0 ? "Ready: real ad units, test mode off, privacy policy link set."
                                             : "• " + string.Join("\n• ", problems);
            Debug.Log("[Release check] " + msg);
            EditorUtility.DisplayDialog("Release readiness", msg, "OK");
        }

        public static List<string> Collect(BuildTarget target)
        {
            var problems = new List<string>();
            if (string.IsNullOrEmpty(AppLinks.PrivacyPolicyUrl))
                problems.Add("Privacy policy link is empty (AppLinks.PrivacyPolicyUrl). Stores require it for apps with ads.");

            bool ios = target == BuildTarget.iOS;
            if (!ReadAdsManager(ios, out bool testMode, out bool realUnits)) problems.Add("AdsManager not found in the game scene.");
            else
            {
                if (testMode) problems.Add("AdsManager Test Mode is ON (Google's test ads would be shown to real players).");
                if (!realUnits) problems.Add("Real " + (ios ? "iOS" : "Android") + " ad unit IDs are missing on the AdsManager (interstitial and rewarded).");
            }

            if (target == BuildTarget.Android)
            {
                if (!PlayerSettings.Android.useCustomKeystore) problems.Add("No release keystore (Player Settings > Publishing Settings). Google Play needs an upload key.");
                if (!EditorUserBuildSettings.buildAppBundle) problems.Add("Build App Bundle (AAB) is off. Google Play only accepts .aab files.");
            }
            return problems;
        }

        /// <summary>
        /// Reads the AdsManager settings straight from the saved scene file (works during a build, when scenes
        /// can't be opened). Save the scene after editing the AdsManager in the Inspector.
        /// </summary>
        static bool ReadAdsManager(bool ios, out bool testMode, out bool realUnits)
        {
            testMode = false;
            realUnits = false;
            string file = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), BeatFactorySceneBuilder.ScenePath);
            if (!System.IO.File.Exists(file)) return false;
            string yaml = System.IO.File.ReadAllText(file);
            int at = yaml.IndexOf("useTestAdUnits:", System.StringComparison.Ordinal);
            if (at < 0) return false;
            string block = yaml.Substring(at, System.Math.Min(2000, yaml.Length - at));
            testMode = Value(block, "useTestAdUnits") != "0";
            string inter = Value(block, ios ? "iosInterstitialId" : "androidInterstitialId");
            string rewarded = Value(block, ios ? "iosRewardedId" : "androidRewardedId");
            realUnits = inter.Length > 0 && rewarded.Length > 0;
            return true;
        }

        static string Value(string block, string key)
        {
            var m = System.Text.RegularExpressions.Regex.Match(block, @"(?m)^\s*" + key + @":[ \t]*(.*)$");
            return m.Success ? m.Groups[1].Value.Trim().Trim('\'', '"') : "";
        }
    }
}
