using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds FIRE_018 from shared gameplay prefabs and adds its return link to FIRE_017.</summary>
public static class Fire018RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Fire/Fire_018.unity";
    private const string Fire017ScenePath = "Assets/Scenes/Levels/Fire/Fire_017.unity";
    private const string TerrainTilePath = "Assets/Tiles/Graybox/Fire018Terrain.asset";
    private const string HintTilePath = "Assets/Tiles/Graybox/Fire018MirrorHint.asset";
    private const string TerrainTexturePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_sand_block_center.png";
    private const string PlatePath = "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab";
    private const string DoorPath = "Assets/Prefabs/Gameplay/Doors/Door2D.prefab";
    private const string EnemyPath = "Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab";
    private const string ExitPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";

    [MenuItem("Tools/W1/Build FIRE-018 Greybox")]
    public static void BuildFromMenu()
    {
        BuildFire018();
        ConnectFire017();
        AssetDatabase.SaveAssets();
        Debug.Log("FIRE_018 greybox and FIRE_017 connection built successfully.");
    }

    private static void BuildFire018()
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

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = new("FIRE_018 Twin Fire Lines");
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

        // Outer shell, upper floor with a central drop, and lower corridor.
        Fill(terrain, terrainTile, -12, 11, -5, -5);
        Fill(terrain, terrainTile, -12, 11, 6, 6);
        Fill(terrain, terrainTile, -12, -12, -4, 5);
        Fill(terrain, terrainTile, 11, 11, -4, 5);
        Fill(terrain, terrainTile, -11, -2, 1, 1);
        Fill(terrain, terrainTile, 1, 10, 1, 1);
        // Prevent bypassing the upper door while keeping the two corridors visually distinct.
        Fill(terrain, terrainTile, 6, 6, 4, 5);
        decoration.SetTile(new Vector3Int(-2, 2, 0), hintTile);
        Bake(terrain);

        GameObject gameplay = Child(room.transform, "Gameplay");
        Transform dynamicRoot = Child(gameplay.transform, "DynamicObjects").transform;
        Transform entrances = Child(gameplay.transform, "Entrances").transform;
        Transform exits = Child(gameplay.transform, "Exits").transform;

        Transform entrance = Marker("Entrance-DEFAULT-From-FIRE_017", new Vector3(-10.5f, 2.92f), entrances);
        PlayerRoomAuthoring.ConfigureEntrance(entrance, "DEFAULT", true, true);
        GameObject returnExitObject = Instance(exitPrefab, exits, "Exit-Back-to-FIRE_017", new Vector2(-11.5f, 3f));
        RoomExit2D returnExit = returnExitObject.GetComponent<RoomExit2D>();
        returnExit.Configure("Fire_017", "FROM_FIRE_018");
        Record(returnExit);

        PressurePlate2D upperLatch = FireballLatch(platePrefab, dynamicRoot, "Latch-Upper", new Vector2(8.5f, 2.625f));
        PressurePlate2D lowerLatch = FireballLatch(platePrefab, dynamicRoot, "Latch-Lower", new Vector2(8.5f, -3.375f));
        HorizontalFireballEnemy2D upperEnemy = Enemy(enemyPrefab, dynamicRoot, "Enemy-Upper", new Vector2(10.5f, 2.5f));
        HorizontalFireballEnemy2D lowerEnemy = Enemy(enemyPrefab, dynamicRoot, "Enemy-Lower", new Vector2(10.5f, -3.5f));
        GameObject doorObject = Instance(doorPrefab, dynamicRoot, "Door-Upper", new Vector2(6.5f, 3f));
        Door2D door = doorObject.GetComponent<Door2D>();
        door.ConfigureControlSources(Door2D.ControlLogic.And, upperLatch, lowerLatch);
        door.SetState(Door2D.VisualState.Closed);
        Record(door);

        Camera camera = CreateCamera();
        GameObject lightObject = Child(room.transform, "Main Light");
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = .85f;
        GameObject systems = Child(room.transform, "RoomSystems");
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, null, true);

        ValidateFire018(scene, terrain, upperLatch, lowerLatch, door, upperEnemy, lowerEnemy, returnExit, camera);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save FIRE_018 scene.");
        AddBuildScene(ScenePath);
    }

    private static void ConnectFire017()
    {
        Scene scene = EditorSceneManager.OpenScene(Fire017ScenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        Tilemap terrain = roots.SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
            .Single(map => map.name == "Terrain");
        // Open a two-tile right-side passage without rebuilding the existing room.
        terrain.SetTile(new Vector3Int(15, -2, 0), null);
        terrain.SetTile(new Vector3Int(15, -1, 0), null);
        Bake(terrain);

        Transform gameplay = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Single(transform => transform.name == "Gameplay");
        Transform entrances = gameplay.Find("Entrances");
        Transform exits = gameplay.Find("Exits");
        Require(entrances != null && exits != null, "FIRE_017 entrance/exit roots are missing.");

        Transform oldEntrance = entrances.Find("Entrance-FROM_FIRE_018");
        if (oldEntrance != null) UnityEngine.Object.DestroyImmediate(oldEntrance.gameObject);
        Transform returnEntrance = Marker("Entrance-FROM_FIRE_018", new Vector3(13.5f, -1.08f), entrances);
        PlayerRoomAuthoring.ConfigureEntrance(returnEntrance, "FROM_FIRE_018", false, false);

        Transform oldExit = exits.Find("Exit-B to FIRE_018");
        if (oldExit != null) UnityEngine.Object.DestroyImmediate(oldExit.gameObject);
        GameObject exitPrefab = RequireAsset(ExitPath);
        GameObject exitObject = Instance(exitPrefab, exits, "Exit-B to FIRE_018", new Vector2(14.5f, -1f));
        RoomExit2D exit = exitObject.GetComponent<RoomExit2D>();
        exit.Configure("Fire_018", "DEFAULT");
        Record(exit);

        Require(entrances.GetComponentsInChildren<RoomEntrance2D>(true)
            .Count(item => item.EntranceId == "FROM_FIRE_018") == 1, "FIRE_017 needs one FROM_FIRE_018 entrance.");
        Require(exits.GetComponentsInChildren<RoomExit2D>(true)
            .Count(item => item.TargetScene == "Fire_018") == 1, "FIRE_017 needs one FIRE_018 exit.");
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, Fire017ScenePath), "Failed to save FIRE_017 connection.");
    }

    private static PressurePlate2D FireballLatch(GameObject prefab, Transform parent, string name, Vector2 position)
    {
        GameObject instance = Instance(prefab, parent, name, position);
        instance.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        PressurePlate2D plate = instance.GetComponent<PressurePlate2D>();
        plate.ConfigureActivationMode(PressurePlate2D.ActivationMode.FireballLatch);
        Record(plate);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        return plate;
    }

    private static HorizontalFireballEnemy2D Enemy(GameObject prefab, Transform parent, string name, Vector2 position)
    {
        GameObject instance = Instance(prefab, parent, name, position);
        HorizontalFireballEnemy2D enemy = instance.GetComponent<HorizontalFireballEnemy2D>();
        Require(enemy != null, $"{name} is missing HorizontalFireballEnemy2D.");
        enemy.SetInitiallyFacingRight(false);
        Record(enemy);
        return enemy;
    }

    private static Camera CreateCamera()
    {
        GameObject go = new("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, .5f, -10f);
        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.backgroundColor = new Color(.055f, .025f, .02f);
        go.AddComponent<AudioListener>();
        return camera;
    }

    private static void ValidateFire018(Scene scene, Tilemap terrain, PressurePlate2D upperLatch,
        PressurePlate2D lowerLatch, Door2D door, HorizontalFireballEnemy2D upperEnemy,
        HorizontalFireballEnemy2D lowerEnemy, RoomExit2D exit, Camera camera)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(terrain.GetComponent<CompositeCollider2D>().pathCount > 0, "Terrain collider geometry is empty.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "FIRE_018 must not serialize Player.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "FIRE_018 needs one player spawner.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PressurePlate2D>(true)).Count() == 2,
            "FIRE_018 needs two pressure plates.");
        Require(upperLatch.Mode == PressurePlate2D.ActivationMode.FireballLatch &&
                lowerLatch.Mode == PressurePlate2D.ActivationMode.FireballLatch,
            "Both FIRE_018 plates must use FireballLatch mode.");
        Require(door.Logic == Door2D.ControlLogic.And && door.ControlSources.Length == 2 &&
                door.ControlSources.Contains(upperLatch) && door.ControlSources.Contains(lowerLatch),
            "Door-Upper must use both latches with AND logic.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<HorizontalFireballEnemy2D>(true)).Count() == 2,
            "FIRE_018 needs two throwers.");
        Require(upperEnemy != null && lowerEnemy != null, "FIRE_018 throwers are missing.");
        Require(exit.TargetScene == "Fire_017" && exit.TargetEntranceId == "FROM_FIRE_018",
            "FIRE_018 return exit target mismatch.");
        Require(camera.GetComponent<CameraFollow2D>() == null, "FIRE_018 must use a fixed camera.");
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
        MirrorSurface2D mirror = map.gameObject.AddComponent<MirrorSurface2D>();
        mirror.kind = MirrorSurface2D.SurfaceKind.Ground;
        mirror.safe = true;
    }

    private static void Bake(Tilemap map)
    {
        map.CompressBounds();
        map.RefreshAllTiles();
        map.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        Physics2D.SyncTransforms();
        map.GetComponent<CompositeCollider2D>().GenerateGeometry();
        Require(map.GetComponent<CompositeCollider2D>().pathCount > 0, $"{map.name} collider geometry is empty.");
    }

    private static Tile MakeTile(string path, Sprite sprite, Color color, Tile.ColliderType colliderType)
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
        tile.name = Path.GetFileNameWithoutExtension(path);
        tile.sprite = sprite;
        tile.color = color;
        tile.colliderType = colliderType;
        EditorUtility.SetDirty(tile);
        return tile;
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
            for (int x = minX; x <= maxX; x++) map.SetTile(new Vector3Int(x, y), tile);
    }

    private static void AddBuildScene(string path)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(scene => scene.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
