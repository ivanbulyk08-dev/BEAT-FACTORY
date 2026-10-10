using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Localization facade: every player-facing text is requested by key.
    ///   Loc.T("ui.settings.language")                       → "LANGUAGE" / "ЯЗЫК" ...
    ///   Loc.F("reward.earned", "amount", "$5K")              → "You earned $5K!"  (named {placeholders})
    ///   Loc.P("count.fans", 3)                               → "3 fans" / "3 фаната" (plural forms key.one/few/many/other)
    /// Only the selected language and English (fallback) are kept in memory. Missing keys fall back to English and are
    /// logged in development builds; raw keys are never shown to players.
    /// </summary>
    public static class Loc
    {
        const string PrefKey = "bf.language";

        static LanguageInfo info;
        static Dictionary<string, string> table, english;
        static readonly HashSet<string> reported = new HashSet<string>();
        static NumberFormatInfo numberFormat;

        /// <summary>Fired after the language changes (LocalizedText and open screens refresh themselves).</summary>
        public static event Action Changed;

        public static LanguageInfo Info { get { Ensure(); return info; } }
        public static Language Current => Info.id;
        public static NumberFormatInfo Numbers { get { Ensure(); return numberFormat; } }

        static void Ensure()
        {
            if (info != null) return;
            string saved = PlayerPrefs.GetString(PrefKey, "");
            var l = LanguageInfo.FromCode(saved);
            Apply(l != null ? l.id : LanguageInfo.FromSystem(Application.systemLanguage), false);
        }

        /// <summary>Saved language code ("" before the first launch finished detecting).</summary>
        public static string SavedCode => PlayerPrefs.GetString(PrefKey, "");

        public static void SetLanguage(Language l)
        {
            Ensure();
            bool changed = info.id != l;
            Apply(l, true);
            if (changed) Changed?.Invoke();
        }

        /// <summary>Applies a language code from a save file (ignored when unknown).</summary>
        public static void SetLanguage(string code)
        {
            var l = LanguageInfo.FromCode(code);
            if (l != null) SetLanguage(l.id);
        }

        static void Apply(Language l, bool save)
        {
            info = LanguageInfo.Get(l);
            if (english == null) english = LocalizationDatabase.Load("en");
            table = l == Language.EN ? english : LocalizationDatabase.Load(info.code);
            numberFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            numberFormat.NumberGroupSeparator = info.groupSeparator;
            numberFormat.NumberDecimalSeparator = info.decimalSeparator;
            if (save)
            {
                PlayerPrefs.SetString(PrefKey, info.code);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Re-reads the table files (editor tools after editing translations).</summary>
        public static void Reload()
        {
            var l = info != null ? info.id : Language.EN;
            english = null;
            info = null;
            Apply(l, false);
            Changed?.Invoke();
        }

        // ------------------------------------------------------------------ lookup

        public static bool Has(string key)
        {
            Ensure();
            return key != null && (table.ContainsKey(key) || english.ContainsKey(key));
        }

        /// <summary>Translated text for a key.</summary>
        public static string T(string key)
        {
            Ensure();
            if (string.IsNullOrEmpty(key)) return "";
            if (table.TryGetValue(key, out var v)) return v;
            Report(key);
            if (english.TryGetValue(key, out v)) return v;
            return Humanize(key);
        }

        /// <summary>Translated text, or the given fallback when no key exists (e.g. data names that stay untranslated).</summary>
        public static string Or(string key, string fallback)
        {
            Ensure();
            if (key != null && table.TryGetValue(key, out var v)) return v;
            if (key != null && english.TryGetValue(key, out v)) return v;
            return fallback;
        }

        /// <summary>Translated text with named placeholders: F("k", "amount", "$5K", "name", "VEXXO").</summary>
        public static string F(string key, params object[] args) => Fill(T(key), args);

        /// <summary>
        /// Plural-aware text. Picks key.one / key.few / key.many / key.other for the language's plural rule;
        /// {n} is the formatted count, extra named args work like F().
        /// </summary>
        public static string P(string key, double count, params object[] args)
        {
            Ensure();
            string cat = info.PluralCategory(count);
            string k = key + "." + cat;
            string text;
            if (!table.TryGetValue(k, out text) && !table.TryGetValue(key + ".other", out text))
            {
                Report(k);
                if (!english.TryGetValue(key + "." + LanguageInfo.Get(Language.EN).PluralCategory(count), out text) &&
                    !english.TryGetValue(key + ".other", out text))
                    text = "{n}";
            }
            text = text.Replace("{n}", NumberFormatter.Format(count));
            return Fill(text, args);
        }

        static string Fill(string text, object[] args)
        {
            if (args == null || args.Length < 2 || text.IndexOf('{') < 0) return text;
            var sb = new StringBuilder(text);
            for (int i = 0; i + 1 < args.Length; i += 2)
                sb.Replace("{" + args[i] + "}", Convert.ToString(args[i + 1], CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        static void Report(string key)
        {
            if (!(Debug.isDebugBuild || Application.isEditor)) return;
            if (!reported.Add(info.code + "|" + key)) return;
            Debug.LogWarning("[MISSING LOCALIZATION] key = " + key + "  (" + info.code + ")");
        }

        /// <summary>Last resort for a key missing everywhere: "concert.sold_out" → "Sold out" (never the raw key).</summary>
        static string Humanize(string key)
        {
            int dot = key.LastIndexOf('.');
            string s = (dot >= 0 ? key.Substring(dot + 1) : key).Replace('_', ' ');
            return s.Length > 0 ? char.ToUpperInvariant(s[0]) + s.Substring(1) : s;
        }

        /// <summary>Upper case for the current language (titles, buttons).</summary>
        public static string Upper(string s) => string.IsNullOrEmpty(s) ? s : s.ToUpperInvariant();
    }
}
