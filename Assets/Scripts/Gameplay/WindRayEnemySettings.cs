using UnityEngine;

[CreateAssetMenu(fileName = "DefaultWindRayEnemy", menuName = "W1/Enemies/Wind Ray Settings")]
public sealed class WindRayEnemySettings : ScriptableObject
{
    [Min(.01f)] [SerializeField] private float detectionRadius = 6f;
    [Min(0f)] [SerializeField] private float edgeHintDistance = .75f;
    [Min(.01f)] [SerializeField] private float windupDuration = .75f;
    [Min(.01f)] [SerializeField] private float dashSpeed = 12f;
    [Min(.01f)] [SerializeField] private float maximumDashDistance = 7f;
    [Min(0f)] [SerializeField] private float recoveryDuration = 1.5f;
    [Min(.01f)] [SerializeField] private float returnSpeed = 2f;
    [Min(.001f)] [SerializeField] private float positionTolerance = .05f;

    public float DetectionRadius => detectionRadius;
    public float EdgeHintDistance => edgeHintDistance;
    public float WindupDuration => windupDuration;
    public float DashSpeed => dashSpeed;
    public float MaximumDashDistance => maximumDashDistance;
    public float RecoveryDuration => recoveryDuration;
    public float ReturnSpeed => returnSpeed;
    public float PositionTolerance => positionTolerance;

    public bool IsValid => detectionRadius > 0f && edgeHintDistance >= 0f && windupDuration > 0f &&
                           dashSpeed > 0f && maximumDashDistance > 0f && recoveryDuration >= 0f &&
                           returnSpeed > 0f && positionTolerance > 0f;

    public void Configure(float radius, float edgeHint, float windup, float dash, float maximumDash,
        float recovery, float returning, float tolerance)
    {
        detectionRadius = radius;
        edgeHintDistance = edgeHint;
        windupDuration = windup;
        dashSpeed = dash;
        maximumDashDistance = maximumDash;
        recoveryDuration = recovery;
        returnSpeed = returning;
        positionTolerance = tolerance;
    }
}
