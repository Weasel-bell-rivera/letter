using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class LocalizationServiceTests
{
    [SetUp]
    public void SetUp() => LocalizationService.ResetForTests();

    [Test]
    public void FormalTables_HaveIdenticalNonEmptyKeyCoverage()
    {
        HashSet<string> english = ReadKeys("Localization/strings.en");
        HashSet<string> chinese = ReadKeys("Localization/strings.zh-Hans");
        Assert.That(english, Is.EquivalentTo(chinese));
        Assert.That(english, Does.Contain("ui.room_exit"));
    }

    [Test]
    public void LocaleSelection_RejectsUnsupportedLocale_AndSourceFallbackIsDeterministic()
    {
        Assert.That(LocalizationService.SetLocale("zh-CN", false), Is.True);
        Assert.That(LocalizationService.CurrentLocale, Is.EqualTo(LocalizationService.SimplifiedChinese));
        Assert.That(LocalizationService.Get("ui.room_exit"), Is.EqualTo("出口"));
        Assert.That(LocalizationService.SetLocale("fr", false), Is.False);
        Assert.That(LocalizationService.Get("missing.key"), Is.EqualTo("missing.key"));
    }

    private static HashSet<string> ReadKeys(string resourcePath)
    {
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        Assert.That(asset, Is.Not.Null, resourcePath);
        LocalizationTable table = JsonUtility.FromJson<LocalizationTable>(asset.text);
        Assert.That(table?.entries, Is.Not.Null);
        HashSet<string> keys = new();
        foreach (LocalizationEntry entry in table.entries)
        {
            Assert.That(entry.key, Is.Not.Empty);
            Assert.That(entry.value, Is.Not.Empty, entry.key);
            Assert.That(keys.Add(entry.key), Is.True, $"Duplicate key: {entry.key}");
        }
        return keys;
    }
}
