using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class MovingTornado2D : MonoBehaviour
{
    public const float DefaultSpeed = 3f;
    public const float DefaultMaximumDistance = 12f;

    [SerializeField] private Vector2 direction = Vector2.right;
    [SerializeField, Range(1f, 6f)] private float speed = DefaultSpeed;
    [SerializeField, Range(1f, 30f)] private float maximumDistance = DefaultMaximumDistance;
    [SerializeField] private BoxCollider2D damageTrigger;

    private Rigidbody2D body;
    private Vector2 origin;
    private float travelled;
    private bool consumed;

    public Vector2 Direction => direction.normalized;
    public float Speed => speed;
    public float MaximumDistance => maximumDistance;
    public event Action<MovingTornado2D> Removed;

    private void Awake()
    {
        ResolveReferences();
        ConfigurePhysics();
        origin = transform.position;
    }

    private void FixedUpdate()
    {
        if (consumed || body == null || damageTrigger == null) return;
        float requested = Mathf.Min(speed * Time.fixedDeltaTime, maximumDistance - travelled);
        if (requested <= 0f)
        {
            Consume();
            return;
        }
        if (TryRedirectAtBlock(requested)) return;
        float allowed = DistanceBeforeBlock(requested);
        if (allowed <= 0f)
        {
            Consume();
            return;
        }

        body.MovePosition(body.position + Direction * allowed);
        travelled += allowed;

        // Consume on the same physics tick that reaches a wall or the configured
        // travel limit. Previously the tornado could remain stopped at a wall.
        bool reachedBlock = allowed + .0001f < requested;
        bool reachedMaximumDistance = travelled + .0001f >= maximumDistance;
        if (reachedBlock || reachedMaximumDistance) Consume();
    }

    public void Configure(Vector2 worldDirection, float moveSpeed, float maxDistance)
    {
        direction = worldDirection.sqrMagnitude > .0001f ? worldDirection.normalized : Vector2.right;
        speed = Mathf.Clamp(moveSpeed, 1f, 6f);
        maximumDistance = Mathf.Clamp(maxDistance, 1f, 30f);
        origin = transform.position;
        travelled = 0f;
        consumed = false;
        ResolveReferences();
        ConfigurePhysics();
    }

    public void ConfigureReferences(BoxCollider2D trigger)
    {
        damageTrigger = trigger;
        ResolveReferences();
        ConfigurePhysics();
    }

    public void RemoveImmediately()
    {
        if (consumed) return;
        consumed = true;
        if (damageTrigger != null) damageTrigger.enabled = false;
        Removed?.Invoke(this);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;
        MirrorCloneController2D clone = other.GetComponentInParent<MirrorCloneController2D>();
        if (clone != null)
        {
            clone.Die();
            Consume();
            return;
        }
        if (other.GetComponentInParent<PlayerController2D>() != null)
        {
            FindAnyObjectByType<RoomResetSystem>()?.ResetRoom();
            Consume();
        }
    }

    private float DistanceBeforeBlock(float requested)
    {
        Bounds bounds = damageTrigger.bounds;
        float allowed = requested;
        foreach (RaycastHit2D hit in Physics2D.BoxCastAll(bounds.center, bounds.size * .9f, 0f,
                     Direction, requested + .03f))
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider == damageTrigger || collider.isTrigger ||
                collider.attachedRigidbody == body) continue;
            if (collider.GetComponentInParent<PlayerController2D>() != null ||
                collider.GetComponentInParent<MirrorCloneController2D>() != null) continue;
            allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - .02f));
        }
        return allowed;
    }

    private bool TryRedirectAtBlock(float requested)
    {
        Bounds bounds = damageTrigger.bounds;
        RaycastHit2D closest = default;
        bool found = false;
        foreach (RaycastHit2D hit in Physics2D.BoxCastAll(bounds.center, bounds.size * .9f, 0f,
                     Direction, requested + .03f))
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider == damageTrigger || collider.isTrigger ||
                collider.attachedRigidbody == body ||
                collider.GetComponentInParent<PlayerController2D>() != null ||
                collider.GetComponentInParent<MirrorCloneController2D>() != null) continue;
            if (!found || hit.distance < closest.distance)
            {
                closest = hit;
                found = true;
            }
        }
        if (!found) return false;
        WindDeflector2D deflector = closest.collider.GetComponentInParent<WindDeflector2D>();
        if (deflector == null || !deflector.TryRedirect(Direction, out Vector2 redirected)) return false;
        float advance = Mathf.Max(0f, closest.distance - .02f);
        travelled += advance;
        direction = redirected;
        Bounds deflectorBounds = closest.collider.bounds;
        float exitDistance = Mathf.Abs(Direction.x) * deflectorBounds.extents.x +
                             Mathf.Abs(Direction.y) * deflectorBounds.extents.y +
                             Mathf.Max(damageTrigger.bounds.extents.x, damageTrigger.bounds.extents.y) + .03f;
        body.position = (Vector2)deflectorBounds.center + Direction * exitDistance;
        transform.position = body.position;
        Physics2D.SyncTransforms();
        return true;
    }

    private void Consume() => RemoveImmediately();

    private void ResolveReferences()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (damageTrigger == null) damageTrigger = GetComponent<BoxCollider2D>();
    }

    private void ConfigurePhysics()
    {
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        if (damageTrigger != null) damageTrigger.isTrigger = true;
    }
}
