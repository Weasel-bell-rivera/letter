using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class GroundFireThrowerDamageTrigger2D : MonoBehaviour
{
    [SerializeField] private GroundFireThrowerEnemy2D owner;
    private Collider2D trigger;

    public Collider2D Trigger => trigger != null ? trigger : trigger = GetComponent<Collider2D>();

    private void Awake()
    {
        trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
        if (owner == null) owner = GetComponentInParent<GroundFireThrowerEnemy2D>();
    }

    public void Configure(GroundFireThrowerEnemy2D enemy)
    {
        owner = enemy;
        trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    public void SetDamageEnabled(bool value)
    {
        if (Trigger != null) Trigger.enabled = value;
    }

    private void OnTriggerEnter2D(Collider2D other) => owner?.HandleCharacterContact(other);
}
