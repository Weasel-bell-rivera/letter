using System;
using UnityEngine;

[DefaultExecutionOrder(120)]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class WindTurbineSwitch2D : MonoBehaviour, IRoomResettable, IOrderedRoomResettable
{
    [SerializeField] private Vector2 acceptedDirection = Vector2.right;
    [SerializeField] private BoxCollider2D receiverVolume;
    [SerializeField] private SpriteRenderer rotorVisual;
    [SerializeField] private Door2D controlledDoor;
    [SerializeField] private float activeRotationSpeed = 360f;

    private bool configurationErrorLogged;
    private Quaternion initialRotorRotation;

    public int ResetOrder => -57;
    public Vector2 AcceptedDirection => acceptedDirection.normalized;
    public bool IsActive { get; private set; }
    public Door2D ControlledDoor => controlledDoor;
    public event Action<WindTurbineSwitch2D, bool> ActiveChanged;

    private void Awake()
    {
        ResolveReferences();
        if (receiverVolume != null) receiverVolume.isTrigger = true;
        if (rotorVisual != null) initialRotorRotation = rotorVisual.transform.localRotation;
        ResetRoomState();
    }

    private void FixedUpdate()
    {
        bool active = ValidateConfiguration() && HasMatchingWind();
        SetActive(active);
        if (active && rotorVisual != null)
            rotorVisual.transform.Rotate(0f, 0f, activeRotationSpeed * Time.fixedDeltaTime);
    }

    public void Configure(Vector2 windDirection, Door2D door = null)
    {
        acceptedDirection = windDirection.sqrMagnitude > .0001f ? windDirection.normalized : Vector2.right;
        controlledDoor = door;
        configurationErrorLogged = false;
        ResolveReferences();
        if (receiverVolume != null) receiverVolume.isTrigger = true;
        ResetRoomState();
    }

    public void ConfigureReferences(BoxCollider2D receiver, SpriteRenderer rotor)
    {
        receiverVolume = receiver;
        rotorVisual = rotor;
        ResolveReferences();
        if (receiverVolume != null) receiverVolume.isTrigger = true;
        if (rotorVisual != null) initialRotorRotation = rotorVisual.transform.localRotation;
        ApplyVisual();
    }

    public void ConfigureControlledDoor(Door2D door)
    {
        controlledDoor = door;
        configurationErrorLogged = false;
        controlledDoor?.SetOpen(IsActive);
    }

    public void ResetRoomState()
    {
        SetActive(false);
        if (rotorVisual != null) rotorVisual.transform.localRotation = initialRotorRotation;
    }

    private bool HasMatchingWind()
    {
        foreach (WindColumn2D wind in FindObjectsByType<WindColumn2D>(FindObjectsSortMode.None))
            if (Vector2.Dot(wind.Direction, AcceptedDirection) >= .95f && wind.CanReach(receiverVolume))
                return true;

        foreach (WindDeflector2D deflector in FindObjectsByType<WindDeflector2D>(FindObjectsSortMode.None))
            if (Vector2.Dot(deflector.OutputDirection, AcceptedDirection) >= .95f &&
                deflector.CanReachOutput(receiverVolume, out _)) return true;
        return false;
    }

    private bool ValidateConfiguration()
    {
        bool valid = receiverVolume != null && acceptedDirection.sqrMagnitude > .0001f &&
                     activeRotationSpeed >= 0f &&
                     (controlledDoor == null || controlledDoor.ControlSource == null);
        if (!valid && !configurationErrorLogged)
        {
            Debug.LogError($"Invalid WindTurbineSwitch2D configuration on {name}.", this);
            configurationErrorLogged = true;
        }
        return valid;
    }

    private void SetActive(bool active)
    {
        if (IsActive == active)
        {
            controlledDoor?.SetOpen(active);
            ApplyVisual();
            return;
        }
        IsActive = active;
        controlledDoor?.SetOpen(active);
        ApplyVisual();
        ActiveChanged?.Invoke(this, active);
    }

    private void ResolveReferences()
    {
        if (receiverVolume == null) receiverVolume = GetComponent<BoxCollider2D>();
        if (rotorVisual == null) rotorVisual = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void ApplyVisual()
    {
        if (rotorVisual != null)
            rotorVisual.color = IsActive
                ? new Color(.35f, 1f, .55f, 1f)
                : new Color(.38f, .62f, .7f, 1f);
    }
}
