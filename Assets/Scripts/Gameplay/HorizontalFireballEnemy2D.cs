using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class HorizontalFireballEnemy2D : MonoBehaviour, IRoomResettable, IFreezingGroundActor2D
{
    public enum EnemyState { Watching, Windup, Cooldown, Frozen }
    public enum TargetKind { None, Player, MirrorClone }

    [Header("Shared configuration")]
    [SerializeField] private HorizontalFireballEnemySettings settings;
    [SerializeField] private HorizontalFireballProjectile2D projectilePrefab;

    [Header("Prefab references")]
    [SerializeField] private BoxCollider2D bodyCollider;
    [SerializeField] private HorizontalFireballEnemyDamageTrigger2D damageTrigger;
    [SerializeField] private Transform fireOrigin;
    [SerializeField] private SpriteRenderer bodyVisual;
    [SerializeField] private SpriteRenderer muzzleVisual;
    [SerializeField] private AudioSource feedbackAudio;

    [Header("Instance configuration")]
    [SerializeField] private bool initiallyFacingRight = true;

    private Rigidbody2D body;
    private Vector2 initialPosition;
    private float phaseRemaining;
    private float lockedDirection;
    private bool initialized;
    private bool configurationErrorLogged;
    private HorizontalFireballProjectile2D activeProjectile;
    private AudioClip windupClip;
    private AudioClip fireClip;
    private Vector3 muzzleBaseScale;

    public EnemyState State { get; private set; } = EnemyState.Watching;
    public TargetKind CurrentTarget { get; private set; } = TargetKind.None;
    public HorizontalFireballEnemySettings Settings => settings;
    public float LockedDirection => lockedDirection;
    public HorizontalFireballProjectile2D ActiveProjectile => activeProjectile;
    public bool IsDamaging => enabled && gameObject.activeInHierarchy && damageTrigger != null &&
                              damageTrigger.Trigger.enabled;
    public Rigidbody2D FreezingBody => body;
    public Collider2D FreezingCollider => bodyCollider;
    public Vector2 FreezingUpAxis => Vector2.up;

    private void Awake()
    {
        ResolveReferences();
        FreezingGroundActor2D.Ensure(gameObject);
        if (muzzleVisual != null) muzzleBaseScale = muzzleVisual.transform.localScale;
        initialPosition = transform.position;
        initialized = true;
        EnsureFeedbackAudio();
        ResetRoomState();
    }

    private void Start() => ValidateConfiguration();

    private void OnDestroy()
    {
        if (windupClip != null) Destroy(windupClip);
        if (fireClip != null) Destroy(fireClip);
    }

    private void FixedUpdate()
    {
        if (!ValidateConfiguration()) return;
        switch (State)
        {
            case EnemyState.Watching:
                StopBody();
                if (activeProjectile == null && TrySelectTarget(out TargetKind target, out float direction))
                    BeginWindup(target, direction);
                break;
            case EnemyState.Windup:
                StopBody();
                phaseRemaining = Mathf.Max(0f, phaseRemaining - Time.fixedDeltaTime);
                if (phaseRemaining <= 0f) Fire();
                break;
            case EnemyState.Cooldown:
                StopBody();
                phaseRemaining = Mathf.Max(0f, phaseRemaining - Time.fixedDeltaTime);
                if (phaseRemaining <= 0f && activeProjectile == null) BeginWatching();
                break;
            case EnemyState.Frozen:
                StopBody();
                break;
        }
        RefreshPresentation();
    }

    public void Configure(HorizontalFireballEnemySettings sharedSettings,
        HorizontalFireballProjectile2D projectile, BoxCollider2D solid,
        HorizontalFireballEnemyDamageTrigger2D damage, Transform origin,
        SpriteRenderer visual, SpriteRenderer muzzle, AudioSource audio)
    {
        settings = sharedSettings;
        projectilePrefab = projectile;
        bodyCollider = solid;
        damageTrigger = damage;
        fireOrigin = origin;
        bodyVisual = visual;
        muzzleVisual = muzzle;
        feedbackAudio = audio;
        ResolveReferences();
        if (muzzleVisual != null && muzzleBaseScale == Vector3.zero)
            muzzleBaseScale = muzzleVisual.transform.localScale;
        damageTrigger?.Configure(this);
        configurationErrorLogged = false;
        RefreshPresentation();
    }

    public void SetInitiallyFacingRight(bool value)
    {
        initiallyFacingRight = value;
        if (State == EnemyState.Watching) lockedDirection = value ? 1f : -1f;
        RefreshPresentation();
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

    public void NotifyProjectileConsumed(HorizontalFireballProjectile2D projectile)
    {
        if (activeProjectile == projectile) activeProjectile = null;
    }

    public void ResetRoomState()
    {
        ResolveReferences();
        if (!initialized)
        {
            initialPosition = transform.position;
            initialized = true;
        }
        if (activeProjectile != null) Destroy(activeProjectile.gameObject);
        activeProjectile = null;
        State = EnemyState.Watching;
        CurrentTarget = TargetKind.None;
        phaseRemaining = 0f;
        lockedDirection = initiallyFacingRight ? 1f : -1f;
        transform.position = initialPosition;
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.position = initialPosition;
            StopBody();
        }
        damageTrigger?.SetDamageEnabled(true);
        RefreshPresentation();
        Physics2D.SyncTransforms();
    }

    public void SetFreezingMovementMultiplier(float multiplier)
    {
        if (body != null && multiplier < 1f) body.linearVelocity *= multiplier;
    }

    public void CompleteFreezingGround()
    {
        if (activeProjectile != null) Destroy(activeProjectile.gameObject);
        activeProjectile = null;
        State = EnemyState.Frozen;
        CurrentTarget = TargetKind.None;
        phaseRemaining = 0f;
        StopBody();
        damageTrigger?.SetDamageEnabled(false);
        RefreshPresentation();
    }

    private void ResolveReferences()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = transform.Find("BodyCollider")?.GetComponent<BoxCollider2D>();
        if (damageTrigger == null)
            damageTrigger = GetComponentInChildren<HorizontalFireballEnemyDamageTrigger2D>(true);
        if (fireOrigin == null) fireOrigin = transform.Find("FireOrigin");
        if (bodyVisual == null) bodyVisual = transform.Find("Visual/BodyVisual")?.GetComponent<SpriteRenderer>();
        if (muzzleVisual == null) muzzleVisual = transform.Find("Visual/MuzzleVisual")?.GetComponent<SpriteRenderer>();
        if (feedbackAudio == null) feedbackAudio = GetComponent<AudioSource>();
        damageTrigger?.Configure(this);
    }

    private bool ValidateConfiguration()
    {
        bool valid = settings != null && settings.IsValid && projectilePrefab != null && body != null &&
                     bodyCollider != null && damageTrigger != null && fireOrigin != null && bodyVisual != null &&
                     muzzleVisual != null && transform.rotation == Quaternion.identity && transform.lossyScale == Vector3.one;
        if (!valid && !configurationErrorLogged)
        {
            Debug.LogError($"Invalid horizontal fireball enemy configuration on {name}.", this);
            configurationErrorLogged = true;
        }
        if (!valid) damageTrigger?.SetDamageEnabled(false);
        return valid;
    }

    private bool TrySelectTarget(out TargetKind kind, out float horizontalDirection)
    {
        kind = TargetKind.None;
        horizontalDirection = initiallyFacingRight ? 1f : -1f;
        PlayerController2D player = FindAnyObjectByType<PlayerController2D>();
        MirrorCloneController2D clone = FindAnyObjectByType<MirrorCloneController2D>();
        bool playerEligible = TryGetEligibleDistance(player, out float playerDistance, out float playerDirection);
        bool cloneEligible = TryGetEligibleDistance(clone, out float cloneDistance, out float cloneDirection);
        if (!playerEligible && !cloneEligible) return false;
        if (playerEligible && (!cloneEligible || playerDistance <= cloneDistance + .0001f))
        {
            kind = TargetKind.Player;
            horizontalDirection = playerDirection;
        }
        else
        {
            kind = TargetKind.MirrorClone;
            horizontalDirection = cloneDirection;
        }
        return true;
    }

    private bool TryGetEligibleDistance(Component target, out float horizontalDistance, out float direction)
    {
        horizontalDistance = float.PositiveInfinity;
        direction = initiallyFacingRight ? 1f : -1f;
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        Collider2D targetCollider = target.GetComponent<Collider2D>();
        Vector2 point = targetCollider != null ? targetCollider.bounds.center : target.transform.position;
        Vector2 origin = fireOrigin.position;
        Vector2 delta = point - origin;
        if (Mathf.Abs(delta.x) > settings.DetectionHalfWidth || Mathf.Abs(delta.y) > settings.DetectionHalfHeight)
            return false;
        if (!HasLineOfSight(origin, point, targetCollider)) return false;
        horizontalDistance = Mathf.Abs(delta.x);
        if (horizontalDistance > settings.DirectionTolerance) direction = Mathf.Sign(delta.x);
        return true;
    }

    private bool HasLineOfSight(Vector2 origin, Vector2 point, Collider2D targetCollider)
    {
        Vector2 delta = point - origin;
        float distance = delta.magnitude;
        if (distance <= settings.DirectionTolerance) return true;
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, delta / distance, distance))
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider.isTrigger || collider == targetCollider ||
                collider.attachedRigidbody == body) continue;
            if (collider.GetComponentInParent<PlayerController2D>() != null ||
                collider.GetComponentInParent<MirrorCloneController2D>() != null) continue;
            return false;
        }
        return true;
    }

    private void BeginWindup(TargetKind target, float direction)
    {
        State = EnemyState.Windup;
        CurrentTarget = target;
        lockedDirection = direction >= 0f ? 1f : -1f;
        phaseRemaining = settings.WindupDuration;
        PlayFeedback(windupClip);
        RefreshPresentation();
    }

    private void Fire()
    {
        if (!IsSpawnSpaceClear())
        {
            BeginCooldown();
            return;
        }
        activeProjectile = Instantiate(projectilePrefab, fireOrigin.position, Quaternion.identity);
        activeProjectile.Launch(this, fireOrigin.position, lockedDirection, settings.ProjectileSpeed,
            settings.ProjectileLifetime, settings.CameraExitMargin);
        PlayFeedback(fireClip);
        BeginCooldown();
    }

    private bool IsSpawnSpaceClear()
    {
        const float radius = .2f;
        foreach (Collider2D overlap in Physics2D.OverlapCircleAll(fireOrigin.position, radius))
        {
            if (overlap == null || overlap.isTrigger || overlap.attachedRigidbody == body) continue;
            return false;
        }
        return true;
    }

    private void BeginCooldown()
    {
        State = EnemyState.Cooldown;
        CurrentTarget = TargetKind.None;
        phaseRemaining = settings.CooldownDuration;
        RefreshPresentation();
    }

    private void BeginWatching()
    {
        State = EnemyState.Watching;
        CurrentTarget = TargetKind.None;
        phaseRemaining = 0f;
        RefreshPresentation();
    }

    private void StopBody()
    {
        if (body == null) return;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    private void RefreshPresentation()
    {
        float facing = State == EnemyState.Windup ? lockedDirection : initiallyFacingRight ? 1f : -1f;
        if (bodyVisual != null)
        {
            bodyVisual.flipX = facing < 0f;
            bodyVisual.color = State switch
            {
                EnemyState.Windup => new Color(1f, .55f, .12f, 1f),
                EnemyState.Cooldown => new Color(.45f, .2f, .14f, 1f),
                EnemyState.Frozen => new Color(.65f, .86f, 1f, 1f),
                _ => new Color(.75f, .28f, .14f, 1f)
            };
        }
        if (muzzleVisual != null)
        {
            Vector3 local = muzzleVisual.transform.localPosition;
            local.x = Mathf.Abs(local.x) * facing;
            muzzleVisual.transform.localPosition = local;
            muzzleVisual.color = State == EnemyState.Windup
                ? new Color(1f, .9f, .25f, 1f)
                : State == EnemyState.Cooldown ? new Color(.3f, .12f, .08f, 1f) : new Color(1f, .45f, .08f, 1f);
            float windupProgress = State == EnemyState.Windup && settings != null
                ? 1f - Mathf.Clamp01(phaseRemaining / settings.WindupDuration)
                : 0f;
            float size = State == EnemyState.Windup ? Mathf.Lerp(1f, 1.45f, windupProgress) : 1f;
            Vector3 baseScale = muzzleBaseScale == Vector3.zero ? Vector3.one : muzzleBaseScale;
            muzzleVisual.transform.localScale = new Vector3(baseScale.x * size, baseScale.y * size, baseScale.z);
        }
        if (fireOrigin != null)
        {
            Vector3 local = fireOrigin.localPosition;
            local.x = Mathf.Abs(local.x) * facing;
            fireOrigin.localPosition = local;
        }
    }

    private void EnsureFeedbackAudio()
    {
        if (feedbackAudio == null) feedbackAudio = GetComponent<AudioSource>();
        if (feedbackAudio == null) feedbackAudio = gameObject.AddComponent<AudioSource>();
        feedbackAudio.playOnAwake = false;
        feedbackAudio.spatialBlend = 0f;
        windupClip ??= CreateTone("Fireball Windup", 360f, 760f, .22f, .045f);
        fireClip ??= CreateTone("Fireball Launch", 920f, 300f, .12f, .065f);
    }

    private void PlayFeedback(AudioClip clip)
    {
        if (feedbackAudio != null && clip != null) feedbackAudio.PlayOneShot(clip);
    }

    private static AudioClip CreateTone(string clipName, float startFrequency, float endFrequency,
        float duration, float volume)
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float progress = i / (float)Mathf.Max(1, sampleCount - 1);
            float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
            float envelope = Mathf.Sin(Mathf.PI * progress);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * volume;
        }
        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnValidate()
    {
        configurationErrorLogged = false;
        ResolveReferences();
        RefreshPresentation();
    }
}
