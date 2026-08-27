using UnityEditor;
using UnityEngine;

public static class SpringPrefabBuilder
{
    public const string ExtendedSpritePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Spring/spring_out.png";
    public const string CompressedSpritePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Spring/spring.png";
    public const string PrefabPath = "Assets/Prefabs/Gameplay/Devices/Spring2D.prefab";

    [MenuItem("Tools/Letter/Rebuild Spring Prefab")]
    public static void CreateSpringPrefab()
    {
        EnsureFolder("Assets/Prefabs/Gameplay", "Devices");
        ConfigureSprite(ExtendedSpritePath);
        ConfigureSprite(CompressedSpritePath);

        Sprite extendedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ExtendedSpritePath);
        Sprite compressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CompressedSpritePath);
        if (extendedSprite == null || compressedSprite == null)
            throw new MissingReferenceException("Spring sprites could not be imported.");

        GameObject root = new("Spring2D");
        try
        {
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            body.gravityScale = 0f;
            body.freezeRotation = true;

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.offset = new Vector2(0f, .5f);
            collider.isTrigger = false;

            SurfaceSemantic2D semantic = root.AddComponent<SurfaceSemantic2D>();
            semantic.Configure(SurfaceSemantic2D.SurfaceType.Spring, true, true);
            Spring2D spring = root.AddComponent<Spring2D>();
            spring.Configure(Spring2D.DefaultTopLaunchHeight, Spring2D.DefaultSideLaunchSpeed,
                Spring2D.DefaultMinimumApproachSpeed);

            GameObject visualObject = new("Visual");
            visualObject.transform.SetParent(root.transform, false);
            visualObject.transform.localPosition = new Vector3(0f, .5f, 0f);
            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = extendedSprite;
            SpringVisual2D visual = visualObject.AddComponent<SpringVisual2D>();
            visual.Configure(spring, renderer, extendedSprite, compressedSprite);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created spring prefab at {PrefabPath}.");
    }

    private static void ConfigureSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new MissingReferenceException($"Missing spring texture: {path}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        TextureImporterSettings settings = new();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }
}
