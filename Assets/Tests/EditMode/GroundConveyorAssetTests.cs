using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class GroundConveyorAssetTests
{
    private const string PrefabPath = "Assets/Prefabs/Gameplay/Surfaces/GroundConveyor2D.prefab";

    [Test]
    public void PrefabHasRequiredGameplayAndVisualStructure()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.transform.position, Is.EqualTo(Vector3.zero));
        Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
        BoxCollider2D collider = prefab.GetComponent<BoxCollider2D>();
        SurfaceSemantic2D semantic = prefab.GetComponent<SurfaceSemantic2D>();
        GroundConveyor2D conveyor = prefab.GetComponent<GroundConveyor2D>();
        ConveyorVisual2D visual = prefab.GetComponentInChildren<ConveyorVisual2D>(true);

        Assert.That(body, Is.Not.Null);
        Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Static));
        Assert.That(collider, Is.Not.Null);
        Assert.That(collider.isTrigger, Is.False);
        Assert.That(semantic, Is.Not.Null);
        Assert.That(semantic.Type, Is.EqualTo(SurfaceSemantic2D.SurfaceType.Conveyor));
        Assert.That(semantic.IsStatic, Is.True);
        Assert.That(semantic.IsSafe, Is.True);
        Assert.That(conveyor, Is.Not.Null);
        Assert.That(conveyor.Direction, Is.EqualTo(GroundConveyor2D.BeltDirection.Right));
        Assert.That(conveyor.Speed, Is.EqualTo(GroundConveyor2D.DefaultSpeed));
        Assert.That(conveyor.InitiallyActive, Is.True);
        Assert.That(prefab.GetComponent<MirrorSurface2D>(), Is.Null);

        Assert.That(visual, Is.Not.Null);
        Assert.That(prefab.transform.Find("Visual/BeltRenderer")?.GetComponent<SpriteRenderer>(), Is.Not.Null);
        Assert.That(prefab.transform.Find("Visual/DirectionIndicator/Marker/UpperStroke")
            ?.GetComponent<SpriteRenderer>(), Is.Not.Null);
        Assert.That(prefab.transform.Find("Visual/DirectionIndicator/Marker/LowerStroke")
            ?.GetComponent<SpriteRenderer>(), Is.Not.Null);
    }

    [Test]
    public void DefaultSpeedStaysBelowPlayerMaximumSpeed()
    {
        PlayerMovementSettings settings = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(
            "Assets/Settings/Player/DefaultPlayerMovement.asset");

        Assert.That(settings, Is.Not.Null);
        Assert.That(GroundConveyor2D.MaximumSpeed, Is.LessThan(settings.maxSpeed));
        Assert.That(GroundConveyor2D.DefaultSpeed, Is.InRange(
            GroundConveyor2D.MinimumSpeed, GroundConveyor2D.MaximumSpeed));
    }
}
