using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class MirrorMinimalHarnessSmokeTests
{
    private readonly List<MirrorHarness2D> harnesses = new();

    [SetUp] public void Reset() => MirrorAbilityState.ResetForTests();

    [TearDown]
    public void TearDown()
    {
        foreach (MirrorHarness2D harness in harnesses) harness.Dispose();
        harnesses.Clear();
    }

    [UnityTest]
    public IEnumerator GroundPlacementAndRecallRoundTrip()
    {
        MirrorHarness2D harness = CreateHarness();
        harness.AddDefaultGround();
        yield return new WaitForFixedUpdate();

        Assert.That(harness.Mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        Assert.That(harness.Place(), Is.True, harness.Mirror.LastFailure.ToString());
        Assert.That(harness.Mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Placed));
        Assert.That(harness.Mirror.Clone, Is.Not.Null);
        Assert.That(harness.Mirror.PlacedMirror, Is.Not.Null);

        harness.Recall();
        Assert.That(harness.Mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        Assert.That(harness.Mirror.Clone, Is.Null);
        Assert.That(harness.Mirror.PlacedMirror, Is.Null);
    }

    [UnityTest]
    public IEnumerator RepeatedLeftClickIsRejectedAfterPlaced()
    {
        MirrorHarness2D harness = CreateHarness();
        harness.AddDefaultGround();
        yield return new WaitForFixedUpdate();

        Assert.That(harness.Place(), Is.True);
        Assert.That(harness.Place(), Is.False);
        Assert.That(harness.Mirror.LastFailure, Is.EqualTo(MirrorPlayer2D.PlacementFailure.AlreadyPlaced));
        Assert.That(harness.Mirror.Clone, Is.Not.Null);
        Assert.That(harness.Mirror.PlacedMirror, Is.Not.Null);

        harness.Recall();
    }

    [UnityTest]
    public IEnumerator AirbornePlacementKeepsState()
    {
        MirrorHarness2D harness = CreateHarness();
        harness.AddDefaultGround();
        harness.SetPlayerPosition(new Vector2(0f, 2f));
        yield return new WaitForFixedUpdate();

        Assert.That(harness.Place(), Is.False);
        Assert.That(harness.Mirror.LastFailure, Is.EqualTo(MirrorPlayer2D.PlacementFailure.Airborne));
        Assert.That(harness.Mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        Assert.That(harness.Mirror.Clone, Is.Null);
        Assert.That(harness.Mirror.PlacedMirror, Is.Null);
    }

    [UnityTest]
    public IEnumerator SpecialWallPlacementSpawnsWithRotatedGravity()
    {
        MirrorHarness2D harness = CreateHarness();
        harness.AddDefaultGround();
        harness.SetPlayerFacingRight();
        harness.AddSpecialWallNearPlayer(+1f);
        yield return new WaitForFixedUpdate();

        Assert.That(harness.Place(), Is.True, harness.Mirror.LastFailure.ToString());
        Assert.That(harness.Mirror.Clone.GravityAxis, Is.EqualTo(Vector2.left));
    }

    [UnityTest]
    public IEnumerator SpecialWallPlacementUsesFacingSide()
    {
        MirrorHarness2D harness = CreateHarness();
        harness.AddDefaultGround();
        harness.SetPlayerPosition(new Vector2(0f, 0f));
        yield return new WaitForFixedUpdate();

        harness.SetPlayerFacingLeft();
        harness.AddSpecialWallNearPlayer(-1f);

        Assert.That(harness.Place(), Is.True, harness.Mirror.LastFailure.ToString());
        Assert.That(harness.Mirror.Clone.GravityAxis, Is.EqualTo(Vector2.right));
    }

    private MirrorHarness2D CreateHarness(bool initiallyUnlocked = true)
    {
        MirrorHarness2D harness = MirrorHarness2D.Create(initiallyUnlocked);
        harnesses.Add(harness);
        return harness;
    }
}
