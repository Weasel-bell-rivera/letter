using UnityEngine;

[DefaultExecutionOrder(110)]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class WindDeflector2D : MonoBehaviour, IRoomResettable, IOrderedRoomResettable
{
    [SerializeField] private Vector2 incomingDirection = Vector2.right;
    [SerializeField] private bool initiallyClockwise;
    [SerializeField] private BoxCollider2D solidCollider;
    [SerializeField] private BoxCollider2D outputVolume;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private PressurePlate2D controlSource;

    public int ResetOrder => -58;
    public Vector2 IncomingDirection => incomingDirection.normalized;
    public bool IsClockwise { get; private set; }
    public Vector2 OutputDirection => Rotate90(IncomingDirection, IsClockwise);
    public PressurePlate2D ControlSource => controlSource;

    private void Awake()
    {
        ResolveReferences();
        ConfigureColliders();
        ResetRoomState();
    }

    private void OnEnable()
    {
        if (controlSource != null) controlSource.ActiveChanged += OnControlChanged;
    }

    private void OnDisable()
    {
        if (controlSource != null) controlSource.ActiveChanged -= OnControlChanged;
    }

    private void FixedUpdate()
    {
        if (!TryGetInputWind(out WindColumn2D source) || outputVolume == null) return;
        foreach (Collider2D overlap in Physics2D.OverlapBoxAll(outputVolume.bounds.center,
                     outputVolume.bounds.size, 0f))
        {
            Rigidbody2D actorBody = FindAffectedBody(overlap);
            if (actorBody == null || IsOutputBlocked(overlap, actorBody)) continue;
            Vector2 velocity = actorBody.linearVelocity;
            float along = Vector2.Dot(velocity, OutputDirection);
            if (along < source.Speed)
                actorBody.linearVelocity = velocity + OutputDirection * (source.Speed - along);
        }
    }

    public void Configure(Vector2 acceptedIncomingDirection, bool clockwiseInitially,
        Vector2 outputSize)
    {
        incomingDirection = acceptedIncomingDirection.sqrMagnitude > .0001f
            ? acceptedIncomingDirection.normalized
            : Vector2.right;
        initiallyClockwise = clockwiseInitially;
        ResolveReferences();
        if (outputVolume != null)
            outputVolume.size = new Vector2(Mathf.Max(.1f, outputSize.x), Mathf.Max(.1f, outputSize.y));
        ConfigureColliders();
        ResetRoomState();
    }

    public void ConfigureReferences(BoxCollider2D solid, BoxCollider2D output, SpriteRenderer renderer)
    {
        solidCollider = solid;
        outputVolume = output;
        visual = renderer;
        ConfigureColliders();
        ApplyVisual();
    }

    public void ConfigureControlSource(PressurePlate2D source)
    {
        if (isActiveAndEnabled && controlSource != null) controlSource.ActiveChanged -= OnControlChanged;
        controlSource = source;
        if (isActiveAndEnabled && controlSource != null) controlSource.ActiveChanged += OnControlChanged;
        SetClockwise(controlSource != null ? controlSource.IsActive != initiallyClockwise : initiallyClockwise);
    }

    public bool TryRedirect(Vector2 incoming, out Vector2 redirected)
    {
        redirected = default;
        if (incoming.sqrMagnitude < .0001f ||
            Vector2.Dot(incoming.normalized, IncomingDirection) < .95f) return false;
        redirected = OutputDirection;
        return true;
    }

    public bool CanReachOutput(Collider2D receiver, out float speed)
    {
        speed = 0f;
        if (receiver == null || outputVolume == null ||
            !outputVolume.bounds.Contains(receiver.bounds.center) ||
            !TryGetInputWind(out WindColumn2D source) ||
            IsOutputBlocked(receiver, receiver.attachedRigidbody)) return false;
        speed = source.Speed;
        return true;
    }

    public void ResetRoomState()
        => SetClockwise(controlSource != null ? controlSource.IsActive != initiallyClockwise : initiallyClockwise);

    private bool TryGetInputWind(out WindColumn2D source)
    {
        source = null;
        foreach (WindColumn2D candidate in FindObjectsByType<WindColumn2D>(FindObjectsSortMode.None))
        {
            if (Vector2.Dot(candidate.Direction, IncomingDirection) < .95f ||
                !candidate.CanReach(solidCollider)) continue;
            source = candidate;
            return true;
        }
        return false;
    }

    private void OnControlChanged(PressurePlate2D _, bool active)
        => SetClockwise(active != initiallyClockwise);

    private void SetClockwise(bool clockwise)
    {
        IsClockwise = clockwise;
        ApplyVisual();
    }

    private void ResolveReferences()
    {
        if (solidCollider == null) solidCollider = GetComponent<BoxCollider2D>();
        if (outputVolume == null) outputVolume = transform.Find("OutputVolume")?.GetComponent<BoxCollider2D>();
        if (visual == null) visual = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void ConfigureColliders()
    {
        if (solidCollider != null) solidCollider.isTrigger = false;
        if (outputVolume != null) outputVolume.isTrigger = true;
    }

    private static Rigidbody2D FindAffectedBody(Collider2D overlap)
    {
        PlayerController2D player = overlap != null ? overlap.GetComponentInParent<PlayerController2D>() : null;
        if (player != null) return player.GetComponent<Rigidbody2D>();
        MirrorCloneController2D clone = overlap != null ? overlap.GetComponentInParent<MirrorCloneController2D>() : null;
        return clone != null ? clone.GetComponent<Rigidbody2D>() : null;
    }

    private bool IsOutputBlocked(Collider2D actorCollider, Rigidbody2D actorBody)
    {
        Vector2 origin = (Vector2)solidCollider.bounds.center + OutputDirection * .55f;
        Vector2 target = actorCollider.bounds.center;
        Vector2 delta = target - origin;
        float distance = delta.magnitude;
        if (distance <= .01f) return false;
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, delta / distance, distance))
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider == outputVolume || collider == actorCollider || collider.isTrigger ||
                collider.attachedRigidbody == actorBody) continue;
            return true;
        }
        return false;
    }

    private void ApplyVisual()
    {
        if (visual == null) return;
        visual.color = IsClockwise
            ? new Color(.25f, .95f, .7f, 1f)
            : new Color(.3f, .75f, 1f, 1f);
        visual.transform.right = OutputDirection;
        if (outputVolume != null)
            outputVolume.transform.localPosition = (Vector3)(OutputDirection *
                (.5f + Mathf.Max(outputVolume.size.x, outputVolume.size.y) * .5f));
    }

    private static Vector2 Rotate90(Vector2 direction, bool clockwise)
        => clockwise ? new Vector2(direction.y, -direction.x) : new Vector2(-direction.y, direction.x);
}
