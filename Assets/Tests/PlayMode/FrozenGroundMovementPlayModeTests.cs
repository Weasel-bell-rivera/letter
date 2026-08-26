using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

public sealed class FrozenGroundMovementPlayModeTests
{
    private GameObject root;
    private PlayerMovementSettings settings;
    private PhysicsMaterial2D frictionlessMaterial;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Frozen Ground Movement Tests");
        settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
        frictionlessMaterial = new PhysicsMaterial2D("Frozen Ground Test")
        {
            friction = 0f,
            bounciness = 0f
        };
    }

    [TearDown]
    public void TearDown()
    {
        if (root != null) Object.DestroyImmediate(root);
        if (settings != null) Object.DestroyImmediate(settings);
        if (frictionlessMaterial != null) Object.DestroyImmediate(frictionlessMaterial);
    }

    [UnityTest]
    public IEnumerator MovingPlayerGraduallyFreezesAtIceCellCenterAndReturnsToCheckpoint()
    {
        CreateGround("Frozen Ground", Vector2.zero, new Vector2(10f, 1f),
            SurfaceSemantic2D.SurfaceType.FrozenGround);
        PlayerController2D player = CreatePlayer(new Vector2(0f, 1.41f));
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        GameObject entranceObject = new("Entrance");
        entranceObject.transform.SetParent(root.transform);
        entranceObject.transform.position = new Vector3(-2f, 1.41f, 0f);
        RoomResetSystem reset = new GameObject("Room Reset").AddComponent<RoomResetSystem>();
        reset.transform.SetParent(root.transform);
        reset.Configure(player, null, entranceObject.transform);
        body.linearVelocity = new Vector2(3f, 0f);
        Physics2D.SyncTransforms();

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.That(player.IsOnFrozenGround, Is.True);
        Assert.That(player.FrozenGroundFreezeAmount, Is.GreaterThan(0f).And.LessThan(1f));
        Assert.That(body.linearVelocity.x, Is.GreaterThan(0f).And.LessThan(3f));

        int fixedSteps = 0;
        while (Vector2.Distance(player.transform.position, entranceObject.transform.position) > .01f && fixedSteps++ < 120)
            yield return new WaitForFixedUpdate();

        Assert.That(player.transform.position, Is.EqualTo(entranceObject.transform.position).Using(Vector3ComparerWithEqualsOperator.Instance));
        Assert.That(player.FrozenGroundFreezeAmount, Is.EqualTo(0f));
        Assert.That(body.linearVelocity.sqrMagnitude, Is.LessThan(.001f));
    }

    [UnityTest]
    public IEnumerator MirrorCloneUsesTheSameFrozenGroundRule()
    {
        CreateGround("Frozen Ground", Vector2.zero, new Vector2(10f, 1f),
            SurfaceSemantic2D.SurfaceType.FrozenGround);
        PlayerController2D source = CreatePlayer(new Vector2(20f, 20f));
        SetHorizontalInput(source, 1f);
        MirrorCloneController2D clone = CreateClone(new Vector2(0f, 1.41f), source, Vector2.right, Vector2.down);
        Rigidbody2D body = clone.GetComponent<Rigidbody2D>();
        Physics2D.SyncTransforms();

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.That(clone.IsOnFrozenGround, Is.True);
        Assert.That(body.linearVelocity.x, Is.EqualTo(0f).Within(.01f));

        SetHorizontalInput(source, 0f);
        body.linearVelocity = new Vector2(3f, body.linearVelocity.y);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.That(body.linearVelocity.x, Is.EqualTo(3f).Within(.03f));
    }

    [UnityTest]
    public IEnumerator RotatedMirrorCloneQueriesIceAlongItsLocalGravity()
    {
        CreateGround("Vertical Frozen Ground", new Vector2(.5f, 0f), new Vector2(1f, 10f),
            SurfaceSemantic2D.SurfaceType.FrozenGround);
        PlayerController2D source = CreatePlayer(new Vector2(20f, 20f));
        SetHorizontalInput(source, 1f);
        MirrorCloneController2D clone = CreateClone(new Vector2(-.41f, 0f), source, Vector2.up, Vector2.right);
        Rigidbody2D body = clone.GetComponent<Rigidbody2D>();
        Physics2D.SyncTransforms();

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.That(clone.IsOnFrozenGround, Is.True);
        Assert.That(Vector2.Dot(body.linearVelocity, Vector2.up), Is.EqualTo(0f).Within(.01f),
            "Rotated clone input must not accelerate along a vertical frozen surface.");
    }

    private PlayerController2D CreatePlayer(Vector2 position)
    {
        GameObject playerObject = new("Player");
        playerObject.transform.SetParent(root.transform);
        playerObject.transform.position = position;
        playerObject.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.8f);
        playerObject.AddComponent<Rigidbody2D>();
        PlayerController2D player = playerObject.AddComponent<PlayerController2D>();
        player.Configure(null, settings);
        return player;
    }

    private MirrorCloneController2D CreateClone(Vector2 position, PlayerController2D source,
        Vector2 moveAxis, Vector2 gravityAxis)
    {
        GameObject cloneObject = new("MirrorClone");
        cloneObject.transform.SetParent(root.transform);
        cloneObject.transform.position = position;
        cloneObject.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.8f);
        cloneObject.AddComponent<Rigidbody2D>();
        MirrorCloneController2D clone = cloneObject.AddComponent<MirrorCloneController2D>();
        clone.Configure(source, moveAxis, gravityAxis);
        return clone;
    }

    private SurfaceSemantic2D CreateGround(string name, Vector2 position, Vector2 size,
        SurfaceSemantic2D.SurfaceType type)
    {
        GameObject ground = new(name);
        ground.transform.SetParent(root.transform);
        ground.transform.position = position;
        BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.sharedMaterial = frictionlessMaterial;
        SurfaceSemantic2D semantic = ground.AddComponent<SurfaceSemantic2D>();
        semantic.Configure(type, true, true);
        return semantic;
    }

    private static void SetHorizontalInput(PlayerController2D player, float value)
    {
        typeof(PlayerController2D).GetField("input", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(player, value);
    }
}
