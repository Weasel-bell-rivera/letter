using UnityEngine;

[CreateAssetMenu(menuName = "W1/Enemies/Horizontal Fireball Settings")]
public sealed class HorizontalFireballEnemySettings : ScriptableObject
{
    [SerializeField] private float detectionHalfWidth = 6f;
    [SerializeField] private float detectionHalfHeight = .75f;
    [SerializeField] private float windupDuration = .6f;
    [SerializeField] private float cooldownDuration = 1.4f;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileLifetime = 2f;
    [SerializeField] private float cameraExitMargin = 1f;
    [SerializeField] private float directionTolerance = .05f;

    public float DetectionHalfWidth => detectionHalfWidth;
    public float DetectionHalfHeight => detectionHalfHeight;
    public float WindupDuration => windupDuration;
    public float CooldownDuration => cooldownDuration;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileLifetime => projectileLifetime;
    public float CameraExitMargin => cameraExitMargin;
    public float DirectionTolerance => directionTolerance;
    public bool IsValid => detectionHalfWidth > 0f && detectionHalfHeight > 0f &&
                           windupDuration > 0f && cooldownDuration >= 0f &&
                           projectileSpeed > 0f && projectileLifetime > 0f &&
                           cameraExitMargin >= 0f && directionTolerance >= 0f;

    public void Configure(float halfWidth, float halfHeight, float windup, float cooldown,
        float speed, float lifetime, float exitMargin, float tolerance)
    {
        detectionHalfWidth = halfWidth;
        detectionHalfHeight = halfHeight;
        windupDuration = windup;
        cooldownDuration = cooldown;
        projectileSpeed = speed;
        projectileLifetime = lifetime;
        cameraExitMargin = exitMargin;
        directionTolerance = tolerance;
    }
}
