using System;
using System.IO;
using UnityEngine;

public sealed class LocalSaveStore
{
    public const string MainFileName = "profile.json";
    public const string TempFileName = "profile.tmp";
    public const string BackupFileName = "profile.backup.json";

    private readonly string directory;
    public string MainPath => Path.Combine(directory, MainFileName);
    public string TempPath => Path.Combine(directory, TempFileName);
    public string BackupPath => Path.Combine(directory, BackupFileName);

    public LocalSaveStore(string directoryPath) => directory = directoryPath;

    public bool TryLoad(out SaveData data, out bool recoveredFromBackup, out bool needsRewrite, out string error)
    {
        recoveredFromBackup = false;
        needsRewrite = false;
        if (TryRead(MainPath, out data, out needsRewrite, out error)) return true;
        string mainError = error;
        if (TryRead(BackupPath, out data, out needsRewrite, out error))
        {
            recoveredFromBackup = true;
            needsRewrite = true;
            return true;
        }
        error = $"Main: {mainError} Backup: {error}";
        return false;
    }

    public bool TryWrite(SaveData data, out string error)
    {
        error = null;
        try
        {
            Directory.CreateDirectory(directory);
            data.updatedAtUtc = DateTime.UtcNow.ToString("O");
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(TempPath, json);
            if (!TryRead(TempPath, out SaveData verified, out _, out error) || verified.saveId != data.saveId)
                return false;

            if (File.Exists(MainPath))
            {
                if (TryRead(MainPath, out _, out _, out _)) File.Copy(MainPath, BackupPath, true);
                File.Replace(TempPath, MainPath, null);
            }
            else File.Move(TempPath, MainPath);
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
        Directory.CreateDirectory(directory);
        if (File.Exists(MainPath)) File.Copy(MainPath, BackupPath, true);
        if (File.Exists(MainPath)) File.Delete(MainPath);
        if (File.Exists(TempPath)) File.Delete(TempPath);
    }

    private static bool TryRead(string path, out SaveData data, out bool changed, out string error)
    {
        data = null;
        changed = false;
        error = null;
        if (!File.Exists(path)) { error = "Missing"; return false; }
        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (!SaveDataMigration.TryMigrate(data, out changed, out error)) return false;
            return SaveDataMigration.Validate(data, out error);
        }
        catch (Exception exception)
        {
            error = exception.GetType().Name;
            data = null;
            return false;
        }
    }
}
