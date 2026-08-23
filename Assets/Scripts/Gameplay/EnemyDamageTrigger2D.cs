using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class EnemyDamageTrigger2D : MonoBehaviour
{
    [SerializeField] private FreezablePatrolEnemy2D owner;
    private Collider2D trigger;

    public bool DamageEnabled => trigger != null && trigger.enabled;

    private void Awake()
    {
        trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
        if (owner == null) owner = GetComponentInParent<FreezablePatrolEnemy2D>();
    }

    public void Configure(FreezablePatrolEnemy2D enemy)
    {
        owner = enemy;
        if (trigger == null) trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    public void SetDamageEnabled(bool enabled)
    {
        if (trigger == null) trigger = GetComponent<Collider2D>();
        trigger.enabled = enabled;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == null || !owner.IsDamaging) return;
        if (other.TryGetComponent(out MirrorCloneController2D clone))
            clone.Die();
        else if (other.TryGetComponent<PlayerController2D>(out _))
            FindFirstObjectByType<RoomResetSystem>()?.ResetRoom();
    }
}
