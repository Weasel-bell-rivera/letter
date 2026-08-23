using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

public sealed class Fire008PlayModeTests
{
    [SetUp]
    public void SetUp() => SaveService.Instance.ReplaceStateForTests(CreateUnlockedState());

    [UnityTest]
    public IEnumerator SceneLoadsWithThreeStagesAndApprovedMechanicsOnly()
    {
        SceneManager.LoadScene("Fire_008");
        yield return null;
        yield return new WaitForFixedUpdate();

        Assert.That(Object.FindObjectsByType<PermanentLatchDoorGroup2D>(FindObjectsSortMode.None), Has.Length.EqualTo(3));
        Assert.That(Object.FindObjectsByType<PressurePlate2D>(FindObjectsSortMode.None), Has.Length.EqualTo(6));
        Assert.That(Object.FindObjectsByType<Door2D>(FindObjectsSortMode.None), Has.Length.EqualTo(3));
        Assert.That(Object.FindObjectsByType<Checkpoint2D>(FindObjectsSortMode.None), Has.Length.EqualTo(2));
        Assert.That(Object.FindObjectsByType<EruptionHazard2D>(FindObjectsSortMode.None), Is.Empty);
        Assert.That(Object.FindObjectsByType<GravityDisablePickup2D>(FindObjectsSortMode.None), Is.Empty);

        Hazard2D[] hazards = Object.FindObjectsByType<Hazard2D>(FindObjectsSortMode.None);
        Assert.That(hazards, Has.Length.EqualTo(1), "Both lava gaps must share the fixed Hazard Tilemap.");
        TilemapCollider2D hazardCollider = hazards[0].GetComponent<TilemapCollider2D>();
        Assert.That(hazardCollider.isTrigger, Is.True);
        Assert.That(hazardCollider.bounds.size.sqrMagnitude, Is.GreaterThan(0f));
        SurfaceSemantic2D hazardSemantic = hazards[0].GetComponent<SurfaceSemantic2D>();
        Assert.That(hazardSemantic.Type, Is.EqualTo(SurfaceSemantic2D.SurfaceType.Hazard));
        Assert.That(hazardSemantic.IsStatic, Is.True);
        Assert.That(hazardSemantic.IsSafe, Is.False);
        CompositeCollider2D terrain = Object.FindFirstObjectByType<CompositeCollider2D>();
        Assert.That(terrain.pathCount, Is.GreaterThan(0));
        SurfaceSemantic2D terrainSemantic = terrain.GetComponent<SurfaceSemantic2D>();
        Assert.That(terrainSemantic.Type, Is.EqualTo(SurfaceSemantic2D.SurfaceType.StaticSolid));
        Assert.That(terrainSemantic.IsStatic, Is.True);
        Assert.That(terrainSemantic.IsSafe, Is.True);
        Assert.That(Physics2D.Raycast(new Vector2(-12f, 6.5f), Vector2.down, 1.5f).collider, Is.Null,
            "D1 must open into the left descent shaft.");
        Assert.That(Physics2D.Raycast(new Vector2(0f, 6.5f), Vector2.down, 1.5f).collider, Is.EqualTo(terrain),
            "The upper/middle separator must remain solid away from D1.");
        Assert.That(Physics2D.Raycast(new Vector2(12f, -3.5f), Vector2.down, 1.5f).collider, Is.Null,
            "D2 must open into the right descent shaft.");
        Assert.That(Physics2D.Raycast(new Vector2(0f, -3.5f), Vector2.down, 1.5f).collider, Is.EqualTo(terrain),
            "The middle/lower separator must remain solid away from D2.");
        RoomExit2D exit = Object.FindFirstObjectByType<RoomExit2D>();
        Assert.That(exit.TargetScene, Is.EqualTo("Fire_009"));
    }

    [UnityTest]
    public IEnumerator AllThreeSavedDoorGroupsRestoreLatched()
    {
        SaveData data = SaveData.CreateNew();
        data.unlockedAbilities.Add(SaveIds.MirrorAbility);
        data.collectedPermanentIds.Add(SaveIds.MirrorPickup);
        data.latchedDoorGroupIds.AddRange(new[]
        {
            SaveIds.Fire008DoorGroup01,
            SaveIds.Fire008DoorGroup02,
            SaveIds.Fire008DoorGroup03
        });
        SaveService.Instance.ReplaceStateForTests(data);

        SceneManager.LoadScene("Fire_008");
        yield return null;
        yield return new WaitForFixedUpdate();

        PermanentLatchDoorGroup2D[] groups = Object.FindObjectsByType<PermanentLatchDoorGroup2D>(FindObjectsSortMode.None);
        Assert.That(groups.All(group => group.IsLatched), Is.True);
        Assert.That(Object.FindObjectsByType<Door2D>(FindObjectsSortMode.None).All(door => door.IsOpen), Is.True);
        Assert.That(Object.FindObjectsByType<PressurePlate2D>(FindObjectsSortMode.None).All(plate => plate.IsLatchedVisual), Is.True);
    }

    [UnityTest]
    public IEnumerator RequestedCrossSceneEntranceSelectsMatchingMarkerAndBindsRoomSystems()
    {
        RoomTransitionState.Request("Fire_008", "FROM_FIRE_009");
        SceneManager.LoadScene("Fire_008");
        yield return null;
        yield return new WaitForFixedUpdate();

        RoomEntrance2D entrance = Object.FindObjectsByType<RoomEntrance2D>()
            .Single(candidate => candidate.EntranceId == "FROM_FIRE_009");
        RoomPlayerSpawner2D spawner = Object.FindAnyObjectByType<RoomPlayerSpawner2D>();
        RoomResetSystem reset = Object.FindAnyObjectByType<RoomResetSystem>();

        Assert.That(spawner.SpawnedEntrance, Is.SameAs(entrance));
        Assert.That(Vector3.Distance(spawner.SpawnedPlayer.transform.position, entrance.transform.position),
            Is.LessThan(.1f), "The dynamic Player may settle slightly after the first physics step.");
        Assert.That(reset.Player, Is.SameAs(spawner.SpawnedPlayer));
        Assert.That(reset.Entrance, Is.SameAs(entrance.transform));
        Assert.That(Camera.main.GetComponent<CameraFollow2D>().Target,
            Is.SameAs(spawner.SpawnedPlayer.transform));
    }

    [UnityTest]
    public IEnumerator AllThreePlatePairsCanPhysicallyLatchTheirDoorGroups()
    {
        SceneManager.LoadScene("Fire_008");
        yield return null;
        yield return new WaitForFixedUpdate();

        PlayerController2D player = Object.FindFirstObjectByType<PlayerController2D>();
        MirrorPlayer2D mirror = player.GetComponent<MirrorPlayer2D>();
        Assert.That(mirror.TryPlace(), Is.True, mirror.LastFailure.ToString());
        Rigidbody2D cloneBody = mirror.Clone.GetComponent<Rigidbody2D>();

        PermanentLatchDoorGroup2D[] groups = Object.FindObjectsByType<PermanentLatchDoorGroup2D>(FindObjectsSortMode.None)
            .OrderBy(group => group.DoorGroupId)
            .ToArray();
        foreach (PermanentLatchDoorGroup2D group in groups)
        {
            Transform plateA = group.transform.Find("PlateA");
            Transform plateB = group.transform.Find("PlateB");
            player.TeleportTo(plateA.position + Vector3.up * .82f);
            cloneBody.position = plateB.position + Vector3.up * .82f;
            cloneBody.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(group.IsLatched, Is.True, $"{group.DoorGroupId} did not latch from its physical plate pair.");
        }

        Assert.That(groups.All(group => SaveService.Instance.HasLatchedDoorGroup(group.DoorGroupId)), Is.True);
        mirror.RecallImmediate();
    }

    [UnityTest]
    public IEnumerator EntranceSupportsGroundMirrorPlacementRecallAndReplacement()
    {
        SceneManager.LoadScene("Fire_008");
        yield return null;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        MirrorPlayer2D mirror = Object.FindFirstObjectByType<MirrorPlayer2D>();
        PlayerController2D player = mirror.GetComponent<PlayerController2D>();
        Camera camera = Camera.main;
        CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
        follow.SnapToTarget();
        Vector3 cameraPosition = camera.transform.position;
        float cameraSize = camera.orthographicSize;
        TilemapCollider2D terrain = Object.FindObjectsByType<TilemapCollider2D>(FindObjectsSortMode.None)
            .Single(collider => !collider.isTrigger);
        CompositeCollider2D composite = terrain.GetComponent<CompositeCollider2D>();
        Assert.That(player.IsGroundedNow, Is.True,
            $"Entrance must be grounded. player={player.transform.position}, terrainBounds={terrain.bounds}, " +
            $"compositeBounds={composite.bounds}, terrainEnabled={terrain.enabled}, compositeEnabled={composite.enabled}.");
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        Assert.That(mirror.TryPlace(), Is.True, mirror.LastFailure.ToString());
        Assert.That(follow.Target, Is.SameAs(player.transform));
        Assert.That(camera.transform.position, Is.EqualTo(cameraPosition));
        Assert.That(camera.orthographicSize, Is.EqualTo(cameraSize));
        Assert.That(mirror.TryPlace(), Is.False);
        Assert.That(mirror.LastFailure, Is.EqualTo(MirrorPlayer2D.PlacementFailure.AlreadyPlaced));
        mirror.RecallImmediate();
        yield return null;
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        Assert.That(mirror.TryPlace(), Is.True, mirror.LastFailure.ToString());
        mirror.RecallImmediate();
    }

    [UnityTest]
    public IEnumerator CameraStopsAtEachRoomEdgeAndResumesInside()
    {
        SceneManager.LoadScene("Fire_008");
        yield return null;

        Camera camera = Camera.main;
        CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
        PlayerController2D player = Object.FindFirstObjectByType<PlayerController2D>();
        camera.aspect = 16f / 9f;

        player.TeleportTo(new Vector3(-13f, 0f, 0f));
        follow.SnapToTarget();
        Assert.That(camera.transform.position.x - camera.orthographicSize * camera.aspect,
            Is.EqualTo(-14f).Within(.001f));

        float leftEdgeCameraX = camera.transform.position.x;
        player.TeleportTo(Vector3.zero);
        yield return new WaitForEndOfFrame();
        Assert.That(camera.transform.position.x, Is.GreaterThan(leftEdgeCameraX),
            "Camera must resume following when the player leaves the edge clamp region.");

        player.TeleportTo(new Vector3(13f, 0f, 0f));
        follow.SnapToTarget();
        Assert.That(camera.transform.position.x + camera.orthographicSize * camera.aspect,
            Is.EqualTo(15f).Within(.001f));

        player.TeleportTo(new Vector3(0f, 13f, 0f));
        follow.SnapToTarget();
        Assert.That(camera.transform.position.y + camera.orthographicSize,
            Is.EqualTo(14f).Within(.001f));

        player.TeleportTo(new Vector3(0f, -13f, 0f));
        follow.SnapToTarget();
        Assert.That(camera.transform.position.y - camera.orthographicSize,
            Is.EqualTo(-14f).Within(.001f));

        player.TeleportTo(Vector3.zero);
        follow.SnapToTarget();
        Assert.That(camera.transform.position.x, Is.EqualTo(0f).Within(.001f));
        Assert.That(camera.transform.position.y, Is.EqualTo(0f).Within(.001f));
    }

    [UnityTest]
    public IEnumerator PlayerDeathResetReturnsToLatestCheckpointAndClearsClone()
    {
        SceneManager.LoadScene("Fire_008");
        yield return null;
        yield return new WaitForFixedUpdate();

        RoomResetSystem reset = Object.FindFirstObjectByType<RoomResetSystem>();
        Camera camera = Camera.main;
        camera.aspect = 16f / 9f;
        PlayerController2D player = reset.Player;
        MirrorPlayer2D mirror = player.GetComponent<MirrorPlayer2D>();
        Vector3 checkpoint = new(11.9f, -8.08f, 0f);
        reset.SetCheckpoint(checkpoint);
        player.TeleportTo(new Vector3(0f, -8.08f, 0f));
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        Assert.That(mirror.TryPlace(), Is.True, mirror.LastFailure.ToString());

        reset.ResetRoom();
        Assert.That(player.transform.position, Is.EqualTo(checkpoint));
        Assert.That(mirror.Clone, Is.Null);
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        Assert.That(camera.transform.position.x + camera.orthographicSize * camera.aspect,
            Is.EqualTo(15f).Within(.001f), "Reset must restore the checkpoint camera framing at the right edge.");
        Assert.That(camera.transform.position.y - camera.orthographicSize,
            Is.EqualTo(-14f).Within(.001f), "Reset must restore the checkpoint camera framing at the bottom edge.");
    }

    private static SaveData CreateUnlockedState()
    {
        SaveData data = SaveData.CreateNew();
        data.unlockedAbilities.Add(SaveIds.MirrorAbility);
        data.collectedPermanentIds.Add(SaveIds.MirrorPickup);
        return data;
    }
}
