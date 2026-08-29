using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Fire006LavaTileApplicator
{
    private const string TexturePath = "Assets/Art/Generated/Fire/Fire006LavaTile.png";
    private const string TilePath = "Assets/Tiles/Fire/Fire006Lava.asset";
    private const string ScenePath = "Assets/Scenes/Levels/Fire/Fire_006.unity";

    [MenuItem("Tools/W1/FIRE-006/Apply Lava Tile Art")]
    public static void Apply()
    {
        ConfigureTexture();
        Tile tile = CreateOrUpdateTile();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            Tilemap hazard = FindHazardTilemap(scene);
            int replaced = 0;
            foreach (Vector3Int position in hazard.cellBounds.allPositionsWithin)
            {
                if (!hazard.HasTile(position))
                    continue;

                hazard.SetTile(position, tile);
                hazard.SetColor(position, Color.white);
                replaced++;
            }

            if (replaced != 8)
                throw new InvalidOperationException($"Expected 8 FIRE_006 lava cells, found {replaced}.");

            hazard.RefreshAllTiles();
            TilemapCollider2D collider = hazard.GetComponent<TilemapCollider2D>();
            if (collider == null || !collider.isTrigger)
                throw new InvalidOperationException("FIRE_006 Hazard must retain its trigger TilemapCollider2D.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Failed to save FIRE_006 after applying lava art.");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Applied Fire006LavaTile to all 8 FIRE_006 Hazard cells without changing gameplay geometry.");
    }

    private static void ConfigureTexture()
    {
        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException($"Missing texture importer for {TexturePath}.");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 1254f;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.SaveAndReimport();
    }

    private static Tile CreateOrUpdateTile()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);
        if (sprite == null)
            throw new InvalidOperationException($"Texture did not import as a Sprite: {TexturePath}.");

        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, TilePath);
        }

        tile.sprite = sprite;
        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static Tilemap FindHazardTilemap(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(true))
                if (tilemap.name == "Hazard")
                    return tilemap;
        }

        throw new InvalidOperationException("FIRE_006 Hazard Tilemap was not found.");
    }
}
