using UnityEngine;

/// <summary>Shared, room-independent tuning for the fixed ground fire thrower.</summary>
[CreateAssetMenu(menuName = "W1/Enemies/Ground Fire Thrower Settings",
    fileName = "GroundFireThrowerEnemySettings")]
public sealed class GroundFireThrowerEnemySettings : ScriptableObject
{
    [SerializeField, Min(.5f)] private float detectionRadius = 7f;
    [SerializeField, Min(.05f)] private float windupDuration = .8f;
    [SerializeField, Min(.5f)] private float projectileSpeed = 7f;
    [SerializeField, Min(.1f)] private float arcHeight = 2f;
    [SerializeField, Min(.05f)] private float cooldownDuration = 1.8f;
    [SerializeField, Min(.1f)] private float projectileLifetime = 3f;
    [SerializeField, Range(.05f, 1f)] private float projectileRadius = .35f;

    public float DetectionRadius => detectionRadius;
    public float WindupDuration => windupDuration;
    public float ProjectileSpeed => projectileSpeed;
    public float ArcHeight => arcHeight;
    public float CooldownDuration => cooldownDuration;
    public float ProjectileLifetime => projectileLifetime;
    public float ProjectileRadius => projectileRadius;
    public bool IsValid => detectionRadius > 0f && windupDuration > 0f && projectileSpeed > 0f &&
                           arcHeight > 0f && cooldownDuration > 0f && projectileLifetime > 0f &&
                           projectileRadius > 0f;

    public void Configure(float detection, float windup, float speed, float height,
        float cooldown, float lifetime, float radius)
    {
        detectionRadius = detection;
        windupDuration = windup;
        projectileSpeed = speed;
        arcHeight = height;
        cooldownDuration = cooldown;
        projectileLifetime = lifetime;
        projectileRadius = radius;
    }
}
