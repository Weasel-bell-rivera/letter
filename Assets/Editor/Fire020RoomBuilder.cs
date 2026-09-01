using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds the authoritative FIRE_020 playable greybox and incrementally connects the
/// hand-authored FIRE_019 scene without invoking its historical rebuild entry point.
/// </summary>
public static class Fire020RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Fire/Fire_020.unity";
    private const string Fire019ScenePath = "Assets/Scenes/Levels/Fire/Fire_019.unity";
    private const string TerrainTilePath = "Assets/Tiles/Fire/Fire013SolidTerrain.asset";
    private const string PlatePath = "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab";
    private const string DoorPath = "Assets/Prefabs/Gameplay/Doors/Door2D.prefab";
    private const string EnemyPath = "Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab";
    private const string EruptionPath = "Assets/Prefabs/Gameplay/Hazards/EruptionHazard.prefab";
    private const string ExitPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";

    [MenuItem("Tools/W1/Build FIRE-020 Greybox")]
    public static void BuildFromMenu()
    {
        Scene originalActive = SceneManager.GetActiveScene();
        BuildFire020(originalActive);
        ConnectFire019(originalActive);
        AddBuildSceneAfter(Fire019ScenePath, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("FIRE_020 greybox and FIRE_019 bidirectional connection built successfully.");
    }

    private static void BuildFire020(Scene originalActive)
    {
        Directory.CreateDirectory("Assets/Scenes/Levels/Fire");
        Tile terrainTile = RequireAsset<Tile>(TerrainTilePath);
        GameObject platePrefab = RequireAsset<GameObject>(PlatePath);
        GameObject doorPrefab = RequireAsset<GameObject>(DoorPath);
        GameObject enemyPrefab = RequireAsset<GameObject>(EnemyPath);
        GameObject eruptionPrefab = RequireAsset<GameObject>(EruptionPath);
        GameObject exitPrefab = RequireAsset<GameObject>(ExitPath);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            GameObject room = new("FIRE_020 Fire Window Cooperation");
            SceneManager.MoveGameObjectToScene(room, scene);
            GameObject gridObject = Child(room.transform, "Grid");
            gridObject.AddComponent<Grid>();
            CreateLayer(gridObject.transform, "Background");
            Tilemap terrain = CreateLayer(gridObject.transform, "Terrain");
            ConfigureTerrain(terrain);
            CreateLayer(gridObject.transform, "OneWayPlatform");
            CreateLayer(gridObject.transform, "SpecialMirrorWall");
            CreateLayer(gridObject.transform, "Hazard");
            CreateLayer(gridObject.transform, "Decoration", false);
            CreateLayer(gridObject.transform, "Foreground");

            // Fixed-screen shell and the lower traversal lane.
            Fill(terrain, terrainTile, -12, 11, 6, 6);
            Fill(terrain, terrainTile, -12, -12, -3, 5);
            Fill(terrain, terrainTile, 11, 11, -3, 5);
            // A solid base fills the fixed camera below the playable floor without changing its Y=-4 surface.
            Fill(terrain, terrainTile, -12, 11, -7, -4);
            terrain.SetTile(new Vector3Int(-12, -3, 0), null);
            terrain.SetTile(new Vector3Int(-12, -2, 0), null);

            // Two-unit ascent step. Its top is also Plate-A's landing surface.
            Fill(terrain, terrainTile, -2, 0, -3, -2);

            // Upper fire line. X=6 is the post-latch drop back to the lower lane.
            Fill(terrain, terrainTile, 1, 10, 0, 0);
            terrain.SetTile(new Vector3Int(6, 0, 0), null);

            // The two vertically stacked doors share one divider column but separate routes.
            terrain.SetTile(new Vector3Int(7, -1, 0), terrainTile);
            Fill(terrain, terrainTile, 7, 7, 3, 5);
            Bake(terrain);

            GameObject gameplay = Child(room.transform, "Gameplay");
            Transform dynamicRoot = Child(gameplay.transform, "DynamicObjects").transform;
            Transform entrances = Child(gameplay.transform, "Entrances").transform;
            Transform exits = Child(gameplay.transform, "Exits").transform;
            Transform references = Child(gameplay.transform, "ReferenceMarkers").transform;

            Transform defaultEntrance = Marker("Entrance-DEFAULT", new Vector3(-8f, -2.08f), entrances);
            PlayerRoomAuthoring.ConfigureEntrance(defaultEntrance, "DEFAULT", true, true);
            Transform fromFire019 = Marker("Entrance-FROM_FIRE_019", new Vector3(-9.5f, -2.08f), entrances);
            PlayerRoomAuthoring.ConfigureEntrance(fromFire019, "FROM_FIRE_019", false, true);

            GameObject returnExitObject = Instance(exitPrefab, exits, "Exit-Back-to-FIRE_019",
                new Vector2(-11.5f, -2f));
            RoomExit2D returnExit = RequireComponent<RoomExit2D>(returnExitObject);
            returnExit.Configure("Fire_019", "FROM_FIRE_020");
            Record(returnExit);

            PressurePlate2D plate = Instance(platePrefab, dynamicRoot, "Plate-A",
                    new Vector2(0f, -.85f))
                .GetComponent<PressurePlate2D>();
            Require(plate != null, "Plate-A is missing PressurePlate2D.");
            plate.ConfigureActivationMode(PressurePlate2D.ActivationMode.Occupancy);
            Record(plate);

            Door2D fireWindow = Instance(doorPrefab, dynamicRoot, "Door-FireWindow",
                    new Vector2(7.5f, 2f))
                .GetComponent<Door2D>();
            Require(fireWindow != null, "Door-FireWindow is missing Door2D.");
            fireWindow.ConfigureControlSource(plate);
            fireWindow.SetState(Door2D.VisualState.Closed);
            Record(fireWindow);

            PressurePlate2D latch = FireballLatch(platePrefab, dynamicRoot, "Latch-Goal",
                new Vector2(5.5f, 1.625f));
            Door2D goalDoor = Instance(doorPrefab, dynamicRoot, "Door-Goal",
                    new Vector2(7.5f, -2f))
                .GetComponent<Door2D>();
            Require(goalDoor != null, "Door-Goal is missing Door2D.");
            goalDoor.ConfigureControlSource(latch);
            goalDoor.SetState(Door2D.VisualState.Closed);
            Record(goalDoor);

            GameObject eruptionObject = Instance(eruptionPrefab, dynamicRoot, "Eruption-A",
                new Vector2(3.5f, 3f));
            EruptionHazard2D eruption = RequireComponent<EruptionHazard2D>(eruptionObject);
            Record(eruption);

            GameObject enemyObject = Instance(enemyPrefab, dynamicRoot, "Enemy-Upper-Fixed",
                new Vector2(10.5f, 1.5f));
            HorizontalFireballEnemy2D enemy = RequireComponent<HorizontalFireballEnemy2D>(enemyObject);
            enemy.SetInitiallyFacingRight(false);
            Record(enemy);

            Marker("MirrorSetupReference", new Vector3(2.5f, 1.92f), references);
            Marker("PlayerLureReference", new Vector3(4.5f, 1.92f), references);
            Marker("FutureExitAnchor-FIRE021", new Vector3(10f, -2.08f), references);

            Camera camera = CreateCamera(room.transform);
            GameObject lightObject = Child(room.transform, "Main Light");
            Light2D light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = .85f;

            GameObject systems = Child(room.transform, "RoomSystems");
            RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
            PlayerRoomAuthoring.ConfigureRoom(systems, defaultEntrance, reset, null, true);

            ValidateFire020(scene, terrain, plate, fireWindow, latch, goalDoor, eruption, enemy,
                returnExit, camera);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save FIRE_020 scene.");
        }
        finally
        {
            RestoreActiveScene(originalActive);
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void ConnectFire019(Scene originalActive)
    {
        Scene scene = SceneManager.GetSceneByPath(Fire019ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(Fire019ScenePath, OpenSceneMode.Additive);
        Require(scene.IsValid() && scene.isLoaded, "Could not load FIRE_019 for an incremental connection patch.");
        Require(!scene.isDirty, "FIRE_019 has unsaved Editor changes; connection patch aborted.");

        try
        {
            GameObject[] roots = scene.GetRootGameObjects();
            Tilemap terrain = roots.SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
                .Single(map => map.name == "Terrain");
            TileBase currentTerrainTile = terrain.GetTile(new Vector3Int(10, -1, 0));
            Require(currentTerrainTile != null, "FIRE_019 middle corridor has no authoritative terrain tile.");

            // Open the right boundary and add an internal, non-bypassable door column.
            terrain.SetTile(new Vector3Int(11, 0, 0), null);
            terrain.SetTile(new Vector3Int(11, 1, 0), null);
            terrain.SetTile(new Vector3Int(9, 2, 0), currentTerrainTile);
            terrain.SetTile(new Vector3Int(9, 3, 0), currentTerrainTile);
            Bake(terrain);

            Transform gameplay = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Single(transform => transform.name == "Gameplay");
            Transform dynamicRoot = RequireChild(gameplay, "DynamicObjects");
            Transform entrances = RequireChild(gameplay, "Entrances");
            Transform exits = RequireChild(gameplay, "Exits");

            DestroyChild(dynamicRoot, "Latch-Exit-to-FIRE020");
            DestroyChild(dynamicRoot, "Door-Exit-to-FIRE020");
            DestroyChild(entrances, "Entrance-FROM_FIRE_020");
            DestroyChild(exits, "Exit-to-FIRE_020");

            GameObject platePrefab = RequireAsset<GameObject>(PlatePath);
            GameObject doorPrefab = RequireAsset<GameObject>(DoorPath);
            GameObject exitPrefab = RequireAsset<GameObject>(ExitPath);

            PressurePlate2D latch = FireballLatch(platePrefab, dynamicRoot, "Latch-Exit-to-FIRE020",
                new Vector2(-5.5f, -4.5f));
            Door2D door = Instance(doorPrefab, dynamicRoot, "Door-Exit-to-FIRE020",
                    new Vector2(9.5f, 1f))
                .GetComponent<Door2D>();
            Require(door != null, "FIRE_019 exit door is missing Door2D.");
            door.ConfigureControlSource(latch);
            door.SetState(Door2D.VisualState.Closed);
            Record(door);

            Transform returnEntrance = Marker("Entrance-FROM_FIRE_020", new Vector3(8f, .92f), entrances);
            PlayerRoomAuthoring.ConfigureEntrance(returnEntrance, "FROM_FIRE_020", false, false);

            GameObject exitObject = Instance(exitPrefab, exits, "Exit-to-FIRE_020", new Vector2(11.5f, 1f));
            RoomExit2D exit = RequireComponent<RoomExit2D>(exitObject);
            exit.Configure("Fire_020", "FROM_FIRE_019");
            Record(exit);

            ValidateFire019Connection(scene, terrain, latch, door, returnEntrance, exit);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, Fire019ScenePath),
                "Failed to save the FIRE_019 to FIRE_020 connection.");
        }
        finally
        {
            RestoreActiveScene(originalActive);
            if (openedHere && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void ValidateFire020(Scene scene, Tilemap terrain, PressurePlate2D plate,
        Door2D fireWindow, PressurePlate2D latch, Door2D goalDoor, EruptionHazard2D eruption,
        HorizontalFireballEnemy2D enemy, RoomExit2D returnExit, Camera camera)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(terrain.GetComponent<CompositeCollider2D>()?.pathCount > 0,
            "FIRE_020 terrain collider geometry is empty.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "FIRE_020 must not serialize Player.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "FIRE_020 needs one RoomPlayerSpawner2D.");
        RoomEntrance2D[] entrances = roots.SelectMany(root => root.GetComponentsInChildren<RoomEntrance2D>(true))
            .ToArray();
        Require(entrances.Count(item => item.EntranceId == "DEFAULT" && item.IsDefault) == 1,
            "FIRE_020 needs exactly one DEFAULT entrance.");
        Require(entrances.Count(item => item.EntranceId == "FROM_FIRE_019" && !item.IsDefault) == 1,
            "FIRE_020 needs one non-default FROM_FIRE_019 entrance.");
        Require(plate.Mode == PressurePlate2D.ActivationMode.Occupancy && fireWindow.ControlSource == plate,
            "Plate-A must temporarily control Door-FireWindow.");
        Require(latch.Mode == PressurePlate2D.ActivationMode.FireballLatch && goalDoor.ControlSource == latch,
            "Latch-Goal must latch Door-Goal open.");
        Require(EruptionPrefabConnected(eruption) && PrefabUtility.GetPrefabInstanceStatus(enemy.gameObject) ==
                PrefabInstanceStatus.Connected,
            "FIRE_020 dynamic hazards must remain connected to shared Prefabs.");
        Require(returnExit.TargetScene == "Fire_019" && returnExit.TargetEntranceId == "FROM_FIRE_020",
            "FIRE_020 return exit target mismatch.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomExit2D>(true)).Count() == 1,
            "FIRE_020 must contain only its FIRE_019 return exit; FIRE_021 remains a marker.");
        Require(camera.GetComponent<CameraFollow2D>() == null && Mathf.Approximately(camera.orthographicSize, 7f),
            "FIRE_020 must use the approved fixed single-screen camera.");
        Tilemap decoration = roots.SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
            .Single(map => map.name == "Decoration");
        Require(!decoration.GetComponent<TilemapRenderer>().enabled,
            "FIRE_020 Decoration renderer must remain disabled for the greybox.");
        Require(terrain.GetTile(new Vector3Int(6, 0, 0)) == null,
            "FIRE_020 needs the X=6 upper drop opening.");
    }

    private static void ValidateFire019Connection(Scene scene, Tilemap terrain, PressurePlate2D latch,
        Door2D door, Transform returnEntrance, RoomExit2D exit)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(latch.Mode == PressurePlate2D.ActivationMode.FireballLatch && door.ControlSource == latch,
            "FIRE_019 exit door must be controlled by its FireballLatch.");
        Require(returnEntrance.GetComponent<RoomEntrance2D>()?.EntranceId == "FROM_FIRE_020",
            "FIRE_019 return entrance ID mismatch.");
        Require(exit.TargetScene == "Fire_020" && exit.TargetEntranceId == "FROM_FIRE_019",
            "FIRE_019 exit target mismatch.");
        Require(terrain.GetTile(new Vector3Int(11, 0, 0)) == null &&
                terrain.GetTile(new Vector3Int(11, 1, 0)) == null,
            "FIRE_019 right boundary opening is blocked.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomEntrance2D>(true))
                .Count(item => item.EntranceId == "FROM_FIRE_020") == 1,
            "FIRE_019 needs exactly one FROM_FIRE_020 entrance.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomExit2D>(true))
                .Count(item => item.TargetScene == "Fire_020") == 1,
            "FIRE_019 needs exactly one FIRE_020 exit.");
    }

    private static bool EruptionPrefabConnected(EruptionHazard2D eruption) =>
        eruption != null && PrefabUtility.GetPrefabInstanceStatus(eruption.gameObject) ==
        PrefabInstanceStatus.Connected;

    private static PressurePlate2D FireballLatch(GameObject prefab, Transform parent, string name,
        Vector2 position)
    {
        GameObject instance = Instance(prefab, parent, name, position);
        instance.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        PressurePlate2D plate = RequireComponent<PressurePlate2D>(instance);
        plate.ConfigureActivationMode(PressurePlate2D.ActivationMode.FireballLatch);
        Record(plate);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        return plate;
    }

    private static Camera CreateCamera(Transform parent)
    {
        GameObject go = Child(parent, "Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.backgroundColor = new Color(.055f, .025f, .02f);
        go.AddComponent<AudioListener>();
        return camera;
    }

    private static Tilemap CreateLayer(Transform parent, string name, bool rendererEnabled = true)
    {
        GameObject go = Child(parent, name);
        Tilemap map = go.AddComponent<Tilemap>();
        TilemapRenderer renderer = go.AddComponent<TilemapRenderer>();
        renderer.enabled = rendererEnabled;
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
        TilemapCollider2D tilemapCollider = map.GetComponent<TilemapCollider2D>();
        tilemapCollider.ProcessTilemapChanges();
        Physics2D.SyncTransforms();
        CompositeCollider2D composite = map.GetComponent<CompositeCollider2D>();
        composite.GenerateGeometry();
        Require(composite.pathCount > 0, $"{map.name} collider geometry is empty.");
    }

    private static GameObject Child(Transform parent, string name)
    {
        GameObject go = new(name);
        Scene targetScene = parent.gameObject.scene;
        if (targetScene.IsValid() && targetScene.isLoaded && go.scene != targetScene)
            SceneManager.MoveGameObjectToScene(go, targetScene);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Transform RequireChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        Require(child != null, $"Missing {parent.name}/{name}.");
        return child;
    }

    private static void DestroyChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
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
        Require(go != null, $"Failed to instantiate {prefab.name}.");
        Scene targetScene = parent.gameObject.scene;
        if (targetScene.IsValid() && targetScene.isLoaded && go.scene != targetScene)
            SceneManager.MoveGameObjectToScene(go, targetScene);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        return go;
    }

    private static T RequireComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        Require(component != null, $"{gameObject.name} is missing {typeof(T).Name}.");
        return component;
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

    private static void AddBuildSceneAfter(string referencePath, string newPath)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
            .Where(scene => scene.path != newPath)
            .ToList();
        int referenceIndex = scenes.FindIndex(scene => scene.path == referencePath);
        Require(referenceIndex >= 0, $"Build Settings is missing {referencePath}.");
        scenes.Insert(referenceIndex + 1, new EditorBuildSettingsScene(newPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void RestoreActiveScene(Scene originalActive)
    {
        if (originalActive.IsValid() && originalActive.isLoaded) SceneManager.SetActiveScene(originalActive);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
