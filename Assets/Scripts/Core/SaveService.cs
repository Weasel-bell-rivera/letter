using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SaveService : MonoBehaviour
{
    public enum LoadOutcome { NewProfile, MainFile, BackupRecovery }

    private static SaveService instance;
    private LocalSaveStore store;
    private SaveData data;
    private bool dirty;
    private bool writing;
    private float retryAt;
    private float activePlayStart;
    private const float RetryDelaySeconds = 2f;

    public static SaveService Instance => EnsureInstance();
    public static bool IsReady => instance != null && instance.data != null;
    public SaveData Data => data;
    public bool HasUnsavedChanges => dirty;
    public string LastWriteError { get; private set; }
    public LoadOutcome LastLoadOutcome { get; private set; }

    public event Action<bool> SaveCompleted;
    public event Action<LoadOutcome> ProfileLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => EnsureInstance();

    private static SaveService EnsureInstance()
    {
        if (instance != null) return instance;
        instance = FindFirstObjectByType<SaveService>();
        if (instance == null)
        {
            GameObject host = new("Save Service");
            instance = host.AddComponent<SaveService>();
        }
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        store ??= new LocalSaveStore(Application.persistentDataPath);
        LoadOrCreate();
        activePlayStart = Time.realtimeSinceStartup;
    }

    private void Update()
    {
        if (dirty && !writing && Time.unscaledTime >= retryAt) TrySaveNow();
    }

    private void OnApplicationPause(bool paused)
    {
        AccumulatePlayTime();
        if (paused) TrySaveNow();
        else activePlayStart = Time.realtimeSinceStartup;
    }

    private void OnApplicationQuit() { AccumulatePlayTime(); TrySaveNow(); }

    public bool HasAbility(string abilityId) => data != null && data.unlockedAbilities.Contains(abilityId);
    public bool HasCollected(string pickupId) => data != null && data.collectedPermanentIds.Contains(pickupId);

    public bool TryCollectPermanent(string pickupId, PermanentPickupType type, string rewardId = null)
    {
        if (data == null || !PermanentPickupId.IsValid(pickupId, type) || HasCollected(pickupId)) return false;
        data.collectedPermanentIds.Add(pickupId);
        if (type == PermanentPickupType.Ability && !string.IsNullOrWhiteSpace(rewardId) && !data.unlockedAbilities.Contains(rewardId))
            data.unlockedAbilities.Add(rewardId);
        else if (type == PermanentPickupType.Progression && !string.IsNullOrWhiteSpace(rewardId) && !data.progressionFlags.Contains(rewardId))
            data.progressionFlags.Add(rewardId);
        MarkDirtyAndSave();
        return true;
    }

    public void RecordRoomEntered(string roomId, string entranceId)
    {
        if (data == null || string.IsNullOrWhiteSpace(roomId)) return;
        data.lastRoomId = roomId;
        data.lastEntranceId = string.IsNullOrWhiteSpace(entranceId) ? SaveIds.DefaultEntrance : entranceId;
        MarkDirtyAndSave();
    }

    public int CountCollectedInRoom(string roomId) => CountPrefix(roomId + ":COLLECTIBLE:");
    public int CountCollectedInRegion(string regionId) => CountPrefix(regionId + "_");
    public int CountAllCollectibles() => data == null ? 0 : data.collectedPermanentIds.FindAll(id => id.Contains(":COLLECTIBLE:")).Count;

    public bool TrySaveNow()
    {
        if (data == null || writing || !dirty) return !dirty;
        writing = true;
        bool success = store.TryWrite(data, out string error);
        writing = false;
        LastWriteError = success ? null : error;
        dirty = !success;
        if (!success) retryAt = Time.unscaledTime + RetryDelaySeconds;
        SaveCompleted?.Invoke(success);
        return success;
    }

    public bool StartNewGame(bool confirmedOverwrite)
    {
        bool hasExistingProgress = data != null && (data.collectedPermanentIds.Count > 0 || data.playTimeSeconds > 1);
        if (hasExistingProgress && !confirmedOverwrite) return false;
        store.PreserveAndDeleteForNewGame();
        data = SaveData.CreateNew();
        LastLoadOutcome = LoadOutcome.NewProfile;
        dirty = true;
        return TrySaveNow();
    }

    public void RetrySave() { retryAt = 0; TrySaveNow(); }

    private void LoadOrCreate()
    {
        if (store.TryLoad(out data, out bool recovered, out bool needsRewrite, out string error))
        {
            LastLoadOutcome = recovered ? LoadOutcome.BackupRecovery : LoadOutcome.MainFile;
            dirty = needsRewrite;
            LastWriteError = null;
        }
        else
        {
            data = SaveData.CreateNew();
            LastLoadOutcome = LoadOutcome.NewProfile;
            LastWriteError = error;
            dirty = true;
        }
        ProfileLoaded?.Invoke(LastLoadOutcome);
        if (dirty) TrySaveNow();
    }

    private void MarkDirtyAndSave() { dirty = true; retryAt = 0; TrySaveNow(); }
    private int CountPrefix(string prefix) => data == null ? 0 : data.collectedPermanentIds.FindAll(id => id.StartsWith(prefix, StringComparison.Ordinal)).Count;
    private void AccumulatePlayTime()
    {
        if (data == null) return;
        float now = Time.realtimeSinceStartup;
        data.playTimeSeconds += Math.Max(0, now - activePlayStart);
        activePlayStart = now;
        dirty = true;
    }

#if UNITY_INCLUDE_TESTS
    public void ReplaceStateForTests(SaveData replacement, LocalSaveStore replacementStore = null)
    {
        data = replacement;
        store = replacementStore ?? new LocalSaveStore(System.IO.Path.Combine(
            Application.temporaryCachePath, "W1SaveTests", Guid.NewGuid().ToString("N")));
        dirty = false;
        LastWriteError = null;
    }
#endif
}
