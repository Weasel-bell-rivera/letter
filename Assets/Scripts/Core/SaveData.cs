using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

[Serializable]
public sealed class SaveData
{
    public int schemaVersion = SaveDataMigration.CurrentSchemaVersion;
    public string saveId;
    public string createdAtUtc;
    public string updatedAtUtc;
    public double playTimeSeconds;
    public string lastRoomId = SaveIds.DefaultRoom;
    public string lastEntranceId = SaveIds.DefaultEntrance;
    public List<string> unlockedAbilities;
    public List<string> collectedPermanentIds;
    public List<string> completedRoomIds;
    public List<string> unlockedRegionIds;
    public List<string> latchedDoorGroupIds;
    public List<RegionProgressData> regionProgress;
    public List<string> progressionFlags;

    public static SaveData CreateNew()
    {
        string now = SaveDataMigration.FormatUtc(DateTimeOffset.UtcNow);
        return new SaveData
        {
            saveId = Guid.NewGuid().ToString("N"),
            createdAtUtc = now,
            updatedAtUtc = now,
            unlockedAbilities = new List<string>(),
            collectedPermanentIds = new List<string>(),
            completedRoomIds = new List<string>(),
            unlockedRegionIds = new List<string>(),
            latchedDoorGroupIds = new List<string>(),
            regionProgress = new List<RegionProgressData>(),
            progressionFlags = new List<string>()
        };
    }

    public static SaveData Clone(SaveData source)
    {
        if (source == null) return null;
        SaveData clone = new()
        {
            schemaVersion = source.schemaVersion,
            saveId = source.saveId,
            createdAtUtc = source.createdAtUtc,
            updatedAtUtc = source.updatedAtUtc,
            playTimeSeconds = source.playTimeSeconds,
            lastRoomId = source.lastRoomId,
            lastEntranceId = source.lastEntranceId,
            unlockedAbilities = Copy(source.unlockedAbilities),
            collectedPermanentIds = Copy(source.collectedPermanentIds),
            completedRoomIds = Copy(source.completedRoomIds),
            unlockedRegionIds = Copy(source.unlockedRegionIds),
            latchedDoorGroupIds = Copy(source.latchedDoorGroupIds),
            progressionFlags = Copy(source.progressionFlags),
            regionProgress = source.regionProgress == null ? null : new List<RegionProgressData>()
        };
        if (source.regionProgress != null)
        {
            foreach (RegionProgressData progress in source.regionProgress)
                clone.regionProgress.Add(progress == null ? null : new RegionProgressData
                {
                    regionId = progress.regionId,
                    collectedCount = progress.collectedCount,
                    totalCount = progress.totalCount
                });
        }
        return clone;
    }

    private static List<string> Copy(List<string> source)
        => source == null ? null : new List<string>(source);
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
    public const string Fire007DoorGroup = "FIRE_007:DOOR_GROUP:01";
    public const string Fire008DoorGroup01 = "FIRE_008:DOOR_GROUP:01";
    public const string Fire008DoorGroup02 = "FIRE_008:DOOR_GROUP:02";
    public const string Fire008DoorGroup03 = "FIRE_008:DOOR_GROUP:03";
}

public static class SaveIdRules
{
    private static readonly Regex RoomPattern = new("^[A-Z]+_[0-9]{3}$", RegexOptions.CultureInvariant);
    private static readonly Regex EntrancePattern = new("^[A-Z][A-Z0-9_]*$", RegexOptions.CultureInvariant);
    private static readonly Regex RegionPattern = new("^[A-Z]+$", RegexOptions.CultureInvariant);
    private static readonly Regex TokenPattern = new("^[A-Z][A-Z0-9_.:-]*$", RegexOptions.CultureInvariant);
    private static readonly Regex PermanentPattern = new(
        "^[A-Z]+_[0-9]{3}:(ABILITY|COLLECTIBLE|PROGRESSION):[0-9]{2}$",
        RegexOptions.CultureInvariant);
    private static readonly Regex DoorGroupPattern = new(
        "^[A-Z]+_[0-9]{3}:DOOR_GROUP:[0-9]{2}$", RegexOptions.CultureInvariant);

    public static bool IsRoomId(string value) => IsMatch(RoomPattern, value);
    public static bool IsEntranceId(string value) => IsMatch(EntrancePattern, value);
    public static bool IsRegionId(string value) => IsMatch(RegionPattern, value);
    public static bool IsTokenId(string value) => IsMatch(TokenPattern, value);
    public static bool IsPermanentId(string value) => IsMatch(PermanentPattern, value);
    public static bool IsDoorGroupId(string value) => IsMatch(DoorGroupPattern, value);

    private static bool IsMatch(Regex pattern, string value)
        => !string.IsNullOrWhiteSpace(value) && pattern.IsMatch(value);
}

public static class SaveDataMigration
{
    public const int CurrentSchemaVersion = 2;

    public static bool TryMigrate(SaveData data, out bool changed, out string error)
    {
        changed = false;
        error = null;
        if (data == null)
        {
            error = "Save data is null.";
            return false;
        }
        if (data.schemaVersion < 0)
        {
            error = $"Invalid schema version {data.schemaVersion}.";
            return false;
        }
        if (data.schemaVersion > CurrentSchemaVersion)
        {
            error = $"Unsupported future schema version {data.schemaVersion}.";
            return false;
        }

        while (data.schemaVersion < CurrentSchemaVersion)
        {
            bool migrated = data.schemaVersion switch
            {
                0 => MigrateVersion0To1(data),
                1 => MigrateVersion1To2(data),
                _ => false
            };
            if (!migrated)
            {
                error = $"No migration is registered for schema version {data.schemaVersion}.";
                return false;
            }
            changed = true;
        }

        if (!HasRequiredCollections(data, out error)) return false;
        changed |= NormalizeContinueLocation(data);
        changed |= NormalizeSet(data.unlockedAbilities);
        changed |= NormalizeSet(data.collectedPermanentIds);
        changed |= NormalizeSet(data.completedRoomIds);
        changed |= NormalizeSet(data.unlockedRegionIds);
        changed |= NormalizeSet(data.latchedDoorGroupIds);
        changed |= NormalizeSet(data.progressionFlags);
        changed |= NormalizeRegionProgress(data.regionProgress);

        bool hasAbility = data.unlockedAbilities.Contains(SaveIds.MirrorAbility);
        bool hasPickup = data.collectedPermanentIds.Contains(SaveIds.MirrorPickup);
        if (hasAbility != hasPickup)
        {
            if (!hasAbility) data.unlockedAbilities.Add(SaveIds.MirrorAbility);
            if (!hasPickup) data.collectedPermanentIds.Add(SaveIds.MirrorPickup);
            data.unlockedAbilities.Sort(StringComparer.Ordinal);
            data.collectedPermanentIds.Sort(StringComparer.Ordinal);
            changed = true;
        }
        return true;
    }

    public static bool Validate(SaveData data, out string error)
    {
        error = null;
        if (data == null) return Fail("Save data is null.", out error);
        if (data.schemaVersion != CurrentSchemaVersion)
            return Fail("Schema version is not current.", out error);
        if (!Guid.TryParse(data.saveId, out _)) return Fail("saveId is invalid.", out error);
        if (!TryParseUtc(data.createdAtUtc, out DateTimeOffset created))
            return Fail("createdAtUtc is invalid or not UTC.", out error);
        if (!TryParseUtc(data.updatedAtUtc, out DateTimeOffset updated))
            return Fail("updatedAtUtc is invalid or not UTC.", out error);
        if (updated < created) return Fail("updatedAtUtc precedes createdAtUtc.", out error);
        if (double.IsNaN(data.playTimeSeconds) || double.IsInfinity(data.playTimeSeconds) || data.playTimeSeconds < 0)
            return Fail("playTimeSeconds is invalid.", out error);
        if (!SaveIdRules.IsRoomId(data.lastRoomId)) return Fail("lastRoomId is invalid.", out error);
        if (!SaveIdRules.IsEntranceId(data.lastEntranceId)) return Fail("lastEntranceId is invalid.", out error);
        if (!ValidateSet(data.unlockedAbilities, SaveIdRules.IsTokenId, "unlockedAbilities", out error)) return false;
        if (!ValidateSet(data.collectedPermanentIds, SaveIdRules.IsPermanentId, "collectedPermanentIds", out error)) return false;
        if (!ValidateSet(data.completedRoomIds, SaveIdRules.IsRoomId, "completedRoomIds", out error)) return false;
        if (!ValidateSet(data.unlockedRegionIds, SaveIdRules.IsRegionId, "unlockedRegionIds", out error)) return false;
        if (!ValidateSet(data.latchedDoorGroupIds, SaveIdRules.IsDoorGroupId, "latchedDoorGroupIds", out error)) return false;
        if (!ValidateSet(data.progressionFlags, SaveIdRules.IsTokenId, "progressionFlags", out error)) return false;
        if (data.regionProgress == null) return Fail("regionProgress is missing.", out error);

        HashSet<string> regions = new(StringComparer.Ordinal);
        foreach (RegionProgressData progress in data.regionProgress)
        {
            if (progress == null || !SaveIdRules.IsRegionId(progress.regionId))
                return Fail("regionProgress contains an invalid regionId.", out error);
            if (!regions.Add(progress.regionId))
                return Fail("regionProgress contains duplicate regionIds.", out error);
            if (progress.collectedCount < 0 || progress.totalCount < 0 || progress.collectedCount > progress.totalCount)
                return Fail($"regionProgress for {progress.regionId} has invalid counts.", out error);
        }
        return true;
    }

    public static string FormatUtc(DateTimeOffset value)
        => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static bool MigrateVersion0To1(SaveData data)
    {
        EnsureCollections(data);
        data.schemaVersion = 1;
        return true;
    }

    private static bool MigrateVersion1To2(SaveData data)
    {
        EnsureCollections(data);
        data.schemaVersion = 2;
        return true;
    }

    private static bool HasRequiredCollections(SaveData data, out string error)
    {
        error = null;
        if (data.unlockedAbilities == null) return Fail("unlockedAbilities is missing.", out error);
        if (data.collectedPermanentIds == null) return Fail("collectedPermanentIds is missing.", out error);
        if (data.completedRoomIds == null) return Fail("completedRoomIds is missing.", out error);
        if (data.unlockedRegionIds == null) return Fail("unlockedRegionIds is missing.", out error);
        if (data.latchedDoorGroupIds == null) return Fail("latchedDoorGroupIds is missing.", out error);
        if (data.regionProgress == null) return Fail("regionProgress is missing.", out error);
        if (data.progressionFlags == null) return Fail("progressionFlags is missing.", out error);
        return true;
    }

    private static void EnsureCollections(SaveData data)
    {
        data.unlockedAbilities ??= new List<string>();
        data.collectedPermanentIds ??= new List<string>();
        data.completedRoomIds ??= new List<string>();
        data.unlockedRegionIds ??= new List<string>();
        data.latchedDoorGroupIds ??= new List<string>();
        data.regionProgress ??= new List<RegionProgressData>();
        data.progressionFlags ??= new List<string>();
    }

    private static bool NormalizeSet(List<string> values)
    {
        HashSet<string> unique = new(StringComparer.Ordinal);
        List<string> normalized = new();
        foreach (string raw in values)
        {
            string value = raw?.Trim();
            if (!string.IsNullOrEmpty(value) && unique.Add(value)) normalized.Add(value);
        }
        normalized.Sort(StringComparer.Ordinal);
        bool changed = values.Count != normalized.Count;
        if (!changed)
            for (int i = 0; i < values.Count; i++)
                if (!string.Equals(values[i], normalized[i], StringComparison.Ordinal)) { changed = true; break; }
        if (changed)
        {
            values.Clear();
            values.AddRange(normalized);
        }
        return changed;
    }

    private static bool NormalizeContinueLocation(SaveData data)
    {
        string room = data.lastRoomId?.Trim().ToUpperInvariant();
        string entrance = data.lastEntranceId?.Trim().ToUpperInvariant();
        if (!SaveIdRules.IsRoomId(room)) room = SaveIds.DefaultRoom;
        if (!SaveIdRules.IsEntranceId(entrance)) entrance = SaveIds.DefaultEntrance;
        bool changed = !string.Equals(data.lastRoomId, room, StringComparison.Ordinal) ||
                       !string.Equals(data.lastEntranceId, entrance, StringComparison.Ordinal);
        data.lastRoomId = room;
        data.lastEntranceId = entrance;
        return changed;
    }

    private static bool NormalizeRegionProgress(List<RegionProgressData> values)
    {
        Dictionary<string, RegionProgressData> merged = new(StringComparer.Ordinal);
        List<RegionProgressData> invalid = new();
        bool changed = false;
        foreach (RegionProgressData item in values)
        {
            if (item == null)
            {
                changed = true;
                continue;
            }
            string regionId = item.regionId?.Trim();
            if (regionId != item.regionId) changed = true;
            if (string.IsNullOrEmpty(regionId))
            {
                invalid.Add(new RegionProgressData
                {
                    regionId = regionId,
                    collectedCount = item.collectedCount,
                    totalCount = item.totalCount
                });
            }
            else if (merged.TryGetValue(regionId, out RegionProgressData existing))
            {
                existing.collectedCount = Math.Max(existing.collectedCount, item.collectedCount);
                existing.totalCount = Math.Max(existing.totalCount, item.totalCount);
                changed = true;
            }
            else
            {
                merged[regionId] = new RegionProgressData
                {
                    regionId = regionId,
                    collectedCount = item.collectedCount,
                    totalCount = item.totalCount
                };
            }
        }
        List<RegionProgressData> normalized = new(merged.Values);
        normalized.AddRange(invalid);
        normalized.Sort((left, right) => string.CompareOrdinal(left.regionId, right.regionId));
        if (!changed && values.Count == normalized.Count)
            for (int i = 0; i < values.Count; i++)
                if (!string.Equals(values[i].regionId, normalized[i].regionId, StringComparison.Ordinal))
                { changed = true; break; }
        if (changed)
        {
            values.Clear();
            values.AddRange(normalized);
        }
        return changed;
    }

    private static bool ValidateSet(List<string> values, Func<string, bool> validator, string field, out string error)
    {
        error = null;
        if (values == null) return Fail($"{field} is missing.", out error);
        HashSet<string> unique = new(StringComparer.Ordinal);
        foreach (string value in values)
        {
            if (!validator(value)) return Fail($"{field} contains an invalid ID.", out error);
            if (!unique.Add(value)) return Fail($"{field} contains duplicate IDs.", out error);
        }
        return true;
    }

    private static bool TryParseUtc(string value, out DateTimeOffset parsed)
    {
        bool valid = DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed);
        return valid && parsed.Offset == TimeSpan.Zero;
    }

    private static bool Fail(string message, out string error)
    {
        error = message;
        return false;
    }
}
