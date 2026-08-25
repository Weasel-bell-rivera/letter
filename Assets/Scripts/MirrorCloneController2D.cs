using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class MirrorCloneController2D : MonoBehaviour, IFreezingGroundActor2D
{
    private PlayerController2D source; private Rigidbody2D body; private BoxCollider2D box; private PlayerMovementSettings settings;
    private Vector2 moveAxis = Vector2.left, gravityAxis = Vector2.down; private int observedJumpInput; private bool gravityDisabled;
    private float lastGrounded = float.NegativeInfinity, lastJumpPressed = float.NegativeInfinity;
    private Transform visualRoot;
    private Collider2D supportCollider;
    private Collider2D surfaceMotionCollider;
    private Vector2 appliedSurfaceVelocity;
    private float freezingMovementMultiplier = 1f;
    public Vector2 GravityAxis => gravityAxis;
    public Vector2 AppliedSurfaceVelocity => appliedSurfaceVelocity;
    public float MovementInput => source != null ? source.HorizontalInput : 0f;
    public Rigidbody2D FreezingBody => body;
    public Collider2D FreezingCollider => box;
    public Vector2 FreezingUpAxis => -gravityAxis;
    public bool IsGroundedNow => box != null && TryGetGroundSurface(out _, out _);
    public bool IsOnFrozenGround => TryGetGroundSurface(out SurfaceSemantic2D surface, out _) && IsFrozenGround(surface);
    public event Action Died;
    public void Configure(PlayerController2D player, Vector2 transformedMoveAxis, Vector2 localGravity)
    { source = player; settings = player.Settings; moveAxis = transformedMoveAxis.normalized; gravityAxis = localGravity.normalized; body = GetComponent<Rigidbody2D>(); box = GetComponent<BoxCollider2D>(); body.gravityScale = 0f; body.freezeRotation = true; observedJumpInput = source.JumpInputSequence; visualRoot = transform.Find("Visual"); supportCollider = null; surfaceMotionCollider = null; appliedSurfaceVelocity = Vector2.zero; freezingMovementMultiplier = 1f; FreezingGroundActor2D.Ensure(gameObject); FreezingVisual2D.Ensure(gameObject); }
    private void FixedUpdate()
    {
        if (source == null || settings == null) return;
        Vector2 velocity = body.linearVelocity;
        bool grounded = TryGetGroundSurface(out SurfaceSemantic2D groundSurface, out RaycastHit2D supportHit);
        supportCollider = grounded ? supportHit.collider : null;
        if (grounded) lastGrounded = Time.time;
        Vector2 nextSurfaceVelocity = SurfaceMotion2D.Resolve(supportHit, grounded, out Collider2D nextMotionCollider);
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
        if (Time.time - lastJumpPressed <= settings.jumpBuffer && Time.time - lastGrounded <= settings.coyoteTime)
        { relativeVelocity -= gravityAxis * settings.JumpSpeed; lastJumpPressed = float.NegativeInfinity; lastGrounded = float.NegativeInfinity; }
        if (!source.JumpHeld && Vector2.Dot(relativeVelocity, -gravityAxis) > 0f)
        { float upward = Vector2.Dot(relativeVelocity, -gravityAxis); relativeVelocity += gravityAxis * upward * (1f - settings.jumpCutMultiplier); }
        if (Mathf.Abs(target) > .01f && visualRoot != null) { Vector3 s = visualRoot.localScale; s.x = Mathf.Abs(s.x) * Mathf.Sign(target); visualRoot.localScale = s; }
        surfaceMotionCollider = grounded ? nextMotionCollider : null;
        appliedSurfaceVelocity = grounded ? nextSurfaceVelocity : Vector2.zero;
        body.linearVelocity = relativeVelocity + (grounded ? nextSurfaceVelocity : Vector2.zero);
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
    public void Die() { Died?.Invoke(); Destroy(gameObject); }
}
