using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SurfaceSemantic2D))]
[DefaultExecutionOrder(-200)]
public sealed class MovingPlatform2D : MonoBehaviour, ISurfaceMotionProvider2D, IRoomResettable,
    IOrderedRoomResettable
{
    [Header("Local path")]
    [SerializeField] private Vector2 startOffset = Vector2.zero;
    [SerializeField] private Vector2 endOffset = new(4f, 0f);

    [Header("Motion")]
    [SerializeField, Min(.01f)] private float moveSpeed = 2f;
    [SerializeField, Min(0f)] private float endpointWait = .35f;
    [SerializeField, Range(0f, 1f)] private float initialPhase;
    [SerializeField] private bool initiallyTowardsEnd = true;
    [SerializeField] private bool initiallyMoving = true;

    private Rigidbody2D body;
    private BoxCollider2D platformCollider;
    private SurfaceSemantic2D surfaceSemantic;
    private Vector2 pathAnchor;
    private Quaternion pathRotation;
    private float phase;
    private float waitRemaining;
    private bool towardsEnd;
    private bool moving;
    private bool initialized;
    private bool configurationErrorLogged;
    private Vector2 surfaceVelocity;

    public int ResetOrder => -100;
    public Vector2 StartOffset => startOffset;
    public Vector2 EndOffset => endOffset;
    public float MoveSpeed => moveSpeed;
    public float EndpointWait => endpointWait;
    public float InitialPhase => initialPhase;
    public float Phase => phase;
    public bool IsTowardsEnd => towardsEnd;
    public bool IsMoving => moving;
    public Vector2 SurfaceVelocity => surfaceVelocity;
    public Vector2 PathStart => pathAnchor + RotateOffset(startOffset);
    public Vector2 PathEnd => pathAnchor + RotateOffset(endOffset);

    private void Awake()
    {
        ResolveReferences();
        pathAnchor = body.position;
        pathRotation = transform.rotation;
        initialized = true;
        ResetRoomState();
    }

    private void Start() => ValidateConfiguration();

    private void FixedUpdate()
    {
        surfaceVelocity = Vector2.zero;
        if (!moving || !ValidateConfiguration())
        {
            if (body != null) body.linearVelocity = Vector2.zero;
            return;
        }

        if (waitRemaining > 0f)
        {
            waitRemaining = Mathf.Max(0f, waitRemaining - Time.fixedDeltaTime);
            body.linearVelocity = Vector2.zero;
            return;
        }

        float pathLength = Vector2.Distance(PathStart, PathEnd);
        float targetPhase = towardsEnd ? 1f : 0f;
        float nextPhase = Mathf.MoveTowards(phase, targetPhase, moveSpeed * Time.fixedDeltaTime / pathLength);
        Vector2 nextPosition = Vector2.Lerp(PathStart, PathEnd, nextPhase);
        Vector2 delta = nextPosition - body.position;

        surfaceVelocity = delta / Time.fixedDeltaTime;
        body.MovePosition(nextPosition);
        phase = nextPhase;

        if (!Mathf.Approximately(phase, targetPhase)) return;
        towardsEnd = !towardsEnd;
        waitRemaining = endpointWait;
    }

    public void ConfigurePath(Vector2 localStart, Vector2 localEnd, float speed, float wait,
        float normalizedInitialPhase = 0f, bool moveTowardsEnd = true, bool moveInitially = true)
    {
        ResolveReferences();
        startOffset = localStart;
        endOffset = localEnd;
        moveSpeed = speed;
        endpointWait = wait;
        initialPhase = Mathf.Clamp01(normalizedInitialPhase);
        initiallyTowardsEnd = moveTowardsEnd;
        initiallyMoving = moveInitially;
        configurationErrorLogged = false;
        pathAnchor = body.position;
        pathRotation = transform.rotation;
        initialized = true;
        ResetRoomState();
    }

    public void SetMoving(bool value)
    {
        moving = value;
        if (!moving)
        {
            surfaceVelocity = Vector2.zero;
            if (body != null) body.linearVelocity = Vector2.zero;
        }
    }

    public bool TryGetSurfaceVelocity(Vector2 contactPoint, Vector2 supportNormal, out Vector2 velocity)
    {
        velocity = surfaceVelocity;
        return body != null && platformCollider != null && surfaceSemantic != null;
    }

    public void ResetRoomState()
    {
        ResolveReferences();
        phase = initialPhase;
        towardsEnd = initiallyTowardsEnd;
        moving = initiallyMoving;
        waitRemaining = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.position = Vector2.Lerp(PathStart, PathEnd, phase);
        surfaceVelocity = Vector2.zero;
        surfaceSemantic.Configure(SurfaceSemantic2D.SurfaceType.DynamicSurface, false, true);
        Physics2D.SyncTransforms();
    }

    private void ResolveReferences()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (platformCollider == null) platformCollider = GetComponent<BoxCollider2D>();
        if (surfaceSemantic == null) surfaceSemantic = GetComponent<SurfaceSemantic2D>();
    }

    private bool ValidateConfiguration()
    {
        bool valid = body != null && platformCollider != null && surfaceSemantic != null &&
                     Vector2.Distance(startOffset, endOffset) > .001f && moveSpeed > 0f &&
                     endpointWait >= 0f && initialPhase >= 0f && initialPhase <= 1f;
        if (!valid && !configurationErrorLogged)
        {
            Debug.LogError($"Invalid moving platform configuration on {name}.", this);
            configurationErrorLogged = true;
        }
        return valid;
    }

    private Vector2 RotateOffset(Vector2 offset) => pathRotation * offset;

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(.01f, moveSpeed);
        endpointWait = Mathf.Max(0f, endpointWait);
        initialPhase = Mathf.Clamp01(initialPhase);
        configurationErrorLogged = false;
    }

    private void OnDrawGizmosSelected()
    {
        Quaternion rotation = Application.isPlaying && initialized ? pathRotation : transform.rotation;
        Vector2 anchor = Application.isPlaying && initialized ? pathAnchor : (Vector2)transform.position;
        Vector2 start = anchor + (Vector2)(rotation * startOffset);
        Vector2 end = anchor + (Vector2)(rotation * endOffset);
        Gizmos.color = new Color(.2f, .85f, 1f, 1f);
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, .12f);
        Gizmos.DrawWireSphere(end, .12f);
    }
}
