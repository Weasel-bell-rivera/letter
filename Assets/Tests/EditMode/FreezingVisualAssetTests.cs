using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class FreezingVisualAssetTests
{
    [TestCase("Assets/Prefabs/Gameplay/Characters/Player.prefab")]
    [TestCase("Assets/Prefabs/Gameplay/Enemies/FreezablePatrolEnemy2D.prefab")]
    public void CharacterPrefabHasSharedFreezingVisual(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<FreezingVisual2D>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<PlayerController2D>() != null ||
                    prefab.GetComponent<FreezablePatrolEnemy2D>() != null, Is.True);
    }
}
