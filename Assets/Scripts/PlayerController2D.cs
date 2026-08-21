using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class PlayerController2D : MonoBehaviour
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
    public float HorizontalInput => input;
    public bool JumpHeld => jumpHeld;
    public int JumpSequence { get; private set; }
    public int JumpInputSequence { get; private set; }
    public PlayerMovementSettings Settings => settings;
    public Transform VisualRoot => visualRoot;
    public bool FacingRight { get; private set; } = true;
    public bool IsGroundedNow => IsGrounded(Vector2.down);
    public bool ControlEnabled => controlEnabled;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>(); bodyCollider = GetComponent<BoxCollider2D>(); body.freezeRotation = true; body.gravityScale = 0f;
        NormalizeVisualToCollider();
    }
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
        Vector3 scale = visualRoot.localScale;
        scale.x *= bodyCollider.bounds.size.x / currentSize.x;
        scale.y *= bodyCollider.bounds.size.y / currentSize.y;
        visualRoot.localScale = scale;
    }
    public void OnMove(InputValue value) => input = controlEnabled ? value.Get<float>() : 0f;
    public void OnJump(InputValue value)
    {
        bool pressed = value.isPressed && controlEnabled;
        if (pressed && !jumpHeld) { lastJumpPressed = Time.time; JumpInputSequence++; }
        jumpHeld = pressed;
    }
    public void OnResetRoom(InputValue value) { if (value.isPressed && controlEnabled) FindFirstObjectByType<RoomResetSystem>()?.ResetRoom(); }
    private void Update() { if (input != 0f) { FacingRight = input > 0f; Face(input); } if (IsGroundedNow) lastGrounded = Time.time; }
    private void FixedUpdate()
    {
        if (settings == null || !controlEnabled) return;
        Vector2 velocity = body.linearVelocity;
        float target = input * settings.maxSpeed;
        float accel = Mathf.Abs(target) > 0.01f ? settings.groundAcceleration : settings.groundDeceleration;
        if (!IsGroundedNow) accel *= settings.airControl;
        velocity.x = Mathf.MoveTowards(velocity.x, target, accel * Time.fixedDeltaTime);
        velocity.y = Mathf.Max(velocity.y - settings.Gravity * Time.fixedDeltaTime, -settings.maxFallSpeed);
        if (Time.time - lastJumpPressed <= settings.jumpBuffer && Time.time - lastGrounded <= settings.coyoteTime)
        { velocity.y = settings.JumpSpeed; lastJumpPressed = float.NegativeInfinity; lastGrounded = float.NegativeInfinity; JumpSequence++; }
        if (!jumpHeld && velocity.y > 0f) velocity.y *= settings.jumpCutMultiplier;
        body.linearVelocity = velocity;
    }
    private void Face(float direction) { if (visualRoot == null) return; Vector3 s = visualRoot.localScale; s.x = Mathf.Abs(s.x) * Mathf.Sign(direction); visualRoot.localScale = s; }
    public bool IsGrounded(Vector2 direction)
    {
        if (bodyCollider == null) return false;
        Bounds b = bodyCollider.bounds;
        foreach (RaycastHit2D hit in Physics2D.BoxCastAll(b.center, b.size * new Vector2(0.8f, 0.9f), 0f, direction, 0.15f, groundMask))
            if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger) return true;
        return false;
    }
    public void SetControlEnabled(bool value) { controlEnabled = value; if (!value) input = 0f; }
    public void TeleportTo(Vector3 position) { transform.position = position; body.position = position; body.linearVelocity = Vector2.zero; }
}
