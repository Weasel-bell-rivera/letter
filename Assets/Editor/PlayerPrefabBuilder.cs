using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class PlayerPrefabBuilder
{
    public const string PlayerPrefabPath = "Assets/Prefabs/Gameplay/Characters/Player.prefab";
    public const string RegistryPath = "Assets/Resources/PlayerPrefabRegistry.asset";
    public const string MirrorVisualPrefabPath = "Assets/Prefabs/Gameplay/Mirrors/PlacedMirror.prefab";
    public const string MirrorSpritePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Coin/coin_gold_side.png";
    public const string MovementSpriteDirectory =
        "Assets/Art/Characters/Player/SilhouetteV1";
    public const string LegacySpriteDirectory =
        "Assets/Art/Characters/Player/HandDrawn";

    private const string MovementSettingsPath = "Assets/Settings/Player/DefaultPlayerMovement.asset";
    private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

    [MenuItem("Tools/W1/Build and Migrate Player Prefab")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    public static void BuildFromCommandLine()
    {
        EnsureDirectories();
        ConfigurePlayerSpriteImports();
        ConfigureMirrorSpriteImport();
        GameObject mirrorVisualPrefab = BuildMirrorVisualPrefab();
        GameObject playerPrefab = BuildPlayerPrefab(mirrorVisualPrefab);
        BuildRegistry(playerPrefab);
        MigrateLevelScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Player prefab built and level scenes migrated successfully.");
    }

    [MenuItem("Tools/W1/Build Mirror Visual Asset %#&m")]
    public static void BuildMirrorVisualFromMenu()
    {
        EnsureDirectories();
        ConfigureMirrorSpriteImport();
        GameObject mirrorVisualPrefab = BuildMirrorVisualPrefab();
        GameObject playerRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            MirrorPlayer2D mirror = playerRoot.GetComponent<MirrorPlayer2D>();
            Require(mirror != null, "Player.prefab is missing MirrorPlayer2D.");
            mirror.ConfigureVisualPrefab(mirrorVisualPrefab);
            EditorUtility.SetDirty(mirror);
            Require(PrefabUtility.SaveAsPrefabAsset(playerRoot, PlayerPrefabPath) != null,
                "Failed to assign the placed mirror visual to Player.prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(playerRoot);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Placed mirror visual built and assigned to Player.prefab.");
    }

    private static void EnsureDirectories()
    {
        Directory.CreateDirectory("Assets/Prefabs/Gameplay/Characters");
        Directory.CreateDirectory("Assets/Prefabs/Gameplay/Mirrors");
        Directory.CreateDirectory("Assets/Resources");
    }

    private static void ConfigurePlayerSpriteImports()
    {
        foreach (string path in AllSpritePaths())
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Require(importer != null, $"Missing Player sprite: {path}");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 284.44446f;
            importer.spritePivot = new Vector2(.5f, .5f);
            TextureImporterSettings textureSettings = new();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(textureSettings);
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureMirrorSpriteImport()
    {
        AssetDatabase.ImportAsset(MirrorSpritePath,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(MirrorSpritePath) as TextureImporter;
        Require(importer != null, $"Missing mirror sprite: {MirrorSpritePath}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128f;
        importer.spritePivot = new Vector2(.5f, .5f);
        TextureImporterSettings textureSettings = new();
        importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(textureSettings);
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static GameObject BuildMirrorVisualPrefab()
    {
        Sprite sprite = LoadSprite(MirrorSpritePath);
        GameObject root = new("PlacedMirror");
        GameObject visual = new("Visual");
        visual.transform.SetParent(root.transform, false);
        // The source image uses a 128px transparent canvas around an 80px-high glyph.
        // Scaling the child to .96 keeps the visible glyph at .6 units: one third of Player height.
        visual.transform.localScale = new Vector3(.96f, .96f, 1f);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 20;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, MirrorVisualPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        Require(prefab != null, "Failed to save PlacedMirror.prefab.");
        return prefab;
    }

    private static GameObject BuildPlayerPrefab(GameObject mirrorVisualPrefab)
    {
        AssetDatabase.ImportAsset(MovementSettingsPath,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(InputActionsPath,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        PlayerMovementSettings movement = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(MovementSettingsPath);
        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        Sprite[] idleFrames = LoadFrames("idle", 2);
        Sprite[] walkFrames = LoadFrames("walk", 8);
        Sprite[] jumpFrames = LoadFrames("jump", 11);
        float[] jumpFrameVerticalOffsets =
        {
            0f, -.00703125f, -.00703125f, -.07382812f, -.13710937f, -.15117186f,
            -.09140625f, 0f, 0f, -.01054687f, 0f
        };
        Sprite[] hitFrames = LoadFrames("hit", 4);
        Sprite[] happyFrames = LoadFrames("happy", 2);
        Require(movement != null, "DefaultPlayerMovement.asset is required.");
        Require(inputActions != null, "InputSystem_Actions.inputactions is required.");

        GameObject root = new("Player");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(.8f, 1.8f);

        GameObject visualObject = new("Visual");
        visualObject.transform.SetParent(root.transform, false);
        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = idleFrames[0];
        renderer.color = new Color(.035f, .04f, .05f, 1f);
        renderer.sortingOrder = 10;
        PlayerVisual2D visual = visualObject.AddComponent<PlayerVisual2D>();
        visual.Configure(renderer, idleFrames, walkFrames, jumpFrames, hitFrames, happyFrames,
            walkFps: 12f, jumpVerticalOffsets: jumpFrameVerticalOffsets);

        PlayerController2D controller = root.AddComponent<PlayerController2D>();
        controller.Configure(visualObject.transform, movement);
        root.AddComponent<FreezingVisual2D>();

        PlayerInput input = root.AddComponent<PlayerInput>();
        input.actions = inputActions;
        input.defaultActionMap = "Player";
        input.notificationBehavior = PlayerNotifications.SendMessages;

        MirrorPlayer2D mirror = root.AddComponent<MirrorPlayer2D>();
        mirror.Configure(controller);
        mirror.ConfigureVisualPrefab(mirrorVisualPrefab);
        mirror.SetInitiallyUnlocked(false);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        Require(prefab != null, "Failed to save Player.prefab.");
        return prefab;
    }

    private static void BuildRegistry(GameObject playerPrefab)
    {
        PlayerPrefabRegistry registry = AssetDatabase.LoadAssetAtPath<PlayerPrefabRegistry>(RegistryPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<PlayerPrefabRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
        }
        registry.Configure(playerPrefab);
        EditorUtility.SetDirty(registry);
    }

    private static void MigrateLevelScenes()
    {
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            string[] scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes/Levels" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .ToArray();
            foreach (string scenePath in scenePaths) MigrateScene(scenePath);
        }
        finally
        {
            if (previousSetup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }
    }

    private static void MigrateScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        PlayerController2D[] players = roots.SelectMany(root =>
            root.GetComponentsInChildren<PlayerController2D>(true)).ToArray();
        if (players.Length == 0 && roots.SelectMany(root =>
                root.GetComponentsInChildren<RoomResetSystem>(true)).Any() == false)
            return;
        Require(players.Length <= 1, $"{scenePath} contains more than one serialized Player.");

        Vector3 spawnPosition = players.Length == 1
            ? players[0].transform.position
            : ExistingEntrancePosition(roots);
        RoomEntrance2D existingDefault = roots.SelectMany(root =>
            root.GetComponentsInChildren<RoomEntrance2D>(true)).FirstOrDefault(candidate => candidate.IsDefault);
        bool facingRight = players.Length == 1 ? players[0].FacingRight : existingDefault?.FacingRight ?? true;
        Transform entrance = ConfigureEntrances(scene, spawnPosition, facingRight);

        foreach (RoomResetSystem reset in roots.SelectMany(root =>
                     root.GetComponentsInChildren<RoomResetSystem>(true)))
        {
            CameraFollow2D camera = roots.SelectMany(root =>
                root.GetComponentsInChildren<CameraFollow2D>(true)).FirstOrDefault();
            reset.Configure(null, null, entrance, camera);
        }

        foreach (CameraFollow2D follow in roots.SelectMany(root =>
                     root.GetComponentsInChildren<CameraFollow2D>(true)))
            follow.Configure(null, follow.FollowsVertical);

        GameObject host = roots.FirstOrDefault(root => root.name == "RoomSystems")
            ?? roots.FirstOrDefault(root => root.name.Contains("Room", StringComparison.OrdinalIgnoreCase))
            ?? roots.First();
        RoomPlayerSpawner2D[] spawners = roots.SelectMany(root =>
            root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).ToArray();
        RoomPlayerSpawner2D keeper = spawners.FirstOrDefault(candidate => candidate.gameObject.name == "RoomSystems")
            ?? spawners.FirstOrDefault();
        if (keeper == null) keeper = host.AddComponent<RoomPlayerSpawner2D>();
        foreach (RoomPlayerSpawner2D duplicate in spawners.Where(candidate => candidate != keeper))
            UnityEngine.Object.DestroyImmediate(duplicate);

        foreach (PlayerController2D player in players) UnityEngine.Object.DestroyImmediate(player.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene), $"Failed to save migrated scene: {scenePath}");
    }

    private static Transform ConfigureEntrances(Scene scene, Vector3 spawnPosition, bool facingRight)
    {
        Transform[] transforms = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .ToArray();
        Transform entranceParent = transforms.FirstOrDefault(candidate => candidate.name == "Entrances");
        if (entranceParent == null)
        {
            GameObject parent = new("Entrances");
            SceneManager.MoveGameObjectToScene(parent, scene);
            entranceParent = parent.transform;
        }

        Transform[] candidates = entranceParent.Cast<Transform>().ToArray();
        Transform selected = candidates.OrderBy(candidate =>
            Vector3.SqrMagnitude(candidate.position - spawnPosition)).FirstOrDefault();
        if (selected == null || Vector3.Distance(selected.position, spawnPosition) > .2f)
        {
            GameObject marker = new("DefaultEntrance");
            marker.transform.SetParent(entranceParent, false);
            marker.transform.position = spawnPosition;
            selected = marker.transform;
            candidates = entranceParent.Cast<Transform>().ToArray();
        }

        foreach (Transform candidate in candidates)
        {
            RoomEntrance2D component = candidate.GetComponent<RoomEntrance2D>();
            bool existingFacing = component == null || component.FacingRight;
            component ??= candidate.gameObject.AddComponent<RoomEntrance2D>();
            bool isDefault = candidate == selected;
            string id = InferEntranceId(candidate.name, isDefault);
            component.Configure(id, isDefault, isDefault ? facingRight : existingFacing);
        }
        return selected;
    }

    private static Vector3 ExistingEntrancePosition(GameObject[] roots)
    {
        RoomResetSystem reset = roots.SelectMany(root =>
            root.GetComponentsInChildren<RoomResetSystem>(true)).FirstOrDefault();
        if (reset != null && reset.Entrance != null) return reset.Entrance.position;
        RoomEntrance2D entrance = roots.SelectMany(root =>
            root.GetComponentsInChildren<RoomEntrance2D>(true)).FirstOrDefault(candidate => candidate.IsDefault);
        return entrance != null ? entrance.transform.position : Vector3.zero;
    }

    private static string InferEntranceId(string objectName, bool isDefault)
    {
        if (isDefault) return SaveIds.DefaultEntrance;
        Match match = Regex.Match(objectName.ToUpperInvariant(), @"(CENTER|FIRE|SNOW|WIND|EARTH)[-_ ]?(\d{3})");
        return match.Success ? $"FROM_{match.Groups[1].Value}_{match.Groups[2].Value}" :
            objectName.Trim().Replace(' ', '_').ToUpperInvariant();
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Require(sprite != null, $"Player sprite did not import as Sprite: {path}");
        return sprite;
    }

    private static Sprite[] LoadFrames(string animation, int count)
    {
        string directory = animation is "hit" or "happy" ? LegacySpriteDirectory : MovementSpriteDirectory;
        return Enumerable.Range(0, count)
        .Select(index => LoadSprite($"{directory}/player_{animation}_{index:00}.png"))
        .ToArray();
    }

    private static string[] AllSpritePaths() => new[]
    {
        (MovementSpriteDirectory, "idle", 2),
        (MovementSpriteDirectory, "walk", 8),
        (MovementSpriteDirectory, "jump", 11),
        (LegacySpriteDirectory, "hit", 4),
        (LegacySpriteDirectory, "happy", 2)
    }.SelectMany(animation => Enumerable.Range(0, animation.Item3)
        .Select(index => $"{animation.Item1}/player_{animation.Item2}_{index:00}.png"))
        .ToArray();

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

}
