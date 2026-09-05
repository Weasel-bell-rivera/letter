using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using W1.Presentation;

/// <summary>
/// Builds a standalone fire greybox for tuning a two-jump moving-platform crossing.
/// This is intentionally a test Scene and does not claim a FIRE room number or map connection.
/// </summary>
public static class MovingLavaPlatformPrototypeBuilder
{
    public const string ScenePath = "Assets/Scenes/Tests/MovingLavaPlatformPrototype.unity";
    private const string TerrainTilePath = "Assets/Tiles/Graybox/Fire009Terrain.asset";
    private const string HazardTilePath = "Assets/Tiles/Graybox/Fire008Hazard.asset";
    private const string MovingPlatformPrefabPath =
        "Assets/Prefabs/Gameplay/Platforms/MovingPlatform2D.prefab";

    private const int LeftGroundMinX = -2;
    private const int LeftGroundMaxX = 22;
    private const int LavaMinX = 23;
    private const int LavaMaxX = 35;
    private const int RightGroundMinX = 36;
    private const int RightGroundMaxX = 47;
    private const int GroundY = -1;

    private const float PlatformAnchorX = 27.5f;
    private const float PlatformRootY = 1.5f;
    private const float PlatformTravel = 2.5f;
    private const float PlatformSpeed = 2f;
    private const float EndpointWait = .75f;

    private static readonly Rect CameraBounds = new(-2f, -4f, 50f, 14f);

    [MenuItem("Tools/W1/Build Moving Lava Platform Prototype")]
    public static void BuildFromMenu() => Build();

    public static void Build()
    {
        Tile terrainTile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
        Tile hazardTile = AssetDatabase.LoadAssetAtPath<Tile>(HazardTilePath);
        GameObject platformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MovingPlatformPrefabPath);
        Require(terrainTile != null, $"Missing terrain Tile: {TerrainTilePath}");
        Require(hazardTile != null, $"Missing lava Tile: {HazardTilePath}");
        Require(platformPrefab != null, $"Missing moving-platform Prefab: {MovingPlatformPrefabPath}");

        Scene previousActive = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            GameObject room = new("Moving Lava Platform Prototype");

            GameObject gridObject = Child(room.transform, "Grid");
            gridObject.AddComponent<Grid>().cellSize = Vector3.one;
            CreateTilemapLayer(gridObject.transform, "Background");
            Tilemap terrain = CreateTilemapLayer(gridObject.transform, "Terrain");
            ConfigureTerrain(terrain);
            CreateTilemapLayer(gridObject.transform, "OneWayPlatform");
            CreateTilemapLayer(gridObject.transform, "SpecialMirrorWall");
            Tilemap hazard = CreateTilemapLayer(gridObject.transform, "Hazard");
            ConfigureHazard(hazard);
            CreateTilemapLayer(gridObject.transform, "Decoration");
            CreateTilemapLayer(gridObject.transform, "Foreground");

            Fill(terrain, terrainTile, LeftGroundMinX, LeftGroundMaxX, GroundY, GroundY);
            Fill(terrain, terrainTile, RightGroundMinX, RightGroundMaxX, GroundY, GroundY);
            Fill(hazard, hazardTile, LavaMinX, LavaMaxX, GroundY - 1, GroundY);
            BakeGeometry(terrain, hazard);

            GameObject visuals = Child(room.transform, "Visuals");
            LowPolyLavaPreview lavaVisual = CreateLavaVisual(visuals.transform);

            GameObject gameplay = Child(room.transform, "Gameplay");
            GameObject dynamicObjects = Child(gameplay.transform, "DynamicObjects");
            MovingPlatform2D platform = CreatePlatform(platformPrefab, dynamicObjects.transform, scene);

            GameObject entrances = Child(gameplay.transform, "Entrances");
            Transform entrance = Marker("Entrance-DEFAULT", new Vector3(0f, .9f, 0f), entrances.transform);

            CameraFollow2D cameraFollow = CreateCamera();
            CreateGlobalLight();

            GameObject systems = Child(room.transform, "RoomSystems");
            RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
            PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, cameraFollow, true);

            Validate(scene, terrain, hazard, lavaVisual, platform, entrance, cameraFollow);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath), $"Failed to save {ScenePath}");
            AssetDatabase.SaveAssets();
            Debug.Log("Moving lava platform prototype built successfully.");
        }
        finally
        {
            if (previousActive.IsValid() && previousActive.isLoaded)
                SceneManager.SetActiveScene(previousActive);
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static MovingPlatform2D CreatePlatform(GameObject prefab, Transform parent, Scene scene)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "MovingPlatform-LavaCrossing";
        instance.transform.SetParent(parent, false);
        instance.transform.position = new Vector3(PlatformAnchorX, PlatformRootY, 0f);
        MovingPlatform2D platform = instance.GetComponent<MovingPlatform2D>();
        Require(platform != null, "Moving-platform Prefab is missing MovingPlatform2D.");
        platform.ConfigurePath(Vector2.zero, new Vector2(PlatformTravel, 0f),
            PlatformSpeed, EndpointWait, 0f, true, true);
        EditorUtility.SetDirty(platform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(platform);
        return platform;
    }

    private static LowPolyLavaPreview CreateLavaVisual(Transform parent)
    {
        GameObject visualObject = Child(parent, "Continuous Lava Visual Overlay");
        visualObject.transform.position = new Vector3((LavaMinX + LavaMaxX + 1) * .5f, 0f, 0f);
        LowPolyLavaPreview preview = visualObject.AddComponent<LowPolyLavaPreview>();

        SerializedObject serialized = new(preview);
        serialized.FindProperty("widthInTiles").intValue = LavaMaxX - LavaMinX + 1;
        serialized.FindProperty("tileSize").floatValue = 1f;
        serialized.FindProperty("depth").floatValue = 2f;
        serialized.FindProperty("surfaceMotion").floatValue = .1f;
        serialized.FindProperty("animationSpeed").floatValue = 1f;
        serialized.FindProperty("bodyOpacity").floatValue = 1f;
        serialized.FindProperty("showBackdrop").boolValue = false;
        serialized.FindProperty("showRockBanks").boolValue = false;
        serialized.FindProperty("bubbleCount").intValue = 9;
        serialized.FindProperty("flameParticleCount").intValue = 14;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return preview;
    }

    private static CameraFollow2D CreateCamera()
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(10.44f, 3f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.backgroundColor = new Color(.94f, .72f, .42f);
        cameraObject.AddComponent<AudioListener>();

        CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
        follow.Configure(null, false);
        follow.ConfigureDamping(.15f);
        follow.ConfigureFraming(new Vector2(0f, 2.1f));
        follow.ConfigureBounds(CameraBounds);
        follow.ConfigureEntryFramingBounds(CameraBounds);
        return follow;
    }

    private static void CreateGlobalLight()
    {
        GameObject lightObject = new("Global Light 2D");
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
    }

    private static Tilemap CreateTilemapLayer(Transform parent, string name)
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

    private static void ConfigureHazard(Tilemap hazard)
    {
        Rigidbody2D body = hazard.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D composite = hazard.gameObject.AddComponent<CompositeCollider2D>();
        composite.isTrigger = true;
        TilemapCollider2D collider = hazard.gameObject.AddComponent<TilemapCollider2D>();
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        hazard.GetComponent<TilemapRenderer>().enabled = false;
        hazard.gameObject.AddComponent<SurfaceSemantic2D>()
            .Configure(SurfaceSemantic2D.SurfaceType.Hazard, true, false);
        hazard.gameObject.AddComponent<Hazard2D>();
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

    private static void Validate(Scene scene, Tilemap terrain, Tilemap hazard,
        LowPolyLavaPreview lavaVisual, MovingPlatform2D platform, Transform entrance,
        CameraFollow2D cameraFollow)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "Prototype must not serialize a room-local Player.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "Prototype needs exactly one RoomPlayerSpawner2D.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomResetSystem>(true)).Count() == 1,
            "Prototype needs exactly one RoomResetSystem.");
        Require(PrefabUtility.GetPrefabInstanceStatus(platform.gameObject) == PrefabInstanceStatus.Connected,
            "Moving platform must remain connected to its shared Prefab.");
        Require(Mathf.Approximately(entrance.position.x, 0f) && LavaMinX == 23,
            "Spawn-to-lava-edge distance must remain twenty-three grid units.");
        Require(LavaMaxX - LavaMinX + 1 == 13, "Lava width must remain thirteen grid units.");
        Require(RightGroundMaxX - RightGroundMinX + 1 == 12,
            "Right shore must remain twelve grid units wide.");
        Require(Mathf.Approximately(PlatformRootY + .5f, 2f),
            "Platform top must remain two units above the shore surface.");
        Require(platform.StartOffset == Vector2.zero &&
                platform.EndOffset == new Vector2(PlatformTravel, 0f),
            "Moving-platform path does not match the tuned endpoints.");
        Require(terrain.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.StaticSolid,
            "Terrain must expose StaticSolid semantics.");
        Require(hazard.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.Hazard &&
                hazard.GetComponent<Hazard2D>() != null,
            "Lava must expose Hazard semantics and use Hazard2D.");
        Require(!hazard.GetComponent<TilemapRenderer>().enabled,
            "The gameplay lava Tilemap renderer must remain hidden behind the continuous visual.");
        Require(lavaVisual != null && lavaVisual.GetComponent<Collider2D>() == null,
            "Low-poly lava must remain presentation-only and must not duplicate gameplay collision.");
        SerializedObject lavaSettings = new(lavaVisual);
        Require(lavaSettings.FindProperty("widthInTiles").intValue == 13 &&
                Mathf.Approximately(lavaSettings.FindProperty("depth").floatValue, 2f) &&
                !lavaSettings.FindProperty("showBackdrop").boolValue &&
                !lavaSettings.FindProperty("showRockBanks").boolValue,
            "Low-poly lava visual is not aligned to the thirteen-cell gameplay hazard.");
        Require(Shader.Find("W1/Low Poly Fire Preview") != null,
            "Low-poly lava Shader is unavailable.");
        Require(cameraFollow.UsesRoomBounds && cameraFollow.RoomBounds == CameraBounds,
            "Camera must use the explicit prototype bounds.");
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

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
