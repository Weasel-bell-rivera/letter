using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[DefaultExecutionOrder(-200)]
public sealed class PressurePlate2D : MonoBehaviour, IRoomResettable, ISurfaceMotionProvider2D
{
    public enum ActivationMode { Occupancy, FireballLatch, DescendingLatch }

    [SerializeField] private ActivationMode activationMode = ActivationMode.Occupancy;
    [SerializeField] private SpriteRenderer plateRenderer;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite pressedSprite;
    [SerializeField] private Color idleColor = new(1f, .65f, .12f, 1f);
    [SerializeField] private Color activeColor = new(.35f, 1f, .5f, 1f);
    [SerializeField] private Color latchedColor = new(.2f, .9f, 1f, 1f);

    [SerializeField] private string permanentSwitchId;
    [SerializeField, Min(.01f)] private float descentDuration = 1f;
    [SerializeField, Min(.01f)] private float returnDuration = .2f;
    [SerializeField] private Rigidbody2D descendingBody;
    [SerializeField] private BoxCollider2D standingSurface;
    [SerializeField, Min(0f)] private float descentDistance = .6f;
    private Vector2 raisedPosition;
    private Vector2 surfaceVelocity;
    private bool bodyInitialized;
    private bool descentLatched;

    public bool TryGetSurfaceVelocity(Vector2 point, Vector2 normal, out Vector2 velocity)
    {
        velocity = surfaceVelocity;
        return descendingBody != null && standingSurface != null;
    }

    private void MoveStandingSurface(bool instant)
    {
        if (descendingBody == null) return;
        if (!bodyInitialized)
        {
            raisedPosition = descendingBody.position;
            bodyInitialized = true;
        }
        Vector2 target = raisedPosition - (Vector2)transform.up * (descentDistance * pressProgress);
        surfaceVelocity = instant ? Vector2.zero : (target - descendingBody.position) / Time.fixedDeltaTime;
        if (instant)
        {
            descendingBody.position = target;
            descendingBody.linearVelocity = Vector2.zero;
        }
        else descendingBody.MovePosition(target);
    }
    private float descentElapsed;
    private bool saveInitialized;
    private bool configurationErrorLogged;

    public bool IsPermanentlyLatched => activationMode == ActivationMode.DescendingLatch && descentLatched;
    public bool IsLatchedSignal => IsFireballLatched || IsPermanentlyLatched;
    public float PressProgress => pressProgress;

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
    private bool visualPressed;
    private float pressProgress;

    public bool IsActive
    {
        get
        {
            if (activationMode == ActivationMode.FireballLatch) return fireballLatched;
            if (activationMode == ActivationMode.DescendingLatch)
            {
                EnsurePermanentState();
                return descentLatched;
            }
            if (ReconcileOccupants()) RefreshState(true);
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
        MoveStandingSurface(true);
        RefreshState(false);
    }

    private void Start()
    {
        if (activationMode == ActivationMode.DescendingLatch) EnsurePermanentState();
    }

    private void EnsurePermanentState()
    {
        if (saveInitialized) return;
        if (!DoorGroupId.IsValid(permanentSwitchId))
        {
            if (!configurationErrorLogged)
            {
                Debug.LogError($"Descending switch requires a unique DoorGroupId on {name}.", this);
                configurationErrorLogged = true;
            }
            return;
        }
        descentLatched = SaveService.Instance.HasLatchedDoorGroup(permanentSwitchId);
        pressProgress = descentLatched ? 1f : 0f;
        saveInitialized = true;
        MoveStandingSurface(true);
        RefreshState(true);
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
        descentLatched = false;
        descentElapsed = 0f;
        pressProgress = 0f;
        saveInitialized = false;
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
        if (activationMode == ActivationMode.FireballLatch) return;
        if (ReconcileOccupants()) RefreshState(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (activationMode == ActivationMode.FireballLatch) return;
        if (ReconcileOccupants()) RefreshState(true);
    }

    private void FixedUpdate()
    {
        if (activationMode == ActivationMode.FireballLatch) return;
        bool changed = ReconcileOccupants();
        if (activationMode == ActivationMode.DescendingLatch)
        {
            EnsurePermanentState();
            if (!saveInitialized) return;
            AdvanceDescent(occupants.Count > 0, Time.fixedDeltaTime);
            MoveStandingSurface(false);
        }
        if (changed) RefreshState(true);
    }

    private void AdvanceDescent(bool occupied, float deltaTime)
    {
        if (descentLatched) return;
        if (!occupied)
        {
            descentElapsed = 0f;
            pressProgress = Mathf.MoveTowards(pressProgress, 0f, deltaTime / Mathf.Max(.01f, returnDuration));
        }
        else
        {
            descentElapsed += deltaTime;
            float targetProgress = Mathf.Clamp01(descentElapsed / Mathf.Max(.01f, descentDuration));
            pressProgress = Mathf.MoveTowards(pressProgress, targetProgress, deltaTime / Mathf.Max(.01f, descentDuration));
            if (pressProgress >= 1f)
            {
                SaveService save = SaveService.Instance;
                if (save.TryLatchDoorGroup(permanentSwitchId) || save.HasLatchedDoorGroup(permanentSwitchId))
                    descentLatched = true;
            }
        }
        RefreshState(true);
    }

    private Rigidbody2D GetValidOccupant(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (body == null || !body.simulated || other.isTrigger) return null;
        PlayerController2D player = body.GetComponent<PlayerController2D>();
        MirrorCloneController2D clone = body.GetComponent<MirrorCloneController2D>();
        if (player == null && clone == null) return null;
        Collider2D actor = player != null ? player.FreezingCollider : clone.FreezingCollider;
        if (actor == null || !actor.enabled || !actor.gameObject.activeInHierarchy) return null;

        // Measure the whole physical body in plate space, independent of animation.
        Bounds bounds = actor.bounds;
        Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
        for (int x = 0; x < 2; x++)
        for (int y = 0; y < 2; y++)
        {
            Vector2 point = transform.InverseTransformPoint(new Vector3(
                x == 0 ? bounds.min.x : bounds.max.x,
                y == 0 ? bounds.min.y : bounds.max.y, bounds.center.z));
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        Vector2 plateMin = trigger.offset - trigger.size * .5f;
        Vector2 plateMax = trigger.offset + trigger.size * .5f;
        const float tolerance = .001f;
        if (min.x < plateMin.x - tolerance || max.x > plateMax.x + tolerance
            || min.y < plateMin.y - tolerance || min.y > plateMax.y + tolerance)
            return null;
        Vector2 actorUp = player != null ? Vector2.up : -clone.GravityAxis;
        if (Vector2.Dot(actorUp, transform.up) < .99f) return null;
        return (player != null ? player.IsGroundedNow : clone.IsGroundedNow) ? body : null;
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
        bool active = activationMode switch
        {
            ActivationMode.FireballLatch => fireballLatched,
            ActivationMode.DescendingLatch => descentLatched,
            _ => occupants.Count > 0
        };
        visualPressed = latchedVisual || active;

        if (legacyDoor != null) legacyDoor.SetOpen(active);
        if (plateRenderer != null) plateRenderer.color = latchedVisual ? latchedColor : active ? activeColor : idleColor;
        ApplyPressVisual();

        if (notify && active != lastReportedActive) ActiveChanged?.Invoke(this, active);
        lastReportedActive = active;
    }

    private void Update()
    {
        if (activationMode == ActivationMode.DescendingLatch)
        {
            ApplyPressVisual();
            return;
        }
        float duration = visualPressed ? .10f : .14f;
        pressProgress = Mathf.MoveTowards(pressProgress, visualPressed ? 1f : 0f, Time.deltaTime / duration);
        ApplyPressVisual();
    }

    private void ApplyPressVisual()
    {
        if (descendingBody != null) return; // The rigid stone moves as a whole, never scales.
        float easedPress = Mathf.SmoothStep(0f, 1f, pressProgress);
        bool usesStateSprites = idleSprite != null && pressedSprite != null;
        if (plateRenderer != null && usesStateSprites)
            plateRenderer.sprite = pressProgress >= 1f ? pressedSprite : idleSprite;
        // Animate only the visual child; the sensing volume must remain stationary.
        if (visualTransform != null && visualTransform != transform && !trigger.transform.IsChildOf(visualTransform))
        {
            Vector3 scale = visualRestScale;
            if (!usesStateSprites) scale.y *= Mathf.Lerp(1f, .45f, easedPress);
            visualTransform.localScale = scale;
            Vector3 depression = transform.TransformVector(Vector3.down * (.06f * easedPress));
            visualTransform.localPosition = visualRestPosition + (visualTransform.parent != null
                ? visualTransform.parent.InverseTransformVector(depression) : depression);
        }
    }

    public void ResetRoomState()
    {
        occupants.Clear();
        fireballLatched = false;
        descentElapsed = 0f;
        if (activationMode == ActivationMode.DescendingLatch)
        {
            saveInitialized = false;
            EnsurePermanentState();
        }
        pressProgress = latchedVisual || descentLatched ? 1f : 0f;
        MoveStandingSurface(true);
        RefreshState(true);
    }
}
