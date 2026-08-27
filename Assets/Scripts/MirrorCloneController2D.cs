using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class MirrorCloneController2D : MonoBehaviour, IFreezingGroundActor2D, ISpringBounceReceiver2D
{
    private PlayerController2D source; private Rigidbody2D body; private BoxCollider2D box; private PlayerMovementSettings settings;
    private Vector2 moveAxis = Vector2.left, gravityAxis = Vector2.down; private int observedJumpInput; private bool gravityDisabled;
    private float lastGrounded = float.NegativeInfinity, lastJumpPressed = float.NegativeInfinity;
    private bool jumpConsumedSinceGrounded;
    private bool separatedFromGroundAfterJump;
    private Transform visualRoot;
    private Collider2D supportCollider;
    private Collider2D surfaceMotionCollider;
    private Vector2 appliedSurfaceVelocity;
    private float freezingMovementMultiplier = 1f;
    private Vector2 springContactVelocity;
    private bool springAntiGravityLaunchActive;
    public Vector2 GravityAxis => gravityAxis;
    public Vector2 AppliedSurfaceVelocity => appliedSurfaceVelocity;
    public Collider2D SupportCollider => supportCollider;
    public float SpringGravityMagnitude => settings != null ? settings.Gravity : 0f;
    public Vector2 SpringContactVelocity => springContactVelocity;
    public float MovementInput => source != null ? source.HorizontalInput : 0f;
    public Rigidbody2D FreezingBody => body;
    public Collider2D FreezingCollider => box;
    public Vector2 FreezingUpAxis => -gravityAxis;
    public bool IsGroundedNow => box != null && TryGetGroundSurface(out _, out _);
    public bool IsOnFrozenGround => TryGetGroundSurface(out SurfaceSemantic2D surface, out _) && IsFrozenGround(surface);
    public event Action Died;
    public void Configure(PlayerController2D player, Vector2 transformedMoveAxis, Vector2 localGravity)
    { source = player; settings = player.Settings; moveAxis = transformedMoveAxis.normalized; gravityAxis = localGravity.normalized; body = GetComponent<Rigidbody2D>(); box = GetComponent<BoxCollider2D>(); body.gravityScale = 0f; body.freezeRotation = true; observedJumpInput = source.JumpInputSequence; visualRoot = transform.Find("Visual"); supportCollider = null; surfaceMotionCollider = null; appliedSurfaceVelocity = Vector2.zero; freezingMovementMultiplier = 1f; springContactVelocity = Vector2.zero; springAntiGravityLaunchActive = false; FreezingGroundActor2D.Ensure(gameObject); FreezingVisual2D.Ensure(gameObject); }
    private void FixedUpdate()
    {
        if (source == null || settings == null) return;
        Vector2 velocity = body.linearVelocity;
        bool grounded = TryGetGroundSurface(out SurfaceSemantic2D groundSurface, out RaycastHit2D supportHit);
        supportCollider = grounded ? supportHit.collider : null;
        Vector2 nextSurfaceVelocity = SurfaceMotion2D.Resolve(supportHit, grounded, out Collider2D nextMotionCollider);
        if (!grounded && jumpConsumedSinceGrounded)
            separatedFromGroundAfterJump = true;
        float relativeUpwardSpeed = Vector2.Dot(velocity - nextSurfaceVelocity, -gravityAxis);
        if (grounded && (!jumpConsumedSinceGrounded ||
                         (separatedFromGroundAfterJump && relativeUpwardSpeed <= 0f)))
        {
            lastGrounded = Time.time;
            jumpConsumedSinceGrounded = false;
            separatedFromGroundAfterJump = false;
        }
        if (observedJumpInput != source.JumpInputSequence) { observedJumpInput = source.JumpInputSequence; lastJumpPressed = Time.time; }
        Vector2 relativeVelocity = SurfaceMotion2D.RemoveRepeatedContribution(velocity,
            surfaceMotionCollider, nextMotionCollider, appliedSurfaceVelocity);
        float along = Vector2.Dot(relativeVelocity, moveAxis), target = source.HorizontalInput * settings.maxSpeed * freezingMovementMultiplier;
        float accel = IsFrozenGround(groundSurface)
            ? 0f
            : (Mathf.Abs(target) > .01f ? settings.groundAcceleration : settings.groundDeceleration) *
              (grounded ? 1f : settings.airControl) * freezingMovementMultiplier;
        relativeVelocity += moveAxis * (Mathf.MoveTowards(along, target, accel * Time.fixedDeltaTime) - along);
        if (!gravityDisabled) relativeVelocity += gravityAxis * settings.Gravity * Time.fixedDeltaTime;
        float falling = Vector2.Dot(relativeVelocity, gravityAxis);
        if (falling > settings.maxFallSpeed) relativeVelocity -= gravityAxis * (falling - settings.maxFallSpeed);
        if (!jumpConsumedSinceGrounded &&
            Time.time - lastJumpPressed <= settings.jumpBuffer &&
            Time.time - lastGrounded <= settings.coyoteTime)
        {
            relativeVelocity -= gravityAxis * settings.JumpSpeed;
            lastJumpPressed = float.NegativeInfinity;
            lastGrounded = float.NegativeInfinity;
            jumpConsumedSinceGrounded = true;
            separatedFromGroundAfterJump = false;
        }
        if (springAntiGravityLaunchActive && Vector2.Dot(relativeVelocity, -gravityAxis) <= 0f)
            springAntiGravityLaunchActive = false;
        if (!source.JumpHeld && Vector2.Dot(relativeVelocity, -gravityAxis) > 0f &&
            !springAntiGravityLaunchActive)
        { float upward = Vector2.Dot(relativeVelocity, -gravityAxis); relativeVelocity += gravityAxis * upward * (1f - settings.jumpCutMultiplier); }
        if (Mathf.Abs(target) > .01f && visualRoot != null)
        {
            Vector3 scale = visualRoot.localScale;
            float facing = Mathf.Abs(moveAxis.x) > .01f
                ? Mathf.Sign(moveAxis.x * target)
                : Mathf.Sign(target);
            scale.x = Mathf.Abs(scale.x) * facing;
            visualRoot.localScale = scale;
        }
        surfaceMotionCollider = grounded ? nextMotionCollider : null;
        appliedSurfaceVelocity = grounded ? nextSurfaceVelocity : Vector2.zero;
        body.linearVelocity = relativeVelocity + (grounded ? nextSurfaceVelocity : Vector2.zero);
        springContactVelocity = body.linearVelocity;
    }
    private bool TryGetGroundSurface(out SurfaceSemantic2D surface, out RaycastHit2D supportHit)
    {
        return SurfaceSupport2D.TryResolve(box, gameObject, gravityAxis, .15f, ~0,
            supportCollider, out surface, out supportHit);
    }
    private static bool IsFrozenGround(SurfaceSemantic2D surface)
        => surface != null && surface.Type == SurfaceSemantic2D.SurfaceType.FrozenGround && surface.IsSafe;
    public void SetGravityDisabled(bool value) { gravityDisabled = value; if (value && body != null) body.linearVelocity -= gravityAxis * Vector2.Dot(body.linearVelocity, gravityAxis); }
    public void SetFreezingMovementMultiplier(float multiplier)
        => freezingMovementMultiplier = Mathf.Clamp01(multiplier);
    public void CompleteFreezingGround()
    {
        if (body != null) body.linearVelocity = Vector2.zero;
        Die();
    }
    public bool ApplySpringBounce(Vector2 outwardNormal, float launchSpeed)
    {
        if (body == null || outwardNormal.sqrMagnitude < .0001f || launchSpeed <= 0f) return false;
        Vector2 normal = outwardNormal.normalized;
        Vector2 incomingVelocity = springContactVelocity;
        Vector2 result = incomingVelocity - normal * Vector2.Dot(incomingVelocity, normal) + normal * launchSpeed;
        body.linearVelocity = result;
        springContactVelocity = result;
        lastJumpPressed = float.NegativeInfinity;
        lastGrounded = float.NegativeInfinity;
        if (Vector2.Dot(normal, -gravityAxis) >= .65f)
        {
            springAntiGravityLaunchActive = true;
            supportCollider = null;
            surfaceMotionCollider = null;
            appliedSurfaceVelocity = Vector2.zero;
        }
        return true;
    }
    public void Die() { Died?.Invoke(); Destroy(gameObject); }
}
