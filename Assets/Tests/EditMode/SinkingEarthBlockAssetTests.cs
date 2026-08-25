using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SinkingEarthBlockAssetTests
{
    private const string PrefabPath = "Assets/Prefabs/Gameplay/Earth/SinkingEarthBlock2D.prefab";

    [Test]
    public void PrefabHasSafeDynamicSurfaceAndResettableMotion()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.transform.position, Is.EqualTo(Vector3.zero));
        Assert.That(prefab.transform.Find("Visual"), Is.Not.Null);
        Assert.That(prefab.transform.Find("TopMarker"), Is.Not.Null);

        SinkingEarthBlock2D block = prefab.GetComponent<SinkingEarthBlock2D>();
        Assert.That(block, Is.Not.Null);
        Assert.That(block, Is.InstanceOf<ISurfaceMotionProvider2D>());
        Assert.That(block, Is.InstanceOf<IRoomResettable>());
        Assert.That(block.SinkDistance, Is.GreaterThan(0f));
        Assert.That(block.SinkSpeed, Is.GreaterThan(0f));
        Assert.That(block.RecoverSpeed, Is.GreaterThan(0f));
        Assert.That(block.WeightForFullSink, Is.GreaterThan(0f));

        Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
        Assert.That(body, Is.Not.Null);
        Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
        Assert.That(body.interpolation, Is.EqualTo(RigidbodyInterpolation2D.Interpolate));
        Assert.That(body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode2D.Continuous));

        BoxCollider2D collider = prefab.GetComponent<BoxCollider2D>();
        Assert.That(collider, Is.Not.Null);
        Assert.That(collider.isTrigger, Is.False);

        SurfaceSemantic2D semantic = prefab.GetComponent<SurfaceSemantic2D>();
        Assert.That(semantic, Is.Not.Null);
        Assert.That(semantic.Type, Is.EqualTo(SurfaceSemantic2D.SurfaceType.DynamicSurface));
        Assert.That(semantic.IsStatic, Is.False);
        Assert.That(semantic.IsSafe, Is.True);
        Assert.That(prefab.GetComponent<MirrorSurface2D>(), Is.Null);
    }
}
