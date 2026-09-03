using System;
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
    public void CurrentMigrationIsIdempotentAndCanonical()
    {
        SaveData data = SaveData.CreateNew();
        data.completedRoomIds.Add("FIRE_007");
        data.completedRoomIds.Add("CENTER_001");
        data.completedRoomIds.Add("FIRE_007");

        Assert.That(SaveDataMigration.TryMigrate(data, out bool firstChanged, out string firstError),
            Is.True, firstError);
        Assert.That(firstChanged, Is.True);
        Assert.That(data.completedRoomIds, Is.EqualTo(new[] { "CENTER_001", "FIRE_007" }));
        Assert.That(SaveDataMigration.TryMigrate(data, out bool secondChanged, out string secondError),
            Is.True, secondError);
        Assert.That(secondChanged, Is.False);
    }

    [Test]
    public void SemanticValidationRejectsInvalidPersistentIds()
    {
        SaveData data = SaveData.CreateNew();
        data.completedRoomIds.Add("not-a-room");

        Assert.That(SaveDataMigration.Validate(data, out string error), Is.False);
        Assert.That(error, Does.Contain("completedRoomIds"));
    }

    [Test]
    public void MigrationRepairsMalformedContinueLocationWithoutLosingProgress()
    {
        SaveData data = SaveData.CreateNew();
        data.lastRoomId = "../unknown";
        data.lastEntranceId = "";
        data.completedRoomIds.Add("CENTER_001");

        Assert.That(SaveDataMigration.TryMigrate(data, out bool changed, out string error), Is.True, error);
        Assert.That(changed, Is.True);
        Assert.That(data.lastRoomId, Is.EqualTo(SaveIds.DefaultRoom));
        Assert.That(data.lastEntranceId, Is.EqualTo(SaveIds.DefaultEntrance));
        Assert.That(data.completedRoomIds, Is.EqualTo(new[] { "CENTER_001" }));
    }

    [Test]
    public void MissingFilesProduceExplicitMissingStatus()
    {
        LocalSaveLoadResult result = new LocalSaveStore(directory).Load();

        Assert.That(result.Status, Is.EqualTo(LocalSaveLoadStatus.Missing));
        Assert.That(result.Data, Is.Null);
    }

    [Test]
    public void ContinueRoomResolutionUsesBuildPathsAndRejectsUnknownRooms()
    {
        string[] buildPaths =
        {
            "Assets/Scenes/Levels/Center/Center_001.unity",
            "Assets/Scenes/Levels/Fire/Fire_007.unity"
        };

        Assert.That(SaveService.TryResolveSceneName("FIRE_007", buildPaths, out string resolved), Is.True);
        Assert.That(resolved, Is.EqualTo("Fire_007"));
        Assert.That(SaveService.TryResolveSceneName("FIRE_999", buildPaths, out _), Is.False);
        Assert.That(SaveService.TryResolveSceneName("../Fire_007", buildPaths, out _), Is.False);
    }

    [Test]
    public void UnsupportedFutureMainNeverDowngradesToValidBackup()
    {
        LocalSaveStore store = new(directory);
        SaveData valid = SaveData.CreateNew();
        Assert.That(store.TryWrite(valid, out string firstError), Is.True, firstError);
        valid.completedRoomIds.Add("CENTER_001");
        Assert.That(store.TryWrite(valid, out string secondError), Is.True, secondError);
        SaveData future = SaveData.CreateNew();
        future.schemaVersion = SaveDataMigration.CurrentSchemaVersion + 1;
        string futureJson = JsonUtility.ToJson(future);
        File.WriteAllText(store.MainPath, futureJson);

        LocalSaveLoadResult result = store.Load();

        Assert.That(result.Status, Is.EqualTo(LocalSaveLoadStatus.UnsupportedFutureVersion));
        Assert.That(result.Data, Is.Null);
        Assert.That(File.ReadAllText(store.BackupPath), Is.Not.Empty);
        Assert.That(store.TryWrite(valid, out _), Is.False);
        Assert.That(File.ReadAllText(store.MainPath), Is.EqualTo(futureJson));
    }

    [Test]
    public void BothInvalidFilesAreBlockedAndPreservedBeforeNewGame()
    {
        Directory.CreateDirectory(directory);
        LocalSaveStore store = new(directory,
            () => new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero));
        File.WriteAllText(store.MainPath, "{ truncated");
        File.WriteAllText(store.BackupPath, "not json");

        Assert.That(store.Load().Status, Is.EqualTo(LocalSaveLoadStatus.Corrupt));
        Assert.That(store.PrepareForNewGame(out string error), Is.True, error);
        Assert.That(File.Exists(store.MainPath), Is.False);
        Assert.That(File.Exists(store.BackupPath), Is.False);
        Assert.That(store.LastPreservedPaths, Has.Count.EqualTo(2));
        Assert.That(File.ReadAllText(store.LastPreservedPaths[0]), Is.EqualTo("{ truncated"));
        Assert.That(File.ReadAllText(store.LastPreservedPaths[1]), Is.EqualTo("not json"));
    }

    [Test]
    public void MigratedMainIsRetainedAsTheSingleBackupBeforeRewrite()
    {
        Directory.CreateDirectory(directory);
        LocalSaveStore store = new(directory);
        SaveData versionOne = SaveData.CreateNew();
        versionOne.schemaVersion = 1;
        File.WriteAllText(store.MainPath, JsonUtility.ToJson(versionOne, true));

        LocalSaveLoadResult load = store.Load();
        Assert.That(load.Status, Is.EqualTo(LocalSaveLoadStatus.Success));
        Assert.That(load.NeedsRewrite, Is.True);
        Assert.That(store.TryWrite(load.Data, out string error), Is.True, error);

        string retained = File.ReadAllText(store.BackupPath);
        Assert.That(retained, Does.Contain("\"schemaVersion\": 1"));
        Assert.That(store.Load().Data.schemaVersion, Is.EqualTo(SaveDataMigration.CurrentSchemaVersion));
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

        Assert.That(store.TryWrite(loaded, out string rewriteError), Is.True, rewriteError);
        Assert.That(store.LastPreservedPaths, Has.Count.EqualTo(1));
        Assert.That(File.ReadAllText(store.LastPreservedPaths[0]), Is.EqualTo("{ truncated"));
        Assert.That(store.Load().Status, Is.EqualTo(LocalSaveLoadStatus.Success));
    }

    [Test]
    public void PartialCurrentSchemaMainRecoversValidBackupInsteadOfRepairingMissingCollections()
    {
        LocalSaveStore store = new(directory);
        SaveData backupVersion = SaveData.CreateNew();
        backupVersion.completedRoomIds.Add("CENTER_001");
        Assert.That(store.TryWrite(backupVersion, out string firstError), Is.True, firstError);

        SaveData newerMain = SaveData.Clone(backupVersion);
        newerMain.completedRoomIds.Add("FIRE_007");
        Assert.That(store.TryWrite(newerMain, out string secondError), Is.True, secondError);
        newerMain.progressionFlags = null;
        File.WriteAllText(store.MainPath, JsonUtility.ToJson(newerMain, true));

        LocalSaveLoadResult result = store.Load();

        Assert.That(result.Status, Is.EqualTo(LocalSaveLoadStatus.Success));
        Assert.That(result.RecoveredFromBackup, Is.True);
        Assert.That(result.NeedsRewrite, Is.True);
        Assert.That(result.Data.completedRoomIds, Is.EqualTo(new[] { "CENTER_001" }));
        Assert.That(result.Data.progressionFlags, Is.Not.Null);
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

    [Test]
    public void FireCollectibleUpdatesPermanentIdsAndRegionProgress()
    {
        SaveService service = SaveService.Instance;
        service.ReplaceStateForTests(SaveData.CreateNew(), new LocalSaveStore(directory));

        Assert.That(service.TryCollectPermanent("FIRE_006:COLLECTIBLE:01", PermanentPickupType.Collectible), Is.True);

        Assert.That(service.Data.collectedPermanentIds, Does.Contain("FIRE_006:COLLECTIBLE:01"));
        RegionProgressData fire = service.GetRegionCollectibleProgress("FIRE");
        Assert.That(fire, Is.Not.Null);
        Assert.That(fire.collectedCount, Is.EqualTo(1));
        Assert.That(fire.totalCount, Is.EqualTo(1));
        Assert.That(service.CountAllCollectibles(), Is.EqualTo(1));
        Assert.That(service.TotalCollectibleCount, Is.EqualTo(7));
    }

    [Test]
    public void FailedWriteKeepsOneInMemoryRewardAndRetryPersistsIt()
    {
        bool failWrites = true;
        LocalSaveStore failingStore = new(directory, null,
            () => failWrites ? "InjectedWriteFailure" : null);
        SaveService service = SaveService.Instance;
        service.ReplaceStateForTests(SaveData.CreateNew(), failingStore);

        Assert.That(service.TryCollectPermanent(SaveIds.MirrorPickup,
            PermanentPickupType.Ability, SaveIds.MirrorAbility), Is.True);
        Assert.That(service.TrySaveNow(), Is.False);
        Assert.That(service.HasUnsavedChanges, Is.True);
        Assert.That(service.HasAbility(SaveIds.MirrorAbility), Is.True);
        Assert.That(service.TryCollectPermanent(SaveIds.MirrorPickup,
            PermanentPickupType.Ability, SaveIds.MirrorAbility), Is.False);

        failWrites = false;
        service.RetrySave();
        Assert.That(service.HasUnsavedChanges, Is.False, service.LastWriteError);
        Assert.That(failingStore.Load().Data.unlockedAbilities, Does.Contain(SaveIds.MirrorAbility));
    }

    [Test]
    public void PersistentStartupWriteFailureRemainsQueryableUntilRetrySucceeds()
    {
        Directory.CreateDirectory(directory);
        SaveData versionOne = SaveData.CreateNew();
        versionOne.schemaVersion = 1;
        File.WriteAllText(Path.Combine(directory, LocalSaveStore.MainFileName),
            JsonUtility.ToJson(versionOne, true));

        bool failWrites = true;
        LocalSaveStore failingStore = new(directory, null,
            () => failWrites ? "InjectedStartupWriteFailure" : null);
        SaveService service = SaveService.Instance;
        service.ReplaceStateForTests(SaveData.CreateNew(), failingStore);
        int persistentFailureEvents = 0;
        void CountFailure(string _) => persistentFailureEvents++;
        service.PersistentSaveFailure += CountFailure;
        try
        {
            service.ReloadFromStoreForTests(failingStore);
            Assert.That(service.GameplayAuthorized, Is.False);
            Assert.That(service.TrySaveNow(), Is.False);
            Assert.That(service.TrySaveNow(), Is.False);

            Assert.That(persistentFailureEvents, Is.EqualTo(1));
            Assert.That(service.HasPersistentSaveFailure, Is.True);
            Assert.That(service.HasUnsavedChanges, Is.True);

            failWrites = false;
            service.RetrySave();
            Assert.That(service.HasPersistentSaveFailure, Is.False);
            Assert.That(service.HasUnsavedChanges, Is.False);
        }
        finally
        {
            service.PersistentSaveFailure -= CountFailure;
        }
    }

    [Test]
    public void CoalescedTypedProgressSubmissionsKeepEveryUniqueValue()
    {
        LocalSaveStore store = new(directory);
        SaveService service = SaveService.Instance;
        service.ReplaceStateForTests(SaveData.CreateNew(), store);

        Assert.That(service.TryCompleteRoom("CENTER_001"), Is.True);
        Assert.That(service.TryCompleteRoom("CENTER_001"), Is.False);
        Assert.That(service.TryRecordApprovedRegionUnlock("FIRE"), Is.True);
        Assert.That(service.TryRecordApprovedRegionUnlock("FIRE"), Is.False);
        Assert.That(service.TrySetProgressionFlag("TEST.APPROVED_FLAG"), Is.True);
        Assert.That(service.TrySetProgressionFlag("TEST.APPROVED_FLAG"), Is.False);
        Assert.That(service.TryLatchDoorGroup(SaveIds.Fire007DoorGroup), Is.True);
        Assert.That(service.TrySaveNow(), Is.True, service.LastWriteError);

        LocalSaveLoadResult load = store.Load();
        Assert.That(load.Data.completedRoomIds, Is.EqualTo(new[] { "CENTER_001" }));
        Assert.That(load.Data.unlockedRegionIds, Is.EqualTo(new[] { "FIRE" }));
        Assert.That(load.Data.progressionFlags, Is.EqualTo(new[] { "TEST.APPROVED_FLAG" }));
        Assert.That(load.Data.latchedDoorGroupIds, Is.EqualTo(new[] { SaveIds.Fire007DoorGroup }));
    }

    [Test]
    public void ProgressSubmittedDuringAWriteRemainsPendingForTheNextRevision()
    {
        LocalSaveStore store = new(directory);
        SaveService service = SaveService.Instance;
        service.ReplaceStateForTests(SaveData.CreateNew(), store);
        Assert.That(service.TryCompleteRoom("CENTER_001"), Is.True);

        void SubmitLaterRevision() => service.TrySetProgressionFlag("TEST.LATE_REVISION");
        service.SaveStarted += SubmitLaterRevision;
        try
        {
            Assert.That(service.TrySaveNow(), Is.True, service.LastWriteError);
        }
        finally
        {
            service.SaveStarted -= SubmitLaterRevision;
        }

        Assert.That(service.HasUnsavedChanges, Is.True);
        LocalSaveLoadResult first = store.Load();
        Assert.That(first.Data.completedRoomIds, Does.Contain("CENTER_001"));
        Assert.That(first.Data.progressionFlags, Does.Not.Contain("TEST.LATE_REVISION"));

        Assert.That(service.TrySaveNow(), Is.True, service.LastWriteError);
        Assert.That(service.HasUnsavedChanges, Is.False);
        Assert.That(store.Load().Data.progressionFlags, Does.Contain("TEST.LATE_REVISION"));
    }

    [Test]
    public void RoomExitCompletionCommitsOnlyAtSuccessfulSpawnBoundary()
    {
        SaveService service = SaveService.Instance;
        service.ReplaceStateForTests(SaveData.CreateNew(), new LocalSaveStore(directory));
        RoomTransitionState.ClearForTests();
        try
        {
            RoomTransitionState.Request("Fire_008", "FROM_FIRE_009", true, "FIRE_007");
            Assert.That(service.HasCompletedRoom("FIRE_007"), Is.False);

            Assert.That(RoomTransitionState.TryConsumeEntrance("Fire_008", out string entranceId,
                out string completedSourceRoomId), Is.True);
            Assert.That(entranceId, Is.EqualTo("FROM_FIRE_009"));
            Assert.That(completedSourceRoomId, Is.EqualTo("FIRE_007"));
            Assert.That(service.HasCompletedRoom("FIRE_007"), Is.False);

            RoomTransitionState.CommitSuccessfulSpawn(service, "FIRE_008", entranceId, true,
                completedSourceRoomId);
            RoomTransitionState.CommitSuccessfulSpawn(service, "FIRE_008", entranceId, true,
                completedSourceRoomId);

            Assert.That(service.Data.completedRoomIds, Is.EqualTo(new[] { "FIRE_007" }));
            Assert.That(RoomTransitionState.TryConsumeEntrance("Fire_008", out _, out _), Is.False);
        }
        finally
        {
            RoomTransitionState.ClearForTests();
        }
    }

    [Test]
    public void ExposedDataIsAnImmutableSnapshot()
    {
        SaveService service = SaveService.Instance;
        service.ReplaceStateForTests(SaveData.CreateNew(), new LocalSaveStore(directory));

        SaveData snapshot = service.Data;
        snapshot.unlockedAbilities.Add("UNAUTHORIZED_MUTATION");

        Assert.That(service.HasAbility("UNAUTHORIZED_MUTATION"), Is.False);
        Assert.That(service.HasUnsavedChanges, Is.False);
    }
}
