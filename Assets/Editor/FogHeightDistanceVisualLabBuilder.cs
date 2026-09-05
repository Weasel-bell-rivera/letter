using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using W1.Presentation;

public static class FogHeightDistanceVisualLabBuilder
{
    private const string ScenePath = "Assets/Scenes/Tests/FogHeightDistanceVisualLab.unity";
    private const string TexturePath = "Assets/Art/Generated/VisualTests/FogHeightDistance/TreeSilhouette.png";
    private const string ShaderName = "W1/Environment/Fog Height Distance Silhouette 2D";
    private const string MaterialFolder = "Assets/Materials/Tests/FogHeightDistanceVisualLab";
    private const string MaterialPath = MaterialFolder + "/FoggedTreeSilhouette.mat";

    [MenuItem("Tools/W1/Visual Labs/Build Fog Height Distance Lab")]
    public static void Build()
    {
        ConfigureTreeTexture();
        Material material = CreateOrUpdateMaterial();
        Sprite treeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);

        if (treeSprite == null || material == null)
        {
            Debug.LogError("Fog visual lab could not load its tree sprite or material.");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new("Fog Height Distance Visual Lab [Prototype]");

        Camera camera = CreateCamera(root.transform);
        CreateUnusedDirectionalLight(root.transform);

        GameObject samplesRoot = new("Visual Samples (No Gameplay Semantics)");
        samplesRoot.transform.SetParent(root.transform, false);

        SpriteRenderer nearTree = CreateTree(samplesRoot.transform, "Near Tree - Height Gradient",
            treeSprite, material, new Vector3(-5.8f, -4.9f, 0f), 0.62f, -3);
        SpriteRenderer middleTree = CreateTree(samplesRoot.transform, "Middle Tree",
            treeSprite, material, new Vector3(0.2f, -4.6f, 4f), 0.52f, -5);
        SpriteRenderer farTree = CreateTree(samplesRoot.transform, "Far Tree - Distance Fade",
            treeSprite, material, new Vector3(5.3f, -4.15f, 8f), 0.43f, -7);

        AddDepthMarker(samplesRoot.transform, "NEAR", new Vector3(-5.8f, -5.45f, -0.1f));
        AddDepthMarker(samplesRoot.transform, "MID", new Vector3(0.2f, -5.45f, -0.1f));
        AddDepthMarker(samplesRoot.transform, "FAR", new Vector3(5.3f, -5.45f, -0.1f));

        FogHeightDistanceVisualLab controller = root.AddComponent<FogHeightDistanceVisualLab>();
        SerializedObject serializedController = new(controller);
        SerializedProperty renderers = serializedController.FindProperty("treeRenderers");
        renderers.arraySize = 3;
        renderers.GetArrayElementAtIndex(0).objectReferenceValue = nearTree;
        renderers.GetArrayElementAtIndex(1).objectReferenceValue = middleTree;
        renderers.GetArrayElementAtIndex(2).objectReferenceValue = farTree;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log($"Built visual-only fog test scene at {ScenePath} with camera {camera.name}.");
    }

    private static void ConfigureTreeTexture()
    {
        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 200f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static Material CreateOrUpdateMaterial()
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"Shader not found: {ShaderName}");
            return null;
        }

        EnsureFolder(MaterialFolder);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.SetColor("_SilhouetteColor", new Color(0.018f, 0.025f, 0.029f, 1f));
        material.SetColor("_FogColor", new Color(0.70f, 0.75f, 0.76f, 1f));
        material.SetFloat("_HeightFogStart", -4f);
        material.SetFloat("_HeightFogEnd", 5f);
        material.SetFloat("_DistanceFogStart", 8f);
        material.SetFloat("_DistanceFogEnd", 20f);
        material.SetFloat("_HeightFogStrength", 0.65f);
        material.SetFloat("_DistanceFogStrength", 0.8f);
        material.SetFloat("_ColorFogStrength", 0.9f);
        material.SetFloat("_AlphaFadeStrength", 0.28f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static Camera CreateCamera(Transform parent)
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.70f, 0.75f, 0.76f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        return camera;
    }

    private static void CreateUnusedDirectionalLight(Transform parent)
    {
        GameObject lightObject = new("Directional Light (Unused by Unlit Shader)");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = new Vector3(1000f, 1000f, 50f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0f;
        lightObject.SetActive(false);
    }

    private static SpriteRenderer CreateTree(Transform parent, string name, Sprite sprite,
        Material material, Vector3 position, float scale, int sortingOrder)
    {
        GameObject tree = new(name);
        tree.transform.SetParent(parent, false);
        tree.transform.position = position;
        tree.transform.localScale = Vector3.one * scale;
        SpriteRenderer renderer = tree.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static void AddDepthMarker(Transform parent, string label, Vector3 position)
    {
        GameObject marker = new($"Depth Label - {label}");
        marker.transform.SetParent(parent, false);
        marker.transform.position = position;
        TextMesh text = marker.AddComponent<TextMesh>();
        text.text = label;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 42;
        text.characterSize = 0.08f;
        text.color = new Color(0.20f, 0.25f, 0.27f, 0.9f);
        MeshRenderer renderer = marker.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 5;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
