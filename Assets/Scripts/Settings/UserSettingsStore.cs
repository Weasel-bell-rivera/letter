using System;
using System.IO;
using UnityEngine;

internal sealed class UserSettingsStore
{
    internal const string MainFileName = "user-settings.json";
    private const string TempFileName = "user-settings.tmp";
    private const string BackupFileName = "user-settings.backup.json";
    private const int CurrentSchemaVersion = 1;

    private readonly string directory;

    internal string MainPath => Path.Combine(directory, MainFileName);
    private string TempPath => Path.Combine(directory, TempFileName);
    private string BackupPath => Path.Combine(directory, BackupFileName);

    internal UserSettingsStore(string directoryPath)
    {
        directory = directoryPath;
    }

    internal bool TryLoad(out UserSettingsFileData data, out bool recoveredFromBackup, out string error)
    {
        recoveredFromBackup = false;
        if (TryRead(MainPath, out data, out error))
            return true;

        string mainError = error;
        if (TryRead(BackupPath, out data, out error))
        {
            recoveredFromBackup = true;
            return true;
        }

        error = File.Exists(MainPath) || File.Exists(BackupPath)
            ? $"Main: {mainError}; Backup: {error}"
            : null;
        return false;
    }

    internal bool TryWrite(UserSettingsFileData data, out string error)
    {
        error = null;
        try
        {
            Directory.CreateDirectory(directory);
            data.schemaVersion = CurrentSchemaVersion;
            File.WriteAllText(TempPath, JsonUtility.ToJson(data, true));

            if (!TryRead(TempPath, out UserSettingsFileData verified, out error) || !data.HasSameValues(verified))
            {
                error ??= "VerificationFailed";
                return false;
            }

            if (File.Exists(MainPath))
            {
                if (TryRead(MainPath, out _, out _))
                    File.Copy(MainPath, BackupPath, true);
                File.Replace(TempPath, MainPath, null);
            }
            else
            {
                File.Move(TempPath, MainPath);
            }

            return true;
        }
        catch (Exception exception)
        {
            error = exception.GetType().Name;
            return false;
        }
    }

    private static bool TryRead(string path, out UserSettingsFileData data, out string error)
    {
        data = null;
        error = null;
        if (!File.Exists(path))
        {
            error = "Missing";
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<UserSettingsFileData>(File.ReadAllText(path));
            if (data == null || data.schemaVersion != CurrentSchemaVersion)
            {
                error = "UnsupportedSchema";
                data = null;
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            error = exception.GetType().Name;
            data = null;
            return false;
        }
    }
}

[Serializable]
internal sealed class UserSettingsFileData
{
    public int schemaVersion = 1;
    public string displayMode = "Fullscreen";
    public int resolutionWidth;
    public int resolutionHeight;
    public string qualityName;
    public int qualityLevel;
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;

    internal bool HasSameValues(UserSettingsFileData other)
    {
        return other != null && schemaVersion == other.schemaVersion && displayMode == other.displayMode &&
               resolutionWidth == other.resolutionWidth && resolutionHeight == other.resolutionHeight &&
               qualityName == other.qualityName && qualityLevel == other.qualityLevel &&
               Mathf.Approximately(masterVolume, other.masterVolume) &&
               Mathf.Approximately(musicVolume, other.musicVolume) &&
               Mathf.Approximately(sfxVolume, other.sfxVolume);
    }
}
