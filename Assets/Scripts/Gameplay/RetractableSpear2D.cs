using UnityEngine;

public sealed class RetractableSpear2D : MonoBehaviour, IRoomResettable, IOrderedRoomResettable
{
    public enum Phase { Retracted, Warning, Extending, Extended, Retracting }

    [Header("Timing")]
    [SerializeField, Min(0f)] private float retractedDuration = 1f;
    [SerializeField, Min(0f)] private float warningDuration = .55f;
    [SerializeField, Min(.01f)] private float extendDuration = .14f;
    [SerializeField, Min(0f)] private float extendedDuration = .7f;
    [SerializeField, Min(.01f)] private float retractDuration = .16f;

    [Header("Geometry")]
    [SerializeField, Min(.01f)] private float extensionDistance = 1.5f;
    [SerializeField, Range(0f, 1f)] private float hazardActivationThreshold = .15f;
    [SerializeField] private Transform movingPart;
    [SerializeField] private Hazard2D hazard;
    [SerializeField] private SpriteRenderer warningRenderer;

    private Vector3 retractedLocalPosition;
    private float timer;
    private float extension;
    private bool initialized;

    public Phase CurrentPhase { get; private set; }
    public float Extension => extension;
    // Hazard2D resets at order 0, so the cycle owner must apply its final inactive
    // state afterwards.
    public int ResetOrder => 10;

    private void Awake()
    {
        ResolveReferences();
        CaptureInitialPose();
        ResetRoomState();
    }

    private void FixedUpdate()
    {
        float remainingStep = Time.fixedDeltaTime;
        while (remainingStep > 0f)
        {
            float consumed = Mathf.Min(remainingStep, Mathf.Max(timer, 0f));
            timer -= consumed;
            remainingStep -= consumed;
            UpdateExtension();

            if (timer > 0f) break;
            AdvancePhase();
            if (consumed <= 0f && timer <= 0f) break;
        }
    }

    public void Configure(Transform animatedPart, Hazard2D damageHazard, SpriteRenderer warningVisual,
        float distance, float idle, float warning, float extend, float hold, float retract)
    {
        movingPart = animatedPart;
        hazard = damageHazard;
        warningRenderer = warningVisual;
        extensionDistance = Mathf.Max(.01f, distance);
        retractedDuration = Mathf.Max(0f, idle);
        warningDuration = Mathf.Max(0f, warning);
        extendDuration = Mathf.Max(.01f, extend);
        extendedDuration = Mathf.Max(0f, hold);
        retractDuration = Mathf.Max(.01f, retract);
        initialized = false;
        CaptureInitialPose();
        ResetRoomState();
    }

    public void ResetRoomState()
    {
        ResolveReferences();
        CaptureInitialPose();
        CurrentPhase = Phase.Retracted;
        timer = retractedDuration;
        extension = 0f;
        ApplyState();
    }

    private void AdvancePhase()
    {
        switch (CurrentPhase)
        {
            case Phase.Retracted: SetPhase(Phase.Warning, warningDuration); break;
            case Phase.Warning: SetPhase(Phase.Extending, extendDuration); break;
            case Phase.Extending: SetPhase(Phase.Extended, extendedDuration); break;
            case Phase.Extended: SetPhase(Phase.Retracting, retractDuration); break;
            default: SetPhase(Phase.Retracted, retractedDuration); break;
        }
    }

    private void SetPhase(Phase phase, float duration)
    {
        CurrentPhase = phase;
        timer = Mathf.Max(0f, duration);
        UpdateExtension();
    }

    private void UpdateExtension()
    {
        switch (CurrentPhase)
        {
            case Phase.Retracted:
            case Phase.Warning:
                extension = 0f;
                break;
            case Phase.Extending:
                extension = 1f - Mathf.Clamp01(timer / extendDuration);
                break;
            case Phase.Extended:
                extension = 1f;
                break;
            case Phase.Retracting:
                extension = Mathf.Clamp01(timer / retractDuration);
                break;
        }
        ApplyState();
    }

    private void ApplyState()
    {
        if (movingPart != null)
            movingPart.localPosition = retractedLocalPosition + Vector3.up * (extensionDistance * extension);

        bool dangerous = (CurrentPhase == Phase.Extending || CurrentPhase == Phase.Extended ||
                          CurrentPhase == Phase.Retracting) && extension >= hazardActivationThreshold;
        hazard?.SetActive(dangerous);

        if (warningRenderer != null)
            warningRenderer.color = CurrentPhase == Phase.Warning
                ? new Color(1f, .68f, .16f, 1f)
                : new Color(.34f, .38f, .44f, 1f);
    }

    private void ResolveReferences()
    {
        if (movingPart == null && transform.childCount > 0) movingPart = transform.GetChild(0);
        if (hazard == null) hazard = GetComponentInChildren<Hazard2D>(true);
    }

    private void CaptureInitialPose()
    {
        if (initialized || movingPart == null) return;
        retractedLocalPosition = movingPart.localPosition;
        initialized = true;
    }

    private void OnValidate()
    {
        retractedDuration = Mathf.Max(0f, retractedDuration);
        warningDuration = Mathf.Max(0f, warningDuration);
        extendDuration = Mathf.Max(.01f, extendDuration);
        extendedDuration = Mathf.Max(0f, extendedDuration);
        retractDuration = Mathf.Max(.01f, retractDuration);
        extensionDistance = Mathf.Max(.01f, extensionDistance);
        hazardActivationThreshold = Mathf.Clamp01(hazardActivationThreshold);
    }
}
