using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public sealed class Snow002AssetTests
{
    private const string ScenePath = "Assets/Scenes/Levels/Snow/Snow_002.unity";
    private const string TilePath = "Assets/Tiles/Snow/FrozenGroundSnowBlock.asset";
    private const int IceMinX = -12;
    private const int IceMaxX = 11;
    private const int IceY = -3;

    [Test]
    public void SceneContainsOneContinuousFrozenGroundTilemap()
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            Tilemap frozenGround = ComponentsInScene<Tilemap>(scene).Single(map => map.name == "FrozenGround");
            Tile expectedTile = AssetDatabase.LoadAssetAtPath<Tile>(TilePath);
            Assert.That(expectedTile, Is.Not.Null);

            int tileCount = 0;
            foreach (Vector3Int position in frozenGround.cellBounds.allPositionsWithin)
            {
                if (!frozenGround.HasTile(position)) continue;
                tileCount++;
                Assert.That(position.y, Is.EqualTo(IceY));
                Assert.That(position.x, Is.InRange(IceMinX, IceMaxX));
                Assert.That(frozenGround.GetTile(position), Is.EqualTo(expectedTile));
            }

            Assert.That(tileCount, Is.EqualTo(IceMaxX - IceMinX + 1));
            for (int x = IceMinX; x <= IceMaxX; x++)
                Assert.That(frozenGround.GetTile(new Vector3Int(x, IceY, 0)), Is.EqualTo(expectedTile),
                    $"Missing FrozenGround Tile at x={x}.");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void FrozenGroundHasRequiredCollisionSemanticAndMirrorPlacementComponents()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            Tilemap frozenGround = ComponentsInScene<Tilemap>(scene).Single(map => map.name == "FrozenGround");
            Rigidbody2D body = frozenGround.GetComponent<Rigidbody2D>();
            TilemapCollider2D tilemapCollider = frozenGround.GetComponent<TilemapCollider2D>();
            CompositeCollider2D composite = frozenGround.GetComponent<CompositeCollider2D>();
            SurfaceSemantic2D semantic = frozenGround.GetComponent<SurfaceSemantic2D>();
            MirrorSurface2D mirrorSurface = frozenGround.GetComponent<MirrorSurface2D>();

            Assert.That(body, Is.Not.Null);
            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Static));
            Assert.That(tilemapCollider, Is.Not.Null);
            Assert.That(tilemapCollider.compositeOperation, Is.EqualTo(Collider2D.CompositeOperation.Merge));
            Assert.That(composite, Is.Not.Null);
            Assert.That(composite.pathCount, Is.GreaterThan(0));
            Assert.That(semantic, Is.Not.Null);
            Assert.That(semantic.Type, Is.EqualTo(SurfaceSemantic2D.SurfaceType.FrozenGround));
            Assert.That(semantic.IsStatic, Is.True);
            Assert.That(semantic.IsSafe, Is.True);
            Assert.That(mirrorSurface, Is.Not.Null);
            Assert.That(mirrorSurface.kind, Is.EqualTo(MirrorSurface2D.SurfaceKind.Ground));
            Assert.That(mirrorSurface.safe, Is.True);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void PrototypeUsesRuntimePlayerSpawnResetAndBuildRegistration()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            Assert.That(ComponentsInScene<PlayerController2D>(scene), Is.Empty);
            Assert.That(ComponentsInScene<MirrorPlayer2D>(scene), Is.Empty);
            Assert.That(ComponentsInScene<RoomPlayerSpawner2D>(scene), Has.Length.EqualTo(1));
            Assert.That(ComponentsInScene<RoomEntrance2D>(scene).Count(entrance => entrance.IsDefault), Is.EqualTo(1));
            Assert.That(ComponentsInScene<RoomResetSystem>(scene).Count(), Is.EqualTo(1));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        Assert.That(EditorBuildSettings.scenes.Any(entry => entry.enabled && entry.path == ScenePath), Is.True);
    }

    private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
}
