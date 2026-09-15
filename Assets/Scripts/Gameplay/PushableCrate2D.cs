using System.Collections.Generic;
using UnityEngine;

/// <summary>Input-aware horizontal braking; character motion and the 2D solver supply the push.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SurfaceSemantic2D))]
[DefaultExecutionOrder(-150)]
public sealed class PushableCrate2D : MonoBehaviour, IRoomResettable, IOrderedRoomResettable,
    ISurfaceMotionProvider2D
{
    private Rigidbody2D body;
    private BoxCollider2D solid;
    private Vector2 initialPosition;
    private float initialRotation;
    private bool initialized;
    private readonly List<RaycastHit2D> supportHits = new();
    private readonly List<RaycastHit2D> pushHits = new();

    // Restore obstacles before pressure plates reconcile occupancy (order 0).
    public int ResetOrder => -90;
    public BoxCollider2D SolidCollider => solid != null ? solid : GetComponent<BoxCollider2D>();

    private void Awake() => Initialize();

    private void FixedUpdate()
    {
        Initialize();
        if (!body.simulated || !solid.enabled) return;
        SetHorizontalBrake(!HasPusher(Vector2.left) && !HasPusher(Vector2.right));
    }

    private bool HasPusher(Vector2 side)
    {
        ContactFilter2D filter = new();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = false;
        pushHits.Clear();
        solid.Cast(side, filter, pushHits, .035f);
        foreach (RaycastHit2D hit in pushHits)
        {
            if (hit.collider == null || hit.rigidbody == null || hit.rigidbody == body ||
                !hit.rigidbody.simulated || Vector2.Dot(hit.normal, side) > -.65f ||
                Physics2D.GetIgnoreCollision(solid, hit.collider)) continue;
            PlayerController2D player = hit.rigidbody.GetComponent<PlayerController2D>();
            MirrorCloneController2D clone = hit.rigidbody.GetComponent<MirrorCloneController2D>();
            Vector2 input;
            Collider2D actor;
            if (player != null && player.isActiveAndEnabled && player.ControlEnabled && !player.IsClimbing)
            {
                input = Vector2.right * player.HorizontalInput;
                actor = player.FreezingCollider;
            }
            else if (clone != null && clone.isActiveAndEnabled && !clone.IsClimbing)
            {
                input = clone.MoveAxis * clone.MovementInput;
                actor = clone.FreezingCollider;
            }
            else continue;
            if (actor != hit.collider || Vector2.Dot(input, -side) <= .01f) continue;
            // A character supported by the top is not a side pusher, even during overlap recovery.
            float separation = Vector2.Dot((Vector2)actor.bounds.center - (Vector2)solid.bounds.center, side);
            if (separation > solid.bounds.extents.x) return true;
        }
        return false;
    }

    private void SetHorizontalBrake(bool stopped)
    {
        RigidbodyConstraints2D constraints = RigidbodyConstraints2D.FreezeRotation |
            (stopped ? RigidbodyConstraints2D.FreezePositionX : RigidbodyConstraints2D.None);
        if (body.constraints != constraints) body.constraints = constraints;
        if (stopped && body.linearVelocity.x != 0f)
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }

    private void Initialize()
    {
        if (initialized) return;
        body = GetComponent<Rigidbody2D>();
        solid = GetComponent<BoxCollider2D>();
        // The authored Transform is authoritative before the first physics synchronization.
        initialPosition = transform.position;
        initialRotation = transform.eulerAngles.z;
        body.bodyType = RigidbodyType2D.Dynamic;
        SetHorizontalBrake(true);
        solid.isTrigger = false;
        GetComponent<SurfaceSemantic2D>().Configure(SurfaceSemantic2D.SurfaceType.DynamicSurface, false, true);
        initialized = true;
    }

    public bool HasPressureSupport(Vector2 plateUp)
    {
        Initialize();
        if (!isActiveAndEnabled || !body.simulated || !solid.enabled ||
            Vector2.Dot(plateUp.normalized, Vector2.up) < .99f) return false;

        // Require a real surface beneath the box; overlapping a trigger in flight is insufficient.
        ContactFilter2D filter = new();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = false;
        supportHits.Clear();
        solid.Cast(Vector2.down, filter, supportHits, .03f);
        foreach (RaycastHit2D hit in supportHits)
        {
            if (hit.collider == null || hit.collider.attachedRigidbody == body || hit.normal.y < .65f)
                continue;
            Rigidbody2D supportBody = hit.collider.attachedRigidbody;
            if (supportBody != null && (supportBody.GetComponent<PlayerController2D>() != null ||
                                       supportBody.GetComponent<MirrorCloneController2D>() != null)) continue;
            Vector2 supportVelocity = supportBody != null ? supportBody.GetPointVelocity(hit.point) : Vector2.zero;
            if (Mathf.Abs(body.linearVelocity.y - supportVelocity.y) <= .5f) return true;
        }
        return false;
    }

    public bool TryGetSurfaceVelocity(Vector2 contactPoint, Vector2 supportNormal, out Vector2 velocity)
    {
        Initialize();
        velocity = isActiveAndEnabled && body.simulated ? body.GetPointVelocity(contactPoint) : Vector2.zero;
        return isActiveAndEnabled && body.simulated && solid.enabled;
    }

    public void ResetRoomState()
    {
        Initialize();
        body.position = initialPosition;
        body.rotation = initialRotation;
        transform.SetPositionAndRotation(new Vector3(initialPosition.x, initialPosition.y, transform.position.z),
            Quaternion.Euler(0f, 0f, initialRotation));
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        supportHits.Clear();
        pushHits.Clear();
        SetHorizontalBrake(true);
        body.WakeUp();
        // Later reset participants must query the restored pose, not the old pressure-plate overlap.
        Physics2D.SyncTransforms();
    }
}
