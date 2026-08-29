using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds the exit-free FIRE_019 three-corridor greybox.</summary>
public static class Fire019RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Fire/Fire_019.unity";
    private const string TerrainTilePath = "Assets/Tiles/Graybox/Fire018Terrain.asset";
    private const string HintTilePath = "Assets/Tiles/Graybox/Fire018MirrorHint.asset";
    private const string PlatePath = "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab";
    private const string DoorPath = "Assets/Prefabs/Gameplay/Doors/Door2D.prefab";
    private const string PatrolPath = "Assets/Prefabs/Gameplay/Enemies/PatrollingHorizontalFireballEnemy2D.prefab";
    private const string ThrowerPath = "Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab";

    [MenuItem("Tools/W1/Build FIRE-019 Greybox")]
    public static void BuildFromMenu()
    {
        Tile terrainTile = RequireAsset<Tile>(TerrainTilePath);
        Tile hintTile = RequireAsset<Tile>(HintTilePath);
        GameObject platePrefab = RequireAsset<GameObject>(PlatePath);
        GameObject doorPrefab = RequireAsset<GameObject>(DoorPath);
        GameObject patrolPrefab = RequireAsset<GameObject>(PatrolPath);
        GameObject throwerPrefab = RequireAsset<GameObject>(ThrowerPath);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // NewScene can unload asset references that were only held by local variables.
        terrainTile = RequireAsset<Tile>(TerrainTilePath);
        hintTile = RequireAsset<Tile>(HintTilePath);
        GameObject room = new("FIRE_019 Three Corridor Fire Lure");
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

        // Outer shell and three horizontal corridors.
        Fill(terrain, terrainTile, -12, 11, -7, -7);
        Fill(terrain, terrainTile, -12, 11, 7, 7);
        Fill(terrain, terrainTile, -12, -12, -6, 6);
        Fill(terrain, terrainTile, 11, 11, -6, 6);
        Fill(terrain, terrainTile, -11, 10, 4, 4);
        Fill(terrain, terrainTile, -11, 3, -1, -1);
        Fill(terrain, terrainTile, 5, 10, -1, -1); // X=4 is the MirrorClone drop.
        Fill(terrain, terrainTile, -11, 10, -6, -6);

        // Door cap prevents bypass while preserving the standard two-cell opening.
        Fill(terrain, terrainTile, 0, 0, 2, 3);
        // Upper-right drop places Player on the right side of Door-A after releasing Plate-A.
        terrain.SetTile(new Vector3Int(4, 4, 0), null);
        decoration.SetTile(new Vector3Int(7, 0, 0), hintTile);
        Bake(terrain);

        GameObject gameplay = Child(room.transform, "Gameplay");
        Transform dynamicRoot = Child(gameplay.transform, "DynamicObjects").transform;
        Transform entrances = Child(gameplay.transform, "Entrances").transform;
        Child(gameplay.transform, "Exits"); // Intentionally empty until a destination room is approved.

        Transform entrance = Marker("Entrance-DEFAULT", new Vector3(-10.5f, 5.92f), entrances);
        PlayerRoomAuthoring.ConfigureEntrance(entrance, "DEFAULT", true, true);

        PressurePlate2D plate = Instance(platePrefab, dynamicRoot, "Plate-A", new Vector2(-2.5f, 5.3f))
            .GetComponent<PressurePlate2D>();
        plate.ConfigureActivationMode(PressurePlate2D.ActivationMode.Occupancy);
        Record(plate);

        Door2D door = Instance(doorPrefab, dynamicRoot, "Door-A", new Vector2(.5f, 1f))
            .GetComponent<Door2D>();
        door.ConfigureControlSource(plate);
        door.SetState(Door2D.VisualState.Closed);
        Record(door);

        PatrollingHorizontalFireballEnemy2D patrol =
            Instance(patrolPrefab, dynamicRoot, "Enemy-Middle-Patrolling", new Vector2(-7.5f, .5f))
                .GetComponent<PatrollingHorizontalFireballEnemy2D>();
        patrol.SetInitiallyMovingRight(true);
        Record(patrol);

        HorizontalFireballEnemy2D lower =
            Instance(throwerPrefab, dynamicRoot, "Enemy-Lower-Fixed", new Vector2(-8.5f, -4.5f))
                .GetComponent<HorizontalFireballEnemy2D>();
        lower.SetInitiallyFacingRight(true);
        Record(lower);

        CreateCamera();
        GameObject lightObject = Child(room.transform, "Main Light");
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = .85f;

        GameObject systems = Child(room.transform, "RoomSystems");
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, null, true);

        Validate(scene, terrain, plate, door, patrol, lower);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save FIRE_019 scene.");
        AddBuildScene(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("FIRE_019 exit-free three-corridor greybox built successfully.");
    }

    private static void Validate(Scene scene, Tilemap terrain, PressurePlate2D plate, Door2D door,
        PatrollingHorizontalFireballEnemy2D patrol, HorizontalFireballEnemy2D lower)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(terrain.GetUsedTilesCount() > 0, "Terrain Tilemap is empty.");
        Require(terrain.GetComponent<TilemapCollider2D>() != null &&
                terrain.GetComponent<CompositeCollider2D>() != null &&
                terrain.GetComponent<Rigidbody2D>()?.bodyType == RigidbodyType2D.Static,
            "Terrain requires TilemapCollider2D, CompositeCollider2D and a Static Rigidbody2D.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "FIRE_019 must not serialize Player.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "FIRE_019 needs one Player spawner.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomExit2D>(true)).Count() == 0,
            "FIRE_019 must not contain an exit yet.");
        Require(plate.Mode == PressurePlate2D.ActivationMode.Occupancy,
            "Plate-A must use Occupancy mode.");
        Require(door.ControlSource == plate,
            "Door-A must be controlled only by Plate-A.");
        Require(patrol != null && lower != null, "Both throwers must exist.");
        Require(PrefabUtility.GetPrefabInstanceStatus(patrol.gameObject) == PrefabInstanceStatus.Connected,
            "Patrolling thrower must remain connected to its shared Prefab.");
        Require(PrefabUtility.GetPrefabInstanceStatus(lower.gameObject) == PrefabInstanceStatus.Connected,
            "Fixed thrower must remain connected to its shared Prefab.");
    }

    private static Camera CreateCamera()
    {
        GameObject go = new("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
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
        map.gameObject.AddComponent<SurfaceSemantic2D>()
            .Configure(SurfaceSemantic2D.SurfaceType.StaticSolid, true, true);
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
        PrefabUtility.RecordPrefabInstancePropertyModifications(component.transform);
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Require(asset != null, $"Missing asset: {path}");
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
        if (!scenes.Any(item => item.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
