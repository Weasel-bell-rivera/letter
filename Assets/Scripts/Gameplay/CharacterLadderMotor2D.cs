using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class CharacterLadderMotor2D : MonoBehaviour
{
    private readonly HashSet<Ladder2D> overlappingLadders = new();
    private Rigidbody2D body;
    private BoxCollider2D bodyCollider;
    private Ladder2D currentLadder;
    private int observedJumpInputSequence;
    private bool jumpSequenceInitialized;
    private bool entryRequiresNeutralInput;

    public bool IsClimbing => currentLadder != null;
    public float VerticalInput { get; private set; }
    public Ladder2D CurrentLadder => currentLadder;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other) => Track(other);
    private void OnTriggerStay2D(Collider2D other) => Track(other);

    private void OnTriggerExit2D(Collider2D other)
    {
        Ladder2D ladder = ResolveLadder(other);
        if (ladder != null) overlappingLadders.Remove(ladder);
    }

    private void OnDisable() => CancelAndStop();

    public bool ProcessMovement(Vector2 moveInput, int jumpInputSequence, out bool consumedJump)
    {
        consumedJump = false;
        if (!jumpSequenceInitialized)
        {
            observedJumpInputSequence = jumpInputSequence;
            jumpSequenceInitialized = true;
        }

        bool jumpPressed = observedJumpInputSequence != jumpInputSequence;
        observedJumpInputSequence = jumpInputSequence;
        if (Mathf.Abs(moveInput.y) <= .01f) entryRequiresNeutralInput = false;

        if (currentLadder != null && jumpPressed)
        {
            consumedJump = true;
            entryRequiresNeutralInput = true;
            CancelAndStop();
            return false;
        }

        if (currentLadder == null && !entryRequiresNeutralInput && Mathf.Abs(moveInput.y) > .01f)
        {
            currentLadder = SelectLadder();
            if (currentLadder != null)
            {
                consumedJump = jumpPressed;
                body.linearVelocity = Vector2.zero;
            }
        }

        if (currentLadder == null) return false;
        if (!currentLadder.IsUsable || !currentLadder.Overlaps(bodyCollider))
        {
            Cancel();
            return false;
        }

        VerticalInput = Mathf.Clamp(moveInput.y, -1f, 1f);
        if (VerticalInput > .01f && bodyCollider.bounds.max.y >= currentLadder.TopY - .05f)
        {
            if (currentLadder.TryGetTopExitPosition(body, bodyCollider, out Vector2 topExit))
            {
                body.position = topExit;
                body.linearVelocity = Vector2.zero;
                currentLadder = null;
                VerticalInput = 0f;
                entryRequiresNeutralInput = true;
                return true;
            }

            VerticalInput = 0f;
        }

        float colliderOffsetX = bodyCollider.bounds.center.x - body.position.x;
        float targetBodyX = currentLadder.CenterX - colliderOffsetX;
        float alignmentVelocity = Mathf.Clamp((targetBodyX - body.position.x) / Time.fixedDeltaTime,
            -Ladder2D.AlignmentSpeed, Ladder2D.AlignmentSpeed);
        body.linearVelocity = new Vector2(alignmentVelocity, VerticalInput * Ladder2D.ClimbSpeed);
        return true;
    }

    public void Cancel()
    {
        currentLadder = null;
        VerticalInput = 0f;
    }

    public void CancelAndStop()
    {
        bool wasClimbing = currentLadder != null;
        Cancel();
        if (wasClimbing && body != null) body.linearVelocity = Vector2.zero;
    }

    public void ClearAll()
    {
        CancelAndStop();
        overlappingLadders.Clear();
        entryRequiresNeutralInput = false;
    }

    private void Track(Collider2D other)
    {
        Ladder2D ladder = ResolveLadder(other);
        if (ladder != null && ladder.IsUsable) overlappingLadders.Add(ladder);
    }

    private Ladder2D SelectLadder()
    {
        Ladder2D selected = null;
        float selectedDistance = float.PositiveInfinity;
        foreach (Ladder2D ladder in overlappingLadders)
        {
            if (ladder == null || !ladder.Overlaps(bodyCollider)) continue;
            float distance = Mathf.Abs(bodyCollider.bounds.center.x - ladder.CenterX);
            if (distance < selectedDistance - .0001f ||
                (Mathf.Abs(distance - selectedDistance) <= .0001f &&
                 (selected == null || ladder.GetEntityId() < selected.GetEntityId())))
            {
                selected = ladder;
                selectedDistance = distance;
            }
        }
        return selected;
    }

    private static Ladder2D ResolveLadder(Collider2D other)
        => other != null ? other.GetComponent<Ladder2D>() ?? other.GetComponentInParent<Ladder2D>() : null;
}
