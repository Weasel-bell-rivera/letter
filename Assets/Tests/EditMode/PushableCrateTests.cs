using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PushableCrateTests
{
    private const string PrefabPath = "Assets/Prefabs/Gameplay/Props/PushableCrate2D.prefab";
    private Scene preview;
    private GameObject crateObject;
    private PushableCrate2D crate;

    [SetUp]
    public void SetUp()
    {
        preview = EditorSceneManager.NewPreviewScene();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        crateObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
        crateObject.transform.position = new Vector3(0f, .61f, 0f);
        crate = crateObject.GetComponent<PushableCrate2D>();
        crate.ResetRoomState();
    }

    [TearDown]
    public void TearDown()
    {
        if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
    }

    [Test]
    public void PrefabIsPhysicalSafeDynamicSurfaceAndCannotHostMirror()
    {
        Rigidbody2D body = crateObject.GetComponent<Rigidbody2D>();
        Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));
        Assert.That(body.mass, Is.EqualTo(2f));
        Assert.That(body.gravityScale, Is.GreaterThan(0f));
        Assert.That(body.constraints, Is.EqualTo(RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX));
        Assert.That(body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode2D.Continuous));
        Assert.That(body.interpolation, Is.EqualTo(RigidbodyInterpolation2D.Interpolate));
        Assert.That(crate.SolidCollider.size, Is.EqualTo(new Vector2(1.130806f, 1.1524358f)));
        Assert.That(crate.SolidCollider.isTrigger, Is.False);
        SurfaceSemantic2D surface = crateObject.GetComponent<SurfaceSemantic2D>();
        Assert.That(surface.Type, Is.EqualTo(SurfaceSemantic2D.SurfaceType.DynamicSurface));
        Assert.That(surface.IsSafe && !surface.IsStatic, Is.True);
        Assert.That(crateObject.GetComponent<MirrorSurface2D>(), Is.Null);
        Assert.That(crateObject.GetComponentInChildren<SpriteRenderer>().sprite, Is.Not.Null);
    }

    [Test]
    public void ResetRestoresInitialPoseAndClearsLinearAndAngularMotion()
    {
        Rigidbody2D body = crateObject.GetComponent<Rigidbody2D>();
        body.position = new Vector2(8f, -4f);
        body.rotation = 30f;
        body.linearVelocity = new Vector2(3f, -9f);
        body.angularVelocity = 10f;
        crate.ResetRoomState();
        Assert.That(body.position, Is.EqualTo(new Vector2(0f, .61f)));
        Assert.That(body.rotation, Is.EqualTo(0f));
        Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
        Assert.That(body.angularVelocity, Is.EqualTo(0f));
        Assert.That(crate.ResetOrder, Is.LessThan(0));
    }

    [Test]
    public void SurfaceVelocityUsesWorldVelocityAndStopsAfterReset()
    {
        crateObject.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(2f, -1f);
        Assert.That(crate.TryGetSurfaceVelocity(Vector2.zero, Vector2.up, out Vector2 velocity), Is.True);
        Assert.That(velocity, Is.EqualTo(new Vector2(2f, -1f)));
        crate.ResetRoomState();
        crate.TryGetSurfaceVelocity(Vector2.zero, Vector2.left, out velocity);
        Assert.That(velocity, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void PressureRequiresCompleteSupportedFootprintAndReleasesInvalidOccupants()
    {
        Assert.That(crate.HasPressureSupport(Vector2.up), Is.False);
        Assert.That(crate.HasPressureSupport(Vector2.right), Is.False);
        var ground = new GameObject("Test support");
        SceneManager.MoveGameObjectToScene(ground, preview);
        ground.transform.position = new Vector3(0f, -.5f, 0f);
        ground.AddComponent<BoxCollider2D>().size = new Vector2(4f, 1f);
        var host = new GameObject("Test pressure plate");
        SceneManager.MoveGameObjectToScene(host, preview);
        host.transform.position = new Vector3(0f, .1f, 0f);
        BoxCollider2D trigger = host.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.25f, .3f);
        PressurePlate2D plate = host.AddComponent<PressurePlate2D>();
        Physics2D.SyncTransforms();
        Assert.That(plate.IsActive, Is.True, "A supported crate can press the ordinary switch.");
        host.transform.position += Vector3.right * .3f;
        Physics2D.SyncTransforms();
        Assert.That(plate.IsActive, Is.True, "A crate resting partly on the plate still presses it.");
        host.transform.position += Vector3.right * 2f;
        Physics2D.SyncTransforms();
        Assert.That(plate.IsActive, Is.False, "Moving fully off the plate releases occupancy.");
        host.transform.position -= Vector3.right * 2f;
        host.transform.position -= Vector3.right * .3f;
        Physics2D.SyncTransforms();
        crateObject.GetComponent<Rigidbody2D>().linearVelocity = Vector2.down * 5f;
        Assert.That(plate.IsActive, Is.False, "A falling crate is not yet supported.");
        crateObject.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        crate.enabled = false;
        Assert.That(plate.IsActive, Is.False);
    }

    [Test]
    public void DisabledCrateCannotSupplyPressureOrMotion()
    {
        crate.enabled = false;
        Assert.That(crate.HasPressureSupport(Vector2.up), Is.False);
        Assert.That(crate.TryGetSurfaceVelocity(Vector2.zero, Vector2.up, out _), Is.False);
    }

    [Test]
    public void CrateCannotActivateFireballOnlySwitch()
    {
        var host = new GameObject("Fireball-only test plate");
        SceneManager.MoveGameObjectToScene(host, preview);
        PressurePlate2D plate = host.AddComponent<PressurePlate2D>();
        plate.ConfigureActivationMode(PressurePlate2D.ActivationMode.FireballLatch);
        Physics2D.SyncTransforms();
        Assert.That(plate.IsActive, Is.False);
    }
}
