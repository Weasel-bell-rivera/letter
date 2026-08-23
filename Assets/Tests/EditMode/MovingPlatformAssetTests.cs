using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MovingPlatformAssetTests
{
    private const string PrefabPath = "Assets/Prefabs/Gameplay/Platforms/MovingPlatform2D.prefab";
    private const string SpritePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_stone_cloud_middle.png";

    [Test]
    public void PrefabHasRequiredReusableStructure()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.transform.position, Is.EqualTo(Vector3.zero));
        Assert.That(prefab.transform.Find("Visual"), Is.Not.Null);
        MovingPlatform2D platform = prefab.GetComponent<MovingPlatform2D>();
        Assert.That(platform, Is.Not.Null);
        Assert.That(platform, Is.InstanceOf<ISurfaceMotionProvider2D>());
        Assert.That(prefab.GetComponent<BoxCollider2D>(), Is.Not.Null);

        DefaultExecutionOrder executionOrder = (DefaultExecutionOrder)System.Attribute.GetCustomAttribute(
            typeof(MovingPlatform2D), typeof(DefaultExecutionOrder));
        Assert.That(executionOrder, Is.Not.Null);
        Assert.That(executionOrder.order, Is.LessThan(0),
            "MovingPlatform2D must calculate its surface velocity before passenger controllers.");

        Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
        Assert.That(body, Is.Not.Null);
        Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
        Assert.That(body.interpolation, Is.EqualTo(RigidbodyInterpolation2D.Interpolate));
        Assert.That(body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode2D.Continuous));

        SurfaceSemantic2D semantic = prefab.GetComponent<SurfaceSemantic2D>();
        Assert.That(semantic, Is.Not.Null);
        Assert.That(semantic.Type, Is.EqualTo(SurfaceSemantic2D.SurfaceType.DynamicSurface));
        Assert.That(semantic.IsStatic, Is.False);
        Assert.That(semantic.IsSafe, Is.True);
        Assert.That(prefab.GetComponent<MirrorSurface2D>(), Is.Null,
            "A moving platform must not be a mirror placement surface.");

        TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        SpriteRenderer renderer = prefab.transform.Find("Visual").GetComponent<SpriteRenderer>();
        BoxCollider2D collider = prefab.GetComponent<BoxCollider2D>();
        Assert.That(importer, Is.Not.Null);
        TextureImporterSettings importerSettings = new();
        importer.ReadTextureSettings(importerSettings);
        Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
        Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(64f));
        Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
        Assert.That(importerSettings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
        Assert.That(sprite, Is.Not.Null);
        Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(64f, 64f)));
        Assert.That(renderer.sprite, Is.EqualTo(sprite));
        Assert.That(renderer.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
        Assert.That(renderer.size, Is.EqualTo(new Vector2(3f, 1f)));
        Assert.That(collider.size, Is.EqualTo(new Vector2(3f, .8125f)));
        Assert.That(collider.offset.y, Is.EqualTo(.09375f).Within(.0001f));
    }

    [Test]
    public void PrefabUsesConfigurableLocalPathDefaults()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        MovingPlatform2D platform = prefab.GetComponent<MovingPlatform2D>();

        Assert.That(platform.StartOffset, Is.EqualTo(Vector2.zero));
        Assert.That(platform.EndOffset, Is.Not.EqualTo(platform.StartOffset));
        Assert.That(platform.MoveSpeed, Is.GreaterThan(0f));
        Assert.That(platform.EndpointWait, Is.GreaterThanOrEqualTo(0f));
        Assert.That(platform.InitialPhase, Is.InRange(0f, 1f));
    }
}
