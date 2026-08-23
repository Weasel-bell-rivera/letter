using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerVisual2D : MonoBehaviour
{
    public enum PresentationPose { Automatic, Duck, Front, Hit }

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite jumpSprite;
    [SerializeField] private Sprite walkSpriteA;
    [SerializeField] private Sprite walkSpriteB;
    [SerializeField] private Sprite duckSprite;
    [SerializeField] private Sprite frontSprite;
    [SerializeField] private Sprite hitSprite;
    [SerializeField, Min(.01f)] private float walkFrameSeconds = .12f;

    private PlayerController2D player;
    private MirrorCloneController2D clone;
    private PresentationPose pose;

    public SpriteRenderer Renderer => spriteRenderer;
    public Sprite IdleSprite => idleSprite;
    public Sprite JumpSprite => jumpSprite;
    public Sprite WalkSpriteA => walkSpriteA;
    public Sprite WalkSpriteB => walkSpriteB;
    public Sprite DuckSprite => duckSprite;
    public Sprite FrontSprite => frontSprite;
    public Sprite HitSprite => hitSprite;
    public float WalkFrameSeconds => walkFrameSeconds;
    public PresentationPose Pose => pose;

    public void Configure(SpriteRenderer targetRenderer, Sprite idle, Sprite jump, Sprite walkA, Sprite walkB,
        Sprite duck, Sprite front, Sprite hit, float frameSeconds = .12f)
    {
        spriteRenderer = targetRenderer;
        idleSprite = idle;
        jumpSprite = jump;
        walkSpriteA = walkA;
        walkSpriteB = walkB;
        duckSprite = duck;
        frontSprite = front;
        hitSprite = hit;
        walkFrameSeconds = Mathf.Max(.01f, frameSeconds);
        Apply(idleSprite);
    }

    public void SetPresentationPose(PresentationPose nextPose)
    {
        pose = nextPose;
        Apply(PoseSprite(nextPose));
    }

    private void Awake()
    {
        ResolveController();
        Apply(idleSprite);
    }

    private void Update()
    {
        if (spriteRenderer == null) return;
        if (pose != PresentationPose.Automatic)
        {
            Apply(PoseSprite(pose));
            return;
        }

        ResolveController();
        bool grounded = player != null ? player.IsGroundedNow : clone != null && clone.IsGroundedNow;
        float horizontal = player != null ? player.HorizontalInput : clone != null ? clone.MovementInput : 0f;

        if (!grounded)
        {
            Apply(jumpSprite != null ? jumpSprite : idleSprite);
            return;
        }

        if (Mathf.Abs(horizontal) > .01f && walkSpriteA != null && walkSpriteB != null)
        {
            int frame = Mathf.FloorToInt(Time.unscaledTime / walkFrameSeconds) & 1;
            Apply(frame == 0 ? walkSpriteA : walkSpriteB);
            return;
        }

        Apply(idleSprite);
    }

    private void ResolveController()
    {
        if (player == null) player = GetComponentInParent<PlayerController2D>();
        if (player == null && clone == null) clone = GetComponentInParent<MirrorCloneController2D>();
    }

    private Sprite PoseSprite(PresentationPose requested)
    {
        return requested switch
        {
            PresentationPose.Duck => duckSprite,
            PresentationPose.Front => frontSprite,
            PresentationPose.Hit => hitSprite,
            _ => idleSprite
        };
    }

    private void Apply(Sprite sprite)
    {
        if (spriteRenderer != null && sprite != null && spriteRenderer.sprite != sprite)
            spriteRenderer.sprite = sprite;
    }
}
