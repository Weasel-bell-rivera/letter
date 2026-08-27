using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SpringPlayModeTests
{
    [SetUp]
    public void ResetAbilityState() => MirrorAbilityState.ResetForTests();

    [UnityTest]
    public IEnumerator PlayerTopBounceUsesFiveUnitHeightAndIsNotJumpCut()
    {
        PlayerMovementSettings settings = CreateSettings();
        Spring2D spring = CreateSpring(Vector2.zero);
        PlayerController2D player = CreatePlayer(new Vector2(0f, 1.95f), settings);
        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        playerBody.linearVelocity = new Vector2(0f, -4f);
        Physics2D.SyncTransforms();

        yield return WaitForUpwardBounce(playerBody);

        float expected = Mathf.Sqrt(2f * settings.Gravity * Spring2D.DefaultTopLaunchHeight);
        Assert.That(playerBody.linearVelocity.y, Is.EqualTo(expected).Within(1.25f));
        float firstBounceVelocity = playerBody.linearVelocity.y;

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.That(playerBody.linearVelocity.y, Is.GreaterThan(firstBounceVelocity - settings.Gravity * .08f));
        DestroyObjects(settings, spring.gameObject, player.gameObject);
    }

    [UnityTest]
    public IEnumerator PlayerAndNormalGravityCloneReceiveSameTopOutput()
    {
        PlayerMovementSettings settings = CreateSettings();
        Spring2D playerSpring = CreateSpring(new Vector2(-2f, 0f));
        Spring2D cloneSpring = CreateSpring(new Vector2(2f, 0f));
        PlayerController2D player = CreatePlayer(new Vector2(-2f, 1.95f), settings);
        PlayerController2D source = CreatePlayer(new Vector2(20f, 20f), settings);
        MirrorCloneController2D clone = CreateClone(new Vector2(2f, 1.95f), source, Vector2.left, Vector2.down);
        player.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(0f, -4f);
        clone.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(0f, -4f);
        Physics2D.SyncTransforms();

        yield return WaitForUpwardBounce(player.GetComponent<Rigidbody2D>());
        yield return WaitForUpwardBounce(clone.GetComponent<Rigidbody2D>());

        Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.y,
            Is.EqualTo(clone.GetComponent<Rigidbody2D>().linearVelocity.y).Within(1.25f));
        DestroyObjects(settings, playerSpring.gameObject, cloneSpring.gameObject, player.gameObject,
            source.gameObject, clone.gameObject);
    }

    [UnityTest]
    public IEnumerator SideFacesPointOutwardAndBottomFaceDoesNotBounce()
    {
        Spring2D spring = CreateSpring(Vector2.zero);
        TestBounceReceiver left = CreateReceiver(new Vector2(-.91f, .5f), Vector2.right * 4f);
        TestBounceReceiver right = CreateReceiver(new Vector2(.91f, .5f), Vector2.left * 4f);
        TestBounceReceiver bottom = CreateReceiver(new Vector2(0f, -.41f), Vector2.up * 4f);
        Physics2D.SyncTransforms();

        for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();

        Assert.That(left.BounceCount, Is.EqualTo(1));
        Assert.That(left.LastNormal, Is.EqualTo(Vector2.left));
        Assert.That(left.LastSpeed, Is.EqualTo(Spring2D.DefaultSideLaunchSpeed));
        Assert.That(right.BounceCount, Is.EqualTo(1));
        Assert.That(right.LastNormal, Is.EqualTo(Vector2.right));
        Assert.That(right.LastSpeed, Is.EqualTo(Spring2D.DefaultSideLaunchSpeed));
        Assert.That(bottom.BounceCount, Is.Zero);
        DestroyObjects(null, spring.gameObject, left.gameObject, right.gameObject, bottom.gameObject);
    }

    [UnityTest]
    public IEnumerator ContinuousContactTriggersOnceAndResetAllowsFreshTrigger()
    {
        Spring2D spring = CreateSpring(Vector2.zero);
        TestBounceReceiver receiver = CreateReceiver(new Vector2(0f, 1.4f), Vector2.down);
        Physics2D.SyncTransforms();

        for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
        Assert.That(receiver.BounceCount, Is.EqualTo(1));

        spring.ResetRoomState();
        yield return new WaitForFixedUpdate();
        Assert.That(receiver.BounceCount, Is.EqualTo(2));
        DestroyObjects(null, spring.gameObject, receiver.gameObject);
    }

    [UnityTest]
    public IEnumerator MirrorPlacementIsRejectedOnSpring()
    {
        PlayerMovementSettings settings = CreateSettings();
        Spring2D spring = CreateSpring(Vector2.zero);
        PlayerController2D player = CreatePlayer(new Vector2(0f, 1.9f), settings);
        MirrorPlayer2D mirror = player.gameObject.AddComponent<MirrorPlayer2D>();
        mirror.Configure(player);
        mirror.SetInitiallyUnlocked(true);
        Physics2D.SyncTransforms();

        Assert.That(player.IsGroundedNow, Is.True);
        Assert.That(mirror.TryPlace(), Is.False);
        Assert.That(mirror.LastFailure, Is.EqualTo(MirrorPlayer2D.PlacementFailure.NoSurface));
        Assert.That(mirror.Clone, Is.Null);
        DestroyObjects(settings, spring.gameObject, player.gameObject);
        yield return null;
    }

    private static IEnumerator WaitForUpwardBounce(Rigidbody2D body)
    {
        for (int i = 0; i < 12; i++)
        {
            yield return new WaitForFixedUpdate();
            if (body != null && body.linearVelocity.y > 10f) yield break;
        }
        Assert.Fail("Character did not receive the expected upward spring bounce.");
    }

    private static Spring2D CreateSpring(Vector2 position)
    {
        GameObject springObject = new("Spring2D");
        springObject.transform.position = position;
        springObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        BoxCollider2D collider = springObject.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.offset = new Vector2(0f, .5f);
        springObject.AddComponent<SurfaceSemantic2D>();
        return springObject.AddComponent<Spring2D>();
    }

    private static PlayerController2D CreatePlayer(Vector2 position, PlayerMovementSettings settings)
    {
        GameObject playerObject = new("Player");
        playerObject.transform.position = position;
        playerObject.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.8f);
        Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        PlayerController2D player = playerObject.AddComponent<PlayerController2D>();
        player.Configure(null, settings);
        return player;
    }

    private static MirrorCloneController2D CreateClone(Vector2 position, PlayerController2D source,
        Vector2 moveAxis, Vector2 gravityAxis)
    {
        GameObject cloneObject = new("MirrorClone");
        cloneObject.transform.position = position;
        cloneObject.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.8f);
        Rigidbody2D body = cloneObject.AddComponent<Rigidbody2D>();
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        MirrorCloneController2D clone = cloneObject.AddComponent<MirrorCloneController2D>();
        clone.Configure(source, moveAxis, gravityAxis);
        return clone;
    }

    private static TestBounceReceiver CreateReceiver(Vector2 position, Vector2 contactVelocity)
    {
        GameObject receiverObject = new("TestBounceReceiver");
        receiverObject.transform.position = position;
        receiverObject.AddComponent<BoxCollider2D>().size = new Vector2(.8f, .8f);
        Rigidbody2D body = receiverObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.linearVelocity = contactVelocity;
        TestBounceReceiver receiver = receiverObject.AddComponent<TestBounceReceiver>();
        receiver.ContactVelocity = contactVelocity;
        return receiver;
    }

    private static PlayerMovementSettings CreateSettings()
        => ScriptableObject.CreateInstance<PlayerMovementSettings>();

    private static void DestroyObjects(PlayerMovementSettings settings, params GameObject[] objects)
    {
        foreach (GameObject target in objects)
            if (target != null) Object.DestroyImmediate(target);
        if (settings != null) Object.DestroyImmediate(settings);
    }

    private sealed class TestBounceReceiver : MonoBehaviour, ISpringBounceReceiver2D
    {
        public Vector2 ContactVelocity { get; set; }
        public int BounceCount { get; private set; }
        public Vector2 LastNormal { get; private set; }
        public float LastSpeed { get; private set; }
        public float SpringGravityMagnitude => 48f;
        public Vector2 SpringContactVelocity => ContactVelocity;

        public bool ApplySpringBounce(Vector2 outwardNormal, float launchSpeed)
        {
            BounceCount++;
            LastNormal = outwardNormal;
            LastSpeed = launchSpeed;
            return true;
        }
    }
}
