using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class Checkpoint2D : MonoBehaviour
{
    private void Awake() => GetComponent<Collider2D>().isTrigger = true;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<PlayerController2D>(out _)) return;
        FindFirstObjectByType<RoomResetSystem>()?.SetCheckpoint(transform.position);
    }
}
