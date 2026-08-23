using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

public sealed class Snow002PlayModeTests
{
    private const string SceneName = "Snow_002";

    [UnitySetUp]
    public IEnumerator LoadRoom()
    {
        SaveService.Instance.ReplaceStateForTests(CreateUnlockedState());
        yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
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
    public IEnumerator PlayerCanStandPlaceMirrorRecallAndResetOnFrozenGround()
    {
        PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
        MirrorPlayer2D mirror = Object.FindAnyObjectByType<MirrorPlayer2D>();
        RoomResetSystem reset = Object.FindAnyObjectByType<RoomResetSystem>();
        Tilemap frozenGround = Object.FindObjectsByType<Tilemap>()
            .Single(map => map.name == "FrozenGround");

        Assert.That(player, Is.Not.Null);
        Assert.That(mirror, Is.Not.Null);
        Assert.That(reset, Is.Not.Null);
        Assert.That(player.IsGroundedNow, Is.True);
        Assert.That(frozenGround.GetComponent<CompositeCollider2D>().pathCount, Is.GreaterThan(0));
        Assert.That(frozenGround.GetComponent<SurfaceSemantic2D>().Type,
            Is.EqualTo(SurfaceSemantic2D.SurfaceType.FrozenGround));

        Assert.That(mirror.TryPlace(), Is.True, $"Mirror placement failed: {mirror.LastFailure}");
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Placed));
        mirror.RecallImmediate();
        yield return null;
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));

        Vector3 checkpoint = reset.Checkpoint;
        player.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(5f, -2f);
        player.transform.position = new Vector3(5f, 2f, 0f);
        Physics2D.SyncTransforms();
        reset.ResetRoom();

        Assert.That(Vector3.Distance(player.transform.position, checkpoint), Is.LessThan(.001f));
        Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.sqrMagnitude, Is.LessThan(.001f));
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        yield return null;
    }
}
