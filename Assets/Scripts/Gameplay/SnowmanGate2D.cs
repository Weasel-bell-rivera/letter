using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class SnowmanGate2D : MonoBehaviour, IRoomResettable, IOrderedRoomResettable
{
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Color waitingColor = Color.white;
    [SerializeField] private Color satisfiedColor = new(.65f, .9f, 1f, .35f);
    private BoxCollider2D blocker;
    public bool IsSatisfied { get; private set; }
    public int ResetOrder => 20;

    private void Awake() { blocker = GetComponent<BoxCollider2D>(); if (visual == null) visual = GetComponentInChildren<SpriteRenderer>(); ResetRoomState(); }
    public void GiveCarrot()
    {
        IsSatisfied = true;
        if (blocker != null) blocker.enabled = false;
        if (visual != null) visual.color = satisfiedColor;
    }
    public void ConfigureVisual(SpriteRenderer renderer) => visual = renderer;
    public void ResetRoomState()
    {
        IsSatisfied = false;
        if (blocker == null) blocker = GetComponent<BoxCollider2D>();
        blocker.enabled = true;
        if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
        if (visual != null) visual.color = waitingColor;
    }
}
