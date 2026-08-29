using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds the approved FIRE_004 fireball-latch door room from reusable prefabs.</summary>
public static class Fire004RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Fire/Fire_004.unity";
    private const string TilePalettePath = "Assets/TilePalettes/Fire.prefab";
    private const string TerrainTilePath = "Assets/Tiles/Graybox/Fire004Terrain.asset";
    private const string HintTilePath = "Assets/Tiles/Graybox/Fire004MirrorHint.asset";
    private const string TerrainTexturePath = "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_sand_block_center.png";
    private const string PlatePath = "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab";
    private const string DoorPath = "Assets/Prefabs/Gameplay/Doors/Door2D.prefab";
    private const string EnemyPath = "Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab";
    private const string ExitPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";

    [MenuItem("Tools/W1/Build FIRE-004 Greybox")]
    public static void BuildFromMenu() => Build();

    [MenuItem("Tools/W1/Fix FIRE-004 Exit Gaps")]
    public static void FixExitGaps()
    {
        Scene scene = SceneManager.GetActiveScene();
        Require(scene.path == ScenePath, "Open FIRE_004 before fixing its exit gaps.");
        Tilemap terrain = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
            .Single(map => map.name == "Terrain");
        Undo.RecordObject(terrain, "Open FIRE-004 exit gaps");
        terrain.SetTile(new Vector3Int(-12, -2, 0), null);
        terrain.SetTile(new Vector3Int(-12, -1, 0), null);
        terrain.SetTile(new Vector3Int(11, 0, 0), null);
        terrain.SetTile(new Vector3Int(11, 1, 0), null);
        terrain.CompressBounds();
        terrain.RefreshAllTiles();
        terrain.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        terrain.GetComponent<CompositeCollider2D>().GenerateGeometry();
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save FIRE_004 exit gaps.");
    }

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
        Sprite builtin = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Tile terrainTile = MakeTile(TerrainTilePath, terrainSprite, Color.white, Tile.ColliderType.Grid);
        Tile hintTile = MakeTile(HintTilePath, builtin, new Color(.15f, .9f, 1f, .75f), Tile.ColliderType.None);
        TilePaletteAuthoring.EnsureTiles(TilePalettePath, terrainTile, hintTile);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = new("FIRE_004 Borrowed Fire Door");
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
        Fill(terrain, terrainTile, -12, -8, -3, -3);
        Fill(terrain, terrainTile, -8, -7, -2, -2);
        Fill(terrain, terrainTile, -7, 11, -1, -1);
        Fill(terrain, terrainTile, -12, 11, 6, 6);
        Fill(terrain, terrainTile, -12, -12, -3, -3);
        Fill(terrain, terrainTile, -12, -12, 0, 6);
        Fill(terrain, terrainTile, 11, 11, -1, -1);
        Fill(terrain, terrainTile, 11, 11, 2, 6);
        Fill(terrain, terrainTile, 8, 8, 2, 6);
        decoration.SetTile(new Vector3Int(0, 0, 0), hintTile);
        terrain.CompressBounds();
        terrain.RefreshAllTiles();
        terrain.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        terrain.GetComponent<CompositeCollider2D>().GenerateGeometry();

        GameObject gameplay = Child(room.transform, "Gameplay");
        Transform dynamicRoot = Child(gameplay.transform, "DynamicObjects").transform;
        Transform entrances = Child(gameplay.transform, "Entrances").transform;
        Transform exits = Child(gameplay.transform, "Exits").transform;
        Transform entrance = Marker("EntranceFromFIRE003", new Vector3(-10.5f, -1.08f), entrances);
        PlayerRoomAuthoring.ConfigureEntrance(entrance, "DEFAULT", true, true);
        Transform returnEntrance = Marker("EntranceFromFIRE005", new Vector3(10.5f, .92f), entrances);
        PlayerRoomAuthoring.ConfigureEntrance(returnEntrance, "FROM_FIRE_005", false, false);

        GameObject plateObject = Instance(platePrefab, dynamicRoot, "Latch-A", new Vector2(5.5f, .625f));
        plateObject.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        PressurePlate2D plate = plateObject.GetComponent<PressurePlate2D>();
        plate.ConfigureActivationMode(PressurePlate2D.ActivationMode.FireballLatch);
        Record(plate);
        PrefabUtility.RecordPrefabInstancePropertyModifications(plateObject.transform);

        GameObject enemyObject = Instance(enemyPrefab, dynamicRoot, "Enemy-H1", new Vector2(2.5f, .5f));
        HorizontalFireballEnemy2D enemy = enemyObject.GetComponent<HorizontalFireballEnemy2D>();
        Require(enemy != null, "Horizontal fireball enemy Prefab is missing its runtime component.");
        enemy.SetInitiallyFacingRight(true);
        Record(enemy);

        GameObject doorObject = Instance(doorPrefab, dynamicRoot, "Door-A", new Vector2(8.5f, 1f));
        Door2D door = doorObject.GetComponent<Door2D>();
        door.ConfigureControlSource(plate);
        Record(door);
        GameObject backExitObject = Instance(exitPrefab, exits, "Exit-Back-to-FIRE003", new Vector2(-11.5f, -1.1f));
        RoomExit2D backExit = backExitObject.GetComponent<RoomExit2D>();
        backExit.Configure("Fire_003", "FROM_FIRE_004");
        Record(backExit);
        GameObject forwardExitObject = Instance(exitPrefab, exits, "Exit-To-FIRE005", new Vector2(11.5f, .9f));
        RoomExit2D forwardExit = forwardExitObject.GetComponent<RoomExit2D>();
        forwardExit.Configure("Fire_005", "DEFAULT");
        Record(forwardExit);

        Camera camera = CreateCamera();
        GameObject lightObject = new("Main Light");
        lightObject.transform.SetParent(room.transform);
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = .85f;
        GameObject systems = Child(room.transform, "RoomSystems");
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, null, true);

        Validate(scene, terrain, plate, door, enemy, backExit, forwardExit, camera);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save FIRE_004.");
        AddBuildScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Camera CreateCamera()
    {
        GameObject go = new("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 2f, -10f);
        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.backgroundColor = new Color(.055f, .025f, .02f);
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
    }

    private static Tile MakeTile(string path, Sprite sprite, Color color, Tile.ColliderType colliderType)
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
        tile.sprite = sprite;
        tile.color = color;
        tile.colliderType = colliderType;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void Validate(Scene scene, Tilemap terrain, PressurePlate2D plate, Door2D door,
        HorizontalFireballEnemy2D enemy, RoomExit2D backExit, RoomExit2D forwardExit, Camera camera)
    {
        Require(terrain.GetComponent<CompositeCollider2D>().pathCount > 0, "Terrain collider geometry is empty.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0, "Scene must not serialize Player.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1, "Scene needs one player spawner.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PressurePlate2D>(true)).Count() == 1, "FIRE_004 needs one plate.");
        Require(plate.Mode == PressurePlate2D.ActivationMode.FireballLatch, "Latch-A must use FireballLatch mode.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Door2D>(true)).Count() == 1, "FIRE_004 needs one door.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<HorizontalFireballEnemy2D>(true)).Count() == 1, "FIRE_004 needs one thrower.");
        Require(enemy != null, "Enemy-H1 is missing.");
        Require(door.ControlSource == plate, "Door-A must be controlled by Latch-A.");
        Require(backExit.TargetScene == "Fire_003" && backExit.TargetEntranceId == "FROM_FIRE_004", "Return exit target mismatch.");
        Require(forwardExit.TargetScene == "Fire_005" && forwardExit.TargetEntranceId == "DEFAULT", "Forward exit target mismatch.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<RoomExit2D>(true)).Count() == 2, "FIRE_004 needs two exits.");
        Require(camera.GetComponent<CameraFollow2D>() == null, "FIRE_004 must use a fixed camera.");
    }

    private static GameObject Child(Transform parent, string name) { GameObject go = new(name); go.transform.SetParent(parent, false); return go; }
    private static Transform Marker(string name, Vector3 position, Transform parent) { GameObject go = Child(parent, name); go.transform.position = position; return go.transform; }
    private static GameObject Instance(GameObject prefab, Transform parent, string name, Vector2 position) { GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name = name; go.transform.SetParent(parent, false); go.transform.position = position; return go; }
    private static void Record(Component component) { EditorUtility.SetDirty(component); PrefabUtility.RecordPrefabInstancePropertyModifications(component); }
    private static GameObject RequireAsset(string path) { GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path); Require(asset != null, $"Missing prefab: {path}"); return asset; }
    private static void Fill(Tilemap map, TileBase tile, int minX, int maxX, int minY, int maxY) { for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++) map.SetTile(new Vector3Int(x, y), tile); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void AddBuildScene()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Any(x => x.path == ScenePath)) return;
        EditorBuildSettings.scenes = scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
    }
}
