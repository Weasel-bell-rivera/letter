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
/// Builds the FIRE_008 Tilemap greybox from the project's reusable gameplay prefabs.
/// This is editor tooling only; the room has no room-specific runtime behaviour.
/// </summary>
public static class Fire008RoomBuilder
{
    public const float CameraOrthographicSize = 7f;
    public const float CameraSmoothTime = .15f;
    public static readonly Rect CameraRoomBounds = new(-14f, -14f, 29f, 28f);

    public const string ScenePath = "Assets/Scenes/Levels/Fire/Fire_008.unity";
    public const string PressurePlatePrefabPath = "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab";
    public const string DoorPrefabPath = "Assets/Prefabs/Gameplay/Doors/Door2D.prefab";
    public const string DoorGroupPrefabPath = "Assets/Prefabs/Gameplay/Doors/PermanentLatchDoorGroup2D.prefab";
    public const string CheckpointPrefabPath = "Assets/Prefabs/Gameplay/Checkpoints/Checkpoint2D.prefab";
    public const string ExitPrefabPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";
    public const string TerrainTexturePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_sand_block_center.png";

    private const string TerrainTilePath = "Assets/Tiles/Graybox/Fire008Terrain.asset";
    private const string HazardTilePath = "Assets/Tiles/Graybox/Fire008Hazard.asset";
    private const string HintTilePath = "Assets/Tiles/Graybox/Fire008MirrorHint.asset";

    [MenuItem("Tools/W1/Build FIRE-008 Greybox")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    // Entry point for: Unity -batchmode -executeMethod Fire008RoomBuilder.BuildFromCommandLine
    public static void BuildFromCommandLine()
    {
        EnsureDirectories();
        BuildScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("FIRE_008 Tilemap greybox built from reusable gameplay prefabs successfully.");
    }

    private static void EnsureDirectories()
    {
        Directory.CreateDirectory("Assets/Scenes/Levels/Fire");
        Directory.CreateDirectory("Assets/Tiles/Graybox");
    }

    private static void BuildScene()
    {
        GameObject groupAsset = AssetDatabase.LoadAssetAtPath<GameObject>(DoorGroupPrefabPath);
        GameObject checkpointAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CheckpointPrefabPath);
        GameObject exitAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ExitPrefabPath);
        Require(groupAsset != null && checkpointAsset != null && exitAsset != null, "FIRE_008 prefab dependencies are missing.");

        Sprite terrainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TerrainTexturePath);
        Require(terrainSprite != null, $"Default terrain texture is missing or not imported as a Sprite: {TerrainTexturePath}");
        Sprite greyboxSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Tile terrainTile = CreateOrUpdateTile(TerrainTilePath, terrainSprite, Color.white, Tile.ColliderType.Grid);
        Tile hazardTile = CreateOrUpdateTile(HazardTilePath, greyboxSprite, new Color(1f, .18f, .02f), Tile.ColliderType.Grid);
        Tile hintTile = CreateOrUpdateTile(HintTilePath, greyboxSprite, new Color(.2f, .9f, 1f, .75f), Tile.ColliderType.None);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = new("FIRE_008 Offset Furnace");
        GameObject gridRoot = new("Grid");
        gridRoot.transform.SetParent(room.transform);
        gridRoot.AddComponent<Grid>();

        CreateTilemapLayer(gridRoot.transform, "Background");
        Tilemap terrain = CreateTilemapLayer(gridRoot.transform, "Terrain");
        ConfigureTerrain(terrain);
        CreateTilemapLayer(gridRoot.transform, "OneWayPlatform");
        CreateTilemapLayer(gridRoot.transform, "SpecialMirrorWall");
        Tilemap hazard = CreateTilemapLayer(gridRoot.transform, "Hazard");
        ConfigureHazard(hazard);
        Tilemap decoration = CreateTilemapLayer(gridRoot.transform, "Decoration");
        CreateTilemapLayer(gridRoot.transform, "Foreground");

        BuildTerrainTiles(terrain, terrainTile);
        BuildHazardTiles(hazard, hazardTile);
        BakeTilemapColliderGeometry(terrain, hazard);
        decoration.SetTile(new Vector3Int(1, 7, 0), hintTile);
        decoration.SetTile(new Vector3Int(-2, -1, 0), hintTile);
        decoration.SetTile(new Vector3Int(0, -10, 0), hintTile);

        GameObject gameplay = new("Gameplay");
        gameplay.transform.SetParent(room.transform);
        GameObject dynamicObjects = new("DynamicObjects");
        dynamicObjects.transform.SetParent(gameplay.transform);
        GameObject entrances = new("Entrances");
        entrances.transform.SetParent(gameplay.transform);
        GameObject exits = new("Exits");
        exits.transform.SetParent(gameplay.transform);

        // Player centers sit 0.9 units above the supporting tile surface. Keep a
        // small clearance so the opening physics step never starts embedded.
        Transform entrance = Marker("EntranceFromFIRE007", new Vector3(2.5f, 8.92f, 0f), entrances.transform);
        Transform returnEntrance = Marker("EntranceFromFIRE009", new Vector3(-11.9f, -11.08f, 0f), entrances.transform);
        PlayerRoomAuthoring.ConfigureEntrance(returnEntrance, "FROM_FIRE_009");

        Camera camera = CreateCamera(entrance.position);
        CameraFollow2D cameraFollow = camera.gameObject.AddComponent<CameraFollow2D>();
        BindRuntimeScript(cameraFollow, "Assets/Scripts/Gameplay/CameraFollow2D.cs");
        cameraFollow.Configure(null, true);
        cameraFollow.ConfigureDamping(CameraSmoothTime);
        cameraFollow.ConfigureBounds(CameraRoomBounds);

        CreateDoorGroup(groupAsset, dynamicObjects.transform, "DoorGroup01", SaveIds.Fire008DoorGroup01,
            new Vector2(-7.6f, 8.15f), new Vector2(6.15f, 8.15f), new Vector2(-10.5f, 9f));
        CreateDoorGroup(groupAsset, dynamicObjects.transform, "DoorGroup02", SaveIds.Fire008DoorGroup02,
            new Vector2(8.8f, 1.15f), new Vector2(-9.9f, .15f), new Vector2(11.5f, 2f));
        CreateDoorGroup(groupAsset, dynamicObjects.transform, "DoorGroup03", SaveIds.Fire008DoorGroup03,
            new Vector2(-10.25f, -8.85f), new Vector2(9.8f, -8.85f), new Vector2(-12.5f, -8f));

        CreateCheckpoint(checkpointAsset, dynamicObjects.transform, "Checkpoint-1", new Vector2(-11.9f, .92f));
        CreateCheckpoint(checkpointAsset, dynamicObjects.transform, "Checkpoint-2", new Vector2(11.9f, -8.08f));
        CreateExit(exitAsset, exits.transform, new Vector2(-13.25f, -12f));

        GameObject systems = new("RoomSystems");
        systems.transform.SetParent(room.transform);
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        BindRuntimeScript(reset, "Assets/Scripts/Core/RoomResetSystem.cs");
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, cameraFollow, false);

        ValidateSceneOrThrow(scene, terrain);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save FIRE_008 scene.");
        AddBuildScene(ScenePath);
    }

    private static void BuildTerrainTiles(Tilemap terrain, TileBase tile)
    {
        // Outer silhouettes and three separated traversal bands.
        Fill(terrain, tile, -14, 13, 13, 13);
        Fill(terrain, tile, -14, -14, -13, 13);
        Fill(terrain, tile, 14, 14, -13, 13);
        Fill(terrain, tile, -10, 13, 7, 7);       // upper floor; left side is the first drop
        Fill(terrain, tile, -13, 10, -1, -1);     // middle floor; right side is the second drop
        Fill(terrain, tile, 1, 10, 0, 0);         // stage-two raised route
        Fill(terrain, tile, -10, 13, 5, 5);       // D1 opens the left descent shaft
        Fill(terrain, tile, -12, -5, -10, -10);   // lower floor segments around lava gaps
        Fill(terrain, tile, -2, 4, -10, -10);
        Fill(terrain, tile, 7, 13, -10, -10);
        Fill(terrain, tile, -13, 10, -5, -5);     // D2 opens the right descent shaft

        // Stable stop walls beside the six plate targets.
        Fill(terrain, tile, 7, 7, 8, 9);
        Fill(terrain, tile, -11, -11, 0, 1);
        Fill(terrain, tile, -11, -11, -9, -8);
        Fill(terrain, tile, 11, 11, -9, -8);

        // Low ceilings make the shared jump affect only the unobstructed character.
        Fill(terrain, tile, -9, -4, 2, 2);
        Fill(terrain, tile, 2, 4, -7, -7);
        Fill(terrain, tile, -8, -6, -7, -7);

        // Doors remain at their native two-tile height. Static Terrain closes
        // the remaining upper part of each shaft instead of stretching sprites.
        Fill(terrain, tile, -11, -11, 10, 12);   // wall cap above D1
        Fill(terrain, tile, 11, 11, 3, 4);       // wall cap above D2
        Fill(terrain, tile, -13, -13, -7, -6);   // wall cap above D3

    }

    private static void BuildHazardTiles(Tilemap hazard, TileBase tile)
    {
        Fill(hazard, tile, -4, -3, -10, -10);
        Fill(hazard, tile, 5, 6, -10, -10);
    }

    private static void BakeTilemapColliderGeometry(Tilemap terrain, Tilemap hazard)
    {
        terrain.CompressBounds();
        hazard.CompressBounds();
        terrain.RefreshAllTiles();
        hazard.RefreshAllTiles();

        TilemapCollider2D terrainCollider = terrain.GetComponent<TilemapCollider2D>();
        CompositeCollider2D composite = terrain.GetComponent<CompositeCollider2D>();
        TilemapCollider2D hazardCollider = hazard.GetComponent<TilemapCollider2D>();
        terrainCollider.ProcessTilemapChanges();
        composite.GenerateGeometry();
        hazardCollider.ProcessTilemapChanges();
        Physics2D.SyncTransforms();

        Require(composite.pathCount > 0, "Terrain composite collider geometry was not generated.");
        Require(hazardCollider.bounds.size.sqrMagnitude > 0f, "Hazard Tilemap collider geometry was not generated.");
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
    }

    private static void ConfigureHazard(Tilemap hazard)
    {
        TilemapCollider2D collider = hazard.gameObject.AddComponent<TilemapCollider2D>();
        collider.isTrigger = true;
        SurfaceSemantic2D semantic = hazard.gameObject.AddComponent<SurfaceSemantic2D>();
        BindRuntimeScript(semantic, "Assets/Scripts/Gameplay/SurfaceSemantic2D.cs");
        semantic.Configure(SurfaceSemantic2D.SurfaceType.Hazard, true, false);
        BindRuntimeScript(hazard.gameObject.AddComponent<Hazard2D>(), "Assets/Scripts/Gameplay/Hazard2D.cs");
    }

    private static void CreateDoorGroup(GameObject prefab, Transform parent, string name, string id,
        Vector2 plateAWorld, Vector2 plateBWorld, Vector2 doorWorld)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        PressurePlate2D plateA = instance.transform.Find("PlateA").GetComponent<PressurePlate2D>();
        PressurePlate2D plateB = instance.transform.Find("PlateB").GetComponent<PressurePlate2D>();
        Door2D door = instance.transform.Find("Door").GetComponent<Door2D>();
        plateA.transform.position = plateAWorld;
        plateB.transform.position = plateBWorld;
        door.transform.position = doorWorld;

        PermanentLatchDoorGroup2D group = instance.GetComponent<PermanentLatchDoorGroup2D>();
        group.Configure(id, door, plateA, plateB);
        EditorUtility.SetDirty(group);
        PrefabUtility.RecordPrefabInstancePropertyModifications(group);
    }

    private static void CreateCheckpoint(GameObject prefab, Transform parent, string name, Vector2 position)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
    }

    private static void CreateExit(GameObject prefab, Transform parent, Vector2 position)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Exit-A to FIRE_009";
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        RoomExit2D exit = instance.GetComponent<RoomExit2D>();
        exit.Configure("Fire_009", "DEFAULT");
        EditorUtility.SetDirty(exit);
        PrefabUtility.RecordPrefabInstancePropertyModifications(exit);
    }

    private static Camera CreateCamera(Vector3 playerPosition)
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(playerPosition.x, playerPosition.y, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = CameraOrthographicSize;
        camera.backgroundColor = new Color(.055f, .025f, .02f);
        return camera;
    }

    private static SpriteRenderer Visual(string name, Transform parent, Vector2 worldSize, Color color)
    {
        GameObject visual = new(name);
        visual.transform.SetParent(parent, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        renderer.color = color;
        ScaleVisual(renderer, worldSize);
        return renderer;
    }

    private static void ScaleVisual(SpriteRenderer renderer, Vector2 worldSize)
    {
        Vector2 native = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(worldSize.x / native.x, worldSize.y / native.y, 1f);
    }

    private static Tile CreateOrUpdateTile(string path, Sprite sprite, Color color, Tile.ColliderType colliderType)
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
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

    private static void ValidateSceneOrThrow(Scene scene, Tilemap terrain)
    {
        PermanentLatchDoorGroup2D[] groups = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PermanentLatchDoorGroup2D>(true)).ToArray();
        string[] ids = groups.Select(group => group.DoorGroupId).ToArray();
        Require(groups.Length == 3, "FIRE_008 must contain exactly three permanent door groups.");
        Require(ids.All(DoorGroupId.IsValid), "FIRE_008 has an empty or invalid DoorGroupId.");
        Require(!DoorGroupId.HasDuplicates(ids), "FIRE_008 has duplicate DoorGroupIds.");
        Require(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PressurePlate2D>(true)).Count() == 6,
            "FIRE_008 must contain six pressure plates.");
        Require(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Checkpoint2D>(true)).Count() == 2,
            "FIRE_008 must contain two stage checkpoints.");
        Require(scene.GetRootGameObjects().SelectMany(root =>
                root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "FIRE_008 must not serialize a room-local Player.");
        Require(scene.GetRootGameObjects().SelectMany(root =>
                root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "FIRE_008 must contain exactly one RoomPlayerSpawner2D.");
        Camera camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Single();
        CameraFollow2D cameraFollow = camera.GetComponent<CameraFollow2D>();
        Require(camera.orthographic && Mathf.Approximately(camera.orthographicSize, CameraOrthographicSize),
            "FIRE_008 must use the approved orthographic camera size.");
        Require(cameraFollow != null && cameraFollow.FollowsVertical && cameraFollow.UsesRoomBounds,
            "FIRE_008 must use vertical follow with explicit room bounds.");
        Require(cameraFollow.RoomBounds == CameraRoomBounds,
            "FIRE_008 camera bounds must match the documented room display bounds.");
        SurfaceSemantic2D[] semantics = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<SurfaceSemantic2D>(true)).ToArray();
        Require(semantics.Any(surface => surface.Type == SurfaceSemantic2D.SurfaceType.StaticSolid && surface.IsStatic && surface.IsSafe),
            "FIRE_008 Terrain must expose the standard StaticSolid surface semantic.");
        Require(semantics.Any(surface => surface.Type == SurfaceSemantic2D.SurfaceType.Hazard && surface.IsStatic && !surface.IsSafe),
            "FIRE_008 Hazard must expose the standard unsafe Hazard surface semantic.");
        foreach (PermanentLatchDoorGroup2D group in groups)
            Require(PrefabUtility.GetPrefabInstanceStatus(group.gameObject) == PrefabInstanceStatus.Connected,
                $"{group.name} must remain connected to PermanentLatchDoorGroup2D.prefab.");
        Require(groups.SelectMany(group => group.GetComponentsInChildren<Door2D>(true))
                .All(door => door.GetComponent<BoxCollider2D>().size == new Vector2(1f, 2f)),
            "Every FIRE_008 door must use the standard two-tile Door2D Prefab size.");
        Require(terrain.HasTile(new Vector3Int(-11, 10, 0)) && terrain.HasTile(new Vector3Int(-11, 12, 0)) &&
                terrain.HasTile(new Vector3Int(11, 3, 0)) && terrain.HasTile(new Vector3Int(11, 4, 0)) &&
                terrain.HasTile(new Vector3Int(-13, -7, 0)) && terrain.HasTile(new Vector3Int(-13, -6, 0)),
            "Static Terrain wall caps must block every FIRE_008 door shaft above its two-tile door.");
        foreach (Checkpoint2D checkpoint in scene.GetRootGameObjects()
                     .SelectMany(root => root.GetComponentsInChildren<Checkpoint2D>(true)))
            Require(PrefabUtility.GetPrefabInstanceStatus(checkpoint.gameObject) == PrefabInstanceStatus.Connected,
                $"{checkpoint.name} must remain connected to Checkpoint2D.prefab.");
        foreach (RoomExit2D exit in scene.GetRootGameObjects()
                     .SelectMany(root => root.GetComponentsInChildren<RoomExit2D>(true)))
            Require(PrefabUtility.GetPrefabInstanceStatus(exit.gameObject) == PrefabInstanceStatus.Connected,
                $"{exit.name} must remain connected to RoomExit2D.prefab.");
    }

    private static void AddBuildScene(string path)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(entry => entry.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
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
}
