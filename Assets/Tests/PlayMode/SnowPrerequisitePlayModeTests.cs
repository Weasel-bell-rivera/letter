using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SnowPrerequisitePlayModeTests
{
    private GameObject root;

    [SetUp]
    public void SetUp() => root = new GameObject("Snow Prerequisite Tests");

    [TearDown]
    public void TearDown()
    {
        if (root != null) Object.DestroyImmediate(root);
    }

    [UnityTest]
    public IEnumerator EnemyPatrolIsBoundedDeterministicAndResettable()
    {
        CreateGround("Terrain", new Vector2(0f, -.5f), new Vector2(12f, 1f),
            SurfaceSemantic2D.SurfaceType.StaticSolid);
        FreezablePatrolEnemy2D enemy = CreateEnemy(new Vector2(0f, .52f), -1f, 1f, 2f, .08f);
        Vector2 initial = enemy.transform.position;

        int fixedSteps = 0;
        while (enemy.FacingRight && fixedSteps++ < 80)
            yield return new WaitForFixedUpdate();

        Assert.That(enemy.State, Is.EqualTo(FreezablePatrolEnemy2D.EnemyState.Active));
        Assert.That(enemy.transform.position.x, Is.InRange(initial.x - 1.05f, initial.x + 1.05f));
        Assert.That(enemy.FacingRight, Is.False,
            "Enemy should reach the right endpoint and turn within the bounded fixed-step budget.");

        enemy.ResetRoomState();
        Assert.That(Vector2.Distance(enemy.transform.position, initial), Is.LessThan(.001f));
        Assert.That(enemy.FacingRight, Is.True);
        Assert.That(enemy.IsDamaging, Is.True);
    }

    [UnityTest]
    public IEnumerator EnemyFreezesOnlyFromStableFootContactAndResetRestoresActiveState()
    {
        CreateGround("Frozen Surface", new Vector2(0f, -.5f), new Vector2(8f, 1f),
            SurfaceSemantic2D.SurfaceType.FrozenGround);
        FreezablePatrolEnemy2D enemy = CreateEnemy(new Vector2(0f, .52f), -2f, 2f, 1f, .1f);
        int freezeEvents = 0;
        enemy.Frozen += () => freezeEvents++;

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.That(enemy.IsFrozen, Is.True);
        Assert.That(enemy.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
        Assert.That(enemy.IsDamaging, Is.False);
        Assert.That(enemy.transform.Find("BodyCollider").GetComponent<SurfaceSemantic2D>().IsSafe, Is.True);
        Assert.That(enemy.transform.Find("BodyCollider").GetComponent<BoxCollider2D>().enabled, Is.True);
        Assert.That(freezeEvents, Is.EqualTo(1));
        yield return new WaitForFixedUpdate();
        Assert.That(freezeEvents, Is.EqualTo(1), "FrozenGround Tile seams must not cause repeated freezing.");

        enemy.ResetRoomState();
        Assert.That(enemy.State, Is.EqualTo(FreezablePatrolEnemy2D.EnemyState.Active));
        Assert.That(enemy.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));
        Assert.That(enemy.IsDamaging, Is.True);
        Assert.That(enemy.transform.Find("BodyCollider").GetComponent<SurfaceSemantic2D>().IsSafe, Is.False);
    }

    [UnityTest]
    public IEnumerator FrozenSemanticOnSideWallDoesNotFreezeEnemy()
    {
        CreateGround("Terrain", new Vector2(0f, -.5f), new Vector2(8f, 1f),
            SurfaceSemantic2D.SurfaceType.StaticSolid);
        CreateGround("Frozen Wall", new Vector2(1.1f, 1f), new Vector2(.5f, 4f),
            SurfaceSemantic2D.SurfaceType.FrozenGround);
        FreezablePatrolEnemy2D enemy = CreateEnemy(new Vector2(0f, .52f), -1f, 1f, .5f, .1f);

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.That(enemy.State, Is.EqualTo(FreezablePatrolEnemy2D.EnemyState.Active));
    }

    [UnityTest]
    public IEnumerator RotatedPressurePlateUsesGenericDoorBindingAndResets()
    {
        GameObject plateObject = new("Wall Plate");
        plateObject.transform.SetParent(root.transform);
        plateObject.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        BoxCollider2D trigger = plateObject.AddComponent<BoxCollider2D>();
        trigger.size = new Vector2(1.25f, .3f);
        GameObject visualObject = new("Visual");
        visualObject.transform.SetParent(plateObject.transform, false);
        SpriteRenderer visual = visualObject.AddComponent<SpriteRenderer>();
        PressurePlate2D plate = plateObject.AddComponent<PressurePlate2D>();
        plate.ConfigureVisual(visual);

        GameObject doorObject = new("Door");
        doorObject.transform.SetParent(root.transform);
        doorObject.transform.position = Vector3.right * 5f;
        doorObject.AddComponent<BoxCollider2D>().size = new Vector2(.75f, 3f);
        Door2D door = doorObject.AddComponent<Door2D>();
        door.Configure(false);
        door.ConfigureControlSource(plate);

        GameObject clone = CreateClone(plateObject.transform.position);
        Vector3 restScale = visual.transform.localScale;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.That(plate.IsActive, Is.True);
        Assert.That(door.IsOpen, Is.True);
        Assert.That(visual.transform.localScale.y, Is.LessThan(restScale.y));

        clone.transform.position = Vector3.right * 20f;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.That(plate.IsActive, Is.False);
        Assert.That(door.IsOpen, Is.False);

        plate.ResetRoomState();
        door.ResetRoomState();
        Assert.That(door.IsOpen, Is.False);
    }

    [UnityTest]
    public IEnumerator DoorWaitsForActiveAndFrozenEnemyToLeaveClosingPath()
    {
        CreateGround("Terrain", new Vector2(0f, -.5f), new Vector2(14f, 1f),
            SurfaceSemantic2D.SurfaceType.StaticSolid);
        FreezablePatrolEnemy2D enemy = CreateEnemy(new Vector2(0f, .52f), -5f, 5f, .1f, .1f);
        Door2D door = CreateDoor(new Vector2(0f, 1.5f));
        door.SetOpen(true);
        Physics2D.SyncTransforms();
        door.SetOpen(false);
        Assert.That(door.IsWaitingToClose, Is.True);

        enemy.transform.position = new Vector2(3f, .52f);
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        Assert.That(door.IsOpen, Is.False);

        Object.DestroyImmediate(root.transform.Find("Terrain").gameObject);
        CreateGround("Frozen Terrain", new Vector2(0f, -.5f), new Vector2(14f, 1f),
            SurfaceSemantic2D.SurfaceType.FrozenGround);
        enemy.transform.position = new Vector2(3f, .52f);
        enemy.GetComponent<Rigidbody2D>().position = enemy.transform.position;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.That(enemy.IsFrozen, Is.True);

        door.transform.position = new Vector2(3f, 1.5f);
        door.SetOpen(true);
        Physics2D.SyncTransforms();
        door.SetOpen(false);
        Assert.That(door.IsWaitingToClose, Is.True, "Frozen enemies must receive the same anti-crush protection.");
    }

    private FreezablePatrolEnemy2D CreateEnemy(Vector2 position, float left, float right, float speed, float wait)
    {
        GameObject enemyObject = new("Enemy");
        enemyObject.transform.SetParent(root.transform);
        enemyObject.transform.position = position;
        Rigidbody2D body = enemyObject.AddComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.gravityScale = 1f;

        GameObject solidObject = new("BodyCollider");
        solidObject.transform.SetParent(enemyObject.transform, false);
        BoxCollider2D solid = solidObject.AddComponent<BoxCollider2D>();
        solid.size = new Vector2(1.2f, 1f);
        SurfaceSemantic2D surface = solidObject.AddComponent<SurfaceSemantic2D>();
        surface.Configure(SurfaceSemantic2D.SurfaceType.DynamicSurface, false, false);

        GameObject damageObject = new("DamageTrigger");
        damageObject.transform.SetParent(enemyObject.transform, false);
        BoxCollider2D damageCollider = damageObject.AddComponent<BoxCollider2D>();
        damageCollider.size = new Vector2(1.34f, 1.08f);
        damageCollider.isTrigger = true;
        EnemyDamageTrigger2D damage = damageObject.AddComponent<EnemyDamageTrigger2D>();

        Transform groundProbe = Probe(enemyObject.transform, "GroundProbe");
        Transform surfaceProbe = Probe(enemyObject.transform, "SurfaceProbe");
        FreezablePatrolEnemy2D enemy = enemyObject.AddComponent<FreezablePatrolEnemy2D>();
        damage.Configure(enemy);
        enemy.ConfigurePrefabReferences(solid, damage, groundProbe, surfaceProbe, null, null, null);
        enemy.ConfigurePatrol(left, right, speed, wait, true);
        return enemy;
    }

    private Transform Probe(Transform parent, string name)
    {
        GameObject probe = new(name);
        probe.transform.SetParent(parent, false);
        probe.transform.localPosition = new Vector3(0f, -.45f, 0f);
        return probe.transform;
    }

    private GameObject CreateGround(string name, Vector2 position, Vector2 size, SurfaceSemantic2D.SurfaceType type)
    {
        GameObject ground = new(name);
        ground.transform.SetParent(root.transform);
        ground.transform.position = position;
        ground.AddComponent<BoxCollider2D>().size = size;
        SurfaceSemantic2D semantic = ground.AddComponent<SurfaceSemantic2D>();
        semantic.Configure(type, true, true);
        return ground;
    }

    private Door2D CreateDoor(Vector2 position)
    {
        GameObject doorObject = new("Door");
        doorObject.transform.SetParent(root.transform);
        doorObject.transform.position = position;
        doorObject.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 3f);
        Door2D door = doorObject.AddComponent<Door2D>();
        door.Configure(false);
        return door;
    }

    private GameObject CreateClone(Vector2 position)
    {
        GameObject clone = new("MirrorClone");
        clone.transform.SetParent(root.transform);
        clone.transform.position = position;
        clone.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.8f);
        Rigidbody2D body = clone.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        clone.AddComponent<MirrorCloneController2D>();
        return clone;
    }
}
