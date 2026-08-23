using System;
using UnityEngine;

public interface ISurfaceMotionProvider2D
{
    bool TryGetSurfaceVelocity(Vector2 contactPoint, Vector2 supportNormal, out Vector2 velocity);
}

public interface IActivationReceiver2D
{
    void SetActive(bool active);
}

public static class SurfaceMotion2D
{
    public static Vector2 Resolve(RaycastHit2D supportHit, bool grounded, out Collider2D motionCollider)
    {
        motionCollider = null;
        if (!grounded || supportHit.collider == null) return Vector2.zero;
        ISurfaceMotionProvider2D provider = supportHit.collider.GetComponent<ISurfaceMotionProvider2D>();
        if (provider == null || !provider.TryGetSurfaceVelocity(supportHit.point, supportHit.normal,
                out Vector2 surfaceVelocity)) return Vector2.zero;
        motionCollider = supportHit.collider;
        return surfaceVelocity;
    }

    public static Vector2 RemoveRepeatedContribution(Vector2 worldVelocity, Collider2D previousCollider,
        Collider2D nextCollider, Vector2 previousSurfaceVelocity)
        => nextCollider != null && nextCollider == previousCollider
            ? worldVelocity - previousSurfaceVelocity
            : worldVelocity;
}

public static class SurfaceSupport2D
{
    private const float MinimumSupportNormal = .65f;
    private const float SamePlaneTolerance = .01f;

    public static bool TryResolve(Collider2D bodyCollider, GameObject owner, Vector2 gravityDirection,
        float castDistance, LayerMask groundMask, Collider2D previousSupport,
        out SurfaceSemantic2D surface, out RaycastHit2D supportHit)
    {
        surface = null;
        supportHit = default;
        if (bodyCollider == null || gravityDirection.sqrMagnitude < .0001f) return false;

        Vector2 down = gravityDirection.normalized;
        Bounds bodyBounds = bodyCollider.bounds;
        RaycastHit2D[] hits = Physics2D.BoxCastAll(bodyBounds.center,
            bodyBounds.size * new Vector2(.8f, .9f), 0f, down, castDistance, groundMask);

        foreach (RaycastHit2D hit in hits)
        {
            if (!IsValidSupport(hit, owner, down) || hit.collider != previousSupport) continue;
            supportHit = hit;
            SurfaceSemantic2D.TryGet(hit.collider, out surface);
            return true;
        }

        bool found = false;
        bool bestCoversFootCenter = false;
        bool bestProvidesMotion = false;
        Vector2 tangent = new(-down.y, down.x);
        foreach (RaycastHit2D hit in hits)
        {
            if (!IsValidSupport(hit, owner, down)) continue;
            bool coversFootCenter = CoversFootCenter(hit.collider.bounds, bodyBounds.center, tangent);
            bool providesMotion = hit.collider.GetComponent<ISurfaceMotionProvider2D>() != null;
            if (found && !IsBetterCandidate(hit, coversFootCenter, providesMotion, supportHit,
                    bestCoversFootCenter, bestProvidesMotion)) continue;
            found = true;
            supportHit = hit;
            bestCoversFootCenter = coversFootCenter;
            bestProvidesMotion = providesMotion;
        }

        if (!found) return false;
        SurfaceSemantic2D.TryGet(supportHit.collider, out surface);
        return true;
    }

    private static bool IsValidSupport(RaycastHit2D hit, GameObject owner, Vector2 down)
    {
        if (hit.collider == null || hit.collider.gameObject == owner || hit.collider.isTrigger) return false;
        if (hit.collider.GetComponent<PlayerController2D>() != null ||
            hit.collider.GetComponent<MirrorCloneController2D>() != null) return false;
        return hit.normal.sqrMagnitude > .0001f && Vector2.Dot(hit.normal.normalized, -down) >= MinimumSupportNormal;
    }

    private static bool CoversFootCenter(Bounds supportBounds, Vector2 footCenter, Vector2 tangent)
    {
        float center = Vector2.Dot(supportBounds.center, tangent);
        float extent = Mathf.Abs(tangent.x) * supportBounds.extents.x +
                       Mathf.Abs(tangent.y) * supportBounds.extents.y;
        float coordinate = Vector2.Dot(footCenter, tangent);
        return coordinate >= center - extent - SamePlaneTolerance &&
               coordinate <= center + extent + SamePlaneTolerance;
    }

    private static bool IsBetterCandidate(RaycastHit2D candidate, bool candidateCoversFootCenter,
        bool candidateProvidesMotion, RaycastHit2D current, bool currentCoversFootCenter,
        bool currentProvidesMotion)
    {
        if (candidate.distance < current.distance - SamePlaneTolerance) return true;
        if (candidate.distance > current.distance + SamePlaneTolerance) return false;
        if (candidateCoversFootCenter != currentCoversFootCenter) return candidateCoversFootCenter;
        if (candidateProvidesMotion != currentProvidesMotion) return candidateProvidesMotion;
        return candidate.collider.GetEntityId() < current.collider.GetEntityId();
    }
}

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SurfaceSemantic2D))]
public sealed class GroundConveyor2D : MonoBehaviour, ISurfaceMotionProvider2D, IActivationReceiver2D,
    IRoomResettable, IOrderedRoomResettable
{
    public enum BeltDirection { Left = -1, Right = 1 }

    public const float MinimumSpeed = .5f;
    public const float MaximumSpeed = 4.5f;
    public const float DefaultSpeed = 2.5f;

    [SerializeField] private BeltDirection direction = BeltDirection.Right;
    [SerializeField, Range(MinimumSpeed, MaximumSpeed)] private float speed = DefaultSpeed;
    [SerializeField] private bool initiallyActive = true;
    [SerializeField] private BoxCollider2D surfaceCollider;
    [SerializeField] private ConveyorVisual2D visual;

    private Rigidbody2D body;
    private SurfaceSemantic2D surfaceSemantic;
    private bool configurationErrorLogged;

    public int ResetOrder => -80;
    public BeltDirection Direction => direction;
    public float Speed => speed;
    public bool InitiallyActive => initiallyActive;
    public bool IsActive { get; private set; }
    public Vector2 SurfaceVelocity => IsActive ? Vector2.right * ((int)direction * speed) : Vector2.zero;
    public event Action<GroundConveyor2D, bool> ActiveChanged;

    private void Awake()
    {
        ResolveReferences();
        ConfigurePhysicsAndSemantic();
        ResetRoomState();
    }

    private void Start() => ValidateConfiguration();

    public bool TryGetSurfaceVelocity(Vector2 contactPoint, Vector2 supportNormal, out Vector2 velocity)
    {
        velocity = Vector2.zero;
        if (!ValidateConfiguration() || Vector2.Dot(supportNormal.normalized, Vector2.up) < .65f) return false;
        velocity = SurfaceVelocity;
        return true;
    }

    public void Configure(BeltDirection beltDirection, float beltSpeed, bool activeInitially = true)
    {
        direction = beltDirection;
        speed = Mathf.Clamp(beltSpeed, MinimumSpeed, MaximumSpeed);
        initiallyActive = activeInitially;
        configurationErrorLogged = false;
        ResolveReferences();
        ConfigurePhysicsAndSemantic();
        ResetRoomState();
    }

    public void ConfigureReferences(BoxCollider2D collider, ConveyorVisual2D conveyorVisual)
    {
        surfaceCollider = collider;
        visual = conveyorVisual;
        ResolveReferences();
        ConfigurePhysicsAndSemantic();
        ApplyVisual(true);
    }

    public void SetActive(bool active)
    {
        if (IsActive == active) return;
        IsActive = active;
        ApplyVisual(false);
        ActiveChanged?.Invoke(this, active);
    }

    public void ResetRoomState()
    {
        ResolveReferences();
        ConfigurePhysicsAndSemantic();
        IsActive = initiallyActive;
        ApplyVisual(true);
    }

    private void ResolveReferences()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (surfaceCollider == null) surfaceCollider = GetComponent<BoxCollider2D>();
        if (surfaceSemantic == null) surfaceSemantic = GetComponent<SurfaceSemantic2D>();
        if (visual == null) visual = GetComponentInChildren<ConveyorVisual2D>(true);
    }

    private void ConfigurePhysicsAndSemantic()
    {
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Static;
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }
        surfaceSemantic?.Configure(SurfaceSemantic2D.SurfaceType.Conveyor, true, true);
        if (surfaceCollider != null)
        {
            surfaceCollider.isTrigger = false;
            surfaceCollider.edgeRadius = 0f;
        }
    }

    private bool ValidateConfiguration()
    {
        bool valid = body != null && surfaceCollider != null && surfaceSemantic != null &&
                     speed >= MinimumSpeed && speed <= MaximumSpeed && transform.rotation == Quaternion.identity;
        if (!valid && !configurationErrorLogged)
        {
            Debug.LogError($"Invalid ground conveyor configuration on {name}.", this);
            configurationErrorLogged = true;
        }
        return valid;
    }

    private void ApplyVisual(bool resetPhase)
        => visual?.ApplyState((int)direction, speed, IsActive, resetPhase);

    private void OnValidate()
    {
        speed = Mathf.Clamp(speed, MinimumSpeed, MaximumSpeed);
        configurationErrorLogged = false;
    }
}
