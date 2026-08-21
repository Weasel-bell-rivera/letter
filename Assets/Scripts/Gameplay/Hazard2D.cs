using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class Hazard2D : MonoBehaviour, IRoomResettable
{
    [SerializeField] private bool active = true;
    private Collider2D trigger;
    public bool Active => active;

    private void Awake() { trigger = GetComponent<Collider2D>(); trigger.isTrigger = true; }
    public void SetActive(bool value) { active = value; if (trigger != null) trigger.enabled = value; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!active) return;
        if (other.TryGetComponent(out MirrorCloneController2D clone)) clone.Die();
        else if (other.TryGetComponent(out PlayerController2D player))
            FindFirstObjectByType<RoomResetSystem>()?.ResetRoom();
    }

    public void ResetRoomState() => SetActive(true);
}
