using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public sealed class GroundFireThrowerEnemyAssetTests
{
    private const string SettingsPath =
        "Assets/Settings/Enemies/DefaultGroundFireThrowerEnemy.asset";
    private const string EnemyPrefabPath =
        "Assets/Prefabs/Gameplay/Enemies/GroundFireThrowerEnemy2D.prefab";
    private const string ProjectilePrefabPath =
        "Assets/Prefabs/Gameplay/Projectiles/ArcFireballProjectile2D.prefab";
    private const string ScenePath = "Assets/Scenes/Levels/Earth/Earth_001.unity";

    [Test]
    public void SharedSettingsMatchApprovedPrototypeValues()
    {
        GroundFireThrowerEnemySettings settings =
            AssetDatabase.LoadAssetAtPath<GroundFireThrowerEnemySettings>(SettingsPath);
        Assert.That(settings, Is.Not.Null);
        Assert.That(settings.IsValid, Is.True);
        Assert.That(settings.DetectionRadius, Is.EqualTo(7f));
        Assert.That(settings.WindupDuration, Is.EqualTo(.8f));
        Assert.That(settings.ProjectileSpeed, Is.EqualTo(7f));
        Assert.That(settings.ArcHeight, Is.EqualTo(2f));
        Assert.That(settings.CooldownDuration, Is.EqualTo(1.8f));
        Assert.That(settings.ProjectileLifetime, Is.EqualTo(3f));
        Assert.That(settings.ProjectileRadius, Is.EqualTo(.35f));
    }

    [Test]
    public void EnemyAndProjectilePrefabsContainRequiredComponents()
    {
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
        Assert.That(enemyPrefab, Is.Not.Null);
        Assert.That(projectilePrefab, Is.Not.Null);
        Assert.That(enemyPrefab.transform.position, Is.EqualTo(Vector3.zero));
        Assert.That(projectilePrefab.transform.position, Is.EqualTo(Vector3.zero));

        GroundFireThrowerEnemy2D enemy = enemyPrefab.GetComponent<GroundFireThrowerEnemy2D>();
        Assert.That(enemy, Is.Not.Null);
        Assert.That(enemy.Settings,
            Is.EqualTo(AssetDatabase.LoadAssetAtPath<GroundFireThrowerEnemySettings>(SettingsPath)));
        Assert.That(enemy.ProjectilePrefab,
            Is.EqualTo(projectilePrefab.GetComponent<ArcFireballProjectile2D>()));
        Assert.That(enemyPrefab.GetComponent<Rigidbody2D>().bodyType,
            Is.EqualTo(RigidbodyType2D.Kinematic));
        Assert.That(enemyPrefab.transform.Find("BodyCollider").GetComponent<BoxCollider2D>().size,
            Is.EqualTo(new Vector2(.9f, .9f)));
        Assert.That(enemyPrefab.transform.Find("DamageTrigger").GetComponent<BoxCollider2D>().size,
            Is.EqualTo(new Vector2(1f, 1f)));
        Assert.That(enemyPrefab.transform.Find("DamageTrigger").GetComponent<Collider2D>().isTrigger,
            Is.True);
        Assert.That(enemyPrefab.transform.Find("RangeFeedback"), Is.Null,
            "The detection radius must not be visible in game.");
        Assert.That(enemyPrefab.transform.Find("TargetMarker"), Is.Not.Null);
        Assert.That(enemyPrefab.transform.Find("FacingRoot/ChargeVisual"), Is.Not.Null);

        ArcFireballProjectile2D projectile = projectilePrefab.GetComponent<ArcFireballProjectile2D>();
        Assert.That(projectile, Is.Not.Null);
        Assert.That(projectilePrefab.GetComponent<Rigidbody2D>().bodyType,
            Is.EqualTo(RigidbodyType2D.Kinematic));
        Assert.That(projectilePrefab.GetComponent<CircleCollider2D>().isTrigger, Is.True);
        Assert.That(projectilePrefab.GetComponent<CircleCollider2D>().radius, Is.EqualTo(.35f));
    }

    [Test]
    public void Earth001ContainsOneConnectedFireThrowerAndPreservesWallEnemy()
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            Tilemap terrain = ComponentsInScene<Tilemap>(scene).Single(map => map.name == "Terrain");
            Assert.That(terrain.GetComponent<CompositeCollider2D>().pathCount, Is.GreaterThan(0));
            Assert.That(ComponentsInScene<VerticalWallPatrolEnemy2D>(scene), Has.Length.EqualTo(1));

            GroundFireThrowerEnemy2D enemy = ComponentsInScene<GroundFireThrowerEnemy2D>(scene).Single();
            Assert.That(enemy.name, Is.EqualTo("Enemy-B-FireThrower"));
            Assert.That(enemy.transform.position, Is.EqualTo(new Vector3(0f, -2.5f, 0f)));
            Assert.That(PrefabUtility.GetPrefabInstanceStatus(enemy.gameObject),
                Is.EqualTo(PrefabInstanceStatus.Connected));
            Assert.That(ComponentsInScene<PlayerController2D>(scene), Is.Empty);
            Assert.That(ComponentsInScene<RoomPlayerSpawner2D>(scene), Has.Length.EqualTo(1));
            Assert.That(ComponentsInScene<RoomResetSystem>(scene), Has.Length.EqualTo(1));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        Assert.That(EditorBuildSettings.scenes.Any(entry => entry.enabled && entry.path == ScenePath),
            Is.True);
    }

    private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        => scene.GetRootGameObjects().SelectMany(root =>
            root.GetComponentsInChildren<T>(true)).ToArray();
}
