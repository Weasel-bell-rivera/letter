using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public sealed class WindRayEnemyAssetTests
{
    private const string SettingsPath = "Assets/Settings/Enemies/DefaultWindRayEnemy.asset";
    private const string PrefabPath = "Assets/Prefabs/Gameplay/Enemies/WindRayEnemy2D.prefab";
    private const string ScenePath = "Assets/Scenes/Levels/Wind/Wind_001.unity";
    private const string FlyClipPath = "Assets/Animations/Enemies/WindRay/WindRayFly.anim";
    private const string RestSpritePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Enemies/Double/Bee/bee_rest.png";
    private const string WingUpSpritePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Enemies/Double/Bee/bee_a.png";
    private const string WingDownSpritePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Enemies/Double/Bee/bee_b.png";

    [Test]
    public void SharedSettingsMatchApprovedValues()
    {
        WindRayEnemySettings settings = AssetDatabase.LoadAssetAtPath<WindRayEnemySettings>(SettingsPath);
        Assert.That(settings, Is.Not.Null);
        Assert.That(settings.IsValid, Is.True);
        Assert.That(settings.DetectionRadius, Is.EqualTo(6f));
        Assert.That(settings.EdgeHintDistance, Is.EqualTo(.75f));
        Assert.That(settings.WindupDuration, Is.EqualTo(.75f));
        Assert.That(settings.DashSpeed, Is.EqualTo(12f));
        Assert.That(settings.MaximumDashDistance, Is.EqualTo(7f));
        Assert.That(settings.RecoveryDuration, Is.EqualTo(1.5f));
        Assert.That(settings.ReturnSpeed, Is.EqualTo(2f));
        Assert.That(settings.PositionTolerance, Is.EqualTo(.05f));
    }

    [Test]
    public void PrefabContainsRequiredSharedComponentsAndNoRoomCoordinates()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.transform.position, Is.EqualTo(Vector3.zero));

        WindRayEnemy2D enemy = prefab.GetComponent<WindRayEnemy2D>();
        Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
        WindRayDamageTrigger2D trigger = prefab.GetComponentInChildren<WindRayDamageTrigger2D>(true);
        Animator animator = prefab.GetComponent<Animator>();
        Assert.That(enemy, Is.Not.Null);
        Assert.That(enemy.Settings, Is.EqualTo(AssetDatabase.LoadAssetAtPath<WindRayEnemySettings>(SettingsPath)));
        Assert.That(body, Is.Not.Null);
        Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
        Assert.That(body.freezeRotation, Is.True);
        Assert.That(trigger, Is.Not.Null);
        Assert.That(trigger.GetComponent<Collider2D>().isTrigger, Is.True);
        Assert.That(((BoxCollider2D)trigger.GetComponent<Collider2D>()).size,
            Is.EqualTo(new Vector2(1.15f, .7f)));
        Assert.That(animator, Is.Not.Null);
        Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
        Assert.That(AssetDatabase.LoadAssetAtPath<AnimationClip>(FlyClipPath), Is.Not.Null);
        Assert.That(prefab.transform.Find("Visual/BodyVisual"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Visual/UpperWing"), Is.Null);
        Assert.That(prefab.transform.Find("Visual/LowerWing"), Is.Null);
        Assert.That(prefab.transform.Find("Visual/Eye"), Is.Null);
        Assert.That(prefab.transform.Find("Visual/RangeFeedback"), Is.Null);
        Assert.That(prefab.transform.Find("Visual/TargetMarker"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Visual/DashTrail"), Is.Not.Null);
        Assert.That(prefab.transform.Find("LineOfSightOrigin"), Is.Not.Null);
    }

    [Test]
    public void FlyAnimationUsesThreeBeeSpritesAndLoopsAtEightFps()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(FlyClipPath);
        Assert.That(clip, Is.Not.Null);
        Assert.That(clip.frameRate, Is.EqualTo(8f));
        Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime, Is.True);

        EditorCurveBinding binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single(item =>
            item.path == "Visual/BodyVisual" && item.propertyName == "m_Sprite");
        ObjectReferenceKeyframe[] frames = AnimationUtility.GetObjectReferenceCurve(clip, binding);
        Object[] expected =
        {
            AssetDatabase.LoadAssetAtPath<Sprite>(RestSpritePath),
            AssetDatabase.LoadAssetAtPath<Sprite>(WingUpSpritePath),
            AssetDatabase.LoadAssetAtPath<Sprite>(WingDownSpritePath),
            AssetDatabase.LoadAssetAtPath<Sprite>(WingUpSpritePath),
            AssetDatabase.LoadAssetAtPath<Sprite>(RestSpritePath)
        };
        Assert.That(frames.Select(frame => frame.value), Is.EqualTo(expected));
    }

    [Test]
    public void Wind001UsesTilemapsAndOneConnectedWindRayPrefab()
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            Tilemap terrain = ComponentsInScene<Tilemap>(scene).Single(map => map.name == "Terrain");
            Tilemap hazard = ComponentsInScene<Tilemap>(scene).Single(map => map.name == "Hazard");
            Assert.That(CountTiles(terrain), Is.EqualTo(28));
            Assert.That(CountTiles(hazard), Is.EqualTo(32));
            for (int x = -14; x <= 13; x++)
                Assert.That(terrain.HasTile(new Vector3Int(x, -3, 0)), Is.True, $"Terrain gap at x={x}.");
            for (int x = -16; x <= 15; x++)
                Assert.That(hazard.HasTile(new Vector3Int(x, -7, 0)), Is.True, $"Hazard gap at x={x}.");

            Assert.That(terrain.GetComponent<CompositeCollider2D>().pathCount, Is.GreaterThan(0));
            Assert.That(terrain.GetComponent<SurfaceSemantic2D>().Type,
                Is.EqualTo(SurfaceSemantic2D.SurfaceType.StaticSolid));
            Assert.That(terrain.GetComponent<MirrorSurface2D>().kind,
                Is.EqualTo(MirrorSurface2D.SurfaceKind.Ground));
            Assert.That(hazard.GetComponent<CompositeCollider2D>().pathCount, Is.GreaterThan(0));
            Assert.That(hazard.GetComponent<Hazard2D>(), Is.Not.Null);

            WindRayEnemy2D enemy = ComponentsInScene<WindRayEnemy2D>(scene).Single();
            Assert.That(enemy.name, Is.EqualTo("WindRay-UpperRight"));
            Assert.That(enemy.transform.position, Is.EqualTo(new Vector3(6f, 2f, 0f)));
            Assert.That(PrefabUtility.GetPrefabInstanceStatus(enemy.gameObject),
                Is.EqualTo(PrefabInstanceStatus.Connected));
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(enemy.gameObject), Is.Not.Null);
            Assert.That(ComponentsInScene<PlayerController2D>(scene), Is.Empty);
            Assert.That(ComponentsInScene<MirrorPlayer2D>(scene), Is.Empty);
            Assert.That(ComponentsInScene<RoomPlayerSpawner2D>(scene), Has.Length.EqualTo(1));
            Assert.That(ComponentsInScene<RoomEntrance2D>(scene).Count(entrance => entrance.IsDefault), Is.EqualTo(1));
            Assert.That(ComponentsInScene<RoomResetSystem>(scene).Length, Is.EqualTo(1));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        Assert.That(EditorBuildSettings.scenes.Any(entry => entry.enabled && entry.path == ScenePath), Is.True);
    }

    private static int CountTiles(Tilemap map)
    {
        int count = 0;
        foreach (Vector3Int position in map.cellBounds.allPositionsWithin)
            if (map.HasTile(position)) count++;
        return count;
    }

    private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
}
