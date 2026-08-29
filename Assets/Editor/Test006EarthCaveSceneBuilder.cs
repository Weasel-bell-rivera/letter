using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Builds a visual-only LIMBO-style underground earth cave in layered brown silhouettes.</summary>
public static class Test006EarthCaveSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Tests/test006.unity";
    public const float RoomWidth = 25f;
    public const float RoomHeight = 14f;
    public const float OrthographicSize = 7f;
    public const float ReferencePlayerHeight = 1.8f;

    private const string DustShaderName = "W1/Environment/CaveDust2D";
    private const string GlowShaderName = "W1/Environment/RadialCaveGlow2D";
    private const string RockShaderName = "W1/Environment/CaveRockSurface2D";
    private const string SoftRockShaderName = "W1/Environment/SoftCaveRockSurface2D";
    private const string MaterialDirectory = "Assets/Materials/Tests/Test006";
    private const string MeshDirectory = "Assets/Meshes/Tests/Test006";
    private const string VolumeProfilePath = MaterialDirectory + "/Test006CaveVolume.asset";

    [MenuItem("Tools/W1/Build test006 Earth Cave")]
    public static void BuildFromMenu()
    {
        Shader dustShader = Shader.Find(DustShaderName);
        Shader glowShader = Shader.Find(GlowShaderName);
        Shader rockShader = Shader.Find(RockShaderName);
        Shader softRockShader = Shader.Find(SoftRockShaderName);
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        Shader spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        Require(dustShader != null, $"Missing shader: {DustShaderName}");
        Require(glowShader != null, $"Missing shader: {GlowShaderName}");
        Require(rockShader != null, $"Missing shader: {RockShaderName}");
        Require(softRockShader != null, $"Missing shader: {SoftRockShaderName}");
        Require(unlitShader != null, "Missing URP Unlit shader.");
        Require(spriteShader != null, "Missing URP 2D Sprite Unlit shader.");

        EnsureFolder("Assets/Materials/Tests", "Test006");
        EnsureFolder("Assets/Meshes/Tests", "Test006");

        Mesh rock = MeshAsset("IrregularCaveRock", new[]
        {
            new Vector2(-.5f, -.28f), new Vector2(-.34f, -.5f), new Vector2(.12f, -.47f),
            new Vector2(.48f, -.24f), new Vector2(.43f, .2f), new Vector2(.16f, .5f),
            new Vector2(-.28f, .42f), new Vector2(-.49f, .1f)
        });
        Mesh spike = MeshAsset("CaveTooth", new[]
        {
            new Vector2(-.5f, .5f), new Vector2(.5f, .5f), new Vector2(.3f, .2f),
            new Vector2(.12f, -.18f), new Vector2(0f, -.5f), new Vector2(-.16f, -.12f),
            new Vector2(-.34f, .22f)
        });
        Mesh softRock = FeatheredMeshAsset("FeatheredMidCaveRock", new[]
        {
            new Vector2(-.5f, -.28f), new Vector2(-.34f, -.5f), new Vector2(.12f, -.47f),
            new Vector2(.48f, -.24f), new Vector2(.43f, .2f), new Vector2(.16f, .5f),
            new Vector2(-.28f, .42f), new Vector2(-.49f, .1f)
        }, .84f);

        Material deepVoid = Solid("DeepVoid", unlitShader, new Color(.018f, .011f, .007f));
        Material foregroundRock = Rock("ForegroundRock", rockShader,
            new Color(.052f, .03f, .016f), new Color(.018f, .01f, .006f), 4.5f, .18f, .18f, .08f);
        Material gameplayRock = Rock("GameplayRock", rockShader,
            new Color(.105f, .058f, .027f), new Color(.045f, .022f, .01f), 6f, .14f, .08f, .06f);
        Material midRock = Rock("MidRock", softRockShader,
            new Color(.225f, .14f, .074f), new Color(.105f, .055f, .025f), 5f, .28f, .22f, .13f);
        Material farRock = Rock("FarRock", rockShader,
            new Color(.39f, .275f, .17f), new Color(.225f, .135f, .07f), 4f, .24f, .16f, .1f);
        Material chamberGlow = Solid("ChamberGlow", unlitShader, new Color(.49f, .355f, .22f));
        Material timber = Solid("OldTimber", unlitShader, new Color(.065f, .035f, .017f));
        Material silhouette = Solid("CharacterSilhouette", unlitShader, new Color(.012f, .008f, .005f));
        Material dustParticle = Solid("DustParticle", spriteShader, new Color(.72f, .55f, .31f));
        Material farDust = Dust("FarDust", dustShader, new Color(.55f, .4f, .25f), .34f, 1.8f,
            6f, new Vector2(.007f, .0015f), .58f, .3f, .5f, .82f, .72f);
        Material midDust = Dust("MidDust", dustShader, new Color(.42f, .29f, .17f), .24f, 3.1f,
            10f, new Vector2(-.014f, .003f), .52f, .24f, .43f, .52f, .8f);
        Material nearDust = Dust("NearDust", dustShader, new Color(.3f, .19f, .105f), .15f, 5f,
            15f, new Vector2(.025f, -.002f), .46f, .2f, .34f, .36f, .86f);
        Material groundDust = Dust("GroundDust", dustShader, new Color(.42f, .29f, .16f), .21f, 3.8f,
            12f, new Vector2(.018f, .001f), .53f, .23f, .2f, .22f, .96f);
        Material edgeHaze = Dust("ForegroundEdgeHaze", dustShader, new Color(.25f, .15f, .075f), .11f, 2.6f,
            8f, new Vector2(-.008f, .001f), .5f, .3f, .5f, .95f, .42f);
        Material tunnelGlow = Glow("TunnelGlow", glowShader, new Color(.76f, .57f, .34f), .68f, 2.2f);
        Material topGlow = Glow("TopCreviceGlow", glowShader, new Color(.67f, .45f, .24f), .34f, 3.4f);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new("test006 - LIMBO Earth Cave Visual Lab");
        CreateCamera(root.transform);
        CreateLight(root.transform);
        CreatePostProcessing(root.transform);
        CreateSpecificationMarkers(root.transform);

        Transform far = Child(root.transform, "01 Far Subterranean Chamber").transform;
        Quad(far, "Deep Brown Chamber", new Vector3(0, 0, 4),
            new Vector2(RoomWidth + .5f, RoomHeight + .5f), chamberGlow);
        CreateFarCaveContours(far, rock, farRock, midRock);
        CreateFarCaveOpening(far, rock, deepVoid, farRock);
        Quad(far, "Soft Tunnel Backlight", new Vector3(5.5f, -.2f, 3.25f),
            new Vector2(10.5f, 12f), tunnelGlow);
        Quad(far, "Soft Ceiling Crevice Light", new Vector3(-2.4f, 4.3f, 3.12f),
            new Vector2(8f, 7f), topGlow, -7f);
        Quad(far, "Narrow Volumetric Ray Left", new Vector3(2.75f, 1.3f, 3.08f),
            new Vector2(2.3f, 11f), topGlow, -8f);
        Quad(far, "Narrow Volumetric Ray Right", new Vector3(6.1f, .7f, 3.07f),
            new Vector2(1.4f, 9.5f), topGlow, 5f);
        Quad(far, "Far Suspended Dust", new Vector3(0, -.1f, 2.9f),
            new Vector2(RoomWidth + .5f, RoomHeight), farDust);

        Transform middle = Child(root.transform, "02 Mid Cave Columns and Mine Supports").transform;
        CreateMidColumns(middle, softRock, midRock);
        CreateMineSupports(middle, timber);
        Quad(middle, "Mid Brown Dust", new Vector3(0, -.7f, 1.1f),
            new Vector2(RoomWidth + .5f, 12.5f), midDust);

        Transform gameplay = Child(root.transform, "03 Gameplay Readability Plane").transform;
        CreateGameplayShelves(gameplay, rock, gameplayRock, foregroundRock);
        CreateReferenceCharacter(gameplay, silhouette);
        CreateStalactites(gameplay, spike, gameplayRock);
        CreateFallingDust(gameplay, dustParticle);
        CreateFloatingMotes(gameplay, dustParticle);
        Quad(gameplay, "Ground Hugging Dust", new Vector3(0, -3.45f, -.5f),
            new Vector2(RoomWidth + .5f, 4f), groundDust);

        Transform near = Child(root.transform, "04 Near Drifting Dust").transform;
        Quad(near, "Near Fine Dust", new Vector3(0, -1f, -1.15f),
            new Vector2(RoomWidth + .5f, 11.5f), nearDust);

        Transform foreground = Child(root.transform, "05 Foreground Cave Mouth").transform;
        Quad(foreground, "Foreground Atmospheric Edge", new Vector3(0, 0, -1.82f),
            new Vector2(RoomWidth + .5f, RoomHeight + .2f), edgeHaze);
        Shape(foreground, "Foreground Ceiling Left", rock, new Vector3(-7.2f, 6.35f, -2),
            new Vector2(15.5f, 3.2f), foregroundRock, -4f);
        Shape(foreground, "Foreground Ceiling Right", rock, new Vector3(7.5f, 6.4f, -2),
            new Vector2(15.7f, 3.45f), foregroundRock, 5f);
        Shape(foreground, "Foreground Left Wall", rock, new Vector3(-11.7f, -.25f, -2),
            new Vector2(3.2f, 14.8f), foregroundRock, -3f);
        Shape(foreground, "Foreground Right Wall", rock, new Vector3(11.75f, .2f, -2),
            new Vector2(3f, 14.4f), foregroundRock, 4f);
        Quad(foreground, "Lower Foreground Shadow", new Vector3(0, -6.62f, -2),
            new Vector2(26f, 1.05f), foregroundRock);

        ValidateScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save test006 scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("test006 LIMBO-style earth cave scene built successfully.");
    }

    private static void CreateCamera(Transform parent)
    {
        GameObject go = Child(parent, "Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0, 0, -10);
        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = OrthographicSize;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.19f, .12f, .065f, 1f);
        camera.allowHDR = true;
        go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        go.AddComponent<AudioListener>();
    }

    private static void CreateLight(Transform parent)
    {
        GameObject go = Child(parent, "Main Light");
        Light2D light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = new Color(.78f, .58f, .36f);
        light.intensity = .72f;
    }

    private static void CreatePostProcessing(Transform parent)
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Test006CaveVolume";
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        }

        if (!profile.TryGet(out DepthOfField depthOfField)) depthOfField = profile.Add<DepthOfField>(true);
        depthOfField.active = true;
        depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
        depthOfField.gaussianStart.Override(10.8f);
        depthOfField.gaussianEnd.Override(13.5f);
        depthOfField.gaussianMaxRadius.Override(.65f);
        depthOfField.highQualitySampling.Override(false);

        if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.color.Override(new Color(.055f, .028f, .012f));
        vignette.intensity.Override(.24f);
        vignette.smoothness.Override(.66f);

        if (!profile.TryGet(out FilmGrain grain)) grain = profile.Add<FilmGrain>(true);
        grain.active = true;
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(.13f);
        grain.response.Override(.72f);
        EditorUtility.SetDirty(profile);

        GameObject go = Child(parent, "Global Cave Post Processing");
        Volume volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;
        volume.sharedProfile = profile;
    }

    private static void CreateSpecificationMarkers(Transform parent)
    {
        GameObject specification = Child(parent, "Environment Specification");
        specification.transform.localScale = new Vector3(RoomWidth, RoomHeight, 1);
        Child(specification.transform, "Display Bounds 25x14 Units");
        Child(specification.transform, "Camera Orthographic Size 7");
        Child(specification.transform, "Reference Player Height 1.8 Units");
        Child(specification.transform, "Visual Only - No Earth Gameplay Semantics");
    }

    private static void CreateFarCaveContours(Transform parent, Mesh rock, Material far, Material mid)
    {
        Shape(parent, "Far Left Wall Contour", rock, new Vector3(-9.2f, -.2f, 3.7f),
            new Vector2(8.2f, 13.5f), far, -2f);
        Shape(parent, "Far Right Wall Contour", rock, new Vector3(9.4f, .15f, 3.68f),
            new Vector2(7.7f, 13.2f), far, 3f);
        Shape(parent, "Far Lower Sediment", rock, new Vector3(-1f, -5.1f, 3.62f),
            new Vector2(18.5f, 4.4f), mid, -1.5f);
        Shape(parent, "Far Ceiling Sediment", rock, new Vector3(-.8f, 5.7f, 3.6f),
            new Vector2(19.5f, 3.4f), mid, 1.2f);
    }

    private static void CreateFarCaveOpening(Transform parent, Mesh rock, Material dark, Material rim)
    {
        Disc(parent, "Far Tunnel Void", new Vector3(5.6f, -.2f, 3.3f), new Vector2(7.2f, 9.5f), dark);
        Shape(parent, "Far Tunnel Rim Left", rock, new Vector3(2.3f, -.2f, 3.2f),
            new Vector2(2.1f, 10.5f), rim, -7f);
        Shape(parent, "Far Tunnel Rim Right", rock, new Vector3(8.9f, -.15f, 3.2f),
            new Vector2(2.2f, 10.3f), rim, 8f);
    }

    private static void CreateMidColumns(Transform parent, Mesh rock, Material material)
    {
        Shape(parent, "Mid Column Left", rock, new Vector3(-8.7f, -.2f, 1.8f),
            new Vector2(2.5f, 10.8f), material, -5f);
        Shape(parent, "Mid Column Right", rock, new Vector3(9.3f, -.6f, 1.7f),
            new Vector2(2.8f, 10.4f), material, 7f);
        Shape(parent, "Mid Rubble Right", rock, new Vector3(7.1f, -4.25f, 1.65f),
            new Vector2(8.5f, 3.2f), material, 4f);
    }

    private static void CreateMineSupports(Transform parent, Material timber)
    {
        CreateSupport(parent, "Left Mine Support", -5.9f, timber, -3f);
        Quad(parent, "Broken Cross Beam", new Vector3(-1.1f, 2.65f, 1.38f),
            new Vector2(6.3f, .23f), timber, -7f);
    }

    private static void CreateSupport(Transform parent, string name, float x, Material timber, float lean)
    {
        Transform support = Child(parent, name).transform;
        Quad(support, "Left Post", new Vector3(x - 1.15f, -.7f, 1.4f), new Vector2(.3f, 7.4f), timber, lean);
        Quad(support, "Right Post", new Vector3(x + 1.15f, -.7f, 1.4f), new Vector2(.3f, 7.4f), timber, -lean);
        Quad(support, "Top Beam", new Vector3(x, 2.8f, 1.35f), new Vector2(2.8f, .34f), timber, lean * .35f);
    }

    private static void CreateGameplayShelves(Transform parent, Mesh rock, Material gameplay, Material dark)
    {
        Shape(parent, "Left Ground Mass", rock, new Vector3(-7.6f, -5.35f, .2f),
            new Vector2(11.8f, 3.7f), gameplay, -2f);
        Shape(parent, "Right Ground Mass", rock, new Vector3(7.8f, -5.2f, .18f),
            new Vector2(11.5f, 3.9f), gameplay, 2f);
        Quad(parent, "Primary Walkway", new Vector3(-1.1f, -3.5f, -.02f),
            new Vector2(12.2f, .52f), gameplay, -.35f);
        Shape(parent, "Single Raised Shelf", rock, new Vector3(7.8f, -1.55f, .02f),
            new Vector2(6.2f, 1.8f), gameplay, 3f);
        Quad(parent, "Deep Crevice", new Vector3(4.15f, -5.2f, -.1f), new Vector2(2f, 4f), dark);
    }

    private static void CreateReferenceCharacter(Transform parent, Material material)
    {
        Quad(parent, "Reference Player Body", new Vector3(-2.3f, -2.82f, -.25f),
            new Vector2(.62f, 1.25f), material);
        Disc(parent, "Reference Player Head", new Vector3(-2.3f, -1.92f, -.25f),
            new Vector2(.55f, .55f), material);
    }

    private static void CreateStalactites(Transform parent, Mesh spike, Material material)
    {
        float[] positions = { -8.8f, -3.9f, 3.5f, 8.6f };
        for (int i = 0; i < positions.Length; i++)
        {
            float length = 1.25f + (i % 3) * .68f;
            Shape(parent, $"Earth Stalactite {i + 1:00}", spike,
                new Vector3(positions[i], 5.45f - length * .5f, -.35f),
                new Vector2(.72f + (i % 2) * .25f, length), material,
                i % 2 == 0 ? -3f : 4f);
        }
    }

    private static void CreateFallingDust(Transform parent, Material material)
    {
        GameObject go = Child(parent, "Falling Earth Dust");
        go.transform.position = new Vector3(0, 1.6f, -.65f);
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 8f;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(.025f, .09f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(.45f, .3f, .16f, .25f), new Color(.82f, .65f, .4f, .7f));
        main.maxParticles = 180;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 18f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(RoomWidth, 8f, 0f);
        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-.08f, .13f);
        velocity.y = new ParticleSystem.MinMaxCurve(-.3f, -.08f);
        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = .12f;
        noise.frequency = .28f;
        noise.scrollSpeed = .12f;
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
    }

    private static void CreateFloatingMotes(Transform parent, Material material)
    {
        GameObject go = Child(parent, "Backlit Floating Motes");
        go.transform.position = new Vector3(3.8f, .4f, -.58f);
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 9f;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 10f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(.018f, .065f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(.5f, .32f, .15f, .18f), new Color(.88f, .69f, .4f, .75f));
        main.maxParticles = 90;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 8f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(8f, 10f, 0f);
        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-.08f, .09f);
        velocity.y = new ParticleSystem.MinMaxCurve(.015f, .11f);
        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = .09f;
        noise.frequency = .23f;
        noise.scrollSpeed = .08f;
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
    }

    private static Material Solid(string name, Shader shader, Color color)
    {
        Require(shader != null, $"Missing shader for {name}.");
        Material material = GetOrCreateMaterial(name, shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Rock(string name, Shader shader, Color baseColor, Color darkColor,
        float textureScale, float variation, float wetness, float strata)
    {
        Material material = GetOrCreateMaterial(name, shader);
        material.SetColor("_BaseColor", baseColor);
        material.SetColor("_DarkColor", darkColor);
        material.SetFloat("_TextureScale", textureScale);
        material.SetFloat("_Variation", variation);
        material.SetFloat("_Wetness", wetness);
        material.SetFloat("_Strata", strata);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Dust(string name, Shader shader, Color color, float opacity, float scale,
        float detail, Vector2 speed, float density, float softness, float bandCenter,
        float bandWidth, float bandStrength)
    {
        Material material = GetOrCreateMaterial(name, shader);
        material.SetColor("_Color", color);
        material.SetFloat("_Opacity", opacity);
        material.SetFloat("_NoiseScale", scale);
        material.SetFloat("_DetailScale", detail);
        material.SetVector("_Speed", speed);
        material.SetFloat("_Density", density);
        material.SetFloat("_Softness", softness);
        material.SetFloat("_BandCenter", bandCenter);
        material.SetFloat("_BandWidth", bandWidth);
        material.SetFloat("_BandStrength", bandStrength);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Glow(string name, Shader shader, Color color, float intensity, float falloff)
    {
        Material material = GetOrCreateMaterial(name, shader);
        material.SetColor("_Color", color);
        material.SetFloat("_Intensity", intensity);
        material.SetFloat("_Falloff", falloff);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GetOrCreateMaterial(string name, Shader shader)
    {
        string path = $"{MaterialDirectory}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else material.shader = shader;
        return material;
    }

    private static Mesh MeshAsset(string name, Vector2[] points)
    {
        string path = $"{MeshDirectory}/{name}.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) return mesh;
        mesh = new Mesh { name = name };
        Vector3[] vertices = new Vector3[points.Length];
        Vector2[] uv = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            vertices[i] = points[i];
            uv[i] = points[i] + Vector2.one * .5f;
        }
        int[] triangles = new int[(points.Length - 2) * 3];
        for (int i = 0; i < points.Length - 2; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 2;
            triangles[i * 3 + 2] = i + 1;
        }
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static Mesh FeatheredMeshAsset(string name, Vector2[] points, float innerScale)
    {
        string path = $"{MeshDirectory}/{name}.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) return mesh;

        int count = points.Length;
        Vector2 center = Vector2.zero;
        for (int i = 0; i < count; i++) center += points[i];
        center /= count;

        Vector3[] vertices = new Vector3[count * 2 + 1];
        Vector2[] uv = new Vector2[vertices.Length];
        Color32[] colors = new Color32[vertices.Length];
        for (int i = 0; i < count; i++)
        {
            Vector2 outer = points[i];
            Vector2 inner = Vector2.Lerp(center, outer, innerScale);
            vertices[i] = outer;
            vertices[count + i] = inner;
            uv[i] = outer + Vector2.one * .5f;
            uv[count + i] = inner + Vector2.one * .5f;
            colors[i] = new Color32(255, 255, 255, 0);
            colors[count + i] = new Color32(255, 255, 255, 255);
        }
        int centerIndex = count * 2;
        vertices[centerIndex] = center;
        uv[centerIndex] = center + Vector2.one * .5f;
        colors[centerIndex] = new Color32(255, 255, 255, 255);

        int[] triangles = new int[count * 9];
        int cursor = 0;
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;
            triangles[cursor++] = i;
            triangles[cursor++] = count + next;
            triangles[cursor++] = next;
            triangles[cursor++] = i;
            triangles[cursor++] = count + i;
            triangles[cursor++] = count + next;
            triangles[cursor++] = centerIndex;
            triangles[cursor++] = count + next;
            triangles[cursor++] = count + i;
        }

        mesh = new Mesh { name = name };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.colors32 = colors;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static GameObject Quad(Transform parent, string name, Vector3 position, Vector2 scale,
        Material material, float rotation = 0f)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1);
        go.transform.rotation = Quaternion.Euler(0, 0, rotation);
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    private static GameObject Disc(Transform parent, string name, Vector3 position, Vector2 scale,
        Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale.x, scale.y, .08f);
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    private static GameObject Shape(Transform parent, string name, Mesh mesh, Vector3 position,
        Vector2 scale, Material material, float rotation = 0f)
    {
        GameObject go = Child(parent, name);
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1);
        go.transform.rotation = Quaternion.Euler(0, 0, rotation);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    private static GameObject Child(Transform parent, string name)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }

    private static void ValidateScene(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.Length == 1, "test006 requires one root object.");
        Camera[] cameras = roots[0].GetComponentsInChildren<Camera>(true);
        Require(cameras.Length == 1, "test006 requires exactly one camera.");
        Require(cameras[0].orthographic && Mathf.Approximately(cameras[0].orthographicSize, OrthographicSize),
            "test006 camera must be orthographic size 7.");
        float ratio = ReferencePlayerHeight / (OrthographicSize * 2f);
        Require(ratio >= .12f && ratio <= .14f, "Reference Player must occupy 12-14 percent of screen height.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
