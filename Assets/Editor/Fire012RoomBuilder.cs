using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds the FIRE_012 "door as shield" Tilemap greybox.</summary>
public static class Fire012RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Fire/Fire_012.unity";
    private const string TerrainTilePath = "Assets/Tiles/Graybox/Fire009Terrain.asset";
    private const string HintTilePath = "Assets/Tiles/Graybox/Fire009MirrorHint.asset";
    private const string PlatePrefabPath = "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab";
    private const string DoorPrefabPath = "Assets/Prefabs/Gameplay/Doors/Door2D.prefab";
    private const string EnemyPrefabPath = "Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab";
    private const string ExitPrefabPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";

    [MenuItem("Tools/W1/Build FIRE-012 Greybox")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    public static void BuildFromCommandLine()
    {
        BuildScene();
        AssetDatabase.SaveAssets();
        Debug.Log("FIRE_012 door-as-shield Tilemap greybox built successfully.");
    }

    private static void BuildScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Tile terrainTile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
        Tile hintTile = AssetDatabase.LoadAssetAtPath<Tile>(HintTilePath);
        GameObject platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlatePrefabPath);
        GameObject doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath);
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        GameObject exitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExitPrefabPath);
        Require(terrainTile && hintTile && platePrefab && doorPrefab && enemyPrefab && exitPrefab,
            "FIRE_012 Tile or Prefab dependency is missing.");

        GameObject room = new("FIRE_012 Door Is A Shield");
        GameObject gridRoot = new("Grid");
        gridRoot.transform.SetParent(room.transform);
        gridRoot.AddComponent<Grid>();
        CreateTilemapLayer(gridRoot.transform, "Background");
        Tilemap terrain = CreateTilemapLayer(gridRoot.transform, "Terrain");
        ConfigureTerrain(terrain);
        CreateTilemapLayer(gridRoot.transform, "OneWayPlatform");
        CreateTilemapLayer(gridRoot.transform, "SpecialMirrorWall");
        CreateTilemapLayer(gridRoot.transform, "Hazard");
        Tilemap decoration = CreateTilemapLayer(gridRoot.transform, "Decoration");
        CreateTilemapLayer(gridRoot.transform, "Foreground");

        // Lower puzzle lane and sealed two-tile door opening.
        Fill(terrain, terrainTile, -15, 14, -3, -3);
        Fill(terrain, terrainTile, -15, 14, 6, 6);
        Fill(terrain, terrainTile, -15, -15, -2, 5);
        Fill(terrain, terrainTile, 14, 14, -2, 5);
        Fill(terrain, terrainTile, -5, -5, 1, 5);

        // Left stair reaches a high return lane; spawn cannot reach it directly.
        Fill(terrain, terrainTile, -13, -12, -2, -2);
        Fill(terrain, terrainTile, -13, -11, -1, -1);
        Fill(terrain, terrainTile, -13, -10, 0, 0);
        Fill(terrain, terrainTile, -13, -9, 1, 1);
        Fill(terrain, terrainTile, -13, 11, 2, 2);
        Fill(terrain, terrainTile, 11, 11, 3, 5);

        // MirrorClone stop wall beyond the plate.
        Fill(terrain, terrainTile, 10, 10, -2, 0);
        decoration.SetTile(new Vector3Int(1, -2, 0), hintTile);
        BakeTerrain(terrain);

        GameObject gameplay = new("Gameplay");
        gameplay.transform.SetParent(room.transform);
        GameObject dynamicObjects = new("DynamicObjects");
        dynamicObjects.transform.SetParent(gameplay.transform);
        GameObject entrances = new("Entrances");
        entrances.transform.SetParent(gameplay.transform);
        GameObject exits = new("Exits");
        exits.transform.SetParent(gameplay.transform);

        Transform entrance = Marker("Entrance-DEFAULT", new Vector3(1f, -1.08f, 0f), entrances.transform);
        PressurePlate2D plate = CreatePlate(platePrefab, dynamicObjects.transform);
        Door2D door = CreateDoor(doorPrefab, dynamicObjects.transform, plate);
        HorizontalFireballEnemy2D enemy = CreateEnemy(enemyPrefab, dynamicObjects.transform);
        CreateExit(exitPrefab, exits.transform);
        CreateCamera();

        GameObject systems = new("RoomSystems");
        systems.transform.SetParent(room.transform);
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        BindRuntimeScript(reset, "Assets/Scripts/Core/RoomResetSystem.cs");
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, null, false);

        ValidateScene(scene, terrain, plate, door, enemy);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save FIRE_012 scene.");
        AddBuildScene(ScenePath);
    }

    private static PressurePlate2D CreatePlate(GameObject prefab, Transform parent)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Plate-A";
        instance.transform.SetParent(parent, false);
        instance.transform.position = new Vector3(8.5f, -1.7f, 0f);
        return instance.GetComponent<PressurePlate2D>();
    }

    private static Door2D CreateDoor(GameObject prefab, Transform parent, PressurePlate2D plate)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Door-A Shield";
        instance.transform.SetParent(parent, false);
        instance.transform.position = new Vector3(-5f, -1.5f, 0f);
        Door2D door = instance.GetComponent<Door2D>();
        door.ConfigureControlSource(plate);
        door.SetState(Door2D.VisualState.Closed);
        EditorUtility.SetDirty(door);
        PrefabUtility.RecordPrefabInstancePropertyModifications(door);
        return door;
    }

    private static HorizontalFireballEnemy2D CreateEnemy(GameObject prefab, Transform parent)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Enemy-H1 Horizontal Fireball";
        instance.transform.SetParent(parent, false);
        instance.transform.position = new Vector3(12.5f, -1.5f, 0f);
        HorizontalFireballEnemy2D enemy = instance.GetComponent<HorizontalFireballEnemy2D>();
        enemy.SetInitiallyFacingRight(false);
        EditorUtility.SetDirty(enemy);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
        return enemy;
    }

    private static void CreateExit(GameObject prefab, Transform parent)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Exit-A Return to FIRE_011";
        instance.transform.SetParent(parent, false);
        instance.transform.position = new Vector3(9.5f, 3f, 0f);
        RoomExit2D exit = instance.GetComponent<RoomExit2D>();
        exit.Configure("Fire_011", "DEFAULT");
        EditorUtility.SetDirty(exit);
        PrefabUtility.RecordPrefabInstancePropertyModifications(exit);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.backgroundColor = new Color(.055f, .025f, .02f);
        cameraObject.AddComponent<AudioListener>();
    }

    private static Tilemap CreateTilemapLayer(Transform parent, string name)
    {
        GameObject layer = new(name);
        layer.transform.SetParent(parent, false);
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
        SurfaceSemantic2D semantic = terrain.gameObject.AddComponent<SurfaceSemantic2D>();
        BindRuntimeScript(semantic, "Assets/Scripts/Gameplay/SurfaceSemantic2D.cs");
        semantic.Configure(SurfaceSemantic2D.SurfaceType.StaticSolid, true, true);
        MirrorSurface2D surface = terrain.gameObject.AddComponent<MirrorSurface2D>();
        BindRuntimeScript(surface, "Assets/Scripts/Gameplay/MirrorSurface2D.cs");
        surface.kind = MirrorSurface2D.SurfaceKind.Ground;
        surface.safe = true;
    }

    private static void BakeTerrain(Tilemap terrain)
    {
        terrain.CompressBounds();
        terrain.RefreshAllTiles();
        terrain.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        terrain.GetComponent<CompositeCollider2D>().GenerateGeometry();
        Physics2D.SyncTransforms();
        Require(terrain.GetComponent<CompositeCollider2D>().pathCount > 0,
            "FIRE_012 Terrain collider geometry was not generated.");
    }

    private static Transform Marker(string name, Vector3 position, Transform parent)
    {
        GameObject marker = new(name);
        marker.transform.SetParent(parent);
        marker.transform.position = position;
        return marker.transform;
    }

    private static void Fill(Tilemap map, TileBase tile, int minX, int maxX, int minY, int maxY)
    {
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                map.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private static void ValidateScene(Scene scene, Tilemap terrain, PressurePlate2D plate,
        Door2D door, HorizontalFireballEnemy2D enemy)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "FIRE_012 must not serialize a room-local Player.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "FIRE_012 must contain exactly one RoomPlayerSpawner2D.");
        Require(door.ControlSource == plate, "Door-A must explicitly reference Plate-A.");
        Require(PrefabUtility.GetPrefabInstanceStatus(plate.gameObject) == PrefabInstanceStatus.Connected &&
                PrefabUtility.GetPrefabInstanceStatus(door.gameObject) == PrefabInstanceStatus.Connected &&
                PrefabUtility.GetPrefabInstanceStatus(enemy.gameObject) == PrefabInstanceStatus.Connected,
            "FIRE_012 gameplay objects must remain connected to shared Prefabs.");
        Require(terrain.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.StaticSolid &&
                terrain.GetComponent<MirrorSurface2D>()?.kind == MirrorSurface2D.SurfaceKind.Ground,
            "FIRE_012 Terrain semantics are invalid.");
        Require(door.GetComponent<BoxCollider2D>().size == new Vector2(1f, 2f),
            "FIRE_012 must use the standard two-tile Door2D size.");
    }

    private static void AddBuildScene(string path)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(entry => entry.path == path))
            scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void BindRuntimeScript(UnityEngine.Object behaviour, string scriptPath)
    {
        MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        Require(script != null, $"Runtime script asset is missing: {scriptPath}");
        SerializedObject serialized = new(behaviour);
        SerializedProperty scriptProperty = serialized.FindProperty("m_Script");
        Require(scriptProperty != null, $"m_Script is unavailable on {behaviour.GetType().Name}.");
        scriptProperty.objectReferenceValue = script;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
