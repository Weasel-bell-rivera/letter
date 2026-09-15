using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Center001CameraAssetTests
{
    private const string ScenePath = "Assets/Scenes/Levels/Center/Center_001.unity";

    [Test]
    public void SceneOwnsOneExplicitlyConfiguredHorizontalFollowCamera()
    {
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera[] cameras = ComponentsInScene<Camera>(scene);
            CameraFollow2D[] controllers = ComponentsInScene<CameraFollow2D>(scene);
            RoomPlayerSpawner2D[] spawners = ComponentsInScene<RoomPlayerSpawner2D>(scene);
            RoomResetSystem[] resets = ComponentsInScene<RoomResetSystem>(scene);

            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(controllers, Has.Length.EqualTo(1));
            Assert.That(spawners, Has.Length.EqualTo(1));
            Assert.That(resets, Has.Length.EqualTo(1));
            Camera camera = cameras[0];
            CameraFollow2D controller = controllers[0];
            RoomPlayerSpawner2D spawner = spawners[0];
            RoomResetSystem reset = resets[0];
            Assert.That(controller.gameObject, Is.SameAs(camera.gameObject));
            Assert.That(camera.gameObject.CompareTag("MainCamera"), Is.True);
            Assert.That(camera.orthographic, Is.True);
            Assert.That(camera.orthographicSize, Is.EqualTo(7f).Within(.001f));
            Assert.That(camera.transform.position.y, Is.EqualTo(-1.5f).Within(.001f));

            Assert.That(controller.Target, Is.Null, "The Scene must not serialize a runtime Player target.");
            Assert.That(controller.FollowsVertical, Is.False);
            Assert.That(controller.SmoothTime, Is.EqualTo(.15f).Within(.001f));
            Assert.That(controller.UsesExplicitFramingOffset, Is.True);
            Assert.That(controller.FramingOffset, Is.EqualTo(Vector2.zero));
            Assert.That(controller.UsesRoomBounds, Is.True);
            Assert.That(controller.RoomBounds, Is.EqualTo(new Rect(-14f, -8.5f, 28f, 14f)));

            Assert.That(spawner.gameObject, Is.Not.SameAs(camera.gameObject),
                "Room spawning belongs to RoomSystems, not the camera GameObject.");
            Assert.That(spawner.RoomCamera, Is.SameAs(controller));
            SerializedObject serializedReset = new(reset);
            Assert.That(serializedReset.FindProperty("cameraFollow").objectReferenceValue,
                Is.SameAs(controller));
        }
        finally
        {
            if (previousSetup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }
    }

    [Test]
    public void ExplicitFramingIgnoresBindingTransformDifferenceAndClampsTheWholeView()
    {
        GameObject cameraObject = new("Test Camera");
        GameObject targetObject = new("Test Player Target");
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            camera.aspect = 16f / 9f;
            cameraObject.transform.position = new Vector3(100f, -1.5f, -10f);
            targetObject.transform.position = new Vector3(-13f, -2.1f, 0f);

            CameraFollow2D controller = cameraObject.AddComponent<CameraFollow2D>();
            controller.ConfigureFraming(Vector2.zero);
            controller.ConfigureBounds(new Rect(-14f, -8.5f, 28f, 14f));
            controller.ConfigureDamping(.15f);
            controller.Configure(targetObject.transform, false);
            controller.SnapToTarget();

            Assert.That(cameraObject.transform.position.x - camera.orthographicSize * camera.aspect,
                Is.EqualTo(-14f).Within(.001f));
            Assert.That(cameraObject.transform.position.y, Is.EqualTo(-1.5f).Within(.001f));

            targetObject.transform.position = new Vector3(13f, -2.1f, 0f);
            controller.SnapToTarget();
            Assert.That(cameraObject.transform.position.x + camera.orthographicSize * camera.aspect,
                Is.EqualTo(14f).Within(.001f));
            Assert.That(cameraObject.transform.position.y, Is.EqualTo(-1.5f).Within(.001f));
        }
        finally
        {
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void BindingFarSpawnClampsImmediatelyAndLegacyEntryFramingCannotOverrideIt()
    {
        GameObject cameraObject = new("Test Camera");
        GameObject first = new("Player");
        GameObject replacement = new("Replacement Player");
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            camera.aspect = 1f;
            cameraObject.transform.position = new Vector3(-100f, 3f, -10f);
            CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
            follow.ConfigureFraming(new Vector2(0f, .56f));
            follow.ConfigureBounds(new Rect(-10f, -4f, 148f, 18f));
            follow.ConfigureEntryFramingBounds(new Rect(-10f, -4f, 30f, 14f));
            follow.ConfigureDamping(1f);
            first.transform.position = new Vector3(100f, 5f, 0f);
            follow.Configure(first.transform, true);
            Assert.That(follow.transform.position.x, Is.EqualTo(100f).Within(.001f));
            Assert.That(follow.transform.position.y, Is.EqualTo(5.56f).Within(.001f));
            follow.BeginEntryFraming();
            Assert.That(follow.transform.position.x, Is.EqualTo(100f).Within(.001f));

            // Rebinding at a room edge must disregard the old target and smoothing state.
            replacement.transform.position = new Vector3(138f, 30f, 0f);
            follow.BindTarget(replacement.transform);
            Assert.That(follow.Target, Is.SameAs(replacement.transform));
            Assert.That(follow.transform.position, Is.EqualTo(new Vector3(131f, 7f, -10f)));
            replacement.transform.position = new Vector3(-10f, -10f, 0f);
            follow.SnapToTarget();
            Assert.That(follow.transform.position, Is.EqualTo(new Vector3(-3f, 3f, -10f)));
            follow.BindTarget(null);
            follow.BeginEntryFraming();
            Assert.That(follow.Target, Is.Null);
            Assert.That(follow.transform.position, Is.EqualTo(new Vector3(-3f, 3f, -10f)));
        }
        finally
        {
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(replacement);
            Object.DestroyImmediate(cameraObject);
        }
    }

    private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
}
