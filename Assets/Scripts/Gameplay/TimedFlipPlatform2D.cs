using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SurfaceSemantic2D))]
[DefaultExecutionOrder(-200)]
public sealed class TimedFlipPlatform2D : MonoBehaviour, ISurfaceMotionProvider2D,
    IRoomResettable, IOrderedRoomResettable
{
    public enum Phase { Stable, Warning, Flipping }

    [SerializeField, Min(.01f)] private float stableDuration = 1.5f;
    [SerializeField, Min(0f)] private float warningDuration = .4f;
    [SerializeField, Min(.01f)] private float flipDuration = .35f;
    [SerializeField] private bool initiallyRunning = true;
    [SerializeField] private Transform visual;
    [SerializeField] private SpriteRenderer platformRenderer;

    private Rigidbody2D body;
    private BoxCollider2D platformCollider;
    private SurfaceSemantic2D surfaceSemantic;
    private Vector3 initialVisualScale;
    private Color initialColor;
    private float timer;
    private bool running;
    private bool reverseFace;
    private bool initialized;

    public Phase CurrentPhase { get; private set; }
    public float FlipProgress { get; private set; }
    public bool IsSolid => platformCollider != null && platformCollider.enabled;
    public int ResetOrder => -100;

    private void Awake()
    {
        ResolveReferences();
        CaptureInitialVisual();
        ResetRoomState();
    }

    private void FixedUpdate()
    {
        if (!running) return;
        timer -= Time.fixedDeltaTime;

        if (CurrentPhase == Phase.Flipping)
        {
            FlipProgress = 1f - Mathf.Clamp01(timer / flipDuration);
            ApplyVisual();
        }

        if (timer > 0f) return;
        if (CurrentPhase == Phase.Stable) SetPhase(Phase.Warning, warningDuration);
        else if (CurrentPhase == Phase.Warning) SetPhase(Phase.Flipping, flipDuration);
        else
        {
            reverseFace = !reverseFace;
            SetPhase(Phase.Stable, stableDuration);
        }
    }

    public void Configure(Transform visualRoot, SpriteRenderer renderer,
        float stable, float warning, float flip, bool runInitially = true)
    {
        visual = visualRoot;
        platformRenderer = renderer;
        stableDuration = Mathf.Max(.01f, stable);
        warningDuration = Mathf.Max(0f, warning);
        flipDuration = Mathf.Max(.01f, flip);
        initiallyRunning = runInitially;
        initialized = false;
        CaptureInitialVisual();
        ResetRoomState();
    }

    public void SetRunning(bool value)
    {
        running = value;
        if (!running && CurrentPhase == Phase.Flipping) SetPhase(Phase.Stable, stableDuration);
    }

    public bool TryGetSurfaceVelocity(Vector2 contactPoint, Vector2 supportNormal, out Vector2 velocity)
    {
        velocity = Vector2.zero;
        return IsSolid && surfaceSemantic != null;
    }

    public void ResetRoomState()
    {
        ResolveReferences();
        CaptureInitialVisual();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        surfaceSemantic.Configure(SurfaceSemantic2D.SurfaceType.DynamicSurface, false, true);
        running = initiallyRunning;
        reverseFace = false;
        SetPhase(Phase.Stable, stableDuration);
        Physics2D.SyncTransforms();
    }

    private void SetPhase(Phase phase, float duration)
    {
        CurrentPhase = phase;
        timer = Mathf.Max(0f, duration);
        FlipProgress = phase == Phase.Flipping ? 0f : 1f;
        if (platformCollider != null) platformCollider.enabled = phase != Phase.Flipping;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (visual != null)
        {
            float edgeScale = CurrentPhase == Phase.Flipping
                ? Mathf.Abs(Mathf.Cos(FlipProgress * Mathf.PI))
                : 1f;
            float faceSign = reverseFace ? -1f : 1f;
            if (CurrentPhase == Phase.Flipping && FlipProgress > .5f) faceSign *= -1f;
            visual.localScale = new Vector3(initialVisualScale.x * faceSign,
                initialVisualScale.y * Mathf.Max(.04f, edgeScale), initialVisualScale.z);
        }

        if (platformRenderer != null)
            platformRenderer.color = CurrentPhase == Phase.Warning
                ? new Color(1f, .72f, .18f, 1f)
                : initialColor;
    }

    private void ResolveReferences()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (platformCollider == null) platformCollider = GetComponent<BoxCollider2D>();
        if (surfaceSemantic == null) surfaceSemantic = GetComponent<SurfaceSemantic2D>();
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
        if (platformRenderer == null && visual != null) platformRenderer = visual.GetComponent<SpriteRenderer>();
    }

    private void CaptureInitialVisual()
    {
        if (initialized || visual == null) return;
        initialVisualScale = visual.localScale;
        initialColor = platformRenderer != null ? platformRenderer.color : Color.white;
        initialized = true;
    }

    private void OnValidate()
    {
        stableDuration = Mathf.Max(.01f, stableDuration);
        warningDuration = Mathf.Max(0f, warningDuration);
        flipDuration = Mathf.Max(.01f, flipDuration);
    }
}
