using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class LocalizationService
{
    public const string English = "en";
    public const string SimplifiedChinese = "zh-Hans";
    public const string SourceLocale = English;
    private const string PreferenceKey = "w1.locale";
    private const string ResourcePrefix = "Localization/strings.";

    private static readonly Dictionary<string, Dictionary<string, string>> tables = new();
    private static string currentLocale;

    public static event Action LocaleChanged;
    public static string CurrentLocale => currentLocale ??= ResolveInitialLocale();
    public static IReadOnlyList<string> SupportedLocales { get; } =
        new[] { English, SimplifiedChinese };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => _ = CurrentLocale;

    public static bool IsSupported(string locale) =>
        string.Equals(locale, English, StringComparison.Ordinal) ||
        string.Equals(locale, SimplifiedChinese, StringComparison.Ordinal);

    public static bool SetLocale(string locale, bool persist = true)
    {
        string normalized = Normalize(locale);
        if (!IsSupported(normalized)) return false;
        if (persist)
        {
            PlayerPrefs.SetString(PreferenceKey, normalized);
            PlayerPrefs.Save();
        }
        if (string.Equals(CurrentLocale, normalized, StringComparison.Ordinal)) return true;
        currentLocale = normalized;
        LocaleChanged?.Invoke();
        return true;
    }

    public static string Get(string key) => Get(key, CurrentLocale);

    public static string Get(string key, string locale)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        string normalized = IsSupported(Normalize(locale)) ? Normalize(locale) : SourceLocale;
        if (TryGet(normalized, key, out string localized)) return localized;
        if (!string.Equals(normalized, SourceLocale, StringComparison.Ordinal) &&
            TryGet(SourceLocale, key, out string source))
        {
            Debug.LogWarning($"Missing localization key '{key}' for locale '{normalized}'; using '{SourceLocale}'.");
            return source;
        }
        Debug.LogWarning($"Missing localization key '{key}' in locale '{normalized}' and source locale; using key.");
        return key;
    }

    public static string Format(string key, params object[] arguments)
    {
        CultureInfo culture = string.Equals(CurrentLocale, SimplifiedChinese, StringComparison.Ordinal)
            ? CultureInfo.GetCultureInfo("zh-CN")
            : CultureInfo.GetCultureInfo("en-US");
        return string.Format(culture, Get(key), arguments);
    }

    private static bool TryGet(string locale, string key, out string value)
    {
        Dictionary<string, string> table = LoadTable(locale);
        return table.TryGetValue(key, out value) && !string.IsNullOrEmpty(value);
    }

    private static Dictionary<string, string> LoadTable(string locale)
    {
        if (tables.TryGetValue(locale, out Dictionary<string, string> cached)) return cached;
        Dictionary<string, string> table = new(StringComparer.Ordinal);
        TextAsset asset = Resources.Load<TextAsset>(ResourcePrefix + locale);
        if (asset == null)
        {
            Debug.LogError($"Missing localization table at Resources/{ResourcePrefix}{locale}.json.");
        }
        else
        {
            LocalizationTable parsed = JsonUtility.FromJson<LocalizationTable>(asset.text);
            if (parsed?.entries != null)
            {
                foreach (LocalizationEntry entry in parsed.entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.key) || table.ContainsKey(entry.key))
                        continue;
                    table.Add(entry.key, entry.value ?? string.Empty);
                }
            }
        }
        tables[locale] = table;
        return table;
    }

    private static string ResolveInitialLocale()
    {
        string saved = Normalize(PlayerPrefs.GetString(PreferenceKey, string.Empty));
        if (IsSupported(saved)) return saved;
        return Application.systemLanguage == SystemLanguage.ChineseSimplified
            ? SimplifiedChinese
            : English;
    }

    private static string Normalize(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale)) return string.Empty;
        string value = locale.Trim().Replace('_', '-');
        if (value.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase)) return SimplifiedChinese;
        if (value.Equals("en", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("en-", StringComparison.OrdinalIgnoreCase)) return English;
        return value;
    }

#if UNITY_INCLUDE_TESTS
    public static void ResetForTests()
    {
        currentLocale = null;
        tables.Clear();
    }
#endif
}

[Serializable]
public sealed class LocalizationTable
{
    public LocalizationEntry[] entries;
}

[Serializable]
public sealed class LocalizationEntry
{
    public string key;
    public string value;
}
