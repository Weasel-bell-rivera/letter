using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Builds SNOW_004..SNOW_015 Tilemap greyboxes from shared gameplay Prefabs.</summary>
public static class SnowRegionRoomsBuilder
{
    private static readonly Rect DefaultCameraBounds = new(-13f, -7f, 26f, 14f);
    private static readonly Rect WideCameraBounds = new(-20f, -7f, 40f, 14f);
    private static readonly Rect Snow007EntryFramingBounds = new(-13f, -7f, 26f, 14f);
    private const float CameraOrthographicSize = 7f;
    private const float CameraSmoothTime = .15f;
    private const string TerrainTilePath = "Assets/Tiles/Snow/SnowTerrainGraybox.asset";
    private const string IceTilePath = "Assets/Tiles/Snow/FrozenGroundSnowBlock.asset";
    private const string IceMaterialPath = "Assets/Settings/Physics/FrozenGround.physicsMaterial2D";
    private const string ExitPrefabPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";
    private const string PlatePrefabPath = "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab";
    private const string DoorPrefabPath = "Assets/Prefabs/Gameplay/Doors/Door2D.prefab";
    private const string EnemyPrefabPath = "Assets/Prefabs/Gameplay/Enemies/FreezablePatrolEnemy2D.prefab";
    private const string FreezingPrefabPath = "Assets/Prefabs/Gameplay/Surfaces/FreezingGroundCell2D.prefab";
    private const string Snow007GroundSpritePath = "Assets/Art/Snow/Tiles/snow_ice_ground_tile_64_v3.png";
    private const string Snow007TerrainTexturePath = "Assets/Art/Earth/Terrain/LowPolyEarthTile-v4.png";
    private const string Snow007TerrainTileFolder = "Assets/Tiles/Snow/Snow007";
    private const string Snow007TerrainTilePath = Snow007TerrainTileFolder + "/Snow007Terrain.asset";
    private const string SnowmanPrefabPath = "Assets/Prefabs/Gameplay/Snow/SnowmanGate2D.prefab";
    private const string CarrotPrefabPath = "Assets/Prefabs/Gameplay/Snow/TemporaryCarrotPickup2D.prefab";
    private const string SnowfallPrefabPath = "Assets/Prefabs/Gameplay/Hazards/PeriodicSnowfall2D.prefab";

    private static readonly Dictionary<int, int[]> Neighbors = new()
    {
        [4] = new[] { 3, 5 }, [5] = new[] { 4, 6, 8 }, [6] = new[] { 5, 7 }, [7] = new[] { 6 },
        [8] = new[] { 5, 9, 11 }, [9] = new[] { 8, 10, 12 }, [10] = new[] { 9, 11, 14 },
        [11] = new[] { 8, 10, 13 }, [12] = new[] { 9, 15 }, [13] = new[] { 11, 14 },
        [14] = new[] { 10, 13, 15 }, [15] = new[] { 12, 14 }
    };

    [MenuItem("Tools/W1/Build SNOW-004 to SNOW-015 Greyboxes")]
    public static void BuildAll()
    {
        Directory.CreateDirectory("Assets/Prefabs/Gameplay/Snow");
        BuildSnowPrefabs();
        for (int room = 4; room <= 15; room++) BuildRoom(room);
        AssetDatabase.SaveAssets();
        Debug.Log("SNOW_004 through SNOW_015 Tilemap greyboxes built successfully.");
    }

    [MenuItem("Tools/W1/Rebuild SNOW_007")]
    public static void RebuildSnow007()
    {
        BuildRoom(7);
        AssetDatabase.SaveAssets();
        Debug.Log("SNOW_007 rebuilt with its low-contrast low-poly Terrain art.");
    }

    private static void BuildSnowPrefabs()
    {
        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        GameObject snowman = new("SnowmanGate2D");
        BoxCollider2D snowmanCollider = snowman.AddComponent<BoxCollider2D>();
        snowmanCollider.size = new Vector2(1.25f, 2.2f);
        SpriteRenderer snowmanVisual = Visual("Visual", snowman.transform, new Vector2(1.25f, 2.2f), Color.white, sprite);
        SnowmanGate2D snowmanGate = snowman.AddComponent<SnowmanGate2D>();
        snowmanGate.ConfigureVisual(snowmanVisual);
        PrefabUtility.SaveAsPrefabAsset(snowman, SnowmanPrefabPath);
        UnityEngine.Object.DestroyImmediate(snowman);

        GameObject carrot = new("TemporaryCarrotPickup2D");
        CircleCollider2D carrotCollider = carrot.AddComponent<CircleCollider2D>();
        carrotCollider.radius = .35f; carrotCollider.isTrigger = true;
        Visual("Visual", carrot.transform, new Vector2(.35f, .8f), new Color(1f, .45f, .05f), sprite);
        carrot.AddComponent<TemporaryCarrotPickup2D>();
        PrefabUtility.SaveAsPrefabAsset(carrot, CarrotPrefabPath);
        UnityEngine.Object.DestroyImmediate(carrot);

        GameObject snowfall = new("PeriodicSnowfall2D");
        EruptionHazard2D cycle = snowfall.AddComponent<EruptionHazard2D>();
        GameObject zone = new("DangerZone"); zone.transform.SetParent(snowfall.transform, false);
        BoxCollider2D zoneCollider = zone.AddComponent<BoxCollider2D>(); zoneCollider.isTrigger = true;
        zoneCollider.size = new Vector2(6f, 5f);
        Hazard2D hazard = zone.AddComponent<Hazard2D>(); hazard.SetActive(false);
        SpriteRenderer snowVisual = Visual("WarningVisual", zone.transform, new Vector2(6f, 5f),
            new Color(.75f, .9f, 1f, .22f), sprite);
        SerializedObject cycleData = new(cycle);
        cycleData.FindProperty("warningDuration").floatValue = 1.5f;
        cycleData.FindProperty("dangerDuration").floatValue = 1.25f;
        cycleData.FindProperty("cooldownDuration").floatValue = 2.5f;
        cycleData.FindProperty("hazard").objectReferenceValue = hazard;
        cycleData.FindProperty("visual").objectReferenceValue = snowVisual;
        cycleData.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(snowfall, SnowfallPrefabPath);
        UnityEngine.Object.DestroyImmediate(snowfall);
    }

    private static void BuildRoom(int id)
    {
        Tile terrainTile = id == 7 ? EnsureSnow007TerrainTile() : Load<Tile>(TerrainTilePath);
        Tile iceTile = Load<Tile>(IceTilePath);
        PhysicsMaterial2D iceMaterial = Load<PhysicsMaterial2D>(IceMaterialPath);
        GameObject exitPrefab = Load<GameObject>(ExitPrefabPath);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new($"SNOW_{id:000} {RoomName(id)}");
        GameObject grid = new("Grid"); grid.transform.SetParent(root.transform); grid.AddComponent<Grid>();
        CreateTilemap(grid.transform, "Background");
        Tilemap terrain = CreateTilemap(grid.transform, "Terrain"); ConfigureSurface(terrain, SurfaceSemantic2D.SurfaceType.StaticSolid, null);
        Tilemap ice = CreateTilemap(grid.transform, "FrozenGround"); ConfigureSurface(ice, SurfaceSemantic2D.SurfaceType.FrozenGround, iceMaterial);
        CreateTilemap(grid.transform, "FreezingGround"); CreateTilemap(grid.transform, "OneWayPlatform");
        CreateTilemap(grid.transform, "SpecialMirrorWall"); CreateTilemap(grid.transform, "Hazard");
        CreateTilemap(grid.transform, "Decoration"); CreateTilemap(grid.transform, "Foreground");
        Fill(terrain, terrainTile, -13, 12, -3, -3);
        ConfigureLayout(id, terrain, ice, terrainTile, iceTile);
        Bake(terrain); if (ice.GetUsedTilesCount() > 0) Bake(ice);

        GameObject gameplay = new("Gameplay"); gameplay.transform.SetParent(root.transform);
        GameObject dynamics = new("DynamicObjects"); dynamics.transform.SetParent(gameplay.transform);
        GameObject entrances = new("Entrances"); entrances.transform.SetParent(gameplay.transform);
        GameObject exits = new("Exits"); exits.transform.SetParent(gameplay.transform);
        Transform entrance = Marker("Entrance-DEFAULT", new Vector3(-9.5f, -1.08f, 0f), entrances.transform);
        CreateReturnEntrances(id, entrances.transform);
        CreateGameplay(id, dynamics.transform);
        CreateExits(id, exitPrefab, exits.transform);
        CameraFollow2D cameraFollow = CreateCamera(id);
        GameObject systems = new("RoomSystems"); systems.transform.SetParent(root.transform);
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, cameraFollow, false);
        Validate(scene, id, terrain, ice);
        string path = $"Assets/Scenes/Levels/Snow/Snow_{id:000}.unity";
        EditorSceneManager.MarkSceneDirty(scene); Require(EditorSceneManager.SaveScene(scene, path), $"Failed to save {path}");
        AddBuildScene(path);
    }

    private static void ConfigureLayout(int id, Tilemap terrain, Tilemap ice, Tile terrainTile, Tile iceTile)
    {
        void Ice(int a, int b) { for (int x = a; x <= b; x++) { terrain.SetTile(new Vector3Int(x,-3), null); ice.SetTile(new Vector3Int(x,-3), iceTile); } }
        if (id == 7)
            foreach (int x in new[] {-5,-4,-3,1,2,3,7,8}) terrain.SetTile(new Vector3Int(x, -3), null);
        if (id is 4 or 5 or 6) Ice(3, 7);
        if (id is 8 or 9) Ice(5, 9);
        if (id is 12 or 13) { Ice(-5, 1); Ice(5, 9); }
        if (id is 14 or 15) { Ice(1, 4); Ice(7, 9); }
        if (id is 4 or 5 or 9 or 14 or 15) Fill(terrain, terrainTile, 9, 11, -1, -1);
        if (id is >= 9 and <= 15)
        {
            Fill(terrain, terrainTile, 8, 11, 2, 2);
            Fill(terrain, terrainTile, 6, 6, -2, -1);
            Fill(terrain, terrainTile, 7, 7, -2, 0);
        }
        if (id == 10) Fill(terrain, terrainTile, 3, 5, 1, 1);
        if (id == 11) Fill(terrain, terrainTile, -6, -3, 0, 0);
        if (id == 13) Fill(terrain, terrainTile, 3, 5, 0, 0);
        if (id == 6) { Fill(terrain, terrainTile, -1, -1, -2, 2); Fill(terrain, terrainTile, 1, 1, -2, 2); }
        if (id is 11 or 12 or 13 or 14 or 15) { Fill(terrain, terrainTile, -10, -7, 2, 2); Fill(terrain, terrainTile, -1, 2, 2, 2); Fill(terrain, terrainTile, 8, 11, 2, 2); }
    }

    private static void CreateGameplay(int id, Transform parent)
    {
        if (id is 4 or 5 or 9 or 13 or 14 or 15) Enemy(parent, id >= 14 ? -2f : 1f, -3f, 7f, true);
        if (id == 9) Enemy(parent, 4f, -1f, 4f, false);
        if (id is 14 or 15) Enemy(parent, 5f, -2f, 5f, true);
        if (id is 5 or 6 or 8 or 9 or 13 or 14 or 15)
        {
            PressurePlate2D p1 = Plate(parent, new Vector2(-7f, -2.35f), "Plate-1");
            Door2D d1 = Door(parent, new Vector2(.5f, -2f), "Door-1", p1);
            if (id is 6 or 9 or 13 or 14 or 15)
            {
                PressurePlate2D p2 = Plate(parent, new Vector2(7f, -2.35f), "Plate-2");
                Door(parent, new Vector2(10.5f, -2f), "Door-2", p2);
            }
        }
        if (id is 7 or 8 or 15)
        {
            int[] xs = id == 7 ? new[] {-5,-4,-3,1,2,3,7,8} : id == 8 ? new[] {-1,0,1,2} : new[] {-6,-5,-4};
            foreach (int x in xs)
            {
                GameObject cell = Prefab(FreezingPrefabPath, parent, new Vector3(x, -2.5f), $"FreezingGround-{x}");
                if (id == 7) ConfigureSnow007GroundVisual(cell);
            }
        }
        if (id is 12 or 14)
        {
            int[] xs = id == 12 ? new[] {2,3,4} : new[] {-6,-5,5,6};
            foreach (int x in xs) Prefab(FreezingPrefabPath, parent, new Vector3(x, -2.5f), $"FreezingGround-{x}");
        }
        if (id is 10 or 11 or 15)
        {
            float snowmanX = id == 10 ? 4f : id == 11 ? 6.5f : 7f;
            Vector3 carrotPosition = id == 10 ? new Vector3(-1f,1f) : id == 11 ? new Vector3(-4.5f,1.2f) : new Vector3(0f,1.2f);
            GameObject snowman = Prefab(SnowmanPrefabPath, parent, new Vector3(snowmanX,-1.9f), "Snowman-Gate");
            GameObject carrot = Prefab(CarrotPrefabPath, parent, carrotPosition, "Carrot");
            carrot.GetComponent<TemporaryCarrotPickup2D>().Configure(snowman.GetComponent<SnowmanGate2D>());
            Record(carrot.GetComponent<TemporaryCarrotPickup2D>());
        }
        if (id == 15)
        {
            PressurePlate2D p3 = Plate(parent, new Vector2(3.5f, -2.35f), "Plate-3");
            Door(parent, new Vector2(5.5f, -2f), "Door-3", p3);
        }
        if (id is >= 11 and <= 15)
        {
            CreateSnowfallZone(parent, -4.5f, "PeriodicSnowfall-LeftGap");
            CreateSnowfallZone(parent, 5f, "PeriodicSnowfall-RightGap");
        }
    }

    private static void CreateSnowfallZone(Transform parent, float x, string name)
    {
        GameObject snow = Prefab(SnowfallPrefabPath, parent, new Vector3(x, .5f), name);
        Vector3 scale = snow.transform.localScale; scale.x = .65f; snow.transform.localScale = scale;
        Record(snow.transform);
    }

    private static void ConfigureSnow007GroundVisual(GameObject cell)
    {
        SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
        renderer.sprite = Load<Sprite>(Snow007GroundSpritePath);
        renderer.color = Color.white;
        Record(renderer);
    }

    private static Tile EnsureSnow007TerrainTile()
    {
        TextureImporter importer = AssetImporter.GetAtPath(Snow007TerrainTexturePath) as TextureImporter;
        Require(importer != null, $"Missing asset: {Snow007TerrainTexturePath}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 1254f;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        Directory.CreateDirectory(Snow007TerrainTileFolder);
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(Snow007TerrainTilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, Snow007TerrainTilePath);
        }

        tile.sprite = Load<Sprite>(Snow007TerrainTexturePath);
        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.Grid;
        Record(tile);
        return tile;
    }

    private static PressurePlate2D Plate(Transform parent, Vector2 position, string name)
        => Prefab(PlatePrefabPath, parent, position, name).GetComponent<PressurePlate2D>();
    private static Door2D Door(Transform parent, Vector2 position, string name, PressurePlate2D plate)
    {
        Door2D door = Prefab(DoorPrefabPath, parent, position, name).GetComponent<Door2D>();
        door.ConfigureControlSource(plate); Record(door); return door;
    }
    private static void Enemy(Transform parent, float x, float left, float right, bool facingRight)
    {
        FreezablePatrolEnemy2D enemy = Prefab(EnemyPrefabPath, parent, new Vector3(x,-1.7f), $"Enemy-{x}").GetComponent<FreezablePatrolEnemy2D>();
        enemy.ConfigurePatrol(left, right, 2f, .35f, facingRight); Record(enemy);
    }
    private static GameObject Prefab(string path, Transform parent, Vector3 position, string name)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(path));
        instance.name = name; instance.transform.SetParent(parent, false); instance.transform.position = position;
        Record(instance.transform); return instance;
    }

    private static void CreateExits(int id, GameObject prefab, Transform parent)
    {
        int[] neighbors = Neighbors[id];
        for (int i = 0; i < neighbors.Length; i++)
        {
            float x = i == 0 ? -11.5f : i == 1 ? 11.1f : 9.5f;
            float y = i >= 2 && id >= 9 ? 3f : -2f;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = $"Exit to SNOW_{neighbors[i]:000}"; instance.transform.SetParent(parent, false);
            instance.transform.position = new Vector3(x, y, 0f);
            string targetEntrance = HasSourceEntrance(neighbors[i], id) ? $"FROM_SNOW_{id:000}" : "DEFAULT";
            RoomExit2D exit = instance.GetComponent<RoomExit2D>(); exit.Configure($"Snow_{neighbors[i]:000}", targetEntrance);
            Record(instance.transform); Record(exit);
        }
    }

    private static void CreateReturnEntrances(int id, Transform parent)
    {
        int[] neighbors = Neighbors[id];
        for (int i = 0; i < neighbors.Length; i++)
        {
            Vector3 position = i switch
            {
                0 => new Vector3(-10f, -1.08f, 0f),
                1 => new Vector3(9.75f, -1.08f, 0f),
                _ when id >= 9 => new Vector3(8.4f, 3.92f, 0f),
                _ => new Vector3(8.75f, -1.08f, 0f)
            };
            Transform marker = Marker($"Entrance-FROM_SNOW_{neighbors[i]:000}", position, parent);
            PlayerRoomAuthoring.ConfigureEntrance(marker, $"FROM_SNOW_{neighbors[i]:000}", false, i == 0);
        }
    }

    private static bool HasSourceEntrance(int targetRoom, int sourceRoom)
        => targetRoom == 3 ? sourceRoom == 4 : Neighbors.TryGetValue(targetRoom, out int[] neighbors) && neighbors.Contains(sourceRoom);

    private static Tilemap CreateTilemap(Transform parent, string name)
    { GameObject go = new(name); go.transform.SetParent(parent, false); Tilemap map = go.AddComponent<Tilemap>(); go.AddComponent<TilemapRenderer>(); return map; }
    private static void ConfigureSurface(Tilemap map, SurfaceSemantic2D.SurfaceType type, PhysicsMaterial2D material)
    {
        Rigidbody2D body = map.gameObject.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D composite = map.gameObject.AddComponent<CompositeCollider2D>(); composite.sharedMaterial = material;
        TilemapCollider2D collider = map.gameObject.AddComponent<TilemapCollider2D>(); collider.compositeOperation = Collider2D.CompositeOperation.Merge; collider.sharedMaterial = material;
        SurfaceSemantic2D semantic = map.gameObject.AddComponent<SurfaceSemantic2D>(); semantic.Configure(type, true, true);
        MirrorSurface2D mirror = map.gameObject.AddComponent<MirrorSurface2D>(); mirror.kind = MirrorSurface2D.SurfaceKind.Ground; mirror.safe = true;
    }
    private static void Fill(Tilemap map, TileBase tile, int minX, int maxX, int minY, int maxY)
    { int w=maxX-minX+1,h=maxY-minY+1; map.SetTilesBlock(new BoundsInt(minX,minY,0,w,h,1), Enumerable.Repeat(tile,w*h).ToArray()); }
    private static void Bake(Tilemap map) { map.CompressBounds(); map.RefreshAllTiles(); map.GetComponent<TilemapCollider2D>().ProcessTilemapChanges(); Physics2D.SyncTransforms(); map.GetComponent<CompositeCollider2D>().GenerateGeometry(); }
    private static Transform Marker(string name, Vector3 position, Transform parent) { GameObject go=new(name); go.transform.SetParent(parent,false); go.transform.position=position; return go.transform; }
    private static SpriteRenderer Visual(string name, Transform parent, Vector2 size, Color color, Sprite sprite)
    { GameObject go=new(name); go.transform.SetParent(parent,false); SpriteRenderer r=go.AddComponent<SpriteRenderer>(); r.sprite=sprite; r.color=color; Vector2 native=sprite.bounds.size; go.transform.localScale=new Vector3(size.x/native.x,size.y/native.y,1); return r; }
    private static CameraFollow2D CreateCamera(int roomId)
    {
        GameObject go = new("Main Camera"); go.tag = "MainCamera";
        go.transform.position = new Vector3(0, 0, -10);
        Camera camera = go.AddComponent<Camera>(); camera.orthographic = true;
        camera.orthographicSize = CameraOrthographicSize; camera.backgroundColor = new Color(.68f, .84f, .94f);
        go.AddComponent<AudioListener>();
        CameraFollow2D follow = go.AddComponent<CameraFollow2D>();
        follow.Configure(null, true); follow.ConfigureDamping(CameraSmoothTime);
        follow.ConfigureBounds(CameraBoundsFor(roomId));
        if (roomId == 7) follow.ConfigureEntryFramingBounds(Snow007EntryFramingBounds);
        return follow;
    }
    private static Rect CameraBoundsFor(int roomId) => roomId is 7 or 8 ? WideCameraBounds : DefaultCameraBounds;
    private static string RoomName(int id) => id switch {4=>"Frozen Step",5=>"Gate the Enemy",6=>"Mirror Twin Route",7=>"Warm Islands",8=>"Clone Freeze",9=>"Enemy Routing",10=>"Carrot for Snowman",11=>"Snowfall Shelter",12=>"Ice in Snowfall",13=>"Split Shelter",14=>"Frozen Stair",15=>"White Pendulum",_=>"Snow Room"};
    private static void Validate(Scene scene, int id, Tilemap terrain, Tilemap ice)
    {
        GameObject[] roots=scene.GetRootGameObjects();
        Require(terrain.GetComponent<SurfaceSemantic2D>()?.Type==SurfaceSemantic2D.SurfaceType.StaticSolid,$"SNOW_{id:000} Terrain semantic missing");
        Require(roots.SelectMany(r=>r.GetComponentsInChildren<PlayerController2D>(true)).Count()==0,$"SNOW_{id:000} serializes Player");
        Require(roots.SelectMany(r=>r.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count()==1,$"SNOW_{id:000} needs one spawner");
        RoomEntrance2D[] entrances = roots.SelectMany(r=>r.GetComponentsInChildren<RoomEntrance2D>(true)).ToArray();
        Require(entrances.Length==Neighbors[id].Length+1 && entrances.Count(value=>value.IsDefault)==1 &&
                entrances.Select(value=>value.EntranceId).Distinct().Count()==entrances.Length,
            $"SNOW_{id:000} source entrance configuration mismatch");
        Require(roots.SelectMany(r=>r.GetComponentsInChildren<RoomExit2D>(true)).Count()==Neighbors[id].Length,$"SNOW_{id:000} exit count mismatch");
        Camera camera = roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).Single();
        CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
        Require(camera.orthographic && Mathf.Approximately(camera.orthographicSize, CameraOrthographicSize) &&
                follow != null && follow.FollowsVertical && follow.UsesRoomBounds &&
                follow.RoomBounds == CameraBoundsFor(id) && Mathf.Approximately(follow.SmoothTime, CameraSmoothTime),
            $"SNOW_{id:000} bounded Player-follow camera mismatch");
        if (id == 7)
            Require(follow.AlignsEntryFramingToBounds && follow.EntryFramingBounds == Snow007EntryFramingBounds,
                "SNOW_007 entrance framing must align the live view with its configured composition bounds");
        if (ice.GetUsedTilesCount()>0) Require(ice.GetComponent<SurfaceSemantic2D>()?.Type==SurfaceSemantic2D.SurfaceType.FrozenGround,$"SNOW_{id:000} ice semantic missing");
    }
    private static void AddBuildScene(string path) { List<EditorBuildSettingsScene> scenes=EditorBuildSettings.scenes.ToList(); if(!scenes.Any(s=>s.path==path)) scenes.Add(new EditorBuildSettingsScene(path,true)); EditorBuildSettings.scenes=scenes.ToArray(); }
    private static T Load<T>(string path) where T:UnityEngine.Object { T value=AssetDatabase.LoadAssetAtPath<T>(path); Require(value!=null,$"Missing asset: {path}"); return value; }
    private static void Record(UnityEngine.Object value) { EditorUtility.SetDirty(value); PrefabUtility.RecordPrefabInstancePropertyModifications(value); }
    private static void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
}
