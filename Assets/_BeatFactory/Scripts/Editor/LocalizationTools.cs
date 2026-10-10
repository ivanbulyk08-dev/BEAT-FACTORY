using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BeatFactory.EditorTools
{
    /// <summary>
    /// Localization tools:
    ///  • Build Font Fallbacks — static SDF atlases (only the characters the translations use) for Cyrillic,
    ///    Chinese Simplified and Korean, added as fallbacks to every game font. The big source fonts stay in the
    ///    project but are not shipped (static atlases don't reference them at runtime).
    ///  • Validate Tables — missing keys, plural forms and placeholder mismatches for every language.
    ///  • Language menu / Next Language — the Localization Test Mode from the editor (works in play mode too).
    /// </summary>
    public static class LocalizationTools
    {
        const string TablesFolder = BeatFactorySceneBuilder.Root + "/Resources/Localization";
        const string SourceFonts = BeatFactorySceneBuilder.Root + "/Fonts/Source";
        const string OutFolder = BeatFactorySceneBuilder.Root + "/Fonts/Localization";

        static readonly string[] GameFonts =
        {
            BeatFactorySceneBuilder.Root + "/Fonts/Anton SDF.asset",
            BeatFactorySceneBuilder.Root + "/Fonts/Oswald SDF.asset",
            BeatFactorySceneBuilder.Root + "/Fonts/Roboto SDF.asset",
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset",
        };

        /// <summary>Symbols the UI uses outside the tables (bullets, arrows, check marks …).</summary>
        const string Symbols = " ·×→●○►✓♥∞…—–“”‘’«»№%$€¡¿";

        // ------------------------------------------------------------------ fonts

        [MenuItem("Tools/Beat Factory/Localization/Build Font Fallbacks")]
        public static void BuildFontFallbacks()
        {
            if (!AssetDatabase.IsValidFolder(OutFolder)) AssetDatabase.CreateFolder(BeatFactorySceneBuilder.Root + "/Fonts", "Localization");

            string cyr = Unique(TableText("ru") + Symbols + Range(0x0410, 0x044F) + "Ёё", c => c >= 0x0400 && c <= 0x04FF || Symbols.IndexOf(c) >= 0);
            string zh = Unique(TableText("zh-CN") + Symbols + "。、，：；！？（）《》【】「」", c => c > 127);
            string ko = Unique(TableText("ko") + Symbols, c => c > 127);

            var report = new StringBuilder();
            var fallbacks = new List<TMP_FontAsset>
            {
                BuildStatic(SourceFonts + "/Oswald-Bold-Cyrillic.ttf", OutFolder + "/BF Cyrillic SDF.asset", cyr, 56, 6, 1024, report),
                BuildStatic(SourceFonts + "/NotoSansSC-Bold.otf", OutFolder + "/BF Chinese SDF.asset", zh, 44, 5, 2048, report),
                BuildStatic(SourceFonts + "/NotoSansKR-Bold.otf", OutFolder + "/BF Korean SDF.asset", ko, 44, 5, 2048, report),
            };
            fallbacks.RemoveAll(f => f == null);

            foreach (var path in GameFonts)
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font == null) continue;
                var list = font.fallbackFontAssetTable ?? new List<TMP_FontAsset>();
                list.RemoveAll(f => f == null || f.name.StartsWith("BF "));
                list.InsertRange(0, fallbacks);
                font.fallbackFontAssetTable = list;
                EditorUtility.SetDirty(font);
                report.AppendLine("Fallbacks → " + font.name);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Localization] Font fallbacks built\n" + report);
        }

        static TMP_FontAsset BuildStatic(string fontPath, string assetPath, string chars, int size, int padding, int atlas, StringBuilder report)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (source == null) { report.AppendLine("MISSING source font " + fontPath); return null; }

            var fa = TMP_FontAsset.CreateFontAsset(source, size, padding, GlyphRenderMode.SDFAA, atlas, atlas, AtlasPopulationMode.Dynamic, true);
            fa.name = Path.GetFileNameWithoutExtension(assetPath);
            fa.TryAddCharacters(chars, out string missing);

            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(fa, assetPath);
            for (int i = 0; i < fa.atlasTextures.Length; i++)
            {
                var tex = fa.atlasTextures[i];
                if (tex == null) continue;
                tex.name = fa.name + " Atlas " + i;
                AssetDatabase.AddObjectToAsset(tex, fa);
            }
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            fa.atlasPopulationMode = AtlasPopulationMode.Static; // no source font at runtime
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();

            report.AppendLine(fa.name + ": " + fa.characterTable.Count + " characters, " + fa.atlasTextures.Length + " atlas(es)" +
                              (string.IsNullOrEmpty(missing) ? "" : ", not in font: " + missing));
            return fa;
        }

        static string TableText(string code)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(TablesFolder + "/" + code + ".txt");
            return asset != null ? asset.text + asset.text.ToUpperInvariant() : "";
        }

        static string Range(int from, int to)
        {
            var sb = new StringBuilder();
            for (int c = from; c <= to; c++) sb.Append((char)c);
            return sb.ToString();
        }

        static string Unique(string s, System.Func<char, bool> keep) =>
            new string(s.Where(keep).Distinct().OrderBy(c => c).ToArray());

        // ------------------------------------------------------------------ validation

        [MenuItem("Tools/Beat Factory/Localization/Validate Tables")]
        public static void Validate()
        {
            var en = Load("en");
            var pluralBases = new HashSet<string>(en.Keys.Where(IsPlural).Select(Base));
            var report = new StringBuilder();
            int problems = 0;
            foreach (var info in LanguageInfo.All)
            {
                if (info.id == Language.EN) continue;
                var t = Load(info.code);
                var lines = new List<string>();
                foreach (var k in en.Keys)
                {
                    if (IsPlural(k))
                    {
                        if (!t.ContainsKey(Base(k) + ".other")) lines.Add("missing plural " + Base(k) + ".other");
                        continue;
                    }
                    if (!t.TryGetValue(k, out var v)) { lines.Add("missing " + k); continue; }
                    if (Placeholders(en[k]) != Placeholders(v)) lines.Add("placeholders " + k + ": " + Placeholders(en[k]) + " vs " + Placeholders(v));
                }
                foreach (var k in t.Keys)
                    if (IsPlural(k) && pluralBases.Contains(Base(k)) && en.TryGetValue(Base(k) + ".other", out var ev) && Placeholders(ev) != Placeholders(t[k]))
                        lines.Add("placeholders " + k);
                if (info.plural == PluralRule.Slavic)
                    foreach (var b in pluralBases)
                        foreach (var form in new[] { ".one", ".few", ".many" })
                            if (!t.ContainsKey(b + form) && !b.StartsWith("challenge.d_money") && !b.StartsWith("challenge.w_money")) lines.Add("missing ru form " + b + form);
                problems += lines.Distinct().Count();
                report.AppendLine(info.code + ": " + (lines.Count == 0 ? "OK (" + t.Count + " entries)" : string.Join("\n   ", lines.Distinct())));
            }
            if (problems == 0) Debug.Log("[Localization] All tables valid\n" + report);
            else Debug.LogWarning("[Localization] " + problems + " problem(s)\n" + report);
        }

        static Dictionary<string, string> Load(string code)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(TablesFolder + "/" + code + ".txt");
            return asset != null ? LocalizationDatabase.Parse(asset.text) : new Dictionary<string, string>();
        }

        static bool IsPlural(string k) => Regex.IsMatch(k, @"\.(one|few|many|other)$");
        static string Base(string k) => Regex.Replace(k, @"\.(one|few|many|other)$", "");
        static string Placeholders(string v) =>
            string.Join(",", Regex.Matches(v, @"\{[a-z0-9]+\}").Cast<Match>().Select(m => m.Value).Distinct().OrderBy(s => s));

        // ------------------------------------------------------------------ Localization Test Mode (editor)

        [MenuItem("Tools/Beat Factory/Localization/Next Language (Test Mode) _F9")]
        static void NextLanguage()
        {
            if (Application.isPlaying) LanguagePicker.NextLanguage();
            else
            {
                Loc.SetLanguage((Language)(((int)Loc.Current + 1) % LanguageInfo.All.Length));
                Debug.Log("[Localization Test Mode] " + Loc.Info.code);
            }
        }

        [MenuItem("Tools/Beat Factory/Localization/Reload Tables")]
        static void Reload()
        {
            AssetDatabase.Refresh();
            UIFactory.ResetEnglishCache();
            Loc.Reload();
        }
    }
}
