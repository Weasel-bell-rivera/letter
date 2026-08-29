using UnityEngine;

[CreateAssetMenu(menuName = "W1/Enemies/Patrolling Horizontal Fireball Settings")]
public sealed class PatrollingHorizontalFireballEnemySettings : ScriptableObject
{
    [SerializeField] private HorizontalFireballEnemySettings attackSettings;
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float turnPauseDuration = .2f;
    [SerializeField] private float forwardProbeMargin = .08f;
    [SerializeField] private float groundProbeDistance = .16f;
    [SerializeField] private float maxGroundHeightDifference = .05f;

    public HorizontalFireballEnemySettings AttackSettings => attackSettings;
    public float PatrolSpeed => patrolSpeed;
    public float TurnPauseDuration => turnPauseDuration;
    public float ForwardProbeMargin => forwardProbeMargin;
    public float GroundProbeDistance => groundProbeDistance;
    public float MaxGroundHeightDifference => maxGroundHeightDifference;
    public bool IsValid => attackSettings != null && attackSettings.IsValid && patrolSpeed > 0f &&
                           turnPauseDuration >= 0f && forwardProbeMargin >= 0f &&
                           groundProbeDistance > 0f && maxGroundHeightDifference >= 0f;

    public void Configure(HorizontalFireballEnemySettings sharedAttackSettings, float speed = 1.5f,
        float pause = .2f, float forwardMargin = .08f, float groundDistance = .16f,
        float groundHeightDifference = .05f)
    {
        attackSettings = sharedAttackSettings;
        patrolSpeed = speed;
        turnPauseDuration = pause;
        forwardProbeMargin = forwardMargin;
        groundProbeDistance = groundDistance;
        maxGroundHeightDifference = groundHeightDifference;
    }
}
