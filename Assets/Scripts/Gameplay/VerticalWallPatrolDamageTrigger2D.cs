using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class VerticalWallPatrolDamageTrigger2D : MonoBehaviour
{
    [SerializeField] private VerticalWallPatrolEnemy2D owner;
    private Collider2D trigger;

    public Collider2D Trigger => trigger != null ? trigger : trigger = GetComponent<Collider2D>();

    private void Awake()
    {
        trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
        if (owner == null) owner = GetComponentInParent<VerticalWallPatrolEnemy2D>();
    }

    public void Configure(VerticalWallPatrolEnemy2D enemy)
    {
        owner = enemy;
        trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    public void SetDamageEnabled(bool enabled) => Trigger.enabled = enabled;

    private void OnTriggerEnter2D(Collider2D other) => owner?.HandleCharacterContact(other);
}
