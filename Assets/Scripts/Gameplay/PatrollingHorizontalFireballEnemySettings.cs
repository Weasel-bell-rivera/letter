using UnityEngine;

[CreateAssetMenu(menuName = "W1/Enemies/Patrolling Horizontal Fireball Settings")]
public sealed class PatrollingHorizontalFireballEnemySettings : ScriptableObject
{
    [SerializeField] private HorizontalFireballEnemySettings attackSettings;
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float gravityScale = 1f;
    [SerializeField] private float turnPauseDuration = .2f;
    [SerializeField] private float forwardProbeMargin = .08f;

    public HorizontalFireballEnemySettings AttackSettings => attackSettings;
    public float PatrolSpeed => patrolSpeed;
    public float GravityScale => gravityScale;
    public float TurnPauseDuration => turnPauseDuration;
    public float ForwardProbeMargin => forwardProbeMargin;
    public bool IsValid => attackSettings != null && attackSettings.IsValid && patrolSpeed > 0f &&
                           gravityScale > 0f && turnPauseDuration >= 0f && forwardProbeMargin >= 0f;

    public void Configure(HorizontalFireballEnemySettings sharedAttackSettings, float speed = 1.5f,
        float gravity = 1f, float pause = .2f, float forwardMargin = .08f)
    {
        attackSettings = sharedAttackSettings;
        patrolSpeed = speed;
        gravityScale = gravity;
        turnPauseDuration = pause;
        forwardProbeMargin = forwardMargin;
    }
}
