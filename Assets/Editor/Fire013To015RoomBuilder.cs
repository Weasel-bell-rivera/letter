using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds the approved-mechanic FIRE_013 through FIRE_017 Tilemap greyboxes.</summary>
public static class Fire013To015RoomBuilder
{
    private const string TerrainTilePath = "Assets/Tiles/Graybox/Fire009Terrain.asset";
    private const string HazardTilePath = "Assets/Tiles/Graybox/Fire008Hazard.asset";
    private const string HintTilePath = "Assets/Tiles/Graybox/Fire009MirrorHint.asset";
    private const string PlatePath = "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab";
    private const string DoorPath = "Assets/Prefabs/Gameplay/Doors/Door2D.prefab";
    private const string EnemyPath = "Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab";
    private const string EruptionPath = "Assets/Prefabs/Gameplay/Hazards/EruptionHazard.prefab";
    private const string RisingLavaPath = "Assets/Prefabs/Gameplay/Hazards/RisingLava2D.prefab";
    private const string RisingLavaArtPath = "Assets/Art/Generated/Fire/lava_rising_handpainted.png";
    private const string ExitPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";

    [MenuItem("Tools/W1/Build FIRE-013 to FIRE-017 Greyboxes")]
    public static void Build()
    {
        CreateRisingLavaPrefab();
        Assets a = LoadAssets();
        Build013(a);
        Build014(a);
        Build015(a);
        Build016(a);
        Build017(a);
        AssetDatabase.SaveAssets();
        Debug.Log("FIRE_013 through FIRE_017 Tilemap greyboxes built successfully.");
    }

    private static void Build013(Assets a)
    {
        Context c = Begin("FIRE_013 Two Door Choice", a, true);
        BaseShell(c, a, -15, 15);
        Fill(c.Terrain, a.Terrain, -6, -6, 1, 5);
        Fill(c.Terrain, a.Terrain, 6, 6, 1, 5);
        Fill(c.Terrain, a.Terrain, -12, -12, -2, 0);
        Fill(c.Terrain, a.Terrain, 12, 12, -2, 0);
        Fill(c.Terrain, a.Terrain, -1, 1, -1, -1);
        Fill(c.Terrain, a.Terrain, 0, 2, 0, 0);
        Fill(c.Terrain, a.Terrain, 1, 3, 1, 1);
        c.Decoration.SetTile(new Vector3Int(0, -2, 0), a.Hint);
        Bake(c.Terrain);

        Transform entrance = Marker("Entrance-DEFAULT", Vector3.up * -1.08f, c.Entrances);
        PressurePlate2D plateA = Plate(a, c, "Plate-A", new Vector3(4f, -1.7f));
        PressurePlate2D plateB = Plate(a, c, "Plate-B", new Vector3(-4f, -1.7f));
        Door2D doorA = Door(a, c, "Door-A to FIRE_014", new Vector3(-6f, -1.5f), plateA);
        Door2D doorB = Door(a, c, "Door-B to FIRE_015", new Vector3(6f, -1.5f), plateB);
        Exit(a, c, "Exit-A to FIRE_014", new Vector3(-13.2f, -1f), "Fire_014");
        Exit(a, c, "Exit-B to FIRE_015", new Vector3(13.2f, -1f), "Fire_015");
        Exit(a, c, "Exit-C to FIRE_009", new Vector3(2.5f, 2f), "Fire_009");
        ConfigureRoom(c, entrance);
        Finish(c, "Assets/Scenes/Levels/Fire/Fire_013.unity", plateA, plateB, doorA, doorB);
    }

    private static void Build014(Assets a)
    {
        Context c = Begin("FIRE_014 Crossfire Return", a, false);
        BaseShell(c, a, -15, 15);
        Fill(c.Terrain, a.Terrain, -8, -8, 1, 5);
        Fill(c.Terrain, a.Terrain, 8, 8, -2, 0);
        c.Decoration.SetTile(new Vector3Int(0, -2, 0), a.Hint);
        Bake(c.Terrain);

        Transform entrance = Marker("Entrance-DEFAULT", Vector3.up * -1.08f, c.Entrances);
        PressurePlate2D plate = Plate(a, c, "Plate-A", new Vector3(6.5f, -1.7f));
        Door2D door = Door(a, c, "Door-A Shield", new Vector3(-8f, -1.5f), plate);
        HorizontalFireballEnemy2D rightEnemy = Enemy(a, c, "Enemy-H1 Right", new Vector3(11.5f, -1.5f), false);
        HorizontalFireballEnemy2D leftEnemy = Enemy(a, c, "Enemy-H2 Left", new Vector3(-12.5f, -1.5f), true);
        Exit(a, c, "Exit-A to FIRE_013", new Vector3(-10.3f, -1f), "Fire_013");
        ConfigureRoom(c, entrance);
        Finish(c, "Assets/Scenes/Levels/Fire/Fire_014.unity", plate, door, rightEnemy, leftEnemy);
    }

    private static void Build015(Assets a)
    {
        Context c = Begin("FIRE_015 Eruption Relay", a, true);
        BaseShell(c, a, -15, 15);
        Fill(c.Terrain, a.Terrain, -10, -10, 1, 5);
        Fill(c.Terrain, a.Terrain, 4, 4, 1, 5);
        Fill(c.Terrain, a.Terrain, 10, 10, -2, 0);
        Fill(c.Hazard, a.Hazard, -4, -3, -3, -3);
        c.Decoration.SetTile(new Vector3Int(1, -2, 0), a.Hint);
        Bake(c.Terrain, c.Hazard);

        Transform entrance = Marker("Entrance-DEFAULT", new Vector3(1f, -1.08f), c.Entrances);
        PressurePlate2D plate = Plate(a, c, "Plate-A", new Vector3(8.5f, -1.7f));
        Door2D door = Door(a, c, "Door-A", new Vector3(-10f, -1.5f), plate);
        EruptionHazard2D eruptionA = Eruption(a, c, "Eruption-A", new Vector3(-1.5f, 0f));
        EruptionHazard2D eruptionB = Eruption(a, c, "Eruption-B", new Vector3(-6.5f, 0f));
        HorizontalFireballEnemy2D enemy = Enemy(a, c, "Enemy-H1", new Vector3(12.5f, -1.5f), false);
        Exit(a, c, "Exit-A to FIRE_016", new Vector3(-12.5f, -1f), "Fire_016");
        Exit(a, c, "Exit-B to FIRE_013", new Vector3(5.5f, -1f), "Fire_013");
        ConfigureRoom(c, entrance);
        Finish(c, "Assets/Scenes/Levels/Fire/Fire_015.unity", plate, door, eruptionA, eruptionB, enemy);
    }

    private static void Build016(Assets a)
    {
        Context c = Begin("FIRE_016 Two Door Relay", a, false);
        BaseShell(c, a, -15, 15);
        Fill(c.Terrain, a.Terrain, -8, -8, 1, 5);
        Fill(c.Terrain, a.Terrain, 8, 8, 1, 5);
        c.Decoration.SetTile(new Vector3Int(0, -2, 0), a.Hint);
        Bake(c.Terrain);

        Transform entrance = Marker("Entrance-DEFAULT", Vector3.up * -1.08f, c.Entrances);
        PressurePlate2D plateA = Plate(a, c, "Plate-A", new Vector3(3.5f, -1.7f));
        PressurePlate2D plateB = Plate(a, c, "Plate-B", new Vector3(-3.5f, -1.7f));
        Door2D doorA = Door(a, c, "Door-A to FIRE_017", new Vector3(-8f, -1.5f), plateA);
        Door2D doorB = Door(a, c, "Door-B to FIRE_015", new Vector3(8f, -1.5f), plateB);
        HorizontalFireballEnemy2D enemy = Enemy(a, c, "Enemy-H1", new Vector3(12.5f, -1.5f), false);
        Exit(a, c, "Exit-A to FIRE_017", new Vector3(-12f, -1f), "Fire_017");
        Exit(a, c, "Exit-B to FIRE_015", new Vector3(12f, -1f), "Fire_015");
        ConfigureRoom(c, entrance);
        Finish(c, "Assets/Scenes/Levels/Fire/Fire_016.unity", plateA, plateB, doorA, doorB, enemy);
    }

    private static void Build017(Assets a)
    {
        Context c = Begin("FIRE_017 Fire Mastery", a, false);
        BaseShell(c, a, -15, 15);
        Fill(c.Terrain, a.Terrain, -11, -11, 1, 5);
        Fill(c.Terrain, a.Terrain, 7, 7, -2, 0);
        c.Decoration.SetTile(new Vector3Int(2, -2, 0), a.Hint);
        Bake(c.Terrain);

        Transform entrance = Marker("Entrance-DEFAULT", new Vector3(2f, -1.08f), c.Entrances);
        PressurePlate2D plate = Plate(a, c, "Plate-A", new Vector3(5.5f, -1.7f));
        Door2D door = Door(a, c, "Door-A Return", new Vector3(-11f, -1.5f), plate);
        EruptionHazard2D eruptionA = Eruption(a, c, "Eruption-A", new Vector3(0f, 0f));
        EruptionHazard2D eruptionB = Eruption(a, c, "Eruption-B", new Vector3(-4.5f, 0f));
        RisingLava2D lavaA = RisingLava(a, c, "RisingLava-A", new Vector3(-1.5f, -2.5f));
        RisingLava2D lavaB = RisingLava(a, c, "RisingLava-B", new Vector3(-6.5f, -2.5f));
        HorizontalFireballEnemy2D enemy = Enemy(a, c, "Enemy-H1", new Vector3(12.5f, -1.5f), false);
        Exit(a, c, "Exit-A to FIRE_016", new Vector3(-13f, -1f), "Fire_016");
        ConfigureRoom(c, entrance);
        Finish(c, "Assets/Scenes/Levels/Fire/Fire_017.unity", plate, door, eruptionA, eruptionB, lavaA, lavaB, enemy);
    }

    private sealed class Assets
    {
        public Tile Terrain, Hazard, Hint;
        public GameObject Plate, Door, Enemy, Eruption, RisingLava, Exit;
    }

    private sealed class Context
    {
        public Scene Scene;
        public Tilemap Terrain, Hazard, Decoration;
        public Transform Dynamic, Entrances, Exits;
        public GameObject Root;
    }

    private static Assets LoadAssets()
    {
        Assets a = new()
        {
            Terrain = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath),
            Hazard = AssetDatabase.LoadAssetAtPath<Tile>(HazardTilePath),
            Hint = AssetDatabase.LoadAssetAtPath<Tile>(HintTilePath),
            Plate = AssetDatabase.LoadAssetAtPath<GameObject>(PlatePath),
            Door = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPath),
            Enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath),
            Eruption = AssetDatabase.LoadAssetAtPath<GameObject>(EruptionPath),
            RisingLava = AssetDatabase.LoadAssetAtPath<GameObject>(RisingLavaPath),
            Exit = AssetDatabase.LoadAssetAtPath<GameObject>(ExitPath)
        };
        Require(a.Terrain && a.Hazard && a.Hint && a.Plate && a.Door && a.Enemy && a.Eruption && a.RisingLava && a.Exit,
            "A shared FIRE_013-015 dependency is missing.");
        return a;
    }

    private static Context Begin(string name, Assets a, bool withHazard)
    {
        Context c = new() { Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single), Root = new GameObject(name) };
        // Unity can invalidate cached Tile objects while replacing the active Scene.
        a.Terrain = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
        a.Hazard = AssetDatabase.LoadAssetAtPath<Tile>(HazardTilePath);
        a.Hint = AssetDatabase.LoadAssetAtPath<Tile>(HintTilePath);
        Require(a.Terrain && a.Hazard && a.Hint, "Shared greybox Tiles became unavailable after Scene creation.");
        GameObject grid = new("Grid"); grid.transform.SetParent(c.Root.transform); grid.AddComponent<Grid>();
        Layer(grid.transform, "Background");
        c.Terrain = Layer(grid.transform, "Terrain"); ConfigureTerrain(c.Terrain);
        Layer(grid.transform, "OneWayPlatform"); Layer(grid.transform, "SpecialMirrorWall");
        c.Hazard = Layer(grid.transform, "Hazard"); if (withHazard) ConfigureHazard(c.Hazard);
        c.Decoration = Layer(grid.transform, "Decoration"); Layer(grid.transform, "Foreground");
        GameObject gameplay = new("Gameplay"); gameplay.transform.SetParent(c.Root.transform);
        c.Dynamic = Child(gameplay.transform, "DynamicObjects");
        c.Entrances = Child(gameplay.transform, "Entrances");
        c.Exits = Child(gameplay.transform, "Exits");
        Camera();
        return c;
    }

    private static void BaseShell(Context c, Assets a, int minX, int maxX)
    {
        Fill(c.Terrain, a.Terrain, minX, maxX, -3, -3);
        Fill(c.Terrain, a.Terrain, minX, maxX, 6, 6);
        Fill(c.Terrain, a.Terrain, minX, minX, -2, 5);
        Fill(c.Terrain, a.Terrain, maxX, maxX, -2, 5);
    }

    private static PressurePlate2D Plate(Assets a, Context c, string name, Vector3 position)
    {
        GameObject go = Instance(a.Plate, c.Dynamic, name, position);
        return go.GetComponent<PressurePlate2D>();
    }

    private static Door2D Door(Assets a, Context c, string name, Vector3 position, PressurePlate2D plate)
    {
        GameObject go = Instance(a.Door, c.Dynamic, name, position);
        Door2D door = go.GetComponent<Door2D>();
        door.ConfigureControlSource(plate); door.SetState(Door2D.VisualState.Closed);
        EditorUtility.SetDirty(door); PrefabUtility.RecordPrefabInstancePropertyModifications(door);
        return door;
    }

    private static HorizontalFireballEnemy2D Enemy(Assets a, Context c, string name, Vector3 position, bool right)
    {
        GameObject go = Instance(a.Enemy, c.Dynamic, name, position);
        HorizontalFireballEnemy2D enemy = go.GetComponent<HorizontalFireballEnemy2D>();
        enemy.SetInitiallyFacingRight(right); EditorUtility.SetDirty(enemy);
        PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
        return enemy;
    }

    private static EruptionHazard2D Eruption(Assets a, Context c, string name, Vector3 position) =>
        Instance(a.Eruption, c.Dynamic, name, position).GetComponent<EruptionHazard2D>();

    private static RisingLava2D RisingLava(Assets a, Context c, string name, Vector3 position) =>
        Instance(a.RisingLava, c.Dynamic, name, position).GetComponent<RisingLava2D>();

    private static void CreateRisingLavaPrefab()
    {
        TextureImporter importer = AssetImporter.GetAtPath(RisingLavaArtPath) as TextureImporter;
        Require(importer, "Generated hand-painted lava texture is missing.");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 887f;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        Sprite lavaSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RisingLavaArtPath);
        Require(lavaSprite, "Generated lava Sprite failed to import.");

        GameObject root = new("RisingLava2D");
        try
        {
            RisingLava2D controller = root.AddComponent<RisingLava2D>();
            GameObject moving = new("MovingLava");
            moving.transform.SetParent(root.transform, false);
            BoxCollider2D trigger = moving.AddComponent<BoxCollider2D>();
            trigger.size = new Vector2(2f, 1f);
            trigger.isTrigger = true;
            moving.AddComponent<Hazard2D>();
            SpriteRenderer visual = moving.AddComponent<SpriteRenderer>();
            visual.sprite = lavaSprite;
            visual.color = Color.white;
            visual.sortingOrder = 3;
            Vector2 native = visual.sprite.bounds.size;
            visual.transform.localScale = new Vector3(2f / native.x, 1f / native.y, 1f);
            controller.Configure(moving.transform, 4f, 1f, 2f, 1.5f, 2f, 2.5f);
            SerializedObject serialized = new(controller);
            serialized.FindProperty("visual").objectReferenceValue = visual;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(PrefabUtility.SaveAsPrefabAsset(root, RisingLavaPath), "Failed to save RisingLava2D Prefab.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void Exit(Assets a, Context c, string name, Vector3 position, string target)
    {
        GameObject go = Instance(a.Exit, c.Exits, name, position);
        RoomExit2D exit = go.GetComponent<RoomExit2D>();
        exit.Configure(target, "DEFAULT"); EditorUtility.SetDirty(exit);
        PrefabUtility.RecordPrefabInstancePropertyModifications(exit);
    }

    private static GameObject Instance(GameObject prefab, Transform parent, string name, Vector3 position)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name; go.transform.SetParent(parent, false); go.transform.position = position;
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        return go;
    }

    private static void ConfigureRoom(Context c, Transform entrance)
    {
        GameObject systems = new("RoomSystems"); systems.transform.SetParent(c.Root.transform);
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        Bind(reset, "Assets/Scripts/Core/RoomResetSystem.cs");
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, null, false);
    }

    private static void Finish(Context c, string path, params Component[] required)
    {
        GameObject[] roots = c.Scene.GetRootGameObjects();
        Require(roots.SelectMany(x => x.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0, "Room serialized a local Player.");
        Require(roots.SelectMany(x => x.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1, "Room needs one spawner.");
        Require(required.All(x => x && PrefabUtility.GetPrefabInstanceStatus(x.gameObject) == PrefabInstanceStatus.Connected), "Gameplay Prefab connection lost.");
        foreach (Door2D door in required.OfType<Door2D>()) Require(door.ControlSource, "Door control source is missing.");
        EditorSceneManager.MarkSceneDirty(c.Scene);
        Require(EditorSceneManager.SaveScene(c.Scene, path), $"Failed to save {path}.");
        AddBuildScene(path);
    }

    private static Tilemap Layer(Transform parent, string name)
    {
        GameObject go = new(name); go.transform.SetParent(parent, false);
        Tilemap map = go.AddComponent<Tilemap>(); go.AddComponent<TilemapRenderer>(); return map;
    }

    private static Transform Child(Transform parent, string name) { GameObject go = new(name); go.transform.SetParent(parent); return go.transform; }
    private static Transform Marker(string name, Vector3 position, Transform parent) { GameObject go = new(name); go.transform.SetParent(parent); go.transform.position = position; return go.transform; }

    private static void ConfigureTerrain(Tilemap map)
    {
        Rigidbody2D rb = map.gameObject.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Static;
        map.gameObject.AddComponent<CompositeCollider2D>();
        TilemapCollider2D col = map.gameObject.AddComponent<TilemapCollider2D>(); col.compositeOperation = Collider2D.CompositeOperation.Merge;
        SurfaceSemantic2D semantic = map.gameObject.AddComponent<SurfaceSemantic2D>(); Bind(semantic, "Assets/Scripts/Gameplay/SurfaceSemantic2D.cs"); semantic.Configure(SurfaceSemantic2D.SurfaceType.StaticSolid, true, true);
        MirrorSurface2D mirror = map.gameObject.AddComponent<MirrorSurface2D>(); Bind(mirror, "Assets/Scripts/Gameplay/MirrorSurface2D.cs"); mirror.kind = MirrorSurface2D.SurfaceKind.Ground; mirror.safe = true;
    }

    private static void ConfigureHazard(Tilemap map)
    {
        Rigidbody2D rb = map.gameObject.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D composite = map.gameObject.AddComponent<CompositeCollider2D>(); composite.isTrigger = true;
        TilemapCollider2D col = map.gameObject.AddComponent<TilemapCollider2D>(); col.compositeOperation = Collider2D.CompositeOperation.Merge;
        SurfaceSemantic2D semantic = map.gameObject.AddComponent<SurfaceSemantic2D>(); Bind(semantic, "Assets/Scripts/Gameplay/SurfaceSemantic2D.cs"); semantic.Configure(SurfaceSemantic2D.SurfaceType.Hazard, true, false);
        Hazard2D hazard = map.gameObject.AddComponent<Hazard2D>(); Bind(hazard, "Assets/Scripts/Gameplay/Hazard2D.cs");
    }

    private static void Bake(params Tilemap[] maps)
    {
        foreach (Tilemap map in maps) { map.CompressBounds(); map.RefreshAllTiles(); map.GetComponent<TilemapCollider2D>().ProcessTilemapChanges(); }
        Physics2D.SyncTransforms();
        foreach (Tilemap map in maps) { map.GetComponent<CompositeCollider2D>().GenerateGeometry(); Require(map.GetComponent<CompositeCollider2D>().pathCount > 0, $"{map.name} collider missing."); }
    }

    private static void Fill(Tilemap map, TileBase tile, int minX, int maxX, int minY, int maxY)
    { for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++) map.SetTile(new Vector3Int(x, y), tile); }

    private static void Camera()
    {
        GameObject go = new("Main Camera"); go.tag = "MainCamera"; go.transform.position = new Vector3(0, 0, -10);
        Camera camera = go.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 7; camera.backgroundColor = new Color(.055f, .025f, .02f); go.AddComponent<AudioListener>();
    }

    private static void AddBuildScene(string path)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(x => x.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void Bind(UnityEngine.Object behaviour, string path)
    {
        MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path); Require(script, $"Missing {path}");
        SerializedObject so = new(behaviour); so.FindProperty("m_Script").objectReferenceValue = script; so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
