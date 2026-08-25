using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WindVisualAssetBuilder
{
    public const string BackgroundTexture = "Assets/Art/Wind/Backgrounds/wind_region_sky_v1.png";
    public const string StreakTexture = "Assets/Art/Wind/Effects/wind_streaks_v1.png";
    public const string BackgroundPrefab = "Assets/Prefabs/Environment/Wind/WindRegionBackdrop.prefab";
    public const string WindColumnPrefab = "Assets/Prefabs/Gameplay/Wind/WindColumn2D.prefab";

    [MenuItem("Tools/W1/Build Wind Visual Assets")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Prefabs/Environment/Wind");
        ConfigureSprite(BackgroundTexture, 100f, false);
        ConfigureSprite(StreakTexture, 650f, true);
        AssetDatabase.ImportAsset(BackgroundTexture, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(StreakTexture, ImportAssetOptions.ForceUpdate);
        BuildWindColumn();
        BuildBackdrop();
        AssetDatabase.SaveAssets();
        Debug.Log("Wind background, animated streak visual, and Prefabs built successfully.");
    }

    [MenuItem("Tools/W1/Apply Wind Backdrop to Existing Rooms")]
    public static void ApplyBackdropToRooms()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BackgroundPrefab);
        Require(prefab != null, "Build wind visual assets before applying room backgrounds.");
        for (int number = 1; number <= 15; number++)
        {
            string path = $"Assets/Scenes/Levels/Wind/Wind_{number:000}.unity";
            if (!File.Exists(path)) continue;
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool exists = false;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.transform.Find("Wind Region Backdrop") != null) { exists = true; break; }
            if (exists) continue;
            GameObject roomRoot = Array.Find(scene.GetRootGameObjects(), root =>
                root.GetComponentInChildren<Grid>(true) != null);
            Require(roomRoot != null, $"Room root with Grid is missing in {path}.");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "Wind Region Backdrop";
            instance.transform.SetParent(roomRoot.transform, false);
            instance.transform.position = new Vector3(0f, 0f, 5f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene), $"Failed to save backdrop in {path}.");
        }
        Debug.Log("Wind backdrop applied to existing WIND_001 through WIND_015 scenes.");
    }

    private static void BuildWindColumn()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(WindColumnPrefab);
        try
        {
            WindColumn2D wind = root.GetComponent<WindColumn2D>();
            BoxCollider2D volume = root.GetComponent<BoxCollider2D>();
            Require(wind != null && volume != null, "WindColumn Prefab gameplay components are missing.");
            Transform old = root.transform.Find("Visual");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

            GameObject effect = new("Visual");
            effect.transform.SetParent(root.transform, false);
            WindColumnVisual2D animation = effect.AddComponent<WindColumnVisual2D>();
            Sprite white = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite streak = AssetDatabase.LoadAssetAtPath<Sprite>(StreakTexture);
            Require(white != null && streak != null, "Wind visual sprites are missing.");
            SpriteRenderer haze = Renderer("Haze", effect.transform, white, 2);
            SpriteRenderer far = Renderer("FarStreaks", effect.transform, streak, 3);
            SpriteRenderer near = Renderer("NearStreaks", effect.transform, streak, 4);
            haze.drawMode = SpriteDrawMode.Sliced;
            far.drawMode = SpriteDrawMode.Tiled;
            near.drawMode = SpriteDrawMode.Tiled;
            far.flipY = true;
            animation.Configure(haze, far, near, volume.size);
            wind.ConfigureReferences(volume, haze);
            PrefabUtility.SaveAsPrefabAsset(root, WindColumnPrefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void BuildBackdrop()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundTexture);
        Require(sprite != null, "Wind background sprite is missing.");
        GameObject root = new("WindRegionBackdrop");
        try
        {
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 1f, 1f, .78f);
            renderer.sortingOrder = -100;
            Vector2 native = sprite.bounds.size;
            root.transform.localScale = new Vector3(27f / native.x, 18f / native.y, 1f);
            PrefabUtility.SaveAsPrefabAsset(root, BackgroundPrefab);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static SpriteRenderer Renderer(string name, Transform parent, Sprite sprite, int order)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static void ConfigureSprite(string path, float pixelsPerUnit, bool alpha)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Require(importer != null, $"Texture importer unavailable: {path}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        TextureImporterSettings settings = new();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.alphaIsTransparency = alpha;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = alpha ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
