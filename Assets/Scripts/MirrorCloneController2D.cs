using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class MirrorCloneController2D : MonoBehaviour
{
    private PlayerController2D source; private Rigidbody2D body; private BoxCollider2D box; private PlayerMovementSettings settings;
    private Vector2 moveAxis = Vector2.left, gravityAxis = Vector2.down; private int observedJumpInput; private bool gravityDisabled;
    private float lastGrounded = float.NegativeInfinity, lastJumpPressed = float.NegativeInfinity;
    private Transform visualRoot;
    public event Action Died;
    public void Configure(PlayerController2D player, Vector2 transformedMoveAxis, Vector2 localGravity)
    { source = player; settings = player.Settings; moveAxis = transformedMoveAxis.normalized; gravityAxis = localGravity.normalized; body = GetComponent<Rigidbody2D>(); box = GetComponent<BoxCollider2D>(); body.gravityScale = 0f; body.freezeRotation = true; observedJumpInput = source.JumpInputSequence; visualRoot = transform.Find("Visual"); }
    private void FixedUpdate()
    {
        if (source == null || settings == null) return;
        Vector2 velocity = body.linearVelocity;
        bool grounded = IsGrounded(); if (grounded) lastGrounded = Time.time;
        if (observedJumpInput != source.JumpInputSequence) { observedJumpInput = source.JumpInputSequence; lastJumpPressed = Time.time; }
        float along = Vector2.Dot(velocity, moveAxis), target = source.HorizontalInput * settings.maxSpeed;
        float accel = (Mathf.Abs(target) > .01f ? settings.groundAcceleration : settings.groundDeceleration) * (grounded ? 1f : settings.airControl);
        velocity += moveAxis * (Mathf.MoveTowards(along, target, accel * Time.fixedDeltaTime) - along);
        if (!gravityDisabled) velocity += gravityAxis * settings.Gravity * Time.fixedDeltaTime;
        float falling = Vector2.Dot(velocity, gravityAxis);
        if (falling > settings.maxFallSpeed) velocity -= gravityAxis * (falling - settings.maxFallSpeed);
        if (Time.time - lastJumpPressed <= settings.jumpBuffer && Time.time - lastGrounded <= settings.coyoteTime)
        { velocity -= gravityAxis * settings.JumpSpeed; lastJumpPressed = float.NegativeInfinity; lastGrounded = float.NegativeInfinity; }
        if (!source.JumpHeld && Vector2.Dot(velocity, -gravityAxis) > 0f)
        { float upward = Vector2.Dot(velocity, -gravityAxis); velocity += gravityAxis * upward * (1f - settings.jumpCutMultiplier); }
        if (Mathf.Abs(target) > .01f && visualRoot != null) { Vector3 s = visualRoot.localScale; s.x = Mathf.Abs(s.x) * Mathf.Sign(target); visualRoot.localScale = s; }
        body.linearVelocity = velocity;
    }
    private bool IsGrounded()
    {
        Bounds b = box.bounds;
        foreach (RaycastHit2D hit in Physics2D.BoxCastAll(b.center, b.size * new Vector2(.8f,.9f), 0f, gravityAxis, .15f))
            if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger && hit.collider.GetComponent<MirrorCloneController2D>() == null && hit.collider.GetComponent<PlayerController2D>() == null) return true;
        return false;
    }
    public void SetGravityDisabled(bool value) { gravityDisabled = value; if (value && body != null) body.linearVelocity -= gravityAxis * Vector2.Dot(body.linearVelocity, gravityAxis); }
    public void Die() { Died?.Invoke(); Destroy(gameObject); }
}
