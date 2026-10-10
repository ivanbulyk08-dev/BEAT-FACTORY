using System.Collections.Generic;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Translation tables: one text file per language in Resources/Localization/{code}.txt.
    /// Format (UTF-8):   key = value      # comments, blank lines ignored, "\n" = new line.
    /// Plural forms use suffixes: key.one / key.few / key.many / key.other.
    /// </summary>
    public static class LocalizationDatabase
    {
        public const string ResourcesFolder = "Localization/";

        public static Dictionary<string, string> Load(string code)
        {
            var asset = Resources.Load<TextAsset>(ResourcesFolder + code);
            var dict = asset != null ? Parse(asset.text) : new Dictionary<string, string>();
            if (asset == null) Debug.LogWarning("[Localization] No table for '" + code + "'");
            else Resources.UnloadAsset(asset); // keep only the parsed strings in memory
            return dict;
        }

        public static Dictionary<string, string> Parse(string text)
        {
            var dict = new Dictionary<string, string>(1024);
            int start = 0;
            if (text.Length > 0 && text[0] == '﻿') start = 1;
            while (start < text.Length)
            {
                int end = text.IndexOf('\n', start);
                if (end < 0) end = text.Length;
                ParseLine(text, start, end, dict);
                start = end + 1;
            }
            return dict;
        }

        static void ParseLine(string text, int start, int end, Dictionary<string, string> dict)
        {
            string line = text.Substring(start, end - start).Trim();
            if (line.Length == 0 || line[0] == '#') return;
            int eq = line.IndexOf('=');
            if (eq <= 0) return;
            string key = line.Substring(0, eq).Trim();
            string value = line.Substring(eq + 1).Trim().Replace("\\n", "\n");
            dict[key] = value;
        }
    }
}
