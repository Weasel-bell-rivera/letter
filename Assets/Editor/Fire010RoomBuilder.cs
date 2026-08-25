using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds the FIRE_010 lava-split and single-door Tilemap greybox.</summary>
public static class Fire010RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Fire/Fire_010.unity";
    private const string TerrainTilePath = "Assets/Tiles/Graybox/Fire009Terrain.asset";
    private const string HazardTilePath = "Assets/Tiles/Graybox/Fire008Hazard.asset";
    private const string HintTilePath = "Assets/Tiles/Graybox/Fire009MirrorHint.asset";
    private const string PlatePrefabPath = "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab";
    private const string DoorPrefabPath = "Assets/Prefabs/Gameplay/Doors/Door2D.prefab";
    private const string ExitPrefabPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";

    [MenuItem("Tools/W1/Build FIRE-010 Greybox")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    public static void BuildFromCommandLine()
    {
        Tile terrainTile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
        Tile hazardTile = AssetDatabase.LoadAssetAtPath<Tile>(HazardTilePath);
        Tile hintTile = AssetDatabase.LoadAssetAtPath<Tile>(HintTilePath);
        GameObject platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlatePrefabPath);
        GameObject doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath);
        GameObject exitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExitPrefabPath);
        Require(terrainTile != null && hazardTile != null && hintTile != null &&
                platePrefab != null && doorPrefab != null && exitPrefab != null,
            "FIRE_010 shared Tile or Prefab dependency is missing.");

        BuildScene(terrainTile, hazardTile, hintTile, platePrefab, doorPrefab, exitPrefab);
        AssetDatabase.SaveAssets();
        Debug.Log("FIRE_010 lava-split single-door Tilemap greybox built successfully.");
    }

    private static void BuildScene(TileBase terrainTile, TileBase hazardTile, TileBase hintTile,
        GameObject platePrefab, GameObject doorPrefab, GameObject exitPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        terrainTile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
        hazardTile = AssetDatabase.LoadAssetAtPath<Tile>(HazardTilePath);
        hintTile = AssetDatabase.LoadAssetAtPath<Tile>(HintTilePath);
        Require(terrainTile != null && hazardTile != null && hintTile != null,
            "FIRE_010 shared Tiles became unavailable while creating the Scene.");
        GameObject room = new("FIRE_010 Lava Gate Split");
        GameObject gridRoot = new("Grid");
        gridRoot.transform.SetParent(room.transform);
        gridRoot.AddComponent<Grid>();

        CreateTilemapLayer(gridRoot.transform, "Background");
        Tilemap terrain = CreateTilemapLayer(gridRoot.transform, "Terrain");
        ConfigureTerrain(terrain);
        CreateTilemapLayer(gridRoot.transform, "OneWayPlatform");
        CreateTilemapLayer(gridRoot.transform, "SpecialMirrorWall");
        Tilemap hazard = CreateTilemapLayer(gridRoot.transform, "Hazard");
        ConfigureHazard(hazard);
        Tilemap decoration = CreateTilemapLayer(gridRoot.transform, "Decoration");
        CreateTilemapLayer(gridRoot.transform, "Foreground");

        Fill(terrain, terrainTile, -13, -7, -3, -3);
        Fill(terrain, terrainTile, -4, 12, -3, -3);
        Fill(terrain, terrainTile, -13, 12, 6, 6);
        Fill(terrain, terrainTile, -13, -13, -2, 5);
        Fill(terrain, terrainTile, 12, 12, -2, 5);
        Fill(terrain, terrainTile, -10, -10, 0, 5);
        Fill(terrain, terrainTile, 8, 8, -2, 0);
        Fill(hazard, hazardTile, -6, -5, -3, -3);
        decoration.SetTile(new Vector3Int(0, -2, 0), hintTile);
        BakeGeometry(terrain, hazard);

        GameObject gameplay = new("Gameplay");
        gameplay.transform.SetParent(room.transform);
        GameObject dynamicObjects = new("DynamicObjects");
        dynamicObjects.transform.SetParent(gameplay.transform);
        GameObject entrances = new("Entrances");
        entrances.transform.SetParent(gameplay.transform);
        GameObject exits = new("Exits");
        exits.transform.SetParent(gameplay.transform);

        Transform entrance = Marker("Entrance-DEFAULT", new Vector3(0f, -1.08f, 0f), entrances.transform);
        PressurePlate2D plate = CreatePlate(platePrefab, dynamicObjects.transform);
        Door2D door = CreateDoor(doorPrefab, dynamicObjects.transform, plate);
        CreateExit(exitPrefab, exits.transform);
        CreateCamera();

        GameObject systems = new("RoomSystems");
        systems.transform.SetParent(room.transform);
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        BindRuntimeScript(reset, "Assets/Scripts/Core/RoomResetSystem.cs");
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, null, false);

        ValidateScene(scene, terrain, hazard, plate, door);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save FIRE_010 scene.");
        AddBuildScene(ScenePath);
    }

    private static PressurePlate2D CreatePlate(GameObject prefab, Transform parent)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Plate-A";
        instance.transform.SetParent(parent, false);
        instance.transform.position = new Vector3(7.25f, -1.7f, 0f);
        return instance.GetComponent<PressurePlate2D>();
    }

    private static Door2D CreateDoor(GameObject prefab, Transform parent, PressurePlate2D plate)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Door-A";
        instance.transform.SetParent(parent, false);
        instance.transform.position = new Vector3(-9.5f, -1.5f, 0f);
        Door2D door = instance.GetComponent<Door2D>();
        door.ConfigureControlSource(plate);
        door.SetState(Door2D.VisualState.Closed);
        EditorUtility.SetDirty(door);
        PrefabUtility.RecordPrefabInstancePropertyModifications(door);
        return door;
    }

    private static void CreateExit(GameObject prefab, Transform parent)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Exit-A to FIRE_011";
        instance.transform.SetParent(parent, false);
        instance.transform.position = new Vector3(-11.5f, -1f, 0f);
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

    private static void ConfigureHazard(Tilemap hazard)
    {
        Rigidbody2D body = hazard.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D composite = hazard.gameObject.AddComponent<CompositeCollider2D>();
        composite.isTrigger = true;
        TilemapCollider2D collider = hazard.gameObject.AddComponent<TilemapCollider2D>();
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        SurfaceSemantic2D semantic = hazard.gameObject.AddComponent<SurfaceSemantic2D>();
        BindRuntimeScript(semantic, "Assets/Scripts/Gameplay/SurfaceSemantic2D.cs");
        semantic.Configure(SurfaceSemantic2D.SurfaceType.Hazard, true, false);
        Hazard2D damage = hazard.gameObject.AddComponent<Hazard2D>();
        BindRuntimeScript(damage, "Assets/Scripts/Gameplay/Hazard2D.cs");
    }

    private static void BakeGeometry(params Tilemap[] maps)
    {
        foreach (Tilemap map in maps)
        {
            map.CompressBounds();
            map.RefreshAllTiles();
            map.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        }
        Physics2D.SyncTransforms();
        foreach (Tilemap map in maps)
        {
            map.GetComponent<CompositeCollider2D>().GenerateGeometry();
            Require(map.GetComponent<CompositeCollider2D>().pathCount > 0,
                $"{map.name} collider geometry was not generated.");
        }
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

    private static void ValidateScene(Scene scene, Tilemap terrain, Tilemap hazard,
        PressurePlate2D plate, Door2D door)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "FIRE_010 must not serialize a room-local Player.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "FIRE_010 must contain exactly one RoomPlayerSpawner2D.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PressurePlate2D>(true)).Count() == 1 &&
                roots.SelectMany(root => root.GetComponentsInChildren<Door2D>(true)).Count() == 1,
            "FIRE_010 must contain exactly one pressure plate and one door.");
        Require(door.ControlSource == plate, "Door-A must explicitly reference Plate-A.");
        Require(PrefabUtility.GetPrefabInstanceStatus(plate.gameObject) == PrefabInstanceStatus.Connected &&
                PrefabUtility.GetPrefabInstanceStatus(door.gameObject) == PrefabInstanceStatus.Connected,
            "FIRE_010 plate and door must remain connected to shared Prefabs.");
        Require(terrain.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.StaticSolid,
            "FIRE_010 Terrain must expose StaticSolid semantics.");
        Require(hazard.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.Hazard &&
                hazard.GetComponent<Hazard2D>() != null,
            "FIRE_010 lava must expose Hazard semantics and use Hazard2D.");
        Require(terrain.GetComponent<MirrorSurface2D>()?.kind == MirrorSurface2D.SurfaceKind.Ground,
            "FIRE_010 Terrain must be an approved ground mirror surface.");
        Require(door.GetComponent<BoxCollider2D>().size == new Vector2(1f, 2f),
            "FIRE_010 must use the standard two-tile Door2D size.");
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
