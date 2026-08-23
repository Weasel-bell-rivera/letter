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
}
