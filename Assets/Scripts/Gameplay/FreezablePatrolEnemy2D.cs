using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class FreezablePatrolEnemy2D : MonoBehaviour, IRoomResettable, IFreezingGroundActor2D
{
    public enum EnemyState { Active, Frozen }

    [Header("Prefab references")]
    [SerializeField] private BoxCollider2D bodyCollider;
    [SerializeField] private SurfaceSemantic2D bodySurface;
    [SerializeField] private EnemyDamageTrigger2D damageTrigger;
    [SerializeField] private Transform groundProbe;
    [SerializeField] private Transform surfaceProbe;
    [SerializeField] private GameObject activeVisual;
    [SerializeField] private GameObject frozenVisual;
    [SerializeField] private GameObject freezeEffect;
    [SerializeField] private AudioSource freezeAudio;
    [SerializeField] private AudioClip freezeClip;

    [Header("Patrol configuration")]
    [SerializeField] private float leftEndpoint = -2f;
    [SerializeField] private float rightEndpoint = 2f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float endpointWait = .35f;
    [SerializeField] private bool initiallyFacingRight = true;

    private Rigidbody2D body;
    private Vector2 initialPosition;
    private bool facingRight;
    private float waitRemaining;
    private bool initialized;
    private bool configurationErrorLogged;
    private bool ownsFreezeClip;
    private float freezingMovementMultiplier = 1f;

    public EnemyState State { get; private set; } = EnemyState.Active;
    public bool IsFrozen => State == EnemyState.Frozen;
    public bool IsDamaging => State == EnemyState.Active && damageTrigger != null && damageTrigger.DamageEnabled;
    public bool FacingRight => facingRight;
    public float LeftEndpoint => leftEndpoint;
    public float RightEndpoint => rightEndpoint;
    public float MoveSpeed => moveSpeed;
    public float EndpointWait => endpointWait;
    public event Action Frozen;
    public Rigidbody2D FreezingBody => body;
    public Collider2D FreezingCollider => bodyCollider;
    public Vector2 FreezingUpAxis => Vector2.up;

    private void Awake()
    {
        ResolveReferences();
        FreezingGroundActor2D.Ensure(gameObject);
        FreezingVisual2D.Ensure(gameObject);
        EnsureFreezeAudio();
        initialPosition = transform.position;
        initialized = true;
        ResetRoomState();
    }

    private void Start() => ValidateConfiguration();

    private void OnDestroy()
    {
        if (ownsFreezeClip && freezeClip != null) Destroy(freezeClip);
    }

    private void FixedUpdate()
    {
        if (!ValidateConfiguration() || State != EnemyState.Active) return;
        if (TryFreezeFromFootContact()) return;

        if (waitRemaining > 0f)
        {
            waitRemaining = Mathf.Max(0f, waitRemaining - Time.fixedDeltaTime);
            SetHorizontalVelocity(0f);
            return;
        }

        float direction = facingRight ? 1f : -1f;
        float endpoint = initialPosition.x + (facingRight ? rightEndpoint : leftEndpoint);
        bool reachedEndpoint = facingRight ? body.position.x >= endpoint : body.position.x <= endpoint;
        if (reachedEndpoint || IsWallAhead(direction) || !HasGroundAhead(direction))
        {
            Vector2 clamped = body.position;
            if (reachedEndpoint) clamped.x = endpoint;
            body.position = clamped;
            TurnAround();
            return;
        }

        SetHorizontalVelocity(direction * moveSpeed * freezingMovementMultiplier);
    }

    public void ConfigurePrefabReferences(BoxCollider2D solid, EnemyDamageTrigger2D damage,
        Transform groundCheck, Transform surfaceCheck, GameObject active, GameObject frozen, GameObject effect)
    {
        bodyCollider = solid;
        bodySurface = solid != null ? solid.GetComponent<SurfaceSemantic2D>() : null;
        damageTrigger = damage;
        groundProbe = groundCheck;
        surfaceProbe = surfaceCheck;
        activeVisual = active;
        frozenVisual = frozen;
        freezeEffect = effect;
        damageTrigger?.Configure(this);
    }

    public void ConfigurePatrol(float leftLocalEndpoint, float rightLocalEndpoint, float speed, float wait,
        bool faceRight = true)
    {
        leftEndpoint = leftLocalEndpoint;
        rightEndpoint = rightLocalEndpoint;
        moveSpeed = speed;
        endpointWait = wait;
        initiallyFacingRight = faceRight;
        configurationErrorLogged = false;
        if (Application.isPlaying && initialized)
        {
            initialPosition = transform.position;
            ResetRoomState();
        }
    }

    private void ResolveReferences()
    {
        body = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponentInChildren<BoxCollider2D>();
        if (bodySurface == null && bodyCollider != null) bodySurface = bodyCollider.GetComponent<SurfaceSemantic2D>();
        if (damageTrigger == null) damageTrigger = GetComponentInChildren<EnemyDamageTrigger2D>(true);
        if (groundProbe == null) groundProbe = transform.Find("GroundProbe");
        if (surfaceProbe == null) surfaceProbe = transform.Find("SurfaceProbe");
        if (activeVisual == null) activeVisual = transform.Find("Visual/ActiveVisual")?.gameObject;
        if (frozenVisual == null) frozenVisual = transform.Find("Visual/FrozenVisual")?.gameObject;
        if (freezeEffect == null) freezeEffect = transform.Find("Visual/FreezeEffect")?.gameObject;
        if (freezeAudio == null) freezeAudio = GetComponent<AudioSource>();
        damageTrigger?.Configure(this);
    }

    private bool ValidateConfiguration()
    {
        bool valid = body != null && bodyCollider != null && bodySurface != null && damageTrigger != null && groundProbe != null &&
                     surfaceProbe != null && leftEndpoint < rightEndpoint && moveSpeed > 0f && endpointWait >= 0f;
        if (!valid && !configurationErrorLogged)
        {
            Debug.LogError($"Invalid freezable patrol enemy configuration on {name}.", this);
            configurationErrorLogged = true;
        }
        return valid;
    }

    private bool TryFreezeFromFootContact()
    {
        if (body.linearVelocity.y > .05f) return false;
        Vector2 probeSize = new(Mathf.Max(.1f, bodyCollider.bounds.size.x * .65f), .08f);
        foreach (RaycastHit2D hit in Physics2D.BoxCastAll(surfaceProbe.position, probeSize, 0f, Vector2.down, .14f))
        {
            if (hit.collider == null || hit.collider.isTrigger || hit.collider.attachedRigidbody == body || hit.normal.y < .65f)
                continue;
            if (!SurfaceSemantic2D.TryGet(hit.collider, out SurfaceSemantic2D semantic) ||
                semantic.Type != SurfaceSemantic2D.SurfaceType.FrozenGround || !semantic.IsStatic || !semantic.IsSafe)
                continue;
            Freeze();
            return true;
        }
        return false;
    }

    private bool IsWallAhead(float direction)
    {
        Bounds bounds = bodyCollider.bounds;
        Vector2 origin = new(bounds.center.x, bounds.center.y + bounds.extents.y * .1f);
        foreach (RaycastHit2D hit in Physics2D.BoxCastAll(origin,
                     new Vector2(.05f, bounds.size.y * .7f), 0f, Vector2.right * direction, bounds.extents.x + .08f))
        {
            if (hit.collider == null || hit.collider.isTrigger || hit.collider.attachedRigidbody == body) continue;
            if (hit.collider.GetComponent<PlayerController2D>() != null || hit.collider.GetComponent<MirrorCloneController2D>() != null)
                continue;
            return true;
        }
        return false;
    }

    private bool HasGroundAhead(float direction)
    {
        Vector2 origin = groundProbe.position + Vector3.right * (direction * bodyCollider.bounds.extents.x * .65f);
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, Vector2.down, .22f))
            if (hit.collider != null && !hit.collider.isTrigger && hit.collider.attachedRigidbody != body) return true;
        return false;
    }

    private void TurnAround()
    {
        facingRight = !facingRight;
        waitRemaining = endpointWait;
        SetHorizontalVelocity(0f);
        RefreshFacingVisual();
    }

    private void SetHorizontalVelocity(float horizontal)
    {
        Vector2 velocity = body.linearVelocity;
        velocity.x = horizontal;
        body.linearVelocity = velocity;
    }

    private void Freeze()
    {
        if (State == EnemyState.Frozen) return;
        State = EnemyState.Frozen;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        damageTrigger.SetDamageEnabled(false);
        bodySurface.Configure(SurfaceSemantic2D.SurfaceType.DynamicSurface, false, true);
        RefreshVisuals(true);
        if (freezeAudio != null && freezeClip != null) freezeAudio.PlayOneShot(freezeClip);
        Frozen?.Invoke();
    }

    public void SetFreezingMovementMultiplier(float multiplier)
        => freezingMovementMultiplier = Mathf.Clamp01(multiplier);

    public void CompleteFreezingGround() => Freeze();

    public void ResetRoomState()
    {
        ResolveReferences();
        State = EnemyState.Active;
        facingRight = initiallyFacingRight;
        waitRemaining = 0f;
        freezingMovementMultiplier = 1f;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.freezeRotation = true;
        body.gravityScale = 1f;
        transform.position = initialPosition;
        body.position = initialPosition;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        damageTrigger?.SetDamageEnabled(true);
        bodySurface?.Configure(SurfaceSemantic2D.SurfaceType.DynamicSurface, false, false);
        RefreshVisuals(false);
        RefreshFacingVisual();
        Physics2D.SyncTransforms();
    }

    private void RefreshVisuals(bool playFreezeEffect)
    {
        if (activeVisual != null) activeVisual.SetActive(State == EnemyState.Active);
        if (frozenVisual != null) frozenVisual.SetActive(State == EnemyState.Frozen);
        if (freezeEffect != null) freezeEffect.SetActive(playFreezeEffect && State == EnemyState.Frozen);
    }

    private void RefreshFacingVisual()
    {
        if (activeVisual == null) return;
        Vector3 scale = activeVisual.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingRight ? 1f : -1f);
        activeVisual.transform.localScale = scale;
    }

    private void EnsureFreezeAudio()
    {
        if (freezeAudio == null) freezeAudio = GetComponent<AudioSource>();
        if (freezeAudio == null) freezeAudio = gameObject.AddComponent<AudioSource>();
        freezeAudio.playOnAwake = false;
        freezeAudio.spatialBlend = 0f;
        if (freezeClip != null) return;

        const int sampleRate = 22050;
        const float duration = .14f;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float progress = i / (float)Mathf.Max(1, sampleCount - 1);
            float frequency = Mathf.Lerp(820f, 1180f, progress);
            float envelope = Mathf.Sin(Mathf.PI * progress);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * .06f;
        }
        freezeClip = AudioClip.Create("Enemy Freeze", sampleCount, 1, sampleRate, false);
        freezeClip.SetData(samples, 0);
        ownsFreezeClip = true;
    }
}
