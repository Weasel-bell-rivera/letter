using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class PlayerController2D : MonoBehaviour, IFreezingGroundActor2D
{
    [SerializeField] private PlayerMovementSettings settings;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private LayerMask groundMask = ~0;
    private Rigidbody2D body;
    private BoxCollider2D bodyCollider;
    private float input;
    private float lastGrounded;
    private float lastJumpPressed = float.NegativeInfinity;
    private bool jumpHeld;
    private bool controlEnabled = true;
    private InputAction jumpAction;
    private Collider2D supportCollider;
    private Collider2D surfaceMotionCollider;
    private Vector2 appliedSurfaceVelocity;
    private float freezingMovementMultiplier = 1f;
    private bool frozenGroundFreezing;
    private float frozenGroundEntryX;
    private float frozenGroundTargetX;
    private float frozenGroundDirection;
    private float frozenGroundEntrySpeed;
    private float frozenGroundFreezeAmount;
    public float HorizontalInput => input;
    public bool JumpHeld => jumpHeld;
    public int JumpSequence { get; private set; }
    public int JumpInputSequence { get; private set; }
    public PlayerMovementSettings Settings => settings;
    public Transform VisualRoot => visualRoot;
    public bool FacingRight { get; private set; } = true;
    public bool IsGroundedNow => IsGrounded(Vector2.down);
    public bool IsOnFrozenGround => IsGroundedOnFrozenSurface(Vector2.down);
    public bool ControlEnabled => controlEnabled;
    public Vector2 AppliedSurfaceVelocity => appliedSurfaceVelocity;
    public Rigidbody2D FreezingBody => body;
    public Collider2D FreezingCollider => bodyCollider;
    public Vector2 FreezingUpAxis => Vector2.up;
    public float FrozenGroundFreezeAmount => frozenGroundFreezeAmount;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>(); bodyCollider = GetComponent<BoxCollider2D>(); body.freezeRotation = true; body.gravityScale = 0f;
        FreezingGroundActor2D.Ensure(gameObject);
        FreezingVisual2D.Ensure(gameObject);
        NormalizeVisualToCollider();
    }
    private void OnEnable() => BindJumpAction();
    private void Start() => BindJumpAction();
    public void Configure(Transform visual, PlayerMovementSettings movement) { visualRoot = visual; settings = movement; }
    private void NormalizeVisualToCollider()
    {
        if (visualRoot == null || bodyCollider == null) return;
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        SpriteRenderer renderer = visualRoot.GetComponentInChildren<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null) return;
        Vector2 currentSize = renderer.bounds.size;
        if (currentSize.x <= 0f || currentSize.y <= 0f) return;
        float facing = Mathf.Sign(visualRoot.localScale.x);
        if (Mathf.Approximately(facing, 0f)) facing = 1f;
        float uniformScale = bodyCollider.bounds.size.y / currentSize.y;
        visualRoot.localScale = new Vector3(facing * uniformScale, uniformScale, 1f);
    }
    public void OnMove(InputValue value) => input = controlEnabled ? value.Get<float>() : 0f;
    public void OnJump(InputValue value)
    {
        // PlayerInput's SendMessages mode does not send the canceled phase for Button
        // actions. Keep its performed notification for compatibility and recover the
        // release state through the direct action callbacks and polling below.
        SetJumpInput(value.isPressed);
    }
    public void OnResetRoom(InputValue value) { if (value.isPressed && controlEnabled) FindAnyObjectByType<RoomResetSystem>()?.ResetRoom(); }
    private void Update()
    {
        BindJumpAction();
        if (jumpAction != null && jumpHeld && !jumpAction.IsPressed()) jumpHeld = false;
        if (input != 0f) { FacingRight = input > 0f; Face(input); }
        if (IsGroundedNow) lastGrounded = Time.time;
    }
    private void FixedUpdate()
    {
        if (settings == null || !controlEnabled) return;
        Vector2 velocity = body.linearVelocity;
        bool grounded = TryGetGroundSurface(Vector2.down, out SurfaceSemantic2D groundSurface,
            out RaycastHit2D supportHit);
        supportCollider = grounded ? supportHit.collider : null;
        bool onFrozenGround = IsFrozenGround(groundSurface);
        if (!frozenGroundFreezing && onFrozenGround &&
            (Mathf.Abs(body.linearVelocity.x) > .01f || Mathf.Abs(input) > .01f))
            BeginFrozenGroundFreezing(supportHit);
        if (frozenGroundFreezing && UpdateFrozenGroundFreezing()) return;
        Vector2 nextSurfaceVelocity = SurfaceMotion2D.Resolve(supportHit, grounded, out Collider2D nextMotionCollider);
        Vector2 relativeVelocity = SurfaceMotion2D.RemoveRepeatedContribution(velocity,
            surfaceMotionCollider, nextMotionCollider, appliedSurfaceVelocity);
        float target = input * settings.maxSpeed * freezingMovementMultiplier;
        float accel = onFrozenGround
            ? 0f
            : (Mathf.Abs(target) > 0.01f ? settings.groundAcceleration : settings.groundDeceleration) * freezingMovementMultiplier;
        if (!grounded) accel *= settings.airControl;
        float relativeHorizontal = relativeVelocity.x;
        if (frozenGroundFreezing)
        {
            relativeHorizontal = frozenGroundDirection * frozenGroundEntrySpeed *
                                 Mathf.Lerp(1f, .25f, frozenGroundFreezeAmount);
        }
        else
        {
            relativeHorizontal = Mathf.MoveTowards(relativeHorizontal, target, accel * Time.fixedDeltaTime);
        }
        relativeVelocity.x = relativeHorizontal;
        surfaceMotionCollider = grounded ? nextMotionCollider : null;
        appliedSurfaceVelocity = grounded ? nextSurfaceVelocity : Vector2.zero;
        relativeVelocity.y = Mathf.Max(relativeVelocity.y - settings.Gravity * Time.fixedDeltaTime,
            -settings.maxFallSpeed);
        if (Time.time - lastJumpPressed <= settings.jumpBuffer && Time.time - lastGrounded <= settings.coyoteTime)
        { relativeVelocity.y = settings.JumpSpeed; lastJumpPressed = float.NegativeInfinity; lastGrounded = float.NegativeInfinity; JumpSequence++; }
        if (!jumpHeld && relativeVelocity.y > 0f) relativeVelocity.y *= settings.jumpCutMultiplier;
        body.linearVelocity = relativeVelocity + (grounded ? nextSurfaceVelocity : Vector2.zero);
    }
    private void Face(float direction) { if (visualRoot == null) return; Vector3 s = visualRoot.localScale; s.x = Mathf.Abs(s.x) * Mathf.Sign(direction); visualRoot.localScale = s; }
    public void SetFacing(bool right)
    {
        FacingRight = right;
        Face(right ? 1f : -1f);
    }
    private void BindJumpAction()
    {
        PlayerInput playerInput = GetComponent<PlayerInput>();
        InputAction nextJump = playerInput?.currentActionMap?.FindAction("Jump", false)
            ?? playerInput?.actions?.FindAction("Jump", false);
        if (jumpAction == nextJump) return;
        UnbindJumpAction();
        jumpAction = nextJump;
        if (jumpAction == null) return;
        jumpAction.performed += OnJumpPerformed;
        jumpAction.canceled += OnJumpCanceled;
    }
    private void UnbindJumpAction()
    {
        if (jumpAction == null) return;
        jumpAction.performed -= OnJumpPerformed;
        jumpAction.canceled -= OnJumpCanceled;
        jumpAction = null;
    }
    private void OnDisable()
    {
        UnbindJumpAction();
        jumpHeld = false;
    }
    private void OnJumpPerformed(InputAction.CallbackContext _) => SetJumpInput(true);
    private void OnJumpCanceled(InputAction.CallbackContext _) => SetJumpInput(false);
    private void SetJumpInput(bool pressed)
    {
        pressed &= controlEnabled;
        if (pressed && !jumpHeld) { lastJumpPressed = Time.time; JumpInputSequence++; }
        jumpHeld = pressed;
    }
    public bool IsGrounded(Vector2 direction)
        => TryGetGroundSurface(direction, out _, out _);

    private bool IsGroundedOnFrozenSurface(Vector2 direction)
        => TryGetGroundSurface(direction, out SurfaceSemantic2D surface, out _) && IsFrozenGround(surface);

    private bool TryGetGroundSurface(Vector2 direction, out SurfaceSemantic2D surface, out RaycastHit2D supportHit)
    {
        return SurfaceSupport2D.TryResolve(bodyCollider, gameObject, direction, .15f, groundMask,
            supportCollider, out surface, out supportHit);
    }

    private static bool IsFrozenGround(SurfaceSemantic2D surface)
        => surface != null && surface.Type == SurfaceSemantic2D.SurfaceType.FrozenGround && surface.IsSafe;

    private void BeginFrozenGroundFreezing(RaycastHit2D supportHit)
    {
        frozenGroundFreezing = true;
        frozenGroundEntryX = body.position.x;
        frozenGroundDirection = Mathf.Abs(body.linearVelocity.x) > .01f
            ? Mathf.Sign(body.linearVelocity.x)
            : Mathf.Sign(input);
        frozenGroundEntrySpeed = Mathf.Max(Mathf.Abs(body.linearVelocity.x), settings.maxSpeed * .5f);

        Tilemap tilemap = supportHit.collider != null ? supportHit.collider.GetComponent<Tilemap>() : null;
        if (tilemap == null && supportHit.collider != null)
            tilemap = supportHit.collider.GetComponentInParent<Tilemap>();
        if (tilemap != null)
        {
            Vector3 probe = new(body.position.x + frozenGroundDirection * .01f, supportHit.point.y, 0f);
            frozenGroundTargetX = tilemap.GetCellCenterWorld(tilemap.WorldToCell(probe)).x;
        }
        else
        {
            frozenGroundTargetX = Mathf.Floor(body.position.x) + .5f;
        }
        if ((frozenGroundTargetX - frozenGroundEntryX) * frozenGroundDirection < 0f)
            frozenGroundTargetX += frozenGroundDirection;
        frozenGroundFreezeAmount = 0f;
    }

    private bool UpdateFrozenGroundFreezing()
    {
        float distance = Mathf.Abs(frozenGroundTargetX - frozenGroundEntryX);
        float travelled = Mathf.Abs(body.position.x - frozenGroundEntryX);
        frozenGroundFreezeAmount = distance <= .001f ? 1f : Mathf.Clamp01(travelled / distance);
        bool reachedCenter = frozenGroundDirection > 0f
            ? body.position.x >= frozenGroundTargetX
            : body.position.x <= frozenGroundTargetX;
        if (!reachedCenter) return false;

        Vector2 centered = body.position;
        centered.x = frozenGroundTargetX;
        body.position = centered;
        body.linearVelocity = Vector2.zero;
        frozenGroundFreezeAmount = 1f;
        FindAnyObjectByType<RoomResetSystem>()?.ResetRoom();
        return true;
    }
    public void SetControlEnabled(bool value) { controlEnabled = value; if (!value) { input = 0f; jumpHeld = false; } }
    public void SetFreezingMovementMultiplier(float multiplier)
        => freezingMovementMultiplier = Mathf.Clamp01(multiplier);
    public void CompleteFreezingGround()
    {
        if (body != null) body.linearVelocity = Vector2.zero;
        FindAnyObjectByType<RoomResetSystem>()?.ResetRoom();
    }
    public void TeleportTo(Vector3 position)
    {
        transform.position = position;
        body.position = position;
        body.linearVelocity = Vector2.zero;
        supportCollider = null;
        surfaceMotionCollider = null;
        appliedSurfaceVelocity = Vector2.zero;
        freezingMovementMultiplier = 1f;
        frozenGroundFreezing = false;
        frozenGroundFreezeAmount = 0f;
        frozenGroundEntryX = 0f;
        frozenGroundTargetX = 0f;
        frozenGroundDirection = 0f;
        frozenGroundEntrySpeed = 0f;
    }
}
