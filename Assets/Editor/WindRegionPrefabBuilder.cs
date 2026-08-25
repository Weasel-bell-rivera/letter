using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WindRegionPrefabBuilder
{
    public const string WindFolder = "Assets/Prefabs/Gameplay/Wind";
    public const string SwitchFolder = "Assets/Prefabs/Gameplay/Switches";
    public const string WindColumnPath = WindFolder + "/WindColumn2D.prefab";
    public const string MovingTornadoPath = WindFolder + "/MovingTornado2D.prefab";
    public const string TornadoGeneratorPath = WindFolder + "/TornadoGenerator2D.prefab";
    public const string WindDeflectorPath = WindFolder + "/WindDeflector2D.prefab";
    public const string WindTurbinePath = SwitchFolder + "/WindTurbineSwitch2D.prefab";
    public const string TornadoArtPath = "Assets/Art/Generated/Wind/small_tornado_3frame_handpainted.png";

    [MenuItem("Tools/W1/Build Wind Region Prefabs")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    [MenuItem("Tools/W1/Rebuild Moving Tornado Art")]
    public static void RebuildMovingTornadoArt()
    {
        Directory.CreateDirectory(WindFolder);
        MovingTornado2D tornadoPrefab = CreateMovingTornado();
        CreateTornadoGenerator(tornadoPrefab);
        AssetDatabase.SaveAssets();
        Debug.Log("Moving tornado three-frame art and dependent generator rebuilt successfully.");
    }

    public static void BuildFromCommandLine()
    {
        Directory.CreateDirectory(WindFolder);
        Directory.CreateDirectory(SwitchFolder);
        WindRayEnemyBuilder.BuildAssets();
        CreateWindColumn();
        MovingTornado2D tornadoPrefab = CreateMovingTornado();
        CreateTornadoGenerator(tornadoPrefab);
        CreateWindDeflector();
        CreateWindTurbine();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Wind region Prefabs built successfully.");
    }

    private static void CreateWindColumn()
    {
        GameObject root = new("WindColumn2D");
        try
        {
            BoxCollider2D volume = root.AddComponent<BoxCollider2D>();
            volume.isTrigger = true;
            volume.size = new Vector2(6f, 1f);
            WindColumn2D wind = root.AddComponent<WindColumn2D>();
            SpriteRenderer visual = Visual("Visual", root.transform, new Vector2(6f, 1f),
                new Color(.45f, .9f, 1f, .42f), 2);
            wind.ConfigureReferences(volume, visual);
            wind.Configure(WindColumn2D.WindMode.Constant, Vector2.right,
                WindColumn2D.DefaultSpeed, volume.size);
            Require(PrefabUtility.SaveAsPrefabAsset(root, WindColumnPath) != null,
                $"Failed to save {WindColumnPath}.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static MovingTornado2D CreateMovingTornado()
    {
        Sprite[] frames = ImportTornadoFrames();
        GameObject root = new("MovingTornado2D");
        try
        {
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(.8f, .8f);
            MovingTornado2D tornado = root.AddComponent<MovingTornado2D>();
            tornado.ConfigureReferences(trigger);
            tornado.Configure(Vector2.right, MovingTornado2D.DefaultSpeed,
                MovingTornado2D.DefaultMaximumDistance);
            GameObject visualObject = new("Visual");
            visualObject.transform.SetParent(root.transform, false);
            SpriteRenderer visual = visualObject.AddComponent<SpriteRenderer>();
            visual.sprite = frames[0];
            visual.color = Color.white;
            visual.sortingOrder = 6;
            SpriteFrameAnimator2D animator = visualObject.AddComponent<SpriteFrameAnimator2D>();
            animator.Configure(visual, frames, 8f);
            Require(PrefabUtility.SaveAsPrefabAsset(root, MovingTornadoPath) != null,
                $"Failed to save {MovingTornadoPath}.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
        return AssetDatabase.LoadAssetAtPath<GameObject>(MovingTornadoPath)
            ?.GetComponent<MovingTornado2D>();
    }

    private static Sprite[] ImportTornadoFrames()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TornadoArtPath) as TextureImporter;
        Require(importer != null, $"Missing generated tornado art at {TornadoArtPath}.");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 835f / .8f;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        const int frameWidth = 627;
        SpriteMetaData[] sheet = new SpriteMetaData[3];
        for (int i = 0; i < sheet.Length; i++)
        {
            sheet[i] = new SpriteMetaData
            {
                name = $"small_tornado_{i}",
                rect = new Rect(i * frameWidth, 0, frameWidth, 835),
                alignment = (int)SpriteAlignment.Custom,
                pivot = new Vector2(.5f, .5f)
            };
        }
#pragma warning disable CS0618
        importer.spritesheet = sheet;
#pragma warning restore CS0618
        importer.SaveAndReimport();
        Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(TornadoArtPath)
            .OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
        Require(frames.Length == 3, "Tornado Sprite Sheet must import exactly three frames.");
        return frames;
    }

    private static void CreateTornadoGenerator(MovingTornado2D tornadoPrefab)
    {
        Require(tornadoPrefab != null, $"Missing generated tornado Prefab at {MovingTornadoPath}.");
        GameObject root = new("TornadoGenerator2D");
        try
        {
            TornadoGenerator2D generator = root.AddComponent<TornadoGenerator2D>();
            SpriteRenderer visual = Visual("Visual", root.transform, new Vector2(1f, 1f),
                new Color(.25f, .72f, .92f, 1f), 5);
            generator.ConfigureVisual(visual);
            generator.Configure(tornadoPrefab, Vector2.right,
                TornadoGenerator2D.DefaultSpawnInterval,
                TornadoGenerator2D.DefaultMaximumAlive, new Vector2(.8f, .8f));
            Require(PrefabUtility.SaveAsPrefabAsset(root, TornadoGeneratorPath) != null,
                $"Failed to save {TornadoGeneratorPath}.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CreateWindDeflector()
    {
        GameObject root = new("WindDeflector2D");
        try
        {
            BoxCollider2D solid = root.AddComponent<BoxCollider2D>();
            solid.isTrigger = false;
            solid.size = Vector2.one;
            GameObject outputObject = new("OutputVolume");
            outputObject.transform.SetParent(root.transform, false);
            BoxCollider2D output = outputObject.AddComponent<BoxCollider2D>();
            output.isTrigger = true;
            output.size = new Vector2(1f, 6f);
            SpriteRenderer visual = Visual("Visual", root.transform, Vector2.one,
                new Color(.3f, .75f, 1f, 1f), 7);
            WindDeflector2D deflector = root.AddComponent<WindDeflector2D>();
            deflector.ConfigureReferences(solid, output, visual);
            deflector.Configure(Vector2.right, false, output.size);
            Require(PrefabUtility.SaveAsPrefabAsset(root, WindDeflectorPath) != null,
                $"Failed to save {WindDeflectorPath}.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CreateWindTurbine()
    {
        GameObject root = new("WindTurbineSwitch2D");
        try
        {
            BoxCollider2D receiver = root.AddComponent<BoxCollider2D>();
            receiver.isTrigger = true;
            receiver.size = Vector2.one;
            SpriteRenderer rotor = Visual("RotorVisual", root.transform, new Vector2(.9f, .9f),
                new Color(.38f, .62f, .7f, 1f), 7);
            WindTurbineSwitch2D turbine = root.AddComponent<WindTurbineSwitch2D>();
            turbine.ConfigureReferences(receiver, rotor);
            turbine.Configure(Vector2.right);
            Require(PrefabUtility.SaveAsPrefabAsset(root, WindTurbinePath) != null,
                $"Failed to save {WindTurbinePath}.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static SpriteRenderer Visual(string name, Transform parent, Vector2 size, Color color, int order)
    {
        GameObject visualObject = new(name);
        visualObject.transform.SetParent(parent, false);
        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        renderer.color = color;
        renderer.sortingOrder = order;
        Vector2 nativeSize = renderer.sprite.bounds.size;
        visualObject.transform.localScale = new Vector3(size.x / nativeSize.x, size.y / nativeSize.y, 1f);
        return renderer;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
