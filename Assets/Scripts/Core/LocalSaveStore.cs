using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public enum LocalSaveLoadStatus
{
    Success,
    Missing,
    Corrupt,
    UnsupportedFutureVersion
}

public sealed class LocalSaveLoadResult
{
    public LocalSaveLoadStatus Status { get; internal set; }
    public SaveData Data { get; internal set; }
    public bool RecoveredFromBackup { get; internal set; }
    public bool NeedsRewrite { get; internal set; }
    public string Error { get; internal set; }
}

public sealed class LocalSaveStore
{
    public const string MainFileName = "profile.json";
    public const string TempFileName = "profile.tmp";
    public const string BackupFileName = "profile.backup.json";

    private enum FileReadStatus { Success, Missing, Corrupt, UnsupportedFutureVersion }

    [Serializable]
    private sealed class SchemaHeader { public int schemaVersion; }

    private readonly string directory;
    private readonly Func<DateTimeOffset> utcNow;
    private readonly Func<string> forcedWriteError;
    private bool preserveCorruptMainOnNextWrite;
    private readonly List<string> lastPreservedPaths = new();

    public string MainPath => Path.Combine(directory, MainFileName);
    public string TempPath => Path.Combine(directory, TempFileName);
    public string BackupPath => Path.Combine(directory, BackupFileName);
    public IReadOnlyList<string> LastPreservedPaths => lastPreservedPaths;
    public bool HasAnyProfileFile => File.Exists(MainPath) || File.Exists(BackupPath);

    public LocalSaveStore(string directoryPath, Func<DateTimeOffset> clock = null,
        Func<string> writeFailure = null)
    {
        directory = directoryPath;
        utcNow = clock ?? (() => DateTimeOffset.UtcNow);
        forcedWriteError = writeFailure;
    }

    public LocalSaveLoadResult Load()
    {
        preserveCorruptMainOnNextWrite = false;
        FileReadStatus mainStatus = TryRead(MainPath, out SaveData main, out bool mainChanged,
            out string mainError);
        if (mainStatus == FileReadStatus.Success)
            return Success(main, false, mainChanged);
        if (mainStatus == FileReadStatus.UnsupportedFutureVersion)
            return Failure(LocalSaveLoadStatus.UnsupportedFutureVersion, $"Main: {mainError}");

        FileReadStatus backupStatus = TryRead(BackupPath, out SaveData backup, out _,
            out string backupError);
        if (backupStatus == FileReadStatus.Success)
        {
            preserveCorruptMainOnNextWrite = mainStatus == FileReadStatus.Corrupt;
            return Success(backup, true, true);
        }
        if (backupStatus == FileReadStatus.UnsupportedFutureVersion)
            return Failure(LocalSaveLoadStatus.UnsupportedFutureVersion,
                $"Main: {mainError}; Backup: {backupError}");
        if (mainStatus == FileReadStatus.Missing && backupStatus == FileReadStatus.Missing)
            return Failure(LocalSaveLoadStatus.Missing, "No local profile exists.");
        return Failure(LocalSaveLoadStatus.Corrupt, $"Main: {mainError}; Backup: {backupError}");
    }

    public bool TryLoad(out SaveData data, out bool recoveredFromBackup, out bool needsRewrite,
        out string error)
    {
        LocalSaveLoadResult result = Load();
        data = result.Data;
        recoveredFromBackup = result.RecoveredFromBackup;
        needsRewrite = result.NeedsRewrite;
        error = result.Error;
        return result.Status == LocalSaveLoadStatus.Success;
    }

    public bool TryWrite(SaveData source, out string error)
        => TryWrite(source, out _, out error);

    public bool TryWrite(SaveData source, out SaveData persisted, out string error)
    {
        persisted = null;
        error = null;
        try
        {
            string injectedFailure = forcedWriteError?.Invoke();
            if (!string.IsNullOrEmpty(injectedFailure))
            {
                error = injectedFailure;
                return false;
            }

            SaveData candidate = SaveData.Clone(source);
            if (candidate == null)
            {
                error = "Save data is null.";
                return false;
            }
            DateTimeOffset updatedAt = utcNow().ToUniversalTime();
            if (DateTimeOffset.TryParse(candidate.createdAtUtc, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out DateTimeOffset createdAt) && updatedAt < createdAt)
                updatedAt = createdAt;
            candidate.updatedAtUtc = SaveDataMigration.FormatUtc(updatedAt);
            if (!SaveDataMigration.TryMigrate(candidate, out _, out error) ||
                !SaveDataMigration.Validate(candidate, out error)) return false;

            Directory.CreateDirectory(directory);
            string json = JsonUtility.ToJson(candidate, true);
            WriteDurably(TempPath, json);
            FileReadStatus tempStatus = TryRead(TempPath, out SaveData verified, out bool tempChanged,
                out error);
            if (tempStatus != FileReadStatus.Success || tempChanged ||
                !string.Equals(JsonUtility.ToJson(verified, true), json, StringComparison.Ordinal))
            {
                error ??= "Temporary file verification failed.";
                return false;
            }

            if (File.Exists(MainPath))
            {
                FileReadStatus activeStatus = TryRead(MainPath, out _, out _, out string activeError);
                if (activeStatus == FileReadStatus.Success)
                    File.Copy(MainPath, BackupPath, true);
                else if (activeStatus != FileReadStatus.Corrupt || !preserveCorruptMainOnNextWrite)
                {
                    error = $"Active main file is not replaceable: {activeError}";
                    return false;
                }
                if (activeStatus == FileReadStatus.Success)
                    File.Replace(TempPath, MainPath, null);
                else
                {
                    PreserveIfPresent(MainPath, "main");
                    File.Move(TempPath, MainPath);
                }
            }
            else
            {
                File.Move(TempPath, MainPath);
            }

            preserveCorruptMainOnNextWrite = false;
            persisted = candidate;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.GetType().Name;
            return false;
        }
    }

    public bool PrepareForNewGame(out string error)
    {
        error = null;
        lastPreservedPaths.Clear();
        try
        {
            Directory.CreateDirectory(directory);
            FileReadStatus mainStatus = TryRead(MainPath, out _, out _, out _);
            FileReadStatus backupStatus = TryRead(BackupPath, out _, out _, out _);
            bool blockedDataExists = mainStatus == FileReadStatus.Corrupt ||
                                     mainStatus == FileReadStatus.UnsupportedFutureVersion ||
                                     backupStatus == FileReadStatus.Corrupt ||
                                     backupStatus == FileReadStatus.UnsupportedFutureVersion;
            if (blockedDataExists)
            {
                PreserveIfPresent(MainPath, "main");
                PreserveIfPresent(BackupPath, "backup");
                PreserveIfPresent(TempPath, "temp");
            }
            else
            {
                if (mainStatus == FileReadStatus.Success)
                {
                    File.Copy(MainPath, BackupPath, true);
                    File.Delete(MainPath);
                }
                if (File.Exists(TempPath)) File.Delete(TempPath);
            }
            preserveCorruptMainOnNextWrite = false;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.GetType().Name;
            return false;
        }
    }

    public void PreserveAndDeleteForNewGame()
    {
        if (!PrepareForNewGame(out string error))
            throw new IOException(error);
    }

    private void PreserveIfPresent(string path, string role)
    {
        if (!File.Exists(path)) return;
        string stamp = utcNow().UtcDateTime.ToString("yyyyMMdd'T'HHmmssfffffff'Z'");
        string preserved = Path.Combine(directory, $"profile.{role}.preserved.{stamp}");
        int suffix = 1;
        while (File.Exists(preserved))
            preserved = Path.Combine(directory, $"profile.{role}.preserved.{stamp}.{suffix++}");
        File.Move(path, preserved);
        lastPreservedPaths.Add(preserved);
    }

    private FileReadStatus TryRead(string path, out SaveData data, out bool changed, out string error)
    {
        data = null;
        changed = false;
        error = null;
        if (!File.Exists(path))
        {
            error = "Missing";
            return FileReadStatus.Missing;
        }

        try
        {
            string json = File.ReadAllText(path);
            string trimmed = json.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal) ||
                !trimmed.EndsWith("}", StringComparison.Ordinal))
            {
                error = "MalformedJson";
                return FileReadStatus.Corrupt;
            }
            SchemaHeader header = JsonUtility.FromJson<SchemaHeader>(json);
            if (header != null && header.schemaVersion > SaveDataMigration.CurrentSchemaVersion)
            {
                error = $"UnsupportedFutureVersion:{header.schemaVersion}";
                return FileReadStatus.UnsupportedFutureVersion;
            }

            data = JsonUtility.FromJson<SaveData>(json);
            if (data != null && header != null) data.schemaVersion = header.schemaVersion;
            if (!SaveDataMigration.TryMigrate(data, out changed, out error) ||
                !SaveDataMigration.Validate(data, out error))
            {
                data = null;
                return FileReadStatus.Corrupt;
            }
            return FileReadStatus.Success;
        }
        catch (Exception exception)
        {
            error = exception.GetType().Name;
            data = null;
            return FileReadStatus.Corrupt;
        }
    }

    private static void WriteDurably(string path, string contents)
    {
        byte[] bytes = new UTF8Encoding(false).GetBytes(contents);
        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush(true);
    }

    private static LocalSaveLoadResult Success(SaveData data, bool backup, bool rewrite)
        => new()
        {
            Status = LocalSaveLoadStatus.Success,
            Data = data,
            RecoveredFromBackup = backup,
            NeedsRewrite = rewrite
        };

    private static LocalSaveLoadResult Failure(LocalSaveLoadStatus status, string error)
        => new() { Status = status, Error = error };
}
