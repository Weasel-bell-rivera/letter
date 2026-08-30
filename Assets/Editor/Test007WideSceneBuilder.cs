using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Copies test007 and expands it into a two-screen-wide horizontal visual test.
/// </summary>
public static class Test007WideSceneBuilder
{
    public const string SourceScenePath = "Assets/Scenes/Tests/test007.unity";
    public const string ScenePath = "Assets/Scenes/Tests/test007-1.unity";

    private const float RoomWidth = 50f;
    private const float ViewportWidth = 25f;
    private const float CoveragePadding = 4f;
    private static readonly Rect CameraBounds = new(-25f, -7f, RoomWidth, 14f);
    private static readonly Vector3 EntrancePosition = new(-21.5f, -2.2f, 0f);

    private static readonly string[] ParallaxLayerNames =
    {
        "01 Color and Fog Backdrop",
        "02 Extreme Far Contours",
        "03 Far Environment",
        "04 Mid Environment",
        "05 Rear Dynamic Fog",
        "07 Front Dynamic Fog and Particles",
        "08 Foreground Occlusion"
    };

    private const string ModuleRoot = "Assets/Art/Earth/Modules/Common/";
    private const string LegacyModuleRoot = "Assets/Art/Earth/Modules/test007_wide/";
    private const string RockWallPath = ModuleRoot + "earth_rock_wall_slab_v1.png";
    private const string RockPillarPath = ModuleRoot + "earth_rock_pillar_v1.png";
    private const string RockHalfArchPath = ModuleRoot + "earth_rock_half_arch_v1.png";
    private const string RockCeilingPath = ModuleRoot + "earth_rock_ceiling_overhang_v1.png";
    private const string TimberPostPath = ModuleRoot + "earth_timber_post_v1.png";
    private const string TimberBeamPath = ModuleRoot + "earth_timber_beam_v1.png";
    private const string TimberBracePath = ModuleRoot + "earth_timber_brace_v1.png";
    private const string TimberJointPath = ModuleRoot + "earth_timber_joint_cap_v1.png";
    private const string ForegroundSideWallPath = ModuleRoot + "earth_foreground_side_wall_v1.png";
    private const string ForegroundCeilingPath = ModuleRoot + "earth_foreground_ceiling_edge_v1.png";
    private const string ForegroundRubblePath = ModuleRoot + "earth_foreground_low_rubble_v1.png";
    private const string ForegroundTimberPath = ModuleRoot + "earth_foreground_timber_post_v1.png";

    private static readonly string[] CommonModulePaths =
    {
        RockWallPath,
        RockPillarPath,
        RockHalfArchPath,
        RockCeilingPath,
        TimberPostPath,
        TimberBeamPath,
        TimberBracePath,
        TimberJointPath,
        ForegroundSideWallPath,
        ForegroundCeilingPath,
        ForegroundRubblePath,
        ForegroundTimberPath
    };

    [MenuItem("Tools/W1/Build test007-1 Wide Earth Scene")]
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
        Require(roots.Length == 1, "test007 must contain exactly one root.");
        Transform root = roots[0].transform;
        root.name = "test007-1 - Wide Layered LIMBO Earth Cave";

        BuildModularVisualLayers(root);
        ExtendPlayablePlatform(root);
        ConfigureEntrance(root);
        ConfigureCamera(root);
        Validate(scene, root);

        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save test007-1 scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("test007-1 wide Earth scene built from test007 at 50 units wide.");
    }

    private static void BuildModularVisualLayers(Transform root)
    {
        Transform backdrop = RequireLayer(root, "01 Color and Fog Backdrop");
        Transform extremeFar = RequireLayer(root, "02 Extreme Far Contours");
        Transform far = RequireLayer(root, "03 Far Environment");
        Transform mid = RequireLayer(root, "04 Mid Environment");
        Transform rearFog = RequireLayer(root, "05 Rear Dynamic Fog");
        Transform frontFog = RequireLayer(root, "07 Front Dynamic Fog and Particles");
        Transform foreground = RequireLayer(root, "08 Foreground Occlusion");

        SpriteRenderer fogLight = backdrop.GetComponentInChildren<SpriteRenderer>(true);
        Require(fogLight != null, "Color and fog backdrop needs its source fog-light Sprite.");
        fogLight.name = "Extendable Fog Light Field";
        Vector3 fogScale = fogLight.transform.localScale;
        fogScale.x *= 2.2f;
        fogLight.transform.localScale = fogScale;

        ClearChildren(extremeFar);
        ClearChildren(far);
        ClearChildren(mid, "Mid Brown Dust");
        ClearChildren(foreground);

        Sprite rockWall = LoadModuleSprite(RockWallPath);
        Sprite rockPillar = LoadModuleSprite(RockPillarPath);
        Sprite rockHalfArch = LoadModuleSprite(RockHalfArchPath);
        Sprite rockCeiling = LoadModuleSprite(RockCeilingPath);
        Sprite timberPost = LoadModuleSprite(TimberPostPath);
        Sprite timberBeam = LoadModuleSprite(TimberBeamPath);
        Sprite timberBrace = LoadModuleSprite(TimberBracePath);
        Sprite timberJoint = LoadModuleSprite(TimberJointPath);
        Sprite foregroundSideWall = LoadModuleSprite(ForegroundSideWallPath);
        Sprite foregroundCeiling = LoadModuleSprite(ForegroundCeilingPath);
        Sprite foregroundRubble = LoadModuleSprite(ForegroundRubblePath);
        Sprite foregroundTimber = LoadModuleSprite(ForegroundTimberPath);

        CreateModuleByWidth(extremeFar, "Extreme Far Rock Wall Slab", rockWall,
            new Vector3(-17f, 3.2f, 3.3f), 15f, false, .2f);
        CreateModule(extremeFar, "Extreme Far Rock Pillar", rockPillar,
            new Vector3(-7f, -.1f, 3.28f), 12.8f, false, .28f);
        CreateModule(extremeFar, "Extreme Far Half Arch", rockHalfArch,
            new Vector3(7f, .5f, 3.26f), 10.5f, true, .25f);
        CreateModuleByWidth(extremeFar, "Extreme Far Ceiling Overhang", rockCeiling,
            new Vector3(18f, 4.5f, 3.24f), 13f, true, .15f);

        CreateModule(far, "Far Rock Pillar Left", rockPillar,
            new Vector3(-21f, -.5f, 2.7f), 10f, true, .38f);
        CreateTimberFrame(far, "Far Timber Frame A", new Vector3(-12f, -.7f, 2.68f),
            timberPost, timberBeam, timberBrace, timberJoint, 7.5f, 7.2f, .42f, false);
        CreateModule(far, "Far Rock Half Arch", rockHalfArch,
            new Vector3(1f, .1f, 2.66f), 9.5f, false, .36f);
        CreateTimberFrame(far, "Far Timber Frame B", new Vector3(14f, -.5f, 2.68f),
            timberPost, timberBeam, timberBrace, timberJoint, 7f, 7.8f, .38f, true);
        CreateModuleByWidth(far, "Far Rock Wall Slab Right", rockWall,
            new Vector3(22f, 3.2f, 2.64f), 10f, true, .32f);

        CreateModule(mid, "Mid Rock Pillar Left", rockPillar,
            new Vector3(-21f, -.6f, 2f), 11f, false, .62f);
        CreateTimberFrame(mid, "Mid Timber Frame A", new Vector3(-11f, -.4f, 1.98f),
            timberPost, timberBeam, timberBrace, timberJoint, 9.2f, 9f, .64f, true);
        CreateModuleByWidth(mid, "Mid Rock Wall Slab", rockWall,
            new Vector3(0f, -6.1f, 1.96f), 12f, false, .42f);
        CreateModule(mid, "Mid Rock Half Arch", rockHalfArch,
            new Vector3(7f, -.2f, 1.94f), 10.5f, true, .58f);
        CreateTimberFrame(mid, "Mid Timber Frame B", new Vector3(18f, -.7f, 1.98f),
            timberPost, timberBeam, timberBrace, timberJoint, 8.8f, 8f, .58f, false);

        CreateModule(foreground, "Foreground Side Wall Left", foregroundSideWall,
            new Vector3(-24.5f, 0f, -2f), 15.5f, false, .96f);
        CreateModule(foreground, "Foreground Side Wall Right", foregroundSideWall,
            new Vector3(24.5f, 0f, -2f), 15.5f, true, .96f);
        CreateModuleByWidth(foreground, "Foreground Ceiling Edge Left", foregroundCeiling,
            new Vector3(-16f, 6f, -2.05f), 16f, false, .9f);
        CreateModuleByWidth(foreground, "Foreground Ceiling Edge Center", foregroundCeiling,
            new Vector3(0f, 6.2f, -2.05f), 16f, true, .88f);
        CreateModuleByWidth(foreground, "Foreground Ceiling Edge Right", foregroundCeiling,
            new Vector3(16f, 6.05f, -2.05f), 16f, false, .9f);
        CreateModule(foreground, "Foreground Low Rubble Left", foregroundRubble,
            new Vector3(-13f, -6.3f, -2.1f), 4.5f, false, .94f);
        CreateModule(foreground, "Foreground Low Rubble Right", foregroundRubble,
            new Vector3(13f, -6.4f, -2.1f), 3.8f, true, .9f);
        CreateModule(foreground, "Foreground Timber Post Left", foregroundTimber,
            new Vector3(-21.5f, -.2f, -2.15f), 11f, false, .9f, 0f, .42f);
        CreateModule(foreground, "Foreground Timber Post Right", foregroundTimber,
            new Vector3(21.5f, -.5f, -2.15f), 10f, true, .86f, 0f, .42f);

        ConfigureFogCoverage(rearFog, RequiredCoverage(.8f));
        ConfigureFogCoverage(frontFog, RequiredCoverage(.35f));
    }

    private static Transform RequireLayer(Transform root, string name)
    {
        Transform layer = root.Find(name);
        Require(layer != null, $"Missing visual layer: {name}");
        return layer;
    }

    private static void ClearChildren(Transform parent, string preservedName = null)
    {
        foreach (Transform child in parent.Cast<Transform>()
                     .Where(child => child.name != preservedName).ToArray())
            UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static Sprite LoadModuleSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Require(importer != null, $"Missing wide-scene module: {path}");
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
        Require(sprite != null, $"Wide-scene module must import as a Sprite: {path}");
        return sprite;
    }

    private static void CreateModule(
        Transform parent, string name, Sprite sprite, Vector3 position,
        float targetHeight, bool flipHorizontal, float alpha, float rotationDegrees = 0f,
        float horizontalScale = 1f)
    {
        float scale = targetHeight / sprite.bounds.size.y;
        CreateModuleWithScale(parent, name, sprite, position, scale, flipHorizontal, alpha,
            rotationDegrees, horizontalScale);
    }

    private static void CreateModuleByWidth(
        Transform parent, string name, Sprite sprite, Vector3 position,
        float targetWidth, bool flipHorizontal, float alpha, float rotationDegrees = 0f,
        float horizontalScale = 1f)
    {
        float scale = targetWidth / sprite.bounds.size.x;
        CreateModuleWithScale(parent, name, sprite, position, scale, flipHorizontal, alpha,
            rotationDegrees, horizontalScale);
    }

    private static void CreateModuleWithScale(
        Transform parent, string name, Sprite sprite, Vector3 position,
        float scale, bool flipHorizontal, float alpha, float rotationDegrees,
        float horizontalScale)
    {
        GameObject module = new(name);
        module.transform.SetParent(parent, false);
        module.transform.localPosition = position;
        module.transform.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
        float xScale = scale * Mathf.Max(.01f, horizontalScale);
        module.transform.localScale = new Vector3(flipHorizontal ? -xScale : xScale, scale, 1f);
        SpriteRenderer renderer = module.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
    }

    private static void CreateTimberFrame(
        Transform parent, string name, Vector3 position,
        Sprite post, Sprite beam, Sprite brace, Sprite joint,
        float height, float width, float alpha, bool reverseBrace)
    {
        GameObject frame = new(name);
        frame.transform.SetParent(parent, false);
        frame.transform.localPosition = position;

        float halfWidth = width * .43f;
        float top = height * .42f;
        CreateModule(frame.transform, "Post Left", post,
            new Vector3(-halfWidth, 0f, 0f), height, false, alpha, 0f, .42f);
        CreateModule(frame.transform, "Post Right", post,
            new Vector3(halfWidth, 0f, 0f), height * .96f, true, alpha * .94f, 0f, .42f);
        CreateModuleByWidth(frame.transform, "Top Beam", beam,
            new Vector3(0f, top, -.01f), width, false, alpha);
        CreateModule(frame.transform, "Diagonal Brace", brace,
            new Vector3(0f, -.15f, -.02f), height * .78f, reverseBrace, alpha * .9f);
        CreateModuleByWidth(frame.transform, "Joint Cap Left", joint,
            new Vector3(-halfWidth, top, -.03f), 1.35f, false, alpha);
        CreateModuleByWidth(frame.transform, "Joint Cap Right", joint,
            new Vector3(halfWidth, top, -.03f), 1.35f, true, alpha * .94f);
    }

    private static void ConfigureFogCoverage(Transform fogLayer, float requiredWidth)
    {
        MeshRenderer[] fogCards = fogLayer.GetComponentsInChildren<MeshRenderer>(true);
        Require(fogCards.Length > 0, $"{fogLayer.name} needs at least one dynamic fog card.");
        foreach (MeshRenderer fogCard in fogCards)
        {
            Vector3 position = fogCard.transform.localPosition;
            position.x = 0f;
            fogCard.transform.localPosition = position;
            Vector3 scale = fogCard.transform.localScale;
            scale.x = Mathf.Max(Mathf.Abs(scale.x), requiredWidth);
            fogCard.transform.localScale = scale;
        }
    }

    private static float RequiredCoverage(float cameraFollowFactor)
    {
        float cameraTravel = RoomWidth - ViewportWidth;
        return ViewportWidth + cameraTravel * (1f - cameraFollowFactor) + CoveragePadding;
    }

    private static void ExtendPlayablePlatform(Transform root)
    {
        Transform platform = root.GetComponentsInChildren<Transform>(true)
            .Single(item => item.name == "Playable Platform");
        platform.localPosition = new Vector3(0f, -3.5f, -.02f);
        platform.localRotation = Quaternion.identity;
        platform.localScale = new Vector3(46f, .52f, 1f);

        Require(platform.GetComponent<BoxCollider2D>() != null,
            "Wide playable platform must retain its BoxCollider2D.");
        Require(platform.GetComponent<SurfaceSemantic2D>()?.Type ==
                SurfaceSemantic2D.SurfaceType.StaticSolid,
            "Wide playable platform must retain safe StaticSolid semantics.");
        Require(platform.GetComponent<MirrorSurface2D>()?.kind == MirrorSurface2D.SurfaceKind.Ground,
            "Wide playable platform must retain its ground mirror surface.");
    }

    private static void ConfigureEntrance(Transform root)
    {
        Transform entrance = root.GetComponentsInChildren<Transform>(true)
            .Single(item => item.name == "Entrance-DEFAULT");
        entrance.position = EntrancePosition;
    }

    private static void ConfigureCamera(Transform root)
    {
        Camera camera = root.GetComponentInChildren<Camera>(true);
        Require(camera != null, "test007-1 requires the copied Main Camera.");
        CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
        Require(follow != null, "test007-1 requires the copied CameraFollow2D.");
        follow.ConfigureBounds(CameraBounds);
        follow.ConfigureEntryFramingBounds(CameraBounds);
    }

    private static void Validate(Scene scene, Transform root)
    {
        Require(scene.path == ScenePath, "Wide scene path does not match test007-1.");
        Require(root.GetComponentsInChildren<PlayerController2D>(true).Length == 0,
            "test007-1 must not serialize a Player instance.");
        Require(root.GetComponentsInChildren<RoomEntrance2D>(true).Length == 1,
            "test007-1 needs exactly one Player entrance.");
        Require(root.GetComponentsInChildren<RoomPlayerSpawner2D>(true).Length == 1,
            "test007-1 needs exactly one Player spawner.");

        CameraFollow2D follow = root.GetComponentInChildren<CameraFollow2D>(true);
        Require(follow != null && follow.UsesRoomBounds && follow.RoomBounds == CameraBounds,
            "test007-1 camera bounds must cover the 50-unit-wide room.");
        Require(follow.AlignsEntryFramingToBounds && follow.EntryFramingBounds == CameraBounds,
            "test007-1 entry framing must use the wide room bounds.");

        foreach (string layerName in ParallaxLayerNames)
        {
            Transform layer = root.Find(layerName);
            Require(layer != null && layer.childCount > 0,
                $"{layerName} must contain visual content.");
            Require(layer.GetComponent<ParallaxLayer2D>() != null,
                $"{layerName} must retain its parallax component.");
            Require(layer.GetComponentsInChildren<Collider2D>(true).Length == 0,
                $"{layerName} must remain presentation-only.");
        }

        Require(root.GetComponentsInChildren<Transform>(true)
                .All(item => !item.name.Contains(" - Wide Left") &&
                             !item.name.Contains(" - Wide Right")),
            "Wide scene must not repeat complete source artwork left and right.");
        Transform[] modularLayers =
        {
            root.Find("02 Extreme Far Contours"),
            root.Find("03 Far Environment"),
            root.Find("04 Mid Environment"),
            root.Find("08 Foreground Occlusion")
        };
        string[] usedModulePaths = modularLayers
            .SelectMany(layer => layer.GetComponentsInChildren<SpriteRenderer>(true))
            .Select(renderer => AssetDatabase.GetAssetPath(renderer.sprite))
            .Where(path => path.StartsWith(ModuleRoot, StringComparison.Ordinal))
            .Distinct()
            .ToArray();
        Require(CommonModulePaths.All(usedModulePaths.Contains) &&
                usedModulePaths.Length == CommonModulePaths.Length,
            "Wide scene must demonstrate all twelve common Earth atomic modules.");
        Require(modularLayers
                .SelectMany(layer => layer.GetComponentsInChildren<SpriteRenderer>(true))
                .Select(renderer => AssetDatabase.GetAssetPath(renderer.sprite))
                .All(path => !path.StartsWith(LegacyModuleRoot, StringComparison.Ordinal)),
            "Wide scene must not depend on the legacy combined test007-wide modules.");
        Require(root.Find("02 Extreme Far Contours")
                .GetComponentsInChildren<SpriteRenderer>(true).Length >= 4,
            "Extreme-far layer must use at least four atomic modules.");
        Require(root.Find("03 Far Environment")
                .GetComponentsInChildren<SpriteRenderer>(true).Length >= 10,
            "Far layer must compose several atomic modules.");
        Require(root.Find("04 Mid Environment")
                .GetComponentsInChildren<SpriteRenderer>(true).Length >= 10,
            "Mid layer must compose several atomic modules.");
        Require(root.Find("08 Foreground Occlusion")
                .GetComponentsInChildren<SpriteRenderer>(true).Length >= 8,
            "Foreground must use independent atomic occlusion modules.");
        ValidateFogCoverage(root.Find("05 Rear Dynamic Fog"), RequiredCoverage(.8f));
        ValidateFogCoverage(root.Find("07 Front Dynamic Fog and Particles"), RequiredCoverage(.35f));
    }

    private static void ValidateFogCoverage(Transform layer, float requiredWidth)
    {
        MeshRenderer[] fogCards = layer.GetComponentsInChildren<MeshRenderer>(true);
        Require(fogCards.Length > 0 && fogCards.All(card =>
            Mathf.Abs(card.transform.localScale.x) >= requiredWidth - .01f),
            $"{layer.name} fog cards must cover their parallax travel width.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
