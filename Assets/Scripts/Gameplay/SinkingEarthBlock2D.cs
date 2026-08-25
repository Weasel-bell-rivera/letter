using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SurfaceSemantic2D))]
[DefaultExecutionOrder(-200)]
public sealed class SinkingEarthBlock2D : MonoBehaviour, ISurfaceMotionProvider2D, IRoomResettable,
    IOrderedRoomResettable
{
    [Header("Motion")]
    [SerializeField, Min(.01f)] private float sinkDistance = 1f;
    [SerializeField, Min(.01f)] private float sinkSpeed = 1.5f;
    [SerializeField, Min(.01f)] private float recoverSpeed = 1f;
    [SerializeField, Min(.01f)] private float weightForFullSink = 1f;

    private readonly HashSet<Rigidbody2D> supportedBodies = new();
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
        if (other != null) supportedBodies.Remove(other);
    }

    private void Track(Collision2D collision)
    {
        Rigidbody2D other = collision.rigidbody;
        if (other != null && other != body) supportedBodies.Add(other);
    }

    private float CalculateSupportedWeight()
    {
        float total = 0f;
        float top = blockCollider.bounds.max.y;
        foreach (Rigidbody2D supportedBody in supportedBodies)
        {
            if (supportedBody == null) continue;
            Collider2D[] colliders = supportedBody.GetComponentsInChildren<Collider2D>();
            bool restsOnTop = false;
            foreach (Collider2D candidate in colliders)
            {
                if (!candidate.enabled || candidate.isTrigger) continue;
                Bounds bounds = candidate.bounds;
                if (bounds.min.y < top - .15f || bounds.max.x <= blockCollider.bounds.min.x ||
                    bounds.min.x >= blockCollider.bounds.max.x) continue;
                restsOnTop = true;
                break;
            }
            if (restsOnTop) total += Mathf.Max(0f, supportedBody.mass);
        }
        return total;
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
