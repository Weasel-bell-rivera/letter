using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Earth006TerrainArtApplier
{
    private const string ScenePath = "Assets/Scenes/Levels/Earth/Earth_006.unity";
    private const string TexturePath = "Assets/Art/Earth/Terrain/Earth006TerrainSolid.png";
    private const string TilePath = "Assets/Tiles/Earth/Earth006/Earth006Terrain.asset";

    [MenuItem("Tools/W1/Earth/Apply EARTH_006 Terrain Art")]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new System.InvalidOperationException($"Open {ScenePath} before applying its terrain art.");

        ConfigureTexture();
        Tile tile = EnsureTile();
        Tilemap terrain = GameObject.Find("EARTH_006 Rail Height Greybox/Grid/Terrain")
            ?.GetComponent<Tilemap>();
        if (terrain == null)
            throw new System.InvalidOperationException("EARTH_006 Terrain Tilemap was not found.");

        Undo.RecordObject(terrain, "Apply EARTH_006 terrain art");
        foreach (Vector3Int position in terrain.cellBounds.allPositionsWithin)
        {
            if (terrain.HasTile(position)) terrain.SetTile(position, tile);
        }

        terrain.RefreshAllTiles();
        terrain.GetComponent<TilemapCollider2D>()?.ProcessTilemapChanges();
        EditorUtility.SetDirty(terrain);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Applied the solid EARTH_006 terrain silhouette without changing its layout or semantics.");
    }

    private static void ConfigureTexture()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
            throw new System.InvalidOperationException($"Missing terrain texture: {TexturePath}");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }

    private static Tile EnsureTile()
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, TilePath);
        }

        tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);
        if (tile.sprite == null)
            throw new System.InvalidOperationException($"Texture did not import as a Sprite: {TexturePath}");

        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        return tile;
    }
}
