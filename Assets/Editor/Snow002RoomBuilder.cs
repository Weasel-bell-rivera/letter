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
/// Builds the approved SNOW_002 horizontal FrozenGround prototype.
/// The room contains no room-specific runtime behaviour.
/// </summary>
public static class Snow002RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Snow/Snow_002.unity";
    public const string FrozenGroundTilePath = "Assets/Tiles/Snow/FrozenGroundSnowBlock.asset";
    public const string MovingPlatformPrefabPath =
        "Assets/Prefabs/Gameplay/Platforms/MovingPlatform2D.prefab";

    public const int IceMinX = -12;
    public const int IceMaxX = 11;
    public const int IceY = -3;
    public static readonly Rect CameraBounds = new(-12f, -7f, 24f, 14f);
    public const float CameraOrthographicSize = 7f;
    public const float CameraSmoothTime = .15f;


    [MenuItem("Tools/W1/Build SNOW-002 Frozen Ground Prototype")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    [MenuItem("Tools/W1/Add Center Moving Platform to SNOW-002")]
    public static void AddMovingPlatformToExistingScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MovingPlatformPrefabPath);
        Require(prefab != null, $"Missing moving platform Prefab: {MovingPlatformPrefabPath}");

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            GameObject room = scene.GetRootGameObjects().Single(root =>
                root.name == "SNOW_002 Horizontal Ice Prototype");
            Transform gameplay = room.transform.Find("Gameplay");
            Require(gameplay != null, "SNOW_002 Gameplay root is missing.");
            Transform dynamicObjects = gameplay.Find("DynamicObjects");
            if (dynamicObjects == null)
            {
                GameObject dynamicRoot = new("DynamicObjects");
                SceneManager.MoveGameObjectToScene(dynamicRoot, scene);
                dynamicRoot.transform.SetParent(gameplay, false);
                dynamicObjects = dynamicRoot.transform;
            }

            bool alreadyExists = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MovingPlatform2D>(true)).Any();
            if (!alreadyExists)
                CreateCenterMovingPlatform(prefab, dynamicObjects, scene);

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene), "Failed to save SNOW_002 moving platform.");
        }
        finally
        {
            if (openedHere && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    // Entry point for: Unity -batchmode -executeMethod Snow002RoomBuilder.BuildFromCommandLine
    public static void BuildFromCommandLine()
    {
        Directory.CreateDirectory("Assets/Scenes/Levels/Snow");

        Tile frozenGroundTile = AssetDatabase.LoadAssetAtPath<Tile>(FrozenGroundTilePath);
        GameObject movingPlatformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MovingPlatformPrefabPath);

        Require(frozenGroundTile != null, $"Missing FrozenGround Tile asset: {FrozenGroundTilePath}");
        Require(frozenGroundTile.sprite != null, "FrozenGround Tile must use the imported snow Sprite.");
        Require(frozenGroundTile.colliderType != Tile.ColliderType.None,
            "FrozenGround Tile must provide collider geometry.");
        Require(movingPlatformPrefab != null, $"Missing moving platform Prefab: {MovingPlatformPrefabPath}");

        BuildScene(frozenGroundTile, movingPlatformPrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SNOW_002 horizontal FrozenGround prototype built successfully.");
    }

    private static void BuildScene(TileBase frozenGroundTile, GameObject movingPlatformPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = new("SNOW_002 Horizontal Ice Prototype");

        GameObject gridObject = new("Grid");
        gridObject.transform.SetParent(room.transform);
        Grid grid = gridObject.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        Tilemap frozenGround = CreateTilemapLayer(gridObject.transform, "FrozenGround");
        ConfigureFrozenGround(frozenGround);
        FillHorizontalIce(frozenGround, frozenGroundTile);
        BakeColliderGeometry(frozenGround);

        GameObject gameplay = new("Gameplay");
        gameplay.transform.SetParent(room.transform);
        GameObject dynamicObjects = new("DynamicObjects");
        dynamicObjects.transform.SetParent(gameplay.transform);
        CreateCenterMovingPlatform(movingPlatformPrefab, dynamicObjects.transform, scene);
        GameObject entrances = new("Entrances");
        entrances.transform.SetParent(gameplay.transform);
        Transform entrance = Marker("PrototypeEntrance", new Vector3(-9.5f, -1.08f, 0f), entrances.transform);

        Camera camera = CreateCamera(entrance.position);
        CameraFollow2D cameraFollow = camera.gameObject.AddComponent<CameraFollow2D>();
        BindRuntimeScript(cameraFollow, "Assets/Scripts/Gameplay/CameraFollow2D.cs");
        cameraFollow.Configure(null, true);
        cameraFollow.ConfigureDamping(CameraSmoothTime);
        cameraFollow.ConfigureBounds(CameraBounds);

        GameObject systems = new("RoomSystems");
        systems.transform.SetParent(room.transform);
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        BindRuntimeScript(reset, "Assets/Scripts/Core/RoomResetSystem.cs");
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, cameraFollow);

        ValidateSceneOrThrow(scene, frozenGround);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save SNOW_002 scene.");
        AddBuildScene(ScenePath);
    }

    private static Tilemap CreateTilemapLayer(Transform parent, string name)
    {
        GameObject layer = new(name);
        layer.transform.SetParent(parent, false);
        Tilemap map = layer.AddComponent<Tilemap>();
        layer.AddComponent<TilemapRenderer>();
        return map;
    }

    private static void ConfigureFrozenGround(Tilemap frozenGround)
    {
        Rigidbody2D body = frozenGround.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;

        frozenGround.gameObject.AddComponent<CompositeCollider2D>();
        TilemapCollider2D collider = frozenGround.gameObject.AddComponent<TilemapCollider2D>();
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;

        SurfaceSemantic2D semantic = frozenGround.gameObject.AddComponent<SurfaceSemantic2D>();
        BindRuntimeScript(semantic, "Assets/Scripts/Gameplay/SurfaceSemantic2D.cs");
        semantic.Configure(SurfaceSemantic2D.SurfaceType.FrozenGround, true, true);

        MirrorSurface2D mirrorSurface = frozenGround.gameObject.AddComponent<MirrorSurface2D>();
        BindRuntimeScript(mirrorSurface, "Assets/Scripts/Gameplay/MirrorSurface2D.cs");
        mirrorSurface.kind = MirrorSurface2D.SurfaceKind.Ground;
        mirrorSurface.safe = true;
    }

    private static void FillHorizontalIce(Tilemap frozenGround, TileBase tile)
    {
        int width = IceMaxX - IceMinX + 1;
        BoundsInt bounds = new(IceMinX, IceY, 0, width, 1, 1);
        TileBase[] tiles = Enumerable.Repeat(tile, width).ToArray();
        frozenGround.SetTilesBlock(bounds, tiles);
    }

    private static void BakeColliderGeometry(Tilemap frozenGround)
    {
        frozenGround.CompressBounds();
        frozenGround.RefreshAllTiles();
        TilemapCollider2D tilemapCollider = frozenGround.GetComponent<TilemapCollider2D>();
        CompositeCollider2D composite = frozenGround.GetComponent<CompositeCollider2D>();
        tilemapCollider.ProcessTilemapChanges();
        Physics2D.SyncTransforms();
        composite.GenerateGeometry();
        Require(composite.pathCount > 0,
            "The FrozenGround strip must bake valid composite collider geometry.");
    }

    private static Camera CreateCamera(Vector3 playerPosition)
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(playerPosition.x, playerPosition.y, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = CameraOrthographicSize;
        camera.backgroundColor = new Color(.72f, .86f, .94f);
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private static SpriteRenderer Visual(string name, Transform parent, Vector2 worldSize, Color color)
    {
        GameObject visual = new(name);
        visual.transform.SetParent(parent, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        renderer.color = color;
        Vector2 native = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(worldSize.x / native.x, worldSize.y / native.y, 1f);
        return renderer;
    }

    private static Transform Marker(string name, Vector3 position, Transform parent)
    {
        GameObject marker = new(name);
        marker.transform.SetParent(parent);
        marker.transform.position = position;
        return marker.transform;
    }

    private static void ValidateSceneOrThrow(Scene scene, Tilemap frozenGround)
    {
        for (int x = IceMinX; x <= IceMaxX; x++)
            Require(frozenGround.HasTile(new Vector3Int(x, IceY, 0)), $"FrozenGround gap at x={x}.");

        SurfaceSemantic2D semantic = frozenGround.GetComponent<SurfaceSemantic2D>();
        Require(semantic != null && semantic.Type == SurfaceSemantic2D.SurfaceType.FrozenGround &&
                semantic.IsStatic && semantic.IsSafe,
            "FrozenGround must expose the explicit static and safe FrozenGround semantic.");
        Require(frozenGround.GetComponent<Rigidbody2D>()?.bodyType == RigidbodyType2D.Static,
            "FrozenGround Rigidbody2D must be Static.");
        Require(frozenGround.GetComponent<TilemapCollider2D>()?.compositeOperation ==
                Collider2D.CompositeOperation.Merge,
            "FrozenGround TilemapCollider2D must merge into its CompositeCollider2D.");
        Require(frozenGround.GetComponent<CompositeCollider2D>() != null,
            "FrozenGround must provide a CompositeCollider2D.");

        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "SNOW_002 prototype must not serialize a room-local Player.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "SNOW_002 prototype must contain exactly one RoomPlayerSpawner2D.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomResetSystem>(true)).Count() == 1,
            "SNOW_002 prototype must contain exactly one RoomResetSystem.");
        Camera camera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Single();
        CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
        Require(camera.orthographic && Mathf.Approximately(camera.orthographicSize, CameraOrthographicSize) &&
                follow != null && follow.FollowsVertical && follow.UsesRoomBounds &&
                follow.RoomBounds == CameraBounds && Mathf.Approximately(follow.SmoothTime, CameraSmoothTime),
            "SNOW_002 must use the approved bounded Player-follow camera.");
        MovingPlatform2D platform = roots.SelectMany(root =>
            root.GetComponentsInChildren<MovingPlatform2D>(true)).Single();
        Require(PrefabUtility.GetPrefabInstanceStatus(platform.gameObject) == PrefabInstanceStatus.Connected,
            "SNOW_002 moving platform must remain connected to its shared Prefab.");
    }

    private static void CreateCenterMovingPlatform(GameObject prefab, Transform parent, Scene scene)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "MovingPlatform-Center";
        instance.transform.SetParent(parent, false);
        instance.transform.position = Vector3.zero;
        MovingPlatform2D platform = instance.GetComponent<MovingPlatform2D>();
        platform.ConfigurePath(new Vector2(-2f, 0f), new Vector2(2f, 0f), 2f, .35f,
            .5f, true, true);
        EditorUtility.SetDirty(platform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(platform);
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
