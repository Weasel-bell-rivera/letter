using UnityEngine;

[CreateAssetMenu(menuName = "Mirror Puzzle/Player Movement Settings")]
public sealed class PlayerMovementSettings : ScriptableObject
{
    public float maxSpeed = 6f;
    public float groundAcceleration = 40f;
    public float groundDeceleration = 50f;
    [Range(0f, 1f)] public float airControl = 0.7f;
    public float jumpHeight = 3f;
    public float timeToApex = 0.35f;
    public float coyoteTime = 0.1f;
    public float jumpBuffer = 0.12f;
    public float maxFallSpeed = 15f;
    [Range(0.1f, 1f)] public float jumpCutMultiplier = 0.5f;

    public float Gravity => 2f * jumpHeight / (timeToApex * timeToApex);
    public float JumpSpeed => Gravity * timeToApex;
    public float ReliableJumpDistance => maxSpeed * timeToApex * 2f - 0.7f;
}
