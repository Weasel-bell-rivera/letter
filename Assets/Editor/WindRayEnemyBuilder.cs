using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class WindRayEnemyBuilder
{
    public const string SettingsPath = "Assets/Settings/Enemies/DefaultWindRayEnemy.asset";
    public const string PrefabPath = "Assets/Prefabs/Gameplay/Enemies/WindRayEnemy2D.prefab";
    public const string RestSpritePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Enemies/Double/Bee/bee_rest.png";
    public const string WingUpSpritePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Enemies/Double/Bee/bee_a.png";
    public const string WingDownSpritePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Enemies/Double/Bee/bee_b.png";
    public const string FlyClipPath = "Assets/Animations/Enemies/WindRay/WindRayFly.anim";
    public const string AnimatorControllerPath =
        "Assets/Animations/Enemies/WindRay/WindRayAnimator.controller";

    [MenuItem("Tools/W1/Build Wind Ray Enemy Assets")]
    public static void BuildFromMenu() => BuildFromCommandLine();

    public static void BuildFromCommandLine()
    {
        BuildAssets();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Wind ray shared settings and Prefab built successfully.");
    }

    public static void BuildAssets()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        Directory.CreateDirectory(Path.GetDirectoryName(FlyClipPath));
        Sprite rest = ConfigureSprite(RestSpritePath);
        Sprite wingUp = ConfigureSprite(WingUpSpritePath);
        Sprite wingDown = ConfigureSprite(WingDownSpritePath);
        AnimatorController animatorController = CreateOrUpdateAnimator(rest, wingUp, wingDown);
        WindRayEnemySettings settings = CreateOrUpdateSettings();
        CreateOrUpdatePrefab(settings, rest, animatorController);
    }

    private static Sprite ConfigureSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Require(importer != null, $"Missing wind ray animation source: {path}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Require(sprite != null, $"Wind ray source did not import as a Sprite: {path}");
        return sprite;
    }

    private static AnimatorController CreateOrUpdateAnimator(Sprite rest, Sprite wingUp, Sprite wingDown)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(FlyClipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = "WindRayFly" };
            AssetDatabase.CreateAsset(clip, FlyClipPath);
        }
        clip.frameRate = 8f;
        EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve(
            "Visual/BodyVisual", typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] frames =
        {
            new() { time = 0f, value = rest },
            new() { time = .125f, value = wingUp },
            new() { time = .25f, value = wingDown },
            new() { time = .375f, value = wingUp },
            new() { time = .5f, value = rest }
        };
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, frames);
        AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
        EditorUtility.SetDirty(clip);

        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState flyState = null;
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            if (child.state.name == "Fly")
            {
                flyState = child.state;
                break;
            }
        }
        flyState ??= stateMachine.AddState("Fly");
        flyState.motion = clip;
        stateMachine.defaultState = flyState;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static WindRayEnemySettings CreateOrUpdateSettings()
    {
        WindRayEnemySettings settings = AssetDatabase.LoadAssetAtPath<WindRayEnemySettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<WindRayEnemySettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }
        settings.name = "DefaultWindRayEnemy";
        settings.Configure(6f, .75f, .75f, 12f, 7f, 1.5f, 2f, .05f);
        EditorUtility.SetDirty(settings);
        return settings;
    }

    private static void CreateOrUpdatePrefab(WindRayEnemySettings settings, Sprite rest,
        RuntimeAnimatorController animatorController)
    {
        GameObject root = new("WindRayEnemy2D");
        try
        {
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            AudioSource audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;

            WindRayEnemy2D enemy = root.AddComponent<WindRayEnemy2D>();
            BindRuntimeScript(enemy, "Assets/Scripts/Gameplay/WindRayEnemy2D.cs");

            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = animatorController;

            GameObject visualRoot = Child("Visual", root.transform);
            GameObject bodyObject = Child("BodyVisual", visualRoot.transform);
            SpriteRenderer bodyVisual = bodyObject.AddComponent<SpriteRenderer>();
            bodyVisual.sprite = rest;
            bodyVisual.color = Color.white;
            bodyVisual.sortingOrder = 5;

            GameObject marker = Child("TargetMarker", visualRoot.transform);
            SpriteRenderer markerVisual = marker.AddComponent<SpriteRenderer>();
            markerVisual.sprite = BuiltinSprite();
            markerVisual.color = new Color(1f, .82f, .1f, .9f);
            markerVisual.sortingOrder = 8;
            marker.transform.localScale = new Vector3(.32f, .32f, 1f);
            marker.SetActive(false);

            LineRenderer trail = Line("DashTrail", visualRoot.transform,
                new Color(.7f, .95f, 1f, .75f), .18f);
            trail.useWorldSpace = true;
            trail.positionCount = 2;
            trail.enabled = false;
            trail.sortingOrder = 3;

            GameObject triggerObject = Child("DamageTrigger", root.transform);
            BoxCollider2D triggerCollider = triggerObject.AddComponent<BoxCollider2D>();
            triggerCollider.size = new Vector2(1.15f, .7f);
            triggerCollider.isTrigger = true;
            WindRayDamageTrigger2D trigger = triggerObject.AddComponent<WindRayDamageTrigger2D>();
            BindRuntimeScript(trigger, "Assets/Scripts/Gameplay/WindRayDamageTrigger2D.cs");

            Transform sightOrigin = Child("LineOfSightOrigin", root.transform).transform;
            enemy.Configure(settings, trigger, sightOrigin, bodyVisual, marker, trail, audio);
            enemy.SetInitialVisualFacing(new Vector2(-1f, -1f));
            trigger.Configure(enemy);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Require(saved != null, $"Failed to save wind ray Prefab: {PrefabPath}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static LineRenderer Line(string name, Transform parent, Color color, float width)
    {
        GameObject go = Child(name, parent);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
        line.startColor = color;
        line.endColor = color;
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 2;
        return line;
    }

    private static SpriteRenderer Visual(string name, Transform parent, Vector2 size, Color color, int order)
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
        MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        Require(script != null, $"Runtime script asset is missing: {scriptPath}");
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
