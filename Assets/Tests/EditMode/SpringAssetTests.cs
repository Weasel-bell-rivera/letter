using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SpringAssetTests
{
    private const string PrefabPath = "Assets/Prefabs/Gameplay/Devices/Spring2D.prefab";
    private const string ExtendedSpritePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Spring/spring_out.png";
    private const string CompressedSpritePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Double/Spring/spring.png";

    [Test]
    public void PrefabHasRequiredGameplayAndVisualStructure()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.transform.position, Is.EqualTo(Vector3.zero));
        Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));

        Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
        BoxCollider2D collider = prefab.GetComponent<BoxCollider2D>();
        SurfaceSemantic2D semantic = prefab.GetComponent<SurfaceSemantic2D>();
        Spring2D spring = prefab.GetComponent<Spring2D>();
        SpringVisual2D visual = prefab.GetComponentInChildren<SpringVisual2D>(true);

        Assert.That(body, Is.Not.Null);
        Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Static));
        Assert.That(collider, Is.Not.Null);
        Assert.That(collider.isTrigger, Is.False);
        Assert.That(collider.size, Is.EqualTo(Vector2.one));
        Assert.That(collider.offset, Is.EqualTo(new Vector2(0f, .5f)));
        Assert.That(semantic, Is.Not.Null);
        Assert.That(semantic.Type, Is.EqualTo(SurfaceSemantic2D.SurfaceType.Spring));
        Assert.That(semantic.IsStatic, Is.True);
        Assert.That(semantic.IsSafe, Is.True);
        Assert.That(prefab.GetComponent<MirrorSurface2D>(), Is.Null);

        Assert.That(spring, Is.Not.Null);
        Assert.That(spring.TopLaunchHeight, Is.EqualTo(Spring2D.DefaultTopLaunchHeight));
        Assert.That(spring.SideLaunchSpeed, Is.EqualTo(Spring2D.DefaultSideLaunchSpeed));
        Assert.That(spring.MinimumApproachSpeed, Is.EqualTo(Spring2D.DefaultMinimumApproachSpeed));

        Assert.That(visual, Is.Not.Null);
        Assert.That(visual.transform.localPosition, Is.EqualTo(new Vector3(0f, .5f, 0f)));
        Assert.That(visual.Renderer, Is.Not.Null);
        Assert.That(visual.Renderer.sprite, Is.SameAs(visual.ExtendedSprite));
        Assert.That(AssetDatabase.GetAssetPath(visual.ExtendedSprite), Is.EqualTo(ExtendedSpritePath));
        Assert.That(AssetDatabase.GetAssetPath(visual.CompressedSprite), Is.EqualTo(CompressedSpritePath));
        Assert.That(visual.CompressionDuration, Is.EqualTo(SpringVisual2D.DefaultCompressionDuration));
    }

    [TestCase(ExtendedSpritePath)]
    [TestCase(CompressedSpritePath)]
    public void SpriteImportSettingsAreStable(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Assert.That(importer, Is.Not.Null);
        Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(128f));
        Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
        Assert.That(importer.mipmapEnabled, Is.False);
        Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));

        TextureImporterSettings settings = new();
        importer.ReadTextureSettings(settings);
        Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
    }

    [Test]
    public void BothCharacterControllersUseTheSharedBounceReceiverContract()
    {
        Assert.That(typeof(ISpringBounceReceiver2D).IsAssignableFrom(typeof(PlayerController2D)), Is.True);
        Assert.That(typeof(ISpringBounceReceiver2D).IsAssignableFrom(typeof(MirrorCloneController2D)), Is.True);
    }
}
