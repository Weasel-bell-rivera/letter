using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class VerticalWallPatrolEnemy2D : MonoBehaviour, IRoomResettable, IFreezingGroundActor2D
{
    public enum WallSide { Left = -1, Right = 1 }

    public const float DefaultLowerEndpoint = -2f;
    public const float DefaultUpperEndpoint = 2f;
    public const float DefaultMoveSpeed = 1.5f;
    public const float MinimumMoveSpeed = .5f;
    public const float MaximumMoveSpeed = 3f;
    public const float DefaultEndpointWait = .3f;
    public const float MaximumEndpointWait = 1f;
    public const float WallProbeDistance = .16f;
    public const float WallNormalThreshold = .65f;
    public static readonly Vector2 DefaultBodySize = new(.72f, .9f);
    public static readonly Vector2 DefaultDamageSize = new(.82f, .98f);
    public const float ColliderWallOffset = .1f;

    [Header("Prefab references")]
    [SerializeField] private BoxCollider2D bodyCollider;
    [SerializeField] private SurfaceSemantic2D bodySurface;
    [SerializeField] private VerticalWallPatrolDamageTrigger2D damageTrigger;
    [SerializeField] private Transform wallProbe;
    [SerializeField] private SpriteRenderer bodyVisual;

    [Header("Vertical patrol")]
    [SerializeField] private float lowerEndpoint = DefaultLowerEndpoint;
    [SerializeField] private float upperEndpoint = DefaultUpperEndpoint;
    [SerializeField, Range(MinimumMoveSpeed, MaximumMoveSpeed)]
    private float moveSpeed = DefaultMoveSpeed;
    [SerializeField, Range(0f, MaximumEndpointWait)]
    private float endpointWait = DefaultEndpointWait;
    [SerializeField] private WallSide wallSide = WallSide.Left;
    [SerializeField] private bool initiallyMovingUp = true;

    private readonly RaycastHit2D[] wallHits = new RaycastHit2D[12];
    private Rigidbody2D body;
    private Collider2D damageCollider;
    private Vector2 pathAnchor;
    private Vector2 lastValidWallPosition;
    private float waitRemaining;
    private bool movingUp;
    private bool initialized;
    private bool operational;
    private bool hasSeenValidWall;
    private bool configurationErrorLogged;
    private bool wallLossWarningLogged;
    private bool frozenByGround;
    private float freezingMovementMultiplier = 1f;

    public float LowerEndpoint => lowerEndpoint;
    public float UpperEndpoint => upperEndpoint;
    public float MoveSpeed => moveSpeed;
    public float EndpointWait => endpointWait;
    public WallSide AttachedWallSide => wallSide;
    public bool InitiallyMovingUp => initiallyMovingUp;
    public bool IsMovingUp => movingUp;
    public bool IsOperational => operational;
    public bool IsDamaging => operational && damageCollider != null && damageCollider.enabled;
    public Vector2 PathAnchor => pathAnchor;
    public Vector2 PathLower => pathAnchor + Vector2.up * lowerEndpoint;
    public Vector2 PathUpper => pathAnchor + Vector2.up * upperEndpoint;
    public bool IsFrozenByGround => frozenByGround;
    public Rigidbody2D FreezingBody => body;
    public Collider2D FreezingCollider => bodyCollider;
    public Vector2 FreezingUpAxis => Vector2.up;

    private void Awake()
    {
        ResolveReferences();
        FreezingGroundActor2D.Ensure(gameObject);
        pathAnchor = body != null ? body.position : (Vector2)transform.position;
        initialized = true;
        ResetRoomState();
    }

    private void Start() => ValidateConfiguration();

    private void FixedUpdate()
    {
        if (frozenByGround || !operational || !ValidateConfiguration()) return;

        if (waitRemaining > 0f)
        {
            waitRemaining = Mathf.Max(0f, waitRemaining - Time.fixedDeltaTime);
            StopBody();
            return;
        }

        if (!TryFindValidWall())
        {
            HandleMissingWall();
            return;
        }

        hasSeenValidWall = true;
        lastValidWallPosition = body.position;
        float targetY = movingUp ? PathUpper.y : PathLower.y;
        float nextY = Mathf.MoveTowards(body.position.y, targetY,
            moveSpeed * freezingMovementMultiplier * Time.fixedDeltaTime);
        Vector2 nextPosition = new(pathAnchor.x, nextY);
        body.MovePosition(nextPosition);

        if (!Mathf.Approximately(nextY, targetY)) return;
        movingUp = !movingUp;
        waitRemaining = endpointWait;
        RefreshPresentation();
    }

    public void ConfigurePrefabReferences(BoxCollider2D solid, SurfaceSemantic2D semantic,
        VerticalWallPatrolDamageTrigger2D damage, Transform probe, SpriteRenderer visual)
    {
        bodyCollider = solid;
        bodySurface = semantic;
        damageTrigger = damage;
        wallProbe = probe;
        bodyVisual = visual;
        ResolveReferences();
        damageTrigger?.Configure(this);
        configurationErrorLogged = false;
        RefreshPresentation();
    }

    public void ConfigurePatrol(float localLowerEndpoint, float localUpperEndpoint, float speed,
        float wait, WallSide side = WallSide.Left, bool moveUpInitially = true)
    {
        lowerEndpoint = localLowerEndpoint;
        upperEndpoint = localUpperEndpoint;
        moveSpeed = speed;
        endpointWait = wait;
        wallSide = side;
        initiallyMovingUp = moveUpInitially;
        configurationErrorLogged = false;
        if (Application.isPlaying && initialized)
        {
            pathAnchor = body.position;
            ResetRoomState();
        }
        else RefreshPresentation();
    }

    public void HandleCharacterContact(Collider2D other)
    {
        if (!IsDamaging || other == null) return;
        MirrorCloneController2D clone = other.GetComponentInParent<MirrorCloneController2D>();
        if (clone != null)
        {
            clone.Die();
            return;
        }

        if (other.GetComponentInParent<PlayerController2D>() != null)
            FindAnyObjectByType<RoomResetSystem>()?.ResetRoom();
    }

    public void ResetRoomState()
    {
        ResolveReferences();
        if (!initialized)
        {
            pathAnchor = body != null ? body.position : (Vector2)transform.position;
            initialized = true;
        }

        movingUp = initiallyMovingUp;
        waitRemaining = 0f;
        hasSeenValidWall = false;
        wallLossWarningLogged = false;
        frozenByGround = false;
        freezingMovementMultiplier = 1f;
        operational = ValidateConfiguration();
        lastValidWallPosition = pathAnchor;
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.position = pathAnchor;
            StopBody();
        }
        bodySurface?.Configure(SurfaceSemantic2D.SurfaceType.DynamicSurface, false, false);
        damageTrigger?.SetDamageEnabled(operational);
        RefreshPresentation();
        Physics2D.SyncTransforms();
    }

    public void SetFreezingMovementMultiplier(float multiplier)
        => freezingMovementMultiplier = Mathf.Clamp01(multiplier);

    public void CompleteFreezingGround()
    {
        frozenByGround = true;
        StopBody();
        damageTrigger?.SetDamageEnabled(false);
        if (bodyVisual != null) bodyVisual.color = new Color(.65f, .86f, 1f, 1f);
    }

    private void ResolveReferences()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = transform.Find("BodyCollider")?.GetComponent<BoxCollider2D>();
        if (bodySurface == null && bodyCollider != null)
            bodySurface = bodyCollider.GetComponent<SurfaceSemantic2D>();
        if (damageTrigger == null)
            damageTrigger = GetComponentInChildren<VerticalWallPatrolDamageTrigger2D>(true);
        if (damageTrigger != null)
        {
            damageTrigger.Configure(this);
            damageCollider = damageTrigger.Trigger;
        }
        if (wallProbe == null) wallProbe = transform.Find("WallProbe");
        if (bodyVisual == null)
            bodyVisual = transform.Find("Visual/BodyVisual")?.GetComponent<SpriteRenderer>();
    }

    private bool ValidateConfiguration()
    {
        bool valid = body != null && bodyCollider != null && bodySurface != null &&
                     damageTrigger != null && damageCollider != null && wallProbe != null &&
                     bodyVisual != null && lowerEndpoint < upperEndpoint &&
                     moveSpeed >= MinimumMoveSpeed && moveSpeed <= MaximumMoveSpeed &&
                     endpointWait >= 0f && endpointWait <= MaximumEndpointWait &&
                     transform.rotation == Quaternion.identity && transform.lossyScale == Vector3.one;
        if (!valid && !configurationErrorLogged)
        {
            Debug.LogError($"Invalid vertical wall patrol configuration on {name}.", this);
            configurationErrorLogged = true;
        }
        if (!valid)
        {
            operational = false;
            if (damageCollider != null) damageCollider.enabled = false;
            StopBody();
        }
        return valid;
    }

    private bool TryFindValidWall()
    {
        int side = (int)wallSide;
        Bounds bounds = bodyCollider.bounds;
        Vector2 direction = Vector2.right * side;
        Vector2 origin = new(bounds.center.x + direction.x * Mathf.Max(0f, bounds.extents.x - .02f),
            wallProbe.position.y);
        Vector2 querySize = new(.04f, Mathf.Max(.1f, bounds.size.y * .65f));
        int count = Physics2D.BoxCast(origin, querySize, 0f, direction, ContactFilter2D.noFilter,
            wallHits, WallProbeDistance);
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = wallHits[i];
            if (hit.collider == null || hit.collider.isTrigger || hit.collider.attachedRigidbody == body ||
                Vector2.Dot(hit.normal, -direction) < WallNormalThreshold) continue;
            if (!SurfaceSemantic2D.TryGet(hit.collider, out SurfaceSemantic2D semantic) ||
                semantic.Type != SurfaceSemantic2D.SurfaceType.StaticSolid || !semantic.IsStatic ||
                !semantic.IsSafe) continue;
            return true;
        }
        return false;
    }

    private void HandleMissingWall()
    {
        StopBody();
        if (!hasSeenValidWall)
        {
            operational = false;
            damageTrigger?.SetDamageEnabled(false);
            if (!configurationErrorLogged)
            {
                Debug.LogError($"Vertical wall patrol {name} has no valid StaticSolid wall at its initial position.", this);
                configurationErrorLogged = true;
            }
            return;
        }

        body.MovePosition(lastValidWallPosition);
        movingUp = !movingUp;
        waitRemaining = endpointWait;
        RefreshPresentation();
        if (wallLossWarningLogged) return;
        Debug.LogWarning($"Vertical wall patrol {name} encountered a wall gap and reversed.", this);
        wallLossWarningLogged = true;
    }

    private void StopBody()
    {
        if (body == null) return;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    private void RefreshPresentation()
    {
        int side = (int)wallSide;
        float offset = side * ColliderWallOffset;
        if (bodyCollider != null)
        {
            bodyCollider.size = DefaultBodySize;
            bodyCollider.offset = new Vector2(offset, 0f);
        }
        if (damageCollider is BoxCollider2D damageBox)
        {
            damageBox.size = DefaultDamageSize;
            damageBox.offset = new Vector2(offset, 0f);
        }
        if (wallProbe != null)
            wallProbe.localPosition = new Vector3(side * (ColliderWallOffset + DefaultBodySize.x * .5f), 0f, 0f);
        if (bodyVisual == null) return;
        bodyVisual.transform.localRotation = Quaternion.Euler(0f, 0f, side > 0 ? 90f : -90f);
        bodyVisual.flipX = movingUp == (wallSide == WallSide.Right);
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Clamp(moveSpeed, MinimumMoveSpeed, MaximumMoveSpeed);
        endpointWait = Mathf.Clamp(endpointWait, 0f, MaximumEndpointWait);
        configurationErrorLogged = false;
        ResolveReferences();
        RefreshPresentation();
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 anchor = Application.isPlaying && initialized ? pathAnchor : (Vector2)transform.position;
        Vector2 lower = anchor + Vector2.up * lowerEndpoint;
        Vector2 upper = anchor + Vector2.up * upperEndpoint;
        Gizmos.color = new Color(.75f, .35f, 1f, 1f);
        Gizmos.DrawLine(lower, upper);
        Gizmos.DrawWireSphere(lower, .1f);
        Gizmos.DrawWireSphere(upper, .1f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(anchor, Vector2.right * ((int)wallSide * WallProbeDistance));
    }
}
