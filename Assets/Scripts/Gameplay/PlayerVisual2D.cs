using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerVisual2D : MonoBehaviour
{
    public enum PresentationPose { Automatic, Duck, Front, Hit }

    private enum AnimationState { Idle, Walk, Jump, ClimbIdle, ClimbMove, Duck, Front, Hit }

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite[] jumpFrames;
    [SerializeField] private Sprite[] climbFrames;
    [SerializeField] private float[] jumpFrameVerticalOffsets;
    [SerializeField] private Sprite[] hitFrames;
    [SerializeField] private Sprite[] happyFrames;
    [SerializeField, Min(1f)] private float idleFramesPerSecond = 2f;
    [SerializeField, Min(1f)] private float walkFramesPerSecond = 8f;
    [SerializeField, Min(1f)] private float jumpFramesPerSecond = 12f;
    [SerializeField, Min(1f)] private float climbFramesPerSecond = 8f;
    [SerializeField, Min(1f)] private float hitFramesPerSecond = 10f;

    private PlayerController2D player;
    private MirrorCloneController2D clone;
    private PresentationPose pose;
    private AnimationState animationState;
    private float stateStartedAt;

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
    public int JumpFrameVerticalOffsetCount => jumpFrameVerticalOffsets?.Length ?? 0;
    public int HitFrameCount => hitFrames?.Length ?? 0;
    public float WalkFrameSeconds => 1f / Mathf.Max(1f, walkFramesPerSecond);
    public PresentationPose Pose => pose;

    public void Configure(SpriteRenderer targetRenderer, Sprite[] idle, Sprite[] walk, Sprite[] jump,
        Sprite[] hit, Sprite[] happy, float idleFps = 2f, float walkFps = 8f,
        float jumpFps = 12f, float hitFps = 10f, float[] jumpVerticalOffsets = null,
        Sprite[] climb = null, float climbFps = 8f)
    {
        spriteRenderer = targetRenderer;
        idleFrames = idle;
        walkFrames = walk;
        jumpFrames = jump;
        climbFrames = climb ?? walk;
        hitFrames = hit;
        happyFrames = happy;
        jumpFrameVerticalOffsets = jumpVerticalOffsets;
        idleFramesPerSecond = Mathf.Max(1f, idleFps);
        walkFramesPerSecond = Mathf.Max(1f, walkFps);
        jumpFramesPerSecond = Mathf.Max(1f, jumpFps);
        climbFramesPerSecond = Mathf.Max(1f, climbFps);
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
                Mathf.Abs(horizontal) > .01f ? AnimationState.Walk : AnimationState.Idle;
        }

        SetAnimationState(nextState);
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
        bool loop = animationState is AnimationState.Idle or AnimationState.Walk or AnimationState.ClimbMove;
        int index = loop ? elapsedFrames % frames.Length : Mathf.Min(elapsedFrames, frames.Length - 1);
        index = Mathf.Max(0, index);
        float verticalOffset = animationState == AnimationState.Jump &&
                               jumpFrameVerticalOffsets != null && index < jumpFrameVerticalOffsets.Length
            ? jumpFrameVerticalOffsets[index]
            : 0f;
        transform.localPosition = Vector3.up * verticalOffset;
        Apply(frames[index]);
    }

    private Sprite[] FramesFor(AnimationState state) => state switch
    {
        AnimationState.Walk => walkFrames,
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
