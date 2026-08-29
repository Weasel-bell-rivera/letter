using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds FIRE_003 from reusable gameplay prefabs; no room-specific runtime behaviour.</summary>
public static class Fire003RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Fire/Fire_003.unity";
    public const string TilePalettePath = "Assets/TilePalettes/Fire.prefab";
    private const string TerrainTilePath = "Assets/Tiles/Graybox/Fire003Terrain.asset";
    private const string HintTilePath = "Assets/Tiles/Graybox/Fire003MirrorHint.asset";
    private const string TerrainTexturePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_sand_block_center.png";
    private const string PlatePath = "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab";
    private const string DoorPath = "Assets/Prefabs/Gameplay/Doors/Door2D.prefab";
    private const string DoorGroupPath = "Assets/Prefabs/Gameplay/Doors/PermanentLatchDoorGroup2D.prefab";
    private const string EnemyPath = "Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab";
    private const string ExitPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";
    // Orthographic size 7 at 16:9 shows about 24.89 units horizontally.
    // A 24-unit room keeps the full puzzle width visible without horizontal tracking.
    private static readonly Rect CameraBounds = new(-12f, -21f, 24f, 36f);

    [MenuItem("Tools/W1/Build FIRE-003 Greybox")]
    public static void BuildFromMenu() => Build();

    [MenuItem("Tools/W1/Tile Palettes/Sync FIRE-003 Palette")]
    public static void SyncTilePaletteFromMenu()
    {
        SyncTilePalette();
        Debug.Log($"FIRE_003 Tile Palette synchronized at {TilePalettePath} without rebuilding the Scene.");
    }

    // Entry point for batch/editor synchronization that deliberately does not open or save the room Scene.
    public static void SyncTilePalette()
    {
        Tile terrainTile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
        Tile hintTile = AssetDatabase.LoadAssetAtPath<Tile>(HintTilePath);
        Require(terrainTile != null, $"Missing FIRE_003 terrain Tile: {TerrainTilePath}");
        Require(hintTile != null, $"Missing FIRE_003 hint Tile: {HintTilePath}");
        TilePaletteAuthoring.EnsureTiles(TilePalettePath, terrainTile, hintTile);
        AssetDatabase.SaveAssets();
    }

    public static void Build()
    {
        Directory.CreateDirectory("Assets/Scenes/Levels/Fire");
        Directory.CreateDirectory("Assets/Tiles/Graybox");

        GameObject platePrefab = RequireAsset(PlatePath);
        GameObject doorPrefab = RequireAsset(DoorPath);
        GameObject groupPrefab = RequireAsset(DoorGroupPath);
        GameObject enemyPrefab = RequireAsset(EnemyPath);
        GameObject exitPrefab = RequireAsset(ExitPath);
        Sprite terrainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TerrainTexturePath);
        Require(terrainSprite != null, $"Missing terrain sprite: {TerrainTexturePath}");
        Sprite builtin = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Tile terrainTile = MakeTile(TerrainTilePath, terrainSprite, Color.white, Tile.ColliderType.Grid);
        Tile hintTile = MakeTile(HintTilePath, builtin, new Color(.15f, .9f, 1f, .75f), Tile.ColliderType.None);
        TilePaletteAuthoring.EnsureTiles(TilePalettePath, terrainTile, hintTile);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = new("FIRE_003 Split Furnace");
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
        BuildTerrain(terrain, terrainTile);
        terrain.CompressBounds();
        terrain.RefreshAllTiles();
        terrain.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        terrain.GetComponent<CompositeCollider2D>().GenerateGeometry();
        decoration.SetTile(new Vector3Int(0, 12, 0), hintTile);
        decoration.SetTile(new Vector3Int(-8, 6, 0), hintTile);
        decoration.SetTile(new Vector3Int(8, 6, 0), hintTile);

        GameObject gameplay = Child(room.transform, "Gameplay");
        Transform dynamicRoot = Child(gameplay.transform, "DynamicObjects").transform;
        Transform entrances = Child(gameplay.transform, "Entrances").transform;
        Transform exits = Child(gameplay.transform, "Exits").transform;

        // FIRE_002 enters through the upper-left wall opening. The spawn is on
        // the safe right lip of the separate adjacent downward opening.
        Transform entrance = Marker("EntranceFromFIRE002", new Vector3(-7.5f, 12.92f), entrances);
        PlayerRoomAuthoring.ConfigureEntrance(entrance, "FROM_FIRE_002", true, true);
        Transform returnEntrance = Marker("EntranceFromFIRE004", new Vector3(10.5f, -17.08f), entrances);
        PlayerRoomAuthoring.ConfigureEntrance(returnEntrance, "FROM_FIRE_004", false, false);

        Camera camera = CreateCamera();
        CameraFollow2D follow = camera.gameObject.AddComponent<CameraFollow2D>();
        follow.Configure(null, true);
        follow.ConfigureDamping(.15f);
        follow.ConfigureBounds(CameraBounds);
        GameObject lightObject = new("Main Light");
        lightObject.transform.SetParent(room.transform);
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = .85f;

        Pair(platePrefab, doorPrefab, dynamicRoot, "Cross-A", new Vector2(-7f, 6.15f), new Vector2(3.5f, 7f), PressurePlate2D.ActivationMode.Occupancy);
        Pair(platePrefab, doorPrefab, dynamicRoot, "Cross-B", new Vector2(7f, 6.15f), new Vector2(-3.5f, 7f), PressurePlate2D.ActivationMode.Occupancy);
        Pair(platePrefab, doorPrefab, dynamicRoot, "Fireball-Latch", new Vector2(-9.5f, -.35f), new Vector2(3.5f, 0f), PressurePlate2D.ActivationMode.FireballLatch);
        Pair(platePrefab, doorPrefab, dynamicRoot, "Cross-C", new Vector2(7f, -6.85f), new Vector2(-3.5f, -6f), PressurePlate2D.ActivationMode.Occupancy);

        Enemy(enemyPrefab, dynamicRoot, "Thrower-E1", new Vector2(-7f, 1f), true);
        Enemy(enemyPrefab, dynamicRoot, "Thrower-E2", new Vector2(7f, -6f), false);
        GameObject finalGroupObject = Instance(groupPrefab, dynamicRoot, "Final-Door-Group", Vector2.zero);
        PressurePlate2D finalA = finalGroupObject.transform.Find("PlateA").GetComponent<PressurePlate2D>();
        PressurePlate2D finalB = finalGroupObject.transform.Find("PlateB").GetComponent<PressurePlate2D>();
        Door2D finalDoor = finalGroupObject.transform.Find("Door").GetComponent<Door2D>();
        finalA.transform.position = new Vector2(-7f, -15.85f);
        finalB.transform.position = new Vector2(7f, -15.85f);
        finalDoor.transform.position = new Vector2(8.5f, -17f);
        PermanentLatchDoorGroup2D group = finalGroupObject.GetComponent<PermanentLatchDoorGroup2D>();
        group.Configure("FIRE_003:DOOR_GROUP:01", finalDoor, finalA, finalB);
        Record(group);

        GameObject exitObject = Instance(exitPrefab, exits, "Exit-A to FIRE_004", new Vector2(10.75f, -18f));
        RoomExit2D exit = exitObject.GetComponent<RoomExit2D>();
        exit.Configure("Fire_004", "DEFAULT");
        Record(exit);

        GameObject systems = Child(room.transform, "RoomSystems");
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, follow, true);

        Validate(scene, terrain);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save FIRE_003.");
        AddBuildScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("FIRE_003 multi-stage greybox built successfully.");
    }

    private static void BuildTerrain(Tilemap map, TileBase tile)
    {
        Fill(map, tile, -12, 11, 14, 14); Fill(map, tile, -12, 11, -20, -20);
        // Upper-left wall opening at Y=11..13 connects FIRE_002 to this room.
        Fill(map, tile, -12, -12, -20, 10); Fill(map, tile, -12, -12, 14, 14);
        Fill(map, tile, 11, 11, -20, 14);
        // Keep two distinct upper-left openings: the side-wall scene entrance
        // and an adjacent two-tile floor drop at X=-10..-9. The right side
        // keeps its independent two-tile floor opening at X=6..7.
        Fill(map, tile, -11, -11, 11, 11);
        Fill(map, tile, -8, 5, 11, 11);
        Fill(map, tile, 8, 10, 11, 11);
        Fill(map, tile, -11, -1, 5, 5); Fill(map, tile, 1, 10, 5, 5);
        Fill(map, tile, -11, -1, -2, -2); Fill(map, tile, 1, 10, -2, -2);
        Fill(map, tile, -11, -1, -8, -8); Fill(map, tile, 1, 10, -8, -8);
        Fill(map, tile, -11, 10, -17, -17);               // final collaboration floor

        // Winding-route ledges and safe drop lips.
        Fill(map, tile, -11, -8, 8, 8); Fill(map, tile, 8, 10, 8, 8);
        Fill(map, tile, -6, -1, 2, 2); Fill(map, tile, 1, 6, 2, 2);
        Fill(map, tile, -11, -8, -5, -5); Fill(map, tile, 8, 10, -5, -5);
        Fill(map, tile, -7, -1, -11, -11); Fill(map, tile, 1, 7, -11, -11);
        Fill(map, tile, -11, -8, -14, -14); Fill(map, tile, 8, 10, -14, -14);

        // Door shaft caps keep standard 1x2 doors from being bypassed.
        Fill(map, tile, -4, -4, 9, 10); Fill(map, tile, 3, 3, 9, 10);
        Fill(map, tile, 3, 3, 2, 4); Fill(map, tile, -4, -4, -4, -3);
        Fill(map, tile, 8, 8, -15, -14);
    }

    private static void Pair(GameObject platePrefab, GameObject doorPrefab, Transform parent, string id,
        Vector2 platePosition, Vector2 doorPosition, PressurePlate2D.ActivationMode mode)
    {
        GameObject plateObject = Instance(platePrefab, parent, $"Plate-{id}", platePosition);
        PressurePlate2D plate = plateObject.GetComponent<PressurePlate2D>();
        plate.ConfigureActivationMode(mode); Record(plate);
        GameObject doorObject = Instance(doorPrefab, parent, $"Door-{id}", doorPosition);
        Door2D door = doorObject.GetComponent<Door2D>();
        door.ConfigureControlSource(plate); Record(door);
    }

    private static void Enemy(GameObject prefab, Transform parent, string name, Vector2 position, bool facingRight)
    {
        GameObject instance = Instance(prefab, parent, name, position);
        HorizontalFireballEnemy2D enemy = instance.GetComponent<HorizontalFireballEnemy2D>();
        enemy.SetInitiallyFacingRight(facingRight); Record(enemy);
    }

    private static Camera CreateCamera()
    {
        GameObject go = new("Main Camera"); go.tag = "MainCamera"; go.transform.position = new Vector3(0f, 8f, -10f);
        Camera camera = go.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 7f;
        camera.backgroundColor = new Color(.055f, .025f, .02f); return camera;
    }

    private static Tilemap CreateLayer(Transform parent, string name)
    { GameObject go = Child(parent, name); Tilemap map = go.AddComponent<Tilemap>(); go.AddComponent<TilemapRenderer>(); return map; }

    private static void ConfigureTerrain(Tilemap map)
    {
        Rigidbody2D body = map.gameObject.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D composite = map.gameObject.AddComponent<CompositeCollider2D>();
        TilemapCollider2D collider = map.gameObject.AddComponent<TilemapCollider2D>();
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        SurfaceSemantic2D semantic = map.gameObject.AddComponent<SurfaceSemantic2D>();
        semantic.Configure(SurfaceSemantic2D.SurfaceType.StaticSolid, true, true);
        MirrorSurface2D mirrorSurface = map.gameObject.AddComponent<MirrorSurface2D>();
        mirrorSurface.kind = MirrorSurface2D.SurfaceKind.Ground;
    }

    private static Tile MakeTile(string path, Sprite sprite, Color color, Tile.ColliderType colliderType)
    { Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path); if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); } tile.sprite = sprite; tile.color = color; tile.colliderType = colliderType; EditorUtility.SetDirty(tile); return tile; }
    private static GameObject Child(Transform parent, string name) { GameObject go = new(name); go.transform.SetParent(parent, false); return go; }
    private static Transform Marker(string name, Vector3 position, Transform parent) { GameObject go = Child(parent, name); go.transform.position = position; return go.transform; }
    private static GameObject Instance(GameObject prefab, Transform parent, string name, Vector2 position) { GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name = name; go.transform.SetParent(parent, false); go.transform.position = position; return go; }
    private static void Record(Component component) { EditorUtility.SetDirty(component); PrefabUtility.RecordPrefabInstancePropertyModifications(component); }
    private static GameObject RequireAsset(string path) { GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path); Require(asset != null, $"Missing prefab: {path}"); return asset; }
    private static void Fill(Tilemap map, TileBase tile, int minX, int maxX, int minY, int maxY) { for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++) map.SetTile(new Vector3Int(x, y), tile); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private static void Validate(Scene scene, Tilemap terrain)
    {
        Require(terrain.GetComponent<CompositeCollider2D>().pathCount > 0, "Terrain collider geometry is empty.");
        Require(!terrain.HasTile(new Vector3Int(-12, 12)),
            "Upper-left FIRE_002 side-wall entrance must remain open.");
        Require(!terrain.HasTile(new Vector3Int(-10, 11)) && !terrain.HasTile(new Vector3Int(-9, 11)),
            "Upper-left downward opening must remain separate and clear.");
        Require(!terrain.HasTile(new Vector3Int(6, 11)) && !terrain.HasTile(new Vector3Int(7, 11)),
            "Right two-tile descent opening must remain clear.");
        Require(terrain.HasTile(new Vector3Int(-11, 11)) && terrain.HasTile(new Vector3Int(-8, 11)) &&
                terrain.HasTile(new Vector3Int(5, 11)) && terrain.HasTile(new Vector3Int(8, 11)),
            "Both downward openings must retain predictable solid lips.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0, "Scene must not serialize Player.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1, "Scene needs one player spawner.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PressurePlate2D>(true)).Count() == 6, "FIRE_003 needs six plates.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Door2D>(true)).Count() == 5, "FIRE_003 needs five doors.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<HorizontalFireballEnemy2D>(true)).Count() == 2, "FIRE_003 needs two throwers.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<EruptionHazard2D>(true)).Count() == 1, "FIRE_003 keeps one eruption lesson.");
        PermanentLatchDoorGroup2D group = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PermanentLatchDoorGroup2D>(true)).Single();
        Require(group.DoorGroupId == "FIRE_003:DOOR_GROUP:01", "Final latch group ID mismatch.");
    }

    private static void AddBuildScene()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Any(x => x.path == ScenePath)) return;
        EditorBuildSettings.scenes = scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
    }
}
