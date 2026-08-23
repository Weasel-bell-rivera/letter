using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class Wind001PlayModeTests
{
    private const string SceneName = "Wind_001";

    [UnitySetUp]
    public IEnumerator LoadRoom()
    {
        SaveService.Instance.ReplaceStateForTests(CreateUnlockedState());
        yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
    }

    private static SaveData CreateUnlockedState()
    {
        SaveData data = SaveData.CreateNew();
        data.unlockedAbilities.Add(SaveIds.MirrorAbility);
        data.collectedPermanentIds.Add(SaveIds.MirrorPickup);
        return data;
    }

    [UnityTest]
    public IEnumerator RoomStartsSafeAndSupportsMirrorPlacementAndReset()
    {
        PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
        MirrorPlayer2D mirror = Object.FindAnyObjectByType<MirrorPlayer2D>();
        RoomResetSystem reset = Object.FindAnyObjectByType<RoomResetSystem>();
        WindRayEnemy2D enemy = Object.FindAnyObjectByType<WindRayEnemy2D>();

        Assert.That(player, Is.Not.Null);
        Assert.That(mirror, Is.Not.Null);
        Assert.That(reset, Is.Not.Null);
        Assert.That(enemy, Is.Not.Null);
        Assert.That(player.IsGroundedNow, Is.True);
        Assert.That(enemy.State, Is.EqualTo(WindRayEnemy2D.EnemyState.Guarding));
        Assert.That(enemy.CurrentTarget, Is.EqualTo(WindRayEnemy2D.TargetKind.None));
        Assert.That(Vector2.Distance(player.transform.position, enemy.transform.position),
            Is.GreaterThan(enemy.Settings.DetectionRadius));

        Assert.That(mirror.TryPlace(), Is.True, $"Mirror placement failed: {mirror.LastFailure}");
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Placed));
        reset.ResetRoom();
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        Assert.That(enemy.State, Is.EqualTo(WindRayEnemy2D.EnemyState.Guarding));
        Assert.That(Vector2.Distance(enemy.transform.position, new Vector2(6f, 2f)), Is.LessThan(.001f));
        Assert.That(Vector2.Distance(player.transform.position, reset.Checkpoint), Is.LessThan(.001f));
        yield return null;
    }

    [UnityTest]
    public IEnumerator MirrorCloneCanTakeLockAndRecallDoesNotCancelAttack()
    {
        PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
        MirrorPlayer2D mirror = Object.FindAnyObjectByType<MirrorPlayer2D>();
        WindRayEnemy2D enemy = Object.FindAnyObjectByType<WindRayEnemy2D>();
        Assert.That(mirror.TryPlace(), Is.True, $"Mirror placement failed: {mirror.LastFailure}");

        MirrorCloneController2D clone = mirror.Clone;
        Vector2 lurePoint = new(1f, -1.08f);
        clone.GetComponent<Rigidbody2D>().position = lurePoint;
        clone.transform.position = lurePoint;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();

        Assert.That(enemy.State, Is.EqualTo(WindRayEnemy2D.EnemyState.Windup));
        Assert.That(enemy.CurrentTarget, Is.EqualTo(WindRayEnemy2D.TargetKind.MirrorClone));
        Assert.That(Vector2.Distance(enemy.LockedPoint, lurePoint), Is.LessThan(.05f));
        Assert.That(Vector2.Distance(player.transform.position, enemy.transform.position),
            Is.GreaterThan(Vector2.Distance(clone.transform.position, enemy.transform.position)));

        Vector2 lockedPoint = enemy.LockedPoint;
        mirror.RecallImmediate();
        yield return null;
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        Assert.That(enemy.State, Is.EqualTo(WindRayEnemy2D.EnemyState.Windup));
        Assert.That(enemy.LockedPoint, Is.EqualTo(lockedPoint));
    }

    [UnityTest]
    public IEnumerator CloneHitDoesNotResetEnemyButPlayerHitDoes()
    {
        PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
        MirrorPlayer2D mirror = Object.FindAnyObjectByType<MirrorPlayer2D>();
        WindRayEnemy2D enemy = Object.FindAnyObjectByType<WindRayEnemy2D>();
        RoomResetSystem reset = Object.FindAnyObjectByType<RoomResetSystem>();
        Assert.That(mirror.TryPlace(), Is.True);
        MirrorCloneController2D clone = mirror.Clone;
        Vector2 lurePoint = new(1f, -1.08f);
        clone.GetComponent<Rigidbody2D>().position = lurePoint;
        clone.transform.position = lurePoint;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        Assert.That(enemy.State, Is.EqualTo(WindRayEnemy2D.EnemyState.Windup));

        enemy.HandleCharacterContact(clone.GetComponent<Collider2D>());
        yield return null;
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        Assert.That(enemy.State, Is.EqualTo(WindRayEnemy2D.EnemyState.Windup));

        enemy.HandleCharacterContact(player.GetComponent<Collider2D>());
        Assert.That(enemy.State, Is.EqualTo(WindRayEnemy2D.EnemyState.Guarding));
        Assert.That(enemy.CurrentTarget, Is.EqualTo(WindRayEnemy2D.TargetKind.None));
        Assert.That(Vector2.Distance(player.transform.position, reset.Checkpoint), Is.LessThan(.001f));
    }

    [UnityTest]
    public IEnumerator SolidWallBlocksDistanceDetection()
    {
        MirrorPlayer2D mirror = Object.FindAnyObjectByType<MirrorPlayer2D>();
        WindRayEnemy2D enemy = Object.FindAnyObjectByType<WindRayEnemy2D>();
        Assert.That(mirror.TryPlace(), Is.True);
        MirrorCloneController2D clone = mirror.Clone;
        clone.GetComponent<Rigidbody2D>().position = new Vector2(1f, -1.08f);
        clone.transform.position = new Vector2(1f, -1.08f);

        GameObject wall = new("Test Occluder");
        wall.transform.position = new Vector3(3.5f, .45f, 0f);
        BoxCollider2D blocker = wall.AddComponent<BoxCollider2D>();
        blocker.size = new Vector2(.6f, 5f);
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();

        Assert.That(enemy.State, Is.EqualTo(WindRayEnemy2D.EnemyState.Guarding));
        Assert.That(enemy.CurrentTarget, Is.EqualTo(WindRayEnemy2D.TargetKind.None));
        Object.Destroy(wall);
        yield return null;
    }
}
