using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class Door2D : MonoBehaviour, IRoomResettable
{
    [SerializeField] private bool initiallyOpen;
    private BoxCollider2D solid;
    public bool IsOpen { get; private set; }
    private void Awake() { solid = GetComponent<BoxCollider2D>(); SetOpen(initiallyOpen); }
    public void SetOpen(bool open) { IsOpen = open; if (solid != null) solid.enabled = !open; }
    public void ResetRoomState() => SetOpen(initiallyOpen);
}
