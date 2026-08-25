using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds the FIRE_009 Tilemap greybox using shared gameplay prefabs.</summary>
public static class Fire009RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Fire/Fire_009.unity";
    public const string EnemyPrefabPath = "Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab";
    public const string ExitPrefabPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";
    public const string TerrainTexturePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_sand_block_center.png";
    public const string TerrainTilePath = "Assets/Tiles/Graybox/Fire009Terrain.asset";
    public const string HintTilePath = "Assets/Tiles/Graybox/Fire009MirrorHint.asset";

    [MenuItem("Tools/W1/Build FIRE-009 Greybox")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    public static void BuildFromCommandLine()
    {
        Directory.CreateDirectory("Assets/Scenes/Levels/Fire");
        Directory.CreateDirectory("Assets/Tiles/Graybox");
        BuildScene();
        AssetDatabase.SaveAssets();
        Debug.Log("FIRE_009 mirror-decoy Tilemap greybox built successfully.");
    }

    private static void BuildScene()
    {
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        GameObject exitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExitPrefabPath);
        Sprite terrainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TerrainTexturePath);
        Require(enemyPrefab != null && exitPrefab != null && terrainSprite != null,
            "FIRE_009 shared prefab or terrain dependency is missing.");

        Tile terrainTile = CreateOrUpdateTile(TerrainTilePath, terrainSprite, Color.white, Tile.ColliderType.Grid);
        Tile hintTile = CreateOrUpdateTile(HintTilePath,
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            new Color(.2f, .9f, 1f, .7f), Tile.ColliderType.None);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = new("FIRE_009 Decoy Window");
        GameObject gridRoot = new("Grid");
        gridRoot.transform.SetParent(room.transform);
        gridRoot.AddComponent<Grid>();

        CreateTilemapLayer(gridRoot.transform, "Background");
        Tilemap terrain = CreateTilemapLayer(gridRoot.transform, "Terrain");
        ConfigureTerrain(terrain);
        CreateTilemapLayer(gridRoot.transform, "OneWayPlatform");
        CreateTilemapLayer(gridRoot.transform, "SpecialMirrorWall");
        CreateTilemapLayer(gridRoot.transform, "Hazard");
        Tilemap decoration = CreateTilemapLayer(gridRoot.transform, "Decoration");
        CreateTilemapLayer(gridRoot.transform, "Foreground");

        Fill(terrain, terrainTile, -13, 12, -3, -3);
        Fill(terrain, terrainTile, -13, 12, 6, 6);
        Fill(terrain, terrainTile, -13, -13, -2, 5);
        Fill(terrain, terrainTile, 12, 12, -2, 5);
        decoration.SetTile(new Vector3Int(0, -2, 0), hintTile);
        BakeTerrain(terrain);

        GameObject gameplay = new("Gameplay");
        gameplay.transform.SetParent(room.transform);
        GameObject dynamicObjects = new("DynamicObjects");
        dynamicObjects.transform.SetParent(gameplay.transform);
        GameObject entrances = new("Entrances");
        entrances.transform.SetParent(gameplay.transform);
        GameObject exits = new("Exits");
        exits.transform.SetParent(gameplay.transform);

        Transform entrance = Marker("Entrance-DEFAULT", new Vector3(0f, -1.08f, 0f), entrances.transform);
        CreateEnemy(enemyPrefab, dynamicObjects.transform);
        CreateExit(exitPrefab, exits.transform);
        CreateCamera();

        GameObject systems = new("RoomSystems");
        systems.transform.SetParent(room.transform);
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        BindRuntimeScript(reset, "Assets/Scripts/Core/RoomResetSystem.cs");
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, null, false);

        ValidateScene(scene, terrain);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save FIRE_009 scene.");
        AddBuildScene(ScenePath);
    }

    private static void CreateEnemy(GameObject prefab, Transform parent)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Enemy-H1 Horizontal Fireball";
        instance.transform.SetParent(parent, false);
        instance.transform.position = new Vector3(7f, -1.5f, 0f);
        HorizontalFireballEnemy2D enemy = instance.GetComponent<HorizontalFireballEnemy2D>();
        Require(enemy != null, "Horizontal fireball enemy Prefab is missing its runtime component.");
        enemy.SetInitiallyFacingRight(false);
        EditorUtility.SetDirty(enemy);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
    }

    private static void CreateExit(GameObject prefab, Transform parent)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Exit-A to FIRE_010";
        instance.transform.SetParent(parent, false);
        instance.transform.position = new Vector3(-7f, -1f, 0f);
        RoomExit2D exit = instance.GetComponent<RoomExit2D>();
        exit.Configure("Fire_010", "DEFAULT");
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

    private static void BakeTerrain(Tilemap terrain)
    {
        terrain.CompressBounds();
        terrain.RefreshAllTiles();
        terrain.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        terrain.GetComponent<CompositeCollider2D>().GenerateGeometry();
        Physics2D.SyncTransforms();
        Require(terrain.GetComponent<CompositeCollider2D>().pathCount > 0,
            "FIRE_009 Terrain collider geometry was not generated.");
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

    private static void ValidateScene(Scene scene, Tilemap terrain)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "FIRE_009 must not serialize a room-local Player.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "FIRE_009 must contain exactly one RoomPlayerSpawner2D.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomEntrance2D>(true)).Count() == 1,
            "FIRE_009 must contain exactly one entrance in the minimal greybox.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomExit2D>(true)).Count() == 1,
            "FIRE_009 must contain exactly one implemented exit in the minimal greybox.");
        HorizontalFireballEnemy2D enemy = roots.SelectMany(root =>
            root.GetComponentsInChildren<HorizontalFireballEnemy2D>(true)).Single();
        Require(PrefabUtility.GetPrefabInstanceStatus(enemy.gameObject) == PrefabInstanceStatus.Connected,
            "FIRE_009 enemy must remain connected to the shared Prefab.");
        Require(Vector2.Distance(enemy.transform.position, new Vector2(7f, -1.5f)) < .001f,
            "FIRE_009 enemy must remain at its approved guard position.");
        SurfaceSemantic2D semantic = terrain.GetComponent<SurfaceSemantic2D>();
        Require(semantic != null && semantic.Type == SurfaceSemantic2D.SurfaceType.StaticSolid &&
                semantic.IsStatic && semantic.IsSafe,
            "FIRE_009 Terrain must expose safe StaticSolid semantics.");
        Require(terrain.GetComponent<MirrorSurface2D>()?.kind == MirrorSurface2D.SurfaceKind.Ground,
            "FIRE_009 Terrain must be an approved ground mirror surface.");
        Camera camera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Single();
        Require(camera.orthographic && Mathf.Approximately(camera.orthographicSize, 7f),
            "FIRE_009 must use the approved fixed single-screen camera.");
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
