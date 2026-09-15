using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PushableCrateBrakeTests
{
    private Scene preview;
    private PushableCrate2D crate;
    private Rigidbody2D body;
    private PlayerController2D player;
    private static readonly BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    [SetUp]
    public void SetUp()
    {
        preview = EditorSceneManager.NewPreviewScene();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Props/PushableCrate2D.prefab");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
        crate = instance.GetComponent<PushableCrate2D>();
        crate.ResetRoomState();
        body = instance.GetComponent<Rigidbody2D>();
        var actor = Actor("Player", new Vector2(-.97f, 0f));
        player = actor.AddComponent<PlayerController2D>();
        typeof(PlayerController2D).GetMethod("Awake", PrivateInstance).Invoke(player, null);
        Physics2D.SyncTransforms();
    }

    [TearDown]
    public void TearDown()
    {
        if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
    }

    private GameObject Actor(string name, Vector2 position)
    {
        var actor = new GameObject(name);
        SceneManager.MoveGameObjectToScene(actor, preview);
        actor.transform.position = position;
        actor.AddComponent<Rigidbody2D>();
        actor.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.8f);
        return actor;
    }

    private void Step()
    {
        Physics2D.SyncTransforms();
        typeof(PushableCrate2D).GetMethod("FixedUpdate", PrivateInstance).Invoke(crate, null);
    }

    private bool IsBraked => (body.constraints & RigidbodyConstraints2D.FreezePositionX) != 0;

    [Test]
    public void ReleaseStopsHorizontalVelocityButPreservesFallingAndCanResume()
    {
        player.SetMoveInput(Vector2.right);
        Step();
        Assert.That(IsBraked, Is.False);
        body.linearVelocity = new Vector2(4f, -3f);
        player.SetMoveInput(Vector2.zero);
        Step();
        Assert.That(IsBraked, Is.True);
        Assert.That(body.linearVelocity, Is.EqualTo(new Vector2(0f, -3f)));
        Assert.That((body.constraints & RigidbodyConstraints2D.FreezePositionY), Is.EqualTo(RigidbodyConstraints2D.None));
        player.SetMoveInput(Vector2.right);
        Step();
        Assert.That(IsBraked, Is.False);
    }

    [Test]
    public void OppositeOrDistantInputCannotKeepCrateMoving()
    {
        player.SetMoveInput(Vector2.left);
        Step();
        Assert.That(IsBraked, Is.True);
        player.transform.position = new Vector3(-3f, 0f, 0f);
        player.SetMoveInput(Vector2.right);
        Step();
        Assert.That(IsBraked, Is.True);
    }

    [Test]
    public void StandingOnTopDoesNotCountAsSidePushing()
    {
        player.transform.position = new Vector3(0f, 1.48f, 0f);
        player.SetMoveInput(Vector2.right);
        Step();
        Assert.That(IsBraked, Is.True);
    }

    [Test]
    public void RightSidePlayerCanPushLeftAndDisabledActorReleases()
    {
        player.transform.position = new Vector3(.99f, 0f, 0f);
        player.SetMoveInput(Vector2.left);
        Step();
        Assert.That(IsBraked, Is.False);
        player.enabled = false;
        Step();
        Assert.That(IsBraked, Is.True);
    }

    [Test]
    public void CloneUsesMappedInputAndRemovalReleasesBrake()
    {
        player.transform.position = new Vector3(-3f, 0f, 0f);
        player.SetMoveInput(Vector2.right);
        var actor = Actor("Clone", new Vector2(.99f, 0f));
        var clone = actor.AddComponent<MirrorCloneController2D>();
        clone.Configure(player, Vector2.left, Vector2.down);
        Step();
        Assert.That(IsBraked, Is.False, "Mapped right input pushes left from the right side.");
        Object.DestroyImmediate(actor);
        Step();
        Assert.That(IsBraked, Is.True);
    }
}
