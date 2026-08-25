using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds the SNOW_003 FrozenGround stopping-distance teaching greybox.</summary>
public static class Snow003RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Snow/Snow_003.unity";
    public const string TerrainTilePath = "Assets/Tiles/Snow/SnowTerrainGraybox.asset";
    public const string FrozenGroundTilePath = "Assets/Tiles/Snow/FrozenGroundSnowBlock.asset";
    public const string FrozenGroundMaterialPath = "Assets/Settings/Physics/FrozenGround.physicsMaterial2D";
    public const string ExitPrefabPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";

    [MenuItem("Tools/W1/Build SNOW-003 Ice Introduction Greybox")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    public static void BuildFromCommandLine()
    {
        Tile terrainTile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
        Tile frozenTile = AssetDatabase.LoadAssetAtPath<Tile>(FrozenGroundTilePath);
        PhysicsMaterial2D frozenMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(FrozenGroundMaterialPath);
        GameObject exitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExitPrefabPath);
        Require(terrainTile != null && frozenTile != null && frozenMaterial != null && exitPrefab != null,
            "SNOW_003 shared Tile, material, or exit Prefab dependency is missing.");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = new("SNOW_003 Ice Introduction");
        GameObject gridRoot = new("Grid");
        gridRoot.transform.SetParent(room.transform);
        gridRoot.AddComponent<Grid>();

        CreateTilemapLayer(gridRoot.transform, "Background");
        Tilemap terrain = CreateTilemapLayer(gridRoot.transform, "Terrain");
        ConfigureSurface(terrain, SurfaceSemantic2D.SurfaceType.StaticSolid, null);
        Tilemap frozenGround = CreateTilemapLayer(gridRoot.transform, "FrozenGround");
        ConfigureSurface(frozenGround, SurfaceSemantic2D.SurfaceType.FrozenGround, frozenMaterial);
        CreateTilemapLayer(gridRoot.transform, "FreezingGround");
        CreateTilemapLayer(gridRoot.transform, "OneWayPlatform");
        CreateTilemapLayer(gridRoot.transform, "SpecialMirrorWall");
        CreateTilemapLayer(gridRoot.transform, "Hazard");
        CreateTilemapLayer(gridRoot.transform, "Decoration");
        CreateTilemapLayer(gridRoot.transform, "Foreground");

        Fill(terrain, terrainTile, -13, -5, -3, -3);
        Fill(frozenGround, frozenTile, -4, 6, -3, -3);
        Fill(terrain, terrainTile, 7, 12, -3, -3);
        Fill(terrain, terrainTile, 7, 7, -2, -1);
        Fill(terrain, terrainTile, -13, -13, -2, 5);
        Fill(terrain, terrainTile, 12, 12, -2, 5);
        Fill(terrain, terrainTile, -13, 12, 6, 6);
        Bake(terrain);
        Bake(frozenGround);

        GameObject gameplay = new("Gameplay");
        gameplay.transform.SetParent(room.transform);
        new GameObject("DynamicObjects").transform.SetParent(gameplay.transform);
        GameObject entrances = new("Entrances");
        entrances.transform.SetParent(gameplay.transform);
        GameObject exits = new("Exits");
        exits.transform.SetParent(gameplay.transform);

        Transform entrance = Marker("Entrance-DEFAULT from SNOW_002", new Vector3(-9f, -1.08f, 0f), entrances.transform);
        CreateExit(exitPrefab, exits.transform, "Exit-Up to SNOW_002", new Vector3(-11.5f, -2f, 0f), "Snow_002");
        CreateExit(exitPrefab, exits.transform, "Exit-Down to SNOW_004", new Vector3(10f, -2f, 0f), "Snow_004");
        CreateCamera();

        GameObject systems = new("RoomSystems");
        systems.transform.SetParent(room.transform);
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        BindRuntimeScript(reset, "Assets/Scripts/Core/RoomResetSystem.cs");
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, null, false);

        Validate(scene, terrain, frozenGround);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save SNOW_003 scene.");
        AddBuildScene(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("SNOW_003 Ice Introduction Tilemap greybox built successfully.");
    }

    private static Tilemap CreateTilemapLayer(Transform parent, string name)
    {
        GameObject layer = new(name);
        layer.transform.SetParent(parent, false);
        Tilemap map = layer.AddComponent<Tilemap>();
        layer.AddComponent<TilemapRenderer>();
        return map;
    }

    private static void ConfigureSurface(Tilemap map, SurfaceSemantic2D.SurfaceType type,
        PhysicsMaterial2D material)
    {
        Rigidbody2D body = map.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D composite = map.gameObject.AddComponent<CompositeCollider2D>();
        composite.sharedMaterial = material;
        TilemapCollider2D collider = map.gameObject.AddComponent<TilemapCollider2D>();
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        collider.sharedMaterial = material;
        SurfaceSemantic2D semantic = map.gameObject.AddComponent<SurfaceSemantic2D>();
        BindRuntimeScript(semantic, "Assets/Scripts/Gameplay/SurfaceSemantic2D.cs");
        semantic.Configure(type, true, true);
        MirrorSurface2D mirrorSurface = map.gameObject.AddComponent<MirrorSurface2D>();
        BindRuntimeScript(mirrorSurface, "Assets/Scripts/Gameplay/MirrorSurface2D.cs");
        mirrorSurface.kind = MirrorSurface2D.SurfaceKind.Ground;
        mirrorSurface.safe = true;
    }

    private static void CreateExit(GameObject prefab, Transform parent, string name, Vector3 position,
        string targetScene)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        RoomExit2D exit = instance.GetComponent<RoomExit2D>();
        Require(exit != null, "RoomExit2D Prefab is missing its runtime component.");
        exit.Configure(targetScene, "DEFAULT");
        EditorUtility.SetDirty(exit);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
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
        camera.backgroundColor = new Color(.68f, .84f, .94f);
        cameraObject.AddComponent<AudioListener>();
    }

    private static Transform Marker(string name, Vector3 position, Transform parent)
    {
        GameObject marker = new(name);
        marker.transform.SetParent(parent, false);
        marker.transform.position = position;
        return marker.transform;
    }

    private static void Fill(Tilemap map, TileBase tile, int minX, int maxX, int minY, int maxY)
    {
        int width = maxX - minX + 1;
        int height = maxY - minY + 1;
        BoundsInt bounds = new(minX, minY, 0, width, height, 1);
        map.SetTilesBlock(bounds, Enumerable.Repeat(tile, width * height).ToArray());
    }

    private static void Bake(Tilemap map)
    {
        map.CompressBounds();
        map.RefreshAllTiles();
        map.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        Physics2D.SyncTransforms();
        map.GetComponent<CompositeCollider2D>().GenerateGeometry();
    }

    private static void Validate(Scene scene, Tilemap terrain, Tilemap frozenGround)
    {
        for (int x = -4; x <= 6; x++)
            Require(frozenGround.HasTile(new Vector3Int(x, -3, 0)), $"FrozenGround gap at x={x}.");
        SurfaceSemantic2D frozenSemantic = frozenGround.GetComponent<SurfaceSemantic2D>();
        Require(frozenSemantic != null && frozenSemantic.Type == SurfaceSemantic2D.SurfaceType.FrozenGround &&
                frozenSemantic.IsStatic && frozenSemantic.IsSafe,
            "SNOW_003 ice must expose safe static FrozenGround semantics.");
        Require(frozenGround.GetComponent<CompositeCollider2D>().sharedMaterial != null,
            "SNOW_003 FrozenGround must use the shared zero-friction material.");
        Require(terrain.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.StaticSolid,
            "SNOW_003 Terrain must expose StaticSolid semantics.");

        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "SNOW_003 must not serialize a room-local Player.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "SNOW_003 must contain exactly one RoomPlayerSpawner2D.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomEntrance2D>(true)).Count() == 1,
            "SNOW_003 must contain exactly one default entrance.");
        RoomExit2D[] exits = roots.SelectMany(root => root.GetComponentsInChildren<RoomExit2D>(true)).ToArray();
        Require(exits.Length == 2 && exits.All(exit => PrefabUtility.GetPrefabInstanceStatus(exit.gameObject) ==
                PrefabInstanceStatus.Connected),
            "SNOW_003 must contain two connected shared RoomExit2D Prefab instances.");
        Camera camera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Single();
        Require(camera.orthographic && Mathf.Approximately(camera.orthographicSize, 7f),
            "SNOW_003 must use the approved fixed single-screen camera.");
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
