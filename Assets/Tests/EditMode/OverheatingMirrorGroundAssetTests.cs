using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class OverheatingMirrorGroundAssetTests
{
    private const string PrefabPath =
        "Assets/Prefabs/Gameplay/Surfaces/OverheatingMirrorGround2D.prefab";

    [Test]
    public void PrefabIsSafeStaticMirrorSurfaceWithApprovedDuration()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            OverheatingMirrorGround2D overheat = root.GetComponent<OverheatingMirrorGround2D>();
            MirrorSurface2D mirrorSurface = root.GetComponent<MirrorSurface2D>();
            SurfaceSemantic2D semantic = root.GetComponent<SurfaceSemantic2D>();
            BoxCollider2D solid = root.GetComponent<BoxCollider2D>();
            Rigidbody2D body = root.GetComponent<Rigidbody2D>();

            Assert.That(overheat, Is.Not.Null);
            Assert.That(overheat.OverheatSeconds,
                Is.EqualTo(OverheatingMirrorGround2D.DefaultOverheatSeconds));
            Assert.That(mirrorSurface, Is.Not.Null);
            Assert.That(mirrorSurface.kind, Is.EqualTo(MirrorSurface2D.SurfaceKind.Ground));
            Assert.That(mirrorSurface.safe, Is.True);
            Assert.That(semantic, Is.Not.Null);
            Assert.That(semantic.Type, Is.EqualTo(SurfaceSemantic2D.SurfaceType.OverheatingGround));
            Assert.That(semantic.IsStatic, Is.True);
            Assert.That(semantic.IsSafe, Is.True);
            Assert.That(solid, Is.Not.Null.And.Property("isTrigger").False);
            Assert.That(body, Is.Not.Null);
            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Static));
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
