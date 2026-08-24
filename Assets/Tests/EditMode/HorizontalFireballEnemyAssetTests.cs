using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class HorizontalFireballEnemyAssetTests
{
    private const string SettingsPath =
        "Assets/Settings/Enemies/DefaultHorizontalFireballEnemy.asset";
    private const string EnemyPrefabPath =
        "Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab";
    private const string ProjectilePrefabPath =
        "Assets/Prefabs/Gameplay/Enemies/Projectiles/HorizontalFireballProjectile2D.prefab";

    [Test]
    public void SharedSettingsMatchApprovedValues()
    {
        HorizontalFireballEnemySettings settings =
            AssetDatabase.LoadAssetAtPath<HorizontalFireballEnemySettings>(SettingsPath);
        Assert.That(settings, Is.Not.Null);
        Assert.That(settings.IsValid, Is.True);
        Assert.That(settings.DetectionHalfWidth, Is.EqualTo(6f));
        Assert.That(settings.DetectionHalfHeight, Is.EqualTo(.75f));
        Assert.That(settings.WindupDuration, Is.EqualTo(.6f));
        Assert.That(settings.CooldownDuration, Is.EqualTo(1.4f));
        Assert.That(settings.ProjectileSpeed, Is.EqualTo(8f));
        Assert.That(settings.ProjectileLifetime, Is.EqualTo(2f));
        Assert.That(settings.CameraExitMargin, Is.EqualTo(1f));
    }

    [Test]
    public void EnemyPrefabContainsRequiredReferencesAndNoTargetMarker()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.transform.position, Is.EqualTo(Vector3.zero));
        Assert.That(prefab.GetComponent<HorizontalFireballEnemy2D>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
        Assert.That(prefab.transform.Find("BodyCollider"), Is.Not.Null);
        Assert.That(prefab.transform.Find("DamageTrigger"), Is.Not.Null);
        Assert.That(prefab.transform.Find("FireOrigin"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Visual/BodyVisual"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Visual/MuzzleVisual"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Visual/TargetMarker"), Is.Null);
    }

    [Test]
    public void ProjectilePrefabIsKinematicTriggerWithHorizontalTrail()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<HorizontalFireballProjectile2D>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
        Assert.That(prefab.GetComponent<Collider2D>().isTrigger, Is.True);
        Assert.That(prefab.transform.Find("Visual"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Trail").GetComponent<LineRenderer>(), Is.Not.Null);
    }
}
