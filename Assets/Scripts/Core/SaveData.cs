using System;
using System.Collections.Generic;

[Serializable]
public sealed class SaveData
{
    public int schemaVersion = SaveDataMigration.CurrentSchemaVersion;
    public string saveId;
    public string createdAtUtc;
    public string updatedAtUtc;
    public double playTimeSeconds;
    public string lastRoomId = "CENTER_001";
    public string lastEntranceId = "DEFAULT";
    public List<string> unlockedAbilities = new();
    public List<string> collectedPermanentIds = new();
    public List<string> completedRoomIds = new();
    public List<string> unlockedRegionIds = new();
    public List<RegionProgressData> regionProgress = new();
    public List<string> progressionFlags = new();

    public static SaveData CreateNew()
    {
        string now = DateTime.UtcNow.ToString("O");
        return new SaveData
        {
            saveId = Guid.NewGuid().ToString("N"),
            createdAtUtc = now,
            updatedAtUtc = now
        };
    }
}

[Serializable]
public sealed class RegionProgressData
{
    public string regionId;
    public int collectedCount;
    public int totalCount;
}

public static class SaveIds
{
    public const string MirrorAbility = "MIRROR";
    public const string MirrorPickup = "CENTER_001:ABILITY:01";
    public const string DefaultRoom = "CENTER_001";
    public const string DefaultEntrance = "DEFAULT";
}

public static class SaveDataMigration
{
    public const int CurrentSchemaVersion = 1;

    public static bool TryMigrate(SaveData data, out bool changed, out string error)
    {
        changed = false;
        error = null;
        if (data == null) { error = "Save data is null."; return false; }
        if (data.schemaVersion < 0 || data.schemaVersion > CurrentSchemaVersion)
        {
            error = $"Unsupported schema version {data.schemaVersion}.";
            return false;
        }

        if (data.schemaVersion == 0)
        {
            data.schemaVersion = 1;
            changed = true;
        }

        EnsureCollections(data);
        changed |= NormalizeSet(data.unlockedAbilities);
        changed |= NormalizeSet(data.collectedPermanentIds);
        changed |= NormalizeSet(data.completedRoomIds);
        changed |= NormalizeSet(data.unlockedRegionIds);
        changed |= NormalizeSet(data.progressionFlags);

        bool hasAbility = data.unlockedAbilities.Contains(SaveIds.MirrorAbility);
        bool hasPickup = data.collectedPermanentIds.Contains(SaveIds.MirrorPickup);
        if (hasAbility != hasPickup)
        {
            if (!hasAbility) data.unlockedAbilities.Add(SaveIds.MirrorAbility);
            if (!hasPickup) data.collectedPermanentIds.Add(SaveIds.MirrorPickup);
            changed = true;
        }
        return true;
    }

    public static bool Validate(SaveData data, out string error)
    {
        error = null;
        if (data == null) { error = "Save data is null."; return false; }
        if (data.schemaVersion != CurrentSchemaVersion) { error = "Schema version is not current."; return false; }
        if (string.IsNullOrWhiteSpace(data.saveId)) { error = "saveId is missing."; return false; }
        if (!DateTime.TryParse(data.createdAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out _)) { error = "createdAtUtc is invalid."; return false; }
        if (!DateTime.TryParse(data.updatedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out _)) { error = "updatedAtUtc is invalid."; return false; }
        if (string.IsNullOrWhiteSpace(data.lastRoomId)) { error = "lastRoomId is missing."; return false; }
        if (data.playTimeSeconds < 0) { error = "playTimeSeconds is negative."; return false; }
        EnsureCollections(data);
        return true;
    }

    private static void EnsureCollections(SaveData data)
    {
        data.unlockedAbilities ??= new List<string>();
        data.collectedPermanentIds ??= new List<string>();
        data.completedRoomIds ??= new List<string>();
        data.unlockedRegionIds ??= new List<string>();
        data.regionProgress ??= new List<RegionProgressData>();
        data.progressionFlags ??= new List<string>();
    }

    private static bool NormalizeSet(List<string> values)
    {
        HashSet<string> unique = new(StringComparer.Ordinal);
        bool changed = false;
        for (int i = values.Count - 1; i >= 0; i--)
        {
            string value = values[i]?.Trim();
            if (string.IsNullOrEmpty(value) || !unique.Add(value)) { values.RemoveAt(i); changed = true; }
            else if (value != values[i]) { values[i] = value; changed = true; }
        }
        values.Sort(StringComparer.Ordinal);
        return changed;
    }
}
