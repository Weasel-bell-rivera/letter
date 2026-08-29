using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds the approved FIRE_005 three-corridor fireball-latch greybox.</summary>
public static class Fire005RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Fire/Fire_005.unity";
    private const string TilePalettePath = "Assets/TilePalettes/Fire.prefab";
    private const string TerrainTilePath = "Assets/Tiles/Graybox/Fire005Terrain.asset";
    private const string HintTilePath = "Assets/Tiles/Graybox/Fire005MirrorHint.asset";
    private const string TerrainTexturePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_sand_block_center.png";
    private const string PlatePath = "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab";
    private const string DoorPath = "Assets/Prefabs/Gameplay/Doors/Door2D.prefab";
    private const string EnemyPath = "Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab";
    private const string ExitPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";

    [MenuItem("Tools/W1/Build FIRE-005 Greybox")]
    public static void BuildFromMenu() => Build();

    public static void Build()
    {
        Directory.CreateDirectory("Assets/Scenes/Levels/Fire");
        Directory.CreateDirectory("Assets/Tiles/Graybox");

        GameObject platePrefab = RequireAsset(PlatePath);
        GameObject doorPrefab = RequireAsset(DoorPath);
        GameObject enemyPrefab = RequireAsset(EnemyPath);
        GameObject exitPrefab = RequireAsset(ExitPath);
        Sprite terrainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TerrainTexturePath);
        Require(terrainSprite != null, $"Missing terrain sprite: {TerrainTexturePath}");

        Tile terrainTile = MakeTile(TerrainTilePath, terrainSprite, Color.white, Tile.ColliderType.Grid);
        Tile hintTile = MakeTile(HintTilePath,
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            new Color(.15f, .9f, 1f, .7f), Tile.ColliderType.None);
        TilePaletteAuthoring.EnsureTiles(TilePalettePath, terrainTile, hintTile);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = new("FIRE_005 Three Corridor Fire Route");
        GameObject gridObject = Child(room.transform, "Grid");
        gridObject.AddComponent<Grid>();
        CreateLayer(gridObject.transform, "Background");
        Tilemap terrain = CreateLayer(gridObject.transform, "Terrain");
        ConfigureTerrain(terrain);
        CreateLayer(gridObject.transform, "OneWayPlatform");
        CreateLayer(gridObject.transform, "SpecialMirrorWall");
        CreateLayer(gridObject.transform, "Hazard");
        Tilemap decoration = CreateLayer(gridObject.transform, "Decoration");
        CreateLayer(gridObject.transform, "Foreground");

        // Outer shell. The upper-left return exit occupies the left-wall cells y=3..5.
        Fill(terrain, terrainTile, -9, 8, 6, 6);
        Fill(terrain, terrainTile, -9, -9, -7, 2);
        Fill(terrain, terrainTile, 8, 8, -7, -6);
        Fill(terrain, terrainTile, 8, 8, -2, 5);

        // Three floors and their alternating two-cell drop shafts.
        Fill(terrain, terrainTile, -8, 2, 2, 2);
        Fill(terrain, terrainTile, -8, -6, -2, -2);
        Fill(terrain, terrainTile, -3, 7, -2, -2);
        // Lower the complete third corridor by one cell so both halves share one
        // floor height and the solid latch backstop has enough jump clearance.
        Fill(terrain, terrainTile, -8, 7, -7, -7);
        Fill(terrain, terrainTile, 2, 2, -6, -5);
        // Keep the first lower-corridor fire line at two units of standing clearance.
        // The drop shaft stays open, but once the Player commits to the rightward route
        // there is no useful jump arc over the first shot; the intended answer is to
        // place the mirror during windup and let MirrorClone intercept the projectile.
        Fill(terrain, terrainTile, -3, 0, -4, -4);
        decoration.SetTile(new Vector3Int(-3, -6, 0), hintTile);
        BakeTerrain(terrain);

        GameObject gameplay = Child(room.transform, "Gameplay");
        Transform dynamicRoot = Child(gameplay.transform, "DynamicObjects").transform;
        Transform entrances = Child(gameplay.transform, "Entrances").transform;
        Transform exits = Child(gameplay.transform, "Exits").transform;

        Transform entrance = Marker("Entrance-DEFAULT", new Vector3(-7f, 3.92f), entrances);
        PlayerRoomAuthoring.ConfigureEntrance(entrance, "DEFAULT", true, true);
        Transform returnEntrance = Marker("EntranceFromFIRE006", new Vector3(3.5f, -5.08f), entrances);
        PlayerRoomAuthoring.ConfigureEntrance(returnEntrance, "FROM_FIRE_006", false, false);

        HorizontalFireballEnemy2D middleEnemy = CreateEnemy(enemyPrefab, dynamicRoot,
            "Enemy-Mid", new Vector2(6.5f, -.5f), false);
        HorizontalFireballEnemy2D lowerEnemy = CreateEnemy(enemyPrefab, dynamicRoot,
            "Enemy-Low", new Vector2(-7.5f, -5.5f), true);

        GameObject latchObject = Instance(platePrefab, dynamicRoot, "Latch-A", new Vector2(1.85f, -5.375f));
        latchObject.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        PressurePlate2D latch = latchObject.GetComponent<PressurePlate2D>();
        Require(latch != null, "Pressure plate Prefab is missing PressurePlate2D.");
        latch.ConfigureActivationMode(PressurePlate2D.ActivationMode.FireballLatch);
        Record(latchObject.transform);
        Record(latch);

        GameObject doorObject = Instance(doorPrefab, dynamicRoot, "Door-A", new Vector2(7.5f, -5f));
        Door2D door = doorObject.GetComponent<Door2D>();
        Require(door != null, "Door Prefab is missing Door2D.");
        door.ConfigureControlSource(latch);
        Record(doorObject.transform);
        Record(door);

        RoomExit2D backExit = CreateExit(exitPrefab, exits, "Exit-Back-to-FIRE004",
            new Vector2(-8.5f, 4.2f), "Fire_004", "FROM_FIRE_005");
        RoomExit2D forwardExit = CreateExit(exitPrefab, exits, "Exit-To-FIRE006",
            new Vector2(8.5f, -4.8f), "Fire_006", "DEFAULT");

        Camera camera = CreateCamera();
        GameObject lightObject = new("Main Light");
        lightObject.transform.SetParent(room.transform);
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = .85f;

        GameObject systems = Child(room.transform, "RoomSystems");
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, null, true);

        Validate(scene, terrain, middleEnemy, lowerEnemy, latch, door, backExit, forwardExit, camera);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save FIRE_005.");
        AddBuildScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("FIRE_005 three-corridor greybox built successfully.");
    }

    private static HorizontalFireballEnemy2D CreateEnemy(GameObject prefab, Transform parent,
        string name, Vector2 position, bool facingRight)
    {
        GameObject go = Instance(prefab, parent, name, position);
        HorizontalFireballEnemy2D enemy = go.GetComponent<HorizontalFireballEnemy2D>();
        Require(enemy != null, "Horizontal fireball enemy Prefab is missing its runtime component.");
        enemy.SetInitiallyFacingRight(facingRight);
        Record(enemy);
        return enemy;
    }

    private static RoomExit2D CreateExit(GameObject prefab, Transform parent, string name,
        Vector2 position, string targetScene, string targetEntrance)
    {
        GameObject go = Instance(prefab, parent, name, position);
        RoomExit2D exit = go.GetComponent<RoomExit2D>();
        Require(exit != null, "Exit Prefab is missing RoomExit2D.");
        exit.Configure(targetScene, targetEntrance);
        Record(exit);
        return exit;
    }

    private static Camera CreateCamera()
    {
        GameObject go = new("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, -.25f, -10f);
        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.backgroundColor = new Color(.055f, .025f, .02f);
        go.AddComponent<AudioListener>();
        return camera;
    }

    private static Tilemap CreateLayer(Transform parent, string name)
    {
        GameObject go = Child(parent, name);
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
        MirrorSurface2D mirrorSurface = map.gameObject.AddComponent<MirrorSurface2D>();
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
        Require(terrain.GetComponent<CompositeCollider2D>().pathCount > 0,
            "FIRE_005 Terrain collider geometry is empty.");
    }

    private static Tile MakeTile(string path, Sprite sprite, Color color, Tile.ColliderType colliderType)
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

    private static void Validate(Scene scene, Tilemap terrain, HorizontalFireballEnemy2D middleEnemy,
        HorizontalFireballEnemy2D lowerEnemy, PressurePlate2D latch, Door2D door,
        RoomExit2D backExit, RoomExit2D forwardExit, Camera camera)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(terrain.GetComponent<CompositeCollider2D>().pathCount > 0, "Terrain collider is empty.");
        Require(roots.SelectMany(x => x.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "FIRE_005 must not serialize Player.");
        Require(roots.SelectMany(x => x.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "FIRE_005 needs one player spawner.");
        Require(roots.SelectMany(x => x.GetComponentsInChildren<HorizontalFireballEnemy2D>(true)).Count() == 2,
            "FIRE_005 needs two horizontal fireball enemies.");
        Require(roots.SelectMany(x => x.GetComponentsInChildren<PressurePlate2D>(true)).Count() == 1 &&
                latch.Mode == PressurePlate2D.ActivationMode.FireballLatch,
            "FIRE_005 needs one FireballLatch.");
        Require(roots.SelectMany(x => x.GetComponentsInChildren<Door2D>(true)).Count() == 1,
            "FIRE_005 needs one door.");
        Require(door.ControlSource == latch, "Door-A must be controlled by Latch-A.");
        Require(Mathf.Abs(Mathf.DeltaAngle(door.transform.eulerAngles.z, 0f)) < .01f,
            "Door-A must be installed vertically in front of the right-side exit.");
        Require(middleEnemy != null && lowerEnemy != null, "Enemy reference is missing.");
        Require(terrain.HasTile(new Vector3Int(-3, -4, 0)) &&
                terrain.HasTile(new Vector3Int(0, -4, 0)),
            "FIRE_005 lower encounter needs the approved low-clearance ceiling.");
        Require(terrain.HasTile(new Vector3Int(-9, -7, 0)) &&
                terrain.HasTile(new Vector3Int(8, -7, 0)),
            "FIRE_005 side walls must reach the lowered third-corridor floor.");
        Require(terrain.HasTile(new Vector3Int(2, -6, 0)) &&
                terrain.HasTile(new Vector3Int(2, -5, 0)),
            "Latch-A needs a solid two-cell Terrain backstop.");
        Require(backExit.TargetScene == "Fire_004" && backExit.TargetEntranceId == "FROM_FIRE_005",
            "Return exit target mismatch.");
        Require(forwardExit.TargetScene == "Fire_006" && forwardExit.TargetEntranceId == "DEFAULT",
            "Forward exit target mismatch.");
        Require(roots.SelectMany(x => x.GetComponentsInChildren<RoomExit2D>(true)).Count() == 2,
            "FIRE_005 needs two exits.");
        Require(camera.orthographic && Mathf.Approximately(camera.orthographicSize, 7f) &&
                camera.GetComponent<CameraFollow2D>() == null,
            "FIRE_005 must use the approved fixed camera.");
    }

    private static GameObject Child(Transform parent, string name)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Transform Marker(string name, Vector3 position, Transform parent)
    {
        GameObject go = Child(parent, name);
        go.transform.position = position;
        return go.transform;
    }

    private static GameObject Instance(GameObject prefab, Transform parent, string name, Vector2 position)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        return go;
    }

    private static void Record(Component component)
    {
        EditorUtility.SetDirty(component);
        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }

    private static GameObject RequireAsset(string path)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Require(asset != null, $"Missing prefab: {path}");
        return asset;
    }

    private static void Fill(Tilemap map, TileBase tile, int minX, int maxX, int minY, int maxY)
    {
        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
            map.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AddBuildScene()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Any(x => x.path == ScenePath)) return;
        EditorBuildSettings.scenes = scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
    }
}
