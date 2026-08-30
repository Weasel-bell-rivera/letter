using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds test007 from the existing layered test006 Earth-cave visual study,
/// then adds only the minimum playable footing and canonical Player spawn path.
/// </summary>
public static class Test007EarthLayeredSceneBuilder
{
    public const string SourceScenePath = "Assets/Scenes/Tests/test006.unity";
    public const string ScenePath = "Assets/Scenes/Tests/test007.unity";
    private static readonly string[] ConceptLayerPaths =
    {
        "Assets/Art/Earth/Backgrounds/test007_layers/test007_layer_01_fog_light_v1.png",
        "Assets/Art/Earth/Backgrounds/test007_layers/test007_layer_02_extreme_far_v1.png",
        "Assets/Art/Earth/Backgrounds/test007_layers/test007_layer_03_far_environment_v1.png",
        "Assets/Art/Earth/Backgrounds/test007_layers/test007_layer_04_mid_environment_v1.png"
    };

    private static readonly Rect CameraBounds = new(-12.5f, -7f, 25f, 14f);
    private static readonly Vector3 EntrancePosition = new(-2.3f, -2.2f, 0f);

    [MenuItem("Tools/W1/Build test007 Layered Earth Scene")]
    public static void BuildFromMenu()
    {
        Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) != null,
            $"Missing source scene: {SourceScenePath}");

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            Require(AssetDatabase.DeleteAsset(ScenePath), $"Could not replace {ScenePath}.");
        Require(AssetDatabase.CopyAsset(SourceScenePath, ScenePath),
            $"Could not copy {SourceScenePath} to {ScenePath}.");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.Length == 1, "test006 visual source must contain exactly one root.");
        GameObject root = roots[0];
        root.name = "test007 - Layered LIMBO Earth Cave";

        ConfigureConceptLayers(root.transform);
        RemoveReferenceCharacter(root.transform);
        GameObject platform = ConfigurePlatform(root.transform);
        ConfigurePlayerSpawn(root.transform);
        ConfigureCamera(root.transform);
        ConfigureParallaxLayers(root.transform);
        Validate(scene, platform);

        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save test007 scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("test007 layered Earth scene built with one platform and one Player spawn point.");
    }

    private static void ConfigureConceptLayers(Transform root)
    {
        Sprite[] sprites = ConceptLayerPaths.Select(LoadConceptLayer).ToArray();
        Transform backdrop = root.Find("01 Far Subterranean Chamber");
        Transform mid = root.Find("02 Mid Cave Columns and Mine Supports");
        Require(backdrop != null && mid != null,
            "test006 visual source is missing its background groups.");

        foreach (Transform child in backdrop.Cast<Transform>().ToArray())
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        foreach (Transform child in mid.Cast<Transform>()
                     .Where(child => child.name != "Mid Brown Dust").ToArray())
            UnityEngine.Object.DestroyImmediate(child.gameObject);

        Transform extremeFar = FindOrCreate(root, "02 Extreme Far Contours");
        Transform farEnvironment = FindOrCreate(root, "03 Far Environment");
        CreateLayerSprite(backdrop, "Fog Light Color Field", sprites[0], 3.9f);
        CreateLayerSprite(extremeFar, "Extreme Far Contours Artwork", sprites[1], 3.3f);
        CreateLayerSprite(farEnvironment, "Far Environment Artwork", sprites[2], 2.7f);
        CreateLayerSprite(mid, "Mid Environment Artwork", sprites[3], 2f);
    }

    private static Sprite LoadConceptLayer(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Require(importer != null, $"Missing concept layer texture: {path}");
        if (importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single || importer.mipmapEnabled)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Require(sprite != null, $"Concept layer must be imported as a Sprite: {path}");
        return sprite;
    }

    private static void CreateLayerSprite(Transform parent, string name, Sprite sprite, float z)
    {
        GameObject layer = new(name);
        layer.transform.SetParent(parent, false);
        layer.transform.localPosition = new Vector3(0f, 0f, z);
        SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        Vector2 sourceSize = sprite.bounds.size;
        layer.transform.localScale = new Vector3(25f / sourceSize.x, 14f / sourceSize.y, 1f);
    }

    private static void RemoveReferenceCharacter(Transform root)
    {
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                     .Where(item => item.name.StartsWith("Reference Player", StringComparison.Ordinal))
                     .ToArray())
            UnityEngine.Object.DestroyImmediate(item.gameObject);
    }

    private static GameObject ConfigurePlatform(Transform root)
    {
        Transform walkway = root.GetComponentsInChildren<Transform>(true)
            .Single(item => item.name == "Primary Walkway");
        walkway.name = "Playable Platform";

        BoxCollider2D collider = walkway.gameObject.GetComponent<BoxCollider2D>();
        if (collider == null) collider = walkway.gameObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1f, 1f);
        collider.offset = Vector2.zero;

        SurfaceSemantic2D semantic = walkway.gameObject.GetComponent<SurfaceSemantic2D>();
        if (semantic == null) semantic = walkway.gameObject.AddComponent<SurfaceSemantic2D>();
        semantic.Configure(SurfaceSemantic2D.SurfaceType.StaticSolid, true, true);
        MirrorSurface2D mirror = walkway.gameObject.GetComponent<MirrorSurface2D>();
        if (mirror == null) mirror = walkway.gameObject.AddComponent<MirrorSurface2D>();
        mirror.kind = MirrorSurface2D.SurfaceKind.Ground;
        mirror.safe = true;
        return walkway.gameObject;
    }

    private static void ConfigurePlayerSpawn(Transform root)
    {
        Transform gameplay = FindOrCreate(root, "Gameplay");
        Transform entrances = FindOrCreate(gameplay, "Entrances");
        Transform entrance = FindOrCreate(entrances, "Entrance-DEFAULT");
        entrance.position = EntrancePosition;

        Transform systemsTransform = FindOrCreate(root, "RoomSystems");
        GameObject systems = systemsTransform.gameObject;
        RoomResetSystem reset = systems.GetComponent<RoomResetSystem>();
        if (reset == null) reset = systems.AddComponent<RoomResetSystem>();
        CameraFollow2D cameraFollow = root.GetComponentInChildren<CameraFollow2D>(true);
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance, reset, cameraFollow, true);
    }

    private static void ConfigureCamera(Transform root)
    {
        Camera camera = root.GetComponentInChildren<Camera>(true);
        Require(camera != null, "test007 requires the inherited Main Camera.");
        CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
        if (follow == null) follow = camera.gameObject.AddComponent<CameraFollow2D>();
        follow.Configure(null, false);
        follow.ConfigureDamping(.2f);
        follow.ConfigureBounds(CameraBounds);
        follow.ConfigureEntryFramingBounds(CameraBounds);

        RoomPlayerSpawner2D spawner = root.GetComponentInChildren<RoomPlayerSpawner2D>(true);
        spawner?.ConfigureCamera(follow);
    }

    private static void ConfigureParallaxLayers(Transform root)
    {
        Camera camera = root.GetComponentInChildren<Camera>(true);
        Require(camera != null, "test007 parallax requires the Main Camera.");

        Transform backdrop = root.Find("01 Far Subterranean Chamber");
        Transform mid = root.Find("02 Mid Cave Columns and Mine Supports");
        Transform gameplay = root.Find("03 Gameplay Readability Plane");
        Transform frontAtmosphere = root.Find("04 Near Drifting Dust");
        Transform foreground = root.Find("05 Foreground Cave Mouth");
        Require(backdrop != null && mid != null && gameplay != null &&
                frontAtmosphere != null && foreground != null,
            "test007 source visual groups are incomplete.");

        backdrop.name = "01 Color and Fog Backdrop";
        mid.name = "04 Mid Environment";
        gameplay.name = "06 Gameplay Terrain and Characters";
        frontAtmosphere.name = "07 Front Dynamic Fog and Particles";
        foreground.name = "08 Foreground Occlusion";

        Transform extremeFar = FindOrCreate(root, "02 Extreme Far Contours");
        Transform farEnvironment = FindOrCreate(root, "03 Far Environment");
        Transform rearFog = FindOrCreate(root, "05 Rear Dynamic Fog");
        Transform midFog = mid.Find("Mid Brown Dust");
        if (midFog != null) midFog.SetParent(rearFog, true);

        ConfigureParallax(backdrop, camera.transform, 1f);
        ConfigureParallax(extremeFar, camera.transform, .95f);
        ConfigureParallax(farEnvironment, camera.transform, .85f);
        ConfigureParallax(mid, camera.transform, .65f);
        ConfigureParallax(rearFog, camera.transform, .8f);
        ConfigureParallax(frontAtmosphere, camera.transform, .35f);
        ConfigureParallax(foreground, camera.transform, .2f);

        ParallaxLayer2D gameplayParallax = gameplay.GetComponent<ParallaxLayer2D>();
        if (gameplayParallax != null) UnityEngine.Object.DestroyImmediate(gameplayParallax);
    }

    private static void ConfigureParallax(Transform layer, Transform camera, float factor)
    {
        ParallaxLayer2D parallax = layer.GetComponent<ParallaxLayer2D>();
        if (parallax == null) parallax = layer.gameObject.AddComponent<ParallaxLayer2D>();
        parallax.Configure(camera, factor, true, false);
    }

    private static Transform FindOrCreate(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) return child;
        GameObject created = new(name);
        created.transform.SetParent(parent, false);
        return created.transform;
    }

    private static void Validate(Scene scene, GameObject platform)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(platform.GetComponent<BoxCollider2D>() != null, "Playable platform needs a BoxCollider2D.");
        Require(platform.GetComponent<SurfaceSemantic2D>()?.Type == SurfaceSemantic2D.SurfaceType.StaticSolid,
            "Playable platform needs safe StaticSolid semantics.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController2D>(true)).Count() == 0,
            "test007 must not serialize a Player instance.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomEntrance2D>(true)).Count() == 1,
            "test007 needs exactly one Player entrance.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomPlayerSpawner2D>(true)).Count() == 1,
            "test007 needs exactly one Player spawner.");
        Require(roots.SelectMany(root => root.GetComponentsInChildren<RoomResetSystem>(true)).Count() == 1,
            "test007 needs exactly one reset system.");
        Transform visualRoot = roots.Select(root => root.transform).SingleOrDefault(root =>
            root.Find("01 Color and Fog Backdrop") != null);
        Require(visualRoot != null, "test007 needs one eight-layer environment root.");

        ValidateParallaxLayer(visualRoot, "01 Color and Fog Backdrop", 1f);
        ValidateParallaxLayer(visualRoot, "02 Extreme Far Contours", .95f);
        ValidateParallaxLayer(visualRoot, "03 Far Environment", .85f);
        ValidateParallaxLayer(visualRoot, "04 Mid Environment", .65f);
        ValidateParallaxLayer(visualRoot, "05 Rear Dynamic Fog", .8f);
        ValidateParallaxLayer(visualRoot, "07 Front Dynamic Fog and Particles", .35f);
        ValidateParallaxLayer(visualRoot, "08 Foreground Occlusion", .2f);

        Transform gameplay = visualRoot.Find("06 Gameplay Terrain and Characters");
        Require(gameplay != null && gameplay.GetComponent<ParallaxLayer2D>() == null,
            "Gameplay layer must stay in world space without parallax.");

        Transform rearFog = visualRoot.Find("05 Rear Dynamic Fog/Mid Brown Dust");
        Transform frontFog = visualRoot.Find("07 Front Dynamic Fog and Particles/Near Fine Dust");
        Require(rearFog != null && rearFog.position.z > gameplay.position.z,
            "Rear dynamic fog must be grouped behind the gameplay layer.");
        Require(frontFog != null && frontFog.position.z < gameplay.position.z,
            "Front dynamic fog must be grouped in front of the gameplay layer.");
        Require(visualRoot.Find("01 Color and Fog Backdrop/Fog Light Color Field") != null &&
                visualRoot.Find("02 Extreme Far Contours/Extreme Far Contours Artwork") != null &&
                visualRoot.Find("03 Far Environment/Far Environment Artwork") != null &&
                visualRoot.Find("04 Mid Environment/Mid Environment Artwork") != null,
            "The first four environment layers must use independent artwork sprites.");
    }

    private static void ValidateParallaxLayer(Transform visualRoot, string layerName, float factor)
    {
        Transform layer = visualRoot.Find(layerName);
        ParallaxLayer2D parallax = layer != null ? layer.GetComponent<ParallaxLayer2D>() : null;
        Require(parallax != null, $"{layerName} needs a ParallaxLayer2D component.");
        Require(Mathf.Approximately(parallax.CameraFollowFactor, factor),
            $"{layerName} must use camera follow factor {factor:0.##}.");
        Require(parallax.FollowsHorizontal && !parallax.FollowsVertical,
            $"{layerName} must use horizontal-only parallax.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
