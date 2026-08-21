using UnityEngine;

[RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
public sealed class GravityDisablePickup2D : MonoBehaviour, IRoomResettable
{
    private MirrorPlayer2D mirrorSystem;
    private CircleCollider2D pickupCollider;
    private SpriteRenderer pickupRenderer;
    private bool collected;

    private void Awake()
    {
        pickupCollider = GetComponent<CircleCollider2D>();
        pickupRenderer = GetComponent<SpriteRenderer>();
        pickupCollider.isTrigger = true;
    }

    public void Configure(MirrorPlayer2D system)
    {
        mirrorSystem = system;
        if (pickupCollider == null) pickupCollider = GetComponent<CircleCollider2D>();
        pickupCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController2D player = other.GetComponent<PlayerController2D>();
        TryCollect(player);
    }

    public bool TryCollect(PlayerController2D player)
    {
        if (collected || player == null || !player.enabled || !player.ControlEnabled || Time.timeScale <= 0f || mirrorSystem == null) return false;
        collected = true;
        mirrorSystem.DisableMirrorGravity();
        pickupCollider.enabled = false;
        if (pickupRenderer != null) pickupRenderer.enabled = false;
        return true;
    }

    public void ResetRoomState()
    {
        collected = false;
        mirrorSystem?.ClearTemporaryEffects();
        if (pickupCollider != null) pickupCollider.enabled = true;
        if (pickupRenderer != null) pickupRenderer.enabled = true;
    }
}
