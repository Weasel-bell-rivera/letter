using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class Door2D : MonoBehaviour, IRoomResettable, IOrderedRoomResettable
{
    public enum VisualState { Closed, TemporaryOpen, LatchedOpen }

    [SerializeField] private bool initiallyOpen;
    [SerializeField] private SpriteRenderer doorRenderer;
    [SerializeField] private SpriteRenderer doorTopRenderer;
    [SerializeField] private Sprite closedBodySprite;
    [SerializeField] private Sprite closedTopSprite;
    [SerializeField] private Sprite openBodySprite;
    [SerializeField] private Sprite openTopSprite;
    [SerializeField] private PressurePlate2D controlSource;
    [SerializeField] private Color closedColor = new(.9f, .22f, .16f, 1f);
    [SerializeField] private Color temporaryOpenColor = new(.45f, 1f, .45f, .18f);
    [SerializeField] private Color latchedOpenColor = new(.2f, .9f, 1f, .18f);

    private BoxCollider2D solid;
    private VisualState requestedState;

    public bool IsOpen { get; private set; }
    public bool IsWaitingToClose => requestedState == VisualState.Closed && IsOpen;
    public VisualState State { get; private set; }
    public PressurePlate2D ControlSource => controlSource;
    public int ResetOrder => 50;

    private void Awake()
    {
        ResolveReferences();
        SetState(initiallyOpen ? VisualState.TemporaryOpen : VisualState.Closed);
    }

    private void OnEnable()
    {
        if (controlSource != null) controlSource.ActiveChanged += OnControlSourceChanged;
    }

    private void OnDisable()
    {
        if (controlSource != null) controlSource.ActiveChanged -= OnControlSourceChanged;
    }

    private void FixedUpdate()
    {
        if (controlSource != null) SetOpen(controlSource.IsActive);
        if (IsWaitingToClose) TryApplyRequestedState();
    }

    public void Configure(bool open, SpriteRenderer visual = null)
    {
        initiallyOpen = open;
        if (visual != null) doorRenderer = visual;
        ResolveReferences();
        SetState(open ? VisualState.TemporaryOpen : VisualState.Closed);
    }

    public void ConfigureVisuals(SpriteRenderer bodyRenderer, SpriteRenderer topRenderer,
        Sprite closedBody, Sprite closedTop, Sprite openBody, Sprite openTop)
    {
        doorRenderer = bodyRenderer;
        doorTopRenderer = topRenderer;
        closedBodySprite = closedBody;
        closedTopSprite = closedTop;
        openBodySprite = openBody;
        openTopSprite = openTop;
        ApplyVisual(State);
    }

    public void ConfigureControlSource(PressurePlate2D source)
    {
        if (isActiveAndEnabled && controlSource != null) controlSource.ActiveChanged -= OnControlSourceChanged;
        controlSource = source;
        if (isActiveAndEnabled && controlSource != null) controlSource.ActiveChanged += OnControlSourceChanged;
        if (Application.isPlaying) SetOpen(controlSource != null ? controlSource.IsActive : initiallyOpen);
    }

    public void SetOpen(bool open) => SetState(open ? VisualState.TemporaryOpen : VisualState.Closed);

    public void SetState(VisualState state)
    {
        requestedState = state;
        TryApplyRequestedState();
    }

    private void ResolveReferences()
    {
        if (solid == null) solid = GetComponent<BoxCollider2D>();
        if (doorRenderer == null) doorRenderer = GetComponentInChildren<SpriteRenderer>();
        if (doorTopRenderer == null)
            doorTopRenderer = transform.Find("TopVisual")?.GetComponent<SpriteRenderer>();
    }

    private void TryApplyRequestedState()
    {
        ResolveReferences();
        // Anti-crush only delays an open door that has received a close request.
        // A character touching an already closed door is never an open signal.
        if (requestedState == VisualState.Closed && IsOpen && IsBlockedByCharacter())
        {
            ApplyVisual(State == VisualState.LatchedOpen ? VisualState.LatchedOpen : VisualState.TemporaryOpen);
            IsOpen = true;
            if (solid != null) solid.enabled = false;
            return;
        }

        State = requestedState;
        IsOpen = State != VisualState.Closed;
        if (solid != null) solid.enabled = !IsOpen;
        ApplyVisual(State);
    }

    private bool IsBlockedByCharacter()
    {
        if (solid == null) return false;
        Vector3 scale = transform.lossyScale;
        Vector2 size = new(Mathf.Abs(solid.size.x * scale.x), Mathf.Abs(solid.size.y * scale.y));
        Vector2 center = transform.TransformPoint(solid.offset);
        float angle = transform.eulerAngles.z;
        foreach (Collider2D overlap in Physics2D.OverlapBoxAll(center, size, angle))
        {
            Rigidbody2D body = overlap.attachedRigidbody;
            if (body == null) continue;
            if (body.GetComponent<PlayerController2D>() != null || body.GetComponent<MirrorCloneController2D>() != null ||
                body.GetComponent<FreezablePatrolEnemy2D>() != null || body.GetComponent<WindRayEnemy2D>() != null ||
                body.GetComponent<VerticalWallPatrolEnemy2D>() != null ||
                body.GetComponent<HorizontalFireballEnemy2D>() != null)
                return true;
        }
        return false;
    }

    private void ApplyVisual(VisualState state)
    {
        if (doorRenderer == null) return;
        bool open = state != VisualState.Closed;
        if (open && openBodySprite != null) doorRenderer.sprite = openBodySprite;
        else if (!open && closedBodySprite != null) doorRenderer.sprite = closedBodySprite;
        if (doorTopRenderer != null)
        {
            if (open && openTopSprite != null) doorTopRenderer.sprite = openTopSprite;
            else if (!open && closedTopSprite != null) doorTopRenderer.sprite = closedTopSprite;
        }

        Color color = state switch
        {
            VisualState.Closed => closedColor,
            VisualState.LatchedOpen => latchedOpenColor,
            _ => temporaryOpenColor
        };
        doorRenderer.color = color;
        if (doorTopRenderer != null) doorTopRenderer.color = color;
    }

    private void OnControlSourceChanged(PressurePlate2D _, bool active) => SetOpen(active);

    public void ResetRoomState() => SetState(controlSource != null && controlSource.IsActive
        ? VisualState.TemporaryOpen
        : initiallyOpen ? VisualState.TemporaryOpen : VisualState.Closed);
}
