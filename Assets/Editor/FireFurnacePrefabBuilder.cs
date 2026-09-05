using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class FireFurnacePrefabBuilder
{
    public const string StaticPrefabPath =
        "Assets/Prefabs/Visual/Regions/Fire/ForgeFurnace2D.prefab";
    public const string ElevatorPrefabPath =
        "Assets/Prefabs/Gameplay/Platforms/ElevatingForgeFurnace2D.prefab";

    private const string StaticSpritePath =
        "Assets/Art/Generated/Fire/Furnaces/forge_ding_lowpoly_labnana_final.png";
    private const string ElevatorSpritePath =
        "Assets/Art/Generated/Fire/Furnaces/elevating_forge_ding_lowpoly_labnana_final.png";

    [MenuItem("Tools/W1/Build Fire Furnace Prefabs")]
    public static void BuildAll()
    {
        EnsureDirectory(Path.GetDirectoryName(StaticPrefabPath));
        EnsureDirectory(Path.GetDirectoryName(ElevatorPrefabPath));

        Sprite staticSprite = ConfigureSprite(StaticSpritePath);
        Sprite elevatorSprite = ConfigureSprite(ElevatorSpritePath);
        BuildStaticFurnace(staticSprite);
        BuildElevatingFurnace(elevatorSprite);

        AssetDatabase.SaveAssets();
        Debug.Log("Built fire-region forge furnace and elevating forge furnace Prefabs.");
    }

    private static void BuildStaticFurnace(Sprite sprite)
    {
        GameObject root = new("ForgeFurnace2D");
        try
        {
            GameObject visual = new("Visual Only - No Collider");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(.8f, .8f, 1f);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = -5;
            Save(root, StaticPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildElevatingFurnace(Sprite sprite)
    {
        GameObject root = new("ElevatingForgeFurnace2D");
        try
        {
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(4.4f, 2.35f);
            collider.offset = Vector2.zero;

            SurfaceSemantic2D semantic = root.AddComponent<SurfaceSemantic2D>();
            semantic.Configure(SurfaceSemantic2D.SurfaceType.DynamicSurface, false, true);

            MovingPlatform2D platform = root.AddComponent<MovingPlatform2D>();
            platform.ConfigurePath(Vector2.zero, new Vector2(0f, 4f), 1.5f, .5f);

            GameObject visual = new("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(.03f, .1f, 0f);
            visual.transform.localScale = new Vector3(.75f, .75f, 1f);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 5;

            Save(root, ElevatorPrefabPath);
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
        Require(importer != null, $"Missing furnace source image: {path}");
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
        Require(sprite != null, $"Furnace image did not import as a Sprite: {path}");
        return sprite;
    }

    private static void Save(GameObject root, string path)
    {
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Require(saved != null, $"Failed to save Prefab at {path}.");
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
