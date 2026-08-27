using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class GroundFireThrowerEnemyPlayModeTests
{
    private const string SceneName = "Earth_001";

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
    public IEnumerator EntranceIsSafeAndApproachLocksPlayerPosition()
    {
        PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
        GroundFireThrowerEnemy2D enemy = Object.FindAnyObjectByType<GroundFireThrowerEnemy2D>();
        Assert.That(player, Is.Not.Null);
        Assert.That(enemy, Is.Not.Null);
        Assert.That(enemy.State, Is.EqualTo(GroundFireThrowerEnemy2D.EnemyState.Guarding));
        Assert.That(Vector2.Distance(player.transform.position, enemy.transform.position),
            Is.GreaterThan(enemy.Settings.DetectionRadius));

        MoveCharacter(player.GetComponent<Rigidbody2D>(), new Vector2(5f, -2.08f));
        yield return new WaitForFixedUpdate();
        Assert.That(enemy.State, Is.EqualTo(GroundFireThrowerEnemy2D.EnemyState.Windup));
        Assert.That(enemy.CurrentTarget, Is.EqualTo(GroundFireThrowerEnemy2D.TargetKind.Player));
        Vector2 locked = enemy.LockedPoint;

        MoveCharacter(player.GetComponent<Rigidbody2D>(), new Vector2(7f, -2.08f));
        yield return new WaitForFixedUpdate();
        Assert.That(enemy.LockedPoint, Is.EqualTo(locked));
    }

    [UnityTest]
    public IEnumerator CloserMirrorCloneTakesLockAndRecallDoesNotCancelWindup()
    {
        PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
        MirrorPlayer2D mirror = Object.FindAnyObjectByType<MirrorPlayer2D>();
        GroundFireThrowerEnemy2D enemy = Object.FindAnyObjectByType<GroundFireThrowerEnemy2D>();
        Assert.That(mirror.TryPlace(), Is.True, $"Mirror placement failed: {mirror.LastFailure}");
        MirrorCloneController2D clone = mirror.Clone;

        MoveCharacter(player.GetComponent<Rigidbody2D>(), new Vector2(5f, -2.08f));
        MoveCharacter(clone.GetComponent<Rigidbody2D>(), new Vector2(2f, -2.08f));
        yield return new WaitForFixedUpdate();
        Assert.That(enemy.State, Is.EqualTo(GroundFireThrowerEnemy2D.EnemyState.Windup));
        Assert.That(enemy.CurrentTarget,
            Is.EqualTo(GroundFireThrowerEnemy2D.TargetKind.MirrorClone));
        Vector2 locked = enemy.LockedPoint;

        mirror.RecallImmediate();
        yield return null;
        Assert.That(enemy.State, Is.EqualTo(GroundFireThrowerEnemy2D.EnemyState.Windup));
        Assert.That(enemy.LockedPoint, Is.EqualTo(locked));
    }

    [UnityTest]
    public IEnumerator ThrowCreatesOneProjectileAndRoomResetClearsIt()
    {
        PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
        RoomResetSystem reset = Object.FindAnyObjectByType<RoomResetSystem>();
        GroundFireThrowerEnemy2D enemy = Object.FindAnyObjectByType<GroundFireThrowerEnemy2D>();
        MoveCharacter(player.GetComponent<Rigidbody2D>(), new Vector2(5f, -2.08f));
        yield return new WaitForFixedUpdate();
        Assert.That(enemy.State, Is.EqualTo(GroundFireThrowerEnemy2D.EnemyState.Windup));

        float deadline = Time.time + enemy.Settings.WindupDuration + .5f;
        while (enemy.State == GroundFireThrowerEnemy2D.EnemyState.Windup && Time.time < deadline)
            yield return new WaitForFixedUpdate();
        Assert.That(enemy.State, Is.EqualTo(GroundFireThrowerEnemy2D.EnemyState.Cooldown));
        Assert.That(enemy.ActiveProjectileCount, Is.EqualTo(1));

        reset.ResetRoom();
        yield return null;
        Assert.That(enemy.State, Is.EqualTo(GroundFireThrowerEnemy2D.EnemyState.Guarding));
        Assert.That(enemy.ActiveProjectileCount, Is.Zero);
        Assert.That(Object.FindObjectsByType<ArcFireballProjectile2D>(FindObjectsSortMode.None), Is.Empty);
    }

    private static void MoveCharacter(Rigidbody2D body, Vector2 position)
    {
        body.position = position;
        body.transform.position = position;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }
}
