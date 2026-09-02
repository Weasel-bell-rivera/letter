using UnityEngine;
using W1.Accessibility;

public sealed class MirrorPlatformerBootstrap : MonoBehaviour
{
    private static Sprite runtimeSprite;

    private void Awake()
    {
        if (GameObject.Find("Mirror Platformer Prototype") != null)
            return;

        BuildPrototype();
    }

    private void BuildPrototype()
    {
        Camera cameraComponent = GetComponent<Camera>();
        if (cameraComponent != null)
        {
            cameraComponent.orthographic = true;
            cameraComponent.orthographicSize = 6f;
            cameraComponent.backgroundColor = new Color(0.035f, 0.055f, 0.1f, 1f);
            transform.position = new Vector3(0f, 0.5f, -10f);
        }

        GameObject root = new("Mirror Platformer Prototype");
        CreatePlatform(root.transform, "Ground", new Vector2(0f, -4.4f), new Vector2(18f, 1f));
        CreatePlatform(root.transform, "Platform Left Low", new Vector2(-4f, -2.3f), new Vector2(3f, 0.35f));
        CreatePlatform(root.transform, "Platform Right Low", new Vector2(4f, -2.3f), new Vector2(3f, 0.35f));
        CreatePlatform(root.transform, "Platform Left High", new Vector2(-2.2f, 0f), new Vector2(2.5f, 0.35f));
        CreatePlatform(root.transform, "Platform Right High", new Vector2(2.2f, 0f), new Vector2(2.5f, 0.35f));

        Transform door = CreateVisual(root.transform, "Door", new Vector2(7.2f, -2.9f),
            new Vector2(0.45f, 2f), new Color(0.9f, 0.25f, 0.3f, 1f), 4);
        BoxCollider2D doorCollider = door.gameObject.AddComponent<BoxCollider2D>();

        Transform plate = CreateVisual(root.transform, "Pressure Plate", new Vector2(4f, -3.82f),
            new Vector2(1.2f, 0.16f), new Color(1f, 0.65f, 0.12f, 1f), 5);
        plate.gameObject.AddComponent<BoxCollider2D>();
        PressurePlate2D pressurePlate = plate.gameObject.AddComponent<PressurePlate2D>();
        pressurePlate.Configure(doorCollider, door.GetComponent<SpriteRenderer>());

        GameObject player = new("Player");
        player.transform.SetParent(root.transform);
        player.transform.position = new Vector3(-4f, -1.2f, 0f);
        player.layer = 2;

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 3.2f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.85f, 1.45f);

        Transform visual = CreatePlayerVisual(player.transform);
        PlayerController2D controller = player.AddComponent<PlayerController2D>();
        controller.Configure(visual, Resources.Load<PlayerMovementSettings>("DefaultPlayerMovement"));

        MirrorPlayer2D mirror = root.AddComponent<MirrorPlayer2D>();
        mirror.Configure(controller);

        Transform gravityPickup = CreateVisual(root.transform, "Zero Gravity Pickup",
            new Vector2(-6.2f, -3.35f), new Vector2(0.48f, 0.48f),
            new Color(0.75f, 0.3f, 1f, 1f), 9);
        CircleCollider2D pickupCollider = gravityPickup.gameObject.AddComponent<CircleCollider2D>();
        pickupCollider.isTrigger = true;
        GravityDisablePickup2D pickup = gravityPickup.gameObject.AddComponent<GravityDisablePickup2D>();
        pickup.Configure(mirror);
    }

    private static Transform CreatePlayerVisual(Transform parent)
    {
        GameObject visual = new("Player Visual");
        visual.transform.SetParent(parent, false);

        CreateVisual(visual.transform, "Body", Vector2.zero, new Vector2(0.85f, 1.45f),
            new Color(0.15f, 0.75f, 1f, 1f), 10);
        CreateVisual(visual.transform, "Face", new Vector2(0.27f, 0.16f), new Vector2(0.16f, 0.16f),
            new Color(0.03f, 0.08f, 0.14f, 1f), 11);
        CreateVisual(visual.transform, "Foot", new Vector2(0.22f, -0.62f), new Vector2(0.38f, 0.13f),
            new Color(0.04f, 0.28f, 0.42f, 1f), 11);

        return visual.transform;
    }

    private static void CreatePlatform(Transform parent, string name, Vector2 position, Vector2 size)
    {
        Transform platform = CreateVisual(parent, name, position, size,
            new Color(0.18f, 0.25f, 0.35f, 1f), 2);
        platform.gameObject.AddComponent<BoxCollider2D>();
    }

    private static Transform CreateVisual(Transform parent, string name, Vector2 position,
        Vector2 size, Color color, int sortingOrder)
    {
        GameObject gameObject = new(name);
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = position;
        gameObject.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetRuntimeSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return gameObject.transform;
    }

    private static Sprite GetRuntimeSprite()
    {
        if (runtimeSprite != null)
            return runtimeSprite;

        Texture2D texture = Texture2D.whiteTexture;
        runtimeSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), texture.width);
        runtimeSprite.name = "Runtime White Sprite";
        return runtimeSprite;
    }

    private void OnGUI()
    {
        Font localizedFont = LocalizedFontProvider.GetFont();
        if (localizedFont == null) return;
        AccessibilityPreferences preferences = AccessibilityPreferencesService.Instance.Current;
        float textScale = preferences.TextScaleMultiplier;

        GUIStyle title = new(GUI.skin.label)
        {
            font = localizedFont,
            fontSize = Mathf.RoundToInt(22f * textScale),
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = Color.white }
        };
        GUIStyle help = new(GUI.skin.label)
        {
            font = localizedFont,
            fontSize = Mathf.RoundToInt(16f * textScale),
            wordWrap = true,
            normal = { textColor = preferences.HighContrast
                ? Color.white
                : new Color(0.82f, 0.88f, 0.96f) }
        };

        Rect safe = Screen.safeArea;
        float left = safe.xMin + 20f;
        float top = Screen.height - safe.yMax + 16f;
        float width = Mathf.Max(1f, safe.width - 40f);
        string[] lines =
        {
            LocalizationService.Get("prototype.title"),
            LocalizationService.Get("prototype.move_help"),
            LocalizationService.Get("prototype.mirror_help"),
            LocalizationService.Get("prototype.pickup_help")
        };

        float[] heights =
        {
            title.CalcHeight(new GUIContent(lines[0]), width),
            help.CalcHeight(new GUIContent(lines[1]), width),
            help.CalcHeight(new GUIContent(lines[2]), width),
            help.CalcHeight(new GUIContent(lines[3]), width)
        };
        float totalHeight = heights[0] + heights[1] + heights[2] + heights[3] + 18f;
        if (preferences.HighContrast)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, .9f);
            GUI.DrawTexture(new Rect(left - 8f, top - 6f, width + 16f, totalHeight + 12f),
                Texture2D.whiteTexture);
            GUI.color = previous;
        }

        GUI.Label(new Rect(left, top, width, heights[0]), lines[0], title);
        top += heights[0] + 6f;
        for (int i = 1; i < lines.Length; i++)
        {
            GUI.Label(new Rect(left, top, width, heights[i]), lines[i], help);
            top += heights[i] + 4f;
        }
    }
}
