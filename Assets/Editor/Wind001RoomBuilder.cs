using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds the approved WIND_001 wind-ray teaching greybox.
/// Room-specific gameplay behaviour is intentionally absent.
/// </summary>
public static class Wind001RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Wind/Wind_001.unity";
    public const string TerrainTilePath = "Assets/Tiles/Wind/Wind001TerrainGraybox.asset";
    public const string HazardTilePath = "Assets/Tiles/Wind/Wind001FallHazardGraybox.asset";
    public const string WindRayPrefabPath = WindRayEnemyBuilder.PrefabPath;
    public const int TerrainMinX = -14;
    public const int TerrainMaxX = 13;
    public const int TerrainY = -3;
    public const int HazardMinX = -16;
    public const int HazardMaxX = 15;
    public const int HazardY = -7;
    public static readonly Vector3 EntrancePosition = new(-5f, -1.08f, 0f);
    public static readonly Vector3 WindRayPosition = new(6f, 2f, 0f);

    private const string TerrainTexturePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_stone_cloud_middle.png";

    [MenuItem("Tools/W1/Build WIND-001 Wind Ray Greybox")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    public static void BuildFromCommandLine()
    {
        Directory.CreateDirectory("Assets/Scenes/Levels/Wind");
        Directory.CreateDirectory("Assets/Tiles/Wind");
        WindRayEnemyBuilder.BuildAssets();

        Sprite terrainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TerrainTexturePath);
        Require(terrainSprite != null, $"Missing terrain Sprite: {TerrainTexturePath}");
        Tile terrainTile = CreateOrUpdateTile(TerrainTilePath, terrainSprite,
            new Color(.72f, .9f, .86f, 1f), Tile.ColliderType.Grid);
        Tile hazardTile = CreateOrUpdateTile(HazardTilePath, terrainSprite,
            new Color(.92f, .2f, .35f, .9f), Tile.ColliderType.Grid);

        GameObject windRayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindRayPrefabPath);
        Require(windRayPrefab != null, $"Missing wind ray Prefab: {WindRayPrefabPath}");

        BuildScene(terrainTile, hazardTile, windRayPrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("WIND_001 wind-ray teaching greybox built successfully.");
    }

    private static void BuildScene(TileBase terrainTile, TileBase hazardTile, GameObject windRayPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = new("WIND_001 Wind Ray Teaching Greybox");

        GameObject gridRoot = new("Grid");
        gridRoot.transform.SetParent(room.transform);
        gridRoot.AddComponent<Grid>().cellSize = Vector3.one;
        CreateTilemapLayer(gridRoot.transform, "Background");
        Tilemap terrain = CreateTilemapLayer(gridRoot.transform, "Terrain");
        CreateTilemapLayer(gridRoot.transform, "OneWayPlatform");
        CreateTilemapLayer(gridRoot.transform, "SpecialMirrorWall");
        Tilemap hazard = CreateTilemapLayer(gridRoot.transform, "Hazard");
        CreateTilemapLayer(gridRoot.transform, "Decoration");
        CreateTilemapLayer(gridRoot.transform, "Foreground");

        ConfigureTerrain(terrain);
        ConfigureHazard(hazard);
        Fill(terrain, terrainTile, TerrainMinX, TerrainMaxX, TerrainY);
        Fill(hazard, hazardTile, HazardMinX, HazardMaxX, HazardY);
        BakeTilemapGeometry(terrain, hazard);

        GameObject gameplay = new("Gameplay");
        gameplay.transform.SetParent(room.transform);
        GameObject dynamicObjects = new("DynamicObjects");
        dynamicObjects.transform.SetParent(gameplay.transform);
        CreateWindRay(windRayPrefab, dynamicObjects.transform, scene);

        GameObject entrances = new("Entrances");
        entrances.transform.SetParent(gameplay.transform);
        Transform entrance = Marker("PrototypeEntrance", EntrancePosition, entrances.transform);

        GameObject goals = new("Goals");
        goals.transform.SetParent(gameplay.transform);
        SpriteRenderer goal = Visual("PrototypeGoal", goals.transform, new Vector2(.35f, 2.2f),
            new Color(.3f, 1f, .55f, .75f), 1);
        goal.transform.position = new Vector3(11f, -1.4f, 0f);

        Camera camera = CreateCamera(entrance.position);
        CameraFollow2D cameraFollow = camera.gameObject.AddComponent<CameraFollow2D>();
        BindRuntimeScript(cameraFollow, "Assets/Scripts/Gameplay/CameraFollow2D.cs");
        cameraFollow.Configure(null, true);

        GameObject systems = new("RoomSystems");
        systems.transform.SetParent(room.transform);
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        BindRuntimeScript(reset, "Assets/Scripts/Core/RoomResetSystem.cs");
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, cameraFollow);

        ValidateSceneOrThrow(scene, terrain, hazard);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save WIND_001 scene.");
        AddBuildScene(ScenePath);
    }

    private static Tile CreateOrUpdateTile(string path, Sprite sprite, Color color, Tile.ColliderType colliderType)
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

    private static Tilemap CreateTilemapLayer(Transform parent, string name)
    {
        GameObject layer = new(name);
        layer.transform.SetParent(parent, false);
        Tilemap tilemap = layer.AddComponent<Tilemap>();
        layer.AddComponent<TilemapRenderer>();
        return tilemap;
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
        MirrorSurface2D mirrorSurface = terrain.gameObject.AddComponent<MirrorSurface2D>();
        BindRuntimeScript(mirrorSurface, "Assets/Scripts/Gameplay/MirrorSurface2D.cs");
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

        SurfaceSemantic2D semantic = hazard.gameObject.AddComponent<SurfaceSemantic2D>();
        BindRuntimeScript(semantic, "Assets/Scripts/Gameplay/SurfaceSemantic2D.cs");
        semantic.Configure(SurfaceSemantic2D.SurfaceType.Hazard, true, false);
        Hazard2D damage = hazard.gameObject.AddComponent<Hazard2D>();
        BindRuntimeScript(damage, "Assets/Scripts/Gameplay/Hazard2D.cs");
    }

    private static void Fill(Tilemap map, TileBase tile, int minX, int maxX, int y)
    {
        int width = maxX - minX + 1;
        BoundsInt bounds = new(minX, y, 0, width, 1, 1);
        TileBase[] tiles = Enumerable.Repeat(tile, width).ToArray();
        map.SetTilesBlock(bounds, tiles);
    }

    private static void BakeTilemapGeometry(params Tilemap[] maps)
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
            CompositeCollider2D composite = map.GetComponent<CompositeCollider2D>();
            composite.GenerateGeometry();
            Require(composite.pathCount > 0, $"{map.name} must bake valid composite geometry.");
        }
    }

    private static void CreateWindRay(GameObject prefab, Transform parent, Scene scene)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "WindRay-UpperRight";
        instance.transform.SetParent(parent, false);
        instance.transform.position = WindRayPosition;
        WindRayEnemy2D enemy = instance.GetComponent<WindRayEnemy2D>();
        Require(enemy != null, "Wind ray Prefab must contain WindRayEnemy2D.");
        enemy.SetInitialVisualFacing(new Vector2(-1f, -1f));
        EditorUtility.SetDirty(enemy);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
    }

    private static Camera CreateCamera(Vector3 playerPosition)
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(playerPosition.x, playerPosition.y, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7.5f;
        camera.backgroundColor = new Color(.55f, .78f, .9f);
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private static SpriteRenderer Visual(string name, Transform parent, Vector2 worldSize, Color color, int order)
    {
        GameObject visual = new(name);
        visual.transform.SetParent(parent, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        renderer.color = color;
        renderer.sortingOrder = order;
        Vector2 native = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(worldSize.x / native.x, worldSize.y / native.y, 1f);
        return renderer;
    }

    private static Transform Marker(string name, Vector3 position, Transform parent)
    {
        GameObject marker = new(name);
        marker.transform.SetParent(parent);
        marker.transform.position = position;
        return marker.transform;
    }

    private static void ValidateSceneOrThrow(Scene scene, Tilemap terrain, Tilemap hazard)
    {
        for (int x = TerrainMinX; x <= TerrainMaxX; x++)
            Require(terrain.HasTile(new Vector3Int(x, TerrainY, 0)), $"Terrain gap at x={x}.");
        for (int x = HazardMinX; x <= HazardMaxX; x++)
            Require(hazard.HasTile(new Vector3Int(x, HazardY, 0)), $"Hazard gap at x={x}.");

        SurfaceSemantic2D terrainSemantic = terrain.GetComponent<SurfaceSemantic2D>();
        Require(terrainSemantic != null && terrainSemantic.Type == SurfaceSemantic2D.SurfaceType.StaticSolid &&
                terrainSemantic.IsStatic && terrainSemantic.IsSafe,
            "Terrain must expose the explicit static and safe StaticSolid semantic.");
        Require(terrain.GetComponent<MirrorSurface2D>()?.kind == MirrorSurface2D.SurfaceKind.Ground,
            "Terrain must be an approved ground mirror surface.");
        Require(hazard.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.Hazard,
            "Fall catcher must expose Hazard semantics.");
        Require(hazard.GetComponent<Hazard2D>() != null, "Fall catcher must use the shared Hazard2D component.");

        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "WIND_001 must not serialize a room-local Player.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "WIND_001 must contain exactly one RoomPlayerSpawner2D.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomResetSystem>(true)).Count() == 1,
            "WIND_001 must contain exactly one RoomResetSystem.");
        WindRayEnemy2D enemy = roots.SelectMany(root =>
            root.GetComponentsInChildren<WindRayEnemy2D>(true)).Single();
        Require(PrefabUtility.GetPrefabInstanceStatus(enemy.gameObject) == PrefabInstanceStatus.Connected,
            "WIND_001 wind ray must remain connected to its shared Prefab.");
        Require(Vector2.Distance(enemy.transform.position, WindRayPosition) < .001f,
            "WIND_001 wind ray must be placed at the approved upper-right guard point.");
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
