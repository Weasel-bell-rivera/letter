using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class WindColumn2D : MonoBehaviour, IRoomResettable, IOrderedRoomResettable
{
    public enum WindMode { Constant, Periodic }
    public enum WindState { Warning, Blowing, Calm }

    public const float DefaultSpeed = 4f;
    public const float DefaultWarningDuration = .5f;
    public const float DefaultBlowingDuration = 2f;
    public const float DefaultCalmDuration = 1.5f;

    [SerializeField] private WindMode mode;
    [SerializeField] private Vector2 direction = Vector2.right;
    [SerializeField, Range(1f, 8f)] private float speed = DefaultSpeed;
    [SerializeField, Range(.2f, 2f)] private float warningDuration = DefaultWarningDuration;
    [SerializeField, Range(.5f, 6f)] private float blowingDuration = DefaultBlowingDuration;
    [SerializeField, Range(.5f, 6f)] private float calmDuration = DefaultCalmDuration;
    [SerializeField] private BoxCollider2D windVolume;
    [SerializeField] private SpriteRenderer visual;

    private WindColumnVisual2D animatedVisual;

    private float phaseRemaining;

    public int ResetOrder => -60;
    public WindMode Mode => mode;
    public WindState State { get; private set; }
    public Vector2 Direction => direction.normalized;
    public float Speed => speed;
    public bool IsBlowing => mode == WindMode.Constant || State == WindState.Blowing;

    public bool CanReach(Collider2D receiver)
    {
        if (!IsBlowing || windVolume == null || receiver == null ||
            !windVolume.bounds.Contains(receiver.bounds.center)) return false;
        return !IsBlocked(receiver, receiver.attachedRigidbody);
    }

    private void Awake()
    {
        ResolveReferences();
        if (windVolume != null) windVolume.isTrigger = true;
        ResetRoomState();
    }

    private void FixedUpdate()
    {
        AdvanceCycle();
        if (!IsBlowing || windVolume == null) return;
        foreach (Collider2D overlap in Physics2D.OverlapBoxAll(windVolume.bounds.center,
                     windVolume.bounds.size, 0f))
        {
            Rigidbody2D actorBody = FindAffectedBody(overlap);
            if (actorBody == null || IsBlocked(overlap, actorBody)) continue;
            Vector2 velocity = actorBody.linearVelocity;
            float alongWind = Vector2.Dot(velocity, Direction);
            if (alongWind < speed) actorBody.linearVelocity = velocity + Direction * (speed - alongWind);
        }
    }

    public void Configure(WindMode windMode, Vector2 worldDirection, float windSpeed,
        Vector2 volumeSize, float warning = DefaultWarningDuration,
        float blowing = DefaultBlowingDuration, float calm = DefaultCalmDuration)
    {
        mode = windMode;
        direction = worldDirection.sqrMagnitude > .0001f ? worldDirection.normalized : Vector2.right;
        speed = Mathf.Clamp(windSpeed, 1f, 8f);
        warningDuration = Mathf.Clamp(warning, .2f, 2f);
        blowingDuration = Mathf.Clamp(blowing, .5f, 6f);
        calmDuration = Mathf.Clamp(calm, .5f, 6f);
        ResolveReferences();
        if (windVolume != null)
        {
            windVolume.size = new Vector2(Mathf.Max(.1f, volumeSize.x), Mathf.Max(.1f, volumeSize.y));
            windVolume.isTrigger = true;
        }
        ResolveAnimatedVisual();
        animatedVisual?.SetWorldSize(windVolume != null ? windVolume.size : volumeSize);
        ResetRoomState();
    }

    public void ConfigureReferences(BoxCollider2D volume, SpriteRenderer windVisual)
    {
        windVolume = volume;
        visual = windVisual;
        ResolveReferences();
        if (windVolume != null) windVolume.isTrigger = true;
        ApplyVisual();
    }

    public void ResetRoomState()
    {
        State = mode == WindMode.Constant ? WindState.Blowing : WindState.Warning;
        phaseRemaining = mode == WindMode.Constant ? 0f : warningDuration;
        ApplyVisual();
    }

    private void AdvanceCycle()
    {
        if (mode == WindMode.Constant) return;
        phaseRemaining = Mathf.Max(0f, phaseRemaining - Time.fixedDeltaTime);
        if (phaseRemaining > 0f) return;
        switch (State)
        {
            case WindState.Warning:
                State = WindState.Blowing;
                phaseRemaining = blowingDuration;
                break;
            case WindState.Blowing:
                State = WindState.Calm;
                phaseRemaining = calmDuration;
                break;
            default:
                State = WindState.Warning;
                phaseRemaining = warningDuration;
                break;
        }
        ApplyVisual();
    }

    private void ResolveReferences()
    {
        if (windVolume == null) windVolume = GetComponent<BoxCollider2D>();
        if (visual == null) visual = GetComponentInChildren<SpriteRenderer>(true);
        ResolveAnimatedVisual();
    }

    private void ResolveAnimatedVisual()
    {
        if (animatedVisual == null) animatedVisual = GetComponentInChildren<WindColumnVisual2D>(true);
    }

    private Rigidbody2D FindAffectedBody(Collider2D overlap)
    {
        if (overlap == null) return null;
        PlayerController2D player = overlap.GetComponentInParent<PlayerController2D>();
        if (player != null) return player.GetComponent<Rigidbody2D>();
        MirrorCloneController2D clone = overlap.GetComponentInParent<MirrorCloneController2D>();
        return clone != null ? clone.GetComponent<Rigidbody2D>() : null;
    }

    private bool IsBlocked(Collider2D actorCollider, Rigidbody2D actorBody)
    {
        Bounds bounds = windVolume.bounds;
        Vector2 windDirection = Direction;
        float upstreamDistance = Mathf.Abs(windDirection.x) * bounds.extents.x +
                                 Mathf.Abs(windDirection.y) * bounds.extents.y;
        // Start just inside the upstream face. The volume center may sit inside a platform
        // in vertical layouts, which incorrectly makes that platform block every receiver.
        Vector2 origin = (Vector2)bounds.center - windDirection * Mathf.Max(0f, upstreamDistance - .08f);
        Vector2 target = actorCollider.bounds.center;
        Vector2 delta = target - origin;
        float distance = delta.magnitude;
        if (distance <= .01f) return false;
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, delta / distance, distance))
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider == windVolume || collider == actorCollider || collider.isTrigger ||
                collider.attachedRigidbody == actorBody) continue;
            return true;
        }
        return false;
    }

    private void ApplyVisual()
    {
        if (visual == null) return;
        ResolveAnimatedVisual();
        if (animatedVisual != null)
        {
            animatedVisual.SetDirection(Direction);
            animatedVisual.ApplyState(State, IsBlowing);
            return;
        }
        visual.transform.right = Direction;
        visual.color = IsBlowing
            ? new Color(.45f, .9f, 1f, .42f)
            : State == WindState.Warning
                ? new Color(.95f, .9f, .35f, .3f)
                : new Color(.45f, .65f, .72f, .12f);
    }
}
