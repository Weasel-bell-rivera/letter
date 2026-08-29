using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Builds a visual-only underground lava cave using layered smoke and silhouettes.</summary>
public static class Test004LavaCaveSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Tests/test004.unity";
    private const string FogShaderName = "W1/Environment/LayeredFog2D";
    private const string MaterialDirectory = "Assets/Materials/Tests/Test004";
    private const string MeshDirectory = "Assets/Meshes/Tests/Test004";
    private const string LavaDripPrefabPath =
        "Assets/Prefabs/Visual/Regions/Fire/BackgroundLavaDrip2D.prefab";
    private const string EmberMaterialPath =
        "Assets/Materials/Regions/Fire/Background/M_BackgroundLavaDrop.mat";

    [MenuItem("Tools/W1/Build test004 Lava Cave")]
    public static void BuildFromMenu()
    {
        Shader fogShader = Shader.Find(FogShaderName);
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        Require(fogShader != null, $"Missing shader: {FogShaderName}");
        Require(unlitShader != null, "Missing URP Unlit shader.");
        GameObject lavaDripPrefab = RequireAsset<GameObject>(LavaDripPrefabPath);
        Material emberMaterial = RequireAsset<Material>(EmberMaterialPath);

        EnsureFolder("Assets/Materials/Tests", "Test004");
        Material voidRock = Solid("VoidRock", unlitShader, new Color(.008f, .004f, .004f));
        Material nearRock = Solid("NearRock", unlitShader, new Color(.025f, .009f, .008f));
        Material midRock = Solid("MidRock", unlitShader, new Color(.105f, .025f, .018f));
        Material farRock = Solid("FarRock", unlitShader, new Color(.24f, .055f, .025f));
        Material lavaCore = Solid("LavaCore", unlitShader, new Color(1f, .56f, .08f));
        Material lavaHot = Solid("LavaHot", unlitShader, new Color(1f, .19f, .015f));
        Material lavaCrust = Solid("LavaCrust", unlitShader, new Color(.19f, .018f, .008f));
        Material farSmoke = Fog("FarSmoke", fogShader, new Color(.33f, .09f, .045f), .38f, 2.1f,
            6f, new Vector2(.009f, .003f), .58f, .29f, .12f);
        Material midSmoke = Fog("MidSmoke", fogShader, new Color(.22f, .075f, .055f), .25f, 3.5f,
            9f, new Vector2(-.018f, .006f), .54f, .24f, .38f);
        Material nearSmoke = Fog("NearSmoke", fogShader, new Color(.12f, .055f, .045f), .22f, 5.4f,
            14f, new Vector2(.032f, -.004f), .49f, .2f, .58f);
        EnsureFolder("Assets", "Meshes");
        EnsureFolder("Assets/Meshes", "Tests");
        EnsureFolder("Assets/Meshes/Tests", "Test004");
        Mesh pillarMesh = MeshAsset("IrregularPillar", new[]
        {
            new Vector2(-.5f, -.5f), new Vector2(.34f, -.5f), new Vector2(.49f, -.2f),
            new Vector2(.3f, .05f), new Vector2(.46f, .34f), new Vector2(.12f, .5f),
            new Vector2(-.3f, .43f), new Vector2(-.47f, .08f)
        });
        Mesh boulderMesh = MeshAsset("BrokenBoulder", new[]
        {
            new Vector2(-.5f, -.18f), new Vector2(-.32f, -.46f), new Vector2(.16f, -.5f),
            new Vector2(.48f, -.2f), new Vector2(.41f, .25f), new Vector2(.08f, .5f),
            new Vector2(-.38f, .33f)
        });
        Mesh spikeMesh = MeshAsset("TaperedStalactite", new[]
        {
            new Vector2(-.5f, .5f), new Vector2(.5f, .5f), new Vector2(.29f, .12f),
            new Vector2(.1f, -.18f), new Vector2(0f, -.5f), new Vector2(-.17f, -.1f),
            new Vector2(-.34f, .22f)
        });

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new("test004 - Underground Lava Cave Visual Lab");
        CreateCamera(root.transform);
        CreateLight(root.transform);

        Transform far = Child(root.transform, "01 Far Lava Chamber").transform;
        Quad(far, "Deep Red Chamber", new Vector3(0, 0, 4), new Vector2(26, 15), farRock);
        CreateFarCaveRibs(far, midRock, farRock, pillarMesh, boulderMesh);
        Quad(far, "Far Magma Glow", new Vector3(0, -4.7f, 3.2f), new Vector2(26, 5.2f), lavaHot);
        Quad(far, "Far Smoke", new Vector3(0, -.5f, 2.7f), new Vector2(26, 14), farSmoke);

        Transform middle = Child(root.transform, "02 Mid Cave Silhouettes").transform;
        CreateRockPillars(middle, midRock, pillarMesh, boulderMesh);
        PlaceDrip(lavaDripPrefab, middle, "Background Lava Drip Left", new Vector3(-7.6f, 5.6f, 1.4f), .8f);
        PlaceDrip(lavaDripPrefab, middle, "Background Lava Drip Right", new Vector3(7.7f, 6.2f, 1.3f), .65f);
        Quad(middle, "Mid Smoke", new Vector3(0, -.5f, .8f), new Vector2(26, 14), midSmoke);

        Transform gameplay = Child(root.transform, "03 Silhouette Walkway").transform;
        CreateLavaPool(gameplay, lavaCore, lavaHot, lavaCrust);
        Quad(gameplay, "Left Basalt Shelf", new Vector3(-7.3f, -3.5f, 0), new Vector2(8.6f, 1.2f), nearRock);
        Quad(gameplay, "Right Basalt Shelf", new Vector3(7.1f, -2.85f, 0), new Vector2(8.8f, 1.2f), nearRock);
        Quad(gameplay, "Bridge", new Vector3(0, -2.15f, -.05f), new Vector2(5.6f, .42f), nearRock);
        Quad(gameplay, "Character", new Vector3(-2.15f, -1.15f, -.12f), new Vector2(.72f, 1.85f), voidRock);
        Disc(gameplay, "Character Head", new Vector3(-2.15f, .02f, -.12f), new Vector2(.74f, .74f), voidRock);
        CreateStalactites(gameplay, nearRock, spikeMesh);
        CreateEmbers(gameplay, emberMaterial);

        Transform near = Child(root.transform, "04 Near Heat Smoke").transform;
        Quad(near, "Near Heat Smoke", new Vector3(0, -1.1f, -1.25f), new Vector2(26, 12), nearSmoke);

        Transform frame = Child(root.transform, "05 Foreground Cave Mouth").transform;
        Shape(frame, "Ceiling Mass Left", boulderMesh, new Vector3(-6.4f, 6.05f, -2),
            new Vector2(15f, 3.1f), voidRock, -4f);
        Shape(frame, "Ceiling Mass Right", boulderMesh, new Vector3(7.1f, 6.2f, -2),
            new Vector2(15.5f, 3.4f), voidRock, 5f);
        Shape(frame, "Left Wall Mass", pillarMesh, new Vector3(-11.25f, 0, -2),
            new Vector2(3.3f, 15.5f), voidRock, -3f);
        Shape(frame, "Right Wall Mass", pillarMesh, new Vector3(11.35f, .8f, -2),
            new Vector2(3f, 14f), voidRock, 4f);
        Quad(frame, "Lower Foreground", new Vector3(0, -6.55f, -2), new Vector2(26, 1.15f), voidRock);

        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save test004 scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("test004 underground lava cave scene built successfully.");
    }

    private static void CreateCamera(Transform parent)
    {
        GameObject go = Child(parent, "Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0, 0, -10);
        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.055f, .008f, .006f, 1f);
        camera.allowHDR = true;
        go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
        go.AddComponent<AudioListener>();
    }

    private static void CreateLight(Transform parent)
    {
        GameObject go = Child(parent, "Main Light");
        Light2D light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = new Color(1f, .52f, .32f);
        light.intensity = .65f;
    }

    private static void CreateFarCaveRibs(Transform parent, Material mid, Material far,
        Mesh pillarMesh, Mesh boulderMesh)
    {
        for (int i = 0; i < 7; i++)
        {
            float x = -10.2f + i * 3.4f;
            float height = 7.2f + (i % 3) * 1.15f;
            Material material = i % 2 == 0 ? mid : far;
            Shape(parent, $"Far Broken Rib {i + 1:00}", pillarMesh,
                new Vector3(x, -.15f + (i % 2) * .4f, 3.5f),
                new Vector2(1.05f + (i % 3) * .22f, height), material,
                i % 2 == 0 ? -7f : 6f);
            Shape(parent, $"Far Arch Fragment {i + 1:00}", boulderMesh,
                new Vector3(x + (i % 2 == 0 ? 1.15f : -1.15f), 3.85f, 3.48f),
                new Vector2(3.6f, 1.4f + (i % 3) * .25f), material,
                i % 2 == 0 ? -18f : 17f);
        }
    }

    private static void CreateRockPillars(Transform parent, Material material,
        Mesh pillarMesh, Mesh boulderMesh)
    {
        Shape(parent, "Mid Pillar Left", pillarMesh, new Vector3(-8.4f, -.2f, 1.6f),
            new Vector2(2.35f, 10.2f), material, -6f);
        Shape(parent, "Mid Pillar Center", pillarMesh, new Vector3(2.1f, .6f, 1.55f),
            new Vector2(1.8f, 9f), material, 5f);
        Shape(parent, "Mid Pillar Right", pillarMesh, new Vector3(9.2f, -.4f, 1.5f),
            new Vector2(2.65f, 10.6f), material, 8f);
        Shape(parent, "Mid Broken Shelf Left", boulderMesh, new Vector3(-5.2f, -3f, 1.5f),
            new Vector2(5.8f, 3.7f), material, -5f);
        Shape(parent, "Mid Broken Shelf Right", boulderMesh, new Vector3(6f, -3.4f, 1.5f),
            new Vector2(6.2f, 4f), material, 7f);
    }

    private static void CreateLavaPool(Transform parent, Material core, Material hot, Material crust)
    {
        Quad(parent, "Lava Pool Core", new Vector3(0, -5.25f, -.1f), new Vector2(25f, 3.3f), hot);
        Quad(parent, "Lava Pool Bright Surface", new Vector3(0, -3.73f, -.18f), new Vector2(24f, .24f), core);
        for (int i = 0; i < 7; i++)
        {
            float x = -9.2f + i * 3.1f;
            float width = 1.2f + (i % 3) * .45f;
            Quad(parent, $"Floating Crust {i + 1:00}", new Vector3(x, -4.05f - (i % 2) * .18f, -.24f),
                new Vector2(width, .22f), crust, i % 2 == 0 ? 4f : -5f);
        }
    }

    private static void CreateStalactites(Transform parent, Material material, Mesh spikeMesh)
    {
        float[] x = { -9.3f, -6.2f, -3.9f, 1.2f, 4.4f, 8.7f };
        for (int i = 0; i < x.Length; i++)
        {
            float length = 1.4f + (i % 3) * .75f;
            Shape(parent, $"Tapered Stalactite {i + 1:00}", spikeMesh,
                new Vector3(x[i], 5.1f - length * .5f, -.35f),
                new Vector2(.75f + (i % 2) * .28f, length), material,
                i % 2 == 0 ? -4f : 5f);
        }
    }

    private static void CreateEmbers(Transform parent, Material material)
    {
        GameObject go = Child(parent, "Rising Embers");
        go.transform.position = new Vector3(0, -3.5f, -.55f);
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 8f;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 5.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.45f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(.035f, .11f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .25f, .025f), new Color(1f, .72f, .16f));
        main.maxParticles = 100;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 10f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(20f, .35f, 0f);
        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-.22f, .22f);
        velocity.y = new ParticleSystem.MinMaxCurve(.25f, .75f);
        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = .24f;
        noise.frequency = .35f;
        noise.scrollSpeed = .18f;
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
    }

    private static void PlaceDrip(GameObject prefab, Transform parent, string name, Vector3 position, float scale)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        instance.transform.localScale = Vector3.one * scale;
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
    }

    private static Material Solid(string name, Shader shader, Color color)
    {
        Material material = GetOrCreateMaterial(name, shader);
        material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Fog(string name, Shader shader, Color color, float opacity, float scale,
        float detail, Vector2 speed, float density, float softness, float verticalFade)
    {
        Material material = GetOrCreateMaterial(name, shader);
        material.SetColor("_Color", color);
        material.SetFloat("_Opacity", opacity);
        material.SetFloat("_NoiseScale", scale);
        material.SetFloat("_DetailScale", detail);
        material.SetVector("_Speed", speed);
        material.SetFloat("_Density", density);
        material.SetFloat("_Softness", softness);
        material.SetFloat("_VerticalFade", verticalFade);
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
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        go.transform.rotation = Quaternion.Euler(0, 0, rotation);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    private static Mesh MeshAsset(string name, Vector2[] outline)
    {
        string path = $"{MeshDirectory}/{name}.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh { name = name };
            AssetDatabase.CreateAsset(mesh, path);
        }
        mesh.Clear();
        Vector3[] vertices = new Vector3[outline.Length];
        Vector2[] uv = new Vector2[outline.Length];
        for (int i = 0; i < outline.Length; i++)
        {
            vertices[i] = new Vector3(outline[i].x, outline[i].y, 0f);
            uv[i] = outline[i] + Vector2.one * .5f;
        }
        int[] triangles = new int[(outline.Length - 2) * 3];
        for (int i = 0; i < outline.Length - 2; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
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

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Require(asset != null, $"Missing asset: {path}");
        return asset;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
