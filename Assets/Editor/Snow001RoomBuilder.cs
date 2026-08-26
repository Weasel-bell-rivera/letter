using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds the approved SNOW_001 greybox from reusable gameplay components.</summary>
public static class Snow001RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Snow/Snow_001.unity";
    public const string TerrainTexturePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_sand_block_center.png";
    public const string TerrainTilePath = "Assets/Tiles/Snow/SnowTerrainGraybox.asset";
    public const string FrozenGroundTilePath = "Assets/Tiles/Snow/FrozenGroundSnowBlock.asset";
    public const string FrozenGroundMaterialPath = "Assets/Settings/Physics/FrozenGround.physicsMaterial2D";
    public static readonly Rect CameraBounds = new(-14f, -3f, 29f, 14f);
    public const float CameraOrthographicSize = 7f;
    public const float CameraSmoothTime = .15f;

    private const string PlatePrefabPath = "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab";
    private const string DoorPrefabPath = "Assets/Prefabs/Gameplay/Doors/Door2D.prefab";
    private const string EnemyPrefabPath = "Assets/Prefabs/Gameplay/Enemies/FreezablePatrolEnemy2D.prefab";
    private const string ExitPrefabPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";

    [MenuItem("Tools/W1/Build SNOW-001 Greybox")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    public static void BuildFromCommandLine()
    {
        Directory.CreateDirectory("Assets/Scenes/Levels/Snow");
        Directory.CreateDirectory("Assets/Tiles/Snow");

        Require(File.Exists(TerrainTexturePath), $"Missing terrain texture: {TerrainTexturePath}");
        ConfigureTerrainTexture();
        Sprite terrainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TerrainTexturePath);
        Require(terrainSprite != null, "Terrain texture did not import as a Sprite.");
        Tile terrainTile = CreateOrUpdateTile(TerrainTilePath, terrainSprite, Color.white, Tile.ColliderType.Grid);
        Tile frozenTile = AssetDatabase.LoadAssetAtPath<Tile>(FrozenGroundTilePath);
        PhysicsMaterial2D frozenMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(FrozenGroundMaterialPath);
        GameObject platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlatePrefabPath);
        GameObject doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath);
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        GameObject exitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExitPrefabPath);

        Require(frozenTile != null && frozenMaterial != null, "FrozenGround assets must be built first.");
        Require(platePrefab != null && doorPrefab != null && enemyPrefab != null && exitPrefab != null,
            "SNOW_001 gameplay Prefab dependencies are missing.");

        BuildScene(terrainTile, frozenTile, frozenMaterial,
            platePrefab, doorPrefab, enemyPrefab, exitPrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SNOW_001 greybox built successfully.");
    }

    private static void ConfigureTerrainTexture()
    {
        AssetDatabase.ImportAsset(TerrainTexturePath,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(TerrainTexturePath) as TextureImporter;
        Require(importer != null, "Terrain texture importer is unavailable.");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 64f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static void BuildScene(TileBase terrainTile, TileBase frozenTile,
        PhysicsMaterial2D frozenMaterial,
        GameObject platePrefab, GameObject doorPrefab, GameObject enemyPrefab, GameObject exitPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = new("SNOW_001 Ice Gate Step");

        GameObject gridObject = new("Grid");
        gridObject.transform.SetParent(room.transform);
        gridObject.AddComponent<Grid>().cellSize = Vector3.one;

        CreateTilemapLayer(gridObject.transform, "Background");
        Tilemap terrain = CreateTilemapLayer(gridObject.transform, "Terrain");
        Tilemap frozenGround = CreateTilemapLayer(gridObject.transform, "FrozenGround");
        CreateTilemapLayer(gridObject.transform, "OneWayPlatform");
        CreateTilemapLayer(gridObject.transform, "Hazard");
        CreateTilemapLayer(gridObject.transform, "Decoration");
        CreateTilemapLayer(gridObject.transform, "Foreground");

        ConfigureSolidTilemap(terrain, SurfaceSemantic2D.SurfaceType.StaticSolid,
            MirrorSurface2D.SurfaceKind.Ground, null);
        ConfigureSolidTilemap(frozenGround, SurfaceSemantic2D.SurfaceType.FrozenGround,
            MirrorSurface2D.SurfaceKind.Ground, frozenMaterial);

        BuildTerrain(terrain, terrainTile);
        BakeTilemapGeometry(terrain);
        Tile frozenTileForScene = AssetDatabase.LoadAssetAtPath<Tile>(FrozenGroundTilePath);
        Require(frozenTileForScene != null, "FrozenGround Tile became unavailable while building the Scene.");
        Fill(frozenGround, frozenTileForScene, 2, 3, -3, -3);
        frozenGround.RefreshAllTiles();
        frozenGround.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        frozenGround.GetComponent<CompositeCollider2D>().GenerateGeometry();

        GameObject gameplay = new("Gameplay");
        gameplay.transform.SetParent(room.transform);
        GameObject dynamicObjects = new("DynamicObjects");
        dynamicObjects.transform.SetParent(gameplay.transform);
        GameObject entrances = new("Entrances");
        entrances.transform.SetParent(gameplay.transform);
        GameObject exits = new("Exits");
        exits.transform.SetParent(gameplay.transform);

        Transform entrance = Marker("Entrance-A from SNOW_002", new Vector3(-8.45f, 6.92f, 0f), entrances.transform);
        CameraFollow2D cameraFollow = CreateCamera();

        PressurePlate2D plate = CreatePlate(platePrefab, dynamicObjects.transform,
            new Vector2(-2.5f, 6.15f));
        Door2D door = CreateDoor(doorPrefab, dynamicObjects.transform, new Vector2(-.5f, -1f), plate);
        FreezablePatrolEnemy2D enemy = CreateEnemy(enemyPrefab, dynamicObjects.transform, new Vector2(-5f, -1.48f));
        CreateExit(exitPrefab, exits.transform, new Vector2(11.5f, 2.92f));

        GameObject systems = new("RoomSystems");
        systems.transform.SetParent(room.transform);
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        BindRuntimeScript(reset, "Assets/Scripts/Core/RoomResetSystem.cs");
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, cameraFollow);

        ValidateSceneOrThrow(scene, terrain, frozenGround, plate, door, enemy, cameraFollow);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save SNOW_001 scene.");
        AddBuildScene(ScenePath);
    }

    private static void BuildTerrain(Tilemap terrain, TileBase tile)
    {
        Fill(terrain, tile, -14, 14, 10, 10);
        Fill(terrain, tile, -14, -14, -3, 10);
        Fill(terrain, tile, 14, 14, -3, 10);
        Fill(terrain, tile, -9, 1, 5, 5);       // entrance and observation route
        Fill(terrain, tile, -4, -4, 0, 5);      // structural wall ending flush with the observation floor
        Fill(terrain, tile, -13, 1, -3, -3);    // enemy lane and safe landing area
        Fill(terrain, tile, 4, 13, 1, 1);       // goal platform, four units over lower floor
        Fill(terrain, tile, 4, 4, -3, 0);       // support wall sealing the drop beside the ice strip
        Fill(terrain, tile, 3, 3, 4, 9);        // prevents dropping from observation onto the goal
        Fill(terrain, tile, -1, -1, 0, 4);      // static wall cap above the two-tile Door-A
    }

    private static Tilemap CreateTilemapLayer(Transform parent, string name)
    {
        GameObject layer = new(name);
        layer.transform.SetParent(parent, false);
        Tilemap tilemap = layer.AddComponent<Tilemap>();
        layer.AddComponent<TilemapRenderer>();
        return tilemap;
    }

    private static void ConfigureSolidTilemap(Tilemap map, SurfaceSemantic2D.SurfaceType semanticType,
        MirrorSurface2D.SurfaceKind mirrorKind, PhysicsMaterial2D material)
    {
        Rigidbody2D body = map.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        map.gameObject.AddComponent<CompositeCollider2D>();
        TilemapCollider2D collider = map.gameObject.AddComponent<TilemapCollider2D>();
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        collider.sharedMaterial = material;

        SurfaceSemantic2D semantic = map.gameObject.AddComponent<SurfaceSemantic2D>();
        BindRuntimeScript(semantic, "Assets/Scripts/Gameplay/SurfaceSemantic2D.cs");
        semantic.Configure(semanticType, true, true);
        MirrorSurface2D mirrorSurface = map.gameObject.AddComponent<MirrorSurface2D>();
        BindRuntimeScript(mirrorSurface, "Assets/Scripts/Gameplay/MirrorSurface2D.cs");
        mirrorSurface.kind = mirrorKind;
        mirrorSurface.safe = true;
    }

    private static void BakeTilemapGeometry(params Tilemap[] maps)
    {
        foreach (Tilemap map in maps)
        {
            map.CompressBounds();
            map.RefreshAllTiles();
            map.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
            map.GetComponent<CompositeCollider2D>().GenerateGeometry();
        }
        Physics2D.SyncTransforms();
    }

    private static PressurePlate2D CreatePlate(GameObject prefab, Transform parent, Vector2 position)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Plate-A";
        instance.transform.SetParent(parent, false);
        instance.transform.SetPositionAndRotation(position, Quaternion.identity);
        return instance.GetComponent<PressurePlate2D>();
    }

    private static Door2D CreateDoor(GameObject prefab, Transform parent, Vector2 position, PressurePlate2D plate)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Door-A";
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        Door2D door = instance.GetComponent<Door2D>();
        door.Configure(false, instance.GetComponentInChildren<SpriteRenderer>());
        door.ConfigureControlSource(plate);
        EditorUtility.SetDirty(door);
        PrefabUtility.RecordPrefabInstancePropertyModifications(door);
        return door;
    }

    private static FreezablePatrolEnemy2D CreateEnemy(GameObject prefab, Transform parent, Vector2 position)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Enemy-A";
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        FreezablePatrolEnemy2D enemy = instance.GetComponent<FreezablePatrolEnemy2D>();
        enemy.ConfigurePatrol(-2f, 8f, 2f, .35f, true);
        EditorUtility.SetDirty(enemy);
        PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
        return enemy;
    }

    private static void CreateExit(GameObject prefab, Transform parent, Vector2 position)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Exit-A to SNOW_002";
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        RoomExit2D exit = instance.GetComponent<RoomExit2D>();
        exit.Configure("Snow_002", "DEFAULT");
        EditorUtility.SetDirty(exit);
        PrefabUtility.RecordPrefabInstancePropertyModifications(exit);
    }

    private static CameraFollow2D CreateCamera()
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 4f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = CameraOrthographicSize;
        camera.backgroundColor = new Color(.58f, .74f, .86f);
        cameraObject.AddComponent<AudioListener>();
        CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
        BindRuntimeScript(follow, "Assets/Scripts/Gameplay/CameraFollow2D.cs");
        follow.Configure(null, true);
        follow.ConfigureDamping(CameraSmoothTime);
        follow.ConfigureBounds(CameraBounds);
        return follow;
    }

    private static SpriteRenderer Visual(string name, Transform parent, Vector2 size, Color color)
    {
        GameObject visual = new(name);
        visual.transform.SetParent(parent, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        renderer.color = color;
        ScaleVisual(renderer, size);
        return renderer;
    }

    private static void ScaleVisual(SpriteRenderer renderer, Vector2 size)
    {
        Vector2 native = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
    }

    private static Transform Marker(string name, Vector3 position, Transform parent)
    {
        GameObject marker = new(name);
        marker.transform.SetParent(parent);
        marker.transform.position = position;
        return marker.transform;
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

    private static void Fill(Tilemap map, TileBase tile, int minX, int maxX, int minY, int maxY)
    {
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                map.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private static void ValidateSceneOrThrow(Scene scene, Tilemap terrain, Tilemap frozenGround,
        PressurePlate2D plate, Door2D door, FreezablePatrolEnemy2D enemy, CameraFollow2D cameraFollow)
    {
        Require(terrain.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.StaticSolid,
            "Terrain semantic is invalid.");
        Require(frozenGround.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.FrozenGround,
            "FrozenGround semantic is invalid.");
        Require(frozenGround.HasTile(new Vector3Int(2, -3, 0)) &&
                frozenGround.HasTile(new Vector3Int(3, -3, 0)),
            "FrozenGround tiles are missing from the approved ice strip.");
        Require(frozenGround.GetComponent<TilemapCollider2D>()?.sharedMaterial?.friction == 0f,
            "FrozenGround must use the zero-friction material.");
        Require(Mathf.Abs(Mathf.DeltaAngle(plate.transform.eulerAngles.z, 0f)) < .1f,
            "Plate-A must be a horizontal Player-accessible pressure plate.");
        Require(plate.transform.position == new Vector3(-2.5f, 6.15f, 0f),
            "Plate-A must remain on the Player observation route.");
        Require(terrain.HasTile(new Vector3Int(-4, 5, 0)) &&
                !terrain.HasTile(new Vector3Int(-4, 6, 0)),
            "Terrain beside Plate-A must remain flush with the observation floor.");
        Require(door.ControlSource == plate, "Door-A must serialize Plate-A as its control source.");
        Require(door.transform.position == new Vector3(-.5f, -1f, 0f),
            "Door-A must align to the one-cell-wide, two-cell-high Terrain opening.");
        Require(door.GetComponent<BoxCollider2D>().size == new Vector2(1f, 2f),
            "Door-A must use the standard two-tile Door2D Prefab size.");
        Require(terrain.HasTile(new Vector3Int(-1, 0, 0)) && terrain.HasTile(new Vector3Int(-1, 4, 0)),
            "Static Terrain must close the shaft above the two-tile Door-A.");
        Require(terrain.HasTile(new Vector3Int(4, -3, 0)) && terrain.HasTile(new Vector3Int(4, 0, 0)),
            "Static Terrain must seal the drop beside the FrozenGround strip.");
        Require(enemy.LeftEndpoint < 0f && enemy.RightEndpoint > 0f, "Enemy-A patrol endpoints must be local offsets.");
        Camera camera = cameraFollow.GetComponent<Camera>();
        Require(camera != null && camera.orthographic &&
                Mathf.Approximately(camera.orthographicSize, CameraOrthographicSize),
            "SNOW_001 must use the approved orthographic camera size.");
        Require(cameraFollow.FollowsVertical && cameraFollow.UsesRoomBounds &&
                cameraFollow.RoomBounds == CameraBounds &&
                Mathf.Approximately(cameraFollow.SmoothTime, CameraSmoothTime),
            "SNOW_001 must use the approved bounded, damped follow camera configuration.");

        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.SelectMany(root => root.GetComponentsInChildren<SurfaceSemantic2D>(true))
                .All(surface => surface.Type != SurfaceSemantic2D.SurfaceType.SpecialMirrorWall),
            "SNOW_001 must not contain SpecialMirrorWall semantics.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "SNOW_001 must not serialize a room-local Player.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "SNOW_001 must contain exactly one RoomPlayerSpawner2D.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomResetSystem>(true)).Count() == 1,
            "SNOW_001 must contain exactly one RoomResetSystem.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomExit2D>(true)).Count() == 1,
            "SNOW_001 must contain exactly one Exit-A.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .All(component => component.GetType().Name != "Snow001PuzzleController"),
            "SNOW_001 must not contain a room-specific puzzle controller.");
    }

    private static void AddBuildScene(string path)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(entry => entry.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
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
