using UnityEngine;

public sealed class EruptionHazard2D : MonoBehaviour, IRoomResettable, IOrderedRoomResettable
{
    public enum Phase { Warning, Dangerous, Cooldown }
    [SerializeField] private float warningDuration = 1f;
    [SerializeField] private float dangerDuration = 1f;
    [SerializeField] private float cooldownDuration = 2f;
    [SerializeField] private Hazard2D hazard;
    [SerializeField] private SpriteRenderer visual;
    private float timer;
    public Phase CurrentPhase { get; private set; }
    public int ResetOrder => 10;

    private void Awake() { if (hazard == null) hazard = GetComponentInChildren<Hazard2D>(); ResetRoomState(); }
    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        SetPhase(CurrentPhase == Phase.Warning ? Phase.Dangerous : CurrentPhase == Phase.Dangerous ? Phase.Cooldown : Phase.Warning);
    }
    private void SetPhase(Phase phase)
    {
        CurrentPhase = phase;
        timer = phase == Phase.Warning ? warningDuration : phase == Phase.Dangerous ? dangerDuration : cooldownDuration;
        hazard?.SetActive(phase == Phase.Dangerous);
        if (visual != null)
        {
            // Keep the full warning phase for timing, but do not show the old
            // yellow placeholder column. The column only represents active danger.
            visual.enabled = phase == Phase.Dangerous;
            visual.color = Color.red;
        }
    }
    public void ResetRoomState() => SetPhase(Phase.Warning);
}
