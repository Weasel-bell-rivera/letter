using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LocalizationFontCoverageScanner
{
    [MenuItem("W1/Localization/Validate Formal Tables And Font")]
    public static void Validate()
    {
        Font font = Resources.Load<Font>(LocalizedFontProvider.FontResourcePath);
        if (font == null)
            throw new System.InvalidOperationException(
                $"Missing bundled font at Resources/{LocalizedFontProvider.FontResourcePath}.");

        HashSet<string> sourceKeys = null;
        foreach (string locale in LocalizationService.SupportedLocales)
        {
            TextAsset asset = Resources.Load<TextAsset>($"Localization/strings.{locale}");
            if (asset == null) throw new System.InvalidOperationException($"Missing table: {locale}");
            LocalizationTable table = JsonUtility.FromJson<LocalizationTable>(asset.text);
            HashSet<string> keys = new();
            foreach (LocalizationEntry entry in table.entries)
            {
                if (string.IsNullOrWhiteSpace(entry.key) || string.IsNullOrEmpty(entry.value) || !keys.Add(entry.key))
                    throw new System.InvalidOperationException($"Invalid or duplicate key in {locale}: {entry.key}");
                if (!LocalizedFontProvider.Covers(font, entry.value, out int missing))
                    throw new System.InvalidOperationException(
                        $"Font '{font.name}' lacks U+{missing:X4} used by '{entry.key}' ({locale}).");
            }
            if (sourceKeys == null) sourceKeys = keys;
            else if (!sourceKeys.SetEquals(keys))
                throw new System.InvalidOperationException($"Key coverage differs for locale {locale}.");
        }
        Debug.Log($"Localization validation passed for {sourceKeys?.Count ?? 0} keys and font '{font.name}'.");
    }
}
