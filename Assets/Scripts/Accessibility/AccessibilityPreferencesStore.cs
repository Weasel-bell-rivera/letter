using System;
using System.IO;
using UnityEngine;

namespace W1.Accessibility
{
    internal sealed class AccessibilityPreferencesStore
    {
        internal const string MainFileName = "accessibility-settings.json";
        private const string TempFileName = "accessibility-settings.tmp";
        private const string BackupFileName = "accessibility-settings.backup.json";
        private const int CurrentSchemaVersion = 1;

        private readonly string directory;

        internal string MainPath => Path.Combine(directory, MainFileName);
        private string TempPath => Path.Combine(directory, TempFileName);
        private string BackupPath => Path.Combine(directory, BackupFileName);

        internal AccessibilityPreferencesStore(string directoryPath)
        {
            directory = directoryPath;
        }

        internal bool TryLoad(out AccessibilityPreferences preferences, out bool recoveredFromBackup, out string error)
        {
            recoveredFromBackup = false;
            if (TryRead(MainPath, out preferences, out error))
                return true;

            string mainError = error;
            if (TryRead(BackupPath, out preferences, out error))
            {
                recoveredFromBackup = true;
                return true;
            }

            error = File.Exists(MainPath) || File.Exists(BackupPath)
                ? $"Main: {mainError}; Backup: {error}"
                : null;
            preferences = AccessibilityPreferences.Default;
            return false;
        }

        internal bool TryWrite(AccessibilityPreferences preferences, out string error)
        {
            error = null;
            try
            {
                Directory.CreateDirectory(directory);
                FileData data = FileData.From(preferences);
                File.WriteAllText(TempPath, JsonUtility.ToJson(data, true));
                if (!TryRead(TempPath, out AccessibilityPreferences verified, out error) || !preferences.Equals(verified))
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

        private static bool TryRead(string path, out AccessibilityPreferences preferences, out string error)
        {
            preferences = AccessibilityPreferences.Default;
            error = null;
            if (!File.Exists(path))
            {
                error = "Missing";
                return false;
            }

            try
            {
                FileData data = JsonUtility.FromJson<FileData>(File.ReadAllText(path));
                if (data == null || data.schemaVersion != CurrentSchemaVersion)
                {
                    error = "UnsupportedSchema";
                    return false;
                }

                preferences = new AccessibilityPreferences(
                    AccessibilityPreferences.SanitizeTextScale((TextScalePreset)data.textScalePercent),
                    data.highContrast,
                    data.reducedMotion);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.GetType().Name;
                return false;
            }
        }

        [Serializable]
        private sealed class FileData
        {
            public int schemaVersion = CurrentSchemaVersion;
            public int textScalePercent = 100;
            public bool highContrast;
            public bool reducedMotion;

            public static FileData From(AccessibilityPreferences preferences) => new()
            {
                textScalePercent = (int)preferences.TextScale,
                highContrast = preferences.HighContrast,
                reducedMotion = preferences.ReducedMotion
            };
        }
    }
}
