using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class GroundConveyorPlayModeTests
{
    [SetUp]
    public void ResetAbilityState() => MirrorAbilityState.ResetForTests();

    [UnityTest]
    public IEnumerator PlayerAndNormalGravityCloneReceiveSameWorldSurfaceVelocity()
    {
        PlayerMovementSettings settings = CreateSettings();
        GroundConveyor2D conveyor = CreateConveyor(GroundConveyor2D.BeltDirection.Right, 2.5f);
        PlayerController2D player = CreatePlayer(new Vector2(-.65f, 1.15f), settings);
        MirrorCloneController2D clone = CreateClone(new Vector2(.65f, 1.15f), player, Vector2.left, Vector2.down);
        Physics2D.SyncTransforms();

        for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();

        Assert.That(player.AppliedSurfaceVelocity.x, Is.EqualTo(2.5f).Within(.02f));
        Assert.That(clone.AppliedSurfaceVelocity.x, Is.EqualTo(2.5f).Within(.02f));
        Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(2.5f).Within(.08f));
        Assert.That(clone.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(2.5f).Within(.08f));

        DestroyObjects(settings, conveyor.gameObject, player.gameObject, clone.gameObject);
    }

    [UnityTest]
    public IEnumerator SidewaysGravityCloneDoesNotReceiveHorizontalBeltMotionFromSideContact()
    {
        PlayerMovementSettings settings = CreateSettings();
        GroundConveyor2D conveyor = CreateConveyor(GroundConveyor2D.BeltDirection.Right, 2.5f);
        PlayerController2D source = CreatePlayer(new Vector2(10f, 10f), settings);
        MirrorCloneController2D clone = CreateClone(new Vector2(-2.4f, 0f), source, Vector2.up, Vector2.right);
        Physics2D.SyncTransforms();

        for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();

        Assert.That(clone.AppliedSurfaceVelocity, Is.EqualTo(Vector2.zero));
        DestroyObjects(settings, conveyor.gameObject, source.gameObject, clone.gameObject);
    }

    [UnityTest]
    public IEnumerator DisableRemovesSurfaceContributionAndResetRestoresInitialState()
    {
        PlayerMovementSettings settings = CreateSettings();
        GroundConveyor2D conveyor = CreateConveyor(GroundConveyor2D.BeltDirection.Left, 2f);
        PlayerController2D player = CreatePlayer(new Vector2(0f, 1.15f), settings);
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.That(player.AppliedSurfaceVelocity.x, Is.EqualTo(-2f).Within(.02f));

        conveyor.SetActive(false);
        yield return new WaitForFixedUpdate();
        Assert.That(player.AppliedSurfaceVelocity, Is.EqualTo(Vector2.zero));
        Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(0f).Within(.08f));

        conveyor.ResetRoomState();
        yield return new WaitForFixedUpdate();
        Assert.That(conveyor.IsActive, Is.True);
        Assert.That(player.AppliedSurfaceVelocity.x, Is.EqualTo(-2f).Within(.02f));
        DestroyObjects(settings, conveyor.gameObject, player.gameObject);
    }

    [UnityTest]
    public IEnumerator MirrorPlacementIsRejectedOnConveyorWithoutChangingConveyorState()
    {
        PlayerMovementSettings settings = CreateSettings();
        GroundConveyor2D conveyor = CreateConveyor(GroundConveyor2D.BeltDirection.Right, 2f);
        PlayerController2D player = CreatePlayer(new Vector2(0f, 1.15f), settings);
        MirrorPlayer2D mirror = player.gameObject.AddComponent<MirrorPlayer2D>();
        mirror.Configure(player);
        mirror.SetInitiallyUnlocked(true);
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();

        Assert.That(mirror.TryPlace(), Is.False);
        Assert.That(mirror.LastFailure, Is.EqualTo(MirrorPlayer2D.PlacementFailure.NoSurface));
        Assert.That(mirror.Clone, Is.Null);
        Assert.That(conveyor.IsActive, Is.True);
        DestroyObjects(settings, conveyor.gameObject, player.gameObject);
    }

    [UnityTest]
    public IEnumerator RoomResetClearsPlayerSurfaceStateAndRestoresConveyor()
    {
        PlayerMovementSettings settings = CreateSettings();
        GroundConveyor2D conveyor = CreateConveyor(GroundConveyor2D.BeltDirection.Right, 2f);
        PlayerController2D player = CreatePlayer(new Vector2(0f, 1.15f), settings);
        MirrorPlayer2D mirror = player.gameObject.AddComponent<MirrorPlayer2D>();
        mirror.Configure(player);
        GameObject room = new("Room");
        GameObject entrance = new("Entrance");
        entrance.transform.SetParent(room.transform);
        entrance.transform.position = new Vector2(10f, 10f);
        RoomResetSystem reset = room.AddComponent<RoomResetSystem>();
        reset.Configure(player, mirror, entrance.transform);
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.That(player.AppliedSurfaceVelocity.x, Is.EqualTo(2f).Within(.02f));

        conveyor.SetActive(false);
        reset.ResetRoom();

        Assert.That(conveyor.IsActive, Is.True);
        Assert.That(player.AppliedSurfaceVelocity, Is.EqualTo(Vector2.zero));
        Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity, Is.EqualTo(Vector2.zero));
        Assert.That(Vector2.Distance(player.transform.position, entrance.transform.position), Is.LessThan(.001f));
        DestroyObjects(settings, conveyor.gameObject, player.gameObject, room);
    }

    [Test]
    public void SurfaceVelocityOnlyComesFromUpwardSupportFace()
    {
        GroundConveyor2D conveyor = CreateConveyor(GroundConveyor2D.BeltDirection.Right, 3f);

        Assert.That(conveyor.TryGetSurfaceVelocity(Vector2.zero, Vector2.up, out Vector2 topVelocity), Is.True);
        Assert.That(topVelocity, Is.EqualTo(Vector2.right * 3f));
        Assert.That(conveyor.TryGetSurfaceVelocity(Vector2.zero, Vector2.left, out Vector2 sideVelocity), Is.False);
        Assert.That(sideVelocity, Is.EqualTo(Vector2.zero));
        Object.DestroyImmediate(conveyor.gameObject);
    }

    private static GroundConveyor2D CreateConveyor(GroundConveyor2D.BeltDirection direction, float speed)
    {
        GameObject conveyorObject = new("GroundConveyor2D");
        conveyorObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        BoxCollider2D collider = conveyorObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(4f, .5f);
        conveyorObject.AddComponent<SurfaceSemantic2D>();
        GroundConveyor2D conveyor = conveyorObject.AddComponent<GroundConveyor2D>();
        conveyor.Configure(direction, speed, true);
        return conveyor;
    }

    private static PlayerController2D CreatePlayer(Vector2 position, PlayerMovementSettings settings)
    {
        GameObject playerObject = new("Player");
        playerObject.transform.position = position;
        playerObject.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.8f);
        playerObject.AddComponent<Rigidbody2D>();
        PlayerController2D player = playerObject.AddComponent<PlayerController2D>();
        player.Configure(null, settings);
        return player;
    }

    private static MirrorCloneController2D CreateClone(Vector2 position, PlayerController2D source,
        Vector2 moveAxis, Vector2 gravityAxis)
    {
        GameObject cloneObject = new("MirrorClone");
        cloneObject.transform.position = position;
        cloneObject.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.8f);
        cloneObject.AddComponent<Rigidbody2D>();
        MirrorCloneController2D clone = cloneObject.AddComponent<MirrorCloneController2D>();
        clone.Configure(source, moveAxis, gravityAxis);
        return clone;
    }

    private static PlayerMovementSettings CreateSettings()
        => ScriptableObject.CreateInstance<PlayerMovementSettings>();

    private static void DestroyObjects(PlayerMovementSettings settings, params GameObject[] objects)
    {
        foreach (GameObject target in objects) Object.DestroyImmediate(target);
        Object.DestroyImmediate(settings);
    }
}
