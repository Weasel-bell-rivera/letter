using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SaveService : MonoBehaviour
{
    public enum LoadOutcome
    {
        NoProfile,
        MainFile,
        BackupRecovery,
        CorruptBlocked,
        UnsupportedFutureVersion,
        NewProfile
    }

    private static SaveService instance;
    private LocalSaveStore store;
    private SaveData data;
    private bool writing;
    private float retryAt;
    private float activePlayStart;
    private bool gameplayPaused;
    private bool playerOperable;
    private bool applicationPaused;
    private bool applicationFocused;
    private bool playTimeRunning;
    private bool gameplayAuthorized;
    private bool persistentFailureRaised;
    private int consecutiveWriteFailures;
    private long stateRevision;
    private long syncedRevision;
    private const float RetryDelaySeconds = 2f;
    private const int PersistentFailureThreshold = 3;
    private const int WorldCollectibleTotal = 7;
    private static readonly Dictionary<string, int> RegionCollectibleTotals = new(StringComparer.Ordinal)
    {
        ["FIRE"] = 1
    };

    public static SaveService Instance => EnsureInstance();
    public static bool IsReady => instance != null && instance.data != null;
    public SaveData Data => SaveData.Clone(data);
    public bool HasUnsavedChanges => data != null && stateRevision != syncedRevision;
    public bool IsPlayTimeRunning => playTimeRunning;
    public bool CanContinue => data != null;
    public bool GameplayAuthorized => gameplayAuthorized && data != null;
    public bool HasPersistentSaveFailure => persistentFailureRaised && HasUnsavedChanges;
    public bool StartupFlowSuppressed { get; private set; }
    public bool RequiresNewGameConfirmation => data != null || store?.HasAnyProfileFile == true;
    public int TotalCollectibleCount => WorldCollectibleTotal;
    public string LastWriteError { get; private set; }
    public LoadOutcome LastLoadOutcome { get; private set; }

    public event Action SaveStarted;
    public event Action<bool> SaveCompleted;
    public event Action<string> PersistentSaveFailure;
    public event Action<LoadOutcome> ProfileLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => EnsureInstance();

    private static SaveService EnsureInstance()
    {
        if (instance != null) return instance;
        instance = FindAnyObjectByType<SaveService>();
        if (instance == null)
        {
            GameObject host = new("Save Service");
            instance = host.AddComponent<SaveService>();
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
        store ??= new LocalSaveStore(Application.persistentDataPath);
        applicationFocused = Application.isFocused;
        InitializeProfile();
        RefreshPlayTimeTracking();
    }

    private void Update()
    {
        if (HasUnsavedChanges && !writing && Time.unscaledTime >= retryAt) TrySaveNow();
    }

    private void OnApplicationPause(bool paused)
    {
        applicationPaused = paused;
        RefreshPlayTimeTracking();
        if (paused) TrySaveNow();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        applicationFocused = hasFocus;
        RefreshPlayTimeTracking();
        if (!hasFocus) TrySaveNow();
    }

    private void OnApplicationQuit()
    {
        StopPlayTimeTracking();
        TrySaveNow();
    }

    public void SetGameplayPaused(bool paused)
    {
        if (gameplayPaused == paused) return;
        gameplayPaused = paused;
        RefreshPlayTimeTracking();
    }

    public void SetPlayerOperable(bool operable)
    {
        if (playerOperable == operable) return;
        playerOperable = operable;
        RefreshPlayTimeTracking();
    }

    public bool TryPrepareForQuit()
    {
        SetGameplayPaused(true);
        SetPlayerOperable(false);
        return TryFinalSave();
    }

    public bool TryPrepareForTitle()
    {
        SetGameplayPaused(true);
        SetPlayerOperable(false);
        return TryFinalSave();
    }

    public bool HasAbility(string abilityId)
        => data != null && data.unlockedAbilities.Contains(abilityId);

    public bool HasCollected(string pickupId)
        => data != null && data.collectedPermanentIds.Contains(pickupId);

    public bool HasCompletedRoom(string roomId)
        => data != null && data.completedRoomIds.Contains(roomId);

    public bool HasUnlockedRegion(string regionId)
        => data != null && data.unlockedRegionIds.Contains(regionId);

    public bool HasProgressionFlag(string flagId)
        => data != null && data.progressionFlags.Contains(flagId);

    public bool HasLatchedDoorGroup(string doorGroupId)
        => data != null && data.latchedDoorGroupIds.Contains(doorGroupId);

    public bool TryLatchDoorGroup(string doorGroupId)
    {
        if (data == null || !SaveIdRules.IsDoorGroupId(doorGroupId) || HasLatchedDoorGroup(doorGroupId))
            return false;
        data.latchedDoorGroupIds.Add(doorGroupId);
        RequestSave();
        return true;
    }

    public bool TryCompleteRoom(string roomId)
    {
        if (data == null || !SaveIdRules.IsRoomId(roomId) || HasCompletedRoom(roomId)) return false;
        data.completedRoomIds.Add(roomId);
        RequestSave();
        return true;
    }

    public bool TryRecordApprovedRegionUnlock(string regionId)
    {
        if (data == null || !SaveIdRules.IsRegionId(regionId) || HasUnlockedRegion(regionId)) return false;
        data.unlockedRegionIds.Add(regionId);
        RequestSave();
        return true;
    }

    public bool TrySetProgressionFlag(string flagId)
    {
        if (data == null || !SaveIdRules.IsTokenId(flagId) || HasProgressionFlag(flagId)) return false;
        data.progressionFlags.Add(flagId);
        RequestSave();
        return true;
    }

    public bool TryCollectPermanent(string pickupId, PermanentPickupType type, string rewardId = null)
    {
        if (data == null || !PermanentPickupId.IsValid(pickupId, type) || HasCollected(pickupId)) return false;
        if (type == PermanentPickupType.Ability &&
            !string.IsNullOrWhiteSpace(rewardId) && !SaveIdRules.IsTokenId(rewardId)) return false;
        if (type == PermanentPickupType.Progression &&
            !string.IsNullOrWhiteSpace(rewardId) && !SaveIdRules.IsTokenId(rewardId)) return false;

        data.collectedPermanentIds.Add(pickupId);
        if (type == PermanentPickupType.Ability && !string.IsNullOrWhiteSpace(rewardId) &&
            !data.unlockedAbilities.Contains(rewardId)) data.unlockedAbilities.Add(rewardId);
        else if (type == PermanentPickupType.Progression && !string.IsNullOrWhiteSpace(rewardId) &&
                 !data.progressionFlags.Contains(rewardId)) data.progressionFlags.Add(rewardId);
        if (type == PermanentPickupType.Collectible) SyncCollectibleProgress();
        RequestSave();
        return true;
    }

    public void RecordRoomEntered(string roomId, string entranceId)
    {
        if (data == null || !SaveIdRules.IsRoomId(roomId)) return;
        string safeEntrance = SaveIdRules.IsEntranceId(entranceId) ? entranceId : SaveIds.DefaultEntrance;
        if (string.Equals(data.lastRoomId, roomId, StringComparison.Ordinal) &&
            string.Equals(data.lastEntranceId, safeEntrance, StringComparison.Ordinal)) return;
        data.lastRoomId = roomId;
        data.lastEntranceId = safeEntrance;
        RequestSave();
    }

    public int CountCollectedInRoom(string roomId)
        => SaveIdRules.IsRoomId(roomId) ? CountPrefix(roomId + ":COLLECTIBLE:") : 0;

    public int CountCollectedInRegion(string regionId)
        => data == null || !SaveIdRules.IsRegionId(regionId) ? 0 : data.collectedPermanentIds.FindAll(id =>
            id.StartsWith(regionId + "_", StringComparison.Ordinal) &&
            id.Contains(":COLLECTIBLE:", StringComparison.Ordinal)).Count;

    public int CountAllCollectibles()
        => data == null ? 0 : data.collectedPermanentIds.FindAll(id =>
            id.Contains(":COLLECTIBLE:", StringComparison.Ordinal)).Count;

    public RegionProgressData GetRegionCollectibleProgress(string regionId)
    {
        RegionProgressData progress = data?.regionProgress.Find(item =>
            item != null && string.Equals(item.regionId, regionId, StringComparison.Ordinal));
        return progress == null ? null : new RegionProgressData
        {
            regionId = progress.regionId,
            collectedCount = progress.collectedCount,
            totalCount = progress.totalCount
        };
    }

    public bool TrySaveNow()
    {
        if (data == null || writing || !HasUnsavedChanges) return data != null && !HasUnsavedChanges;
        CaptureActivePlayTime();
        writing = true;
        long writeRevision = stateRevision;
        SaveData snapshot = SaveData.Clone(data);
        SaveStarted?.Invoke();
        bool success = store.TryWrite(snapshot, out SaveData persisted, out string error);
        writing = false;

        if (success)
        {
            if (stateRevision == writeRevision) data = persisted;
            syncedRevision = Math.Max(syncedRevision, writeRevision);
            LastWriteError = null;
            consecutiveWriteFailures = 0;
            persistentFailureRaised = false;
        }
        else
        {
            LastWriteError = error;
            consecutiveWriteFailures++;
            retryAt = Time.unscaledTime + RetryDelaySeconds;
            if (consecutiveWriteFailures >= PersistentFailureThreshold && !persistentFailureRaised)
            {
                persistentFailureRaised = true;
                PersistentSaveFailure?.Invoke(error);
            }
        }

        SaveCompleted?.Invoke(success);
        return success;
    }

    public bool StartNewGame(bool confirmedOverwrite)
    {
        if (RequiresNewGameConfirmation && !confirmedOverwrite) return false;
        StopPlayTimeTracking();
        if (!store.PrepareForNewGame(out string preparationError))
        {
            LastWriteError = preparationError;
            return false;
        }

        data = SaveData.CreateNew();
        SyncCollectibleProgress();
        stateRevision = 1;
        syncedRevision = 0;
        consecutiveWriteFailures = 0;
        persistentFailureRaised = false;
        LastWriteError = null;
        LastLoadOutcome = LoadOutcome.NewProfile;
        ProfileLoaded?.Invoke(LastLoadOutcome);
        TrySaveNow();
        return RouteToGameplay(SaveIds.DefaultRoom, SaveIds.DefaultEntrance);
    }

    public bool ContinueGame()
    {
        if (data == null) return false;
        string requestedRoom = data.lastRoomId;
        string entrance = data.lastEntranceId;
        if (!TryResolveBuildScene(requestedRoom, out _))
        {
            requestedRoom = SaveIds.DefaultRoom;
            entrance = SaveIds.DefaultEntrance;
            if (!TryResolveBuildScene(requestedRoom, out _))
            {
                LastWriteError = "The safe fallback room is not available in the build.";
                return false;
            }
            data.lastRoomId = requestedRoom;
            data.lastEntranceId = entrance;
            RequestSave();
        }
        return RouteToGameplay(requestedRoom, entrance);
    }

    public void RetrySave()
    {
        retryAt = 0f;
        TrySaveNow();
    }

    public void ReturnToTitleState()
    {
        gameplayAuthorized = false;
        SetPlayerOperable(false);
        SetGameplayPaused(true);
        FindAnyObjectByType<MirrorPlayer2D>()?.RecallImmediate();
        PlayerController2D player = FindAnyObjectByType<PlayerController2D>();
        player?.SetControlEnabled(false);
    }

    private void InitializeProfile()
    {
        LocalSaveLoadResult result = store.Load();
        stateRevision = 0;
        syncedRevision = 0;
        gameplayAuthorized = false;
        switch (result.Status)
        {
            case LocalSaveLoadStatus.Success:
                data = result.Data;
                LastLoadOutcome = result.RecoveredFromBackup
                    ? LoadOutcome.BackupRecovery
                    : LoadOutcome.MainFile;
                if (result.NeedsRewrite) stateRevision++;
                if (SyncCollectibleProgress()) stateRevision++;
                LastWriteError = null;
                break;
            case LocalSaveLoadStatus.Missing:
                data = null;
                LastLoadOutcome = LoadOutcome.NoProfile;
                LastWriteError = null;
                break;
            case LocalSaveLoadStatus.UnsupportedFutureVersion:
                data = null;
                LastLoadOutcome = LoadOutcome.UnsupportedFutureVersion;
                LastWriteError = result.Error;
                break;
            default:
                data = null;
                LastLoadOutcome = LoadOutcome.CorruptBlocked;
                LastWriteError = result.Error;
                break;
        }
        ProfileLoaded?.Invoke(LastLoadOutcome);
        if (HasUnsavedChanges) TrySaveNow();
    }

    private void RequestSave()
    {
        stateRevision++;
        retryAt = 0f;
    }

    private bool TryFinalSave()
    {
        if (data == null)
        {
            LastWriteError = "Save data is unavailable.";
            SaveCompleted?.Invoke(false);
            return false;
        }
        CaptureActivePlayTime();
        if (!HasUnsavedChanges) RequestSave();
        retryAt = 0f;
        return TrySaveNow();
    }

    private bool RouteToGameplay(string roomId, string entranceId)
    {
        if (data == null || !TryResolveBuildScene(roomId, out string sceneName)) return false;
        string safeEntrance = SaveIdRules.IsEntranceId(entranceId) ? entranceId : SaveIds.DefaultEntrance;
        gameplayAuthorized = true;
        gameplayPaused = false;
        playerOperable = false;
        Time.timeScale = 1f;
        RefreshPlayTimeTracking();
        RoomTransitionState.Request(sceneName, safeEntrance, true);
        try
        {
            SceneManager.LoadScene(sceneName);
            return true;
        }
        catch (Exception exception)
        {
            gameplayAuthorized = false;
            RoomTransitionState.Cancel();
            LastWriteError = $"SceneLoad:{exception.GetType().Name}";
            return false;
        }
    }

    private static bool TryResolveBuildScene(string roomId, out string sceneName)
    {
        List<string> paths = new();
        for (int index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
            paths.Add(SceneUtility.GetScenePathByBuildIndex(index));
        return TryResolveSceneName(roomId, paths, out sceneName);
    }

    public static bool TryResolveSceneName(string roomId, IEnumerable<string> scenePaths,
        out string sceneName)
    {
        sceneName = null;
        if (!SaveIdRules.IsRoomId(roomId) || scenePaths == null) return false;
        foreach (string path in scenePaths)
        {
            string candidate = Path.GetFileNameWithoutExtension(path);
            if (!string.Equals(candidate, roomId, StringComparison.OrdinalIgnoreCase)) continue;
            sceneName = candidate;
            return true;
        }
        return false;
    }

    private int CountPrefix(string prefix)
        => data == null ? 0 : data.collectedPermanentIds.FindAll(id =>
            id.StartsWith(prefix, StringComparison.Ordinal)).Count;

    private bool SyncCollectibleProgress()
    {
        if (data == null) return false;
        bool changed = false;
        foreach (KeyValuePair<string, int> configured in RegionCollectibleTotals)
        {
            RegionProgressData progress = data.regionProgress.Find(item =>
                item != null && string.Equals(item.regionId, configured.Key, StringComparison.Ordinal));
            if (progress == null)
            {
                progress = new RegionProgressData { regionId = configured.Key };
                data.regionProgress.Add(progress);
                changed = true;
            }

            int collected = CountCollectedInRegion(configured.Key);
            if (progress.collectedCount != collected)
            {
                progress.collectedCount = collected;
                changed = true;
            }
            if (progress.totalCount != configured.Value)
            {
                progress.totalCount = configured.Value;
                changed = true;
            }
        }
        data.regionProgress.Sort((left, right) => string.CompareOrdinal(left?.regionId, right?.regionId));
        return changed;
    }

    private void RefreshPlayTimeTracking()
    {
        bool shouldRun = data != null && gameplayAuthorized && playerOperable && !gameplayPaused &&
                         !applicationPaused && applicationFocused;
        if (shouldRun == playTimeRunning) return;
        if (shouldRun)
        {
            activePlayStart = Time.realtimeSinceStartup;
            playTimeRunning = true;
        }
        else
        {
            StopPlayTimeTracking();
        }
    }

    private void CaptureActivePlayTime()
    {
        if (!playTimeRunning || data == null) return;
        float now = Time.realtimeSinceStartup;
        double elapsed = Math.Max(0d, now - activePlayStart);
        activePlayStart = now;
        if (elapsed <= 0d) return;
        data.playTimeSeconds += elapsed;
        stateRevision++;
    }

    private void StopPlayTimeTracking()
    {
        if (!playTimeRunning) return;
        CaptureActivePlayTime();
        playTimeRunning = false;
    }

#if UNITY_INCLUDE_TESTS
    public void ReplaceStateForTests(SaveData replacement, LocalSaveStore replacementStore = null)
    {
        data = SaveData.Clone(replacement);
        store = replacementStore ?? new LocalSaveStore(Path.Combine(
            Application.temporaryCachePath, "W1SaveTests", Guid.NewGuid().ToString("N")));
        stateRevision = 0;
        syncedRevision = 0;
        LastWriteError = null;
        gameplayPaused = false;
        playerOperable = false;
        applicationPaused = false;
        applicationFocused = true;
        gameplayAuthorized = data != null;
        StartupFlowSuppressed = true;
        playTimeRunning = false;
        consecutiveWriteFailures = 0;
        persistentFailureRaised = false;
        if (data != null) SyncCollectibleProgress();
        RefreshPlayTimeTracking();
    }

    public void ReplaceStoreForTests(LocalSaveStore replacementStore)
    {
        store = replacementStore;
        retryAt = 0f;
    }

    public void ReloadFromStoreForTests(LocalSaveStore replacementStore)
    {
        store = replacementStore;
        StopPlayTimeTracking();
        InitializeProfile();
    }
#endif
}
