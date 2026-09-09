using System.IO;
using NUnit.Framework;

public sealed class PermanentDoorSaveTests
{
    private string directory;

    [SetUp]
    public void SetUp() => directory = Path.Combine(Path.GetTempPath(), "w1-door-save-tests", System.Guid.NewGuid().ToString("N"));

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    [Test]
    public void VersionOneAddsLatchedDoorGroupsAndMigratesToVersionTwo()
    {
        SaveData data = SaveData.CreateNew();
        data.schemaVersion = 1;
        data.latchedDoorGroupIds = null;

        Assert.That(SaveDataMigration.TryMigrate(data, out bool changed, out string error), Is.True, error);
        Assert.That(changed, Is.True);
        Assert.That(data.schemaVersion, Is.EqualTo(2));
        Assert.That(data.latchedDoorGroupIds, Is.Not.Null.And.Empty);
    }

    [Test]
    public void MigrationNormalizesLatchedDoorGroupsAsASet()
    {
        SaveData data = SaveData.CreateNew();
        data.latchedDoorGroupIds.Add(SaveIds.Fire007DoorGroup);
        data.latchedDoorGroupIds.Add(SaveIds.Fire007DoorGroup);

        Assert.That(SaveDataMigration.TryMigrate(data, out bool changed, out string error), Is.True, error);
        Assert.That(changed, Is.True);
        Assert.That(data.latchedDoorGroupIds, Is.EqualTo(new[] { SaveIds.Fire007DoorGroup }));
    }

    [Test]
    public void StoreRoundTripKeepsPermanentDoorLatch()
    {
        LocalSaveStore store = new(directory);
        SaveData data = SaveData.CreateNew();
        data.latchedDoorGroupIds.Add(SaveIds.Fire007DoorGroup);

        Assert.That(store.TryWrite(data, out string writeError), Is.True, writeError);
        Assert.That(store.TryLoad(out SaveData loaded, out _, out _, out string loadError), Is.True, loadError);
        Assert.That(loaded.latchedDoorGroupIds, Does.Contain(SaveIds.Fire007DoorGroup));
    }

    [TestCase("FIRE_007:DOOR_GROUP:01", true)]
    [TestCase("FIRE_008:DOOR_GROUP:03", true)]
    [TestCase("FIRE_7:DOOR_GROUP:01", false)]
    [TestCase("fire_007:DOOR_GROUP:01", false)]
    [TestCase("FIRE_007:DOOR:01", false)]
    public void DoorGroupIdValidationUsesStableFormat(string id, bool expected)
        => Assert.That(DoorGroupId.IsValid(id), Is.EqualTo(expected));

    [Test]
    public void Fire008UsesThreeDistinctStableDoorGroupIds()
    {
        string[] ids =
        {
            SaveIds.Fire008DoorGroup01,
            SaveIds.Fire008DoorGroup02,
            SaveIds.Fire008DoorGroup03
        };
        foreach (string id in ids) Assert.That(DoorGroupId.IsValid(id), Is.True);
        Assert.That(DoorGroupId.HasDuplicates(ids), Is.False);
    }
    [Test]
    public void DescendingSwitchPrefabHasIndependentModeAndFlatTopArt()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
            "Assets/Prefabs/Gameplay/Switches/DescendingLatchSwitch2D.prefab");
        Assert.That(prefab, Is.Not.Null);
        var plate = prefab.GetComponent<PressurePlate2D>();
        Assert.That(plate.Mode, Is.EqualTo(PressurePlate2D.ActivationMode.DescendingLatch));
        var renderer = prefab.GetComponentInChildren<UnityEngine.SpriteRenderer>();
        Assert.That(renderer.sprite, Is.Not.Null);
        Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("W1/Gameplay/Flat Switch Silhouette"));
        var body = prefab.GetComponent<UnityEngine.Rigidbody2D>();
        Assert.That(body, Is.Not.Null);
        Assert.That(body.bodyType, Is.EqualTo(UnityEngine.RigidbodyType2D.Kinematic));
        Assert.That(plate, Is.InstanceOf<ISurfaceMotionProvider2D>());
        var colliders = prefab.GetComponents<UnityEngine.BoxCollider2D>();
        Assert.That(System.Array.Exists(colliders, collider => !collider.isTrigger && collider.size.x >= 2.5f), Is.True);
        var serialized = new UnityEditor.SerializedObject(plate);
        Assert.That(serialized.FindProperty("standingSurface").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("descendingBody").objectReferenceValue, Is.EqualTo(body));
        Assert.That(serialized.FindProperty("permanentSwitchId").stringValue, Is.Empty,
            "Production IDs belong to room instances, never the reusable template.");
    }

    [Test]
    public void LeavingBeforeBottomCancelsProgressAndCannotOpenDoor()
    {
        var host = new UnityEngine.GameObject("Descending switch test");
        host.SetActive(false);
        try
        {
            var plate = host.AddComponent<PressurePlate2D>();
            plate.ConfigureActivationMode(PressurePlate2D.ActivationMode.DescendingLatch);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(PressurePlate2D).GetField("saveInitialized", flags).SetValue(plate, true);
            var advance = typeof(PressurePlate2D).GetMethod("AdvanceDescent", flags);
            advance.Invoke(plate, new object[] { true, .75f });
            Assert.That(plate.PressProgress, Is.EqualTo(.75f).Within(.001f));
            Assert.That(plate.IsActive, Is.False);
            advance.Invoke(plate, new object[] { false, .2f });
            Assert.That(plate.PressProgress, Is.Zero);
            advance.Invoke(plate, new object[] { true, .3f });
            Assert.That(plate.PressProgress, Is.EqualTo(.3f).Within(.001f));
            Assert.That(plate.IsActive, Is.False, "Separate short presses cannot accumulate to latch.");
        }
        finally { UnityEngine.Object.DestroyImmediate(host); }
    }
    [Test]
    public void BottomLatchesAndReloadRestoresSwitchAndDoor()
    {
        var save = SaveService.Instance;
        var store = new LocalSaveStore(directory);
        save.ReplaceStateForTests(SaveData.CreateNew(), store);
        var host = new UnityEngine.GameObject("Permanent descending switch");
        host.SetActive(false);
        var doorHost = new UnityEngine.GameObject("Controlled door");
        try
        {
            var plate = host.AddComponent<PressurePlate2D>();
            plate.ConfigureActivationMode(PressurePlate2D.ActivationMode.DescendingLatch);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(PressurePlate2D).GetField("permanentSwitchId", flags).SetValue(plate, "FIRE_001:DOOR_GROUP:99");
            Assert.That(plate.IsActive, Is.False);
            var advance = typeof(PressurePlate2D).GetMethod("AdvanceDescent", flags);
            advance.Invoke(plate, new object[] { true, 1f });
            var door = doorHost.AddComponent<Door2D>();
            door.ConfigureControlSource(plate);
            door.ResetRoomState();
            Assert.That(door.IsOpen, Is.True);
            Assert.That(door.State, Is.EqualTo(Door2D.VisualState.LatchedOpen));
            advance.Invoke(plate, new object[] { false, 2f });
            Assert.That(plate.PressProgress, Is.EqualTo(1f));
            Assert.That(save.TrySaveNow(), Is.True);
            Assert.That(store.TryLoad(out SaveData loaded, out _, out _, out string error), Is.True, error);
            save.ReplaceStateForTests(loaded, store);
            plate.ResetRoomState();
            door.ResetRoomState();
            Assert.That(plate.IsActive, Is.True);
            Assert.That(plate.PressProgress, Is.EqualTo(1f));
            Assert.That(door.IsOpen, Is.True);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(doorHost);
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(save.gameObject);
        }
    }
}
