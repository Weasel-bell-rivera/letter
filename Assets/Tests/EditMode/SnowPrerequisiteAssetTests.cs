using NUnit.Framework;
using UnityEditor;
using UnityEngine;
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
}
