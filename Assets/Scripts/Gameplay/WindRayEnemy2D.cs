using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class WindRayEnemy2D : MonoBehaviour, IRoomResettable, IFreezingGroundActor2D
{
    public enum EnemyState { Guarding, Windup, Dashing, Recovering, Returning, Frozen, Defeated }
    public enum TargetKind { None, Player, MirrorClone }
    public enum ContactOutcome { ContinueAttack, DefeatAfterHit }

    [Header("Shared configuration")]
    [SerializeField] private WindRayEnemySettings settings;

    [Header("Prefab references")]
    [SerializeField] private WindRayDamageTrigger2D damageTrigger;
    [SerializeField] private Transform lineOfSightOrigin;
    [SerializeField] private SpriteRenderer bodyVisual;
    [SerializeField] private GameObject targetMarker;
    [SerializeField] private LineRenderer dashTrail;
    [SerializeField] private AudioSource feedbackAudio;

    [Header("Instance presentation")]
    [SerializeField] private Vector2 initialVisualFacing = new(-1f, -1f);

    [Header("Enemy prototype")]
    [SerializeField] private ContactOutcome contactOutcome = ContactOutcome.ContinueAttack;

    private Rigidbody2D body;
    private Collider2D damageCollider;
    private Vector2 guardPoint;
    private Vector2 lockedPoint;
    private Vector2 dashDirection;
    private float dashRemaining;
    private float phaseRemaining;
    private bool initialized;
    private bool configurationErrorLogged;
    private AudioClip windupClip;
    private AudioClip dashClip;
    private AudioClip recoveryClip;
    private float freezingMovementMultiplier = 1f;

    public EnemyState State { get; private set; } = EnemyState.Guarding;
    public TargetKind CurrentTarget { get; private set; } = TargetKind.None;
    public WindRayEnemySettings Settings => settings;
    public Vector2 GuardPoint => guardPoint;
    public Vector2 LockedPoint => lockedPoint;
    public bool HasLockedPoint => CurrentTarget != TargetKind.None;
    public ContactOutcome OutcomeOnContact => contactOutcome;
    public float PhaseRemaining => phaseRemaining;
    public bool IsDamaging => enabled && gameObject.activeInHierarchy && damageCollider != null && damageCollider.enabled;
    public Rigidbody2D FreezingBody => body;
    public Collider2D FreezingCollider => damageCollider;
    public Vector2 FreezingUpAxis => Vector2.up;

    private void Awake()
    {
        ResolveReferences();
        FreezingGroundActor2D.Ensure(gameObject);
        guardPoint = transform.position;
        initialized = true;
        EnsureFeedbackAudio();
        ResetRoomState();
    }

    private void Start() => ValidateConfiguration();

    private void OnDestroy()
    {
        if (windupClip != null) Destroy(windupClip);
        if (dashClip != null) Destroy(dashClip);
        if (recoveryClip != null) Destroy(recoveryClip);
    }

    private void FixedUpdate()
    {
        if (!ValidateConfiguration()) return;

        RefreshProximityFeedback();
        switch (State)
        {
            case EnemyState.Guarding:
                StopBody();
                if (TrySelectTarget(out TargetKind target, out Vector2 targetPoint))
                    BeginWindup(target, targetPoint);
                break;
            case EnemyState.Windup:
                StopBody();
                phaseRemaining = Mathf.Max(0f, phaseRemaining - Time.fixedDeltaTime);
                if (phaseRemaining <= 0f) BeginDash();
                break;
            case EnemyState.Dashing:
                AdvanceDash();
                break;
            case EnemyState.Recovering:
                StopBody();
                phaseRemaining = Mathf.Max(0f, phaseRemaining - Time.fixedDeltaTime);
                if (phaseRemaining <= 0f) BeginReturn();
                break;
            case EnemyState.Returning:
                AdvanceReturn();
                break;
            case EnemyState.Frozen:
            case EnemyState.Defeated:
                StopBody();
                break;
        }
        RefreshDynamicFeedback();
    }

    public void Configure(WindRayEnemySettings sharedSettings, WindRayDamageTrigger2D damage,
        Transform sightOrigin, SpriteRenderer visual, GameObject marker, LineRenderer trail,
        AudioSource audio)
    {
        settings = sharedSettings;
        damageTrigger = damage;
        lineOfSightOrigin = sightOrigin;
        bodyVisual = visual;
        targetMarker = marker;
        dashTrail = trail;
        feedbackAudio = audio;
        ResolveReferences();
        damageTrigger?.Configure(this);
        configurationErrorLogged = false;
        RefreshStaticFeedback();
    }

    public void SetInitialVisualFacing(Vector2 direction)
    {
        if (direction.sqrMagnitude <= .0001f) return;
        initialVisualFacing = direction.normalized;
        if (State == EnemyState.Guarding) Face(initialVisualFacing);
    }

    public void SetContactOutcome(ContactOutcome outcome) => contactOutcome = outcome;

    public void HandleCharacterContact(Collider2D other)
    {
        if (!IsDamaging || other == null) return;
        MirrorCloneController2D clone = other.GetComponentInParent<MirrorCloneController2D>();
        if (clone != null)
        {
            clone.Die();
            if (contactOutcome == ContactOutcome.DefeatAfterHit) EnterDefeatedState();
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
            guardPoint = transform.position;
            initialized = true;
        }

        State = EnemyState.Guarding;
        CurrentTarget = TargetKind.None;
        lockedPoint = guardPoint;
        dashDirection = Vector2.zero;
        dashRemaining = 0f;
        phaseRemaining = 0f;
        freezingMovementMultiplier = 1f;
        transform.position = guardPoint;
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.freezeRotation = true;
            body.position = guardPoint;
            StopBody();
        }
        if (damageCollider != null) damageCollider.enabled = true;
        if (bodyVisual != null) bodyVisual.enabled = true;
        if (feedbackAudio != null) feedbackAudio.enabled = true;
        RefreshStaticFeedback();
        RefreshStateVisuals();
        Physics2D.SyncTransforms();
    }

    public void SetFreezingMovementMultiplier(float multiplier)
        => freezingMovementMultiplier = Mathf.Clamp01(multiplier);

    public void CompleteFreezingGround()
    {
        State = EnemyState.Frozen;
        CurrentTarget = TargetKind.None;
        dashDirection = Vector2.zero;
        dashRemaining = 0f;
        phaseRemaining = 0f;
        StopBody();
        if (damageCollider != null) damageCollider.enabled = false;
        RefreshStateVisuals();
    }

    private void EnterDefeatedState()
    {
        State = EnemyState.Defeated;
        CurrentTarget = TargetKind.None;
        dashDirection = Vector2.zero;
        dashRemaining = 0f;
        phaseRemaining = 0f;
        StopBody();
        if (damageCollider != null) damageCollider.enabled = false;
        if (bodyVisual != null) bodyVisual.enabled = false;
        if (targetMarker != null) targetMarker.SetActive(false);
        if (dashTrail != null) dashTrail.enabled = false;
        if (feedbackAudio != null) feedbackAudio.enabled = false;
    }

    private void ResolveReferences()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (damageTrigger == null) damageTrigger = GetComponentInChildren<WindRayDamageTrigger2D>(true);
        if (damageTrigger != null)
        {
            damageTrigger.Configure(this);
            damageCollider = damageTrigger.Trigger;
        }
        if (lineOfSightOrigin == null) lineOfSightOrigin = transform.Find("LineOfSightOrigin");
        if (bodyVisual == null) bodyVisual = transform.Find("Visual/BodyVisual")?.GetComponent<SpriteRenderer>();
        if (targetMarker == null) targetMarker = transform.Find("Visual/TargetMarker")?.gameObject;
        if (dashTrail == null) dashTrail = transform.Find("Visual/DashTrail")?.GetComponent<LineRenderer>();
        if (feedbackAudio == null) feedbackAudio = GetComponent<AudioSource>();
    }

    private bool ValidateConfiguration()
    {
        bool valid = settings != null && settings.IsValid && body != null && damageTrigger != null &&
                     damageCollider != null && lineOfSightOrigin != null && bodyVisual != null &&
                     targetMarker != null && dashTrail != null;
        if (!valid && !configurationErrorLogged)
        {
            Debug.LogError($"Invalid WindRayEnemy2D configuration on {name}.", this);
            configurationErrorLogged = true;
        }
        return valid;
    }

    private bool TrySelectTarget(out TargetKind kind, out Vector2 point)
    {
        kind = TargetKind.None;
        point = default;

        PlayerController2D player = FindAnyObjectByType<PlayerController2D>();
        MirrorCloneController2D clone = FindAnyObjectByType<MirrorCloneController2D>();
        bool playerEligible = TryGetEligiblePoint(player, out Vector2 playerPoint, out float playerDistanceSq);
        bool cloneEligible = TryGetEligiblePoint(clone, out Vector2 clonePoint, out float cloneDistanceSq);
        if (!playerEligible && !cloneEligible) return false;

        const float tieToleranceSq = .0001f;
        if (playerEligible && (!cloneEligible || playerDistanceSq <= cloneDistanceSq + tieToleranceSq))
        {
            kind = TargetKind.Player;
            point = playerPoint;
        }
        else
        {
            kind = TargetKind.MirrorClone;
            point = clonePoint;
        }
        return true;
    }

    private bool TryGetEligiblePoint(Component target, out Vector2 point, out float distanceSq)
    {
        point = default;
        distanceSq = float.PositiveInfinity;
        if (target == null || !target.gameObject.activeInHierarchy) return false;

        Collider2D targetCollider = target.GetComponent<Collider2D>();
        point = targetCollider != null ? (Vector2)targetCollider.bounds.center : (Vector2)target.transform.position;
        Vector2 origin = lineOfSightOrigin != null ? lineOfSightOrigin.position : body.position;
        distanceSq = (point - origin).sqrMagnitude;
        if (distanceSq > settings.DetectionRadius * settings.DetectionRadius) return false;
        return HasLineOfSight(origin, point, targetCollider);
    }

    private bool HasLineOfSight(Vector2 origin, Vector2 targetPoint, Collider2D targetCollider)
    {
        Vector2 delta = targetPoint - origin;
        float distance = delta.magnitude;
        if (distance <= settings.PositionTolerance) return true;

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

    private void BeginWindup(TargetKind target, Vector2 point)
    {
        State = EnemyState.Windup;
        CurrentTarget = target;
        lockedPoint = point;
        phaseRemaining = settings.WindupDuration;
        Face(lockedPoint - body.position);
        PlayFeedback(windupClip);
        RefreshStateVisuals();
    }

    private void BeginDash()
    {
        Vector2 delta = lockedPoint - body.position;
        float distance = delta.magnitude;
        if (distance <= settings.PositionTolerance)
        {
            BeginRecovery();
            return;
        }

        State = EnemyState.Dashing;
        dashDirection = delta / distance;
        dashRemaining = Mathf.Min(distance, settings.MaximumDashDistance);
        Face(dashDirection);
        PlayFeedback(dashClip);
        RefreshStateVisuals();
    }

    private void AdvanceDash()
    {
        float requested = Mathf.Min(settings.DashSpeed * freezingMovementMultiplier * Time.fixedDeltaTime,
            dashRemaining);
        float allowed = DistanceBeforeBlock(dashDirection, requested);
        if (allowed > 0f) body.MovePosition(body.position + dashDirection * allowed);
        dashRemaining = Mathf.Max(0f, dashRemaining - allowed);

        bool blocked = allowed + .0001f < requested;
        if (blocked || dashRemaining <= settings.PositionTolerance) BeginRecovery();
    }

    private void BeginRecovery()
    {
        State = EnemyState.Recovering;
        phaseRemaining = settings.RecoveryDuration;
        dashDirection = Vector2.zero;
        dashRemaining = 0f;
        StopBody();
        PlayFeedback(recoveryClip);
        RefreshStateVisuals();
    }

    private void BeginReturn()
    {
        State = EnemyState.Returning;
        phaseRemaining = 0f;
        RefreshStateVisuals();
    }

    private void AdvanceReturn()
    {
        Vector2 delta = guardPoint - body.position;
        float distance = delta.magnitude;
        if (distance <= settings.PositionTolerance)
        {
            body.position = guardPoint;
            transform.position = guardPoint;
            State = EnemyState.Guarding;
            CurrentTarget = TargetKind.None;
            lockedPoint = guardPoint;
            Face(initialVisualFacing);
            RefreshStateVisuals();
            return;
        }

        Vector2 direction = delta / distance;
        Face(direction);
        float requested = Mathf.Min(settings.ReturnSpeed * freezingMovementMultiplier * Time.fixedDeltaTime,
            distance);
        float allowed = DistanceBeforeBlock(direction, requested);
        if (allowed > 0f) body.MovePosition(body.position + direction * allowed);
    }

    private float DistanceBeforeBlock(Vector2 direction, float distance)
    {
        if (distance <= 0f || damageCollider == null) return 0f;
        Bounds bounds = damageCollider.bounds;
        float closest = distance;
        foreach (RaycastHit2D hit in Physics2D.BoxCastAll(bounds.center, bounds.size * .95f, 0f,
                     direction, distance + .03f))
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider.isTrigger || collider.attachedRigidbody == body) continue;
            if (collider.GetComponentInParent<PlayerController2D>() != null ||
                collider.GetComponentInParent<MirrorCloneController2D>() != null) continue;
            closest = Mathf.Min(closest, Mathf.Max(0f, hit.distance - .02f));
        }
        return closest;
    }

    private void StopBody()
    {
        if (body == null) return;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    private void Face(Vector2 direction)
    {
        if (bodyVisual == null || direction.sqrMagnitude <= .0001f) return;
        Transform facingRoot = bodyVisual.transform.parent != null ? bodyVisual.transform.parent : bodyVisual.transform;
        facingRoot.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private void RefreshStaticFeedback()
    {
        Face(initialVisualFacing);
    }

    private void RefreshProximityFeedback()
    {
        if (bodyVisual == null || settings == null || State != EnemyState.Guarding) return;
        float closest = DistanceToClosestCharacter();
        float hintStart = settings.DetectionRadius + settings.EdgeHintDistance;
        if (closest > settings.DetectionRadius && closest <= hintStart)
        {
            float denominator = Mathf.Max(.001f, settings.EdgeHintDistance);
            float proximity = 1f - Mathf.Clamp01((closest - settings.DetectionRadius) / denominator);
            bodyVisual.color = Color.Lerp(Color.white, new Color(.68f, .92f, 1f, 1f), proximity);
        }
        else
        {
            bodyVisual.color = Color.white;
        }
    }

    private float DistanceToClosestCharacter()
    {
        Vector2 origin = lineOfSightOrigin != null ? lineOfSightOrigin.position : body.position;
        float closest = float.PositiveInfinity;
        PlayerController2D player = FindAnyObjectByType<PlayerController2D>();
        MirrorCloneController2D clone = FindAnyObjectByType<MirrorCloneController2D>();
        if (player != null) closest = Mathf.Min(closest, Vector2.Distance(origin, player.transform.position));
        if (clone != null) closest = Mathf.Min(closest, Vector2.Distance(origin, clone.transform.position));
        return closest;
    }

    private void RefreshStateVisuals()
    {
        if (bodyVisual != null)
        {
            bodyVisual.color = State switch
            {
                EnemyState.Guarding => Color.white,
                EnemyState.Windup => new Color(1f, .82f, .42f, 1f),
                EnemyState.Dashing => new Color(1f, .55f, .5f, 1f),
                EnemyState.Recovering => new Color(.5f, .55f, .62f, 1f),
                EnemyState.Frozen => new Color(.65f, .86f, 1f, 1f),
                EnemyState.Defeated => Color.clear,
                _ => new Color(.82f, .9f, 1f, 1f)
            };
        }
        if (targetMarker != null) targetMarker.SetActive(State == EnemyState.Windup);
        if (dashTrail != null) dashTrail.enabled = State == EnemyState.Dashing;
        RefreshDynamicFeedback();
    }

    private void RefreshDynamicFeedback()
    {
        if (targetMarker != null && State == EnemyState.Windup)
            targetMarker.transform.position = lockedPoint;
        if (dashTrail != null && State == EnemyState.Dashing)
        {
            Vector3 origin = body != null ? (Vector3)body.position : transform.position;
            dashTrail.SetPosition(0, origin);
            dashTrail.SetPosition(1, origin - (Vector3)dashDirection * 1.4f);
        }
    }

    private void EnsureFeedbackAudio()
    {
        if (feedbackAudio == null) feedbackAudio = GetComponent<AudioSource>();
        if (feedbackAudio == null) feedbackAudio = gameObject.AddComponent<AudioSource>();
        feedbackAudio.playOnAwake = false;
        feedbackAudio.spatialBlend = 0f;
        windupClip ??= CreateTone("Wind Ray Warning", 520f, 760f, .18f, .045f);
        dashClip ??= CreateTone("Wind Ray Dash", 920f, 420f, .12f, .065f);
        recoveryClip ??= CreateTone("Wind Ray Recovery", 300f, 210f, .16f, .035f);
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
}
