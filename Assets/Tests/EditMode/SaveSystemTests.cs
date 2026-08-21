using System.IO;
using NUnit.Framework;
using UnityEngine;

public sealed class SaveSystemTests
{
    private string directory;

    [SetUp]
    public void SetUp() => directory = Path.Combine(Path.GetTempPath(), "w1-save-tests", System.Guid.NewGuid().ToString("N"));

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    [Test]
    public void MigrationRepairsMirrorAbilityAndPickupAsUnion()
    {
        SaveData data = SaveData.CreateNew();
        data.collectedPermanentIds.Add(SaveIds.MirrorPickup);

        Assert.That(SaveDataMigration.TryMigrate(data, out bool changed, out string error), Is.True, error);
        Assert.That(changed, Is.True);
        Assert.That(data.unlockedAbilities, Does.Contain(SaveIds.MirrorAbility));
    }

    [Test]
    public void VersionZeroSampleMigratesToCurrentSchema()
    {
        SaveData data = SaveData.CreateNew();
        data.schemaVersion = 0;
        data.unlockedAbilities = null;

        Assert.That(SaveDataMigration.TryMigrate(data, out bool changed, out string error), Is.True, error);
        Assert.That(changed, Is.True);
        Assert.That(data.schemaVersion, Is.EqualTo(SaveDataMigration.CurrentSchemaVersion));
        Assert.That(data.unlockedAbilities, Is.Not.Null);
    }

    [Test]
    public void StoreRecoversValidBackupWhenMainIsTruncated()
    {
        LocalSaveStore store = new(directory);
        SaveData first = SaveData.CreateNew();
        Assert.That(store.TryWrite(first, out string firstError), Is.True, firstError);
        first.completedRoomIds.Add("CENTER_001");
        Assert.That(store.TryWrite(first, out string secondError), Is.True, secondError);
        File.WriteAllText(store.MainPath, "{ truncated");

        Assert.That(store.TryLoad(out SaveData loaded, out bool recovered, out bool rewrite, out string loadError), Is.True, loadError);
        Assert.That(recovered, Is.True);
        Assert.That(rewrite, Is.True);
        Assert.That(loaded.saveId, Is.EqualTo(first.saveId));
    }

    [Test]
    public void StoreRoundTripKeepsAllSubmittedSetValues()
    {
        LocalSaveStore store = new(directory);
        SaveData data = SaveData.CreateNew();
        data.collectedPermanentIds.Add("FIRE_004:COLLECTIBLE:01");
        data.collectedPermanentIds.Add(SaveIds.MirrorPickup);
        data.unlockedAbilities.Add(SaveIds.MirrorAbility);

        Assert.That(store.TryWrite(data, out string writeError), Is.True, writeError);
        Assert.That(store.TryLoad(out SaveData loaded, out _, out _, out string loadError), Is.True, loadError);
        Assert.That(loaded.collectedPermanentIds, Is.EquivalentTo(data.collectedPermanentIds));
        Assert.That(loaded.unlockedAbilities, Is.EquivalentTo(data.unlockedAbilities));
    }

    [TestCase("CENTER_001:ABILITY:01", PermanentPickupType.Ability, true)]
    [TestCase("FIRE_004:COLLECTIBLE:01", PermanentPickupType.Collectible, true)]
    [TestCase("FIRE_004:ABILITY:01", PermanentPickupType.Collectible, false)]
    [TestCase("fire_004:COLLECTIBLE:01", PermanentPickupType.Collectible, false)]
    [TestCase("FIRE_004:COLLECTIBLE:1", PermanentPickupType.Collectible, false)]
    public void PermanentIdValidationMatchesApprovedFormat(string id, PermanentPickupType type, bool expected)
        => Assert.That(PermanentPickupId.IsValid(id, type), Is.EqualTo(expected));

    [Test]
    public void DuplicatePermanentIdsAreDetected()
        => Assert.That(PermanentPickupId.HasDuplicates(new[] { SaveIds.MirrorPickup, SaveIds.MirrorPickup }), Is.True);
}
