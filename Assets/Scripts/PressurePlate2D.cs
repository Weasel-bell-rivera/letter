using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class PressurePlate2D : MonoBehaviour, IRoomResettable
{
    public enum ActivationMode { Occupancy, FireballLatch }

    [SerializeField] private ActivationMode activationMode = ActivationMode.Occupancy;
    [SerializeField] private SpriteRenderer plateRenderer;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite pressedSprite;
    [SerializeField] private Color idleColor = new(1f, .65f, .12f, 1f);
    [SerializeField] private Color activeColor = new(.35f, 1f, .5f, 1f);
    [SerializeField] private Color latchedColor = new(.2f, .9f, 1f, 1f);

    private readonly HashSet<Rigidbody2D> occupants = new();
    private readonly HashSet<Rigidbody2D> detectedOccupants = new();
    private readonly List<Collider2D> overlapResults = new();
    private readonly ContactFilter2D overlapFilter = ContactFilter2D.noFilter;
    private Door2D legacyDoor;
    private BoxCollider2D trigger;
    private Transform visualTransform;
    private Vector3 visualRestScale;
    private Vector3 visualRestPosition;
    private bool latchedVisual;
    private bool fireballLatched;
    private bool lastReportedActive;

    public bool IsActive
    {
        get
        {
            if (activationMode == ActivationMode.FireballLatch) return fireballLatched;
            ReconcileOccupants();
            return occupants.Count > 0;
        }
    }

    public ActivationMode Mode => activationMode;
    public bool IsFireballLatched => activationMode == ActivationMode.FireballLatch && fireballLatched;
    public bool IsLatchedVisual => latchedVisual;
    public event Action<PressurePlate2D, bool> ActiveChanged;

    private void Awake()
    {
        trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        ResolveVisual();
        RefreshState(false);
    }

    // 保留旧原型的单板配置入口；实际开关交给通用Door2D处理。
    public void Configure(Collider2D door, SpriteRenderer doorVisual)
    {
        if (door == null) return;
        legacyDoor = door.GetComponent<Door2D>();
        if (legacyDoor == null) legacyDoor = door.gameObject.AddComponent<Door2D>();
        legacyDoor.Configure(false, doorVisual);
        ResolveVisual();
        RefreshState(false);
    }

    public void ConfigureVisual(SpriteRenderer renderer)
    {
        plateRenderer = renderer;
        ResolveVisual();
        RefreshState(false);
    }

    public void ConfigureStateSprites(Sprite idle, Sprite pressed)
    {
        idleSprite = idle;
        pressedSprite = pressed;
        RefreshState(false);
    }

    public void ConfigureActivationMode(ActivationMode mode)
    {
        activationMode = mode;
        occupants.Clear();
        fireballLatched = false;
        RefreshState(true);
    }

    public bool TryActivateByFireball(HorizontalFireballProjectile2D projectile)
    {
        if (projectile == null || activationMode != ActivationMode.FireballLatch) return false;
        if (!fireballLatched)
        {
            fireballLatched = true;
            RefreshState(true);
        }
        return true;
    }

    public void SetLatchedVisual(bool latched)
    {
        latchedVisual = latched;
        RefreshState(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activationMode != ActivationMode.Occupancy) return;
        Rigidbody2D body = GetValidOccupant(other);
        if (body != null && occupants.Add(body)) RefreshState(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (activationMode != ActivationMode.Occupancy) return;
        Rigidbody2D body = other.attachedRigidbody;
        if (body != null && occupants.Remove(body)) RefreshState(true);
    }

    private void FixedUpdate()
    {
        if (activationMode != ActivationMode.Occupancy) return;
        if (ReconcileOccupants()) RefreshState(true);
    }

    private static Rigidbody2D GetValidOccupant(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (body == null) return null;
        return body.GetComponent<PlayerController2D>() != null || body.GetComponent<MirrorCloneController2D>() != null
            ? body
            : null;
    }

    private bool ReconcileOccupants()
    {
        if (trigger == null) trigger = GetComponent<BoxCollider2D>();
        detectedOccupants.Clear();
        overlapResults.Clear();
        trigger.Overlap(overlapFilter, overlapResults);
        foreach (Collider2D overlap in overlapResults)
        {
            Rigidbody2D body = GetValidOccupant(overlap);
            if (body != null) detectedOccupants.Add(body);
        }

        bool changed = !occupants.SetEquals(detectedOccupants);
        if (!changed) return false;
        occupants.Clear();
        occupants.UnionWith(detectedOccupants);
        return true;
    }

    private void ResolveVisual()
    {
        if (plateRenderer == null) plateRenderer = GetComponentInChildren<SpriteRenderer>();
        if (plateRenderer == null || visualTransform == plateRenderer.transform) return;
        visualTransform = plateRenderer.transform;
        visualRestScale = visualTransform.localScale;
        visualRestPosition = visualTransform.localPosition;
    }

    private void RefreshState(bool notify)
    {
        bool active = activationMode == ActivationMode.FireballLatch ? fireballLatched : occupants.Count > 0;
        bool pressed = latchedVisual || active;

        bool usesStateSprites = idleSprite != null && pressedSprite != null;
        if (plateRenderer != null && usesStateSprites)
            plateRenderer.sprite = pressed ? pressedSprite : idleSprite;
        if (legacyDoor != null) legacyDoor.SetOpen(active);
        if (plateRenderer != null) plateRenderer.color = latchedVisual ? latchedColor : active ? activeColor : idleColor;
        if (visualTransform != null)
        {
            Vector3 scale = visualRestScale;
            if (!usesStateSprites) scale.y *= pressed ? .45f : 1f;
            visualTransform.localScale = scale;
            visualTransform.localPosition = visualRestPosition + (!usesStateSprites && pressed
                ? Vector3.down * .055f
                : Vector3.zero);
        }

        if (notify && active != lastReportedActive) ActiveChanged?.Invoke(this, active);
        lastReportedActive = active;
    }

    public void ResetRoomState()
    {
        occupants.Clear();
        fireballLatched = false;
        RefreshState(true);
    }
}
