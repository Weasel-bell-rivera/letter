using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Builds the shared fixed thrower and deterministic arc fireball assets.</summary>
public static class GroundFireThrowerEnemyBuilder
{
    public const string SettingsPath =
        "Assets/Settings/Enemies/DefaultGroundFireThrowerEnemy.asset";
    public const string ProjectilePrefabPath =
        "Assets/Prefabs/Gameplay/Projectiles/ArcFireballProjectile2D.prefab";
    public const string EnemyPrefabPath =
        "Assets/Prefabs/Gameplay/Enemies/GroundFireThrowerEnemy2D.prefab";

    [MenuItem("Tools/W1/Build Ground Fire Thrower Enemy Assets")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    public static void BuildFromCommandLine()
    {
        BuildAssets();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Ground fire thrower settings, projectile and enemy Prefabs built successfully.");
    }

    public static void BuildAssets()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
        Directory.CreateDirectory(Path.GetDirectoryName(ProjectilePrefabPath));
        Directory.CreateDirectory(Path.GetDirectoryName(EnemyPrefabPath));
        GroundFireThrowerEnemySettings settings = CreateOrUpdateSettings();
        ArcFireballProjectile2D projectile = CreateOrUpdateProjectilePrefab();
        CreateOrUpdateEnemyPrefab(settings, projectile);
    }

    private static GroundFireThrowerEnemySettings CreateOrUpdateSettings()
    {
        GroundFireThrowerEnemySettings settings =
            AssetDatabase.LoadAssetAtPath<GroundFireThrowerEnemySettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<GroundFireThrowerEnemySettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }
        settings.name = "DefaultGroundFireThrowerEnemy";
        settings.Configure(7f, .8f, 7f, 2f, 1.8f, 3f, .35f);
        EditorUtility.SetDirty(settings);
        BindRuntimeScript(settings, "Assets/Scripts/Gameplay/GroundFireThrowerEnemySettings.cs");
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<GroundFireThrowerEnemySettings>(SettingsPath);
    }

    private static ArcFireballProjectile2D CreateOrUpdateProjectilePrefab()
    {
        GameObject root = new("ArcFireballProjectile2D");
        try
        {
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D hit = root.AddComponent<CircleCollider2D>();
            hit.radius = .35f;
            hit.isTrigger = true;

            ArcFireballProjectile2D projectile = root.AddComponent<ArcFireballProjectile2D>();
            BindRuntimeScript(projectile, "Assets/Scripts/Gameplay/ArcFireballProjectile2D.cs");
            projectile = root.GetComponent<ArcFireballProjectile2D>();
            Require(projectile != null, "Could not bind ArcFireballProjectile2D to its runtime script.");

            GameObject visualRoot = Child("Visual", root.transform);
            SpriteRenderer glow = Visual("Glow", visualRoot.transform, new Vector2(.68f, .68f),
                new Color(1f, .28f, .04f, .28f), 14);
            glow.transform.localPosition = Vector3.zero;
            SpriteRenderer bodyVisual = Visual("BodyVisual", visualRoot.transform,
                new Vector2(.46f, .46f), new Color(1f, .72f, .08f, 1f), 15);
            SpriteRenderer core = Visual("Core", bodyVisual.transform,
                new Vector2(.2f, .2f), new Color(1f, .96f, .62f, 1f), 16);
            core.transform.localPosition = Vector3.zero;

            projectile.ConfigurePrefabReferences(bodyVisual);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
            Require(saved != null, $"Failed to save fireball Prefab: {ProjectilePrefabPath}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
        return AssetDatabase.LoadAssetAtPath<ArcFireballProjectile2D>(ProjectilePrefabPath);
    }

    private static void CreateOrUpdateEnemyPrefab(GroundFireThrowerEnemySettings settings,
        ArcFireballProjectile2D projectilePrefab)
    {
        Require(projectilePrefab != null, $"Missing projectile Prefab: {ProjectilePrefabPath}");
        GameObject root = new("GroundFireThrowerEnemy2D");
        try
        {
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            GroundFireThrowerEnemy2D enemy = root.AddComponent<GroundFireThrowerEnemy2D>();
            BindRuntimeScript(enemy, "Assets/Scripts/Gameplay/GroundFireThrowerEnemy2D.cs");
            enemy = root.GetComponent<GroundFireThrowerEnemy2D>();
            Require(enemy != null, "Could not bind GroundFireThrowerEnemy2D to its runtime script.");

            GameObject bodyObject = Child("BodyCollider", root.transform);
            BoxCollider2D bodyCollider = bodyObject.AddComponent<BoxCollider2D>();
            bodyCollider.size = new Vector2(.9f, .9f);
            SurfaceSemantic2D bodySurface = bodyObject.AddComponent<SurfaceSemantic2D>();
            BindRuntimeScript(bodySurface, "Assets/Scripts/Gameplay/SurfaceSemantic2D.cs");
            bodySurface = bodyObject.GetComponent<SurfaceSemantic2D>();
            Require(bodySurface != null, "Could not bind SurfaceSemantic2D to the thrower body.");
            bodySurface.Configure(SurfaceSemantic2D.SurfaceType.DynamicSurface, false, false);

            GameObject damageObject = Child("DamageTrigger", root.transform);
            BoxCollider2D damageCollider = damageObject.AddComponent<BoxCollider2D>();
            damageCollider.size = new Vector2(1f, 1f);
            damageCollider.isTrigger = true;
            GroundFireThrowerDamageTrigger2D damage =
                damageObject.AddComponent<GroundFireThrowerDamageTrigger2D>();
            BindRuntimeScript(damage,
                "Assets/Scripts/Gameplay/GroundFireThrowerDamageTrigger2D.cs");
            damage = damageObject.GetComponent<GroundFireThrowerDamageTrigger2D>();
            Require(damage != null, "Could not bind GroundFireThrowerDamageTrigger2D to its runtime script.");

            Transform facingRoot = Child("FacingRoot", root.transform).transform;
            GameObject visualRoot = Child("Visual", facingRoot);
            SpriteRenderer bodyVisual = Visual("BodyVisual", visualRoot.transform,
                new Vector2(.82f, .82f), new Color(.88f, .44f, .2f, 1f), 6);
            bodyVisual.transform.localPosition = new Vector3(0f, .02f, 0f);
            SpriteRenderer eye = Visual("Eye", visualRoot.transform, new Vector2(.14f, .14f),
                new Color(.12f, .06f, .03f, 1f), 7);
            eye.transform.localPosition = new Vector3(.2f, .14f, 0f);
            SpriteRenderer foot = Visual("Foot", visualRoot.transform, new Vector2(.72f, .14f),
                new Color(.3f, .12f, .06f, 1f), 7);
            foot.transform.localPosition = new Vector3(0f, -.36f, 0f);

            Transform throwOrigin = Child("ThrowOrigin", facingRoot).transform;
            throwOrigin.localPosition = new Vector3(.55f, .27f, 0f);
            GameObject charge = Child("ChargeVisual", facingRoot);
            SpriteRenderer chargeRenderer = Visual("BodyVisual", charge.transform,
                new Vector2(.38f, .38f), new Color(1f, .75f, .08f, .92f), 12);
            chargeRenderer.transform.localPosition = Vector3.zero;
            charge.transform.localPosition = throwOrigin.localPosition;
            charge.SetActive(false);

            Transform sightOrigin = Child("LineOfSightOrigin", root.transform).transform;
            sightOrigin.localPosition = new Vector3(0f, .18f, 0f);

            GameObject marker = Child("TargetMarker", root.transform);
            SpriteRenderer markerOuter = Visual("Outer", marker.transform,
                new Vector2(.42f, .12f), new Color(1f, .45f, .06f, .92f), 11);
            SpriteRenderer markerInner = Visual("Inner", marker.transform,
                new Vector2(.12f, .42f), new Color(1f, .45f, .06f, .92f), 11);
            markerOuter.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            markerInner.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            marker.SetActive(false);

            enemy.Configure(settings, projectilePrefab, bodyCollider, bodySurface, damage,
                facingRoot, sightOrigin, throwOrigin, bodyVisual, charge, marker);
            enemy.SetInitiallyFacingRight(true);
            damage.Configure(enemy);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            Require(saved != null, $"Failed to save ground fire thrower Prefab: {EnemyPrefabPath}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static SpriteRenderer Visual(string name, Transform parent, Vector2 size,
        Color color, int order)
    {
        GameObject go = Child(name, parent);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = BuiltinSprite();
        renderer.color = color;
        renderer.sortingOrder = order;
        Vector2 native = renderer.sprite.bounds.size;
        go.transform.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
        return renderer;
    }

    private static Sprite BuiltinSprite()
        => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    private static GameObject Child(string name, Transform parent)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void BindRuntimeScript(UnityEngine.Object behaviour, string scriptPath)
    {
        MonoScript script = behaviour switch
        {
            MonoBehaviour component => MonoScript.FromMonoBehaviour(component),
            ScriptableObject asset => MonoScript.FromScriptableObject(asset),
            _ => null
        };
        script ??= AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        Require(script != null, $"Runtime script asset is missing: {scriptPath}");
        Require(script.GetClass() == behaviour.GetType(),
            $"Runtime script does not resolve {behaviour.GetType().Name}: {scriptPath}");
        SerializedObject serialized = new(behaviour);
        SerializedProperty scriptProperty = serialized.FindProperty("m_Script");
        Require(scriptProperty != null, $"m_Script is unavailable on {behaviour.GetType().Name}.");
        scriptProperty.objectReferenceValue = script;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
