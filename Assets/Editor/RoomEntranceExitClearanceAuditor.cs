using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RoomEntranceExitClearanceAuditor
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Gameplay/Characters/Player.prefab";
    private const float RequiredEdgeClearance = 1f;

    [MenuItem("Tools/W1/Validation/Audit Room Entrance Exit Clearance")]
    public static void Audit() => ProcessScenes(false);

    private static void ProcessScenes(bool applyFixes)
    {
        string originalScenePath = SceneManager.GetActiveScene().path;
        Vector2 playerSize = LoadPlayerColliderSize();
        string[] scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes/Levels" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        int issueCount = 0;
        int fixedSceneCount = 0;
        try
        {
            foreach (string scenePath in scenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                GameObject[] roots = scene.GetRootGameObjects();
                RoomEntrance2D[] entrances = roots.SelectMany(root =>
                    root.GetComponentsInChildren<RoomEntrance2D>(true)).ToArray();
                RoomExit2D[] exits = roots.SelectMany(root =>
                    root.GetComponentsInChildren<RoomExit2D>(true)).ToArray();
                Physics2D.SyncTransforms();
                bool sceneChanged = false;

                foreach (RoomEntrance2D entrance in entrances)
                {
                    RoomExit2D exit = FindRelatedExit(entrance, exits);
                    if (exit == null) continue;
                    BoxCollider2D trigger = exit.GetComponent<BoxCollider2D>();
                    if (trigger == null || !trigger.isTrigger) continue;

                    Vector2 entrancePosition = entrance.transform.position;
                    Vector2 exitPosition = trigger.bounds.center;
                    Vector2 centerDelta = entrancePosition - exitPosition;
                    float horizontalGap = Mathf.Abs(centerDelta.x) - trigger.bounds.extents.x - playerSize.x * .5f;
                    float verticalGap = Mathf.Abs(centerDelta.y) - trigger.bounds.extents.y - playerSize.y * .5f;
                    if (horizontalGap + .0001f >= RequiredEdgeClearance ||
                        verticalGap + .0001f >= RequiredEdgeClearance) continue;
                    issueCount++;

                    Vector3 oldPosition = entrance.transform.position;
                    if (!TryFindSafePosition(entrance, trigger, playerSize, out Vector3 newPosition))
                    {
                        Debug.Log($"ROOM_EXIT_CLEARANCE_PROTECTED scene={scenePath} entrance={entrance.EntranceId} exit={exit.name} old={oldPosition} proposed=none reason=no-supported-layout-candidate");
                        continue;
                    }

                    Debug.Log($"ROOM_EXIT_CLEARANCE_PROTECTED scene={scenePath} entrance={entrance.EntranceId} exit={exit.name} old={oldPosition} proposed={newPosition}");
                    if (!applyFixes) continue;

                    Undo.RecordObject(entrance.transform, "Fix room entrance exit clearance");
                    entrance.transform.position = newPosition;
                    EditorUtility.SetDirty(entrance.transform);
                    sceneChanged = true;
                }

                if (applyFixes && sceneChanged)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new InvalidOperationException($"Failed to save {scenePath}");
                    fixedSceneCount++;
                }
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(originalScenePath))
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }

        Debug.Log($"ROOM_EXIT_CLEARANCE_SUMMARY scenes={scenePaths.Length} protectedEntrances={issueCount} fixedScenes={(applyFixes ? fixedSceneCount : 0)} mode={(applyFixes ? "fix" : "audit")}");
    }

    private static bool TryFindSafePosition(RoomEntrance2D entrance, BoxCollider2D trigger,
        Vector2 playerSize, out Vector3 result)
    {
        Vector3 current = entrance.transform.position;
        Vector2 exitCenter = trigger.bounds.center;
        float requiredX = trigger.bounds.extents.x + playerSize.x * .5f + RequiredEdgeClearance;
        float requiredY = trigger.bounds.extents.y + playerSize.y * .5f + RequiredEdgeClearance;
        float preferredHorizontalSign = Mathf.Sign(current.x - exitCenter.x);
        if (Mathf.Approximately(preferredHorizontalSign, 0f))
            preferredHorizontalSign = entrance.FacingRight ? 1f : -1f;
        float preferredVerticalSign = Mathf.Sign(current.y - exitCenter.y);
        if (Mathf.Approximately(preferredVerticalSign, 0f)) preferredVerticalSign = 1f;

        Vector3[] candidates =
        {
            new(exitCenter.x + preferredHorizontalSign * requiredX, current.y, current.z),
            new(exitCenter.x - preferredHorizontalSign * requiredX, current.y, current.z),
            new(current.x, exitCenter.y + preferredVerticalSign * requiredY, current.z),
            new(current.x, exitCenter.y - preferredVerticalSign * requiredY, current.z)
        };

        foreach (Vector3 candidate in candidates)
        {
            if (!CanContainPlayer(candidate, playerSize)) continue;
            result = candidate;
            return true;
        }

        result = current;
        return false;
    }

    private static bool CanContainPlayer(Vector3 position, Vector2 playerSize)
    {
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(position, playerSize * .9f, 0f);
        if (overlaps.Any(collider => collider != null && collider.enabled && !collider.isTrigger))
            return false;

        Vector2 rayOrigin = (Vector2)position + Vector2.down * (playerSize.y * .45f);
        RaycastHit2D[] supportHits = Physics2D.RaycastAll(rayOrigin, Vector2.down, playerSize.y * .15f);
        return supportHits.Any(hit => hit.collider != null && hit.collider.enabled && !hit.collider.isTrigger);
    }

    private static RoomExit2D FindRelatedExit(RoomEntrance2D entrance, IReadOnlyCollection<RoomExit2D> exits)
    {
        string entranceId = entrance.EntranceId;
        if (!string.IsNullOrEmpty(entranceId) && entranceId.StartsWith("FROM_", StringComparison.Ordinal))
        {
            string sourceRoom = NormalizeRoomId(entranceId.Substring("FROM_".Length));
            RoomExit2D exact = exits.FirstOrDefault(exit => NormalizeRoomId(exit.TargetScene) == sourceRoom);
            if (exact != null) return exact;
        }

        return entrance.IsDefault
            ? exits.OrderBy(exit => Vector2.SqrMagnitude((Vector2)(entrance.transform.position - exit.transform.position))).FirstOrDefault()
            : null;
    }

    private static string NormalizeRoomId(string value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Replace('-', '_').ToUpperInvariant();

    private static Vector2 LoadPlayerColliderSize()
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab == null)
            throw new InvalidOperationException($"Missing Player Prefab: {PlayerPrefabPath}");
        BoxCollider2D collider = playerPrefab.GetComponent<BoxCollider2D>();
        if (collider == null)
            throw new InvalidOperationException("Player Prefab is missing BoxCollider2D.");
        return Vector2.Scale(collider.size, playerPrefab.transform.localScale);
    }
}
