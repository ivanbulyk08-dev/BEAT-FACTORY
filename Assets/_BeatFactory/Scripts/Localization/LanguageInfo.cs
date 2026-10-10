using System;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>Supported languages. Saved by code (LanguageInfo.code), never by enum index.</summary>
    public enum Language { EN, ES, PT_BR, ZH_CN, RU, KO, FR }

    /// <summary>How a language picks plural forms (CLDR rules, integer part).</summary>
    public enum PluralRule
    {
        OneOther,      // en, es: 1 → one
        ZeroOneOther,  // fr, pt-BR: 0 and 1 → one
        Slavic,        // ru: one / few / many
        None,          // zh, ko: always other
    }

    /// <summary>Everything the game needs to know about a language. Add a new language here + a table file.</summary>
    public class LanguageInfo
    {
        public Language id;
        public string code;          // table file name + save id: "en", "pt-BR" ...
        public string nativeName;    // shown in the language picker
        public bool rightToLeft;     // future RTL languages (Arabic, Hebrew): LocalizedText flips the text direction
        public PluralRule plural;
        public string groupSeparator;
        public string decimalSeparator;

        public static readonly LanguageInfo[] All =
        {
            new LanguageInfo { id = Language.EN, code = "en", nativeName = "English", plural = PluralRule.OneOther, groupSeparator = ",", decimalSeparator = "." },
            new LanguageInfo { id = Language.ES, code = "es", nativeName = "Español", plural = PluralRule.OneOther, groupSeparator = ".", decimalSeparator = "," },
            new LanguageInfo { id = Language.PT_BR, code = "pt-BR", nativeName = "Português (Brasil)", plural = PluralRule.ZeroOneOther, groupSeparator = ".", decimalSeparator = "," },
            new LanguageInfo { id = Language.ZH_CN, code = "zh-CN", nativeName = "简体中文", plural = PluralRule.None, groupSeparator = ",", decimalSeparator = "." },
            new LanguageInfo { id = Language.RU, code = "ru", nativeName = "Русский", plural = PluralRule.Slavic, groupSeparator = " ", decimalSeparator = "," },
            new LanguageInfo { id = Language.KO, code = "ko", nativeName = "한국어", plural = PluralRule.None, groupSeparator = ",", decimalSeparator = "." },
            new LanguageInfo { id = Language.FR, code = "fr", nativeName = "Français", plural = PluralRule.ZeroOneOther, groupSeparator = " ", decimalSeparator = "," },
        };

        public static LanguageInfo Get(Language l) => All[(int)l];

        public static LanguageInfo FromCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            foreach (var l in All) if (string.Equals(l.code, code, StringComparison.OrdinalIgnoreCase)) return l;
            return null;
        }

        /// <summary>Device language → supported language (English when unsupported).</summary>
        public static Language FromSystem(SystemLanguage s)
        {
            switch (s)
            {
                case SystemLanguage.Spanish: return Language.ES;
                case SystemLanguage.Portuguese: return Language.PT_BR;
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified: return Language.ZH_CN;
                case SystemLanguage.Russian: return Language.RU;
                case SystemLanguage.Korean: return Language.KO;
                case SystemLanguage.French: return Language.FR;
                default: return Language.EN;
            }
        }

        /// <summary>CLDR plural category for a count: "one", "few", "many" or "other".</summary>
        public string PluralCategory(double n)
        {
            n = Math.Abs(n);
            bool integer = Math.Floor(n) == n && n < 1e9;
            long i = (long)Math.Floor(Math.Min(n, 1e15));
            switch (plural)
            {
                case PluralRule.OneOther: return integer && i == 1 ? "one" : "other";
                case PluralRule.ZeroOneOther: return i <= 1 ? "one" : "other";
                case PluralRule.Slavic:
                    // numbers from 10,000 are shown short ("1,5M фанатов"), which reads as "many"
                    if (n >= 1e4) return "many";
                    if (!integer) return "other";
                    long m10 = i % 10, m100 = i % 100;
                    if (m10 == 1 && m100 != 11) return "one";
                    if (m10 >= 2 && m10 <= 4 && (m100 < 12 || m100 > 14)) return "few";
                    return "many";
                default: return "other";
            }
        }
    }
}
