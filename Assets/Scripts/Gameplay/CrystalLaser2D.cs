using UnityEngine;

public sealed class CrystalLaser2D : MonoBehaviour, IRoomResettable, IOrderedRoomResettable
{
    public enum Phase { Warning, Firing, Cooldown }

    [Header("Timing")]
    [SerializeField, Min(0f)] private float warningDuration = .65f;
    [SerializeField, Min(.01f)] private float firingDuration = .8f;
    [SerializeField, Min(0f)] private float cooldownDuration = 1.25f;

    [Header("Beam")]
    [SerializeField, Min(.1f)] private float beamLength = 6f;
    [SerializeField, Min(.02f)] private float beamWidth = .22f;
    [SerializeField] private Hazard2D hazard;
    [SerializeField] private BoxCollider2D dangerCollider;
    [SerializeField] private SpriteRenderer warningLine;
    [SerializeField] private SpriteRenderer beamVisual;

    private float timer;

    public Phase CurrentPhase { get; private set; }
    // Hazard2D resets at order 0, so the cycle owner must apply its final warning
    // state afterwards.
    public int ResetOrder => 10;
    public float BeamLength => beamLength;
    public float BeamWidth => beamWidth;

    private void Awake()
    {
        ResolveReferences();
        ApplyGeometry();
        ResetRoomState();
    }

    private void FixedUpdate()
    {
        timer -= Time.fixedDeltaTime;
        if (timer > 0f) return;

        if (CurrentPhase == Phase.Warning) SetPhase(Phase.Firing);
        else if (CurrentPhase == Phase.Firing) SetPhase(Phase.Cooldown);
        else SetPhase(Phase.Warning);
    }

    public void Configure(Hazard2D damageHazard, BoxCollider2D collider,
        SpriteRenderer telegraph, SpriteRenderer beam, float length, float width,
        float warning, float firing, float cooldown)
    {
        hazard = damageHazard;
        dangerCollider = collider;
        warningLine = telegraph;
        beamVisual = beam;
        beamLength = Mathf.Max(.1f, length);
        beamWidth = Mathf.Max(.02f, width);
        warningDuration = Mathf.Max(0f, warning);
        firingDuration = Mathf.Max(.01f, firing);
        cooldownDuration = Mathf.Max(0f, cooldown);
        ApplyGeometry();
        ResetRoomState();
    }

    public void ResetRoomState()
    {
        ResolveReferences();
        ApplyGeometry();
        SetPhase(Phase.Warning);
    }

    private void SetPhase(Phase phase)
    {
        CurrentPhase = phase;
        timer = phase == Phase.Warning ? warningDuration :
            phase == Phase.Firing ? firingDuration : cooldownDuration;
        hazard?.SetActive(phase == Phase.Firing);
        if (warningLine != null) warningLine.enabled = phase == Phase.Warning;
        if (beamVisual != null) beamVisual.enabled = phase == Phase.Firing;
    }

    private void ApplyGeometry()
    {
        if (dangerCollider != null)
        {
            dangerCollider.size = new Vector2(beamLength, beamWidth);
            dangerCollider.offset = new Vector2(beamLength * .5f, 0f);
            dangerCollider.isTrigger = true;
        }
        ConfigureLine(warningLine, Mathf.Max(.035f, beamWidth * .18f));
        ConfigureLine(beamVisual, beamWidth);
    }

    private void ConfigureLine(SpriteRenderer renderer, float width)
    {
        if (renderer == null) return;
        renderer.drawMode = SpriteDrawMode.Simple;
        Vector2 spriteSize = renderer.sprite != null ? renderer.sprite.bounds.size : Vector2.one;
        renderer.transform.localScale = new Vector3(
            beamLength / Mathf.Max(.001f, spriteSize.x),
            width / Mathf.Max(.001f, spriteSize.y), 1f);
        renderer.transform.localPosition = new Vector3(beamLength * .5f, 0f, 0f);
    }

    private void ResolveReferences()
    {
        if (hazard == null) hazard = GetComponentInChildren<Hazard2D>(true);
        if (dangerCollider == null && hazard != null) dangerCollider = hazard.GetComponent<BoxCollider2D>();
    }

    private void OnValidate()
    {
        warningDuration = Mathf.Max(0f, warningDuration);
        firingDuration = Mathf.Max(.01f, firingDuration);
        cooldownDuration = Mathf.Max(0f, cooldownDuration);
        beamLength = Mathf.Max(.1f, beamLength);
        beamWidth = Mathf.Max(.02f, beamWidth);
        ApplyGeometry();
    }
}
