using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SinkingEarthBlockPlayModeTests
{
    [UnityTest]
    public IEnumerator SideContactDoesNotApplyWeight()
    {
        SinkingEarthBlock2D block = CreateBlock();
        Rigidbody2D actor = CreateActor(new Vector2(-1.25f, 0f));

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.That(block.CurrentWeight, Is.Zero,
            "An actor touching the side of a sinking block must not be treated as standing on top.");

        Object.Destroy(block.gameObject);
        Object.Destroy(actor.gameObject);
    }

    [UnityTest]
    public IEnumerator TopContactAppliesBodyMass()
    {
        SinkingEarthBlock2D block = CreateBlock();
        Rigidbody2D actor = CreateActor(new Vector2(0f, 1.35f));

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.That(block.CurrentWeight, Is.EqualTo(actor.mass).Within(.01f));

        Object.Destroy(block.gameObject);
        Object.Destroy(actor.gameObject);
    }

    [UnityTest]
    public IEnumerator HalfWidthPlayerContactSinksMonotonicallyWithoutVerticalJitter()
    {
        PlayerMovementSettings settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
        SinkingEarthBlock2D block = CreateBlock();
        GameObject playerObject = new("Player");
        playerObject.transform.position = new Vector2(1f, 1.4f);
        playerObject.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.8f);
        Rigidbody2D playerBody = playerObject.AddComponent<Rigidbody2D>();
        playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        PlayerController2D player = playerObject.AddComponent<PlayerController2D>();
        player.Configure(null, settings);
        Physics2D.SyncTransforms();

        float previousY = block.GetComponent<Rigidbody2D>().position.y;
        int upwardSteps = 0;
        float maximumPassengerGap = 0f;
        for (int i = 0; i < 90; i++)
        {
            yield return new WaitForFixedUpdate();
            float currentY = block.GetComponent<Rigidbody2D>().position.y;
            if (currentY > previousY + .001f) upwardSteps++;
            previousY = currentY;
            maximumPassengerGap = Mathf.Max(maximumPassengerGap,
                player.GetComponent<BoxCollider2D>().bounds.min.y -
                block.GetComponent<BoxCollider2D>().bounds.max.y);
        }

        Assert.That(block.CurrentWeight, Is.EqualTo(playerBody.mass).Within(.01f));
        Assert.That(upwardSteps, Is.Zero,
            "A half-width passenger made the sinking block reverse upward while it was still supported.");
        Assert.That(maximumPassengerGap, Is.LessThan(.08f),
            "The sinking block separated from its passenger instead of carrying it continuously.");

        Object.Destroy(block.gameObject);
        Object.Destroy(playerObject);
        Object.Destroy(settings);
    }

    [UnityTest]
    public IEnumerator SlowEntryFromLeftSwitchesOnceAndKeepsSinking()
    {
        yield return WalkOntoPlatform(-1f);
    }

    [UnityTest]
    public IEnumerator SlowEntryFromRightSwitchesOnceAndKeepsSinking()
    {
        yield return WalkOntoPlatform(1f);
    }

    private static IEnumerator WalkOntoPlatform(float side)
    {
        PlayerMovementSettings settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
        SinkingEarthBlock2D block = CreateBlock();
        BoxCollider2D blockCollider = block.GetComponent<BoxCollider2D>();

        GameObject terrainObject = new("Terrain");
        terrainObject.transform.position = new Vector2(side * 2f, 0f);
        BoxCollider2D terrainCollider = terrainObject.AddComponent<BoxCollider2D>();
        terrainCollider.size = new Vector2(2f, 1f);

        GameObject playerObject = new("Player");
        playerObject.transform.position = new Vector2(side * 1.5f, 1.4f);
        BoxCollider2D playerCollider = playerObject.AddComponent<BoxCollider2D>();
        playerCollider.size = new Vector2(.8f, 1.8f);
        Rigidbody2D playerBody = playerObject.AddComponent<Rigidbody2D>();
        playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        PlayerController2D player = playerObject.AddComponent<PlayerController2D>();
        player.Configure(null, settings);
        Physics2D.SyncTransforms();

        bool sinkingStarted = false;
        int upwardStepsAfterSink = 0;
        int supportSwitches = 0;
        Collider2D previousSupport = null;
        float previousBlockY = block.GetComponent<Rigidbody2D>().position.y;
        for (int i = 0; i < 140; i++)
        {
            Vector2 position = playerBody.position;
            position.x -= side * .01f;
            playerBody.position = position;
            playerBody.linearVelocity = new Vector2(0f, playerBody.linearVelocity.y);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Collider2D currentSupport = player.SupportCollider;
            if (previousSupport != null && currentSupport != previousSupport) supportSwitches++;
            previousSupport = currentSupport;

            float currentBlockY = block.GetComponent<Rigidbody2D>().position.y;
            if (block.CurrentWeight > 0f) sinkingStarted = true;
            if (sinkingStarted && currentBlockY > previousBlockY + .001f) upwardStepsAfterSink++;
            previousBlockY = currentBlockY;
        }

        Assert.That(sinkingStarted, Is.True, "The Player never completed the transition onto the platform.");
        Assert.That(player.SupportCollider, Is.EqualTo(blockCollider));
        Assert.That(supportSwitches, Is.LessThanOrEqualTo(1),
            "Support ownership oscillated while slowly crossing the Terrain/platform seam.");
        Assert.That(upwardStepsAfterSink, Is.Zero,
            "The platform reversed upward after its supported descent had begun.");

        Object.Destroy(block.gameObject);
        Object.Destroy(terrainObject);
        Object.Destroy(playerObject);
        Object.Destroy(settings);
    }

    private static SinkingEarthBlock2D CreateBlock()
    {
        GameObject blockObject = new("SinkingEarthBlock2D");
        Rigidbody2D body = blockObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        BoxCollider2D collider = blockObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(2f, 1f);
        blockObject.AddComponent<SurfaceSemantic2D>();
        return blockObject.AddComponent<SinkingEarthBlock2D>();
    }

    private static Rigidbody2D CreateActor(Vector2 position)
    {
        GameObject actorObject = new("Actor");
        actorObject.transform.position = position;
        Rigidbody2D body = actorObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        BoxCollider2D collider = actorObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(.8f, 1.8f);
        return body;
    }
}
