using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public sealed class PressurePlate2D : MonoBehaviour, IRoomResettable
{
    private readonly HashSet<Rigidbody2D> occupants = new();
    private Collider2D doorCollider;
    private SpriteRenderer doorRenderer;
    private SpriteRenderer plateRenderer;

    public void Configure(Collider2D door, SpriteRenderer doorVisual)
    {
        doorCollider = door;
        doorRenderer = doorVisual;
        plateRenderer = GetComponent<SpriteRenderer>();

        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        RefreshState();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody == null)
            return;

        occupants.Add(other.attachedRigidbody);
        RefreshState();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.attachedRigidbody == null)
            return;

        occupants.Remove(other.attachedRigidbody);
        RefreshState();
    }

    private void FixedUpdate()
    {
        // 镜像在压力板上被右键删除时，Unity 不保证一定发送 TriggerExit。
        if (occupants.RemoveWhere(item => item == null) > 0)
            RefreshState();
    }

    private void RefreshState()
    {
        bool pressed = occupants.Count > 0;

        if (doorCollider != null)
            doorCollider.enabled = !pressed;

        if (doorRenderer != null)
        {
            Color doorColor = doorRenderer.color;
            doorColor.a = pressed ? 0.18f : 1f;
            doorRenderer.color = doorColor;
        }

        if (plateRenderer != null)
        {
            plateRenderer.color = pressed
                ? new Color(0.35f, 1f, 0.5f, 1f)
                : new Color(1f, 0.65f, 0.12f, 1f);
        }
    }

    public void ResetRoomState()
    {
        occupants.Clear();
        RefreshState();
    }
}
