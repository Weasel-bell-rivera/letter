using UnityEngine;

/// <summary>Small reusable unscaled sprite-frame loop for gameplay visuals.</summary>
public sealed class SpriteFrameAnimator2D : MonoBehaviour
{
    [SerializeField] private SpriteRenderer target;
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(1f)] private float framesPerSecond = 8f;

    private float elapsed;
    private int frameIndex;

    public int FrameCount => frames?.Length ?? 0;
    public float FramesPerSecond => framesPerSecond;

    private void OnEnable()
    {
        elapsed = 0f;
        frameIndex = 0;
        ApplyFrame();
    }

    private void Update()
    {
        if (target == null || frames == null || frames.Length < 2) return;
        elapsed += Time.deltaTime;
        float frameDuration = 1f / framesPerSecond;
        while (elapsed >= frameDuration)
        {
            elapsed -= frameDuration;
            frameIndex = (frameIndex + 1) % frames.Length;
        }
        ApplyFrame();
    }

    public void Configure(SpriteRenderer renderer, Sprite[] animationFrames, float fps)
    {
        target = renderer;
        frames = animationFrames;
        framesPerSecond = Mathf.Max(1f, fps);
        frameIndex = 0;
        elapsed = 0f;
        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (target != null && frames != null && frames.Length > 0)
            target.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
    }
}
