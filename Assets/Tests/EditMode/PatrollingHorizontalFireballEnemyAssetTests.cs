using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class PatrollingHorizontalFireballEnemyAssetTests
{
    private const string SettingsPath =
        "Assets/Settings/Enemies/DefaultPatrollingHorizontalFireballEnemy.asset";
    private const string PrefabPath =
        "Assets/Prefabs/Gameplay/Enemies/PatrollingHorizontalFireballEnemy2D.prefab";
    private const string ScenePath =
        "Assets/Scenes/Tests/PatrollingHorizontalFireballEnemyTest.unity";

    [Test]
    public void ApprovedSharedSettingsMatchDesignValues()
    {
        PatrollingHorizontalFireballEnemySettings settings =
            AssetDatabase.LoadAssetAtPath<PatrollingHorizontalFireballEnemySettings>(SettingsPath);
        Assert.That(settings, Is.Not.Null);
        Assert.That(settings.IsValid, Is.True);
        Assert.That(settings.PatrolSpeed, Is.EqualTo(1.5f).Within(.0001f));
        Assert.That(settings.TurnPauseDuration, Is.EqualTo(.2f).Within(.0001f));
    }

    [Test]
    public void PrefabIsIndependentAndComposesApprovedAttackController()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<PatrollingHorizontalFireballEnemy2D>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<HorizontalFireballEnemy2D>(), Is.Not.Null);
        Assert.That(PrefabUtility.GetPrefabAssetType(prefab), Is.EqualTo(PrefabAssetType.Regular));
    }

    [Test]
    public void TestSceneContainsConnectedEnemyAndStaticSolidTilemap()
    {
        SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Assert.That(scene, Is.Not.Null);
    }
}
