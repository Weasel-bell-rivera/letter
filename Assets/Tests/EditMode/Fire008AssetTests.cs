using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class Fire008AssetTests
{
    private const string ScenePath = "Assets/Scenes/Levels/Fire/Fire_008.unity";
    private const string Snow001ScenePath = "Assets/Scenes/Levels/Snow/Snow_001.unity";
    private const string Wind002ScenePath = "Assets/Scenes/Levels/Wind/Wind_002.unity";
    private const string PressurePlatePrefabPath = "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab";
    private const string DoorPrefabPath = "Assets/Prefabs/Gameplay/Doors/Door2D.prefab";
    private const string DoorGroupPrefabPath = "Assets/Prefabs/Gameplay/Doors/PermanentLatchDoorGroup2D.prefab";
    private const string CheckpointPrefabPath = "Assets/Prefabs/Gameplay/Checkpoints/Checkpoint2D.prefab";
    private const string ExitPrefabPath = "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab";
    private const string TerrainTilePath = "Assets/Tiles/Graybox/Fire008Terrain.asset";
    private const string PlayerPrefabPath = "Assets/Prefabs/Gameplay/Characters/Player.prefab";
    private const string TerrainTexturePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_sand_block_center.png";
    private const string DoorClosedPath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Door/door_closed.png";
    private const string DoorClosedTopPath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Door/door_closed_top.png";
    private const string DoorOpenPath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Door/door_open.png";
    private const string DoorOpenTopPath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Door/door_open_top.png";
    private const string SwitchIdlePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Switch/switch_yellow.png";
    private const string SwitchPressedPath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Switch/switch_yellow_pressed.png";

    [Test]
    public void ReusableGameplayPrefabsExistAndHaveRequiredComponents()
    {
        GameObject plate = AssetDatabase.LoadAssetAtPath<GameObject>(PressurePlatePrefabPath);
        GameObject door = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath);
        GameObject group = AssetDatabase.LoadAssetAtPath<GameObject>(DoorGroupPrefabPath);
        GameObject checkpoint = AssetDatabase.LoadAssetAtPath<GameObject>(CheckpointPrefabPath);
        GameObject exit = AssetDatabase.LoadAssetAtPath<GameObject>(ExitPrefabPath);

        Assert.That(plate, Is.Not.Null);
        Assert.That(plate.GetComponent<PressurePlate2D>(), Is.Not.Null);
        Assert.That(door, Is.Not.Null);
        Assert.That(door.GetComponent<Door2D>(), Is.Not.Null);
        Assert.That(group, Is.Not.Null);
        Assert.That(group.GetComponent<PermanentLatchDoorGroup2D>(), Is.Not.Null);
        Assert.That(group.GetComponentsInChildren<PressurePlate2D>(true), Has.Length.EqualTo(2));
        Assert.That(group.GetComponentsInChildren<Door2D>(true), Has.Length.EqualTo(1));
        Assert.That(checkpoint, Is.Not.Null);
        Assert.That(checkpoint.GetComponent<Checkpoint2D>(), Is.Not.Null);
        Assert.That(exit, Is.Not.Null);
        Assert.That(exit.GetComponent<RoomExit2D>(), Is.Not.Null);
    }

    [Test]
    public void DoorGroupPrefabUsesEmptyTemplateIdAndNestedPrefabDependencies()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(DoorGroupPrefabPath);
        try
        {
            Assert.That(root.GetComponent<PermanentLatchDoorGroup2D>().DoorGroupId, Is.Empty);
            PressurePlate2D[] plates = root.GetComponentsInChildren<PressurePlate2D>(true);
            Door2D door = root.GetComponentInChildren<Door2D>(true);
            Assert.That(plates, Has.Length.EqualTo(2));
            Assert.That(plates.All(plate => PrefabUtility.IsPartOfPrefabInstance(plate)), Is.True);
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(door), Is.True);
            Transform legacyConnectionFeedback = root.transform.Find("ConnectionFeedback");
            Assert.That(legacyConnectionFeedback == null || !legacyConnectionFeedback.gameObject.activeSelf, Is.True,
                "Door groups must not draw lines between pressure plates and doors.");
            SerializedObject group = new(root.GetComponent<PermanentLatchDoorGroup2D>());
            Assert.That(group.FindProperty("connectionRenderers").arraySize, Is.Zero);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    [Test]
    public void Fire008TerrainTileUsesDefaultTerrainSprite()
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
        Sprite expected = AssetDatabase.LoadAssetAtPath<Sprite>(TerrainTexturePath);

        Assert.That(tile, Is.Not.Null);
        Assert.That(expected, Is.Not.Null);
        Assert.That(tile.sprite, Is.SameAs(expected));
        Assert.That(tile.color, Is.EqualTo(Color.white));
        Assert.That(tile.colliderType, Is.EqualTo(Tile.ColliderType.Grid));
    }

    [Test]
    public void DoorAndPressurePlatePrefabsUseApprovedStateSprites()
    {
        GameObject doorRoot = PrefabUtility.LoadPrefabContents(DoorPrefabPath);
        GameObject plateRoot = PrefabUtility.LoadPrefabContents(PressurePlatePrefabPath);
        try
        {
            Sprite closed = AssetDatabase.LoadAssetAtPath<Sprite>(DoorClosedPath);
            Sprite closedTop = AssetDatabase.LoadAssetAtPath<Sprite>(DoorClosedTopPath);
            Sprite open = AssetDatabase.LoadAssetAtPath<Sprite>(DoorOpenPath);
            Sprite openTop = AssetDatabase.LoadAssetAtPath<Sprite>(DoorOpenTopPath);
            Sprite switchIdle = AssetDatabase.LoadAssetAtPath<Sprite>(SwitchIdlePath);
            Sprite switchPressed = AssetDatabase.LoadAssetAtPath<Sprite>(SwitchPressedPath);

            Assert.That(doorRoot.transform.Find("Visual").GetComponent<SpriteRenderer>().sprite, Is.SameAs(closed));
            Assert.That(doorRoot.transform.Find("TopVisual").GetComponent<SpriteRenderer>().sprite, Is.SameAs(closedTop));
            SerializedObject door = new(doorRoot.GetComponent<Door2D>());
            Assert.That(door.FindProperty("closedBodySprite").objectReferenceValue, Is.SameAs(closed));
            Assert.That(door.FindProperty("closedTopSprite").objectReferenceValue, Is.SameAs(closedTop));
            Assert.That(door.FindProperty("openBodySprite").objectReferenceValue, Is.SameAs(open));
            Assert.That(door.FindProperty("openTopSprite").objectReferenceValue, Is.SameAs(openTop));
            Assert.That(door.FindProperty("initiallyOpen").boolValue, Is.False);
            Assert.That(door.FindProperty("controlSource").objectReferenceValue, Is.Null,
                "The reusable door prefab must only receive an explicit switch reference from its scene or door group.");
            BoxCollider2D doorCollider = doorRoot.GetComponent<BoxCollider2D>();
            Assert.That(doorCollider.enabled, Is.True);
            Assert.That(doorCollider.size, Is.EqualTo(new Vector2(1f, 2f)));
            Assert.That(doorRoot.transform.Find("Visual").localPosition, Is.EqualTo(new Vector3(0f, -.5f, 0f)));
            Assert.That(doorRoot.transform.Find("TopVisual").localPosition, Is.EqualTo(new Vector3(0f, .5f, 0f)));
            Assert.That(doorRoot.transform.Find("Visual").localScale, Is.EqualTo(Vector3.one));
            Assert.That(doorRoot.transform.Find("TopVisual").localScale, Is.EqualTo(Vector3.one));

            Assert.That(plateRoot.transform.Find("Visual").GetComponent<SpriteRenderer>().sprite, Is.SameAs(switchIdle));
            SerializedObject plate = new(plateRoot.GetComponent<PressurePlate2D>());
            Assert.That(plate.FindProperty("idleSprite").objectReferenceValue, Is.SameAs(switchIdle));
            Assert.That(plate.FindProperty("pressedSprite").objectReferenceValue, Is.SameAs(switchPressed));
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(doorRoot);
            PrefabUtility.UnloadPrefabContents(plateRoot);
        }
    }

    [Test]
    public void Fire008SceneContainsUniqueConfiguredPrefabInstances()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        PermanentLatchDoorGroup2D[] groups = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PermanentLatchDoorGroup2D>(true)).ToArray();
        string[] ids = groups.Select(group => group.DoorGroupId).ToArray();

        Assert.That(groups, Has.Length.EqualTo(3));
        Assert.That(ids, Is.EquivalentTo(new[]
        {
            SaveIds.Fire008DoorGroup01,
            SaveIds.Fire008DoorGroup02,
            SaveIds.Fire008DoorGroup03
        }));
        Assert.That(DoorGroupId.HasDuplicates(ids), Is.False);
        Assert.That(groups.All(group => PrefabUtility.GetPrefabInstanceStatus(group.gameObject) == PrefabInstanceStatus.Connected), Is.True);
        Assert.That(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PressurePlate2D>(true)).ToArray(), Has.Length.EqualTo(6));
        Checkpoint2D[] checkpoints = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Checkpoint2D>(true)).ToArray();
        Assert.That(checkpoints, Has.Length.EqualTo(2));
        Assert.That(checkpoints.All(checkpoint => PrefabUtility.GetPrefabInstanceStatus(checkpoint.gameObject) == PrefabInstanceStatus.Connected), Is.True);
        RoomExit2D exit = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<RoomExit2D>(true)).Single();
        Assert.That(PrefabUtility.GetPrefabInstanceStatus(exit.gameObject), Is.EqualTo(PrefabInstanceStatus.Connected));
        SpriteRenderer[] visibleConnections = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(false))
            .Where(renderer => renderer.name == "LineA" || renderer.name == "LineB")
            .ToArray();
        Assert.That(visibleConnections, Is.Empty, "Pressure plates and doors must not be joined by drawn lines.");
    }

    [Test]
    public void Fire008TilemapsExposeExplicitSurfaceSemantics()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Tilemap[] tilemaps = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true)).ToArray();
        SurfaceSemantic2D terrain = tilemaps.Single(tilemap => tilemap.name == "Terrain")
            .GetComponent<SurfaceSemantic2D>();
        SurfaceSemantic2D hazard = tilemaps.Single(tilemap => tilemap.name == "Hazard")
            .GetComponent<SurfaceSemantic2D>();
        Tilemap terrainTilemap = tilemaps.Single(tilemap => tilemap.name == "Terrain");

        Assert.That(terrain, Is.Not.Null);
        Assert.That(terrain.Type, Is.EqualTo(SurfaceSemantic2D.SurfaceType.StaticSolid));
        Assert.That(terrain.IsStatic, Is.True);
        Assert.That(terrain.IsSafe, Is.True);
        Assert.That(hazard, Is.Not.Null);
        Assert.That(hazard.Type, Is.EqualTo(SurfaceSemantic2D.SurfaceType.Hazard));
        Assert.That(hazard.IsStatic, Is.True);
        Assert.That(hazard.IsSafe, Is.False);
        Assert.That(terrainTilemap.HasTile(new Vector3Int(-11, 10, 0)), Is.True);
        Assert.That(terrainTilemap.HasTile(new Vector3Int(-11, 12, 0)), Is.True);
        Assert.That(terrainTilemap.HasTile(new Vector3Int(11, 3, 0)), Is.True);
        Assert.That(terrainTilemap.HasTile(new Vector3Int(11, 4, 0)), Is.True);
        Assert.That(terrainTilemap.HasTile(new Vector3Int(-13, -7, 0)), Is.True);
        Assert.That(terrainTilemap.HasTile(new Vector3Int(-13, -6, 0)), Is.True);
    }

    [Test]
    public void Fire008CameraUsesApprovedScaleDampingAndExplicitBounds()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Camera camera = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Single();
        CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        BoxCollider2D playerCollider = playerPrefab.GetComponent<BoxCollider2D>();

        Assert.That(camera.orthographic, Is.True);
        Assert.That(camera.orthographicSize, Is.EqualTo(7f).Within(.001f));
        float playerHeightRatio = playerCollider.size.y / (camera.orthographicSize * 2f);
        Assert.That(playerHeightRatio, Is.InRange(.12f, .14f));

        Assert.That(follow, Is.Not.Null);
        Assert.That(follow.Target, Is.Null, "The room camera binds to the spawned Player at runtime.");
        Assert.That(follow.FollowsVertical, Is.True);
        Assert.That(follow.SmoothTime, Is.EqualTo(.15f).Within(.001f));
        Assert.That(follow.UsesRoomBounds, Is.True);
        Assert.That(follow.RoomBounds, Is.EqualTo(new Rect(-14f, -14f, 29f, 28f)));

        RoomResetSystem reset = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<RoomResetSystem>(true)).Single();
        SerializedObject serializedReset = new(reset);
        Assert.That(serializedReset.FindProperty("cameraFollow").objectReferenceValue, Is.SameAs(follow));
        Assert.That(serializedReset.FindProperty("player").objectReferenceValue, Is.Null);
        Assert.That(serializedReset.FindProperty("mirror").objectReferenceValue, Is.Null);
    }

    [Test]
    public void ExistingDoorRoomsUseTwoTileDoorsAndStaticWallCaps()
    {
        AssertDoorRoomGeometry(ScenePath, 3, new[]
        {
            new Vector3Int(-11, 10, 0), new Vector3Int(-11, 12, 0),
            new Vector3Int(11, 3, 0), new Vector3Int(11, 4, 0),
            new Vector3Int(-13, -7, 0), new Vector3Int(-13, -6, 0)
        });
        AssertDoorRoomGeometry(Snow001ScenePath, 1, new[]
        {
            new Vector3Int(-1, 0, 0), new Vector3Int(-1, 4, 0)
        });
        AssertDoorRoomGeometry(Wind002ScenePath, 2, new[]
        {
            new Vector3Int(8, 9, 0), new Vector3Int(8, 12, 0),
            new Vector3Int(9, -2, 0), new Vector3Int(9, 0, 0)
        });
    }

    [Test]
    public void DuplicateDoorGroupIdsAreRejected()
    {
        Assert.That(DoorGroupId.HasDuplicates(new[]
        {
            SaveIds.Fire008DoorGroup01,
            SaveIds.Fire008DoorGroup02,
            SaveIds.Fire008DoorGroup01
        }), Is.True);
    }

    private static void AssertDoorRoomGeometry(string scenePath, int expectedDoorCount, Vector3Int[] wallCaps)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Door2D[] doors = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Door2D>(true)).ToArray();
        Tilemap terrain = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
            .Single(tilemap => tilemap.name == "Terrain");

        Assert.That(doors, Has.Length.EqualTo(expectedDoorCount), scenePath);
        Assert.That(doors.All(door => door.GetComponent<BoxCollider2D>().size == new Vector2(1f, 2f)),
            Is.True, $"{scenePath} contains a stretched door instance.");
        foreach (Vector3Int cell in wallCaps)
            Assert.That(terrain.HasTile(cell), Is.True, $"{scenePath} is missing the static wall cap at {cell}.");
    }
}
