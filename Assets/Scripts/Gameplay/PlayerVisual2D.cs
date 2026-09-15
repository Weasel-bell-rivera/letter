using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerVisual2D : MonoBehaviour
{
    public enum PresentationPose { Automatic, Duck, Front, Hit }

    private enum AnimationState { Idle, Walk, Jump, ClimbIdle, ClimbMove, Duck, Front, Hit, Push }

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite[] jumpFrames;
    [SerializeField] private Sprite[] climbFrames;
    [SerializeField] private Sprite[] pushFrames;
    [SerializeField] private Vector2 baseVisualOffset;
    [SerializeField] private float pushFacingOffset;
    [SerializeField] private float[] jumpFrameVerticalOffsets;
    [SerializeField] private Sprite[] hitFrames;
    [SerializeField] private Sprite[] happyFrames;
    [SerializeField, Min(1f)] private float idleFramesPerSecond = 2f;
    [SerializeField, Min(1f)] private float walkFramesPerSecond = 8f;
    [SerializeField, Min(1f)] private float jumpFramesPerSecond = 12f;
    [SerializeField, Min(1f)] private float climbFramesPerSecond = 8f;
    [SerializeField, Min(1f)] private float pushFramesPerSecond = 6f;
    [SerializeField, Min(1f)] private float hitFramesPerSecond = 10f;

    private PlayerController2D player;
    private MirrorCloneController2D clone;
    private PresentationPose pose;
    private AnimationState animationState;
    private float stateStartedAt;
    private readonly List<RaycastHit2D> pushHits = new();

    public SpriteRenderer Renderer => spriteRenderer;
    public Sprite IdleSprite => First(idleFrames);
    public Sprite JumpSprite => First(jumpFrames);
    public Sprite WalkSpriteA => Frame(walkFrames, 0);
    public Sprite WalkSpriteB => Frame(walkFrames, 1);
    public Sprite DuckSprite => First(idleFrames);
    public Sprite FrontSprite => First(happyFrames) != null ? First(happyFrames) : First(idleFrames);
    public Sprite HitSprite => First(hitFrames) != null ? First(hitFrames) : First(idleFrames);
    public int IdleFrameCount => idleFrames?.Length ?? 0;
    public int WalkFrameCount => walkFrames?.Length ?? 0;
    public int JumpFrameCount => jumpFrames?.Length ?? 0;
    public int ClimbFrameCount => climbFrames?.Length ?? 0;
    public int PushFrameCount => pushFrames?.Length ?? 0;
    public int JumpFrameVerticalOffsetCount => jumpFrameVerticalOffsets?.Length ?? 0;
    public int HitFrameCount => hitFrames?.Length ?? 0;
    public float WalkFrameSeconds => 1f / Mathf.Max(1f, walkFramesPerSecond);
    public PresentationPose Pose => pose;

    public void Configure(SpriteRenderer targetRenderer, Sprite[] idle, Sprite[] walk, Sprite[] jump,
        Sprite[] hit, Sprite[] happy, float idleFps = 2f, float walkFps = 8f,
        float jumpFps = 12f, float hitFps = 10f, float[] jumpVerticalOffsets = null,
        Sprite[] climb = null, float climbFps = 8f, Sprite[] push = null, float pushFps = 6f,
        Vector2 visualOffset = default, float pushOffset = 0f)
    {
        spriteRenderer = targetRenderer;
        idleFrames = idle;
        walkFrames = walk;
        jumpFrames = jump;
        climbFrames = climb ?? walk;
        pushFrames = push ?? walk;
        baseVisualOffset = visualOffset;
        pushFacingOffset = pushOffset;
        hitFrames = hit;
        happyFrames = happy;
        jumpFrameVerticalOffsets = jumpVerticalOffsets;
        idleFramesPerSecond = Mathf.Max(1f, idleFps);
        walkFramesPerSecond = Mathf.Max(1f, walkFps);
        jumpFramesPerSecond = Mathf.Max(1f, jumpFps);
        climbFramesPerSecond = Mathf.Max(1f, climbFps);
        pushFramesPerSecond = Mathf.Max(1f, pushFps);
        hitFramesPerSecond = Mathf.Max(1f, hitFps);
        SetAnimationState(AnimationState.Idle);
    }

    // Kept for lightweight runtime prototypes that still configure the legacy seven sprites.
    public void Configure(SpriteRenderer targetRenderer, Sprite idle, Sprite jump, Sprite walkA, Sprite walkB,
        Sprite duck, Sprite front, Sprite hit, float frameSeconds = .12f)
    {
        Configure(targetRenderer, new[] { idle }, new[] { walkA, walkB }, new[] { jump },
            new[] { hit }, new[] { front }, 1f, 1f / Mathf.Max(.01f, frameSeconds), 1f, 1f);
    }

    public void SetPresentationPose(PresentationPose nextPose)
    {
        pose = nextPose;
        SetAnimationState(PresentationState(nextPose));
    }

    private void Awake()
    {
        ResolveController();
        stateStartedAt = Time.unscaledTime;
        SetAnimationState(AnimationState.Idle);
    }

    private void Update()
    {
        if (spriteRenderer == null) return;

        ResolveController();
        AnimationState nextState;
        if (pose != PresentationPose.Automatic)
        {
            nextState = PresentationState(pose);
        }
        else
        {
            bool climbing = player != null ? player.IsClimbing : clone != null && clone.IsClimbing;
            float climbInput = player != null ? player.ClimbInput : clone != null ? clone.ClimbInput : 0f;
            bool grounded = player != null ? player.IsGroundedNow : clone != null && clone.IsGroundedNow;
            float horizontal = player != null ? player.HorizontalInput : clone != null ? clone.MovementInput : 0f;
            nextState = climbing
                ? (Mathf.Abs(climbInput) > .01f ? AnimationState.ClimbMove : AnimationState.ClimbIdle)
                : !grounded ? AnimationState.Jump :
                IsPressingCrate(horizontal) ? AnimationState.Push :
                Mathf.Abs(horizontal) > .01f ? AnimationState.Walk : AnimationState.Idle;
        }

        SetAnimationState(nextState);
    }

    private bool IsPressingCrate(float input)
    {
        if (Mathf.Abs(input) <= .01f) return false;
        Collider2D actor = player != null ? player.FreezingCollider : clone != null ? clone.FreezingCollider : null;
        if (actor == null || !actor.enabled || actor.attachedRigidbody == null ||
            !actor.attachedRigidbody.simulated) return false;
        Vector2 axis = player != null ? Vector2.right : clone.MoveAxis;
        Collider2D support = player != null ? player.SupportCollider : clone.SupportCollider;
        Vector2 direction = axis * Mathf.Sign(input);
        ContactFilter2D filter = new();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(actor.gameObject.layer));
        filter.useTriggers = false;
        pushHits.Clear();
        // A short shape query tolerates the solver's contact gap without retaining stale contacts
        // through teleports, crate resets, clone destruction or scene changes.
        actor.Cast(direction, filter, pushHits, .035f);
        foreach (RaycastHit2D hit in pushHits)
        {
            if (hit.collider == null || hit.collider == support || hit.rigidbody == null ||
                Vector2.Dot(hit.normal, direction) > -.65f ||
                Physics2D.GetIgnoreCollision(actor, hit.collider)) continue;
            PushableCrate2D crate = hit.rigidbody.GetComponent<PushableCrate2D>();
            if (crate == null || !crate.isActiveAndEnabled || crate.SolidCollider != hit.collider) continue;
            // Exclude support-only contact (standing on a crate), including initial-overlap hits.
            Vector2 separation = (Vector2)hit.collider.bounds.center - (Vector2)actor.bounds.center;
            Vector2 halfSize = actor.bounds.extents;
            float sideExtent = Mathf.Abs(direction.x) * halfSize.x + Mathf.Abs(direction.y) * halfSize.y;
            if (Vector2.Dot(separation, direction) > sideExtent) return true;
        }
        return false;
    }

    private void ResolveController()
    {
        if (player == null) player = GetComponentInParent<PlayerController2D>();
        if (player == null && clone == null) clone = GetComponentInParent<MirrorCloneController2D>();
    }

    private void SetAnimationState(AnimationState nextState)
    {
        if (animationState != nextState)
        {
            animationState = nextState;
            stateStartedAt = Time.unscaledTime;
        }
        ApplyCurrentFrame();
    }

    private void ApplyCurrentFrame()
    {
        Sprite[] frames = FramesFor(animationState);
        if (frames == null || frames.Length == 0) frames = idleFrames;
        if (frames == null || frames.Length == 0) return;

        float fps = FramesPerSecondFor(animationState);
        int elapsedFrames = Mathf.FloorToInt((Time.unscaledTime - stateStartedAt) * fps);
        bool loop = animationState is AnimationState.Idle or AnimationState.Walk or AnimationState.ClimbMove or AnimationState.Push;
        int index = loop ? elapsedFrames % frames.Length : Mathf.Min(elapsedFrames, frames.Length - 1);
        // An attached, stationary character holds the authored ladder resting pose.
        if (animationState == AnimationState.ClimbIdle) index = 0;
        index = Mathf.Max(0, index);
        float verticalOffset = animationState == AnimationState.Jump &&
                               jumpFrameVerticalOffsets != null && index < jumpFrameVerticalOffsets.Length
            ? jumpFrameVerticalOffsets[index]
            : 0f;
        float horizontalOffset = animationState == AnimationState.Push
            ? pushFacingOffset * Mathf.Sign(transform.localScale.x) : 0f;
        transform.localPosition = (Vector3)baseVisualOffset +
            new Vector3(horizontalOffset, verticalOffset, 0f);
        Apply(frames[index]);
    }

    private Sprite[] FramesFor(AnimationState state) => state switch
    {
        AnimationState.Walk => walkFrames,
        AnimationState.Push => pushFrames,
        AnimationState.Jump => jumpFrames,
        AnimationState.ClimbIdle => ClimbFrames(),
        AnimationState.ClimbMove => ClimbFrames(),
        AnimationState.Front => happyFrames,
        AnimationState.Hit => hitFrames,
        _ => idleFrames
    };

    private float FramesPerSecondFor(AnimationState state) => state switch
    {
        AnimationState.Walk => walkFramesPerSecond,
        AnimationState.Push => pushFramesPerSecond,
        AnimationState.Jump => jumpFramesPerSecond,
        AnimationState.ClimbIdle => climbFramesPerSecond,
        AnimationState.ClimbMove => climbFramesPerSecond,
        AnimationState.Hit => hitFramesPerSecond,
        _ => idleFramesPerSecond
    };

    private static AnimationState PresentationState(PresentationPose requested) => requested switch
    {
        PresentationPose.Duck => AnimationState.Duck,
        PresentationPose.Front => AnimationState.Front,
        PresentationPose.Hit => AnimationState.Hit,
        _ => AnimationState.Idle
    };

    private static Sprite First(Sprite[] frames) => Frame(frames, 0);
    private Sprite[] ClimbFrames() => climbFrames != null && climbFrames.Length > 0 ? climbFrames : walkFrames;
    private static Sprite Frame(Sprite[] frames, int index) =>
        frames != null && index >= 0 && index < frames.Length ? frames[index] : null;

    private void Apply(Sprite sprite)
    {
        if (spriteRenderer != null && sprite != null && spriteRenderer.sprite != sprite)
            spriteRenderer.sprite = sprite;
    }
}
