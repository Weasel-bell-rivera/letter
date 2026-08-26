using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class RoomPlayerSpawner2D : MonoBehaviour
{
    [SerializeField] private bool spawnOnAwake = true;

    public PlayerController2D SpawnedPlayer { get; private set; }
    public RoomEntrance2D SpawnedEntrance { get; private set; }

    private void Awake()
    {
        if (spawnOnAwake) SpawnPlayer();
    }

    public PlayerController2D SpawnPlayer()
    {
        if (SpawnedPlayer != null) return SpawnedPlayer;

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
        string requestedId = RoomTransitionState.ConsumeEntrance(scene.name);
        SpawnedEntrance = SelectEntrance(entrances, requestedId);
        if (SpawnedEntrance == null)
        {
            Debug.LogError($"{scene.name} has no valid default RoomEntrance2D.", this);
            return null;
        }

        GameObject instance = Instantiate(registry.PlayerPrefab, SpawnedEntrance.transform.position,
            Quaternion.identity);
        instance.name = "Player";
        SceneManager.MoveGameObjectToScene(instance, scene);
        SpawnedPlayer = instance.GetComponent<PlayerController2D>();
        MirrorPlayer2D mirror = instance.GetComponent<MirrorPlayer2D>();
        SpawnedPlayer.SetControlEnabled(false);
        SpawnedPlayer.SetFacing(SpawnedEntrance.FacingRight);

        Physics2D.SyncTransforms();
        if (!IsSpawnPositionClear(SpawnedPlayer))
        {
            Debug.LogError($"{scene.name} entrance {SpawnedEntrance.EntranceId} cannot contain the full Player collider.",
                SpawnedEntrance);
            instance.SetActive(false);
            Destroy(instance);
            SpawnedPlayer = null;
            return null;
        }

        RoomResetSystem reset = FindInScene<RoomResetSystem>(scene).FirstOrDefault();
        CameraFollow2D cameraFollow = FindInScene<CameraFollow2D>(scene).FirstOrDefault();
        reset?.Configure(SpawnedPlayer, mirror, SpawnedEntrance.transform, cameraFollow);
        if (cameraFollow != null)
        {
            cameraFollow.Configure(SpawnedPlayer.transform, cameraFollow.FollowsVertical);
            cameraFollow.BeginEntryFraming();
        }

        Physics2D.SyncTransforms();
        SpawnedPlayer.SetControlEnabled(true);
        return SpawnedPlayer;
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
            if (overlap == body || overlap.isTrigger || overlap.GetComponent<PlayerController2D>() != null)
                continue;
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

    public static void Request(string sceneName, string entranceId)
    {
        targetScene = sceneName;
        targetEntrance = string.IsNullOrWhiteSpace(entranceId) ? SaveIds.DefaultEntrance : entranceId;
    }

    public static string ConsumeEntrance(string loadedScene)
    {
        if (string.IsNullOrWhiteSpace(targetScene) ||
            !string.Equals(targetScene, loadedScene, StringComparison.OrdinalIgnoreCase))
            return null;

        string result = targetEntrance;
        targetScene = null;
        targetEntrance = null;
        return result;
    }

#if UNITY_INCLUDE_TESTS
    public static void ClearForTests()
    {
        targetScene = null;
        targetEntrance = null;
    }
#endif
}
