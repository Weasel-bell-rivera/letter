using UnityEngine;

public sealed class EruptionHazard2D : MonoBehaviour, IRoomResettable
{
    public enum Phase { Warning, Dangerous, Cooldown }
    [SerializeField] private float warningDuration = 1f;
    [SerializeField] private float dangerDuration = 1f;
    [SerializeField] private float cooldownDuration = 2f;
    [SerializeField] private Hazard2D hazard;
    [SerializeField] private SpriteRenderer visual;
    private float timer;
    public Phase CurrentPhase { get; private set; }

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
        if (visual != null) visual.color = phase == Phase.Warning ? Color.yellow : phase == Phase.Dangerous ? Color.red : Color.gray;
    }
    public void ResetRoomState() => SetPhase(Phase.Warning);
}
