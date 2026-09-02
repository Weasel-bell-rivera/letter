using System;
using UnityEngine;

namespace W1.Accessibility
{
    [DefaultExecutionOrder(-9900)]
    public sealed class AccessibilityPreferencesService : MonoBehaviour
    {
        private static AccessibilityPreferencesService instance;
        private AccessibilityPreferencesStore store;

        public static AccessibilityPreferencesService Instance => EnsureInstance();
        public static bool IsReady => instance != null && instance.IsInitialized;

        public bool IsInitialized { get; private set; }
        public AccessibilityPreferences Current { get; private set; } = AccessibilityPreferences.Default;
        public string PersistencePath => store?.MainPath;
        public string LastPersistenceError { get; private set; }
        public bool RecoveredFromBackup { get; private set; }

        public event Action<AccessibilityPreferences> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        private static AccessibilityPreferencesService EnsureInstance()
        {
            if (instance != null)
                return instance;

            instance = FindAnyObjectByType<AccessibilityPreferencesService>();
            if (instance == null)
                instance = new GameObject("Accessibility Preferences Service").AddComponent<AccessibilityPreferencesService>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            store = new AccessibilityPreferencesStore(Application.persistentDataPath);
            if (!store.TryLoad(out AccessibilityPreferences loaded, out bool recovered, out string error))
                loaded = AccessibilityPreferences.Default;
            Current = loaded;
            RecoveredFromBackup = recovered;
            LastPersistenceError = error;
            IsInitialized = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public bool SetTextScale(TextScalePreset value) => Apply(Current.WithTextScale(value));
        public bool SetHighContrast(bool value) => Apply(Current.WithHighContrast(value));
        public bool SetReducedMotion(bool value) => Apply(Current.WithReducedMotion(value));
        public bool RestoreDefaults() => Apply(AccessibilityPreferences.Default);

        public bool Apply(AccessibilityPreferences preferences)
        {
            AccessibilityPreferences sanitized = new(preferences.TextScale, preferences.HighContrast,
                preferences.ReducedMotion);
            bool persisted = store.TryWrite(sanitized, out string error);
            LastPersistenceError = persisted ? null : error;
            if (!persisted)
                return false;

            bool changed = !Current.Equals(sanitized);
            Current = sanitized;
            if (changed)
                Changed?.Invoke(Current);
            return true;
        }

        internal void RestoreRuntimeSnapshotAfterFailedTransaction(AccessibilityPreferences preferences)
        {
            AccessibilityPreferences sanitized = new(preferences.TextScale, preferences.HighContrast,
                preferences.ReducedMotion);
            bool changed = !Current.Equals(sanitized);
            Current = sanitized;
            if (changed)
                Changed?.Invoke(Current);
        }
    }

    public static class AccessibilityMotionPolicy
    {
        public static float Duration(float standardDuration, float reducedMotionDuration = 0f)
        {
            float duration = AccessibilityPreferencesService.Instance.Current.ReducedMotion
                ? reducedMotionDuration
                : standardDuration;
            return Mathf.Max(0f, duration);
        }

        public static bool AllowDecorativeLoop => !AccessibilityPreferencesService.Instance.Current.ReducedMotion;
    }
}
