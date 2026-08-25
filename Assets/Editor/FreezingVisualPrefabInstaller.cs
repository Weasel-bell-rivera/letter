using System;
using UnityEditor;
using UnityEngine;

public static class FreezingVisualPrefabInstaller
{
    private const string PlayerPath = "Assets/Prefabs/Gameplay/Characters/Player.prefab";
    private const string EnemyPath = "Assets/Prefabs/Gameplay/Enemies/FreezablePatrolEnemy2D.prefab";

    [MenuItem("Tools/W1/Install Freezing Visuals on Character Prefabs")]
    public static void Install()
    {
        InstallOn(PlayerPath);
        InstallOn(EnemyPath);
        AssetDatabase.SaveAssets();
        Debug.Log("FreezingVisual2D installed on Player and freezable enemy Prefabs.");
    }

    private static void InstallOn(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null) throw new InvalidOperationException($"Missing Prefab: {path}");
        try
        {
            if (root.GetComponent<FreezingVisual2D>() == null) root.AddComponent<FreezingVisual2D>();
            if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                throw new InvalidOperationException($"Failed to save Prefab: {path}");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
