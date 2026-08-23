using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PermanentLatchDoorGroupPlayModeTests
{
    private GameObject root;

    [SetUp]
    public void SetUp()
    {
        SaveService.Instance.ReplaceStateForTests(SaveData.CreateNew());
        root = new GameObject("Permanent Door Group Test");
    }

    [TearDown]
    public void TearDown()
    {
        if (root != null) Object.DestroyImmediate(root);
    }

    [UnityTest]
    public IEnumerator OnePlateOpensTemporarilyAndReleaseClosesDoor()
    {
        TestGroup setup = CreateGroup();
        GameObject player = CreatePlayer("Player", setup.PlateA.transform.position);

        yield return Settle();
        Assert.That(setup.PlateA.IsActive, Is.True);
        Assert.That(setup.Group.State, Is.EqualTo(PermanentLatchDoorGroup2D.GroupState.TemporaryOpen));
        Assert.That(setup.Door.IsOpen, Is.True);

        player.transform.position = Vector3.right * 20f;
        Physics2D.SyncTransforms();
        setup.Group.SettlePhysicsState();
        Assert.That(setup.Group.State, Is.EqualTo(PermanentLatchDoorGroup2D.GroupState.Closed));
        Assert.That(setup.Door.IsOpen, Is.False);
        Assert.That(setup.Door.GetComponent<BoxCollider2D>().enabled, Is.True);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TwoPlatesLatchOnceAndSurviveReleaseResetAndRecreation()
    {
        TestGroup setup = CreateGroup();
        GameObject player = CreatePlayer("Player", setup.PlateA.transform.position);
        GameObject clone = CreateClone("MirrorClone", setup.PlateB.transform.position);
        int latchEvents = 0;
        setup.Group.Latched += () => latchEvents++;

        yield return Settle();
        Assert.That(setup.Group.IsLatched, Is.True);
        Assert.That(setup.Door.State, Is.EqualTo(Door2D.VisualState.LatchedOpen));
        Assert.That(setup.PlateA.IsLatchedVisual, Is.True);
        Assert.That(setup.PlateB.IsLatchedVisual, Is.True);
        Assert.That(SaveService.Instance.HasLatchedDoorGroup(SaveIds.Fire007DoorGroup), Is.True);
        Assert.That(latchEvents, Is.EqualTo(1));

        player.transform.position = Vector3.right * 20f;
        Object.Destroy(clone);
        Physics2D.SyncTransforms();
        yield return Settle();
        setup.Door.ResetRoomState();
        setup.PlateA.ResetRoomState();
        setup.PlateB.ResetRoomState();
        setup.Group.ResetRoomState();
        Assert.That(setup.Group.IsLatched, Is.True);
        Assert.That(setup.Door.IsOpen, Is.True);
        Assert.That(latchEvents, Is.EqualTo(1));

        Object.DestroyImmediate(setup.Group.gameObject);
        TestGroup restored = CreateGroup("Restored Group");
        yield return null;
        Assert.That(restored.Group.IsLatched, Is.True);
        Assert.That(restored.Door.State, Is.EqualTo(Door2D.VisualState.LatchedOpen));
    }

    [UnityTest]
    public IEnumerator DestroyedCloneIsPrunedAndCannotHoldTemporaryDoorOpen()
    {
        TestGroup setup = CreateGroup();
        GameObject clone = CreateClone("MirrorClone", setup.PlateA.transform.position);

        yield return Settle();
        Assert.That(setup.Group.State, Is.EqualTo(PermanentLatchDoorGroup2D.GroupState.TemporaryOpen));
        Object.Destroy(clone);
        yield return Settle();
        Assert.That(setup.PlateA.IsActive, Is.False);
        Assert.That(setup.Group.State, Is.EqualTo(PermanentLatchDoorGroup2D.GroupState.Closed));
    }

    [UnityTest]
    public IEnumerator DoorWaitsForCharacterToLeaveBeforeClosing()
    {
        TestGroup setup = CreateGroup();
        GameObject player = CreatePlayer("Player", setup.Door.transform.position);
        setup.Door.SetOpen(true);
        Physics2D.SyncTransforms();
        setup.Door.SetOpen(false);

        Assert.That(setup.Door.IsOpen, Is.True);
        Assert.That(setup.Door.IsWaitingToClose, Is.True);
        player.transform.position = Vector3.right * 20f;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        Assert.That(setup.Door.IsOpen, Is.False);
        Assert.That(setup.Door.GetComponent<BoxCollider2D>().enabled, Is.True);
    }

    [UnityTest]
    public IEnumerator PlayerTouchingClosedDoorDoesNotOpenIt()
    {
        TestGroup setup = CreateGroup();
        CreatePlayer("Player", setup.Door.transform.position);

        yield return Settle();

        AssertClosedAndBlocking(setup.Door);
    }

    [UnityTest]
    public IEnumerator MirrorCloneTouchingClosedDoorDoesNotOpenIt()
    {
        TestGroup setup = CreateGroup();
        CreateClone("MirrorClone", setup.Door.transform.position);

        yield return Settle();

        AssertClosedAndBlocking(setup.Door);
    }

    [UnityTest]
    public IEnumerator ConfiguredSceneGroupRejectsDisconnectedReferences()
    {
        GameObject broken = new("Broken Door Group");
        broken.transform.SetParent(root.transform);
        LogAssert.Expect(LogType.Error, "Invalid permanent door group configuration on Broken Door Group.");
        PermanentLatchDoorGroup2D group = broken.AddComponent<PermanentLatchDoorGroup2D>();
        group.Configure(SaveIds.Fire008DoorGroup01, null, null, null);
        yield return null;
        Assert.That(group.IsLatched, Is.False);
    }

    private TestGroup CreateGroup(string name = "Door Group")
    {
        GameObject host = new(name);
        host.transform.SetParent(root.transform);

        PressurePlate2D plateA = CreatePlate(host.transform, "Plate-A", new Vector2(-2f, 0f));
        PressurePlate2D plateB = CreatePlate(host.transform, "Plate-B", new Vector2(2f, 0f));
        GameObject doorObject = new("Door-A");
        doorObject.transform.SetParent(host.transform);
        doorObject.AddComponent<BoxCollider2D>().size = new Vector2(.6f, 5f);
        Door2D door = doorObject.AddComponent<Door2D>();
        door.Configure(false);

        PermanentLatchDoorGroup2D group = host.AddComponent<PermanentLatchDoorGroup2D>();
        group.Configure(SaveIds.Fire007DoorGroup, door, plateA, plateB);
        return new TestGroup(group, door, plateA, plateB);
    }

    private static PressurePlate2D CreatePlate(Transform parent, string name, Vector2 position)
    {
        GameObject plate = new(name);
        plate.transform.SetParent(parent);
        plate.transform.position = position;
        plate.AddComponent<BoxCollider2D>().size = new Vector2(1.2f, .3f);
        return plate.AddComponent<PressurePlate2D>();
    }

    private GameObject CreatePlayer(string name, Vector3 position)
    {
        GameObject player = new(name);
        player.transform.SetParent(root.transform);
        player.transform.position = position;
        player.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.8f);
        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        player.AddComponent<PlayerController2D>();
        return player;
    }

    private GameObject CreateClone(string name, Vector3 position)
    {
        GameObject clone = new(name);
        clone.transform.SetParent(root.transform);
        clone.transform.position = position;
        clone.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.8f);
        Rigidbody2D body = clone.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        clone.AddComponent<MirrorCloneController2D>();
        return clone;
    }

    private static IEnumerator Settle()
    {
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
    }

    private static void AssertClosedAndBlocking(Door2D door)
    {
        Assert.That(door.State, Is.EqualTo(Door2D.VisualState.Closed));
        Assert.That(door.IsOpen, Is.False);
        Assert.That(door.IsWaitingToClose, Is.False);
        Assert.That(door.GetComponent<BoxCollider2D>().enabled, Is.True);
    }

    private readonly struct TestGroup
    {
        public readonly PermanentLatchDoorGroup2D Group;
        public readonly Door2D Door;
        public readonly PressurePlate2D PlateA;
        public readonly PressurePlate2D PlateB;

        public TestGroup(PermanentLatchDoorGroup2D group, Door2D door, PressurePlate2D plateA, PressurePlate2D plateB)
        { Group = group; Door = door; PlateA = plateA; PlateB = plateB; }
    }
}
