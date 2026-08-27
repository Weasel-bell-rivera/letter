using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BackgroundLavaDripPrefabBuilder
{
    public const string PrefabPath =
        "Assets/Prefabs/Visual/Regions/Fire/BackgroundLavaDrip2D.prefab";
    public const string DropMaterialPath =
        "Assets/Materials/Regions/Fire/Background/M_BackgroundLavaDrop.mat";
    public const string SplashMaterialPath =
        "Assets/Materials/Regions/Fire/Background/M_BackgroundLavaSplash.mat";
    public const string Earth001ScenePath = "Assets/Scenes/Levels/Earth/Earth_001.unity";
    public const string Earth001InstanceName = "BackgroundLavaDrip-A";

    public static readonly Vector3 Earth001Position = new(-9f, 8.5f, 0f);

    private const string StreamSpritePath =
        "Assets/Art/Regions/Fire/Background/lava_stream.png";
    private const string DropSpritePath =
        "Assets/Art/Regions/Fire/Background/lava_drop.png";
    private const string SplashSpritePath =
        "Assets/Art/Regions/Fire/Background/lava_splash.png";
    private const string GlowSpritePath =
        "Assets/Art/Regions/Fire/Background/lava_glow.png";

    [MenuItem("Tools/Letter/Rebuild Background Lava Drip Prefab")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    [MenuItem("Tools/Letter/Rebuild Background Lava Drip And Place In EARTH-001")]
    public static void BuildAndPlaceInEarth001FromMenu() => BuildAndPlaceInEarth001();

    public static void BuildFromCommandLine()
    {
        BuildAssets();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created background lava drip prefab at {PrefabPath}.");
    }

    public static void BuildAndPlaceInEarth001()
    {
        GameObject prefab = BuildAssets();
        PlaceInEarth001(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created {PrefabPath} and placed {Earth001InstanceName} in EARTH_001.");
    }

    public static GameObject BuildAssets()
    {
        EnsureDirectory(Path.GetDirectoryName(PrefabPath));
        EnsureDirectory(Path.GetDirectoryName(DropMaterialPath));

        Sprite stream = ConfigureSprite(StreamSpritePath);
        Sprite drop = ConfigureSprite(DropSpritePath);
        Sprite splash = ConfigureSprite(SplashSpritePath);
        Sprite glow = ConfigureSprite(GlowSpritePath);
        Material dropMaterial = CreateOrUpdateParticleMaterial(DropMaterialPath, drop.texture);
        Material splashMaterial = CreateOrUpdateParticleMaterial(SplashMaterialPath, splash.texture);

        GameObject root = new("BackgroundLavaDrip2D");
        try
        {
            GameObject visual = Child("Visual", root.transform);
            CreateSprite("Glow", visual.transform, glow, new Vector3(0f, -2.4f, 0f),
                new Vector3(.95f, .8f, 1f), new Color(1f, .42f, .12f, .4f), -110);
            CreateSprite("Stream", visual.transform, stream, new Vector3(0f, -1.8f, 0f),
                new Vector3(.55f, .6f, 1f), new Color(.9f, .38f, .16f, .86f), -100);
            CreateDropParticles(Child("FallingDrops", root.transform), dropMaterial);
            CreateSplashParticles(Child("BottomSplashes", root.transform), splashMaterial);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Require(saved != null, $"Failed to save background lava drip prefab: {PrefabPath}");
            return saved;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Sprite ConfigureSprite(string path)
    {
        AssetDatabase.ImportAsset(path,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Require(importer != null, $"Missing background lava source image: {path}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 256f;
        importer.spritePivot = new Vector2(.5f, .5f);
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        TextureImporterSettings settings = new();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Require(sprite != null, $"Background lava image did not import as a Sprite: {path}");
        return sprite;
    }

    private static Material CreateOrUpdateParticleMaterial(string path, Texture texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ??
                        Shader.Find("Sprites/Default");
        Require(shader != null, "No compatible transparent Sprite shader is available.");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        material.name = Path.GetFileNameWithoutExtension(path);
        material.mainTexture = texture;
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreateSprite(string name, Transform parent, Sprite sprite, Vector3 position,
        Vector3 scale, Color color, int sortingOrder)
    {
        GameObject child = Child(name, parent);
        child.transform.localPosition = position;
        child.transform.localScale = scale;
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private static void CreateDropParticles(GameObject gameObject, Material material)
    {
        gameObject.transform.localPosition = new Vector3(0f, -3.1f, 0f);
        ParticleSystem particles = gameObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 5f;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.25f, 1.65f);
        main.startSpeed = 0f;
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(.24f, .38f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(.55f, .8f);
        main.startSizeZ = 1f;
        main.startRotation = new ParticleSystem.MinMaxCurve(-.08f, .08f);
        main.startColor = new Color(1f, .68f, .34f, .82f);
        main.maxParticles = 28;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 2.4f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(.5f, .05f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-.16f, .16f);
        velocity.y = new ParticleSystem.MinMaxCurve(-2.6f, -2f);
        velocity.z = 0f;

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
        noise.strength = .08f;
        noise.frequency = .55f;
        noise.scrollSpeed = .18f;
        noise.damping = true;

        ConfigureFade(particles, .08f, .82f);
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortingOrder = -95;
    }

    private static void CreateSplashParticles(GameObject gameObject, Material material)
    {
        gameObject.transform.localPosition = new Vector3(0f, -6.15f, 0f);
        ParticleSystem particles = gameObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 5f;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .55f);
        main.startSpeed = 0f;
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(.65f, .95f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(.4f, .6f);
        main.startSizeZ = 1f;
        main.startColor = new Color(1f, .54f, .2f, .62f);
        main.maxParticles = 8;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 1.2f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = false;
        ConfigureFade(particles, .04f, .64f);

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortingOrder = -94;
    }

    private static void ConfigureFade(ParticleSystem particles, float fadeInEnd, float fadeOutStart)
    {
        Gradient gradient = new();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, fadeInEnd),
                new GradientAlphaKey(1f, fadeOutStart),
                new GradientAlphaKey(0f, 1f)
            });
        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = gradient;
    }

    private static void PlaceInEarth001(GameObject prefab)
    {
        Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(Earth001ScenePath) != null,
            $"Missing EARTH_001 scene: {Earth001ScenePath}");
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        Scene scene = EditorSceneManager.OpenScene(Earth001ScenePath, OpenSceneMode.Single);
        try
        {
            Transform background = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Single(transform => transform.name == "Background");
            Transform existing = background.Find(Earth001InstanceName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = Earth001InstanceName;
            instance.transform.SetParent(background, false);
            instance.transform.position = Earth001Position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, Earth001ScenePath),
                "Failed to save EARTH_001 with its background lava drip instance.");
        }
        finally
        {
            if (previousSetup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }
    }

    private static GameObject Child(string name, Transform parent)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void EnsureDirectory(string path)
    {
        if (!string.IsNullOrEmpty(path)) Directory.CreateDirectory(path);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
