using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds WIND_004 through WIND_015 from Tilemaps and shared gameplay Prefabs.</summary>
public static class WindRoomBatchBuilder
{
    private enum Puzzle { PeriodicWind, OpposingWind, CloneWind, RayWind, Tornado, Deflector,
        SwitchedDeflector, TurbineDoor, PeriodicTurbine, TornadoDoor, DeflectorTurbine, Finale }

    private readonly struct RoomSpec
    {
        public readonly int Number;
        public readonly string Title;
        public readonly string Target;
        public readonly Puzzle Puzzle;
        public RoomSpec(int number, string title, string target, Puzzle puzzle)
            => (Number, Title, Target, Puzzle) = (number, title, target, puzzle);
    }

    private static readonly RoomSpec[] Rooms =
    {
        new(4, "Breathing Gap", "Wind_009", Puzzle.PeriodicWind),
        new(5, "Crosswind Choice", "Wind_006", Puzzle.OpposingWind),
        new(6, "Shared Current", "Wind_005", Puzzle.CloneWind),
        new(7, "Sacrificial Draft", "Wind_010", Puzzle.RayWind),
        new(8, "Tornado Telegraph", "Wind_014", Puzzle.Tornado),
        new(9, "First Turn", "Wind_013", Puzzle.Deflector),
        new(10, "Remote Rudder", "Wind_009", Puzzle.SwitchedDeflector),
        new(11, "Wind Key", "Wind_010", Puzzle.TurbineDoor),
        new(12, "Pulse Gate", "Wind_011", Puzzle.PeriodicTurbine),
        new(13, "Shelter Gate", "Wind_014", Puzzle.TornadoDoor),
        new(14, "Bent Power", "Wind_015", Puzzle.DeflectorTurbine),
        new(15, "Eye of the Passage", "Wind_016", Puzzle.Finale),
    };

    private const string Root = "Assets/Prefabs/Gameplay/";
    private const string TerrainTexture = "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_stone_cloud_middle.png";

    [MenuItem("Tools/W1/Build WIND-004 to WIND-015 Greyboxes")]
    public static void BuildAll()
    {
        Directory.CreateDirectory("Assets/Scenes/Levels/Wind");
        Directory.CreateDirectory("Assets/Tiles/Wind");
        foreach (RoomSpec room in Rooms) Build(room);
        AssetDatabase.SaveAssets();
        Debug.Log("WIND_004 through WIND_015 Tilemap greyboxes built successfully.");
    }

    [MenuItem("Tools/W1/Build WIND-004 to WIND-008 Expanded Greyboxes")]
    public static void BuildExpandedEarlyRooms()
    {
        Directory.CreateDirectory("Assets/Scenes/Levels/Wind");
        Directory.CreateDirectory("Assets/Tiles/Wind");
        foreach (RoomSpec room in Rooms.Where(room => room.Number <= 8)) Build(room);
        AssetDatabase.SaveAssets();
        Debug.Log("WIND_004 through WIND_008 expanded Tilemap greyboxes built successfully.");
    }

    private static void Build(RoomSpec spec)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TerrainTexture);
        Require(sprite != null, "Wind terrain sprite is missing.");
        string code = $"WIND_{spec.Number:000}";
        Tile tile = TileFor($"Assets/Tiles/Wind/{code}TerrainGraybox.asset", sprite);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = New(null, $"{code} {spec.Title}");
        AddBackdrop(room.transform);
        GameObject grid = New(room.transform, "Grid");
        grid.AddComponent<Grid>();
        Layer(grid.transform, "Background");
        Tilemap terrain = Layer(grid.transform, "Terrain");
        ConfigureTerrain(terrain);
        Layer(grid.transform, "OneWayPlatform");
        Layer(grid.transform, "SpecialMirrorWall");
        Layer(grid.transform, "Hazard");
        Layer(grid.transform, "Decoration");
        Layer(grid.transform, "Foreground");
        BuildShell(terrain, tile, spec.Puzzle);

        GameObject gameplay = New(room.transform, "Gameplay");
        GameObject dynamics = New(gameplay.transform, "DynamicObjects");
        GameObject entrances = New(gameplay.transform, "Entrances");
        GameObject exits = New(gameplay.transform, "Exits");
        Transform entrance = Marker(entrances.transform, "Entrance-DEFAULT", new Vector3(-10f, -1.08f));
        AddPuzzle(spec.Puzzle, dynamics.transform);
        AddExit(exits.transform, spec.Target);
        AddCamera();
        GameObject systems = New(room.transform, "RoomSystems");
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        Bind(reset, "Assets/Scripts/Core/RoomResetSystem.cs");
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, null, false);
        Validate(scene, terrain, spec.Puzzle);
        EditorSceneManager.MarkSceneDirty(scene);
        string scenePath = $"Assets/Scenes/Levels/Wind/Wind_{spec.Number:000}.unity";
        Require(EditorSceneManager.SaveScene(scene, scenePath), $"Failed to save {code}.");
        AddBuildScene(scenePath);
    }

    private static void BuildShell(Tilemap terrain, Tile tile, Puzzle puzzle)
    {
        Fill(terrain, tile, -13, 12, -3, -3);
        Fill(terrain, tile, -13, 12, 6, 6);
        Fill(terrain, tile, -13, -13, -2, 5);
        Fill(terrain, tile, 12, 12, -2, 5);
        if (puzzle == Puzzle.PeriodicWind)
        {
            Fill(terrain, tile, -5, -3, -1, -1);
            Fill(terrain, tile, 1, 3, 1, 1);
            Fill(terrain, tile, 7, 9, 0, 0);
            Fill(terrain, tile, -2, -2, -2, -1);
            Fill(terrain, tile, 5, 5, -2, 0);
        }
        if (puzzle == Puzzle.OpposingWind)
        {
            Fill(terrain, tile, -8, -4, 0, 0);
            Fill(terrain, tile, -1, 2, 2, 2);
            Fill(terrain, tile, 6, 9, 0, 0);
            Fill(terrain, tile, -3, -3, -2, 0);
            Fill(terrain, tile, 4, 4, -2, 0);
        }
        if (puzzle == Puzzle.CloneWind)
        {
            Fill(terrain, tile, -8, -6, 0, 0);
            Fill(terrain, tile, 7, 9, 0, 0);
        }
        if (puzzle == Puzzle.RayWind)
        {
            Fill(terrain, tile, -5, -2, 0, 0);
            Fill(terrain, tile, 3, 6, 1, 1);
            Fill(terrain, tile, 8, 10, 0, 0);
        }
        if (puzzle == Puzzle.Tornado)
        {
            Fill(terrain, tile, -6, -4, 0, 0);
            Fill(terrain, tile, 0, 2, 1, 1);
            Fill(terrain, tile, 6, 8, 0, 0);
            Fill(terrain, tile, -2, -2, -2, 0);
            Fill(terrain, tile, 4, 4, -2, 0);
        }
        if (puzzle is Puzzle.Deflector or Puzzle.SwitchedDeflector or Puzzle.DeflectorTurbine)
        {
            Fill(terrain, tile, 0, 0, 0, 3);
            Fill(terrain, tile, 1, 5, 4, 4);
        }
        terrain.CompressBounds();
        terrain.RefreshAllTiles();
        terrain.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        terrain.GetComponent<CompositeCollider2D>().GenerateGeometry();
        Require(terrain.GetComponent<CompositeCollider2D>().pathCount > 0, "Terrain collider bake failed.");
    }

    private static void AddPuzzle(Puzzle puzzle, Transform parent)
    {
        switch (puzzle)
        {
            case Puzzle.PeriodicWind:
                Wind(parent, new Vector2(-7.5f, -1.5f), Vector2.right, new Vector2(5f, 2f), true);
                Wind(parent, new Vector2(0f, .5f), Vector2.right, new Vector2(6f, 2f), true);
                Wind(parent, new Vector2(7.5f, -1f), Vector2.right, new Vector2(5f, 3f), true);
                break;
            case Puzzle.OpposingWind:
                Wind(parent, new Vector2(-7f, 0f), Vector2.up, new Vector2(4f, 5f));
                Wind(parent, new Vector2(0f, 0f), Vector2.right, new Vector2(6f, 4f));
                Wind(parent, new Vector2(7f, -1f), Vector2.left, new Vector2(5f, 3f));
                break;
            case Puzzle.CloneWind:
                Wind(parent, new Vector2(-2f, -1.5f), Vector2.left, new Vector2(16f, 2f));
                PressurePlate2D clonePlate = Plate(parent, new Vector2(-9f, -2.35f));
                Door2D cloneDoor = Door(parent, new Vector2(5f, -1f));
                cloneDoor.ConfigureControlSource(clonePlate);
                Record(cloneDoor);
                break;
            case Puzzle.RayWind:
                Wind(parent, new Vector2(-5f, -1.5f), Vector2.right, new Vector2(8f, 2f));
                Wind(parent, new Vector2(6f, 0f), Vector2.left, new Vector2(8f, 4f), true);
                Prefab(Root + "Enemies/SacrificialWindRayEnemy2D.prefab", parent, "Enemy-Sacrificial Wind Ray A", new Vector2(0f, 2f));
                Prefab(Root + "Enemies/SacrificialWindRayEnemy2D.prefab", parent, "Enemy-Sacrificial Wind Ray B", new Vector2(7f, 3f));
                break;
            case Puzzle.Tornado:
                Generator(parent, new Vector2(-8f, -1.5f));
                Generator(parent, new Vector2(1f, -.5f));
                Door2D tornadoDoor = Door(parent, new Vector2(5f, -1f));
                PressurePlate2D tornadoPlate = Plate(parent, new Vector2(-5f, -2.35f));
                tornadoDoor.ConfigureControlSource(tornadoPlate);
                Record(tornadoDoor);
                break;
            case Puzzle.Deflector:
                Wind(parent, new Vector2(-5f, -1.5f), Vector2.right, new Vector2(10f, 2f));
                Deflector(parent, new Vector2(0f, -1.5f), false, null);
                break;
            case Puzzle.SwitchedDeflector:
                Wind(parent, new Vector2(-5f, -1.5f), Vector2.right, new Vector2(10f, 2f));
                PressurePlate2D control = Plate(parent, new Vector2(-7f, -2.35f));
                Deflector(parent, new Vector2(0f, -1.5f), false, control);
                break;
            case Puzzle.TurbineDoor:
                Wind(parent, new Vector2(-5f, -1.5f), Vector2.right, new Vector2(14f, 2f));
                TurbineDoor(parent, new Vector2(1f, -1.5f), new Vector2(5f, -1f));
                break;
            case Puzzle.PeriodicTurbine:
                Wind(parent, new Vector2(-5f, -1.5f), Vector2.right, new Vector2(14f, 2f), true);
                TurbineDoor(parent, new Vector2(1f, -1.5f), new Vector2(5f, -1f));
                break;
            case Puzzle.TornadoDoor:
                Generator(parent, new Vector2(-8f, -1.5f));
                Door2D shelter = Door(parent, new Vector2(2f, -1f));
                PressurePlate2D switchPlate = Plate(parent, new Vector2(-5f, -2.35f));
                shelter.ConfigureControlSource(switchPlate);
                Record(shelter);
                break;
            case Puzzle.DeflectorTurbine:
                Wind(parent, new Vector2(-5f, -1.5f), Vector2.right, new Vector2(10f, 2f));
                Deflector(parent, new Vector2(0f, -1.5f), false, null);
                TurbineDoor(parent, new Vector2(0f, 2f), new Vector2(5f, -1f), Vector2.up);
                break;
            case Puzzle.Finale:
                Wind(parent, new Vector2(-4f, -1.5f), Vector2.right, new Vector2(12f, 2f), true);
                Generator(parent, new Vector2(-9f, -1.5f));
                Prefab(Root + "Enemies/SacrificialWindRayEnemy2D.prefab", parent, "Enemy-Final Wind Ray", new Vector2(4f, 1f));
                break;
        }
    }

    private static WindColumn2D Wind(Transform parent, Vector2 position, Vector2 direction, Vector2 size, bool periodic = false)
    {
        GameObject instance = Prefab(Root + "Wind/WindColumn2D.prefab", parent, periodic ? "Wind-Periodic" : "Wind-Constant", position);
        WindColumn2D wind = instance.GetComponent<WindColumn2D>();
        wind.Configure(periodic ? WindColumn2D.WindMode.Periodic : WindColumn2D.WindMode.Constant, direction, 4f, size);
        Transform visual = instance.transform.Find("Visual");
        Record(wind, instance.GetComponent<BoxCollider2D>(), visual);
        return wind;
    }

    private static void AddBackdrop(Transform parent)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(WindVisualAssetBuilder.BackgroundPrefab);
        Require(source != null, "Wind backdrop Prefab is missing.");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        instance.name = "Wind Region Backdrop";
        instance.transform.SetParent(parent, false);
        instance.transform.position = new Vector3(0f, 0f, 5f);
        Record(instance.transform);
    }

    private static void Generator(Transform parent, Vector2 position)
        => Prefab(Root + "Wind/TornadoGenerator2D.prefab", parent, "Tornado-Generator", position);

    private static PressurePlate2D Plate(Transform parent, Vector2 position)
        => Prefab(Root + "Switches/PressurePlate2D.prefab", parent, "Pressure-Plate", position).GetComponent<PressurePlate2D>();

    private static WindDeflector2D Deflector(Transform parent, Vector2 position, bool clockwise, PressurePlate2D control)
    {
        GameObject instance = Prefab(Root + "Wind/WindDeflector2D.prefab", parent, "Wind-Deflector", position);
        WindDeflector2D deflector = instance.GetComponent<WindDeflector2D>();
        deflector.Configure(Vector2.right, clockwise, new Vector2(2f, 5f));
        deflector.ConfigureControlSource(control);
        Record(deflector, instance.GetComponent<BoxCollider2D>(), instance.transform.Find("OutputVolume")?.GetComponent<BoxCollider2D>());
        return deflector;
    }

    private static Door2D Door(Transform parent, Vector2 position)
    {
        GameObject instance = Prefab(Root + "Doors/Door2D.prefab", parent, "Door-Closed", position);
        Door2D door = instance.GetComponent<Door2D>();
        door.Configure(false);
        Record(door, instance.GetComponent<BoxCollider2D>());
        return door;
    }

    private static void TurbineDoor(Transform parent, Vector2 turbinePosition, Vector2 doorPosition, Vector2? accepted = null)
    {
        Door2D door = Door(parent, doorPosition);
        GameObject instance = Prefab(Root + "Switches/WindTurbineSwitch2D.prefab", parent, "Wind-Turbine", turbinePosition);
        WindTurbineSwitch2D turbine = instance.GetComponent<WindTurbineSwitch2D>();
        turbine.Configure(accepted ?? Vector2.right, door);
        Record(turbine);
    }

    private static void AddExit(Transform parent, string target)
    {
        GameObject instance = Prefab(Root + "Exits/RoomExit2D.prefab", parent, $"Exit-A to {target.ToUpperInvariant()}", new Vector2(10f, -1f));
        RoomExit2D exit = instance.GetComponent<RoomExit2D>();
        exit.Configure(target, "DEFAULT");
        Record(exit);
    }

    private static GameObject Prefab(string path, Transform parent, string name, Vector2 position)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Require(source != null, $"Missing Prefab: {path}");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        Record(instance.transform);
        return instance;
    }

    private static void AddCamera()
    {
        GameObject go = New(null, "Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.backgroundColor = new Color(.035f, .08f, .11f);
        go.AddComponent<AudioListener>();
    }

    private static void ConfigureTerrain(Tilemap terrain)
    {
        Rigidbody2D body = terrain.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        terrain.gameObject.AddComponent<CompositeCollider2D>();
        TilemapCollider2D collider = terrain.gameObject.AddComponent<TilemapCollider2D>();
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        SurfaceSemantic2D semantic = terrain.gameObject.AddComponent<SurfaceSemantic2D>();
        Bind(semantic, "Assets/Scripts/Gameplay/SurfaceSemantic2D.cs");
        semantic.Configure(SurfaceSemantic2D.SurfaceType.StaticSolid, true, true);
        MirrorSurface2D surface = terrain.gameObject.AddComponent<MirrorSurface2D>();
        Bind(surface, "Assets/Scripts/Gameplay/MirrorSurface2D.cs");
        surface.kind = MirrorSurface2D.SurfaceKind.Ground;
        surface.safe = true;
    }

    private static void Validate(Scene scene, Tilemap terrain, Puzzle puzzle)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.SelectMany(r => r.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0, "Room-local Player found.");
        Require(roots.SelectMany(r => r.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1, "Spawner count invalid.");
        Require(roots.SelectMany(r => r.GetComponentsInChildren<RoomExit2D>(true)).Count() == 1, "Exit count invalid.");
        Require(terrain.GetComponent<CompositeCollider2D>().pathCount > 0, "Terrain collider missing.");
        foreach (WindColumn2D wind in roots.SelectMany(r => r.GetComponentsInChildren<WindColumn2D>(true))) Connected(wind.gameObject);
        foreach (TornadoGenerator2D generator in roots.SelectMany(r => r.GetComponentsInChildren<TornadoGenerator2D>(true))) Connected(generator.gameObject);
        foreach (WindDeflector2D deflector in roots.SelectMany(r => r.GetComponentsInChildren<WindDeflector2D>(true))) Connected(deflector.gameObject);
        foreach (WindTurbineSwitch2D turbine in roots.SelectMany(r => r.GetComponentsInChildren<WindTurbineSwitch2D>(true))) Connected(turbine.gameObject);
        foreach (Door2D door in roots.SelectMany(r => r.GetComponentsInChildren<Door2D>(true))) Connected(door.gameObject);
        if (puzzle == Puzzle.TurbineDoor || puzzle == Puzzle.PeriodicTurbine || puzzle == Puzzle.DeflectorTurbine)
            Require(roots.SelectMany(r => r.GetComponentsInChildren<WindTurbineSwitch2D>(true)).Single().ControlledDoor != null, "Turbine door is not linked.");
    }

    private static void Connected(GameObject go)
        => Require(PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.Connected, $"Prefab disconnected: {go.name}");

    private static Tilemap Layer(Transform parent, string name)
    {
        GameObject go = New(parent, name);
        Tilemap map = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();
        return map;
    }

    private static GameObject New(Transform parent, string name)
    {
        GameObject go = new(name);
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static Transform Marker(Transform parent, string name, Vector3 position)
    {
        GameObject go = New(parent, name);
        go.transform.position = position;
        return go.transform;
    }

    private static void Fill(Tilemap map, TileBase tile, int minX, int maxX, int minY, int maxY)
    {
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++) map.SetTile(new Vector3Int(x, y), tile);
    }

    private static Tile TileFor(string path, Sprite sprite)
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
        tile.name = Path.GetFileNameWithoutExtension(path);
        tile.sprite = sprite;
        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void AddBuildScene(string path)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void Record(params UnityEngine.Object[] targets)
    {
        foreach (UnityEngine.Object target in targets)
            if (target != null) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static void Bind(UnityEngine.Object behaviour, string path)
    {
        MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
        Require(script != null, $"Missing runtime script: {path}");
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
