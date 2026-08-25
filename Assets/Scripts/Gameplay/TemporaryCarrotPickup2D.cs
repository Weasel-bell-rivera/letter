using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class TemporaryCarrotPickup2D : MonoBehaviour, IRoomResettable
{
    [SerializeField] private SnowmanGate2D targetSnowman;
    private Collider2D trigger;
    private SpriteRenderer visual;
    public bool Collected { get; private set; }

    private void Awake()
    {
        trigger = GetComponent<Collider2D>(); trigger.isTrigger = true;
        visual = GetComponentInChildren<SpriteRenderer>();
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Collected || !other.TryGetComponent<PlayerController2D>(out _)) return;
        Collected = true;
        targetSnowman?.GiveCarrot();
        trigger.enabled = false;
        if (visual != null) visual.enabled = false;
    }
    public void Configure(SnowmanGate2D target) => targetSnowman = target;
    public void ResetRoomState()
    {
        Collected = false;
        if (trigger == null) trigger = GetComponent<Collider2D>();
        if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
        trigger.enabled = true;
        if (visual != null) visual.enabled = true;
    }
}
