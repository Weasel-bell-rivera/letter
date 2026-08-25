using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Wind003RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Wind/Wind_003.unity";
    private const string WindPrefabPath = "Assets/Prefabs/Gameplay/Wind/WindColumn2D.prefab";
    private const string ExitPrefabPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";
    private const string TerrainTexturePath = "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_stone_cloud_middle.png";
    private const string TerrainTilePath = "Assets/Tiles/Wind/Wind003TerrainGraybox.asset";

    [MenuItem("Tools/W1/Build WIND-003 Constant Wind Greybox")]
    public static void BuildFromMenu()
    {
        Directory.CreateDirectory("Assets/Scenes/Levels/Wind");
        Directory.CreateDirectory("Assets/Tiles/Wind");
        BuildScene();
        AssetDatabase.SaveAssets();
        Debug.Log("WIND_003 constant-wind Tilemap greybox built successfully.");
    }

    private static void BuildScene()
    {
        GameObject windPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindPrefabPath);
        GameObject exitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExitPrefabPath);
        Sprite terrainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TerrainTexturePath);
        Require(windPrefab != null && exitPrefab != null && terrainSprite != null, "WIND_003 dependency is missing.");
        Tile terrainTile = CreateOrUpdateTile(TerrainTilePath, terrainSprite);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = new("WIND_003 Tailwind Corridor");
        AddBackdrop(room.transform);
        GameObject gridRoot = Child(room.transform, "Grid");
        gridRoot.AddComponent<Grid>();
        CreateLayer(gridRoot.transform, "Background");
        Tilemap terrain = CreateLayer(gridRoot.transform, "Terrain");
        ConfigureTerrain(terrain);
        CreateLayer(gridRoot.transform, "OneWayPlatform");
        CreateLayer(gridRoot.transform, "SpecialMirrorWall");
        CreateLayer(gridRoot.transform, "Hazard");
        CreateLayer(gridRoot.transform, "Decoration");
        CreateLayer(gridRoot.transform, "Foreground");

        Fill(terrain, terrainTile, -13, 12, 6, 6);
        Fill(terrain, terrainTile, -13, -13, -6, 5);
        Fill(terrain, terrainTile, 12, 12, -6, 5);
        Fill(terrain, terrainTile, -11, -7, 1, 1);
        Fill(terrain, terrainTile, -12, 7, -3, -3);
        Fill(terrain, terrainTile, -6, -3, 0, 0);
        Fill(terrain, terrainTile, -1, 2, 1, 1);
        Fill(terrain, terrainTile, 4, 7, 0, 0);
        Fill(terrain, terrainTile, -2, -2, -2, 0);
        Fill(terrain, terrainTile, 3, 3, -2, 0);
        Fill(terrain, terrainTile, 7, 7, -6, -4);
        Fill(terrain, terrainTile, 11, 11, -6, -3);
        Fill(terrain, terrainTile, 7, 11, -7, -7);
        terrain.CompressBounds();
        terrain.RefreshAllTiles();
        terrain.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        terrain.GetComponent<CompositeCollider2D>().GenerateGeometry();
        Require(terrain.GetComponent<CompositeCollider2D>().pathCount > 0, "Terrain collider bake failed.");

        GameObject gameplay = Child(room.transform, "Gameplay");
        GameObject dynamicObjects = Child(gameplay.transform, "DynamicObjects");
        GameObject entrances = Child(gameplay.transform, "Entrances");
        GameObject exits = Child(gameplay.transform, "Exits");
        Transform entrance = Marker("Entrance-DEFAULT", new Vector3(-9f, 2.92f), entrances.transform);
        CreateWind(windPrefab, dynamicObjects.transform, "Wind-W1 Short Right", new Vector2(-8f, -1.5f), new Vector2(5f, 2f));
        CreateWind(windPrefab, dynamicObjects.transform, "Wind-W2 Raised Right", new Vector2(0f, -.5f), new Vector2(5f, 3f));
        CreateWind(windPrefab, dynamicObjects.transform, "Wind-W3 Exit Right", new Vector2(6f, -1f), new Vector2(5f, 3f));
        CreateExit(exitPrefab, exits.transform);
        CreateCamera();

        GameObject systems = Child(room.transform, "RoomSystems");
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        BindRuntimeScript(reset, "Assets/Scripts/Core/RoomResetSystem.cs");
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, null, false);

        Validate(scene, terrain);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save WIND_003 scene.");
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(entry => entry.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void CreateWind(GameObject prefab, Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        WindColumn2D wind = instance.GetComponent<WindColumn2D>();
        Require(wind != null, "Wind Prefab is missing WindColumn2D.");
        wind.Configure(WindColumn2D.WindMode.Constant, Vector2.right, 4f, size);
        Transform visual = instance.transform.Find("Visual");
        Record(instance.transform, wind, instance.GetComponent<BoxCollider2D>(), visual);
    }

    private static void AddBackdrop(Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindVisualAssetBuilder.BackgroundPrefab);
        Require(prefab != null, "Wind backdrop Prefab is missing.");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Wind Region Backdrop";
        instance.transform.SetParent(parent, false);
        instance.transform.position = new Vector3(0f, 0f, 5f);
        Record(instance.transform);
    }

    private static void CreateExit(GameObject prefab, Transform parent)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Exit-A to WIND_004";
        instance.transform.SetParent(parent, false);
        instance.transform.position = new Vector3(9f, -5f);
        RoomExit2D exit = instance.GetComponent<RoomExit2D>();
        Require(exit != null, "Exit Prefab is missing RoomExit2D.");
        exit.Configure("Wind_004", "DEFAULT");
        Record(instance.transform, exit);
    }

    private static void CreateCamera()
    {
        GameObject target = new("Main Camera");
        target.tag = "MainCamera";
        target.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = target.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.backgroundColor = new Color(.035f, .08f, .11f);
        target.AddComponent<AudioListener>();
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

    private static Tilemap CreateLayer(Transform parent, string name)
    {
        GameObject layer = Child(parent, name);
        Tilemap map = layer.AddComponent<Tilemap>();
        layer.AddComponent<TilemapRenderer>();
        return map;
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
            for (int x = minX; x <= maxX; x++) map.SetTile(new Vector3Int(x, y), tile);
    }

    private static Tile CreateOrUpdateTile(string path, Sprite sprite)
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.name = Path.GetFileNameWithoutExtension(path);
        tile.sprite = sprite;
        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void Validate(Scene scene, Tilemap terrain)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.SelectMany(r => r.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0, "Room-local Player found.");
        Require(roots.SelectMany(r => r.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1, "Spawner count invalid.");
        Require(roots.SelectMany(r => r.GetComponentsInChildren<RoomEntrance2D>(true)).Count() == 1, "Entrance count invalid.");
        Require(roots.SelectMany(r => r.GetComponentsInChildren<RoomExit2D>(true)).Count() == 1, "Exit count invalid.");
        WindColumn2D[] winds = roots.SelectMany(r => r.GetComponentsInChildren<WindColumn2D>(true)).ToArray();
        Require(winds.Length == 3, "WIND_003 must contain three teaching wind sections.");
        foreach (WindColumn2D wind in winds)
        {
            Require(PrefabUtility.GetPrefabInstanceStatus(wind.gameObject) == PrefabInstanceStatus.Connected, "Wind Prefab disconnected.");
            Require(wind.Mode == WindColumn2D.WindMode.Constant && Vector2.Distance(wind.Direction, Vector2.right) < .001f && Mathf.Approximately(wind.Speed, 4f), "Wind configuration invalid.");
            Require(wind.GetComponent<BoxCollider2D>()?.isTrigger == true, "Wind volume invalid.");
        }
        SurfaceSemantic2D semantic = terrain.GetComponent<SurfaceSemantic2D>();
        Require(semantic != null && semantic.Type == SurfaceSemantic2D.SurfaceType.StaticSolid && semantic.IsStatic && semantic.IsSafe, "Terrain semantics invalid.");
        Camera camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single();
        Require(camera.orthographic && Mathf.Approximately(camera.orthographicSize, 7f), "Camera configuration invalid.");
    }

    private static void Record(params UnityEngine.Object[] targets)
    {
        foreach (UnityEngine.Object target in targets)
            if (target != null) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static void BindRuntimeScript(UnityEngine.Object behaviour, string scriptPath)
    {
        MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        Require(script != null, $"Runtime script missing: {scriptPath}");
        SerializedObject serialized = new(behaviour);
        SerializedProperty property = serialized.FindProperty("m_Script");
        Require(property != null, $"m_Script unavailable on {behaviour.GetType().Name}.");
        property.objectReferenceValue = script;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
