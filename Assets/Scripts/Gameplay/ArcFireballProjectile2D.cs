using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class ArcFireballProjectile2D : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bodyVisual;

    private readonly RaycastHit2D[] sweepHits = new RaycastHit2D[24];
    private GroundFireThrowerEnemy2D owner;
    private Rigidbody2D body;
    private CircleCollider2D hitCollider;
    private Vector2 startPoint;
    private Vector2 lockedPoint;
    private float travelDuration;
    private float arcHeight;
    private float maximumLifetime;
    private float elapsed;
    private bool active;
    private bool destroying;

    public bool IsActive => active && !destroying;
    public Vector2 StartPoint => startPoint;
    public Vector2 LockedPoint => lockedPoint;
    public float Elapsed => elapsed;
    public float Radius => hitCollider != null ? hitCollider.radius : 0f;

    private void Awake()
    {
        ResolveReferences();
        ConfigureBody();
    }

    public void ConfigurePrefabReferences(SpriteRenderer visual)
    {
        bodyVisual = visual;
        ResolveReferences();
        ConfigureBody();
    }

    public void Launch(GroundFireThrowerEnemy2D projectileOwner, Vector2 target, float speed,
        float height, float lifetime, float radius)
    {
        ResolveReferences();
        ConfigureBody();
        owner = projectileOwner;
        startPoint = body.position;
        lockedPoint = target;
        arcHeight = Mathf.Max(.01f, height);
        maximumLifetime = Mathf.Max(Time.fixedDeltaTime, lifetime);
        travelDuration = Mathf.Max(.2f, Vector2.Distance(startPoint, lockedPoint) /
                                          Mathf.Max(.01f, speed));
        hitCollider.radius = Mathf.Max(.01f, radius);
        elapsed = 0f;
        active = true;
        destroying = false;
        transform.position = startPoint;
        Physics2D.SyncTransforms();
    }

    private void FixedUpdate()
    {
        if (!IsActive) return;

        float nextElapsed = elapsed + Time.fixedDeltaTime;
        if (nextElapsed >= maximumLifetime)
        {
            DestroyProjectile();
            return;
        }

        Vector2 current = body.position;
        Vector2 next = EvaluatePosition(nextElapsed);
        Vector2 movement = next - current;
        if (SweepForImpact(current, movement)) return;

        if (movement.sqrMagnitude > .000001f && bodyVisual != null)
            bodyVisual.transform.right = movement.normalized;
        body.MovePosition(next);
        elapsed = nextElapsed;
    }

    private Vector2 EvaluatePosition(float time)
    {
        float progress = time / travelDuration;
        if (progress <= 1f)
            return Vector2.LerpUnclamped(startPoint, lockedPoint, progress) +
                   Vector2.up * (4f * arcHeight * progress * (1f - progress));

        float extra = time - travelDuration;
        Vector2 endVelocity = (lockedPoint - startPoint) / travelDuration -
                              Vector2.up * (4f * arcHeight / travelDuration);
        Vector2 gravity = Vector2.down * (8f * arcHeight /
                                          (travelDuration * travelDuration));
        return lockedPoint + endVelocity * extra + .5f * gravity * extra * extra;
    }

    private bool SweepForImpact(Vector2 origin, Vector2 movement)
    {
        float distance = movement.magnitude;
        if (distance <= .00001f) return false;

        int count = Physics2D.CircleCast(origin, hitCollider.radius * .9f,
            movement / distance, ContactFilter2D.noFilter, sweepHits, distance);
        for (int i = 0; i < count; i++)
        {
            Collider2D collider = sweepHits[i].collider;
            if (collider == null || IsOwnerCollider(collider) || collider == hitCollider) continue;
            if (HandleImpact(collider)) return true;
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsActive && !IsOwnerCollider(other)) HandleImpact(other);
    }

    private bool HandleImpact(Collider2D other)
    {
        if (other == null) return false;

        MirrorCloneController2D clone = other.GetComponentInParent<MirrorCloneController2D>();
        if (clone != null)
        {
            DestroyProjectile();
            clone.Die();
            return true;
        }

        if (other.GetComponentInParent<PlayerController2D>() != null)
        {
            RoomResetSystem reset = FindAnyObjectByType<RoomResetSystem>();
            DestroyProjectile();
            reset?.ResetRoom();
            return true;
        }

        if (other.isTrigger) return false;
        DestroyProjectile();
        return true;
    }

    private bool IsOwnerCollider(Collider2D other)
    {
        if (other == null || owner == null) return false;
        return other.transform == owner.transform || other.transform.IsChildOf(owner.transform);
    }

    public void DestroyProjectile()
    {
        if (destroying) return;
        destroying = true;
        active = false;
        owner?.ForgetProjectile(this);
        owner = null;
        if (Application.isPlaying) Destroy(gameObject);
        else DestroyImmediate(gameObject);
    }

    private void OnDestroy()
    {
        owner?.ForgetProjectile(this);
        owner = null;
    }

    private void ResolveReferences()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (hitCollider == null) hitCollider = GetComponent<CircleCollider2D>();
        if (bodyVisual == null) bodyVisual = transform.Find("Visual/BodyVisual")?.GetComponent<SpriteRenderer>();
    }

    private void ConfigureBody()
    {
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
        if (hitCollider != null) hitCollider.isTrigger = true;
    }
}
