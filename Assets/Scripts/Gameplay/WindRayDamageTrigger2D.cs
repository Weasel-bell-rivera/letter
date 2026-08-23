using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class WindRayDamageTrigger2D : MonoBehaviour
{
    [SerializeField] private WindRayEnemy2D owner;
    private Collider2D trigger;

    public Collider2D Trigger => trigger != null ? trigger : trigger = GetComponent<Collider2D>();

    private void Awake()
    {
        trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
        if (owner == null) owner = GetComponentInParent<WindRayEnemy2D>();
    }

    public void Configure(WindRayEnemy2D enemy)
    {
        owner = enemy;
        trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) => owner?.HandleCharacterContact(other);
}
