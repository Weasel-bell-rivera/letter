using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Builds a small visual lab demonstrating LIMBO-style layered 2D fog.</summary>
public static class Test003LayeredFogSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Tests/test003.unity";
    private const string FogShaderName = "W1/Environment/LayeredFog2D";
    private const string MaterialDirectory = "Assets/Materials/Tests/Test003";

    [MenuItem("Tools/W1/Build test003 Layered Fog")]
    public static void BuildFromMenu()
    {
        Shader fogShader = Shader.Find(FogShaderName);
        Require(fogShader != null, $"Missing shader: {FogShaderName}");

        EnsureFolder("Assets/Materials", "Tests");
        EnsureFolder("Assets/Materials/Tests", "Test003");
        Material silhouette = GetOrCreateMaterial("Silhouette", Shader.Find("Universal Render Pipeline/Unlit"));
        silhouette.SetColor("_BaseColor", new Color(.012f, .014f, .016f, 1f));
        Material farShape = GetOrCreateMaterial("FarShapes", Shader.Find("Universal Render Pipeline/Unlit"));
        farShape.SetColor("_BaseColor", new Color(.22f, .25f, .26f, 1f));
        Material midShape = GetOrCreateMaterial("MidShapes", Shader.Find("Universal Render Pipeline/Unlit"));
        midShape.SetColor("_BaseColor", new Color(.09f, .105f, .11f, 1f));
        Material farFog = ConfigureFog("FarFog", fogShader, new Color(.68f, .71f, .72f), .34f, 2.2f,
            6f, new Vector2(.012f, .003f), .59f, .27f, .12f);
        Material midFog = ConfigureFog("MidFog", fogShader, new Color(.62f, .65f, .66f), .22f, 3.3f,
            9f, new Vector2(-.022f, .005f), .52f, .24f, .3f);
        Material nearFog = ConfigureFog("NearFog", fogShader, new Color(.74f, .76f, .76f), .16f, 4.7f,
            13f, new Vector2(.037f, -.004f), .46f, .2f, .5f);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new("test003 - Layered Fog Visual Lab");
        CreateCamera(root.transform);
        CreateLight(root.transform);

        Transform far = Child(root.transform, "01 Background Far").transform;
        Quad(far, "Sky Gradient Base", new Vector3(0, 0, 3), new Vector2(26, 15), farShape);
        CreateDistantForest(far, farShape);
        Quad(far, "Far Fog - Slow Large Noise", new Vector3(0, 0, 2.5f), new Vector2(26, 15), farFog);

        Transform middle = Child(root.transform, "02 Background Mid").transform;
        CreateMidForest(middle, midShape);
        Quad(middle, "Mid Fog - Opposing Drift", new Vector3(0, -.2f, 1), new Vector2(26, 14), midFog);

        Transform gameplay = Child(root.transform, "03 Gameplay Silhouette").transform;
        Quad(gameplay, "Ground", new Vector3(0, -5.5f, 0), new Vector2(26, 3), silhouette);
        Quad(gameplay, "Platform Left", new Vector3(-5.2f, -2.6f, 0), new Vector2(4.4f, .45f), silhouette);
        Quad(gameplay, "Platform Right", new Vector3(4.7f, -1.4f, 0), new Vector2(5.2f, .45f), silhouette);
        Quad(gameplay, "Character", new Vector3(-1.3f, -3.45f, -.05f), new Vector2(.75f, 2.1f), silhouette);
        Disc(gameplay, "Character Head", new Vector3(-1.3f, -2.15f, -.05f), new Vector2(.78f, .78f), silhouette);
        CreateForegroundBranches(gameplay, silhouette);

        Transform near = Child(root.transform, "04 Fog Near").transform;
        Quad(near, "Near Fog - Fast Fine Noise", new Vector3(0, -.8f, -1.5f), new Vector2(26, 13), nearFog);

        Transform foreground = Child(root.transform, "05 Foreground Frame").transform;
        Quad(foreground, "Lower Foreground", new Vector3(0, -6.55f, -2), new Vector2(26, 1.2f), silhouette);
        Quad(foreground, "Left Foreground", new Vector3(-11.75f, 0, -2), new Vector2(1.5f, 14), silhouette);

        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save test003 scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("test003 layered fog scene built successfully.");
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
        camera.backgroundColor = new Color(.56f, .59f, .6f, 1f);
        camera.allowHDR = true;
        go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
        go.AddComponent<AudioListener>();
    }

    private static void CreateLight(Transform parent)
    {
        GameObject go = Child(parent, "Main Light");
        Light2D light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
    }

    private static void CreateDistantForest(Transform parent, Material material)
    {
        float[] heights = { 7.5f, 5.8f, 8.2f, 6.4f, 7.1f, 5.5f, 8.8f, 6.2f, 7.8f };
        for (int i = 0; i < heights.Length; i++)
        {
            float x = -10.5f + i * 2.65f;
            Quad(parent, $"Far Trunk {i + 1:00}", new Vector3(x, -1.2f, 2), new Vector2(.5f, heights[i]), material);
            Disc(parent, $"Far Crown {i + 1:00}", new Vector3(x, 2.2f, 2), new Vector2(3.1f, 3.8f), material);
        }
    }

    private static void CreateMidForest(Transform parent, Material material)
    {
        for (int i = 0; i < 6; i++)
        {
            float x = -10f + i * 4.1f;
            float height = 7.2f + (i % 3) * 1.15f;
            Quad(parent, $"Mid Trunk {i + 1:00}", new Vector3(x, -.7f, .5f), new Vector2(.72f, height), material);
            Disc(parent, $"Mid Crown {i + 1:00}", new Vector3(x, 2.9f, .5f), new Vector2(4.1f, 4.8f), material);
        }
    }

    private static void CreateForegroundBranches(Transform parent, Material material)
    {
        Quad(parent, "Branch Left", new Vector3(-8.8f, 2.8f, -.4f), new Vector2(7.5f, .3f), material, -18f);
        Quad(parent, "Branch Right", new Vector3(8.4f, 3.7f, -.4f), new Vector2(7f, .36f), material, 22f);
        Quad(parent, "Hanging Branch", new Vector3(7.2f, 1.4f, -.4f), new Vector2(4.8f, .24f), material, 75f);
    }

    private static Material ConfigureFog(string name, Shader shader, Color color, float opacity,
        float noiseScale, float detailScale, Vector2 speed, float density, float softness, float verticalFade)
    {
        Material material = GetOrCreateMaterial(name, shader);
        material.SetColor("_Color", color);
        material.SetFloat("_Opacity", opacity);
        material.SetFloat("_NoiseScale", noiseScale);
        material.SetFloat("_DetailScale", detailScale);
        material.SetVector("_Speed", speed);
        material.SetFloat("_Density", density);
        material.SetFloat("_Softness", softness);
        material.SetFloat("_VerticalFade", verticalFade);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GetOrCreateMaterial(string name, Shader shader)
    {
        Require(shader != null, $"Missing shader for {name}.");
        string path = $"{MaterialDirectory}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else material.shader = shader;
        EditorUtility.SetDirty(material);
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

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
