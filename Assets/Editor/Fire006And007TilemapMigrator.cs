using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Incrementally migrates the approved static geometry in FIRE_006 and FIRE_007
/// to the standard Tilemap hierarchy. Runtime gameplay objects are preserved.
/// </summary>
public static class Fire006And007TilemapMigrator
{
    private const string Fire006Scene = "Assets/Scenes/Levels/Fire/Fire_006.unity";
    private const string Fire007Scene = "Assets/Scenes/Levels/Fire/Fire_007.unity";
    private const string PalettePath = "Assets/TilePalettes/Fire.prefab";
    private const string TerrainTilePath = "Assets/Tiles/Fire/FireTerrainBasaltCenter.asset";
    private const string HazardTilePath = "Assets/Tiles/Graybox/Fire008Hazard.asset";
    private const string HintTilePath = "Assets/Tiles/Graybox/Fire003MirrorHint.asset";

    [MenuItem("Tools/W1/Migrate FIRE-006 and FIRE-007 to Tilemap")]
    public static void MigrateBoth()
    {
        Tile terrain = RequireAsset<Tile>(TerrainTilePath);
        Tile hazard = RequireAsset<Tile>(HazardTilePath);
        Tile hint = RequireAsset<Tile>(HintTilePath);
        SyncPalette(terrain, hazard, hint);

        MigrateFire006(terrain, hazard);
        MigrateFire007(terrain, hint);
        AssetDatabase.SaveAssets();
        Debug.Log("FIRE_006 and FIRE_007 static geometry migrated to Tilemap.");
    }

    [MenuItem("Tools/W1/Tile Palettes/Sync FIRE-006 and FIRE-007 Palette")]
    public static void SyncPaletteFromMenu()
    {
        SyncPalette(RequireAsset<Tile>(TerrainTilePath), RequireAsset<Tile>(HazardTilePath),
            RequireAsset<Tile>(HintTilePath));
        AssetDatabase.SaveAssets();
        Debug.Log("FIRE_006 and FIRE_007 Tile Palette entries synchronized without rebuilding either Scene.");
    }

    private static void SyncPalette(params TileBase[] tiles) => TilePaletteAuthoring.EnsureTiles(PalettePath, tiles);

    private static void MigrateFire006(TileBase terrainTile, TileBase hazardTile)
    {
        Scene scene = EditorSceneManager.OpenScene(Fire006Scene, OpenSceneMode.Additive);
        GameObject room = RequireRoot(scene, "FIRE_006 Greybox");
        RemoveChildren(room.transform,
            "Safe Platform - Left", "Safe Platform - Right",
            "Upper Structure - Left", "Upper Structure - Right",
            "U Obstacle - Left Leg", "U Obstacle - Right Leg", "U Obstacle - Base",
            "Boundary Wall - Left", "Boundary Wall - Right",
            "Lava - Central Long Trench", "Grid");

        Transform grid = CreateGrid(room.transform);
        CreateLayer(grid, "Background");
        Tilemap terrain = CreateLayer(grid, "Terrain");
        ConfigureTerrain(terrain);
        CreateLayer(grid, "OneWayPlatform");
        CreateLayer(grid, "SpecialMirrorWall");
        Tilemap hazard = CreateLayer(grid, "Hazard");
        ConfigureHazard(hazard);
        CreateLayer(grid, "Decoration");
        CreateLayer(grid, "Foreground");

        // Approved layout: 8-unit side platforms, 8-unit central trench,
        // two upper beams and a 4x4 downward U-shaped obstruction.
        Fill(terrain, terrainTile, -12, -5, -5, -5);
        Fill(terrain, terrainTile, 4, 11, -5, -5);
        Fill(terrain, terrainTile, -12, -3, 5, 5);
        Fill(terrain, terrainTile, 3, 11, 5, 5);
        Fill(terrain, terrainTile, -3, -3, 2, 5);
        Fill(terrain, terrainTile, 2, 2, 2, 5);
        Fill(terrain, terrainTile, -3, 2, 2, 2);
        Fill(terrain, terrainTile, -13, -13, -5, 6);
        Fill(terrain, terrainTile, 12, 12, -5, 6);
        Fill(hazard, hazardTile, -4, 3, -5, -5);

        Bake(terrain, hazard);
        ValidateFire006(terrain, hazard);
        SaveAndClose(scene, Fire006Scene);
    }

    private static void MigrateFire007(TileBase terrainTile, TileBase hintTile)
    {
        Scene scene = EditorSceneManager.OpenScene(Fire007Scene, OpenSceneMode.Additive);
        GameObject room = RequireRoot(scene, "FIRE_007 Double Latch");
        RemoveChildren(room.transform,
            "Continuous Safe Ground", "Boundary Wall - Left", "Boundary Wall - Right",
            "Upper Boundary", "Grid");

        Transform grid = CreateGrid(room.transform);
        CreateLayer(grid, "Background");
        Tilemap terrain = CreateLayer(grid, "Terrain");
        ConfigureTerrain(terrain);
        CreateLayer(grid, "OneWayPlatform");
        CreateLayer(grid, "SpecialMirrorWall");
        CreateLayer(grid, "Hazard");
        Tilemap decoration = CreateLayer(grid, "Decoration");
        CreateLayer(grid, "Foreground");

        Fill(terrain, terrainTile, -13, 12, -4, -4);
        Fill(terrain, terrainTile, -14, -14, -4, 7);
        Fill(terrain, terrainTile, 13, 13, -4, 7);
        Fill(terrain, terrainTile, -14, 13, 7, 7);
        decoration.SetTile(new Vector3Int(0, -3, 0), hintTile);

        Bake(terrain);
        ValidateFire007(terrain, decoration);
        SaveAndClose(scene, Fire007Scene);
    }

    private static Transform CreateGrid(Transform parent)
    {
        GameObject go = new("Grid");
        go.transform.SetParent(parent, false);
        Grid grid = go.AddComponent<Grid>();
        grid.cellSize = Vector3.one;
        return go.transform;
    }

    private static Tilemap CreateLayer(Transform parent, string name)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        Tilemap map = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();
        return map;
    }

    private static void ConfigureTerrain(Tilemap map)
    {
        Rigidbody2D body = map.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        map.gameObject.AddComponent<CompositeCollider2D>();
        TilemapCollider2D collider = map.gameObject.AddComponent<TilemapCollider2D>();
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        SurfaceSemantic2D semantic = map.gameObject.AddComponent<SurfaceSemantic2D>();
        semantic.Configure(SurfaceSemantic2D.SurfaceType.StaticSolid, true, true);
        MirrorSurface2D mirror = map.gameObject.AddComponent<MirrorSurface2D>();
        mirror.kind = MirrorSurface2D.SurfaceKind.Ground;
    }

    private static void ConfigureHazard(Tilemap map)
    {
        TilemapCollider2D collider = map.gameObject.AddComponent<TilemapCollider2D>();
        collider.isTrigger = true;
        SurfaceSemantic2D semantic = map.gameObject.AddComponent<SurfaceSemantic2D>();
        semantic.Configure(SurfaceSemantic2D.SurfaceType.Hazard, true, false);
        map.gameObject.AddComponent<Hazard2D>();
    }

    private static void Bake(params Tilemap[] maps)
    {
        foreach (Tilemap map in maps)
        {
            map.CompressBounds();
            map.RefreshAllTiles();
            TilemapCollider2D tilemapCollider = map.GetComponent<TilemapCollider2D>();
            if (tilemapCollider == null)
                continue;
            tilemapCollider.ProcessTilemapChanges();
            CompositeCollider2D composite = map.GetComponent<CompositeCollider2D>();
            if (composite != null)
                composite.GenerateGeometry();
        }
        Physics2D.SyncTransforms();
    }

    private static void ValidateFire006(Tilemap terrain, Tilemap hazard)
    {
        Require(terrain.GetComponent<CompositeCollider2D>().pathCount > 0, "FIRE_006 terrain collider is empty.");
        Require(hazard.GetComponent<TilemapCollider2D>().bounds.size.x == 8f, "FIRE_006 lava width changed.");
        Require(hazard.GetComponent<SurfaceSemantic2D>().Type == SurfaceSemantic2D.SurfaceType.Hazard,
            "FIRE_006 lava lacks explicit Hazard semantics.");
        Require(GameObject.Find("Exit-A") == null, "FIRE_006 must not gain an exit during geometry migration.");
    }

    private static void ValidateFire007(Tilemap terrain, Tilemap decoration)
    {
        Require(terrain.GetComponent<CompositeCollider2D>().pathCount > 0, "FIRE_007 terrain collider is empty.");
        Require(decoration.HasTile(new Vector3Int(0, -3, 0)), "FIRE_007 mirror hint is missing.");
        Require(GameObject.Find("Plate-A") != null && GameObject.Find("Plate-B") != null &&
                GameObject.Find("Door-A") != null && GameObject.Find("FIRE_007:DOOR_GROUP:01") != null,
            "FIRE_007 runtime puzzle objects were not preserved.");
    }

    private static void RemoveChildren(Transform root, params string[] names)
    {
        foreach (string name in names)
        {
            Transform child = root.Find(name);
            if (child != null)
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void Fill(Tilemap map, TileBase tile, int minX, int maxX, int minY, int maxY)
    {
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                map.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private static GameObject RequireRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name)
                return root;
        throw new InvalidOperationException($"Missing room root '{name}' in {scene.path}.");
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Require(asset != null, $"Missing required asset: {path}");
        return asset;
    }

    private static void SaveAndClose(Scene scene, string path)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, path), $"Failed to save {path}.");
        Require(EditorSceneManager.CloseScene(scene, true), $"Failed to close {path} after saving.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
