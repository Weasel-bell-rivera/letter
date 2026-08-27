using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds the approved FIRE_002 narrow-corridor decoy teaching greybox.</summary>
public static class Fire002RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Fire/Fire_002.unity";
    public const string EnemyPrefabPath = "Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab";
    public const string ExitPrefabPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";
    public const string TerrainTexturePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_sand_block_center.png";
    public const string TerrainTilePath = "Assets/Tiles/Graybox/Fire002Terrain.asset";
    public const string HintTilePath = "Assets/Tiles/Graybox/Fire002MirrorHint.asset";

    [MenuItem("Tools/W1/Build FIRE-002 Greybox")]
    public static void BuildFromMenu() => Build();

    public static void Build()
    {
        Directory.CreateDirectory("Assets/Scenes/Levels/Fire");
        Directory.CreateDirectory("Assets/Tiles/Graybox");

        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        GameObject exitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExitPrefabPath);
        Sprite terrainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TerrainTexturePath);
        Require(enemyPrefab != null && exitPrefab != null && terrainSprite != null,
            "FIRE_002 shared dependencies are missing.");

        Tile terrainTile = CreateOrUpdateTile(TerrainTilePath, terrainSprite, Color.white, Tile.ColliderType.Grid);
        Tile hintTile = CreateOrUpdateTile(HintTilePath, terrainSprite,
            new Color(.2f, .9f, 1f, .75f), Tile.ColliderType.None);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = new("FIRE_002 Narrow Corridor Decoy");
        GameObject gridRoot = Child(room.transform, "Grid");
        gridRoot.AddComponent<Grid>();

        CreateTilemap(gridRoot.transform, "Background");
        Tilemap terrain = CreateTilemap(gridRoot.transform, "Terrain");
        ConfigureTerrain(terrain);
        CreateTilemap(gridRoot.transform, "OneWayPlatform");
        CreateTilemap(gridRoot.transform, "SpecialMirrorWall");
        CreateTilemap(gridRoot.transform, "Hazard");
        Tilemap decoration = CreateTilemap(gridRoot.transform, "Decoration");
        CreateTilemap(gridRoot.transform, "Foreground");

        // Outer shell and upper route from the FIRE_001 entrance to the drop.
        Fill(terrain, terrainTile, -11, 10, -6, -6);
        Fill(terrain, terrainTile, -11, 10, 7, 7);
        Fill(terrain, terrainTile, -11, -11, -5, 6);
        Fill(terrain, terrainTile, 10, 10, -5, 6);
        Fill(terrain, terrainTile, -10, 0, 3, 3);

        // The lower route has exactly 2 units of standing clearance: enough for
        // the 1.8-unit actor, but too little to perform a useful jump dodge.
        Fill(terrain, terrainTile, -10, 9, -6, -6);
        Fill(terrain, terrainTile, -10, -1, -3, -3);
        Fill(terrain, terrainTile, 3, 6, -3, -3);
        Fill(terrain, terrainTile, 3, 3, -2, 2); // right wall of the drop shaft
        decoration.SetTile(new Vector3Int(1, -5, 0), hintTile);
        BakeTerrain(terrain);

        GameObject gameplay = Child(room.transform, "Gameplay");
        GameObject dynamicObjects = Child(gameplay.transform, "DynamicObjects");
        GameObject entrances = Child(gameplay.transform, "Entrances");
        GameObject exits = Child(gameplay.transform, "Exits");
        // Keep explicit clearance above the upper Terrain composite. The Player
        // settles onto the floor after spawning instead of being rejected as embedded.
        Transform entrance = Marker("Entrance-DEFAULT", new Vector3(-9f, 4.92f, 0f), entrances.transform);
        Transform fromFire001 = Marker("EntranceFromFIRE001", new Vector3(-9f, 4.92f, 0f), entrances.transform);
        PlayerRoomAuthoring.ConfigureEntrance(fromFire001, "FROM_FIRE_001", false, true);
        CreateEnemy(enemyPrefab, dynamicObjects.transform);
        CreateExit(exitPrefab, exits.transform);
        CreateCamera();

        GameObject systems = Child(room.transform, "RoomSystems");
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, null, true);

        Validate(scene, terrain);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save FIRE_002 scene.");
        AddBuildScene(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("FIRE_002 Tilemap greybox built successfully.");
    }

    private static void CreateEnemy(GameObject prefab, Transform parent)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Enemy-H1 Horizontal Fireball";
        instance.transform.SetParent(parent, false);
        instance.transform.position = new Vector3(8.5f, -4.5f, 0f);
        HorizontalFireballEnemy2D enemy = instance.GetComponent<HorizontalFireballEnemy2D>();
        Require(enemy != null, "HorizontalFireballEnemy2D Prefab is missing its runtime component.");
        enemy.SetInitiallyFacingRight(false);
        EditorUtility.SetDirty(enemy);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
    }

    private static void CreateExit(GameObject prefab, Transform parent)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Exit-A to FIRE_003";
        instance.transform.SetParent(parent, false);
        instance.transform.position = new Vector3(-9.25f, -4.5f, 0f);
        RoomExit2D exit = instance.GetComponent<RoomExit2D>();
        Require(exit != null, "RoomExit2D Prefab is missing its runtime component.");
        exit.Configure("Fire_003", "DEFAULT");
        EditorUtility.SetDirty(exit);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(exit);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, .5f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.backgroundColor = new Color(.055f, .025f, .02f);
        cameraObject.AddComponent<AudioListener>();
    }

    private static Tilemap CreateTilemap(Transform parent, string name)
    {
        GameObject layer = Child(parent, name);
        Tilemap map = layer.AddComponent<Tilemap>();
        layer.AddComponent<TilemapRenderer>();
        return map;
    }

    private static void ConfigureTerrain(Tilemap terrain)
    {
        Rigidbody2D body = terrain.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        terrain.gameObject.AddComponent<CompositeCollider2D>();
        TilemapCollider2D collider = terrain.gameObject.AddComponent<TilemapCollider2D>();
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        terrain.gameObject.AddComponent<SurfaceSemantic2D>()
            .Configure(SurfaceSemantic2D.SurfaceType.StaticSolid, true, true);
        MirrorSurface2D mirrorSurface = terrain.gameObject.AddComponent<MirrorSurface2D>();
        mirrorSurface.kind = MirrorSurface2D.SurfaceKind.Ground;
        mirrorSurface.safe = true;
    }

    private static void BakeTerrain(Tilemap terrain)
    {
        terrain.CompressBounds();
        terrain.RefreshAllTiles();
        terrain.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        terrain.GetComponent<CompositeCollider2D>().GenerateGeometry();
        Physics2D.SyncTransforms();
    }

    private static Tile CreateOrUpdateTile(string path, Sprite sprite, Color color, Tile.ColliderType colliderType)
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.name = Path.GetFileNameWithoutExtension(path);
        tile.sprite = sprite;
        tile.color = color;
        tile.colliderType = colliderType;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static GameObject Child(Transform parent, string name)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static Transform Marker(string name, Vector3 position, Transform parent)
    {
        GameObject marker = Child(parent, name);
        marker.transform.position = position;
        return marker.transform;
    }

    private static void Fill(Tilemap map, TileBase tile, int minX, int maxX, int minY, int maxY)
    {
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                map.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private static void Validate(Scene scene, Tilemap terrain)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "FIRE_002 must not serialize a room-local Player.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "FIRE_002 must contain exactly one RoomPlayerSpawner2D.");
        RoomEntrance2D[] entrances = roots.SelectMany(root => root.GetComponentsInChildren<RoomEntrance2D>(true)).ToArray();
        Require(entrances.Length == 2 && entrances.Count(item => item.IsDefault) == 1 &&
                entrances.Any(item => item.EntranceId == "FROM_FIRE_001"),
            "FIRE_002 must contain DEFAULT and FROM_FIRE_001 entrances.");
        RoomExit2D exit = roots.SelectMany(root => root.GetComponentsInChildren<RoomExit2D>(true)).Single();
        Require(PrefabUtility.GetPrefabInstanceStatus(exit.gameObject) == PrefabInstanceStatus.Connected,
            "FIRE_002 exit must remain connected to RoomExit2D.prefab.");
        Require(terrain.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.StaticSolid,
            "FIRE_002 Terrain must expose StaticSolid semantics.");
        Require(terrain.GetComponent<CompositeCollider2D>().pathCount > 0,
            "FIRE_002 collider geometry was not generated.");
        HorizontalFireballEnemy2D enemy = roots.SelectMany(root =>
            root.GetComponentsInChildren<HorizontalFireballEnemy2D>(true)).Single();
        Require(PrefabUtility.GetPrefabInstanceStatus(enemy.gameObject) == PrefabInstanceStatus.Connected &&
                Vector2.Distance(enemy.transform.position, new Vector2(8.5f, -4.5f)) < .001f,
            "FIRE_002 fixed thrower must remain connected at the approved position.");
        Camera camera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Single();
        Require(camera.orthographic && Mathf.Approximately(camera.orthographicSize, 7f),
            "FIRE_002 must use the documented fixed single-screen camera.");
    }

    private static void AddBuildScene(string path)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(scene => scene.path == path))
            scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
