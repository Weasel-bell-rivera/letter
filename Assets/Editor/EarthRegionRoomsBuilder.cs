using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds EARTH_002..EARTH_015 Tilemap greyboxes from shared gameplay Prefabs.</summary>
public static class EarthRegionRoomsBuilder
{
    private const string TerrainTilePath = "Assets/Tiles/Earth/Earth001TerrainGraybox.asset";
    private const string SinkPrefabPath = "Assets/Prefabs/Gameplay/Earth/SinkingEarthBlock2D.prefab";
    private const string MovingPrefabPath = "Assets/Prefabs/Gameplay/Platforms/MovingPlatform2D.prefab";
    private const string PlatePrefabPath = "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab";
    private const string DoorPrefabPath = "Assets/Prefabs/Gameplay/Doors/Door2D.prefab";
    private const string EnemyPrefabPath = "Assets/Prefabs/Gameplay/Enemies/VerticalWallPatrolEnemy2D.prefab";
    private const string ExitPrefabPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";
    private const string Earth005FarBackgroundPath =
        "Assets/Art/Earth/Backgrounds/earth005_far_background_v1.png";
    private const string EarthSupportColumnPath =
        "Assets/Art/Earth/Midground/earth_support_column_v1.png";
    private const string EarthStrataOverlayPath =
        "Assets/Art/Earth/Midground/earth_strata_overlay_v1.png";
    private const string EarthSinkingBlockFramePath =
        "Assets/Art/Earth/Midground/earth_sinking_block_frame_v1.png";
    private static readonly string[] Earth005TerrainSpritePaths =
    {
        "Assets/Art/Earth/Terrain/earth_terrain_default_dirt.png"
    };
    private const string Earth005TerrainTileFolder = "Assets/Tiles/Earth/Earth005";
    private const string Earth007TerrainTexturePath = "Assets/Art/Earth/Terrain/LowPolyEarthTile-v4.png";
    private const string Earth007TerrainTileFolder = "Assets/Tiles/Earth/Earth007";
    private const string Earth007TerrainTilePath = Earth007TerrainTileFolder + "/Earth007Terrain.asset";
    private const string EarthAmbientGlowPath =
        "Assets/Art/Earth/Lighting/earth_ambient_glow_v1.png";

    private static readonly Dictionary<int, int[]> ImplementedNeighbors = new()
    {
        [2] = new[] {1}, [3] = new[] {1,4}, [4] = new[] {3,5}, [5] = new[] {4,7},
        [6] = new[] {7,10}, [7] = new[] {5,8,6,11}, [8] = new[] {7,9}, [9] = new[] {8},
        [10] = new[] {6,14}, [11] = new[] {7,12,15}, [12] = new[] {11,13},
        [13] = new[] {12}, [14] = new[] {10}, [15] = new[] {11}
    };

    [MenuItem("Tools/W1/Build EARTH-002 to EARTH-015 Greyboxes")]
    public static void BuildAll()
    {
        Require(AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath) != null,
            $"Missing terrain Tile: {TerrainTilePath}");
        Require(AssetDatabase.LoadAssetAtPath<GameObject>(SinkPrefabPath) != null,
            $"Missing sinking block Prefab: {SinkPrefabPath}");
        Directory.CreateDirectory("Assets/Scenes/Levels/Earth");

        for (int id = 2; id <= 15; id++) BuildRoom(id);

        AssetDatabase.SaveAssets();
        Debug.Log("EARTH_002 through EARTH_015 Tilemap greyboxes built successfully.");
    }

    [MenuItem("Tools/W1/Build EARTH-005 Visual Readability Pass")]
    public static void BuildEarth005()
    {
        Require(AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath) != null,
            $"Missing terrain Tile: {TerrainTilePath}");
        Require(AssetDatabase.LoadAssetAtPath<GameObject>(SinkPrefabPath) != null,
            $"Missing sinking block Prefab: {SinkPrefabPath}");
        Directory.CreateDirectory("Assets/Scenes/Levels/Earth");
        BuildRoom(5);
        AssetDatabase.SaveAssets();
        Debug.Log("EARTH_005 visual readability pass built successfully.");
    }

    [MenuItem("Tools/W1/Rebuild EARTH_007")]
    public static void RebuildEarth007()
    {
        Require(AssetDatabase.LoadAssetAtPath<GameObject>(SinkPrefabPath) != null,
            $"Missing sinking block Prefab: {SinkPrefabPath}");
        Directory.CreateDirectory("Assets/Scenes/Levels/Earth");
        BuildRoom(7);
        AssetDatabase.SaveAssets();
        Debug.Log("EARTH_007 rebuilt with its low-contrast low-poly Terrain art.");
    }

    private static void BuildRoom(int id)
    {
        Tile terrainTile = id == 7 ? EnsureEarth007TerrainTile() : Load<Tile>(TerrainTilePath);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new($"EARTH_{id:000} {RoomName(id)} Greybox");

        GameObject grid = new("Grid");
        grid.transform.SetParent(root.transform, false);
        grid.AddComponent<Grid>().cellSize = Vector3.one;
        CreateTilemap(grid.transform, "Background");
        Tilemap terrain = CreateTilemap(grid.transform, "Terrain");
        ConfigureTerrain(terrain);
        CreateTilemap(grid.transform, "FrozenGround");
        CreateTilemap(grid.transform, "OneWayPlatform");
        CreateTilemap(grid.transform, "SpecialMirrorWall");
        CreateTilemap(grid.transform, "Hazard");
        CreateTilemap(grid.transform, "Decoration");
        CreateTilemap(grid.transform, "Foreground");

        BuildTerrain(id, terrain, terrainTile);
        if (id == 5) ApplyEarth005TerrainArt(terrain, EnsureEarth005TerrainTiles());
        if (id == 5) ApplyEarth005ReadabilityLayers(grid.transform, terrain);
        Bake(terrain);

        GameObject gameplay = new("Gameplay");
        gameplay.transform.SetParent(root.transform, false);
        GameObject dynamics = new("DynamicObjects");
        dynamics.transform.SetParent(gameplay.transform, false);
        GameObject entrances = new("Entrances");
        entrances.transform.SetParent(gameplay.transform, false);
        GameObject exits = new("Exits");
        exits.transform.SetParent(gameplay.transform, false);

        float defaultEntranceX = id is 5 or 6 or 7 ? -9.5f : -10f;
        Transform entrance = Marker("Entrance-DEFAULT", new Vector3(defaultEntranceX, -2.08f, 0f), entrances.transform);
        CreateReturnEntrances(id, entrances.transform);
        CreateGameplay(id, dynamics.transform, scene);
        if (id == 5) ApplyEarth005ReadabilityToBlocks(dynamics.transform);
        CreateExits(id, exits.transform, scene);
        if (id == 5) CreateEarth005FarBackground(root.transform);
        CameraFollow2D cameraFollow = CreateCamera(id);

        GameObject systems = new("RoomSystems");
        systems.transform.SetParent(root.transform, false);
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, cameraFollow, true);

        Validate(scene, id, terrain);
        string path = $"Assets/Scenes/Levels/Earth/Earth_{id:000}.unity";
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, path), $"Failed to save {path}");
        AddBuildScene(path);
    }

    private static void BuildTerrain(int id, Tilemap terrain, Tile tile)
    {
        Fill(terrain, tile, -13, 12, -4, -4);
        Fill(terrain, tile, -13, 12, -7, -7);
        Fill(terrain, tile, -13, -13, -6, 7);
        Fill(terrain, tile, 12, 12, -6, 7);
        Fill(terrain, tile, -13, 12, 7, 7);

        foreach (float blockX in BlockXs(id)) ClearForBlock(terrain, blockX, BlockWidth(id));
        if (id is 3 or 5 or 9 or 13)
        {
            Fill(terrain, tile, -10, -7, 0, 0);
            Fill(terrain, tile, 7, 10, 2, 2);
        }
        if (id is 4 or 7 or 8 or 15)
        {
            Fill(terrain, tile, -2, 2, 1, 1);
        }
        if (id is 6 or 10 or 14)
        {
            Fill(terrain, tile, 7, 11, 0, 0);
        }
        if (id is 11 or 12)
        {
            Fill(terrain, tile, -5, -5, -3, 4);
            Fill(terrain, tile, 5, 5, -3, 4);
        }
        if (id == 15)
        {
            Fill(terrain, tile, -11, -8, 3, 3);
            Fill(terrain, tile, 8, 11, 3, 3);
        }
    }

    private static void ClearForBlock(Tilemap terrain, float centerX, float width)
    {
        int minX = Mathf.FloorToInt(centerX - width * .5f);
        int maxX = Mathf.CeilToInt(centerX + width * .5f) - 1;
        for (int x = minX; x <= maxX; x++) terrain.SetTile(new Vector3Int(x, -4, 0), null);
    }

    private static void CreateGameplay(int id, Transform parent, Scene scene)
    {
        float[] xs = BlockXs(id);
        for (int i = 0; i < xs.Length; i++)
        {
            float distance = id is 3 or 10 or 13 ? 3f : id == 14 ? 4f : 2f;
            float recovery = id == 9 ? (i == 0 ? .7f : 1.2f) : 1f;
            CreateSink(parent, scene, xs[i], BlockWidth(id), distance, recovery, $"SinkingBlock-{(char)('A' + i)}");
        }

        if (id == 6) CreateMoving(parent, scene, new Vector2(6f, -1.5f), new Vector2(-2f, 0f),
            new Vector2(2f, 0f), "MovingPlatform-A");
        if (id == 10) CreateMoving(parent, scene, new Vector2(4f, -4.5f), new Vector2(-3f, 0f),
            new Vector2(3f, 0f), "MovingPlatform-A");
        if (id == 14)
        {
            CreateMoving(parent, scene, new Vector2(4f, -2f), new Vector2(-3f, 0f),
                new Vector2(3f, 0f), "MovingPlatform-A");
            CreateMoving(parent, scene, new Vector2(-4f, -5f), new Vector2(-3f, 0f),
                new Vector2(3f, 0f), "MovingPlatform-B");
        }
        if (id == 15) CreateMoving(parent, scene, new Vector2(0f, 1.5f), new Vector2(-3f, 0f),
            new Vector2(3f, 0f), "MovingPlatform-A");

        if (id is 8 or 15)
        {
            PressurePlate2D plate = CreatePlate(parent, scene,
                id == 8 ? new Vector2(-7f, -2.85f) : new Vector2(7f, -3.35f), "PressurePlate-A");
            CreateDoor(parent, scene, id == 8 ? new Vector2(.5f, -2f) : new Vector2(10.5f, -3f),
                plate, "Door-A");
        }

        if (id == 11)
            CreateEnemy(parent, scene, new Vector2(-3.54f, 0f), -2f, 2f, "Enemy-A");
        if (id == 12)
        {
            CreateEnemy(parent, scene, new Vector2(-3.54f, 0f), -2f, 2f, "Enemy-A");
            CreateEnemy(parent, scene, new Vector2(6.46f, 0f), -2f, 2f, "Enemy-B");
        }
    }

    private static float[] BlockXs(int id) => id switch
    {
        2 or 3 or 6 or 10 or 11 or 12 or 14 => new[] {0f},
        4 => new[] {-5f, 5f},
        5 => new[] {-4f, 4f},
        7 => new[] {-6f, 6f},
        8 => new[] {5f},
        9 => new[] {-2f, 3f},
        13 => new[] {-4f, 4f},
        15 => new[] {-6f, 0f, 6f},
        _ => Array.Empty<float>()
    };

    private static float BlockWidth(int id) => id is 3 or 6 or 7 or 10 or 12 or 14 ? 3f : 2f;

    private static void CreateSink(Transform parent, Scene scene, float x, float width,
        float distance, float recovery, string name)
    {
        GameObject instance = Prefab(SinkPrefabPath, parent, scene, new Vector3(x, -3.5f), name);
        instance.transform.localScale = new Vector3(width / 2f, 1f, 1f);
        SinkingEarthBlock2D block = instance.GetComponent<SinkingEarthBlock2D>();
        Require(block != null, "Sinking block Prefab is missing SinkingEarthBlock2D.");
        SerializedObject data = new(block);
        data.FindProperty("sinkDistance").floatValue = distance;
        data.FindProperty("sinkSpeed").floatValue = 1.5f;
        data.FindProperty("recoverSpeed").floatValue = recovery;
        data.FindProperty("weightForFullSink").floatValue = 1f;
        data.ApplyModifiedPropertiesWithoutUndo();
        Record(instance.transform);
        Record(block);
    }

    private static void ApplyEarth005ReadabilityLayers(Transform grid, Tilemap terrain)
    {
        terrain.color = Color.white;
        SetTilemapSorting(grid, "Background", -30);
        SetTilemapSorting(grid, "Decoration", -5);
        SetTilemapSorting(grid, "Foreground", 30);
    }

    private static Tile[] EnsureEarth005TerrainTiles()
    {
        Directory.CreateDirectory(Earth005TerrainTileFolder);
        Tile[] tiles = new Tile[Earth005TerrainSpritePaths.Length];
        for (int i = 0; i < tiles.Length; i++)
        {
            Sprite sprite = Load<Sprite>(Earth005TerrainSpritePaths[i]);
            string tilePath = $"{Earth005TerrainTileFolder}/Earth005Terrain_{(char)('A' + i)}.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            tile.sprite = sprite;
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            tiles[i] = tile;
        }

        AssetDatabase.SaveAssets();
        return tiles;
    }

    private static Tile EnsureEarth007TerrainTile()
    {
        TextureImporter importer = AssetImporter.GetAtPath(Earth007TerrainTexturePath) as TextureImporter;
        Require(importer != null, $"Missing terrain texture: {Earth007TerrainTexturePath}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 1254f;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        Directory.CreateDirectory(Earth007TerrainTileFolder);
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(Earth007TerrainTilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, Earth007TerrainTilePath);
        }

        tile.sprite = Load<Sprite>(Earth007TerrainTexturePath);
        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void ApplyEarth005TerrainArt(Tilemap terrain, IReadOnlyList<Tile> variants)
    {
        Require(variants != null && variants.Count > 0, "EARTH_005 terrain art needs Tile variants.");
        terrain.CompressBounds();
        foreach (Vector3Int position in terrain.cellBounds.allPositionsWithin)
        {
            if (!terrain.HasTile(position)) continue;
            uint hash = unchecked((uint)(position.x * 73856093) ^ (uint)(position.y * 19349663));
            hash ^= hash >> 13;
            hash *= 1274126177u;
            terrain.SetTile(position, variants[(int)(hash % (uint)variants.Count)]);
        }
    }

    private static void ApplyEarth005ReadabilityToBlocks(Transform dynamics)
    {
        ApplyBlockReadability(dynamics.Find("SinkingBlock-A"),
            new Color(.56f, .31f, .12f, 1f), new Color(1f, .72f, .25f, 1f));
        ApplyBlockReadability(dynamics.Find("SinkingBlock-B"),
            new Color(.56f, .31f, .12f, 1f), new Color(1f, .72f, .25f, 1f));
    }

    private static void ApplyBlockReadability(Transform block, Color bodyColor, Color markerColor)
    {
        Require(block != null, "EARTH_005 readability pass is missing a sinking block.");
        SpriteRenderer body = block.Find("Visual")?.GetComponent<SpriteRenderer>();
        SpriteRenderer marker = block.Find("TopMarker")?.GetComponent<SpriteRenderer>();
        Require(body != null && marker != null,
            "EARTH_005 sinking block readability visuals are incomplete.");
        body.color = bodyColor;
        marker.color = markerColor;
        Record(body);
        Record(marker);
    }

    private static void SetTilemapSorting(Transform grid, string layerName, int order)
    {
        TilemapRenderer renderer = grid.Find(layerName)?.GetComponent<TilemapRenderer>();
        Require(renderer != null, $"EARTH_005 is missing the {layerName} TilemapRenderer.");
        renderer.sortingOrder = order;
        EditorUtility.SetDirty(renderer);
    }

    private static void CreateEarth005FarBackground(Transform room)
    {
        Sprite sprite = Load<Sprite>(Earth005FarBackgroundPath);
        Require(sprite != null, $"Missing EARTH_005 far-background Sprite: {Earth005FarBackgroundPath}");

        GameObject art = new("Art");
        art.transform.SetParent(room, false);
        GameObject far = ArtGroup("FarBackground", art.transform);
        SizedSprite(sprite, far.transform, "Earth005FarBackground", new Vector2(-.25f, .5f),
            new Vector2(28f, 14f), new Color(.72f, .72f, .72f, 1f), -40);
        CreateEarth005Midground(art.transform);
        CreateEarth005LightingGuides(art.transform);
        CreateEarth005Atmosphere(art.transform);
        CreateEarth005ForegroundFrame(art.transform);
    }

    private static void CreateEarth005Midground(Transform art)
    {
        Sprite support = Load<Sprite>(EarthSupportColumnPath);
        Sprite strata = Load<Sprite>(EarthStrataOverlayPath);
        Sprite blockFrame = Load<Sprite>(EarthSinkingBlockFramePath);
        Require(support != null && strata != null && blockFrame != null,
            "EARTH_005 midground art assets are incomplete.");

        GameObject mid = ArtGroup("Midground", art);
        SizedSprite(support, mid.transform, "LeftRockSupport", new Vector2(-11.25f, .8f),
            new Vector2(2.2f, 8.2f), new Color(.48f, .48f, .48f, .9f), -18);
        SpriteRenderer rightSupport = SizedSprite(support, mid.transform, "RightRockSupport",
            new Vector2(10.75f, 1f), new Vector2(2.2f, 8.2f),
            new Color(.48f, .48f, .48f, .9f), -18);
        rightSupport.flipX = true;

        SizedSprite(strata, mid.transform, "LeftRouteStrata", new Vector2(-8.5f, -.25f),
            new Vector2(5.6f, 1.45f), new Color(.52f, .52f, .52f, .92f), -12);
        SpriteRenderer rightStrata = SizedSprite(strata, mid.transform, "RightRouteStrata",
            new Vector2(8.5f, 1.75f), new Vector2(5.6f, 1.45f),
            new Color(.52f, .52f, .52f, .92f), -12);
        rightStrata.flipX = true;
        SizedSprite(strata, mid.transform, "LowerTerrainTransition", new Vector2(0f, -4.35f),
            new Vector2(12f, 1.5f), new Color(.42f, .42f, .42f, .82f), -14);

        SizedSprite(blockFrame, mid.transform, "SinkingBlockFrameA", new Vector2(-4f, -4.45f),
            new Vector2(3.4f, 5.1f), new Color(.58f, .58f, .58f, .92f), -8);
        SpriteRenderer frameB = SizedSprite(blockFrame, mid.transform, "SinkingBlockFrameB",
            new Vector2(4f, -4.45f), new Vector2(3.4f, 5.1f),
            new Color(.58f, .58f, .58f, .92f), -8);
        frameB.flipX = true;
    }

    private static void CreateEarth005LightingGuides(Transform art)
    {
        Sprite glow = Load<Sprite>(EarthAmbientGlowPath);
        Require(glow != null, $"Missing EARTH_005 ambient glow Sprite: {EarthAmbientGlowPath}");
        GameObject lighting = ArtGroup("LightingGuides", art);

        AddPulse(SizedSprite(glow, lighting.transform, "PuzzleLightPool", new Vector2(0f, -3.15f),
            new Vector2(12f, 4.4f), new Color(.78f, .48f, .18f, .12f), -9), 9f, .08f, .015f, 0f);
        AddPulse(SizedSprite(glow, lighting.transform, "LeftRouteClarity", new Vector2(-8.5f, -.15f),
            new Vector2(7f, 2.5f), new Color(.72f, .45f, .2f, .075f), -13), 11f, .06f, .01f, 1.7f);
        AddPulse(SizedSprite(glow, lighting.transform, "RightRouteClarity", new Vector2(8.5f, 1.85f),
            new Vector2(7f, 2.5f), new Color(.72f, .45f, .2f, .075f), -13), 11f, .06f, .01f, 4.1f);
        SizedSprite(glow, lighting.transform, "ExitGuideLeft", new Vector2(-10.7f, -2.5f),
            new Vector2(3.2f, 3.6f), new Color(.62f, .58f, .25f, .09f), -7);
        SizedSprite(glow, lighting.transform, "ExitGuideRight", new Vector2(9.8f, -2.5f),
            new Vector2(3.2f, 3.6f), new Color(.62f, .58f, .25f, .09f), -7);
    }

    private static void AddPulse(SpriteRenderer renderer, float seconds, float alphaVariation,
        float scaleVariation, float phase)
    {
        AmbientSpritePulse2D pulse = renderer.gameObject.AddComponent<AmbientSpritePulse2D>();
        pulse.Configure(seconds, alphaVariation, scaleVariation, phase);
        EditorUtility.SetDirty(pulse);
    }

    private static void CreateEarth005Atmosphere(Transform art)
    {
        GameObject atmosphere = ArtGroup("Atmosphere", art);
        Sprite glow = Load<Sprite>(EarthAmbientGlowPath);
        Require(glow != null, "EARTH_005 atmosphere needs the ambient glow Sprite.");
        SizedSprite(glow, atmosphere.transform, "DepthHazeLeft", new Vector2(-6.2f, 2.2f),
            new Vector2(9f, 4.8f), new Color(.52f, .39f, .27f, .11f), -22);
        SizedSprite(glow, atmosphere.transform, "DepthHazeRight", new Vector2(6.2f, .5f),
            new Vector2(9f, 4.8f), new Color(.52f, .39f, .27f, .095f), -22);
        SizedSprite(glow, atmosphere.transform, "CentralGroundHaze", new Vector2(0f, -2.6f),
            new Vector2(14f, 3.2f), new Color(.62f, .42f, .24f, .08f), -10);
        CreateEarth005Dust(atmosphere.transform);
    }

    private static void CreateEarth005ForegroundFrame(Transform art)
    {
        Sprite support = Load<Sprite>(EarthSupportColumnPath);
        Sprite strata = Load<Sprite>(EarthStrataOverlayPath);
        Require(support != null && strata != null,
            "EARTH_005 foreground frame needs support and strata Sprites.");
        GameObject foreground = ArtGroup("ForegroundFrame", art);
        SizedSprite(support, foreground.transform, "ForegroundFrameLeft", new Vector2(-12.4f, .4f),
            new Vector2(2.8f, 12.8f), new Color(.4f, .4f, .4f, .82f), 25);
        SpriteRenderer right = SizedSprite(support, foreground.transform, "ForegroundFrameRight",
            new Vector2(11.4f, .4f), new Vector2(2.8f, 12.8f),
            new Color(.4f, .4f, .4f, .82f), 25);
        right.flipX = true;
        SpriteRenderer top = SizedSprite(strata, foreground.transform, "ForegroundTopStrata",
            new Vector2(0f, 6.35f), new Vector2(16f, 1.8f),
            new Color(.34f, .34f, .34f, .82f), 25);
        top.flipY = true;
    }

    private static GameObject ArtGroup(string name, Transform parent)
    {
        GameObject group = new(name);
        group.transform.SetParent(parent, false);
        return group;
    }

    private static SpriteRenderer SizedSprite(Sprite sprite, Transform parent, string name,
        Vector2 position, Vector2 size, Color color, int sortingOrder)
    {
        GameObject visual = new(name);
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = position;
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        Vector2 nativeSize = sprite.bounds.size;
        visual.transform.localScale = new Vector3(size.x / nativeSize.x, size.y / nativeSize.y, 1f);
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static void CreateEarth005Dust(Transform parent)
    {
        GameObject dust = new("LowDensityDust");
        dust.transform.SetParent(parent, false);
        dust.transform.localPosition = new Vector3(0f, 5.5f, 0f);
        ParticleSystem particles = dust.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.08f, .18f);
        main.startSize = new ParticleSystem.MinMaxCurve(.04f, .11f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(.56f, .42f, .25f, .12f), new Color(.8f, .65f, .4f, .24f));
        main.maxParticles = 48;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 3f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(23f, .5f, .1f);
        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(-.12f, -.04f);

        ParticleSystemRenderer renderer = dust.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = -2;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private static void CreateMoving(Transform parent, Scene scene, Vector2 position,
        Vector2 start, Vector2 end, string name)
    {
        GameObject instance = Prefab(MovingPrefabPath, parent, scene, position, name);
        MovingPlatform2D moving = instance.GetComponent<MovingPlatform2D>();
        Require(moving != null, "Moving platform Prefab is missing MovingPlatform2D.");
        moving.ConfigurePath(start, end, 2f, .5f);
        Record(moving);
    }

    private static PressurePlate2D CreatePlate(Transform parent, Scene scene, Vector2 position, string name)
        => Prefab(PlatePrefabPath, parent, scene, position, name).GetComponent<PressurePlate2D>();

    private static void CreateDoor(Transform parent, Scene scene, Vector2 position,
        PressurePlate2D plate, string name)
    {
        Door2D door = Prefab(DoorPrefabPath, parent, scene, position, name).GetComponent<Door2D>();
        Require(door != null && plate != null, "Door or pressure plate Prefab is invalid.");
        door.ConfigureControlSource(plate);
        Record(door);
    }

    private static void CreateEnemy(Transform parent, Scene scene, Vector2 position,
        float bottom, float top, string name)
    {
        GameObject instance = Prefab(EnemyPrefabPath, parent, scene, position, name);
        VerticalWallPatrolEnemy2D enemy = instance.GetComponent<VerticalWallPatrolEnemy2D>();
        Require(enemy != null, "Vertical wall enemy Prefab is invalid.");
        enemy.ConfigurePatrol(bottom, top, 1.5f, .3f, VerticalWallPatrolEnemy2D.WallSide.Left, true);
        Record(enemy);
    }

    private static void CreateExits(int id, Transform parent, Scene scene)
    {
        int[] neighbors = ImplementedNeighbors[id];
        for (int i = 0; i < neighbors.Length; i++)
        {
            float x = neighbors.Length == 1 ? 10f : Mathf.Lerp(-11f, 10f, i / (float)(neighbors.Length - 1));
            GameObject instance = Prefab(ExitPrefabPath, parent, scene, new Vector3(x, -3f),
                $"Exit to EARTH_{neighbors[i]:000}");
            RoomExit2D exit = instance.GetComponent<RoomExit2D>();
            Require(exit != null, "Room exit Prefab is invalid.");
            string targetEntrance = ImplementedNeighbors.TryGetValue(neighbors[i], out int[] targetNeighbors) &&
                                    targetNeighbors.Contains(id) ? $"FROM_EARTH_{id:000}" : "DEFAULT";
            exit.Configure($"Earth_{neighbors[i]:000}", targetEntrance);
            if (id == 5)
            {
                SpriteRenderer visual = instance.transform.Find("Visual")?.GetComponent<SpriteRenderer>();
                Require(visual != null, "EARTH_005 exit visual is missing.");
                visual.color = new Color(.44f, .68f, .34f, .3f);
                EditorUtility.SetDirty(visual);
            }
            Record(exit);
        }
    }

    private static void CreateReturnEntrances(int id, Transform parent)
    {
        int[] neighbors = ImplementedNeighbors[id];
        for (int i = 0; i < neighbors.Length; i++)
        {
            float exitX = neighbors.Length == 1 ? 10f : Mathf.Lerp(-11f, 10f, i / (float)(neighbors.Length - 1));
            float exitClearance = id == 8 ? 2f : 1.5f;
            float spawnX = exitX < 0f ? exitX + exitClearance : exitX - exitClearance;
            Transform marker = Marker($"Entrance-FROM_EARTH_{neighbors[i]:000}", new Vector3(spawnX, -2.08f), parent);
            PlayerRoomAuthoring.ConfigureEntrance(marker, $"FROM_EARTH_{neighbors[i]:000}", false, exitX < 0f);
        }
    }

    private static CameraFollow2D CreateCamera(int id)
    {
        GameObject go = new("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.backgroundColor = id == 5
            ? new Color(.075f, .055f, .04f)
            : new Color(.24f, .17f, .11f);
        go.AddComponent<AudioListener>();
        CameraFollow2D follow = go.AddComponent<CameraFollow2D>();
        follow.Configure(null, id is 5 or 10 or 12 or 14 or 15);
        follow.ConfigureDamping(.15f);
        follow.ConfigureBounds(new Rect(-13f, -7f, 26f, 15f));
        return follow;
    }

    private static GameObject Prefab(string path, Transform parent, Scene scene, Vector3 position, string name)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(path), scene);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        Record(instance.transform);
        return instance;
    }

    private static Tilemap CreateTilemap(Transform parent, string name)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        Tilemap map = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();
        return map;
    }

    private static void ConfigureTerrain(Tilemap map)
    {
        Rigidbody2D body = map.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D composite = map.gameObject.AddComponent<CompositeCollider2D>();
        TilemapCollider2D collider = map.gameObject.AddComponent<TilemapCollider2D>();
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        SurfaceSemantic2D semantic = map.gameObject.AddComponent<SurfaceSemantic2D>();
        semantic.Configure(SurfaceSemantic2D.SurfaceType.StaticSolid, true, true);
        MirrorSurface2D mirror = map.gameObject.AddComponent<MirrorSurface2D>();
        mirror.kind = MirrorSurface2D.SurfaceKind.Ground;
        mirror.safe = true;
    }

    private static void Fill(Tilemap map, TileBase tile, int minX, int maxX, int minY, int maxY)
    {
        int width = maxX - minX + 1;
        int height = maxY - minY + 1;
        BoundsInt bounds = new(minX, minY, 0, width, height, 1);
        map.SetTilesBlock(bounds, Enumerable.Repeat(tile, width * height).ToArray());
    }

    private static void Bake(Tilemap map)
    {
        map.CompressBounds();
        map.RefreshAllTiles();
        map.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        Physics2D.SyncTransforms();
        map.GetComponent<CompositeCollider2D>().GenerateGeometry();
        Require(map.GetComponent<CompositeCollider2D>().pathCount > 0, $"{map.name} has no baked geometry.");
    }

    private static Transform Marker(string name, Vector3 position, Transform parent)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        return go.transform;
    }

    private static void Validate(Scene scene, int id, Tilemap terrain)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(terrain.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.StaticSolid,
            $"EARTH_{id:000} Terrain semantic missing.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            $"EARTH_{id:000} must not serialize Player.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            $"EARTH_{id:000} needs exactly one RoomPlayerSpawner2D.");
        RoomEntrance2D[] entrances = roots.SelectMany(root =>
            root.GetComponentsInChildren<RoomEntrance2D>(true)).ToArray();
        Require(entrances.Length == ImplementedNeighbors[id].Length + 1 &&
                entrances.Count(entrance => entrance.IsDefault) == 1,
            $"EARTH_{id:000} needs one DEFAULT and one source entrance per implemented neighbor.");
        SinkingEarthBlock2D[] blocks = roots.SelectMany(root =>
            root.GetComponentsInChildren<SinkingEarthBlock2D>(true)).ToArray();
        Require(blocks.Length == BlockXs(id).Length, $"EARTH_{id:000} sinking block count mismatch.");
        Require(blocks.All(block => PrefabUtility.GetPrefabInstanceStatus(block.gameObject) ==
                PrefabInstanceStatus.Connected), $"EARTH_{id:000} has a disconnected sinking block.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomExit2D>(true)).Count() ==
                ImplementedNeighbors[id].Length, $"EARTH_{id:000} exit count mismatch.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Count() == 1,
            $"EARTH_{id:000} needs exactly one Camera.");
    }

    private static void AddBuildScene(string path)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(scene => scene.path == path))
            scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static string RoomName(int id) => id switch
    {
        2 => "Shaft Mouth", 3 => "Rising Step", 4 => "Split Weight", 5 => "Offset Meeting",
        6 => "Rail Height", 7 => "Mine Junction", 8 => "Twin Watch", 9 => "Recovery Window",
        10 => "Rising Transfer", 11 => "Stone Vein Merge", 12 => "Wall Shift",
        13 => "Reverse Strata", 14 => "Deep Shaft Cross", 15 => "Weight Core", _ => "Earth Room"
    };

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        T result = AssetDatabase.LoadAssetAtPath<T>(path);
        Require(result != null, $"Missing asset: {path}");
        return result;
    }

    private static void Record(UnityEngine.Object value)
    {
        EditorUtility.SetDirty(value);
        PrefabUtility.RecordPrefabInstancePropertyModifications(value);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
