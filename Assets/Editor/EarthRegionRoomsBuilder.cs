using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds EARTH_002..EARTH_015 Tilemap greyboxes from shared gameplay Prefabs.</summary>
public static class EarthRegionRoomsBuilder
{
    private const string TerrainTilePath = "Assets/Tiles/Earth/Earth001TerrainGraybox.asset";
    private const string SinkPrefabPath = "Assets/Prefabs/Gameplay/Earth/SinkingEarthBlock2D.prefab";
    private const string MovingPrefabPath = "Assets/Prefabs/Gameplay/Platforms/MovingPlatform2D.prefab";
    private const string PlatePrefabPath = "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab";
    private const string DoorPrefabPath = "Assets/Prefabs/Gameplay/Doors/Door2D.prefab";
    private const string EnemyPrefabPath = "Assets/Prefabs/Gameplay/Enemies/VerticalWallPatrolEnemy2D.prefab";
    private const string ExitPrefabPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";

    private static readonly Dictionary<int, int[]> ImplementedNeighbors = new()
    {
        [2] = new[] {1}, [3] = new[] {1,4}, [4] = new[] {3,5}, [5] = new[] {4,7},
        [6] = new[] {7,10}, [7] = new[] {5,8,6,11}, [8] = new[] {7,9}, [9] = new[] {8},
        [10] = new[] {6,14}, [11] = new[] {7,12,15}, [12] = new[] {11,13},
        [13] = new[] {12}, [14] = new[] {10}, [15] = new[] {11}
    };

    [MenuItem("Tools/W1/Build EARTH-002 to EARTH-015 Greyboxes")]
    public static void BuildAll()
    {
        Require(AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath) != null,
            $"Missing terrain Tile: {TerrainTilePath}");
        Require(AssetDatabase.LoadAssetAtPath<GameObject>(SinkPrefabPath) != null,
            $"Missing sinking block Prefab: {SinkPrefabPath}");
        Directory.CreateDirectory("Assets/Scenes/Levels/Earth");

        for (int id = 2; id <= 15; id++) BuildRoom(id);

        AssetDatabase.SaveAssets();
        Debug.Log("EARTH_002 through EARTH_015 Tilemap greyboxes built successfully.");
    }

    private static void BuildRoom(int id)
    {
        Tile terrainTile = Load<Tile>(TerrainTilePath);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new($"EARTH_{id:000} {RoomName(id)} Greybox");

        GameObject grid = new("Grid");
        grid.transform.SetParent(root.transform, false);
        grid.AddComponent<Grid>().cellSize = Vector3.one;
        CreateTilemap(grid.transform, "Background");
        Tilemap terrain = CreateTilemap(grid.transform, "Terrain");
        ConfigureTerrain(terrain);
        CreateTilemap(grid.transform, "FrozenGround");
        CreateTilemap(grid.transform, "OneWayPlatform");
        CreateTilemap(grid.transform, "SpecialMirrorWall");
        CreateTilemap(grid.transform, "Hazard");
        CreateTilemap(grid.transform, "Decoration");
        CreateTilemap(grid.transform, "Foreground");

        BuildTerrain(id, terrain, terrainTile);
        Bake(terrain);

        GameObject gameplay = new("Gameplay");
        gameplay.transform.SetParent(root.transform, false);
        GameObject dynamics = new("DynamicObjects");
        dynamics.transform.SetParent(gameplay.transform, false);
        GameObject entrances = new("Entrances");
        entrances.transform.SetParent(gameplay.transform, false);
        GameObject exits = new("Exits");
        exits.transform.SetParent(gameplay.transform, false);

        Transform entrance = Marker("Entrance-DEFAULT", new Vector3(-10f, -2.08f, 0f), entrances.transform);
        CreateGameplay(id, dynamics.transform, scene);
        CreateExits(id, exits.transform, scene);
        CameraFollow2D cameraFollow = CreateCamera(id);

        GameObject systems = new("RoomSystems");
        systems.transform.SetParent(root.transform, false);
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, cameraFollow, true);

        Validate(scene, id, terrain);
        string path = $"Assets/Scenes/Levels/Earth/Earth_{id:000}.unity";
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, path), $"Failed to save {path}");
        AddBuildScene(path);
    }

    private static void BuildTerrain(int id, Tilemap terrain, Tile tile)
    {
        Fill(terrain, tile, -13, 12, -4, -4);
        Fill(terrain, tile, -13, 12, -7, -7);
        Fill(terrain, tile, -13, -13, -6, 7);
        Fill(terrain, tile, 12, 12, -6, 7);
        Fill(terrain, tile, -13, 12, 7, 7);

        foreach (float blockX in BlockXs(id)) ClearForBlock(terrain, blockX, BlockWidth(id));
        if (id is 3 or 5 or 9 or 13)
        {
            Fill(terrain, tile, -10, -7, 0, 0);
            Fill(terrain, tile, 7, 10, 2, 2);
        }
        if (id is 4 or 7 or 8 or 15)
        {
            Fill(terrain, tile, -2, 2, 1, 1);
        }
        if (id is 6 or 10 or 14)
        {
            Fill(terrain, tile, 7, 11, 0, 0);
        }
        if (id is 11 or 12)
        {
            Fill(terrain, tile, -5, -5, -3, 4);
            Fill(terrain, tile, 5, 5, -3, 4);
        }
        if (id == 15)
        {
            Fill(terrain, tile, -11, -8, 3, 3);
            Fill(terrain, tile, 8, 11, 3, 3);
        }
    }

    private static void ClearForBlock(Tilemap terrain, float centerX, float width)
    {
        int minX = Mathf.FloorToInt(centerX - width * .5f);
        int maxX = Mathf.CeilToInt(centerX + width * .5f) - 1;
        for (int x = minX; x <= maxX; x++) terrain.SetTile(new Vector3Int(x, -4, 0), null);
    }

    private static void CreateGameplay(int id, Transform parent, Scene scene)
    {
        float[] xs = BlockXs(id);
        for (int i = 0; i < xs.Length; i++)
        {
            float distance = id is 3 or 10 or 13 ? 3f : id == 14 ? 4f : 2f;
            float recovery = id == 9 ? (i == 0 ? .7f : 1.2f) : 1f;
            CreateSink(parent, scene, xs[i], BlockWidth(id), distance, recovery, $"SinkingBlock-{(char)('A' + i)}");
        }

        if (id == 6) CreateMoving(parent, scene, new Vector2(6f, -1.5f), new Vector2(-2f, 0f),
            new Vector2(2f, 0f), "MovingPlatform-A");
        if (id == 10) CreateMoving(parent, scene, new Vector2(4f, -4.5f), new Vector2(-3f, 0f),
            new Vector2(3f, 0f), "MovingPlatform-A");
        if (id == 14)
        {
            CreateMoving(parent, scene, new Vector2(4f, -2f), new Vector2(-3f, 0f),
                new Vector2(3f, 0f), "MovingPlatform-A");
            CreateMoving(parent, scene, new Vector2(-4f, -5f), new Vector2(-3f, 0f),
                new Vector2(3f, 0f), "MovingPlatform-B");
        }
        if (id == 15) CreateMoving(parent, scene, new Vector2(0f, 1.5f), new Vector2(-3f, 0f),
            new Vector2(3f, 0f), "MovingPlatform-A");

        if (id is 8 or 15)
        {
            PressurePlate2D plate = CreatePlate(parent, scene,
                id == 8 ? new Vector2(-7f, -3.35f) : new Vector2(7f, -3.35f), "PressurePlate-A");
            CreateDoor(parent, scene, id == 8 ? new Vector2(0f, -3f) : new Vector2(10f, -3f),
                plate, "Door-A");
        }

        if (id == 11)
            CreateEnemy(parent, scene, new Vector2(-3.54f, 0f), -2f, 2f, "Enemy-A");
        if (id == 12)
        {
            CreateEnemy(parent, scene, new Vector2(-3.54f, 0f), -2f, 2f, "Enemy-A");
            CreateEnemy(parent, scene, new Vector2(6.46f, 0f), -2f, 2f, "Enemy-B");
        }
    }

    private static float[] BlockXs(int id) => id switch
    {
        2 or 3 or 6 or 10 or 11 or 12 or 14 => new[] {0f},
        4 => new[] {-5f, 5f},
        5 => new[] {-4f, 4f},
        7 => new[] {-6f, 6f},
        8 => new[] {5f},
        9 => new[] {-2f, 3f},
        13 => new[] {-4f, 4f},
        15 => new[] {-6f, 0f, 6f},
        _ => Array.Empty<float>()
    };

    private static float BlockWidth(int id) => id is 3 or 6 or 7 or 10 or 12 or 14 ? 3f : 2f;

    private static void CreateSink(Transform parent, Scene scene, float x, float width,
        float distance, float recovery, string name)
    {
        GameObject instance = Prefab(SinkPrefabPath, parent, scene, new Vector3(x, -3.5f), name);
        instance.transform.localScale = new Vector3(width / 2f, 1f, 1f);
        SinkingEarthBlock2D block = instance.GetComponent<SinkingEarthBlock2D>();
        Require(block != null, "Sinking block Prefab is missing SinkingEarthBlock2D.");
        SerializedObject data = new(block);
        data.FindProperty("sinkDistance").floatValue = distance;
        data.FindProperty("sinkSpeed").floatValue = 1.5f;
        data.FindProperty("recoverSpeed").floatValue = recovery;
        data.FindProperty("weightForFullSink").floatValue = 1f;
        data.ApplyModifiedPropertiesWithoutUndo();
        Record(instance.transform);
        Record(block);
    }

    private static void CreateMoving(Transform parent, Scene scene, Vector2 position,
        Vector2 start, Vector2 end, string name)
    {
        GameObject instance = Prefab(MovingPrefabPath, parent, scene, position, name);
        MovingPlatform2D moving = instance.GetComponent<MovingPlatform2D>();
        Require(moving != null, "Moving platform Prefab is missing MovingPlatform2D.");
        moving.ConfigurePath(start, end, 2f, .5f);
        Record(moving);
    }

    private static PressurePlate2D CreatePlate(Transform parent, Scene scene, Vector2 position, string name)
        => Prefab(PlatePrefabPath, parent, scene, position, name).GetComponent<PressurePlate2D>();

    private static void CreateDoor(Transform parent, Scene scene, Vector2 position,
        PressurePlate2D plate, string name)
    {
        Door2D door = Prefab(DoorPrefabPath, parent, scene, position, name).GetComponent<Door2D>();
        Require(door != null && plate != null, "Door or pressure plate Prefab is invalid.");
        door.ConfigureControlSource(plate);
        Record(door);
    }

    private static void CreateEnemy(Transform parent, Scene scene, Vector2 position,
        float bottom, float top, string name)
    {
        GameObject instance = Prefab(EnemyPrefabPath, parent, scene, position, name);
        VerticalWallPatrolEnemy2D enemy = instance.GetComponent<VerticalWallPatrolEnemy2D>();
        Require(enemy != null, "Vertical wall enemy Prefab is invalid.");
        enemy.ConfigurePatrol(bottom, top, 1.5f, .3f, VerticalWallPatrolEnemy2D.WallSide.Left, true);
        Record(enemy);
    }

    private static void CreateExits(int id, Transform parent, Scene scene)
    {
        int[] neighbors = ImplementedNeighbors[id];
        for (int i = 0; i < neighbors.Length; i++)
        {
            float x = neighbors.Length == 1 ? 10f : Mathf.Lerp(-11f, 10f, i / (float)(neighbors.Length - 1));
            GameObject instance = Prefab(ExitPrefabPath, parent, scene, new Vector3(x, -3f),
                $"Exit to EARTH_{neighbors[i]:000}");
            RoomExit2D exit = instance.GetComponent<RoomExit2D>();
            Require(exit != null, "Room exit Prefab is invalid.");
            exit.Configure($"Earth_{neighbors[i]:000}", "DEFAULT");
            Record(exit);
        }
    }

    private static CameraFollow2D CreateCamera(int id)
    {
        GameObject go = new("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.backgroundColor = new Color(.24f, .17f, .11f);
        go.AddComponent<AudioListener>();
        CameraFollow2D follow = go.AddComponent<CameraFollow2D>();
        follow.Configure(null, id is 5 or 10 or 12 or 14 or 15);
        follow.ConfigureDamping(.15f);
        follow.ConfigureBounds(new Rect(-13f, -7f, 26f, 15f));
        return follow;
    }

    private static GameObject Prefab(string path, Transform parent, Scene scene, Vector3 position, string name)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(path), scene);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        Record(instance.transform);
        return instance;
    }

    private static Tilemap CreateTilemap(Transform parent, string name)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        Tilemap map = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();
        return map;
    }

    private static void ConfigureTerrain(Tilemap map)
    {
        Rigidbody2D body = map.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D composite = map.gameObject.AddComponent<CompositeCollider2D>();
        TilemapCollider2D collider = map.gameObject.AddComponent<TilemapCollider2D>();
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        SurfaceSemantic2D semantic = map.gameObject.AddComponent<SurfaceSemantic2D>();
        semantic.Configure(SurfaceSemantic2D.SurfaceType.StaticSolid, true, true);
        MirrorSurface2D mirror = map.gameObject.AddComponent<MirrorSurface2D>();
        mirror.kind = MirrorSurface2D.SurfaceKind.Ground;
        mirror.safe = true;
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
        Require(map.GetComponent<CompositeCollider2D>().pathCount > 0, $"{map.name} has no baked geometry.");
    }

    private static Transform Marker(string name, Vector3 position, Transform parent)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        return go.transform;
    }

    private static void Validate(Scene scene, int id, Tilemap terrain)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(terrain.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.StaticSolid,
            $"EARTH_{id:000} Terrain semantic missing.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            $"EARTH_{id:000} must not serialize Player.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            $"EARTH_{id:000} needs exactly one RoomPlayerSpawner2D.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomEntrance2D>(true)).Count() == 1,
            $"EARTH_{id:000} needs exactly one DEFAULT entrance.");
        SinkingEarthBlock2D[] blocks = roots.SelectMany(root =>
            root.GetComponentsInChildren<SinkingEarthBlock2D>(true)).ToArray();
        Require(blocks.Length == BlockXs(id).Length, $"EARTH_{id:000} sinking block count mismatch.");
        Require(blocks.All(block => PrefabUtility.GetPrefabInstanceStatus(block.gameObject) ==
                PrefabInstanceStatus.Connected), $"EARTH_{id:000} has a disconnected sinking block.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomExit2D>(true)).Count() ==
                ImplementedNeighbors[id].Length, $"EARTH_{id:000} exit count mismatch.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Count() == 1,
            $"EARTH_{id:000} needs exactly one Camera.");
    }

    private static void AddBuildScene(string path)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(scene => scene.path == path))
            scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static string RoomName(int id) => id switch
    {
        2 => "Shaft Mouth", 3 => "Rising Step", 4 => "Split Weight", 5 => "Offset Meeting",
        6 => "Rail Height", 7 => "Mine Junction", 8 => "Twin Watch", 9 => "Recovery Window",
        10 => "Rising Transfer", 11 => "Stone Vein Merge", 12 => "Wall Shift",
        13 => "Reverse Strata", 14 => "Deep Shaft Cross", 15 => "Weight Core", _ => "Earth Room"
    };

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        T result = AssetDatabase.LoadAssetAtPath<T>(path);
        Require(result != null, $"Missing asset: {path}");
        return result;
    }

    private static void Record(UnityEngine.Object value)
    {
        EditorUtility.SetDirty(value);
        PrefabUtility.RecordPrefabInstancePropertyModifications(value);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
