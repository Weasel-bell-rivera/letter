using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class PatrollingHorizontalFireballEnemyTestSceneBuilder
{
    public const string SettingsPath =
        "Assets/Settings/Enemies/DefaultPatrollingHorizontalFireballEnemy.asset";
    public const string PrefabPath =
        "Assets/Prefabs/Gameplay/Enemies/PatrollingHorizontalFireballEnemy2D.prefab";
    public const string ScenePath =
        "Assets/Scenes/Tests/PatrollingHorizontalFireballEnemyTest.unity";
    private const string BaseEnemyPrefabPath =
        "Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab";
    private const string AttackSettingsPath =
        "Assets/Settings/Enemies/DefaultHorizontalFireballEnemy.asset";
    private const string TerrainTilePath = "Assets/Tiles/Graybox/Fire002Terrain.asset";
    private const string SourceScenePath = "Assets/Scenes/Levels/Fire/Fire_002.unity";

    [MenuItem("Tools/W1/Tests/Build Patrolling Fireball Enemy Test Scene")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    public static void BuildFromCommandLine()
    {
        Directory.CreateDirectory("Assets/Settings/Enemies");
        Directory.CreateDirectory("Assets/Prefabs/Gameplay/Enemies");
        Directory.CreateDirectory("Assets/Scenes/Tests");

        PatrollingHorizontalFireballEnemySettings settings = CreateOrUpdateSettings();
        GameObject prefab = CreateOrUpdatePrefab(settings);
        CreateOrUpdateScene(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Patrolling horizontal fireball enemy test scene built successfully.");
    }

    private static PatrollingHorizontalFireballEnemySettings CreateOrUpdateSettings()
    {
        HorizontalFireballEnemySettings attack =
            AssetDatabase.LoadAssetAtPath<HorizontalFireballEnemySettings>(AttackSettingsPath);
        Require(attack != null, $"Missing approved attack settings: {AttackSettingsPath}");
        PatrollingHorizontalFireballEnemySettings settings =
            AssetDatabase.LoadAssetAtPath<PatrollingHorizontalFireballEnemySettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<PatrollingHorizontalFireballEnemySettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }
        settings.name = "DefaultPatrollingHorizontalFireballEnemy";
        settings.Configure(attack, 1.5f, .2f, .08f, .16f, .05f);
        EditorUtility.SetDirty(settings);
        return settings;
    }

    private static GameObject CreateOrUpdatePrefab(PatrollingHorizontalFireballEnemySettings settings)
    {
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BaseEnemyPrefabPath);
        Require(basePrefab != null, $"Missing base horizontal fireball enemy: {BaseEnemyPrefabPath}");
        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        try
        {
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            root.name = "PatrollingHorizontalFireballEnemy2D";
            HorizontalFireballEnemy2D attack = root.GetComponent<HorizontalFireballEnemy2D>();
            BoxCollider2D solid = root.transform.Find("BodyCollider")?.GetComponent<BoxCollider2D>();
            Require(attack != null && solid != null, "Base enemy Prefab structure is incomplete.");
            PatrollingHorizontalFireballEnemy2D patrol =
                root.GetComponent<PatrollingHorizontalFireballEnemy2D>() ??
                root.AddComponent<PatrollingHorizontalFireballEnemy2D>();
            patrol.Configure(settings, attack, solid);
            patrol.SetInitiallyMovingRight(true);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Require(saved != null, $"Failed to save patrol enemy Prefab: {PrefabPath}");
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CreateOrUpdateScene(GameObject enemyPrefab)
    {
        Require(File.Exists(SourceScenePath), $"Missing test scene template: {SourceScenePath}");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            Require(AssetDatabase.DeleteAsset(ScenePath), $"Failed to replace old test scene: {ScenePath}");
        Require(AssetDatabase.CopyAsset(SourceScenePath, ScenePath),
            $"Failed to copy test scene template to {ScenePath}");
        AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        GameObject root = roots.Single(item => item.name == "FIRE_002 Narrow Corridor Decoy");
        root.name = "Patrolling Horizontal Fireball Enemy Test";
        Tilemap terrain = roots.SelectMany(item => item.GetComponentsInChildren<Tilemap>(true))
            .Single(item => item.name == "Terrain");
        Transform dynamicObjects = roots.SelectMany(item => item.GetComponentsInChildren<Transform>(true))
            .Single(item => item.name == "DynamicObjects");

        foreach (HorizontalFireballEnemy2D oldEnemy in roots.SelectMany(item =>
                     item.GetComponentsInChildren<HorizontalFireballEnemy2D>(true)).ToArray())
            UnityEngine.Object.DestroyImmediate(oldEnemy.gameObject);
        foreach (RoomExit2D exit in roots.SelectMany(item =>
                     item.GetComponentsInChildren<RoomExit2D>(true)).ToArray())
            UnityEngine.Object.DestroyImmediate(exit.gameObject);

        GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, scene);
        enemy.name = "Enemy-Patrolling-Fireball";
        enemy.transform.SetParent(dynamicObjects, false);
        enemy.transform.position = new Vector3(8.5f, -4.5f, 0f);
        PatrollingHorizontalFireballEnemy2D patrol =
            enemy.GetComponent<PatrollingHorizontalFireballEnemy2D>();
        Require(patrol != null, "Patrolling enemy Prefab is missing its patrol controller.");
        patrol.SetInitiallyMovingRight(false);
        EditorUtility.SetDirty(patrol);
        PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(patrol);

        Validate(scene, terrain);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), $"Failed to save {ScenePath}");
    }

    private static Tilemap CreateTerrainTilemap(Transform parent)
    {
        GameObject terrainObject = Child(parent, "Terrain");
        Tilemap terrain = terrainObject.AddComponent<Tilemap>();
        terrainObject.AddComponent<TilemapRenderer>();
        Rigidbody2D body = terrainObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        terrainObject.AddComponent<CompositeCollider2D>();
        TilemapCollider2D collider = terrainObject.AddComponent<TilemapCollider2D>();
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        terrainObject.AddComponent<SurfaceSemantic2D>()
            .Configure(SurfaceSemantic2D.SurfaceType.StaticSolid, true, true);
        MirrorSurface2D mirror = terrainObject.AddComponent<MirrorSurface2D>();
        mirror.kind = MirrorSurface2D.SurfaceKind.Ground;
        mirror.safe = true;
        return terrain;
    }

    private static void BakeTerrain(Tilemap terrain)
    {
        terrain.CompressBounds();
        terrain.RefreshAllTiles();
        terrain.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        terrain.GetComponent<CompositeCollider2D>().GenerateGeometry();
        Physics2D.SyncTransforms();
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(1f, 0f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.5f;
        camera.backgroundColor = new Color(.07f, .025f, .015f);
        cameraObject.AddComponent<AudioListener>();
    }

    private static void Validate(Scene scene, Tilemap terrain)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.SelectMany(item => item.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "Test scene must use runtime Player spawning.");
        Require(roots.SelectMany(item => item.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "Test scene must contain exactly one Player spawner.");
        PatrollingHorizontalFireballEnemy2D patrol = roots.SelectMany(item =>
            item.GetComponentsInChildren<PatrollingHorizontalFireballEnemy2D>(true)).Single();
        Require(PrefabUtility.GetPrefabInstanceStatus(patrol.gameObject) == PrefabInstanceStatus.Connected,
            "Patrolling enemy must remain connected to its shared Prefab.");
        Require(terrain.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.StaticSolid,
            "Terrain must expose StaticSolid semantics.");
        TilemapCollider2D tilemapCollider = terrain.GetComponent<TilemapCollider2D>();
        Require(tilemapCollider != null && terrain.GetComponent<CompositeCollider2D>() != null,
            "Terrain must contain Tilemap and Composite collider components before saving.");
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
