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
/// Builds the approved EARTH_001 vertical wall patrol observation greybox.
/// Room-specific gameplay behaviour is intentionally absent.
/// </summary>
public static class Earth001RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Earth/Earth_001.unity";
    public const string TerrainTilePath = "Assets/Tiles/Earth/Earth001TerrainGraybox.asset";
    public const string EnemyPrefabPath =
        "Assets/Prefabs/Gameplay/Enemies/VerticalWallPatrolEnemy2D.prefab";

    public const int GroundMinX = -13;
    public const int GroundMaxX = 12;
    public const int GroundY = -4;
    public const int WallX = -6;
    public const int WallMinY = -3;
    public const int WallMaxY = 5;

    public static readonly Vector3 EntrancePosition = new(8f, -2.08f, 0f);
    public static readonly Vector3 EnemyPosition = new(-4.54f, 1f, 0f);
    public static readonly Vector3 CameraPosition = new(0f, 2f, -10f);
    public static readonly Rect CameraBounds = new(-13f, -5f, 26f, 14f);

    private const string TerrainTexturePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_sand_block_center.png";

    [MenuItem("Tools/W1/Build EARTH-001 Vertical Wall Patrol Greybox")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    public static void BuildFromCommandLine()
    {
        Directory.CreateDirectory("Assets/Scenes/Levels/Earth");
        Directory.CreateDirectory("Assets/Tiles/Earth");

        Sprite terrainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TerrainTexturePath);
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);

        Require(terrainSprite != null, $"Missing terrain Sprite: {TerrainTexturePath}");
        Require(enemyPrefab != null, $"Missing vertical wall patrol Prefab: {EnemyPrefabPath}");

        Tile terrainTile = CreateOrUpdateTerrainTile(terrainSprite);
        BuildScene(terrainTile, enemyPrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("EARTH_001 vertical wall patrol observation greybox built successfully.");
    }

    private static Tile CreateOrUpdateTerrainTile(Sprite sprite)
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, TerrainTilePath);
        }

        tile.name = Path.GetFileNameWithoutExtension(TerrainTilePath);
        tile.sprite = sprite;
        tile.color = new Color(.78f, .62f, .42f, 1f);
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void BuildScene(TileBase terrainTile, GameObject enemyPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = new("EARTH_001 Wall Patrol Observation Greybox");

        GameObject gridRoot = new("Grid");
        gridRoot.transform.SetParent(room.transform);
        gridRoot.AddComponent<Grid>().cellSize = Vector3.one;
        CreateTilemapLayer(gridRoot.transform, "Background");
        Tilemap terrain = CreateTilemapLayer(gridRoot.transform, "Terrain");
        CreateTilemapLayer(gridRoot.transform, "FrozenGround");
        CreateTilemapLayer(gridRoot.transform, "OneWayPlatform");
        CreateTilemapLayer(gridRoot.transform, "SpecialMirrorWall");
        CreateTilemapLayer(gridRoot.transform, "Hazard");
        CreateTilemapLayer(gridRoot.transform, "Decoration");
        CreateTilemapLayer(gridRoot.transform, "Foreground");

        ConfigureTerrain(terrain);
        FillHorizontal(terrain, terrainTile, GroundMinX, GroundMaxX, GroundY);
        FillVertical(terrain, terrainTile, WallX, WallMinY, WallMaxY);
        BakeTilemapGeometry(terrain);

        GameObject gameplay = new("Gameplay");
        gameplay.transform.SetParent(room.transform);
        GameObject dynamicObjects = new("DynamicObjects");
        dynamicObjects.transform.SetParent(gameplay.transform);
        CreateEnemy(enemyPrefab, dynamicObjects.transform, scene);

        GameObject entrances = new("Entrances");
        entrances.transform.SetParent(gameplay.transform);
        Transform entrance = Marker("PrototypeEntrance", EntrancePosition, entrances.transform);
        Camera camera = CreateFixedCamera();
        CameraFollow2D cameraController = camera.gameObject.AddComponent<CameraFollow2D>();
        BindRuntimeScript(cameraController, "Assets/Scripts/Gameplay/CameraFollow2D.cs");
        cameraController.Configure(null, false);
        cameraController.ConfigureBounds(CameraBounds);
        cameraController.enabled = false;

        GameObject systems = new("RoomSystems");
        systems.transform.SetParent(room.transform);
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        BindRuntimeScript(reset, "Assets/Scripts/Core/RoomResetSystem.cs");
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, cameraController, false);

        ValidateSceneOrThrow(scene, terrain, camera, cameraController);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save EARTH_001 scene.");
        AddBuildScene(ScenePath);
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

    private static void FillHorizontal(Tilemap map, TileBase tile, int minX, int maxX, int y)
    {
        int width = maxX - minX + 1;
        BoundsInt bounds = new(minX, y, 0, width, 1, 1);
        map.SetTilesBlock(bounds, Enumerable.Repeat(tile, width).ToArray());
    }

    private static void FillVertical(Tilemap map, TileBase tile, int x, int minY, int maxY)
    {
        for (int y = minY; y <= maxY; y++) map.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private static void BakeTilemapGeometry(Tilemap map)
    {
        map.CompressBounds();
        map.RefreshAllTiles();
        map.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        Physics2D.SyncTransforms();
        CompositeCollider2D composite = map.GetComponent<CompositeCollider2D>();
        composite.GenerateGeometry();
        Require(composite.pathCount > 0, "EARTH_001 Terrain must bake valid composite geometry.");
    }

    private static void CreateEnemy(GameObject prefab, Transform parent, Scene scene)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "Enemy-A";
        instance.transform.SetParent(parent, false);
        instance.transform.position = EnemyPosition;

        VerticalWallPatrolEnemy2D enemy = instance.GetComponent<VerticalWallPatrolEnemy2D>();
        Require(enemy != null, "Vertical wall patrol Prefab must contain VerticalWallPatrolEnemy2D.");
        enemy.ConfigurePatrol(-2f, 2f, 1.5f, .3f,
            VerticalWallPatrolEnemy2D.WallSide.Left, true);
        EditorUtility.SetDirty(enemy);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
    }

    private static Camera CreateFixedCamera()
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = CameraPosition;
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.backgroundColor = new Color(.53f, .43f, .32f);
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private static Transform Marker(string name, Vector3 position, Transform parent)
    {
        GameObject marker = new(name);
        marker.transform.SetParent(parent);
        marker.transform.position = position;
        return marker.transform;
    }

    private static void ValidateSceneOrThrow(Scene scene, Tilemap terrain, Camera camera,
        CameraFollow2D cameraController)
    {
        for (int x = GroundMinX; x <= GroundMaxX; x++)
            Require(terrain.HasTile(new Vector3Int(x, GroundY, 0)), $"Terrain ground gap at x={x}.");
        for (int y = WallMinY; y <= WallMaxY; y++)
            Require(terrain.HasTile(new Vector3Int(WallX, y, 0)), $"Terrain wall gap at y={y}.");

        SurfaceSemantic2D semantic = terrain.GetComponent<SurfaceSemantic2D>();
        Require(semantic != null && semantic.Type == SurfaceSemantic2D.SurfaceType.StaticSolid &&
                semantic.IsStatic && semantic.IsSafe,
            "EARTH_001 Terrain must expose static and safe StaticSolid semantics.");
        Require(terrain.GetComponent<MirrorSurface2D>()?.kind == MirrorSurface2D.SurfaceKind.Ground,
            "EARTH_001 Terrain must expose the approved ground mirror surface.");

        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "EARTH_001 must not serialize a room-local Player.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "EARTH_001 must contain exactly one RoomPlayerSpawner2D.");
        RoomEntrance2D entrance = roots.SelectMany(root =>
            root.GetComponentsInChildren<RoomEntrance2D>(true)).Single();
        Require(entrance.IsDefault && entrance.EntranceId == SaveIds.DefaultEntrance &&
                !entrance.FacingRight && Vector2.Distance(entrance.transform.position, EntrancePosition) < .001f,
            "EARTH_001 must contain one approved left-facing DEFAULT entrance.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomResetSystem>(true)).Count() == 1,
            "EARTH_001 must contain exactly one RoomResetSystem.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomExit2D>(true)).Count() == 0,
            "EARTH_001 approved observation greybox must not contain a formal exit.");

        VerticalWallPatrolEnemy2D enemy = roots.SelectMany(root =>
            root.GetComponentsInChildren<VerticalWallPatrolEnemy2D>(true)).Single();
        Require(PrefabUtility.GetPrefabInstanceStatus(enemy.gameObject) == PrefabInstanceStatus.Connected,
            "EARTH_001 Enemy-A must remain connected to its shared Prefab.");
        Require(Vector2.Distance(enemy.transform.position, EnemyPosition) < .001f,
            "EARTH_001 Enemy-A must use the approved position.");
        Require(enemy.AttachedWallSide == VerticalWallPatrolEnemy2D.WallSide.Left &&
                Mathf.Approximately(enemy.LowerEndpoint, -2f) && Mathf.Approximately(enemy.UpperEndpoint, 2f) &&
                Mathf.Approximately(enemy.MoveSpeed, 1.5f) && Mathf.Approximately(enemy.EndpointWait, .3f) &&
                enemy.InitiallyMovingUp,
            "EARTH_001 Enemy-A must use the approved patrol configuration.");

        Require(camera.orthographic && Mathf.Approximately(camera.orthographicSize, 7f) &&
                Vector3.Distance(camera.transform.position, CameraPosition) < .001f,
            "EARTH_001 must use the approved fixed camera composition.");
        Require(!cameraController.enabled && cameraController.Target == null && cameraController.UsesRoomBounds &&
                cameraController.RoomBounds == CameraBounds,
            "EARTH_001 camera controller must remain fixed and use the approved room bounds.");
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
