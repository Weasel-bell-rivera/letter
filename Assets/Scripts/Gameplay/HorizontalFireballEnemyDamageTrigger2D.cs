using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class HorizontalFireballEnemyDamageTrigger2D : MonoBehaviour
{
    [SerializeField] private HorizontalFireballEnemy2D owner;
    private Collider2D trigger;

    public Collider2D Trigger => trigger != null ? trigger : trigger = GetComponent<Collider2D>();

    private void Awake()
    {
        Trigger.isTrigger = true;
        if (owner == null) owner = GetComponentInParent<HorizontalFireballEnemy2D>();
    }

    public void Configure(HorizontalFireballEnemy2D enemy)
    {
        owner = enemy;
        Trigger.isTrigger = true;
    }

    public void SetDamageEnabled(bool value) => Trigger.enabled = value;

    private void OnTriggerEnter2D(Collider2D other) => owner?.HandleCharacterContact(other);
}
