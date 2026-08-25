using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Builds the approved reusable assets required before SNOW_001 greyboxing.</summary>
public static class SnowPrerequisiteBuilder
{
    public const string SnowTexturePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_snow_block.png";
    public const string FrozenGroundTilePath = "Assets/Tiles/Snow/FrozenGroundSnowBlock.asset";
    public const string FrozenGroundMaterialPath = "Assets/Settings/Physics/FrozenGround.physicsMaterial2D";
    public const string EnemyPrefabPath = "Assets/Prefabs/Gameplay/Enemies/FreezablePatrolEnemy2D.prefab";

    [MenuItem("Tools/W1/Build Snow Prerequisite Assets")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    public static void BuildFromCommandLine()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FrozenGroundTilePath));
        Directory.CreateDirectory(Path.GetDirectoryName(FrozenGroundMaterialPath));
        Directory.CreateDirectory(Path.GetDirectoryName(EnemyPrefabPath));
        Require(File.Exists(SnowTexturePath), $"Missing imported snow texture at {SnowTexturePath}.");
        ConfigureSnowTexture();
        CreateFrozenGroundTile();
        CreateFrozenGroundMaterial();
        CreateEnemyPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Snow prerequisite assets built successfully.");
    }

    private static void CreateFrozenGroundMaterial()
    {
        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(FrozenGroundMaterialPath);
        if (material == null)
        {
            material = new PhysicsMaterial2D("FrozenGround");
            AssetDatabase.CreateAsset(material, FrozenGroundMaterialPath);
        }
        material.friction = 0f;
        material.bounciness = 0f;
        EditorUtility.SetDirty(material);
    }

    private static void ConfigureSnowTexture()
    {
        AssetDatabase.ImportAsset(SnowTexturePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(SnowTexturePath) as TextureImporter;
        Require(importer != null, "Snow texture importer is unavailable.");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 64f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static void CreateFrozenGroundTile()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SnowTexturePath);
        Require(sprite != null, "Snow texture did not import as a Sprite.");
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(FrozenGroundTilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, FrozenGroundTilePath);
        }
        tile.name = "FrozenGroundSnowBlock";
        tile.sprite = sprite;
        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
    }

    private static void CreateEnemyPrefab()
    {
        GameObject root = new("FreezablePatrolEnemy2D");
        try
        {
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 1f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            AudioSource freezeAudio = root.AddComponent<AudioSource>();
            freezeAudio.playOnAwake = false;
            freezeAudio.spatialBlend = 0f;

            GameObject visualRoot = Child(root.transform, "Visual", Vector3.zero);
            GameObject activeVisual = Visual(visualRoot.transform, "ActiveVisual", new Vector2(1.2f, 1f),
                new Color(.85f, .28f, .32f));
            GameObject frozenVisual = Visual(visualRoot.transform, "FrozenVisual", new Vector2(1.2f, 1f),
                new Color(.55f, .9f, 1f));
            GameObject freezeEffect = Visual(visualRoot.transform, "FreezeEffect", new Vector2(1.42f, 1.2f),
                new Color(.75f, .95f, 1f, .35f));
            frozenVisual.SetActive(false);
            freezeEffect.SetActive(false);

            GameObject bodyObject = Child(root.transform, "BodyCollider", Vector3.zero);
            BoxCollider2D solid = bodyObject.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(1.2f, 1f);
            SurfaceSemantic2D dynamicSurface = bodyObject.AddComponent<SurfaceSemantic2D>();
            dynamicSurface.Configure(SurfaceSemantic2D.SurfaceType.DynamicSurface, false, false);

            GameObject damageObject = Child(root.transform, "DamageTrigger", Vector3.zero);
            BoxCollider2D damageCollider = damageObject.AddComponent<BoxCollider2D>();
            damageCollider.size = new Vector2(1.34f, 1.08f);
            damageCollider.isTrigger = true;
            EnemyDamageTrigger2D damage = damageObject.AddComponent<EnemyDamageTrigger2D>();

            Transform groundProbe = Child(root.transform, "GroundProbe", new Vector3(0f, -.45f, 0f)).transform;
            Transform surfaceProbe = Child(root.transform, "SurfaceProbe", new Vector3(0f, -.45f, 0f)).transform;

            FreezablePatrolEnemy2D enemy = root.AddComponent<FreezablePatrolEnemy2D>();
            root.AddComponent<FreezingVisual2D>();
            damage.Configure(enemy);
            enemy.ConfigurePrefabReferences(solid, damage, groundProbe, surfaceProbe,
                activeVisual, frozenVisual, freezeEffect);
            enemy.ConfigurePatrol(-2f, 2f, 2f, .35f, true);
            PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static GameObject Child(Transform parent, string name, Vector3 localPosition)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        return child;
    }

    private static GameObject Visual(Transform parent, string name, Vector2 size, Color color)
    {
        GameObject visual = Child(parent, name, Vector3.zero);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        renderer.color = color;
        Vector2 nativeSize = renderer.sprite.bounds.size;
        visual.transform.localScale = new Vector3(size.x / nativeSize.x, size.y / nativeSize.y, 1f);
        return visual;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
