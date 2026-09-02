using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public sealed class SettingsService : MonoBehaviour
{
    private const string MixerResourceName = "W1AudioMixer";
    private const string MasterVolumeParameter = "MasterVolume";
    private const string MusicVolumeParameter = "MusicVolume";
    private const string SfxVolumeParameter = "SfxVolume";
    private const float MinimumDecibels = -80f;
    private const float SourceRouteScanInterval = 2f;

    private static SettingsService instance;

    private ReadOnlyCollection<DisplayResolutionOption> supportedResolutions;
    private ReadOnlyCollection<string> qualityLevelNames;
    private UserSettingsStore store;
    private SettingsSnapshot appliedSettings;
    private SettingsSnapshot draftSettings;
    private DisplayResolutionOption defaultResolution;
    private int defaultQualityLevel;
    private AudioMixer mixer;
    private bool initialized;
    private bool audioReady;
    private bool draftAudioPreviewActive;
    private bool routeSourcesPending;
    private float nextSourceRouteScanAt;
    private readonly Dictionary<AudioSource, bool> sourceMuteStateBeforeGate = new();
    private readonly List<AudioSource> staleMutedSources = new();

    public static SettingsService Instance => EnsureInstance();
    public static bool IsReady => instance != null && instance.initialized;

    public IReadOnlyList<DisplayResolutionOption> SupportedResolutions => supportedResolutions;
    public IReadOnlyList<string> QualityLevelNames => qualityLevelNames;
    public SettingsSnapshot AppliedSettings => appliedSettings;
    public SettingsSnapshot DraftSettings => draftSettings;
    public string PersistencePath => store?.MainPath;
    public string LastPersistenceError { get; private set; }
    public AudioMixerGroup MusicOutputGroup { get; private set; }
    public AudioMixerGroup SfxOutputGroup { get; private set; }

    public event Action DraftChanged;
    public event Action AppliedChanged;

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

    private static SettingsService EnsureInstance()
    {
        if (instance != null)
            return instance;

        instance = FindAnyObjectByType<SettingsService>();
        if (instance == null)
        {
            GameObject host = new("Settings Service");
            instance = host.AddComponent<SettingsService>();
        }

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
        BuildRuntimeOptions();
        LoadMixer();
        store = new UserSettingsStore(Application.persistentDataPath);
        LoadAndApplySettings();
        initialized = true;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
    }

    private void Start()
    {
        audioReady = true;
        ApplyAudio(appliedSettings);
        RouteUnassignedSourcesToSfx();
        nextSourceRouteScanAt = Time.unscaledTime + SourceRouteScanInterval;
    }

    private void Update()
    {
        if (!audioReady)
            return;

        if (routeSourcesPending || Time.unscaledTime >= nextSourceRouteScanAt)
        {
            RouteUnassignedSourcesToSfx();
            routeSourcesPending = false;
            nextSourceRouteScanAt = Time.unscaledTime + SourceRouteScanInterval;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
    }

    private void OnDestroy()
    {
        RestoreAllSourceMuteStates();
        if (instance == this)
            instance = null;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus || !audioReady)
            return;

        ApplyAudio(CurrentAudioSettings);
        routeSourcesPending = true;
    }

    public void BeginEdit()
    {
        draftSettings = appliedSettings;
        draftAudioPreviewActive = true;
        ApplyAudioPreview();
        DraftChanged?.Invoke();
    }

    public void SetDraftDisplayMode(GameDisplayMode value)
    {
        GameDisplayMode safeValue = value == GameDisplayMode.Windowed
            ? GameDisplayMode.Windowed
            : GameDisplayMode.Fullscreen;
        DisplayResolutionOption safeResolution = ResolveSupportedResolution(
            draftSettings.Resolution.Width, draftSettings.Resolution.Height, safeValue);
        if (draftSettings.DisplayMode == safeValue && draftSettings.Resolution == safeResolution)
            return;

        draftSettings = draftSettings.WithDisplayMode(safeValue).WithResolution(safeResolution);
        DraftChanged?.Invoke();
    }

    public void SetDraftResolution(DisplayResolutionOption value)
    {
        DisplayResolutionOption safeValue = ResolveSupportedResolution(
            value.Width, value.Height, draftSettings.DisplayMode);
        if (draftSettings.Resolution == safeValue)
            return;

        draftSettings = draftSettings.WithResolution(safeValue);
        DraftChanged?.Invoke();
    }

    public void SetDraftResolution(int width, int height)
    {
        SetDraftResolution(new DisplayResolutionOption(width, height));
    }

    public void SetDraftQualityLevel(int value)
    {
        int safeValue = ClampQualityLevel(value);
        if (draftSettings.QualityLevel == safeValue)
            return;

        draftSettings = draftSettings.WithQualityLevel(safeValue);
        DraftChanged?.Invoke();
    }

    public void SetDraftMasterVolume(float value)
    {
        float safeValue = SanitizeVolume(value);
        if (Mathf.Approximately(draftSettings.MasterVolume, safeValue))
            return;

        draftSettings = draftSettings.WithMasterVolume(safeValue);
        draftAudioPreviewActive = true;
        ApplyAudioPreview();
        DraftChanged?.Invoke();
    }

    public void SetDraftMusicVolume(float value)
    {
        float safeValue = SanitizeVolume(value);
        if (Mathf.Approximately(draftSettings.MusicVolume, safeValue))
            return;

        draftSettings = draftSettings.WithMusicVolume(safeValue);
        draftAudioPreviewActive = true;
        ApplyAudioPreview();
        DraftChanged?.Invoke();
    }

    public void SetDraftSfxVolume(float value)
    {
        float safeValue = SanitizeVolume(value);
        if (Mathf.Approximately(draftSettings.SfxVolume, safeValue))
            return;

        draftSettings = draftSettings.WithSfxVolume(safeValue);
        draftAudioPreviewActive = true;
        ApplyAudioPreview();
        DraftChanged?.Invoke();
    }

    public bool Apply()
    {
        SettingsSnapshot candidate = SanitizeSnapshot(draftSettings);
        bool persisted = store.TryWrite(CreateFileData(candidate), out string error);
        if (!persisted)
        {
            LastPersistenceError = error;
            draftSettings = candidate;
            draftAudioPreviewActive = false;
            ApplyGraphics(appliedSettings);
            ApplyAudio(appliedSettings);
            DraftChanged?.Invoke();
            return false;
        }

        appliedSettings = candidate;
        draftSettings = candidate;
        draftAudioPreviewActive = false;
        LastPersistenceError = null;
        ApplyGraphics(candidate);
        ApplyAudio(candidate);
        AppliedChanged?.Invoke();
        DraftChanged?.Invoke();
        return true;
    }

    public void Cancel()
    {
        draftSettings = appliedSettings;
        draftAudioPreviewActive = false;
        ApplyAudioPreview();
        DraftChanged?.Invoke();
    }

    public void RestoreDefaults()
    {
        draftSettings = new SettingsSnapshot(GameDisplayMode.Fullscreen, defaultResolution,
            defaultQualityLevel, 1f, 1f, 1f);
        draftAudioPreviewActive = true;
        ApplyAudioPreview();
        DraftChanged?.Invoke();
    }

    public void RouteToMusic(AudioSource source)
    {
        if (source != null && MusicOutputGroup != null)
        {
            source.outputAudioMixerGroup = MusicOutputGroup;
            ApplyMuteGate(source, CurrentAudioSettings);
        }
    }

    public void RouteToSfx(AudioSource source)
    {
        if (source != null && SfxOutputGroup != null)
        {
            source.outputAudioMixerGroup = SfxOutputGroup;
            ApplyMuteGate(source, CurrentAudioSettings);
        }
    }

    public static float NormalizedToDecibels(float normalized)
    {
        float safeValue = SanitizeVolume(normalized);
        return safeValue <= 0f ? MinimumDecibels : Mathf.Log10(safeValue) * 20f;
    }

    private void BuildRuntimeOptions()
    {
        List<DisplayResolutionOption> options = new();
        HashSet<long> seen = new();
        foreach (Resolution resolution in Screen.resolutions)
        {
            if (resolution.width <= 0 || resolution.height <= 0)
                continue;

            long key = ((long)resolution.width << 32) | (uint)resolution.height;
            if (seen.Add(key))
                options.Add(new DisplayResolutionOption(resolution.width, resolution.height));
        }

        if (options.Count == 0)
            options.Add(new DisplayResolutionOption(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height)));

        options.Sort((left, right) =>
        {
            int widthComparison = left.Width.CompareTo(right.Width);
            return widthComparison != 0 ? widthComparison : left.Height.CompareTo(right.Height);
        });
        supportedResolutions = options.AsReadOnly();

        string[] names = QualitySettings.names;
        if (names == null || names.Length == 0)
            names = new[] { "Default" };
        qualityLevelNames = Array.AsReadOnly(names);

        defaultResolution = FindClosestSupportedResolution(Screen.width, Screen.height,
            GameDisplayMode.Fullscreen);
        defaultQualityLevel = ClampQualityLevel(QualitySettings.GetQualityLevel());
    }

    private void LoadMixer()
    {
        mixer = Resources.Load<AudioMixer>(MixerResourceName);
        if (mixer == null)
        {
            Debug.LogError($"SettingsService could not load Resources/{MixerResourceName}.mixer; volume controls are unavailable.");
            return;
        }

        MusicOutputGroup = FindGroup("Music");
        SfxOutputGroup = FindGroup("SFX");
        if (MusicOutputGroup == null || SfxOutputGroup == null)
            Debug.LogError("W1AudioMixer must contain Music and SFX child groups.");
    }

    private AudioMixerGroup FindGroup(string groupName)
    {
        AudioMixerGroup[] matches = mixer.FindMatchingGroups(groupName);
        foreach (AudioMixerGroup group in matches)
            if (group != null && string.Equals(group.name, groupName, StringComparison.Ordinal))
                return group;
        return null;
    }

    private void LoadAndApplySettings()
    {
        bool loaded = store.TryLoad(out UserSettingsFileData data, out bool recoveredFromBackup, out string loadError);
        SettingsSnapshot loadedSettings = loaded ? CreateSnapshot(data) : CreateDefaultSnapshot();
        appliedSettings = SanitizeSnapshot(loadedSettings);
        draftSettings = appliedSettings;
        draftAudioPreviewActive = false;
        ApplyGraphics(appliedSettings);

        bool dataChanged = !loaded || !CreateFileData(appliedSettings).HasSameValues(data);
        if (recoveredFromBackup || dataChanged)
        {
            bool persisted = store.TryWrite(CreateFileData(appliedSettings), out string writeError);
            LastPersistenceError = persisted ? null : writeError;
        }
        else
        {
            LastPersistenceError = loadError;
        }

        if (!loaded && loadError != null)
            Debug.LogWarning($"User settings could not be loaded ({loadError}); safe defaults were applied.");
    }

    private SettingsSnapshot CreateDefaultSnapshot()
    {
        return new SettingsSnapshot(GameDisplayMode.Fullscreen, defaultResolution,
            defaultQualityLevel, 1f, 1f, 1f);
    }

    private SettingsSnapshot CreateSnapshot(UserSettingsFileData data)
    {
        GameDisplayMode mode = string.Equals(data.displayMode, "Windowed", StringComparison.Ordinal)
            ? GameDisplayMode.Windowed
            : GameDisplayMode.Fullscreen;

        int quality = -1;
        if (!string.IsNullOrEmpty(data.qualityName))
        {
            for (int index = 0; index < qualityLevelNames.Count; index++)
            {
                if (!string.Equals(qualityLevelNames[index], data.qualityName, StringComparison.Ordinal))
                    continue;
                quality = index;
                break;
            }
        }

        if (quality < 0)
            quality = data.qualityLevel;

        return new SettingsSnapshot(mode,
            ResolveSupportedResolution(data.resolutionWidth, data.resolutionHeight, mode),
            quality, data.masterVolume, data.musicVolume, data.sfxVolume);
    }

    private UserSettingsFileData CreateFileData(SettingsSnapshot snapshot)
    {
        return new UserSettingsFileData
        {
            displayMode = snapshot.DisplayMode == GameDisplayMode.Windowed ? "Windowed" : "Fullscreen",
            resolutionWidth = snapshot.Resolution.Width,
            resolutionHeight = snapshot.Resolution.Height,
            qualityLevel = snapshot.QualityLevel,
            qualityName = qualityLevelNames[snapshot.QualityLevel],
            masterVolume = snapshot.MasterVolume,
            musicVolume = snapshot.MusicVolume,
            sfxVolume = snapshot.SfxVolume
        };
    }

    private SettingsSnapshot SanitizeSnapshot(SettingsSnapshot snapshot)
    {
        if (snapshot == null)
            return CreateDefaultSnapshot();

        GameDisplayMode mode = snapshot.DisplayMode == GameDisplayMode.Windowed
            ? GameDisplayMode.Windowed
            : GameDisplayMode.Fullscreen;
        return new SettingsSnapshot(mode,
            ResolveSupportedResolution(snapshot.Resolution.Width, snapshot.Resolution.Height, mode),
            ClampQualityLevel(snapshot.QualityLevel), SanitizeVolume(snapshot.MasterVolume),
            SanitizeVolume(snapshot.MusicVolume), SanitizeVolume(snapshot.SfxVolume));
    }

    private DisplayResolutionOption ResolveSupportedResolution(int width, int height, GameDisplayMode mode)
    {
        if (supportedResolutions != null)
        {
            foreach (DisplayResolutionOption option in supportedResolutions)
                if (option.Width == width && option.Height == height && IsSafeForMode(option, mode))
                    return option;

            if (supportedResolutions.Count > 0)
            {
                if (width > 0 && height > 0)
                    return FindClosestSupportedResolution(width, height, mode);
                return FindClosestSupportedResolution(Screen.width, Screen.height, mode);
            }
        }

        return new DisplayResolutionOption(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
    }

    private DisplayResolutionOption FindClosestSupportedResolution(int targetWidth, int targetHeight,
        GameDisplayMode mode)
    {
        DisplayResolutionOption closest = supportedResolutions[0];
        bool foundSafeOption = false;
        double closestDistance = double.MaxValue;
        foreach (DisplayResolutionOption option in supportedResolutions)
        {
            if (!IsSafeForMode(option, mode))
                continue;

            double widthDifference = option.Width - (double)Mathf.Max(1, targetWidth);
            double heightDifference = option.Height - (double)Mathf.Max(1, targetHeight);
            double distance = widthDifference * widthDifference + heightDifference * heightDifference;
            if (foundSafeOption && distance >= closestDistance)
                continue;

            closest = option;
            closestDistance = distance;
            foundSafeOption = true;
        }

        return foundSafeOption ? closest : supportedResolutions[0];
    }

    private static bool IsSafeForMode(DisplayResolutionOption option, GameDisplayMode mode)
    {
        if (mode != GameDisplayMode.Windowed)
            return true;

        Display display = Display.main;
        int desktopWidth = display != null && display.systemWidth > 0 ? display.systemWidth : Screen.width;
        int desktopHeight = display != null && display.systemHeight > 0 ? display.systemHeight : Screen.height;
        return option.Width <= Mathf.Max(1, desktopWidth) && option.Height <= Mathf.Max(1, desktopHeight);
    }

    private int ClampQualityLevel(int value)
    {
        int count = qualityLevelNames?.Count ?? 1;
        return Mathf.Clamp(value, 0, Mathf.Max(0, count - 1));
    }

    private static float SanitizeVolume(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return 1f;
        return Mathf.Clamp01(value);
    }

    private void ApplyGraphics(SettingsSnapshot snapshot)
    {
        QualitySettings.SetQualityLevel(snapshot.QualityLevel, true);
        FullScreenMode mode = snapshot.DisplayMode == GameDisplayMode.Windowed
            ? FullScreenMode.Windowed
            : FullScreenMode.FullScreenWindow;
        Screen.SetResolution(snapshot.Resolution.Width, snapshot.Resolution.Height, mode);
    }

    private void ApplyAudioPreview()
    {
        if (audioReady)
            ApplyAudio(draftSettings);
    }

    private void ApplyAudio(SettingsSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        if (mixer != null)
        {
            SetMixerVolume(MasterVolumeParameter, snapshot.MasterVolume);
            SetMixerVolume(MusicVolumeParameter, snapshot.MusicVolume);
            SetMixerVolume(SfxVolumeParameter, snapshot.SfxVolume);
        }
        ApplyMuteGates(snapshot);
    }

    private void SetMixerVolume(string parameterName, float normalizedValue)
    {
        if (!mixer.SetFloat(parameterName, NormalizedToDecibels(normalizedValue)))
            Debug.LogError($"W1AudioMixer is missing exposed parameter '{parameterName}'.");
    }

    private void RouteUnassignedSourcesToSfx()
    {
        if (SfxOutputGroup == null)
            return;

        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include);
        foreach (AudioSource source in sources)
            if (source != null && source.outputAudioMixerGroup == null)
                source.outputAudioMixerGroup = SfxOutputGroup;
        ApplyMuteGates(CurrentAudioSettings, sources);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        routeSourcesPending = true;
    }

    private void OnAudioConfigurationChanged(bool deviceWasChanged)
    {
        if (!audioReady)
            return;

        ApplyAudio(CurrentAudioSettings);
        routeSourcesPending = true;
    }

    private SettingsSnapshot CurrentAudioSettings =>
        draftAudioPreviewActive && draftSettings != null ? draftSettings : appliedSettings;

    private void ApplyMuteGates(SettingsSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include);
        ApplyMuteGates(snapshot, sources);
    }

    private void ApplyMuteGates(SettingsSnapshot snapshot, AudioSource[] sources)
    {
        if (snapshot == null)
            return;

        foreach (AudioSource source in sources)
            ApplyMuteGate(source, snapshot);
        RemoveDestroyedMuteStateEntries();
    }

    private void ApplyMuteGate(AudioSource source, SettingsSnapshot snapshot)
    {
        if (source == null || snapshot == null)
            return;

        bool isMusic = MusicOutputGroup != null && source.outputAudioMixerGroup == MusicOutputGroup;
        bool isSfx = SfxOutputGroup != null && source.outputAudioMixerGroup == SfxOutputGroup;
        if (!isMusic && !isSfx)
        {
            if (sourceMuteStateBeforeGate.TryGetValue(source, out bool priorMuteState))
            {
                source.mute = priorMuteState;
                sourceMuteStateBeforeGate.Remove(source);
            }
            return;
        }

        bool masterMuteGate = snapshot.MasterVolume <= 0f;
        bool musicMuteGate = isMusic && snapshot.MusicVolume <= 0f;
        bool sfxMuteGate = isSfx && snapshot.SfxVolume <= 0f;
        bool shouldMute = masterMuteGate || musicMuteGate || sfxMuteGate;
        if (shouldMute)
        {
            if (!sourceMuteStateBeforeGate.ContainsKey(source))
                sourceMuteStateBeforeGate.Add(source, source.mute);
            source.mute = true;
            return;
        }

        if (!sourceMuteStateBeforeGate.TryGetValue(source, out bool wasMuted))
            return;

        source.mute = wasMuted;
        sourceMuteStateBeforeGate.Remove(source);
    }

    private void RemoveDestroyedMuteStateEntries()
    {
        staleMutedSources.Clear();
        foreach (AudioSource source in sourceMuteStateBeforeGate.Keys)
            if (source == null)
                staleMutedSources.Add(source);
        foreach (AudioSource source in staleMutedSources)
            sourceMuteStateBeforeGate.Remove(source);
        staleMutedSources.Clear();
    }

    private void RestoreAllSourceMuteStates()
    {
        foreach (KeyValuePair<AudioSource, bool> entry in sourceMuteStateBeforeGate)
            if (entry.Key != null)
                entry.Key.mute = entry.Value;
        sourceMuteStateBeforeGate.Clear();
    }
}
