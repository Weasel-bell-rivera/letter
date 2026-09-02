using UnityEngine;

public static class LocalizedFontProvider
{
    public const string FontResourcePath = "Localization/W1UIFont";
    private static Font cached;
    private static bool attempted;

    public static Font GetFont()
    {
        if (!attempted)
        {
            attempted = true;
            cached = Resources.Load<Font>(FontResourcePath);
            if (cached == null)
                Debug.LogError($"Bundled localization font is missing at Resources/{FontResourcePath}. " +
                    "Text must not fall back to an OS font; add a redistribution-safe font with Latin and Simplified Chinese coverage.");
        }
        return cached;
    }

    public static bool Covers(Font font, string text, out int missingCodePoint)
    {
        missingCodePoint = -1;
        if (font == null) return false;
        if (string.IsNullOrEmpty(text)) return true;
        foreach (char character in text)
        {
            if (char.IsSurrogate(character) || char.IsWhiteSpace(character) || font.HasCharacter(character)) continue;
            missingCodePoint = character;
            return false;
        }
        return true;
    }
}
