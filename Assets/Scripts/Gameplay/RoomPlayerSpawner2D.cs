using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class RoomPlayerSpawner2D : MonoBehaviour
{
    [SerializeField] private bool spawnOnAwake = true;
    [SerializeField] private CameraFollow2D roomCamera;

    public PlayerController2D SpawnedPlayer { get; private set; }
    public RoomEntrance2D SpawnedEntrance { get; private set; }
    public CameraFollow2D RoomCamera => roomCamera;

    public void ConfigureCamera(CameraFollow2D cameraController) => roomCamera = cameraController;

    private void Awake()
    {
        if (spawnOnAwake) SpawnPlayer();
    }

    public PlayerController2D SpawnPlayer()
    {
        if (SpawnedPlayer != null) return SpawnedPlayer;
        SaveService save = SaveService.Instance;
        save.SetPlayerOperable(false);
        if (!save.GameplayAuthorized) return null;

        Scene scene = gameObject.scene;
        PlayerController2D existing = FindInScene<PlayerController2D>(scene).FirstOrDefault();
        if (existing != null)
        {
            Debug.LogError($"{scene.name} contains a serialized Player. Rooms must use RoomPlayerSpawner2D instead.", existing);
            return null;
        }

        PlayerPrefabRegistry registry = Resources.Load<PlayerPrefabRegistry>(PlayerPrefabRegistry.ResourcesPath);
        string registryError = null;
        if (registry == null || !registry.IsValid(out registryError))
        {
            Debug.LogError(registry == null ? "Player Prefab Registry resource is missing." : registryError, this);
            return null;
        }

        RoomEntrance2D[] entrances = FindInScene<RoomEntrance2D>(scene).ToArray();
        bool shouldRecordContinuation = RoomTransitionState.TryConsumeEntrance(scene.name,
            out string requestedId, out string completedSourceRoomId);
        SpawnedEntrance = SelectEntrance(entrances, requestedId);
        if (SpawnedEntrance == null)
        {
            Debug.LogError($"{scene.name} has no valid default RoomEntrance2D.", this);
            return null;
        }

        GameObject instance = CreatePlayerInstance(registry.PlayerPrefab, SpawnedEntrance, scene);
        SpawnedPlayer = instance.GetComponent<PlayerController2D>();
        Physics2D.SyncTransforms();
        if (!IsSpawnPositionClear(SpawnedPlayer) && !SpawnedEntrance.IsDefault)
        {
            instance.SetActive(false);
            Destroy(instance);
            SpawnedEntrance = entrances.FirstOrDefault(entrance => entrance.IsDefault);
            if (SpawnedEntrance != null)
            {
                instance = CreatePlayerInstance(registry.PlayerPrefab, SpawnedEntrance, scene);
                SpawnedPlayer = instance.GetComponent<PlayerController2D>();
                Physics2D.SyncTransforms();
            }
        }
        if (SpawnedPlayer == null || !IsSpawnPositionClear(SpawnedPlayer))
        {
            Debug.LogError($"{scene.name} has no safe requested or default RoomEntrance2D.", this);
            if (instance != null)
            {
                instance.SetActive(false);
                Destroy(instance);
            }
            SpawnedPlayer = null;
            return null;
        }

        MirrorPlayer2D mirror = instance.GetComponent<MirrorPlayer2D>();

        RoomResetSystem reset = FindInScene<RoomResetSystem>(scene).FirstOrDefault();
        CameraFollow2D cameraFollow = ResolveRoomCamera(scene);
        reset?.Configure(SpawnedPlayer, mirror, SpawnedEntrance.transform, cameraFollow);
        if (cameraFollow != null)
        {
            cameraFollow.Configure(SpawnedPlayer.transform, cameraFollow.FollowsVertical);
            cameraFollow.BeginEntryFraming();
        }

        Physics2D.SyncTransforms();
        SpawnedPlayer.SetControlEnabled(true);
        save.SetPlayerOperable(true);
        RoomTransitionState.CommitSuccessfulSpawn(save, scene.name.ToUpperInvariant(),
            SpawnedEntrance.EntranceId, shouldRecordContinuation, completedSourceRoomId);
        return SpawnedPlayer;
    }

    private static GameObject CreatePlayerInstance(GameObject prefab, RoomEntrance2D entrance, Scene scene)
    {
        GameObject instance = Instantiate(prefab, entrance.transform.position, Quaternion.identity);
        instance.name = "Player";
        SceneManager.MoveGameObjectToScene(instance, scene);
        PlayerController2D controller = instance.GetComponent<PlayerController2D>();
        controller.SetControlEnabled(false);
        controller.SetFacing(entrance.FacingRight);
        return instance;
    }

    private CameraFollow2D ResolveRoomCamera(Scene scene)
    {
        if (roomCamera != null && roomCamera.gameObject.scene == scene) return roomCamera;

        CameraFollow2D[] cameras = FindInScene<CameraFollow2D>(scene);
        if (cameras.Length > 1)
        {
            Debug.LogError($"{scene.name} contains multiple CameraFollow2D components. " +
                "Formal rooms must explicitly reference exactly one room camera.", this);
            return null;
        }

        roomCamera = cameras.FirstOrDefault();
        return roomCamera;
    }

    private static RoomEntrance2D SelectEntrance(RoomEntrance2D[] entrances, string requestedId)
    {
        if (!string.IsNullOrWhiteSpace(requestedId))
        {
            RoomEntrance2D requested = entrances.FirstOrDefault(entrance =>
                string.Equals(entrance.EntranceId, requestedId, StringComparison.OrdinalIgnoreCase));
            if (requested != null) return requested;
        }

        return entrances.FirstOrDefault(entrance => entrance.IsDefault);
    }

    private static bool IsSpawnPositionClear(PlayerController2D player)
    {
        BoxCollider2D body = player.GetComponent<BoxCollider2D>();
        if (body == null) return false;
        foreach (Collider2D overlap in Physics2D.OverlapBoxAll(body.bounds.center, body.bounds.size * .95f, 0f))
        {
            if (overlap == body || overlap.GetComponentInParent<PlayerController2D>() != null) continue;
            Hazard2D hazard = overlap.GetComponentInParent<Hazard2D>();
            if (hazard != null && hazard.Active) return false;
            SurfaceSemantic2D semantic = overlap.GetComponent<SurfaceSemantic2D>() ??
                                         overlap.GetComponentInParent<SurfaceSemantic2D>();
            if (semantic != null && (!semantic.IsSafe ||
                                     semantic.Type == SurfaceSemantic2D.SurfaceType.Hazard)) return false;
            if (overlap.isTrigger) continue;
            return false;
        }
        return true;
    }

    private static T[] FindInScene<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .ToArray();
    }
}

public static class RoomTransitionState
{
    private static string targetScene;
    private static string targetEntrance;
    private static bool recordOnSuccessfulSpawn;
    private static string completedSourceRoom;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Cancel();

    public static void Request(string sceneName, string entranceId, bool recordContinuation = true,
        string completedSourceRoomId = null)
    {
        targetScene = sceneName;
        targetEntrance = string.IsNullOrWhiteSpace(entranceId) ? SaveIds.DefaultEntrance : entranceId;
        recordOnSuccessfulSpawn = recordContinuation;
        completedSourceRoom = SaveIdRules.IsRoomId(completedSourceRoomId)
            ? completedSourceRoomId
            : null;
    }

    public static bool TryConsumeEntrance(string loadedScene, out string entranceId)
        => TryConsumeEntrance(loadedScene, out entranceId, out _);

    public static bool TryConsumeEntrance(string loadedScene, out string entranceId,
        out string completedSourceRoomId)
    {
        entranceId = null;
        completedSourceRoomId = null;
        if (string.IsNullOrWhiteSpace(targetScene) ||
            !string.Equals(targetScene, loadedScene, StringComparison.OrdinalIgnoreCase))
            return false;

        entranceId = targetEntrance;
        completedSourceRoomId = completedSourceRoom;
        bool result = recordOnSuccessfulSpawn;
        Cancel();
        return result;
    }

    public static string ConsumeEntrance(string loadedScene)
    {
        TryConsumeEntrance(loadedScene, out string result);
        return result;
    }

    public static void CommitSuccessfulSpawn(SaveService save, string loadedRoomId,
        string actualEntranceId, bool recordContinuation, string completedSourceRoomId)
    {
        if (save == null) return;
        if (recordContinuation) save.RecordRoomEntered(loadedRoomId, actualEntranceId);
        if (SaveIdRules.IsRoomId(completedSourceRoomId))
            save.TryCompleteRoom(completedSourceRoomId);
    }

    public static void Cancel()
    {
        targetScene = null;
        targetEntrance = null;
        recordOnSuccessfulSpawn = false;
        completedSourceRoom = null;
    }

#if UNITY_INCLUDE_TESTS
    public static void ClearForTests()
    {
        Cancel();
    }
#endif
}
