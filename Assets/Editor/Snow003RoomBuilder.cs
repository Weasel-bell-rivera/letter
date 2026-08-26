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
    public static readonly Rect CameraBounds = new(-13f, -7f, 26f, 14f);
    public const float CameraOrthographicSize = 7f;
    public const float CameraSmoothTime = .15f;
    public const string ScenePath = "Assets/Scenes/Levels/Snow/Snow_003.unity";
    public const string TerrainTilePath = "Assets/Tiles/Snow/SnowTerrainGraybox.asset";
    public const string FrozenGroundTilePath = "Assets/Tiles/Snow/FrozenGroundSnowBlock.asset";
    public const string FrozenGroundMaterialPath = "Assets/Settings/Physics/FrozenGround.physicsMaterial2D";
    public const string ExitPrefabPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";

    [MenuItem("Tools/W1/Build SNOW-003 Ice Introduction Greybox")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    [MenuItem("Tools/W1/Upgrade SNOW-003 Expanded Ice Lesson")]
    public static void UpgradeExpandedIceLesson()
    {
        Tile terrainTile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
        Tile frozenTile = AssetDatabase.LoadAssetAtPath<Tile>(FrozenGroundTilePath);
        Require(terrainTile != null && frozenTile != null, "SNOW_003 Tile dependencies are missing.");

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForUpgrade = !scene.IsValid() || !scene.isLoaded;
        if (openedForUpgrade) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        try
        {
            GameObject[] roots = scene.GetRootGameObjects();
            Tilemap terrain = roots.SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
                .Single(map => map.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.StaticSolid);
            Tilemap frozenGround = roots.SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
                .Single(map => map.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.FrozenGround);

            Fill(terrain, terrainTile, 9, 11, -1, -1);
            Fill(terrain, terrainTile, 5, 8, 1, 1);
            Fill(frozenGround, frozenTile, -5, 4, 1, 1);
            Fill(terrain, terrainTile, -6, -6, 2, 3);
            Fill(terrain, terrainTile, -10, -7, 1, 1);

            RoomEntrance2D returnEntrance = roots.SelectMany(root =>
                    root.GetComponentsInChildren<RoomEntrance2D>(true))
                .Single(value => value.EntranceId == "FROM_SNOW_004");
            returnEntrance.transform.position = new Vector3(-9.75f, 2.92f, 0f);
            returnEntrance.Configure("FROM_SNOW_004", false, true);
            EditorUtility.SetDirty(returnEntrance);

            RoomExit2D forwardExit = roots.SelectMany(root => root.GetComponentsInChildren<RoomExit2D>(true))
                .Single(value => value.TargetScene == "Snow_004");
            forwardExit.transform.position = new Vector3(-8f, 2f, 0f);
            EditorUtility.SetDirty(forwardExit);
            PrefabUtility.RecordPrefabInstancePropertyModifications(forwardExit.transform);

            Bake(terrain);
            Bake(frozenGround);
            Validate(scene, terrain, frozenGround);
            for (int x = -5; x <= 4; x++)
                Require(frozenGround.HasTile(new Vector3Int(x, 1, 0)), $"Upper FrozenGround gap at x={x}.");
            Require(Vector3.Distance(returnEntrance.transform.position, forwardExit.transform.position) > 1.5f,
                "SNOW_003 return entrance must not overlap the forward exit trigger.");

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene), "Failed to save upgraded SNOW_003 scene.");
            AssetDatabase.SaveAssets();
            Debug.Log("SNOW_003 expanded two-direction ice lesson upgraded successfully.");
        }
        finally
        {
            if (openedForUpgrade && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

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
        Fill(terrain, terrainTile, 9, 11, -1, -1);
        Fill(terrain, terrainTile, 5, 8, 1, 1);
        Fill(frozenGround, frozenTile, -5, 4, 1, 1);
        Fill(terrain, terrainTile, -6, -6, 2, 3);
        Fill(terrain, terrainTile, -10, -7, 1, 1);
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
        Transform returnEntrance = Marker("Entrance-FROM_SNOW_004", new Vector3(-9.75f, 2.92f, 0f), entrances.transform);
        PlayerRoomAuthoring.ConfigureEntrance(returnEntrance, "FROM_SNOW_004", false, true);
        CreateExit(exitPrefab, exits.transform, "Exit-Up to SNOW_002", new Vector3(-11.5f, -2f, 0f), "Snow_002");
        CreateExit(exitPrefab, exits.transform, "Exit-Down to SNOW_004", new Vector3(-8f, 2f, 0f), "Snow_004", "FROM_SNOW_003");
        CameraFollow2D cameraFollow = CreateCamera();

        GameObject systems = new("RoomSystems");
        systems.transform.SetParent(room.transform);
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        BindRuntimeScript(reset, "Assets/Scripts/Core/RoomResetSystem.cs");
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, cameraFollow, true);

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
        string targetScene, string targetEntrance = "DEFAULT")
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        RoomExit2D exit = instance.GetComponent<RoomExit2D>();
        Require(exit != null, "RoomExit2D Prefab is missing its runtime component.");
        exit.Configure(targetScene, targetEntrance);
        EditorUtility.SetDirty(exit);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(exit);
    }

    private static CameraFollow2D CreateCamera()
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = CameraOrthographicSize;
        camera.backgroundColor = new Color(.68f, .84f, .94f);
        cameraObject.AddComponent<AudioListener>();
        CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
        BindRuntimeScript(follow, "Assets/Scripts/Gameplay/CameraFollow2D.cs");
        follow.Configure(null, true);
        follow.ConfigureDamping(CameraSmoothTime);
        follow.ConfigureBounds(CameraBounds);
        return follow;
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
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomEntrance2D>(true)).Count() == 2,
            "SNOW_003 must contain exactly two room entrances.");
        RoomEntrance2D[] entrances = roots.SelectMany(root => root.GetComponentsInChildren<RoomEntrance2D>(true)).ToArray();
        Require(entrances.Count(value => value.IsDefault) == 1 &&
                entrances.Any(value => value.EntranceId == "FROM_SNOW_004" && value.FacingRight),
            "SNOW_003 must contain DEFAULT and right-facing FROM_SNOW_004 entrances.");
        RoomExit2D[] exits = roots.SelectMany(root => root.GetComponentsInChildren<RoomExit2D>(true)).ToArray();
        Require(exits.Length == 2 && exits.All(exit => PrefabUtility.GetPrefabInstanceStatus(exit.gameObject) ==
                PrefabInstanceStatus.Connected),
            "SNOW_003 must contain two connected shared RoomExit2D Prefab instances.");
        Camera camera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Single();
        CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
        Require(camera.orthographic && Mathf.Approximately(camera.orthographicSize, CameraOrthographicSize) &&
                follow != null && follow.FollowsVertical && follow.UsesRoomBounds &&
                follow.RoomBounds == CameraBounds && Mathf.Approximately(follow.SmoothTime, CameraSmoothTime),
            "SNOW_003 must use the approved bounded Player-follow camera.");
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
