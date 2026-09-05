using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class OverheatingMirrorGroundPlayModeTests
{
    [UnityTest]
    public IEnumerator PlacedMirrorOverheatsThenReturnsToHeldState()
    {
        using MirrorHarness2D harness = MirrorHarness2D.CreateUnlocked();
        MirrorSurface2D surface = harness.AddDefaultGround();
        OverheatingMirrorGround2D overheat = surface.gameObject.AddComponent<OverheatingMirrorGround2D>();
        overheat.Configure(0.1f);

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.That(harness.Place(), Is.True, harness.Mirror.LastFailure.ToString());
        GameObject placedMirror = harness.Mirror.PlacedMirror;
        GameObject clone = harness.Mirror.Clone.gameObject;
        Assert.That(overheat.IsHeating, Is.True);

        yield return new WaitForSeconds(0.15f);

        Assert.That(harness.Mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        Assert.That(harness.Mirror.PlacedMirror, Is.Null);
        Assert.That(harness.Mirror.Clone, Is.Null);
        Assert.That(placedMirror == null || !placedMirror.activeSelf, Is.True);
        Assert.That(clone == null || !clone.activeSelf, Is.True);
        Assert.That(overheat.IsHeating, Is.False);
    }

    [UnityTest]
    public IEnumerator ManualRecallCancelsOverheatAndAllowsAnotherPlacement()
    {
        using MirrorHarness2D harness = MirrorHarness2D.CreateUnlocked();
        MirrorSurface2D surface = harness.AddDefaultGround();
        OverheatingMirrorGround2D overheat = surface.gameObject.AddComponent<OverheatingMirrorGround2D>();
        overheat.Configure(0.1f);

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.That(harness.Place(), Is.True, harness.Mirror.LastFailure.ToString());
        harness.Recall();
        yield return null;
        Assert.That(overheat.IsHeating, Is.False);

        yield return new WaitForSeconds(0.15f);
        Assert.That(harness.Mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        Assert.That(harness.Place(), Is.True, harness.Mirror.LastFailure.ToString());
        harness.Recall();
    }
}
