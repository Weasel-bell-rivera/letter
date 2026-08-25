using UnityEngine;

/// <summary>Moves a non-solid lava Trigger through a deterministic vertical cycle.</summary>
public sealed class RisingLava2D : MonoBehaviour, IRoomResettable, IOrderedRoomResettable
{
    public enum Phase { Warning, Rising, TopHold, Falling, BottomHold }

    [SerializeField] private Transform movingRoot;
    [SerializeField, Min(0.01f)] private float riseHeight = 4f;
    [SerializeField, Min(0.01f)] private float warningDuration = 1f;
    [SerializeField, Min(0.01f)] private float risingDuration = 2f;
    [SerializeField, Min(0.01f)] private float topHoldDuration = 1.5f;
    [SerializeField, Min(0.01f)] private float fallingDuration = 2f;
    [SerializeField, Min(0.01f)] private float bottomHoldDuration = 2.5f;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Phase initialPhase = Phase.Warning;

    private Vector3 bottomLocalPosition;
    private float phaseTimer;
    private bool bottomCaptured;

    public Phase CurrentPhase { get; private set; }
    public float PhaseProgress { get; private set; }
    public int ResetOrder => 10;

    private void Awake()
    {
        if (movingRoot == null) movingRoot = transform;
        bottomLocalPosition = movingRoot.localPosition;
        bottomCaptured = true;
        ResetRoomState();
    }

    private void Update()
    {
        phaseTimer += Time.deltaTime;
        for (int transitions = 0; transitions < 8; transitions++)
        {
            float duration = Duration(CurrentPhase);
            if (phaseTimer < duration) break;
            phaseTimer -= duration;
            CurrentPhase = Next(CurrentPhase);
            ApplyVisual();
        }
        PhaseProgress = Mathf.Clamp01(phaseTimer / Duration(CurrentPhase));
        ApplyPosition();
    }

    public void Configure(Transform moving, float height, float warning, float rising,
        float topHold, float falling, float bottomHold, Phase startPhase = Phase.Warning)
    {
        movingRoot = moving;
        riseHeight = Mathf.Max(.01f, height);
        warningDuration = Mathf.Max(.01f, warning);
        risingDuration = Mathf.Max(.01f, rising);
        topHoldDuration = Mathf.Max(.01f, topHold);
        fallingDuration = Mathf.Max(.01f, falling);
        bottomHoldDuration = Mathf.Max(.01f, bottomHold);
        initialPhase = startPhase;
    }

    public void ResetRoomState()
    {
        if (movingRoot == null) movingRoot = transform;
        if (!bottomCaptured)
        {
            bottomLocalPosition = movingRoot.localPosition;
            bottomCaptured = true;
        }
        SetPhase(initialPhase);
    }

    private void SetPhase(Phase phase)
    {
        CurrentPhase = phase;
        phaseTimer = 0f;
        PhaseProgress = 0f;
        ApplyPosition();
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (visual != null)
            visual.color = CurrentPhase == Phase.Warning ? new Color(1f, .82f, .45f) : Color.white;
    }

    private void ApplyPosition()
    {
        float height01 = CurrentPhase switch
        {
            Phase.Rising => PhaseProgress,
            Phase.TopHold => 1f,
            Phase.Falling => 1f - PhaseProgress,
            _ => 0f
        };
        movingRoot.localPosition = bottomLocalPosition + Vector3.up * (riseHeight * height01);
    }

    private float Duration(Phase phase) => phase switch
    {
        Phase.Warning => warningDuration,
        Phase.Rising => risingDuration,
        Phase.TopHold => topHoldDuration,
        Phase.Falling => fallingDuration,
        _ => bottomHoldDuration
    };

    private static Phase Next(Phase phase) => phase switch
    {
        Phase.Warning => Phase.Rising,
        Phase.Rising => Phase.TopHold,
        Phase.TopHold => Phase.Falling,
        Phase.Falling => Phase.BottomHold,
        _ => Phase.Warning
    };
}
