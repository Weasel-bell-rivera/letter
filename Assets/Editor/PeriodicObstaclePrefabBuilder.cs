using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PeriodicObstaclePrefabBuilder
{
    public const string SpearPath = "Assets/Prefabs/Gameplay/Hazards/RetractableSpear2D.prefab";
    public const string LaserPath = "Assets/Prefabs/Gameplay/Hazards/CrystalLaser2D.prefab";
    public const string FlipPlatformPath = "Assets/Prefabs/Gameplay/Platforms/TimedFlipPlatform2D.prefab";

    [MenuItem("Tools/W1/Build Periodic Obstacle Prefabs")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SpearPath));
        Directory.CreateDirectory(Path.GetDirectoryName(FlipPlatformPath));
        BuildSpear();
        BuildLaser();
        BuildFlipPlatform();
        AssetDatabase.SaveAssets();
        Debug.Log("Built retractable spear, crystal laser, and timed flip platform Prefabs.");
    }

    private static void BuildSpear()
    {
        GameObject root = new("RetractableSpear2D");
        try
        {
            RetractableSpear2D spear = root.AddComponent<RetractableSpear2D>();

            GameObject baseObject = SpriteObject("Base", root.transform, new Vector2(.85f, .5f),
                new Color(.34f, .38f, .44f, 1f), 2);
            baseObject.transform.localPosition = new Vector3(0f, -.25f, 0f);

            GameObject movingPart = new("MovingPart");
            movingPart.transform.SetParent(root.transform, false);
            // Keep only the tip visible at the installation opening while retracted.
            movingPart.transform.localPosition = new Vector3(0f, -1.5f, 0f);
            BoxCollider2D danger = movingPart.AddComponent<BoxCollider2D>();
            danger.isTrigger = true;
            danger.size = new Vector2(.42f, 1.65f);
            danger.offset = new Vector2(0f, .825f);
            Hazard2D hazard = movingPart.AddComponent<Hazard2D>();
            hazard.SetActive(false);
            danger.enabled = false;

            GameObject shaft = SpriteObject("Shaft", movingPart.transform, new Vector2(.32f, 1.35f),
                new Color(.78f, .82f, .88f, 1f), 3);
            shaft.transform.localPosition = new Vector3(0f, .675f, 0f);
            GameObject tip = SpriteObject("Tip", movingPart.transform, new Vector2(.48f, .48f),
                new Color(.94f, .97f, 1f, 1f), 4);
            tip.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            spear.Configure(movingPart.transform, hazard, baseObject.GetComponent<SpriteRenderer>(),
                1.5f, 1f, .55f, .14f, .7f, .16f);
            Save(root, SpearPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void BuildLaser()
    {
        GameObject root = new("CrystalLaser2D");
        try
        {
            CrystalLaser2D laser = root.AddComponent<CrystalLaser2D>();
            GameObject emitter = SpriteObject("Emitter", root.transform, new Vector2(.7f, .7f),
                new Color(.5f, .25f, .78f, 1f), 4);
            emitter.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            GameObject dangerObject = new("DangerZone");
            dangerObject.transform.SetParent(root.transform, false);
            BoxCollider2D danger = dangerObject.AddComponent<BoxCollider2D>();
            danger.isTrigger = true;
            Hazard2D hazard = dangerObject.AddComponent<Hazard2D>();
            hazard.SetActive(false);
            danger.enabled = false;

            SpriteRenderer warning = SpriteObject("WarningLine", root.transform, new Vector2(6f, .04f),
                new Color(1f, .25f, .75f, .8f), 2).GetComponent<SpriteRenderer>();
            SpriteRenderer beam = SpriteObject("Beam", root.transform, new Vector2(6f, .22f),
                new Color(1f, .82f, 1f, .95f), 3).GetComponent<SpriteRenderer>();
            laser.Configure(hazard, danger, warning, beam, 6f, .22f, .65f, .8f, 1.25f);
            Save(root, LaserPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void BuildFlipPlatform()
    {
        GameObject root = new("TimedFlipPlatform2D");
        try
        {
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(2f, .35f);
            SurfaceSemantic2D semantic = root.AddComponent<SurfaceSemantic2D>();
            semantic.Configure(SurfaceSemantic2D.SurfaceType.DynamicSurface, false, true);

            GameObject visual = new("Visual");
            visual.transform.SetParent(root.transform, false);
            GameObject bodyVisual = SpriteObject("Body", visual.transform, new Vector2(2f, .35f),
                new Color(.25f, .62f, .75f, 1f), 2);
            GameObject topMarker = SpriteObject("TopMarker", visual.transform, new Vector2(1.8f, .07f),
                new Color(.72f, .95f, 1f, 1f), 3);
            topMarker.transform.localPosition = new Vector3(0f, .13f, 0f);

            TimedFlipPlatform2D platform = root.AddComponent<TimedFlipPlatform2D>();
            platform.Configure(visual.transform, bodyVisual.GetComponent<SpriteRenderer>(), 1.5f, .4f, .35f);
            Save(root, FlipPlatformPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static GameObject SpriteObject(string name, Transform parent, Vector2 size, Color color,
        int sortingOrder)
    {
        GameObject result = new(name);
        result.transform.SetParent(parent, false);
        SpriteRenderer renderer = result.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        renderer.color = color;
        renderer.drawMode = SpriteDrawMode.Simple;
        Vector2 spriteSize = renderer.sprite != null ? renderer.sprite.bounds.size : Vector2.one;
        result.transform.localScale = new Vector3(
            size.x / Mathf.Max(.001f, spriteSize.x),
            size.y / Mathf.Max(.001f, spriteSize.y), 1f);
        renderer.sortingOrder = sortingOrder;
        return result;
    }

    private static void Save(GameObject root, string path)
    {
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        if (saved == null) throw new InvalidOperationException($"Failed to save Prefab at {path}.");
    }
}
