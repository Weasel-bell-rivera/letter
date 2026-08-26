using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SurfaceSemantic2D))]
[DefaultExecutionOrder(-200)]
public sealed class SinkingEarthBlock2D : MonoBehaviour, ISurfaceMotionProvider2D, IRoomResettable,
    IOrderedRoomResettable
{
    private const float MinimumTopContactNormal = .65f;
    private const float TopContactTolerance = .15f;
    [Header("Motion")]
    [SerializeField, Min(.01f)] private float sinkDistance = 1f;
    [SerializeField, Min(.01f)] private float sinkSpeed = 1.5f;
    [SerializeField, Min(.01f)] private float recoverSpeed = 1f;
    [SerializeField, Min(.01f)] private float weightForFullSink = 1f;

    private readonly HashSet<Rigidbody2D> supportedBodies = new();
    private readonly List<Rigidbody2D> actorsToRelease = new();
    private Rigidbody2D body;
    private BoxCollider2D blockCollider;
    private SurfaceSemantic2D surfaceSemantic;
    private Vector2 initialPosition;
    private Vector2 surfaceVelocity;
    private bool initialized;

    public int ResetOrder => -100;
    public float SinkDistance => sinkDistance;
    public float SinkSpeed => sinkSpeed;
    public float RecoverSpeed => recoverSpeed;
    public float WeightForFullSink => weightForFullSink;
    public float CurrentWeight { get; private set; }
    public Vector2 SurfaceVelocity => surfaceVelocity;

    private void Awake()
    {
        ResolveReferences();
        initialPosition = body.position;
        initialized = true;
        ConfigureBody();
    }

    private void FixedUpdate()
    {
        RemoveInvalidBodies();
        CurrentWeight = CalculateSupportedWeight();
        float sinkFraction = Mathf.Clamp01(CurrentWeight / weightForFullSink);
        Vector2 target = initialPosition + Vector2.down * sinkDistance * sinkFraction;
        float speed = target.y < body.position.y ? sinkSpeed : recoverSpeed;
        Vector2 next = Vector2.MoveTowards(body.position, target, speed * Time.fixedDeltaTime);
        surfaceVelocity = (next - body.position) / Time.fixedDeltaTime;
        body.MovePosition(next);
    }

    private void OnCollisionEnter2D(Collision2D collision) => Track(collision);
    private void OnCollisionStay2D(Collision2D collision) => Track(collision);

    private void OnCollisionExit2D(Collision2D collision)
    {
        Rigidbody2D other = collision.rigidbody;
        if (other != null && !TryGetAuthoritativeSupport(other, out _)) supportedBodies.Remove(other);
    }

    private void Track(Collision2D collision)
    {
        Rigidbody2D other = collision.rigidbody;
        if (other == null || other == body) return;

        if (!HasTopSupportContact(collision)) return;
        supportedBodies.Add(other);
    }

    private bool HasTopSupportContact(Collision2D collision)
    {
        if (collision.rigidbody.worldCenterOfMass.y <= body.worldCenterOfMass.y) return false;

        float minimumContactHeight = blockCollider.bounds.max.y - TopContactTolerance;
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (contact.point.y < minimumContactHeight) continue;
            if (Mathf.Abs(contact.normal.normalized.y) >= MinimumTopContactNormal) return true;
        }

        return false;
    }

    private float CalculateSupportedWeight()
    {
        float total = 0f;
        actorsToRelease.Clear();
        foreach (Rigidbody2D supportedBody in supportedBodies)
        {
            if (supportedBody == null) continue;
            if (TryGetAuthoritativeSupport(supportedBody, out Collider2D actorSupport) &&
                actorSupport != blockCollider)
            {
                actorsToRelease.Add(supportedBody);
                continue;
            }
            total += Mathf.Max(0f, supportedBody.mass);
        }
        foreach (Rigidbody2D actor in actorsToRelease) supportedBodies.Remove(actor);
        return total;
    }

    private static bool TryGetAuthoritativeSupport(Rigidbody2D actor, out Collider2D support)
    {
        PlayerController2D player = actor.GetComponent<PlayerController2D>();
        if (player != null)
        {
            support = player.SupportCollider;
            return true;
        }

        MirrorCloneController2D clone = actor.GetComponent<MirrorCloneController2D>();
        if (clone != null)
        {
            support = clone.SupportCollider;
            return true;
        }

        support = null;
        return false;
    }

    public bool TryGetSurfaceVelocity(Vector2 contactPoint, Vector2 supportNormal, out Vector2 velocity)
    {
        velocity = surfaceVelocity;
        return body != null && blockCollider != null && surfaceSemantic != null;
    }

    public void ResetRoomState()
    {
        ResolveReferences();
        if (!initialized)
        {
            initialPosition = body.position;
            initialized = true;
        }

        supportedBodies.Clear();
        CurrentWeight = 0f;
        surfaceVelocity = Vector2.zero;
        ConfigureBody();
        body.position = initialPosition;
        Physics2D.SyncTransforms();
    }

    private void ConfigureBody()
    {
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        surfaceSemantic.Configure(SurfaceSemantic2D.SurfaceType.DynamicSurface, false, true);
    }

    private void ResolveReferences()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (blockCollider == null) blockCollider = GetComponent<BoxCollider2D>();
        if (surfaceSemantic == null) surfaceSemantic = GetComponent<SurfaceSemantic2D>();
    }

    private void RemoveInvalidBodies() => supportedBodies.RemoveWhere(item => item == null || !item.simulated);

    private void OnValidate()
    {
        sinkDistance = Mathf.Max(.01f, sinkDistance);
        sinkSpeed = Mathf.Max(.01f, sinkSpeed);
        recoverSpeed = Mathf.Max(.01f, recoverSpeed);
        weightForFullSink = Mathf.Max(.01f, weightForFullSink);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 start = Application.isPlaying && initialized ? initialPosition : transform.position;
        Vector3 end = start + Vector3.down * sinkDistance;
        Gizmos.color = new Color(.55f, .32f, .12f, 1f);
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireCube(end, GetComponent<BoxCollider2D>()?.size ?? Vector2.one);
    }
}
