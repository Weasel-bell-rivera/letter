using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Earth008TerrainArtApplier
{
    private const string ScenePath = "Assets/Scenes/Levels/Earth/Earth_008.unity";
    private const string TexturePath = "Assets/Art/Earth/Terrain/earth008_rock_block_v1.png";
    private const string TilePath = "Assets/Tiles/Earth/Earth008/Earth008Terrain.asset";

    [MenuItem("Tools/W1/Earth/Apply EARTH_008 Terrain Art")]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new System.InvalidOperationException($"Open {ScenePath} before applying its terrain art.");

        ConfigureTexture();
        Tile tile = EnsureTile();
        Tilemap terrain = GameObject.Find("EARTH_008 Twin Watch Greybox/Grid/Terrain")
            ?.GetComponent<Tilemap>();
        if (terrain == null)
            throw new System.InvalidOperationException("EARTH_008 Terrain Tilemap was not found.");

        Undo.RecordObject(terrain, "Apply EARTH_008 terrain art");
        foreach (Vector3Int position in terrain.cellBounds.allPositionsWithin)
        {
            if (terrain.HasTile(position))
                terrain.SetTile(position, tile);
        }

        terrain.RefreshAllTiles();
        terrain.GetComponent<TilemapCollider2D>()?.ProcessTilemapChanges();
        EditorUtility.SetDirty(terrain);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Applied the EARTH_008 rock-block art without changing terrain layout, collision, or surface semantics.");
    }

    private static void ConfigureTexture()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
            throw new System.InvalidOperationException($"Missing terrain texture: {TexturePath}");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        // Slight visual overscan hides the transparent rounded-corner seams between 1-unit cells.
        // Tile collision remains grid-based and unchanged.
        importer.spritePixelsPerUnit = 1180f;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
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
