using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds the formal snow-summit visual test room from documented Snow-region systems.</summary>
public static class Test005SnowSummitSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Tests/test005.unity";
    public static readonly Rect CameraBounds = new(-18f, -7f, 36f, 18f);
    public const float OrthographicSize = 7f;
    public const float CameraSmoothTime = .15f;

    private const string FarPath = "Assets/Art/Snow/Backgrounds/snow_summit_far_background_v2.png";
    private const string MidPath = "Assets/Art/Snow/Backgrounds/snow_summit_mid_ridge_v2.png";
    private const string ForegroundPath = "Assets/Art/Snow/Backgrounds/snow_summit_foreground_frame_v2.png";
    private const string TreePath = "Assets/Art/Snow/Decorations/snow_windswept_tree_v2.png";
    private const string TerrainTexturePath = "Assets/Art/Snow/Tiles/snow_safe_ground_tile_v2.png";
    private const string TerrainBodyTexturePath = "Assets/Art/Snow/Tiles/snow_safe_ground_body_tile_v2.png";
    private const string TerrainTilePath = "Assets/Tiles/Snow/Test005SafeSnowTerrain.asset";
    private const string TerrainBodyTilePath = "Assets/Tiles/Snow/Test005SafeSnowTerrainBody.asset";
    private const string VolumeProfilePath = "Assets/Materials/Tests/Test005/Test005SnowAtmosphere.asset";

    private const int FarSortingOrder = -40;
    private const int MidSortingOrder = -24;
    private const int FocusSortingOrder = -4;
    private const int ForegroundSortingOrder = 30;

    [MenuItem("Tools/W1/Build test005 Snow Summit")]
    public static void BuildFromMenu()
    {
        Directory.CreateDirectory("Assets/Scenes/Tests");
        Directory.CreateDirectory("Assets/Tiles/Snow");
        Directory.CreateDirectory("Assets/Materials/Tests/Test005");
        Sprite farSprite = ImportSprite(FarPath, 100f, false);
        Sprite midSprite = ImportSprite(MidPath, 100f, true);
        Sprite foregroundSprite = ImportSprite(ForegroundPath, 100f, true);
        Sprite treeSprite = ImportSprite(TreePath, 220f, true);
        Sprite terrainSprite = ImportSprite(TerrainTexturePath, 1256f, false);
        Sprite terrainBodySprite = ImportSprite(TerrainBodyTexturePath, 1256f, false);
        Tile terrainTile = CreateOrUpdateTile(TerrainTilePath, terrainSprite, Color.white);
        Tile terrainBodyTile = CreateOrUpdateTile(TerrainBodyTilePath, terrainBodySprite, Color.white);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = new("test005 - Snow Summit");
        CreateEnvironment(room.transform, farSprite, midSprite, foregroundSprite, treeSprite);
        Tilemap terrain = CreateGeometry(room.transform, terrainTile, terrainBodyTile);
        CameraFollow2D cameraFollow = CreateCamera(room.transform);
        CreateLight(room.transform);
        CreatePostProcessing(room.transform);
        CreateSnowLayers(room.transform);
        CreateRuntimeSystems(room.transform, cameraFollow);

        Validate(scene, terrain, cameraFollow);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save test005 scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("test005 rebuilt with modular Snow art, Tilemap terrain, and a bounded follow camera.");
    }

    private static void CreateEnvironment(
        Transform parent, Sprite far, Sprite mid, Sprite foreground, Sprite tree)
    {
        Transform environment = Child(parent, "Environment").transform;
        SpriteObject(environment, "Far Background - Alpine Sky", far, new Vector3(0f, 2f, 5f),
            new Vector2(50f, 28.13f), new Color(.86f, .9f, .93f, 1f), FarSortingOrder);
        SpriteObject(environment, "Midground - Distant Ridge", mid, new Vector3(3.5f, -1.8f, 3f),
            new Vector2(36f, 16.35f), new Color(.34f, .42f, .5f, .43f), MidSortingOrder);
        SpriteObject(environment, "Decorative Focus - Windswept Tree", tree,
            new Vector3(4.1f, -1.62f, -.5f), new Vector2(5.3f, 5.56f),
            new Color(.28f, .34f, .4f, .94f), FocusSortingOrder);
        SpriteObject(environment, "Foreground - Snow Rock Frame", foreground, new Vector3(0f, 0f, -3f),
            new Vector2(36f, 20.27f), new Color(.28f, .34f, .4f, .92f), ForegroundSortingOrder);
    }

    private static Tilemap CreateGeometry(Transform parent, TileBase terrainTile, TileBase terrainBodyTile)
    {
        GameObject gridObject = Child(parent, "Grid");
        gridObject.AddComponent<Grid>().cellSize = Vector3.one;
        CreateTilemapLayer(gridObject.transform, "Background");
        Tilemap terrain = CreateTilemapLayer(gridObject.transform, "Terrain");
        CreateTilemapLayer(gridObject.transform, "FrozenGround");
        CreateTilemapLayer(gridObject.transform, "FreezingGround");
        CreateTilemapLayer(gridObject.transform, "OneWayPlatform");
        CreateTilemapLayer(gridObject.transform, "SpecialMirrorWall");
        CreateTilemapLayer(gridObject.transform, "Hazard");
        CreateTilemapLayer(gridObject.transform, "Decoration");
        CreateTilemapLayer(gridObject.transform, "Foreground");

        Rigidbody2D body = terrain.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        terrain.gameObject.AddComponent<CompositeCollider2D>();
        TilemapCollider2D collider = terrain.gameObject.AddComponent<TilemapCollider2D>();
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        terrain.gameObject.AddComponent<SurfaceSemantic2D>()
            .Configure(SurfaceSemantic2D.SurfaceType.StaticSolid, true, true);
        MirrorSurface2D mirrorSurface = terrain.gameObject.AddComponent<MirrorSurface2D>();
        mirrorSurface.kind = MirrorSurface2D.SurfaceKind.Ground;
        mirrorSurface.safe = true;
        Fill(terrain, terrainBodyTile, -19, 18, -7, -6);
        Fill(terrain, terrainTile, -19, 18, -5, -5);
        terrain.CompressBounds();
        terrain.RefreshAllTiles();
        collider.ProcessTilemapChanges();
        terrain.GetComponent<CompositeCollider2D>().GenerateGeometry();
        return terrain;
    }

    private static CameraFollow2D CreateCamera(Transform parent)
    {
        GameObject go = Child(parent, "Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(-5.5f, 0f, -10f);
        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = OrthographicSize;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.52f, .62f, .7f, 1f);
        camera.allowHDR = true;
        go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        go.AddComponent<AudioListener>();
        CameraFollow2D follow = go.AddComponent<CameraFollow2D>();
        follow.Configure(null, true);
        follow.ConfigureDamping(CameraSmoothTime);
        follow.ConfigureBounds(CameraBounds);
        follow.ConfigureEntryFramingBounds(CameraBounds);
        return follow;
    }

    private static void CreateRuntimeSystems(Transform parent, CameraFollow2D cameraFollow)
    {
        Transform gameplay = Child(parent, "Gameplay").transform;
        Transform entrances = Child(gameplay, "Entrances").transform;
        Transform entrance = Child(entrances, "Entrance-DEFAULT").transform;
        entrance.position = new Vector3(-3f, -3.45f, 0f);
        GameObject systems = Child(parent, "RoomSystems");
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, cameraFollow, true);
    }

    private static void CreateLight(Transform parent)
    {
        GameObject go = Child(parent, "Cold Overcast Global Light");
        Light2D light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = new Color(.78f, .87f, .94f);
        light.intensity = .76f;
    }

    private static void CreatePostProcessing(Transform parent)
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Test005SnowAtmosphere";
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        }

        if (!profile.TryGet(out DepthOfField depthOfField))
            depthOfField = profile.Add<DepthOfField>(true);
        depthOfField.active = true;
        depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
        depthOfField.gaussianStart.Override(10.5f);
        depthOfField.gaussianEnd.Override(15.5f);
        depthOfField.gaussianMaxRadius.Override(.42f);
        depthOfField.highQualitySampling.Override(false);

        if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.color.Override(new Color(.025f, .055f, .085f));
        vignette.intensity.Override(.24f);
        vignette.smoothness.Override(.72f);

        if (!profile.TryGet(out FilmGrain grain)) grain = profile.Add<FilmGrain>(true);
        grain.active = true;
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(.1f);
        grain.response.Override(.74f);

        if (!profile.TryGet(out ColorAdjustments color)) color = profile.Add<ColorAdjustments>(true);
        color.active = true;
        color.postExposure.Override(-.08f);
        color.contrast.Override(8f);
        color.saturation.Override(-12f);
        EditorUtility.SetDirty(profile);

        GameObject go = Child(parent, "Global Snow Atmosphere");
        Volume volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;
        volume.sharedProfile = profile;
    }

    private static void CreateSnowLayers(Transform parent)
    {
        CreateSnowLayer(parent, "Far Snow - Decorative Only", new Vector3(0f, 2f, 2f),
            new Vector2(.012f, .035f), new Vector2(.05f, .16f), 7f, 75,
            new Vector2(-.32f, -.14f), new Vector2(-.045f, .015f), -16);
        CreateSnowLayer(parent, "Near Spindrift - Decorative Only", new Vector3(0f, 1f, -1.5f),
            new Vector2(.03f, .085f), new Vector2(.1f, .32f), 12f, 120,
            new Vector2(-.9f, -.42f), new Vector2(-.14f, .025f), 12);
    }

    private static void CreateSnowLayer(Transform parent, string name, Vector3 position,
        Vector2 sizeRange, Vector2 alphaRange, float emissionRate, int maxParticles,
        Vector2 horizontalVelocity, Vector2 verticalVelocity, int sortingOrder)
    {
        GameObject go = Child(parent, name);
        go.transform.position = position;
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 8f;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(.82f, .9f, .95f, alphaRange.x),
            new Color(.96f, .98f, 1f, alphaRange.y));
        main.maxParticles = maxParticles;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = emissionRate;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(38f, 14f, 0f);
        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(horizontalVelocity.x, horizontalVelocity.y);
        velocity.y = new ParticleSystem.MinMaxCurve(verticalVelocity.x, verticalVelocity.y);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = .08f;
        noise.frequency = .25f;
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        renderer.sortingOrder = sortingOrder;
    }

    private static Sprite ImportSprite(string path, float pixelsPerUnit, bool alpha)
    {
        Require(File.Exists(path), $"Missing image asset: {path}");
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Require(importer != null, $"Could not import texture: {path}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.spritePivot = new Vector2(.5f, .5f);
        importer.alphaIsTransparency = alpha;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = path == TerrainTexturePath || path == TerrainBodyTexturePath
            ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Require(sprite != null, $"Texture did not import as Sprite: {path}");
        return sprite;
    }

    private static Tile CreateOrUpdateTile(string path, Sprite sprite, Color color)
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
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static Tilemap CreateTilemapLayer(Transform parent, string name)
    {
        GameObject layer = Child(parent, name);
        Tilemap map = layer.AddComponent<Tilemap>();
        TilemapRenderer renderer = layer.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = name == "Terrain" ? 0 : name == "Foreground" ? 20 : -10;
        return map;
    }

    private static void Fill(Tilemap map, TileBase tile, int minX, int maxX, int minY, int maxY)
    {
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                map.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private static SpriteRenderer SpriteObject(Transform parent, string name, Sprite sprite,
        Vector3 position, Vector2 targetSize, Color color, int sortingOrder)
    {
        GameObject go = Child(parent, name);
        go.transform.position = position;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        Vector2 native = sprite.bounds.size;
        go.transform.localScale = new Vector3(targetSize.x / native.x, targetSize.y / native.y, 1f);
        return renderer;
    }

    private static GameObject Child(Transform parent, string name)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Validate(Scene scene, Tilemap terrain, CameraFollow2D follow)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Camera camera = follow.GetComponent<Camera>();
        Require(camera != null && camera.orthographic && Mathf.Approximately(camera.orthographicSize, OrthographicSize),
            "test005 camera must use Snow-region orthographic size 7.");
        Require(follow.FollowsVertical && follow.UsesRoomBounds && follow.RoomBounds == CameraBounds &&
                follow.AlignsEntryFramingToBounds && Mathf.Approximately(follow.SmoothTime, CameraSmoothTime),
            "test005 camera must use explicit Snow-region follow, bounds, entry framing, and damping.");
        Require(terrain.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.StaticSolid,
            "Terrain must carry safe StaticSolid semantics.");
        Require(terrain.GetComponent<MirrorSurface2D>()?.kind == MirrorSurface2D.SurfaceKind.Ground,
            "Terrain must carry Ground mirror semantics.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "Formal rooms must not serialize a room-local Player.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "test005 must contain exactly one RoomPlayerSpawner2D.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomResetSystem>(true)).Count() == 1,
            "test005 must contain exactly one RoomResetSystem.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<MeshFilter>(true)).Count() == 0,
            "Visible environment geometry must not use custom triangle or Quad meshes.");
        SpriteRenderer far = roots.SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true))
            .Single(renderer => renderer.name == "Far Background - Alpine Sky");
        SpriteRenderer mid = roots.SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true))
            .Single(renderer => renderer.name == "Midground - Distant Ridge");
        SpriteRenderer focus = roots.SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true))
            .Single(renderer => renderer.name == "Decorative Focus - Windswept Tree");
        SpriteRenderer foreground = roots.SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true))
            .Single(renderer => renderer.name == "Foreground - Snow Rock Frame");
        Require(far.sortingOrder < mid.sortingOrder && mid.sortingOrder < focus.sortingOrder &&
                focus.sortingOrder < terrain.GetComponent<TilemapRenderer>().sortingOrder &&
                terrain.GetComponent<TilemapRenderer>().sortingOrder < foreground.sortingOrder,
            "Snow environment layers must render in strict far-to-gameplay-to-foreground order.");
        Require(focus.GetComponent<Collider2D>() == null,
            "The decorative windswept tree must not carry gameplay collision.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<Volume>(true)).Count() == 1,
            "test005 must contain exactly one global atmosphere volume.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
