using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(HorizontalFireballEnemy2D))]
public sealed class PatrollingHorizontalFireballEnemy2D : MonoBehaviour, IRoomResettable,
    IOrderedRoomResettable
{
    public enum PatrolState { Patrolling, TurnPause, AttackStopped, Frozen }

    [SerializeField] private PatrollingHorizontalFireballEnemySettings settings;
    [SerializeField] private HorizontalFireballEnemy2D attackController;
    [SerializeField] private BoxCollider2D bodyCollider;
    [SerializeField] private bool initiallyMovingRight = true;

    private Rigidbody2D body;
    private Vector2 initialPosition;
    private float movementDirection;
    private float turnRemaining;
    private bool pendingTurn;
    private bool initialized;
    private bool configurationErrorLogged;

    public PatrolState State { get; private set; } = PatrolState.Patrolling;
    public float MovementDirection => movementDirection;
    public bool HasPendingTurn => pendingTurn;
    public int ResetOrder => 10;

    private void Awake()
    {
        ResolveReferences();
        ApplyBodyConfiguration();
        initialPosition = transform.position;
        initialized = true;
        ResetRoomState();
    }

    private void Start() => ValidateConfiguration();

    private void FixedUpdate()
    {
        if (!ValidateConfiguration()) return;
        if (attackController.State == HorizontalFireballEnemy2D.EnemyState.Frozen)
        {
            State = PatrolState.Frozen;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            return;
        }
        ApplyBodyConfiguration();

        bool attackStopped = attackController.State != HorizontalFireballEnemy2D.EnemyState.Watching;
        if (attackStopped)
        {
            State = PatrolState.AttackStopped;
            StopBody();
            if (!pendingTurn && ShouldTurn()) pendingTurn = true;
            return;
        }

        if (pendingTurn)
        {
            pendingTurn = false;
            BeginTurnPause();
        }

        if (State == PatrolState.TurnPause)
        {
            StopBody();
            turnRemaining = Mathf.Max(0f, turnRemaining - Time.fixedDeltaTime);
            if (turnRemaining <= 0f)
            {
                movementDirection = -movementDirection;
                State = PatrolState.Patrolling;
                attackController.SetInitiallyFacingRight(movementDirection > 0f);
            }
            return;
        }

        if (HasForwardStaticBlocker())
        {
            BeginTurnPause();
            return;
        }

        State = PatrolState.Patrolling;
        attackController.SetInitiallyFacingRight(movementDirection > 0f);
        body.linearVelocity = new Vector2(movementDirection * settings.PatrolSpeed, body.linearVelocity.y);
    }

    public void Configure(PatrollingHorizontalFireballEnemySettings sharedSettings,
        HorizontalFireballEnemy2D attack, BoxCollider2D solid)
    {
        settings = sharedSettings;
        attackController = attack;
        bodyCollider = solid;
        ResolveReferences();
        ApplyBodyConfiguration();
        configurationErrorLogged = false;
    }

    public void SetInitiallyMovingRight(bool value)
    {
        initiallyMovingRight = value;
        movementDirection = value ? 1f : -1f;
        attackController?.SetInitiallyFacingRight(value);
    }

    public void ResetRoomState()
    {
        ResolveReferences();
        if (!initialized)
        {
            initialPosition = transform.position;
            initialized = true;
        }
        movementDirection = initiallyMovingRight ? 1f : -1f;
        turnRemaining = 0f;
        pendingTurn = false;
        State = PatrolState.Patrolling;
        transform.position = initialPosition;
        if (body != null)
        {
            ApplyBodyConfiguration();
            body.position = initialPosition;
            StopBody();
        }
        attackController?.SetInitiallyFacingRight(initiallyMovingRight);
        Physics2D.SyncTransforms();
    }

    private bool ShouldTurn() => HasForwardStaticBlocker();

    private bool HasForwardStaticBlocker()
    {
        Bounds bounds = bodyCollider.bounds;
        float distance = settings.PatrolSpeed * Time.fixedDeltaTime + settings.ForwardProbeMargin;
        Vector2 origin = bounds.center;
        Vector2 size = new(Mathf.Max(.01f, bounds.size.x - .04f), Mathf.Max(.01f, bounds.size.y - .08f));
        foreach (RaycastHit2D hit in Physics2D.BoxCastAll(origin, size, 0f,
                     Vector2.right * movementDirection, distance))
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider.isTrigger || IsOwnCollider(collider)) continue;
            if (IsStaticSolid(collider) || collider.GetComponentInParent<Door2D>() != null) return true;
        }
        return false;
    }

    private static bool IsStaticSolid(Collider2D collider)
    {
        SurfaceSemantic2D semantic = collider.GetComponent<SurfaceSemantic2D>() ??
                                     collider.GetComponentInParent<SurfaceSemantic2D>();
        return semantic != null && semantic.Type == SurfaceSemantic2D.SurfaceType.StaticSolid &&
               semantic.IsStatic && semantic.IsSafe;
    }

    private bool IsOwnCollider(Collider2D collider)
        => collider.transform == transform || collider.transform.IsChildOf(transform);

    private void BeginTurnPause()
    {
        State = PatrolState.TurnPause;
        turnRemaining = settings.TurnPauseDuration;
        StopBody();
    }

    private void StopBody()
    {
        if (body == null) return;
        body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        body.angularVelocity = 0f;
    }

    private void ApplyBodyConfiguration()
    {
        if (body == null || settings == null) return;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = settings.GravityScale;
        body.freezeRotation = true;
    }

    private void ResolveReferences()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (attackController == null) attackController = GetComponent<HorizontalFireballEnemy2D>();
        if (bodyCollider == null)
            bodyCollider = transform.Find("BodyCollider")?.GetComponent<BoxCollider2D>();
    }

    private bool ValidateConfiguration()
    {
        bool valid = settings != null && settings.IsValid && attackController != null && body != null &&
                     bodyCollider != null && attackController.Settings == settings.AttackSettings &&
                     transform.rotation == Quaternion.identity && transform.lossyScale == Vector3.one;
        if (!valid && !configurationErrorLogged)
        {
            Debug.LogError($"Invalid patrolling horizontal fireball enemy configuration on {name}.", this);
            configurationErrorLogged = true;
        }
        return valid;
    }

    private void OnValidate()
    {
        configurationErrorLogged = false;
        ResolveReferences();
    }
}
