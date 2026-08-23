using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class MovingPlatformPlayModeTests
{
    [UnityTest]
    public IEnumerator MovesDeterministicallyAndResetRestoresInitialState()
    {
        MovingPlatform2D platform = CreatePlatform();
        platform.ConfigurePath(Vector2.zero, new Vector2(2f, 0f), 1f, .1f, .25f, true, true);
        Vector2 initialPosition = platform.GetComponent<Rigidbody2D>().position;

        for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();

        Assert.That(platform.transform.position.x, Is.GreaterThan(initialPosition.x + .1f));
        platform.SetMoving(false);
        platform.ResetRoomState();

        Assert.That(Vector2.Distance(platform.GetComponent<Rigidbody2D>().position, initialPosition), Is.LessThan(.001f));
        Assert.That(platform.Phase, Is.EqualTo(.25f).Within(.0001f));
        Assert.That(platform.IsTowardsEnd, Is.True);
        Assert.That(platform.IsMoving, Is.True);
        Object.Destroy(platform.gameObject);
    }

    [UnityTest]
    public IEnumerator WaitsAtEndpointBeforeReversing()
    {
        MovingPlatform2D platform = CreatePlatform();
        platform.ConfigurePath(Vector2.zero, new Vector2(.1f, 0f), 1f, .1f);

        for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
        Vector2 endpoint = platform.GetComponent<Rigidbody2D>().position;
        Assert.That(platform.Phase, Is.EqualTo(1f).Within(.0001f));
        Assert.That(platform.IsTowardsEnd, Is.False);
        Assert.That(platform.SurfaceVelocity.x, Is.GreaterThan(0f));

        for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
        Assert.That(Vector2.Distance(platform.GetComponent<Rigidbody2D>().position, endpoint), Is.LessThan(.001f));
        Assert.That(platform.SurfaceVelocity, Is.EqualTo(Vector2.zero));

        yield return new WaitForFixedUpdate();
        Assert.That(platform.GetComponent<Rigidbody2D>().position.x, Is.LessThan(endpoint.x));
        Object.Destroy(platform.gameObject);
    }

    [UnityTest]
    public IEnumerator CarriesPlayerAndMirrorCloneWithoutParentingThem()
    {
        PlayerMovementSettings settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
        MovingPlatform2D platform = CreatePlatform();
        platform.GetComponent<BoxCollider2D>().size = new Vector2(3f, .5f);
        platform.ConfigurePath(Vector2.zero, new Vector2(2f, 0f), 1f, 0f);

        GameObject playerObject = new("Player");
        playerObject.transform.position = new Vector2(-.6f, 1.15f);
        playerObject.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.8f);
        Rigidbody2D playerBody = playerObject.AddComponent<Rigidbody2D>();
        playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        PlayerController2D player = playerObject.AddComponent<PlayerController2D>();
        player.Configure(null, settings);

        GameObject cloneObject = new("MirrorClone");
        cloneObject.transform.position = new Vector2(.6f, 1.15f);
        cloneObject.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.8f);
        Rigidbody2D cloneBody = cloneObject.AddComponent<Rigidbody2D>();
        cloneBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        MirrorCloneController2D clone = cloneObject.AddComponent<MirrorCloneController2D>();
        clone.Configure(player, Vector2.left, Vector2.down);
        Physics2D.SyncTransforms();

        Vector2 playerOffset = playerObject.transform.position - platform.transform.position;
        Vector2 cloneOffset = cloneObject.transform.position - platform.transform.position;
        for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();

        Assert.That(platform.transform.position.x, Is.GreaterThan(.1f));
        Vector2 currentPlayerOffset = (Vector2)playerObject.transform.position - (Vector2)platform.transform.position;
        Vector2 currentCloneOffset = (Vector2)cloneObject.transform.position - (Vector2)platform.transform.position;
        Assert.That(Vector2.Distance(currentPlayerOffset, playerOffset), Is.LessThan(.04f));
        Assert.That(Vector2.Distance(currentCloneOffset, cloneOffset), Is.LessThan(.04f));
        Assert.That(playerObject.transform.parent, Is.Null);
        Assert.That(cloneObject.transform.parent, Is.Null);
        Assert.That(player.AppliedSurfaceVelocity.x, Is.EqualTo(1f).Within(.02f));
        Assert.That(clone.AppliedSurfaceVelocity.x, Is.EqualTo(1f).Within(.02f));

        Object.Destroy(platform.gameObject);
        Object.Destroy(playerObject);
        Object.Destroy(cloneObject);
        Object.Destroy(settings);
    }

    [UnityTest]
    public IEnumerator CarriesMirrorCloneUsingItsRotatedGravityDirection()
    {
        PlayerMovementSettings settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
        MovingPlatform2D platform = CreatePlatform();
        platform.ConfigurePath(Vector2.zero, new Vector2(0f, 2f), 1f, 0f);

        GameObject sourceObject = new("PlayerSource");
        sourceObject.transform.position = new Vector2(10f, 10f);
        sourceObject.AddComponent<BoxCollider2D>();
        sourceObject.AddComponent<Rigidbody2D>();
        PlayerController2D source = sourceObject.AddComponent<PlayerController2D>();
        source.Configure(null, settings);

        GameObject cloneObject = new("SidewaysMirrorClone");
        cloneObject.transform.position = new Vector2(-1.4f, 0f);
        cloneObject.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1f);
        cloneObject.AddComponent<Rigidbody2D>();
        MirrorCloneController2D clone = cloneObject.AddComponent<MirrorCloneController2D>();
        clone.Configure(source, Vector2.up, Vector2.right);
        Physics2D.SyncTransforms();

        Vector2 initialOffset = cloneObject.transform.position - platform.transform.position;
        for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();

        Vector2 currentOffset = (Vector2)cloneObject.transform.position - (Vector2)platform.transform.position;
        Assert.That(platform.transform.position.y, Is.GreaterThan(.1f));
        Assert.That(Vector2.Distance(currentOffset, initialOffset), Is.LessThan(.04f));
        Assert.That(clone.AppliedSurfaceVelocity.y, Is.EqualTo(1f).Within(.02f));

        Object.Destroy(platform.gameObject);
        Object.Destroy(sourceObject);
        Object.Destroy(cloneObject);
        Object.Destroy(settings);
    }

    [UnityTest]
    public IEnumerator PublishesCurrentStepVelocityAndClearsItWhenStopped()
    {
        MovingPlatform2D platform = CreatePlatform();
        platform.ConfigurePath(Vector2.zero, new Vector2(2f, 0f), 1.5f, 0f);

        yield return new WaitForFixedUpdate();

        Assert.That(platform.TryGetSurfaceVelocity(Vector2.zero, Vector2.up, out Vector2 velocity), Is.True);
        Assert.That(velocity.x, Is.EqualTo(1.5f).Within(.02f));
        platform.SetMoving(false);
        Assert.That(platform.TryGetSurfaceVelocity(Vector2.zero, Vector2.up, out velocity), Is.True);
        Assert.That(velocity, Is.EqualTo(Vector2.zero));
        Object.Destroy(platform.gameObject);
    }

    private static MovingPlatform2D CreatePlatform()
    {
        GameObject platformObject = new("MovingPlatform2D");
        Rigidbody2D body = platformObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        platformObject.AddComponent<BoxCollider2D>().size = new Vector2(2f, .5f);
        platformObject.AddComponent<SurfaceSemantic2D>();
        return platformObject.AddComponent<MovingPlatform2D>();
    }
}
