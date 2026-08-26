using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public sealed class SnowPrerequisiteAssetTests
{
    private const string TexturePath =
        "Assets/Art/Kenney/NewPlatformerPack/Sprites/Tiles/Default/terrain_snow_block.png";
    private const string TilePath = "Assets/Tiles/Snow/FrozenGroundSnowBlock.asset";
    private const string FrozenGroundMaterialPath = "Assets/Settings/Physics/FrozenGround.physicsMaterial2D";
    private const string EnemyPrefabPath = "Assets/Prefabs/Gameplay/Enemies/FreezablePatrolEnemy2D.prefab";

    [Test]
    public void SnowTextureAndFrozenGroundTileAreImportedForGameplay()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TilePath);

        Assert.That(importer, Is.Not.Null);
        Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
        Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(64f));
        Assert.That(sprite, Is.Not.Null);
        Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(64f, 64f)));
        Assert.That(tile, Is.Not.Null);
        Assert.That(tile.sprite, Is.EqualTo(sprite));
        Assert.That(tile.colliderType, Is.EqualTo(Tile.ColliderType.Grid));

        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(FrozenGroundMaterialPath);
        Assert.That(material, Is.Not.Null);
        Assert.That(material.friction, Is.EqualTo(0f));
        Assert.That(material.bounciness, Is.EqualTo(0f));
    }

    [Test]
    public void FreezableEnemyPrefabHasRequiredReusableStructure()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<Rigidbody2D>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<AudioSource>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<FreezablePatrolEnemy2D>(), Is.Not.Null);
        Assert.That(prefab.transform.Find("Visual/ActiveVisual"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Visual/FrozenVisual"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Visual/FreezeEffect"), Is.Not.Null);
        SpriteRenderer activeRenderer = prefab.transform.Find("Visual/ActiveVisual").GetComponent<SpriteRenderer>();
        SpriteRenderer frozenOverlay = prefab.transform.Find("Visual/FrozenVisual").GetComponent<SpriteRenderer>();
        Assert.That(frozenOverlay.sprite, Is.EqualTo(activeRenderer.sprite),
            "Frozen state must tint the original enemy image instead of replacing it with a color block.");
        Assert.That(frozenOverlay.transform.localScale, Is.EqualTo(activeRenderer.transform.localScale));
        Assert.That(frozenOverlay.color.a, Is.LessThan(1f));
        Assert.That(prefab.transform.Find("BodyCollider")?.GetComponent<BoxCollider2D>(), Is.Not.Null);
        Assert.That(prefab.transform.Find("BodyCollider")?.GetComponent<SurfaceSemantic2D>()?.Type,
            Is.EqualTo(SurfaceSemantic2D.SurfaceType.DynamicSurface));
        Assert.That(prefab.transform.Find("DamageTrigger")?.GetComponent<EnemyDamageTrigger2D>(), Is.Not.Null);
        Assert.That(prefab.transform.Find("GroundProbe"), Is.Not.Null);
        Assert.That(prefab.transform.Find("SurfaceProbe"), Is.Not.Null);
    }

    [Test]
    public void EnemyPrefabUsesLocalPatrolOffsetsInsteadOfRoomCoordinates()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        FreezablePatrolEnemy2D enemy = prefab.GetComponent<FreezablePatrolEnemy2D>();
        Assert.That(prefab.transform.position, Is.EqualTo(Vector3.zero));
        Assert.That(enemy.LeftEndpoint, Is.LessThan(0f));
        Assert.That(enemy.RightEndpoint, Is.GreaterThan(0f));
        Assert.That(enemy.MoveSpeed, Is.GreaterThan(0f));
        Assert.That(enemy.EndpointWait, Is.GreaterThanOrEqualTo(0f));
    }

    [Test]
    public void EveryApprovedSnowRoomUsesTheRegionalPlayerFollowCamera()
    {
        for (int roomId = 1; roomId <= 15; roomId++)
        {
            string scenePath = $"Assets/Scenes/Levels/Snow/Snow_{roomId:000}.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                Camera camera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Single();
                CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
                RoomResetSystem reset = roots.SelectMany(root =>
                    root.GetComponentsInChildren<RoomResetSystem>(true)).Single();
                Rect expectedBounds = roomId == 1
                    ? new Rect(-14f, -3f, 29f, 14f)
                    : roomId == 2
                        ? new Rect(-12f, -7f, 24f, 14f)
                        : roomId is 7 or 8
                            ? new Rect(-20f, -7f, 40f, 14f)
                        : new Rect(-13f, -7f, 26f, 14f);

                Assert.That(camera.orthographic, Is.True, $"SNOW_{roomId:000} camera must be orthographic.");
                Assert.That(camera.orthographicSize, Is.EqualTo(7f).Within(.001f));
                Assert.That(follow, Is.Not.Null, $"SNOW_{roomId:000} must follow the spawned Player.");
                Assert.That(follow.Target, Is.Null, "The runtime spawner binds the Player after entering the room.");
                Assert.That(follow.FollowsVertical, Is.True);
                Assert.That(follow.SmoothTime, Is.EqualTo(.15f).Within(.001f));
                Assert.That(follow.UsesRoomBounds, Is.True);
                Assert.That(follow.RoomBounds, Is.EqualTo(expectedBounds));
                if (roomId == 7)
                {
                    Assert.That(follow.AlignsEntryFramingToBounds, Is.True);
                    Assert.That(follow.EntryFramingBounds, Is.EqualTo(new Rect(-13f, -7f, 26f, 14f)),
                        "SNOW_007 must align its live entrance view with the physical walls.");
                }

                SerializedObject serializedReset = new(reset);
                Assert.That(serializedReset.FindProperty("cameraFollow").objectReferenceValue,
                    Is.SameAs(follow), $"SNOW_{roomId:000} reset must restore its follow camera.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
