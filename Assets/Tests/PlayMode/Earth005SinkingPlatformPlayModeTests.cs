using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class Earth005SinkingPlatformPlayModeTests
{
    [UnitySetUp]
    public IEnumerator IgnoreUnrelatedScenePresentationErrors()
    {
        LogAssert.ignoreFailingMessages = true;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator RestoreLogAssertions()
    {
        LogAssert.ignoreFailingMessages = false;
        yield return null;
    }

    [UnityTest]
    public IEnumerator HalfWidthPlayerContactDoesNotMakeRoomPlatformReverse()
    {
        yield return SceneManager.LoadSceneAsync("Earth_005", LoadSceneMode.Single);
        yield return new WaitForFixedUpdate();

        PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
        SinkingEarthBlock2D block = Object.FindObjectsByType<SinkingEarthBlock2D>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Single(item => item.name == "SinkingBlock-A");
        BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
        BoxCollider2D blockCollider = block.GetComponent<BoxCollider2D>();
        Rigidbody2D blockBody = block.GetComponent<Rigidbody2D>();

        float halfOverlapX = blockCollider.bounds.max.x;
        float supportedY = blockCollider.bounds.max.y + playerCollider.bounds.extents.y;
        player.TeleportTo(new Vector2(halfOverlapX, supportedY));
        Physics2D.SyncTransforms();

        float previousY = blockBody.position.y;
        int upwardSteps = 0;
        for (int i = 0; i < 120; i++)
        {
            yield return new WaitForFixedUpdate();
            float currentY = blockBody.position.y;
            if (currentY > previousY + .001f) upwardSteps++;
            previousY = currentY;
        }

        Assert.That(player.SupportCollider, Is.Not.EqualTo(blockCollider),
            "At the exact Terrain/platform seam the Player should remain supported by stable Terrain.");
        Assert.That(block.CurrentWeight, Is.Zero,
            "The sinking block must not count a Player whose resolved support is Terrain.");
        Assert.That(upwardSteps, Is.Zero,
            "EARTH_005 SinkingBlock-A reversed upward while the half-width Player remained supported.");
    }
}
