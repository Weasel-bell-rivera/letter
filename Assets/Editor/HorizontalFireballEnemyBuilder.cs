using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HorizontalFireballEnemyBuilder
{
    public const string SettingsPath = "Assets/Settings/Enemies/DefaultHorizontalFireballEnemy.asset";
    public const string EnemyPrefabPath =
        "Assets/Prefabs/Gameplay/Enemies/HorizontalFireballEnemy2D.prefab";
    public const string EnemyBodySpritePath =
        "Assets/Art/Generated/Enemies/FurnaceToad.png";
    public const string ProjectilePrefabPath =
        "Assets/Prefabs/Gameplay/Enemies/Projectiles/HorizontalFireballProjectile2D.prefab";
    public const string ProjectileSpritePath =
        "Assets/Art/Generated/Enemies/Projectiles/Fireball.png";

    [MenuItem("Tools/W1/Build Horizontal Fireball Enemy Assets")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    public static void BuildFromCommandLine()
    {
        BuildAssets();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Horizontal fireball enemy assets built successfully.");
    }

    public static void BuildAssets()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
        Directory.CreateDirectory(Path.GetDirectoryName(EnemyPrefabPath));
        Directory.CreateDirectory(Path.GetDirectoryName(ProjectilePrefabPath));
        HorizontalFireballEnemySettings settings = CreateOrUpdateSettings();
        HorizontalFireballProjectile2D projectile = CreateOrUpdateProjectilePrefab();
        CreateOrUpdateEnemyPrefab(settings, projectile);
    }

    private static HorizontalFireballEnemySettings CreateOrUpdateSettings()
    {
        HorizontalFireballEnemySettings settings =
            AssetDatabase.LoadAssetAtPath<HorizontalFireballEnemySettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<HorizontalFireballEnemySettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }
        settings.name = "DefaultHorizontalFireballEnemy";
        settings.Configure(6f, .75f, .6f, 1.4f, 8f, 2f, 1f, .05f);
        EditorUtility.SetDirty(settings);
        return settings;
    }

    private static HorizontalFireballProjectile2D CreateOrUpdateProjectilePrefab()
    {
        GameObject root = new("HorizontalFireballProjectile2D");
        try
        {
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D trigger = root.AddComponent<CircleCollider2D>();
            trigger.radius = .2f;
            trigger.isTrigger = true;
            root.AddComponent<HorizontalFireballProjectile2D>();

            GameObject visual = Child("Visual", root.transform);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = ProjectileSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = 7;
            ScaleVisual(renderer, new Vector2(.5f, .4f));

            GameObject trailObject = Child("Trail", root.transform);
            LineRenderer trail = trailObject.AddComponent<LineRenderer>();
            trail.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
            trail.useWorldSpace = false;
            trail.positionCount = 2;
            trail.SetPosition(0, new Vector3(-.4f, 0f, 0f));
            trail.SetPosition(1, new Vector3(-.1f, 0f, 0f));
            trail.startWidth = .16f;
            trail.endWidth = .05f;
            trail.startColor = new Color(1f, .35f, .04f, .7f);
            trail.endColor = new Color(1f, .75f, .1f, .15f);
            trail.sortingOrder = 6;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
            Require(saved != null, $"Failed to save fireball Prefab: {ProjectilePrefabPath}");
            return AssetDatabase.LoadAssetAtPath<HorizontalFireballProjectile2D>(ProjectilePrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CreateOrUpdateEnemyPrefab(HorizontalFireballEnemySettings settings,
        HorizontalFireballProjectile2D projectile)
    {
        GameObject root = new("HorizontalFireballEnemy2D");
        try
        {
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            AudioSource audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
            HorizontalFireballEnemy2D enemy = root.AddComponent<HorizontalFireballEnemy2D>();

            GameObject visualRoot = Child("Visual", root.transform);
            SpriteRenderer bodyVisual = Visual("BodyVisual", visualRoot.transform,
                new Vector2(1.15f, 1f), Color.white, 5, EnemyBodySprite());
            SpriteRenderer muzzleVisual = Visual("MuzzleVisual", visualRoot.transform,
                new Vector2(.2f, .2f), new Color(1f, .45f, .08f, 1f), 6, BuiltinMuzzleSprite());
            muzzleVisual.transform.localPosition = new Vector3(.55f, 0f, 0f);

            GameObject solidObject = Child("BodyCollider", root.transform);
            BoxCollider2D solid = solidObject.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(.9f, 1f);

            GameObject damageObject = Child("DamageTrigger", root.transform);
            BoxCollider2D damageCollider = damageObject.AddComponent<BoxCollider2D>();
            damageCollider.size = new Vector2(1f, 1.1f);
            damageCollider.offset = new Vector2(0f, .05f);
            damageCollider.isTrigger = true;
            HorizontalFireballEnemyDamageTrigger2D damage =
                damageObject.AddComponent<HorizontalFireballEnemyDamageTrigger2D>();

            Transform origin = Child("FireOrigin", root.transform).transform;
            origin.localPosition = new Vector3(.7f, 0f, 0f);
            enemy.Configure(settings, projectile, solid, damage, origin, bodyVisual, muzzleVisual, audio);
            enemy.SetInitiallyFacingRight(true);
            damage.Configure(enemy);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            Require(saved != null, $"Failed to save enemy Prefab: {EnemyPrefabPath}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static SpriteRenderer Visual(string name, Transform parent, Vector2 size, Color color, int order,
        Sprite sprite = null)
    {
        GameObject go = Child(name, parent);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : BuiltinSprite();
        renderer.color = color;
        renderer.sortingOrder = order;
        ScaleVisual(renderer, size);
        return renderer;
    }

    private static void ScaleVisual(SpriteRenderer renderer, Vector2 size)
    {
        Vector2 native = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
    }

    private static Sprite BuiltinSprite()
        => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    private static Sprite BuiltinMuzzleSprite()
        => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

    private static Sprite EnemyBodySprite()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnemyBodySpritePath);
        Require(sprite != null, $"Missing furnace toad body sprite: {EnemyBodySpritePath}");
        return sprite;
    }

    private static Sprite ProjectileSprite()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ProjectileSpritePath);
        Require(sprite != null, $"Missing fireball sprite: {ProjectileSpritePath}");
        return sprite;
    }

    private static GameObject Child(string name, Transform parent)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
