using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class GroundFireThrowerEnemy2D : MonoBehaviour, IRoomResettable
{
    public enum EnemyState { Guarding, Windup, Cooldown }
    public enum TargetKind { None, Player, MirrorClone }

    [Header("Shared configuration")]
    [SerializeField] private GroundFireThrowerEnemySettings settings;
    [SerializeField] private ArcFireballProjectile2D projectilePrefab;

    [Header("Prefab references")]
    [SerializeField] private BoxCollider2D bodyCollider;
    [SerializeField] private SurfaceSemantic2D bodySurface;
    [SerializeField] private GroundFireThrowerDamageTrigger2D damageTrigger;
    [SerializeField] private Transform facingRoot;
    [SerializeField] private Transform lineOfSightOrigin;
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private SpriteRenderer bodyVisual;
    [SerializeField] private GameObject chargeVisual;
    [SerializeField] private GameObject targetMarker;

    [Header("Instance presentation")]
    [SerializeField] private bool initiallyFacingRight = true;

    private readonly HashSet<ArcFireballProjectile2D> ownedProjectiles = new();
    private Rigidbody2D body;
    private Collider2D damageCollider;
    private Vector2 guardPoint;
    private Vector2 lockedPoint;
    private float phaseRemaining;
    private bool initialized;
    private bool configurationErrorLogged;

    public EnemyState State { get; private set; } = EnemyState.Guarding;
    public TargetKind CurrentTarget { get; private set; } = TargetKind.None;
    public GroundFireThrowerEnemySettings Settings => settings;
    public ArcFireballProjectile2D ProjectilePrefab => projectilePrefab;
    public Vector2 GuardPoint => guardPoint;
    public Vector2 LockedPoint => lockedPoint;
    public float PhaseRemaining => phaseRemaining;
    public int ActiveProjectileCount => ownedProjectiles.Count;
    public bool IsDamaging => enabled && gameObject.activeInHierarchy && damageCollider != null &&
                              damageCollider.enabled;

    private void Awake()
    {
        ResolveReferences();
        guardPoint = transform.position;
        initialized = true;
        ResetRoomState();
    }

    private void Start() => ValidateConfiguration();

    private void FixedUpdate()
    {
        if (!ValidateConfiguration()) return;
        StopBody();

        switch (State)
        {
            case EnemyState.Guarding:
                if (TrySelectTarget(out TargetKind target, out Vector2 point))
                    BeginWindup(target, point);
                break;
            case EnemyState.Windup:
                phaseRemaining = Mathf.Max(0f, phaseRemaining - Time.fixedDeltaTime);
                RefreshWindupVisual();
                if (phaseRemaining <= 0f) ThrowFireball();
                break;
            case EnemyState.Cooldown:
                phaseRemaining = Mathf.Max(0f, phaseRemaining - Time.fixedDeltaTime);
                if (phaseRemaining <= 0f) EnterGuarding();
                break;
        }
    }

    public void Configure(GroundFireThrowerEnemySettings sharedSettings,
        ArcFireballProjectile2D fireballPrefab, BoxCollider2D solid,
        SurfaceSemantic2D semantic, GroundFireThrowerDamageTrigger2D damage,
        Transform orientationRoot, Transform sightOrigin, Transform launchOrigin,
        SpriteRenderer visual, GameObject charge, GameObject marker)
    {
        settings = sharedSettings;
        projectilePrefab = fireballPrefab;
        bodyCollider = solid;
        bodySurface = semantic;
        damageTrigger = damage;
        facingRoot = orientationRoot;
        lineOfSightOrigin = sightOrigin;
        throwOrigin = launchOrigin;
        bodyVisual = visual;
        chargeVisual = charge;
        targetMarker = marker;
        ResolveReferences();
        damageTrigger?.Configure(this);
        configurationErrorLogged = false;
        RefreshStateVisuals();
    }

    public void SetInitiallyFacingRight(bool value)
    {
        initiallyFacingRight = value;
        if (State == EnemyState.Guarding) Face(value ? Vector2.right : Vector2.left);
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
            guardPoint = body != null ? body.position : (Vector2)transform.position;
            initialized = true;
        }

        DestroyOwnedProjectiles();
        State = EnemyState.Guarding;
        CurrentTarget = TargetKind.None;
        lockedPoint = guardPoint;
        phaseRemaining = 0f;
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.position = guardPoint;
            transform.position = guardPoint;
            StopBody();
        }
        bodySurface?.Configure(SurfaceSemantic2D.SurfaceType.DynamicSurface, false, false);
        damageTrigger?.SetDamageEnabled(ValidateConfiguration());
        Face(initiallyFacingRight ? Vector2.right : Vector2.left);
        RefreshStateVisuals();
        Physics2D.SyncTransforms();
    }

    public void ForgetProjectile(ArcFireballProjectile2D projectile)
    {
        if (projectile != null) ownedProjectiles.Remove(projectile);
    }

    private bool TrySelectTarget(out TargetKind selectedKind, out Vector2 selectedPoint)
    {
        selectedKind = TargetKind.None;
        selectedPoint = default;
        Vector2 origin = lineOfSightOrigin.position;

        PlayerController2D player = FindAnyObjectByType<PlayerController2D>();
        MirrorCloneController2D clone = FindAnyObjectByType<MirrorCloneController2D>();
        bool playerValid = TryEvaluateTarget(player != null ? player.GetComponent<Collider2D>() : null,
            origin, out Vector2 playerPoint, out float playerDistance);
        bool cloneValid = TryEvaluateTarget(clone != null ? clone.GetComponent<Collider2D>() : null,
            origin, out Vector2 clonePoint, out float cloneDistance);

        if (!playerValid && !cloneValid) return false;
        if (playerValid && (!cloneValid || playerDistance <= cloneDistance + .0001f))
        {
            selectedKind = TargetKind.Player;
            selectedPoint = playerPoint;
        }
        else
        {
            selectedKind = TargetKind.MirrorClone;
            selectedPoint = clonePoint;
        }
        return true;
    }

    private bool TryEvaluateTarget(Collider2D target, Vector2 origin, out Vector2 point,
        out float distance)
    {
        point = default;
        distance = float.PositiveInfinity;
        if (target == null || !target.enabled || !target.gameObject.activeInHierarchy) return false;
        point = target.bounds.center;
        Vector2 delta = point - origin;
        distance = delta.magnitude;
        if (distance > settings.DetectionRadius) return false;
        if (distance <= .0001f) return true;

        foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, delta / distance, distance))
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider.isTrigger || collider == target ||
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
        RefreshStateVisuals();
    }

    private void ThrowFireball()
    {
        ArcFireballProjectile2D projectile = Instantiate(projectilePrefab,
            throwOrigin.position, Quaternion.identity);
        projectile.name = $"ArcFireball-{name}";
        ownedProjectiles.Add(projectile);
        projectile.Launch(this, lockedPoint, settings.ProjectileSpeed, settings.ArcHeight,
            settings.ProjectileLifetime, settings.ProjectileRadius);
        State = EnemyState.Cooldown;
        CurrentTarget = TargetKind.None;
        phaseRemaining = settings.CooldownDuration;
        RefreshStateVisuals();
    }

    private void EnterGuarding()
    {
        State = EnemyState.Guarding;
        CurrentTarget = TargetKind.None;
        phaseRemaining = 0f;
        RefreshStateVisuals();
    }

    private void Face(Vector2 direction)
    {
        if (facingRoot == null || Mathf.Abs(direction.x) <= .0001f) return;
        Vector3 scale = facingRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction.x);
        facingRoot.localScale = scale;
    }

    private void RefreshStateVisuals()
    {
        if (bodyVisual != null)
        {
            bodyVisual.color = State switch
            {
                EnemyState.Windup => new Color(1f, .62f, .24f, 1f),
                EnemyState.Cooldown => new Color(.68f, .55f, .46f, 1f),
                _ => new Color(.88f, .44f, .2f, 1f)
            };
        }
        if (chargeVisual != null) chargeVisual.SetActive(State == EnemyState.Windup);
        if (targetMarker != null) targetMarker.SetActive(State == EnemyState.Windup);
        RefreshWindupVisual();
    }

    private void RefreshWindupVisual()
    {
        if (State != EnemyState.Windup || settings == null) return;
        float progress = 1f - Mathf.Clamp01(phaseRemaining / settings.WindupDuration);
        if (chargeVisual != null)
            chargeVisual.transform.localScale = Vector3.one * Mathf.Lerp(.25f, 1f, progress);
        if (targetMarker != null)
        {
            targetMarker.transform.position = lockedPoint;
            float pulse = .9f + Mathf.Sin(progress * Mathf.PI * 4f) * .15f;
            targetMarker.transform.localScale = Vector3.one * pulse;
        }
    }

    private bool ValidateConfiguration()
    {
        bool valid = body != null && settings != null && settings.IsValid && projectilePrefab != null &&
                     bodyCollider != null && bodySurface != null && damageTrigger != null &&
                     damageCollider != null && facingRoot != null && lineOfSightOrigin != null &&
                     throwOrigin != null && bodyVisual != null && chargeVisual != null &&
                     targetMarker != null && transform.rotation == Quaternion.identity &&
                     transform.lossyScale == Vector3.one;
        if (!valid && !configurationErrorLogged)
        {
            Debug.LogError($"Invalid ground fire thrower configuration on {name}.", this);
            configurationErrorLogged = true;
        }
        if (!valid)
        {
            damageTrigger?.SetDamageEnabled(false);
            StopBody();
        }
        return valid;
    }

    private void ResolveReferences()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = transform.Find("BodyCollider")?.GetComponent<BoxCollider2D>();
        if (bodySurface == null && bodyCollider != null)
            bodySurface = bodyCollider.GetComponent<SurfaceSemantic2D>();
        if (damageTrigger == null)
            damageTrigger = GetComponentInChildren<GroundFireThrowerDamageTrigger2D>(true);
        if (damageTrigger != null)
        {
            damageTrigger.Configure(this);
            damageCollider = damageTrigger.Trigger;
        }
        if (facingRoot == null) facingRoot = transform.Find("FacingRoot");
        if (lineOfSightOrigin == null) lineOfSightOrigin = transform.Find("LineOfSightOrigin");
        if (throwOrigin == null) throwOrigin = transform.Find("FacingRoot/ThrowOrigin");
        if (bodyVisual == null)
            bodyVisual = transform.Find("FacingRoot/Visual/BodyVisual")?.GetComponent<SpriteRenderer>();
        if (chargeVisual == null) chargeVisual = transform.Find("FacingRoot/ChargeVisual")?.gameObject;
        if (targetMarker == null) targetMarker = transform.Find("TargetMarker")?.gameObject;
    }

    private void DestroyOwnedProjectiles()
    {
        ArcFireballProjectile2D[] projectiles = new ArcFireballProjectile2D[ownedProjectiles.Count];
        ownedProjectiles.CopyTo(projectiles);
        ownedProjectiles.Clear();
        foreach (ArcFireballProjectile2D projectile in projectiles)
            if (projectile != null) projectile.DestroyProjectile();
    }

    private void StopBody()
    {
        if (body == null) return;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    private void OnDestroy() => DestroyOwnedProjectiles();

    private void OnValidate()
    {
        configurationErrorLogged = false;
        ResolveReferences();
        RefreshStateVisuals();
    }

    private void OnDrawGizmosSelected()
    {
        if (settings == null) return;
        Gizmos.color = new Color(1f, .4f, .1f, .45f);
        Gizmos.DrawWireSphere(lineOfSightOrigin != null ? lineOfSightOrigin.position : transform.position,
            settings.DetectionRadius);
    }
}
