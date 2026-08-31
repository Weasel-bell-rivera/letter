using System;
using UnityEngine;

public sealed class MirrorHarness2D : IDisposable
{
    private readonly GameObject root;
    public GameObject Root => root;
    public PlayerController2D Player { get; }
    public MirrorPlayer2D Mirror { get; }
    public Rigidbody2D PlayerBody { get; }
    public BoxCollider2D PlayerCollider { get; }

    private MirrorHarness2D(bool initiallyUnlocked)
    {
        root = new GameObject("MirrorHarness");

        GameObject player = new("Harness Player");
        player.transform.SetParent(root.transform);
        PlayerCollider = player.GetComponent<BoxCollider2D>();
        if (PlayerCollider == null) PlayerCollider = player.AddComponent<BoxCollider2D>();
        PlayerBody = player.GetComponent<Rigidbody2D>();
        if (PlayerBody == null) PlayerBody = player.AddComponent<Rigidbody2D>();
        Player = player.AddComponent<PlayerController2D>();
        PlayerBody = player.GetComponent<Rigidbody2D>();
        PlayerCollider = player.GetComponent<BoxCollider2D>();
        PlayerCollider.size = new Vector2(.8f, 1.8f);
        Mirror = player.AddComponent<MirrorPlayer2D>();
        Mirror.Configure(Player);
        Mirror.SetInitiallyUnlocked(initiallyUnlocked);

        Player.transform.position = Vector3.zero;
    }

    public static MirrorHarness2D Create(bool initiallyUnlocked = true)
        => new MirrorHarness2D(initiallyUnlocked);

    public static MirrorHarness2D CreateUnlocked() => Create(true);

    public static MirrorHarness2D CreateLocked() => Create(false);

    public void Dispose()
    {
        if (Mirror != null)
        {
            if (Mirror.Clone != null) UnityEngine.Object.DestroyImmediate(Mirror.Clone.gameObject);
            if (Mirror.PlacedMirror != null) UnityEngine.Object.DestroyImmediate(Mirror.PlacedMirror);
        }
        if (root != null) UnityEngine.Object.DestroyImmediate(root);
    }

    public void SyncPhysics()
    {
        Physics2D.SyncTransforms();
    }

    public void SetPlayerPosition(Vector2 worldPosition)
    {
        Player.transform.position = worldPosition;
        SyncPhysics();
    }

    public void SetPlayerFacingRight()
    {
        Player.SetFacing(true);
    }

    public void SetPlayerFacingLeft()
    {
        Player.SetFacing(false);
    }

    public bool Place()
    {
        SyncPhysics();
        return Mirror.TryPlace();
    }

    public void Recall()
    {
        Mirror.RecallImmediate();
    }

    public MirrorSurface2D AddGroundSurface(Vector2 position, Vector2 size, bool safe = true)
    {
        GameObject ground = new("Harness Ground");
        ground.transform.SetParent(root.transform);
        ground.transform.position = position;

        BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
        collider.size = size;

        MirrorSurface2D surface = ground.AddComponent<MirrorSurface2D>();
        surface.kind = MirrorSurface2D.SurfaceKind.Ground;
        surface.safe = safe;
        return surface;
    }

    public MirrorSurface2D AddDefaultGround(float y = -1.4f, float width = 8f, float height = 1f, bool safe = true)
    {
        return AddGroundSurface(new Vector2(0f, y), new Vector2(width, height), safe);
    }

    public MirrorSurface2D AddSpecialWallNearPlayer(float sideSign, float extraDistance = 0.2f, float height = 1.8f, bool safe = true)
    {
        Bounds playerBounds = PlayerCollider.bounds;
        Vector2 wallCenter = new Vector2(
            playerBounds.center.x + sideSign * (playerBounds.extents.x + extraDistance),
            playerBounds.center.y
        );

        GameObject wall = new("Harness SpecialWall");
        wall.transform.SetParent(root.transform);
        wall.transform.position = wallCenter;

        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(.2f, height);

        MirrorSurface2D surface = wall.AddComponent<MirrorSurface2D>();
        surface.kind = MirrorSurface2D.SurfaceKind.SpecialWall;
        surface.safe = safe;
        return surface;
    }
}
