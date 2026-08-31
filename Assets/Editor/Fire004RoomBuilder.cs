using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds the approved FIRE_004 fireball-latch door room from reusable prefabs.</summary>
public static class Fire004RoomBuilder
{
    public const string ScenePath = "Assets/Scenes/Levels/Fire/Fire_004.unity";
    private const string TilePalettePath = "Assets/TilePalettes/Fire.prefab";
    private const string TerrainTilePath = "Assets/Tiles/Graybox/Fire004Terrain.asset";
    private const string HintTilePath = "Assets/Tiles/Graybox/Fire004MirrorHint.asset";
    private const string TerrainTexturePath = "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_sand_block_center.png";
    private const string PlatePath = "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab";
    private const string DoorPath = "Assets/Prefabs/Gameplay/Doors/Door2D.prefab";
    private const string EnemyPath = "Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab";
    private const string ExitPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";
    private const string FarBackdropPath =
        "Assets/Art/Fire/Backgrounds/fire_cavern_far_background_v1.png";
    private const string RockSilhouettesPath =
        "Assets/Art/Generated/Fire/fire_parallax_rock_silhouettes_handpainted_v1.png";
    private const string MidgroundRuinsPath =
        "Assets/Art/Generated/Fire/fire_midground_ruins_machinery_handpainted_v1.png";
    private const string ForegroundEdgesPath =
        "Assets/Art/Generated/Fire/fire_foreground_edge_modules_handpainted_v1.png";

    [MenuItem("Tools/W1/Build FIRE-004 Greybox")]
    public static void BuildFromMenu() => Build();

    [MenuItem("Tools/W1/Fix FIRE-004 Exit Gaps")]
    public static void FixExitGaps()
    {
        Scene scene = SceneManager.GetActiveScene();
        Require(scene.path == ScenePath, "Open FIRE_004 before fixing its exit gaps.");
        Tilemap terrain = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
            .Single(map => map.name == "Terrain");
        Undo.RecordObject(terrain, "Open FIRE-004 exit gaps");
        terrain.SetTile(new Vector3Int(-12, -2, 0), null);
        terrain.SetTile(new Vector3Int(-12, -1, 0), null);
        terrain.SetTile(new Vector3Int(11, 0, 0), null);
        terrain.SetTile(new Vector3Int(11, 1, 0), null);
        terrain.CompressBounds();
        terrain.RefreshAllTiles();
        terrain.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        terrain.GetComponent<CompositeCollider2D>().GenerateGeometry();
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save FIRE_004 exit gaps.");
    }

    [MenuItem("Tools/W1/Apply FIRE-004 Layered Visuals")]
    public static void ApplyLayeredVisuals()
    {
        Scene scene = SceneManager.GetActiveScene();
        Require(scene.path == ScenePath, "Open FIRE_004 before applying its layered visuals.");
        GameObject room = scene.GetRootGameObjects()
            .Single(root => root.name == "FIRE_004 Borrowed Fire Door");
        Camera camera = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
            .Single();
        Tilemap terrain = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
            .Single(map => map.name == "Terrain");

        Transform existing = room.transform.Find("EnvironmentVisuals");
        if (existing != null)
        {
            ValidateVisualOnly(existing);
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        GameObject visuals = CreateEnvironmentVisuals(room.transform, camera.transform);
        Undo.RegisterCreatedObjectUndo(visuals, "Apply FIRE-004 layered visuals");
        Undo.RecordObject(terrain, "Tint FIRE-004 terrain");
        terrain.color = new Color(.38f, .25f, .24f, 1f);
        ValidateEnvironmentVisuals(scene, camera, terrain);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save FIRE_004 layered visuals.");
    }

    public static void Build()
    {
        Directory.CreateDirectory("Assets/Scenes/Levels/Fire");
        Directory.CreateDirectory("Assets/Tiles/Graybox");
        GameObject platePrefab = RequireAsset(PlatePath);
        GameObject doorPrefab = RequireAsset(DoorPath);
        GameObject enemyPrefab = RequireAsset(EnemyPath);
        GameObject exitPrefab = RequireAsset(ExitPath);
        Sprite terrainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TerrainTexturePath);
        Require(terrainSprite != null, $"Missing terrain sprite: {TerrainTexturePath}");
        Sprite builtin = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Tile terrainTile = MakeTile(TerrainTilePath, terrainSprite, Color.white, Tile.ColliderType.Grid);
        Tile hintTile = MakeTile(HintTilePath, builtin, new Color(.15f, .9f, 1f, .75f), Tile.ColliderType.None);
        TilePaletteAuthoring.EnsureTiles(TilePalettePath, terrainTile, hintTile);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = new("FIRE_004 Borrowed Fire Door");
        GameObject gridObject = Child(room.transform, "Grid");
        gridObject.AddComponent<Grid>();
        CreateLayer(gridObject.transform, "Background");
        Tilemap terrain = CreateLayer(gridObject.transform, "Terrain");
        ConfigureTerrain(terrain);
        terrain.color = new Color(.38f, .25f, .24f, 1f);
        CreateLayer(gridObject.transform, "OneWayPlatform");
        CreateLayer(gridObject.transform, "SpecialMirrorWall");
        CreateLayer(gridObject.transform, "Hazard");
        Tilemap decoration = CreateLayer(gridObject.transform, "Decoration");
        CreateLayer(gridObject.transform, "Foreground");
        Fill(terrain, terrainTile, -12, -8, -3, -3);
        Fill(terrain, terrainTile, -8, -7, -2, -2);
        Fill(terrain, terrainTile, -7, 11, -1, -1);
        Fill(terrain, terrainTile, -12, 11, 6, 6);
        Fill(terrain, terrainTile, -12, -12, -3, -3);
        Fill(terrain, terrainTile, -12, -12, 0, 6);
        Fill(terrain, terrainTile, 11, 11, -1, -1);
        Fill(terrain, terrainTile, 11, 11, 2, 6);
        Fill(terrain, terrainTile, 8, 8, 2, 6);
        decoration.SetTile(new Vector3Int(0, 0, 0), hintTile);
        terrain.CompressBounds();
        terrain.RefreshAllTiles();
        terrain.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        terrain.GetComponent<CompositeCollider2D>().GenerateGeometry();

        GameObject gameplay = Child(room.transform, "Gameplay");
        Transform dynamicRoot = Child(gameplay.transform, "DynamicObjects").transform;
        Transform entrances = Child(gameplay.transform, "Entrances").transform;
        Transform exits = Child(gameplay.transform, "Exits").transform;
        Transform entrance = Marker("EntranceFromFIRE003", new Vector3(-10.5f, -1.08f), entrances);
        PlayerRoomAuthoring.ConfigureEntrance(entrance, "DEFAULT", true, true);
        Transform returnEntrance = Marker("EntranceFromFIRE005", new Vector3(10.5f, .92f), entrances);
        PlayerRoomAuthoring.ConfigureEntrance(returnEntrance, "FROM_FIRE_005", false, false);

        GameObject plateObject = Instance(platePrefab, dynamicRoot, "Latch-A", new Vector2(5.5f, .625f));
        plateObject.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        PressurePlate2D plate = plateObject.GetComponent<PressurePlate2D>();
        plate.ConfigureActivationMode(PressurePlate2D.ActivationMode.FireballLatch);
        Record(plate);
        PrefabUtility.RecordPrefabInstancePropertyModifications(plateObject.transform);

        GameObject enemyObject = Instance(enemyPrefab, dynamicRoot, "Enemy-H1", new Vector2(2.5f, .5f));
        HorizontalFireballEnemy2D enemy = enemyObject.GetComponent<HorizontalFireballEnemy2D>();
        Require(enemy != null, "Horizontal fireball enemy Prefab is missing its runtime component.");
        enemy.SetInitiallyFacingRight(true);
        Record(enemy);

        GameObject doorObject = Instance(doorPrefab, dynamicRoot, "Door-A", new Vector2(8.5f, 1f));
        Door2D door = doorObject.GetComponent<Door2D>();
        door.ConfigureControlSource(plate);
        Record(door);
        GameObject backExitObject = Instance(exitPrefab, exits, "Exit-Back-to-FIRE003", new Vector2(-11.5f, -1.1f));
        RoomExit2D backExit = backExitObject.GetComponent<RoomExit2D>();
        backExit.Configure("Fire_003", "FROM_FIRE_004");
        Record(backExit);
        GameObject forwardExitObject = Instance(exitPrefab, exits, "Exit-To-FIRE005", new Vector2(11.5f, .9f));
        RoomExit2D forwardExit = forwardExitObject.GetComponent<RoomExit2D>();
        forwardExit.Configure("Fire_005", "DEFAULT");
        Record(forwardExit);

        Camera camera = CreateCamera();
        CreateEnvironmentVisuals(room.transform, camera.transform);
        GameObject lightObject = new("Main Light");
        lightObject.transform.SetParent(room.transform);
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = .85f;
        GameObject systems = Child(room.transform, "RoomSystems");
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, null, true);

        Validate(scene, terrain, plate, door, enemy, backExit, forwardExit, camera);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save FIRE_004.");
        AddBuildScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Camera CreateCamera()
    {
        GameObject go = new("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 2f, -10f);
        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.backgroundColor = new Color(.055f, .025f, .02f);
        return camera;
    }

    private static Tilemap CreateLayer(Transform parent, string name)
    {
        GameObject go = Child(parent, name);
        Tilemap map = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();
        return map;
    }

    private static void ConfigureTerrain(Tilemap map)
    {
        Rigidbody2D body = map.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        map.gameObject.AddComponent<CompositeCollider2D>();
        TilemapCollider2D collider = map.gameObject.AddComponent<TilemapCollider2D>();
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        SurfaceSemantic2D semantic = map.gameObject.AddComponent<SurfaceSemantic2D>();
        semantic.Configure(SurfaceSemantic2D.SurfaceType.StaticSolid, true, true);
        MirrorSurface2D mirrorSurface = map.gameObject.AddComponent<MirrorSurface2D>();
        mirrorSurface.kind = MirrorSurface2D.SurfaceKind.Ground;
    }

    private static Tile MakeTile(string path, Sprite sprite, Color color, Tile.ColliderType colliderType)
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
        tile.sprite = sprite;
        tile.color = color;
        tile.colliderType = colliderType;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void Validate(Scene scene, Tilemap terrain, PressurePlate2D plate, Door2D door,
        HorizontalFireballEnemy2D enemy, RoomExit2D backExit, RoomExit2D forwardExit, Camera camera)
    {
        Require(terrain.GetComponent<CompositeCollider2D>().pathCount > 0, "Terrain collider geometry is empty.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0, "Scene must not serialize Player.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1, "Scene needs one player spawner.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PressurePlate2D>(true)).Count() == 1, "FIRE_004 needs one plate.");
        Require(plate.Mode == PressurePlate2D.ActivationMode.FireballLatch, "Latch-A must use FireballLatch mode.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Door2D>(true)).Count() == 1, "FIRE_004 needs one door.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<HorizontalFireballEnemy2D>(true)).Count() == 1, "FIRE_004 needs one thrower.");
        Require(enemy != null, "Enemy-H1 is missing.");
        Require(door.ControlSource == plate, "Door-A must be controlled by Latch-A.");
        Require(backExit.TargetScene == "Fire_003" && backExit.TargetEntranceId == "FROM_FIRE_004", "Return exit target mismatch.");
        Require(forwardExit.TargetScene == "Fire_005" && forwardExit.TargetEntranceId == "DEFAULT", "Forward exit target mismatch.");
        Require(scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<RoomExit2D>(true)).Count() == 2, "FIRE_004 needs two exits.");
        Require(camera.GetComponent<CameraFollow2D>() == null, "FIRE_004 must use a fixed camera.");
        ValidateEnvironmentVisuals(scene, camera, terrain);
    }

    private static GameObject CreateEnvironmentVisuals(Transform room, Transform cameraTransform)
    {
        GameObject root = Child(room, "EnvironmentVisuals");
        root.transform.SetSiblingIndex(0);

        Transform backdrop = CreateParallaxLayer(root.transform, "01 Color and Fog Backdrop",
            cameraTransform, 1f);
        CreateSprite(backdrop, "Backdrop_DarkFireCavern",
            LoadSprite(FarBackdropPath, "fire_cavern_far_background_v1_0"),
            new Vector2(0f, 2f), 1.6f, new Color(.72f, .67f, .72f, 1f), -100);

        Transform extremeFar = CreateParallaxLayer(root.transform, "02 Extreme Far Contours",
            cameraTransform, .95f);
        CreateSprite(extremeFar, "ExtremeFar_CeilingLeft",
            LoadSprite(RockSilhouettesPath, "fire_parallax_rock_silhouettes_handpainted_v1_0"),
            new Vector2(-8.7f, 7.25f), 1.12f, new Color(.35f, .27f, .38f, .38f), -82);
        CreateSprite(extremeFar, "ExtremeFar_CeilingRight",
            LoadSprite(RockSilhouettesPath, "fire_parallax_rock_silhouettes_handpainted_v1_2"),
            new Vector2(8.15f, 7.15f), .84f, new Color(.33f, .25f, .36f, .36f), -81);

        Transform far = CreateParallaxLayer(root.transform, "03 Far Environment",
            cameraTransform, .85f);
        CreateSprite(far, "Far_RockMassLeft",
            LoadSprite(RockSilhouettesPath, "fire_parallax_rock_silhouettes_handpainted_v1_10"),
            new Vector2(-8f, -2.15f), .9f, new Color(.44f, .33f, .43f, .38f), -56);
        CreateSprite(far, "Far_RockMassRight",
            LoadSprite(RockSilhouettesPath, "fire_parallax_rock_silhouettes_handpainted_v1_11"),
            new Vector2(7.8f, -2.05f), 1.05f, new Color(.42f, .31f, .41f, .36f), -55);

        Transform mid = CreateParallaxLayer(root.transform, "04 Mid Environment",
            cameraTransform, .65f);
        CreateSprite(mid, "Mid_RuinedPillar",
            LoadSprite(MidgroundRuinsPath, "fire_midground_ruins_machinery_handpainted_v1_6"),
            new Vector2(-5.9f, .8f), .72f, new Color(.46f, .36f, .48f, .58f), -32);
        CreateSprite(mid, "Mid_OverheadPipe",
            LoadSprite(MidgroundRuinsPath, "fire_midground_ruins_machinery_handpainted_v1_1"),
            new Vector2(6.9f, 4.65f), .82f, new Color(.45f, .35f, .47f, .56f), -31);

        CreateParallaxLayer(root.transform, "05 Rear Dynamic Fog (Reserved)", cameraTransform, .8f);
        CreateParallaxLayer(root.transform, "07 Front Dynamic Fog and Particles (Reserved)",
            cameraTransform, .35f);

        Transform foreground = CreateParallaxLayer(root.transform, "08 Foreground Occlusion",
            cameraTransform, .2f);
        CreateSprite(foreground, "Foreground_LeftEdge",
            LoadSprite(ForegroundEdgesPath, "fire_foreground_edge_modules_handpainted_v1_0"),
            new Vector2(-13.2f, 2f), .74f, new Color(.38f, .30f, .36f, .82f), 31);
        CreateSprite(foreground, "Foreground_RightEdge",
            LoadSprite(ForegroundEdgesPath, "fire_foreground_edge_modules_handpainted_v1_4"),
            new Vector2(13.45f, 2.2f), .4f, new Color(.37f, .29f, .35f, .8f), 32);

        return root;
    }

    private static Transform CreateParallaxLayer(Transform parent, string name,
        Transform cameraTransform, float followFactor)
    {
        GameObject layer = Child(parent, name);
        layer.AddComponent<ParallaxLayer2D>().Configure(cameraTransform, followFactor, true, false);
        return layer.transform;
    }

    private static void CreateSprite(Transform parent, string name, Sprite sprite, Vector2 position,
        float uniformScale, Color color, int sortingOrder)
    {
        GameObject visual = Child(parent, name);
        visual.transform.localPosition = new Vector3(position.x, position.y, 0f);
        visual.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private static Sprite LoadSprite(string path, string spriteName)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .SingleOrDefault(candidate => candidate.name == spriteName);
        Require(sprite != null, $"Missing sprite '{spriteName}' in {path}.");
        return sprite;
    }

    private static void ValidateEnvironmentVisuals(Scene scene, Camera camera, Tilemap terrain)
    {
        GameObject environment = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(item => item.name == "EnvironmentVisuals")
            .Select(item => item.gameObject)
            .Single();
        ValidateVisualOnly(environment.transform);

        string[] names =
        {
            "01 Color and Fog Backdrop",
            "02 Extreme Far Contours",
            "03 Far Environment",
            "04 Mid Environment",
            "05 Rear Dynamic Fog (Reserved)",
            "07 Front Dynamic Fog and Particles (Reserved)",
            "08 Foreground Occlusion"
        };
        float[] factors = { 1f, .95f, .85f, .65f, .8f, .35f, .2f };
        for (int i = 0; i < names.Length; i++)
        {
            Transform layer = environment.transform.Find(names[i]);
            Require(layer != null, $"FIRE_004 is missing visual layer '{names[i]}'.");
            ParallaxLayer2D parallax = layer.GetComponent<ParallaxLayer2D>();
            Require(parallax != null && Mathf.Approximately(parallax.CameraFollowFactor, factors[i]) &&
                    parallax.FollowsHorizontal && !parallax.FollowsVertical,
                $"FIRE_004 visual layer '{names[i]}' has an invalid parallax configuration.");
            SerializedObject serializedParallax = new SerializedObject(parallax);
            Require(serializedParallax.FindProperty("cameraTransform").objectReferenceValue == camera.transform,
                $"FIRE_004 visual layer '{names[i]}' must explicitly reference Main Camera.");
        }

        Require(environment.GetComponentsInChildren<ParallaxLayer2D>(true).Length == names.Length,
            "FIRE_004 must have exactly seven non-gameplay parallax layers.");
        Require(environment.GetComponentsInChildren<SpriteRenderer>(true).Length == 9,
            "FIRE_004 layered environment must contain exactly nine visual modules.");
        Require(scene.GetRootGameObjects().SelectMany(root =>
                root.GetComponentsInChildren<Transform>(true))
            .Single(item => item.name == "Gameplay")
            .GetComponent<ParallaxLayer2D>() == null,
            "FIRE_004 gameplay layer must remain in world space.");
        Require(terrain.transform.parent != null && terrain.transform.parent.name == "Grid" &&
                terrain.transform.parent.parent != null &&
                terrain.transform.parent.parent.name == "FIRE_004 Borrowed Fire Door",
            "FIRE_004 terrain must remain under the room Grid, outside EnvironmentVisuals.");
        Color expectedTerrainColor = new Color(.38f, .25f, .24f, 1f);
        Require(Vector4.Distance(terrain.color, expectedTerrainColor) < .001f,
            "FIRE_004 terrain tint must preserve the approved dark gameplay silhouette.");

        SpriteRenderer backdrop = environment.transform
            .Find("01 Color and Fog Backdrop/Backdrop_DarkFireCavern")
            .GetComponent<SpriteRenderer>();
        float requiredWidth = camera.orthographicSize * 2f * (16f / 9f) + 1f;
        float requiredHeight = camera.orthographicSize * 2f + 1f;
        Require(backdrop.bounds.size.x >= requiredWidth && backdrop.bounds.size.y >= requiredHeight,
            "FIRE_004 backdrop does not cover the 16:9 fixed-camera viewport plus padding.");

        foreach (SpriteRenderer renderer in environment.transform
                     .Find("08 Foreground Occlusion")
                     .GetComponentsInChildren<SpriteRenderer>(true))
        {
            Require(renderer.bounds.max.x <= -11.75f || renderer.bounds.min.x >= 11.75f,
                $"Foreground module '{renderer.name}' intrudes into the gameplay-readable window.");
        }
    }

    private static void ValidateVisualOnly(Transform root)
    {
        Require(root.GetComponentsInChildren<Component>(true).All(component =>
                component is Transform || component is SpriteRenderer ||
                component is ParallaxLayer2D),
            "EnvironmentVisuals may contain only Transform, SpriteRenderer, and ParallaxLayer2D components.");
        Require(root.GetComponentsInChildren<Rigidbody2D>(true).Length == 0,
            "EnvironmentVisuals must not contain Rigidbody2D components.");
        Require(root.GetComponentsInChildren<Collider2D>(true).Length == 0,
            "EnvironmentVisuals must not contain Collider2D components or Triggers.");
        Require(root.GetComponentsInChildren<SurfaceSemantic2D>(true).Length == 0,
            "EnvironmentVisuals must not contain surface semantics.");
        Require(root.GetComponentsInChildren<MirrorSurface2D>(true).Length == 0,
            "EnvironmentVisuals must not contain mirror surfaces.");
    }

    private static GameObject Child(Transform parent, string name) { GameObject go = new(name); go.transform.SetParent(parent, false); return go; }
    private static Transform Marker(string name, Vector3 position, Transform parent) { GameObject go = Child(parent, name); go.transform.position = position; return go.transform; }
    private static GameObject Instance(GameObject prefab, Transform parent, string name, Vector2 position) { GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name = name; go.transform.SetParent(parent, false); go.transform.position = position; return go; }
    private static void Record(Component component) { EditorUtility.SetDirty(component); PrefabUtility.RecordPrefabInstancePropertyModifications(component); }
    private static GameObject RequireAsset(string path) { GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path); Require(asset != null, $"Missing prefab: {path}"); return asset; }
    private static void Fill(Tilemap map, TileBase tile, int minX, int maxX, int minY, int maxY) { for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++) map.SetTile(new Vector3Int(x, y), tile); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void AddBuildScene()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Any(x => x.path == ScenePath)) return;
        EditorBuildSettings.scenes = scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
    }
}
