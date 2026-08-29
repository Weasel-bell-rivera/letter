using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Builds a visual-only snow-mountain summit at the approved snow-region camera scale.</summary>
public static class Test005SnowSummitSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Tests/test005.unity";
    public const float RoomWidth = 25f;
    public const float RoomHeight = 14f;
    public const float OrthographicSize = 7f;
    public const float ReferencePlayerHeight = 1.8f;

    private const string FogShaderName = "W1/Environment/LayeredFog2D";
    private const string MaterialDirectory = "Assets/Materials/Tests/Test005";
    private const string MeshDirectory = "Assets/Meshes/Tests/Test005";
    private const string BackdropPath = "Assets/Art/Snow/Backgrounds/snow_summit_background_v1.png";
    private const string RidgeMeshPath = MeshDirectory + "/SummitRidge.asset";
    private const string SnowCapMeshPath = MeshDirectory + "/SummitSnowCap.asset";

    [MenuItem("Tools/W1/Build test005 Snow Summit")]
    public static void BuildFromMenu()
    {
        Shader fogShader = Shader.Find(FogShaderName);
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        Require(fogShader != null, $"Missing shader: {FogShaderName}");
        Require(unlitShader != null, "Missing URP Unlit shader.");

        EnsureFolder("Assets/Materials/Tests", "Test005");
        EnsureFolder("Assets", "Meshes");
        EnsureFolder("Assets/Meshes", "Tests");
        EnsureFolder("Assets/Meshes/Tests", "Test005");
        Sprite backdrop = ConfigureBackdropSprite();
        Mesh ridgeMesh = GetOrCreateRidgeMesh();
        Mesh snowCapMesh = GetOrCreateSnowCapMesh();

        Material nearRock = Solid("NearRock", unlitShader, new Color(.075f, .105f, .125f));
        Material snow = Solid("Snow", unlitShader, new Color(.9f, .955f, .98f));
        Material iceShadow = Solid("IceShadow", unlitShader, new Color(.43f, .62f, .72f));
        Material silhouette = Solid("Silhouette", unlitShader, new Color(.02f, .028f, .035f));
        Material snowParticle = Solid("SnowParticle", Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"),
            new Color(.96f, .985f, 1f));
        Material farCloud = Fog("FarCloud", fogShader, new Color(.86f, .92f, .95f), .42f, 2.1f,
            6f, new Vector2(.009f, .002f), .58f, .3f, .08f);
        Material midCloud = Fog("MidCloud", fogShader, new Color(.77f, .86f, .91f), .3f, 3.6f,
            9f, new Vector2(-.018f, .004f), .52f, .24f, .28f);
        Material nearMist = Fog("NearMist", fogShader, new Color(.83f, .91f, .95f), .18f, 5.5f,
            14f, new Vector2(.032f, -.003f), .46f, .2f, .5f);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new("test005 - Snow Mountain Summit Visual Lab");
        CreateCamera(root.transform);
        CreateLight(root.transform);
        CreateSpecificationMarkers(root.transform);

        Transform far = Child(root.transform, "01 Far Sky and Mountain Range").transform;
        CreateBackdropSprite(far, backdrop);
        Quad(far, "Far Cloud Layer", new Vector3(0, .1f, 2.8f), new Vector2(RoomWidth + .4f, 8f), farCloud);

        Transform middle = Child(root.transform, "02 Mid Mountain Ridges").transform;
        Quad(middle, "Mid Cloud Layer", new Vector3(0, -.65f, 1.5f), new Vector2(RoomWidth + .4f, 7.5f), midCloud);

        Transform summit = Child(root.transform, "03 Summit Gameplay Readability Plane").transform;
        CreateSummit(summit, ridgeMesh, snowCapMesh, nearRock, snow, iceShadow);
        CreateReferenceCharacter(summit, silhouette);
        CreateWindFlags(summit, silhouette);
        CreateDecorativeSnow(summit, snowParticle);

        Transform near = Child(root.transform, "04 Near Summit Mist").transform;
        Quad(near, "Near Mist Layer", new Vector3(0, -1.5f, -1.2f), new Vector2(RoomWidth + .4f, 10f), nearMist);

        Transform foreground = Child(root.transform, "05 Foreground Snow Cornice").transform;
        Quad(foreground, "Foreground Snow Haze", new Vector3(0, -6.85f, -2),
            new Vector2(RoomWidth + .4f, .3f), nearMist);

        ValidateScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save test005 scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("test005 snow summit scene built successfully at 25x14 units with orthographic size 7.");
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
        camera.backgroundColor = new Color(.56f, .72f, .84f, 1f);
        camera.allowHDR = true;
        go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
        go.AddComponent<AudioListener>();
    }

    private static void CreateLight(Transform parent)
    {
        GameObject go = Child(parent, "Main Light");
        Light2D light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = new Color(.83f, .91f, 1f);
        light.intensity = .9f;
    }

    private static void CreateSpecificationMarkers(Transform parent)
    {
        GameObject specification = Child(parent, "Environment Specification");
        specification.transform.localScale = new Vector3(RoomWidth, RoomHeight, 1);
        Child(specification.transform, "Display Bounds 25x14 Units");
        Child(specification.transform, "Camera Orthographic Size 7");
        Child(specification.transform, "Reference Player Height 1.8 Units = 12.86 Percent of Screen");
        Child(specification.transform, "Visual Only - No Snow Gameplay Semantics");
    }

    private static void CreateSummit(Transform parent, Mesh ridgeMesh, Mesh snowCapMesh,
        Material rock, Material snow, Material iceShadow)
    {
        MeshObject(parent, "Irregular Summit Ridge", ridgeMesh, new Vector3(0, 0, .25f), rock);
        MeshObject(parent, "Wind Packed Snow Cap", snowCapMesh, new Vector3(0, 0, -.02f), snow);
        Quad(parent, "Right Wind Scoured Ice", new Vector3(6.65f, -2.97f, -.08f),
            new Vector2(4.3f, .11f), iceShadow, -1.2f);
    }

    private static void CreateReferenceCharacter(Transform parent, Material material)
    {
        // Total visual height is exactly 1.8 units: -3.075 to -1.275.
        Quad(parent, "Reference Player Body", new Vector3(-1.8f, -2.45f, -.25f),
            new Vector2(.62f, 1.25f), material);
        Disc(parent, "Reference Player Head", new Vector3(-1.8f, -1.55f, -.25f),
            new Vector2(.55f, .55f), material);
    }

    private static void CreateWindFlags(Transform parent, Material material)
    {
        Quad(parent, "Summit Marker Pole", new Vector3(5.3f, -1.5f, -.18f),
            new Vector2(.09f, 3.1f), material, -2f);
        Quad(parent, "Wind Flag", new Vector3(4.65f, -.3f, -.2f),
            new Vector2(1.35f, .32f), material, -9f);
    }

    private static void CreateDecorativeSnow(Transform parent, Material material)
    {
        GameObject go = Child(parent, "Decorative Windblown Snow");
        go.transform.position = new Vector3(0, 2f, -.65f);
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 8f;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(.025f, .085f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(.8f, .9f, .96f, .55f), Color.white);
        main.maxParticles = 180;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 22f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(RoomWidth, RoomHeight, 0f);
        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-1.2f, -.65f);
        velocity.y = new ParticleSystem.MinMaxCurve(-.34f, -.08f);
        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = .18f;
        noise.frequency = .42f;
        noise.scrollSpeed = .2f;
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
    }

    private static Sprite ConfigureBackdropSprite()
    {
        AssetDatabase.ImportAsset(BackdropPath,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(BackdropPath) as TextureImporter;
        Require(importer != null, $"Missing generated snow summit background: {BackdropPath}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 64f;
        importer.spritePivot = new Vector2(.5f, .5f);
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackdropPath);
        Require(sprite != null, $"Background did not import as Sprite: {BackdropPath}");
        return sprite;
    }

    private static void CreateBackdropSprite(Transform parent, Sprite sprite)
    {
        GameObject go = Child(parent, "Production Snow Summit Background");
        go.transform.position = new Vector3(0, 0, 4);
        float targetWidth = RoomWidth + .4f;
        float targetHeight = RoomHeight + .4f;
        go.transform.localScale = new Vector3(targetWidth / sprite.bounds.size.x,
            targetHeight / sprite.bounds.size.y, 1f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
    }

    private static Mesh GetOrCreateRidgeMesh()
    {
        Vector2[] top =
        {
            new(-12.7f, -3.45f), new(-11.4f, -3.08f), new(-9.7f, -3.22f),
            new(-8.2f, -2.92f), new(-6.5f, -3.02f), new(-4.9f, -2.82f),
            new(-3.2f, -3.04f), new(-1.4f, -2.9f), new(.2f, -3.08f),
            new(2f, -2.87f), new(3.8f, -3.02f), new(5.7f, -2.9f),
            new(7.4f, -3.06f), new(9.2f, -2.88f), new(10.8f, -3.16f),
            new(12.7f, -3.38f)
        };
        return GetOrCreateStripMesh(RidgeMeshPath, "SummitRidge", top, -7.3f);
    }

    private static Mesh GetOrCreateSnowCapMesh()
    {
        Vector2[] top =
        {
            new(-9.6f, -3.19f), new(-8.2f, -2.89f), new(-6.5f, -2.99f),
            new(-4.9f, -2.79f), new(-3.2f, -3.01f), new(-1.4f, -2.87f),
            new(.2f, -3.05f), new(2f, -2.84f), new(3.8f, -2.99f),
            new(5.7f, -2.87f), new(7.4f, -3.03f), new(9.3f, -2.85f)
        };
        return GetOrCreateStripMesh(SnowCapMeshPath, "SummitSnowCap", top, -.2f);
    }

    private static Mesh GetOrCreateStripMesh(string path, string name, Vector2[] top, float bottomOffset)
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh { name = name };
            AssetDatabase.CreateAsset(mesh, path);
        }

        int count = top.Length;
        Vector3[] vertices = new Vector3[count * 2];
        Vector2[] uv = new Vector2[count * 2];
        int[] triangles = new int[(count - 1) * 6];
        for (int i = 0; i < count; i++)
        {
            vertices[i] = top[i];
            vertices[i + count] = new Vector3(top[i].x, top[i].y + bottomOffset);
            float u = i / (float)(count - 1);
            uv[i] = new Vector2(u, 1f);
            uv[i + count] = new Vector2(u, 0f);
            if (i == count - 1) continue;
            int offset = i * 6;
            // Clockwise from the camera at -Z so URP's back-face culling keeps the strip visible.
            triangles[offset] = i;
            triangles[offset + 1] = i + 1;
            triangles[offset + 2] = i + count;
            triangles[offset + 3] = i + 1;
            triangles[offset + 4] = i + count + 1;
            triangles[offset + 5] = i + count;
        }
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    private static GameObject MeshObject(Transform parent, string name, Mesh mesh, Vector3 position,
        Material material)
    {
        GameObject go = Child(parent, name);
        go.transform.position = position;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go;
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
        Camera[] cameras = scene.GetRootGameObjects()[0].GetComponentsInChildren<Camera>(true);
        Require(cameras.Length == 1, "test005 requires exactly one camera.");
        Require(cameras[0].orthographic && Mathf.Approximately(cameras[0].orthographicSize, OrthographicSize),
            "test005 camera must be orthographic size 7.");
        float ratio = ReferencePlayerHeight / (OrthographicSize * 2f);
        Require(ratio >= .12f && ratio <= .14f, "Reference Player must occupy 12-14 percent of screen height.");
        Require(Mathf.Approximately(RoomWidth, 25f) && Mathf.Approximately(RoomHeight, 14f),
            "test005 display bounds must remain 25x14 units.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
