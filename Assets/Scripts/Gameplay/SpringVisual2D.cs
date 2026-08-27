using UnityEngine;

public sealed class SpringVisual2D : MonoBehaviour, IRoomResettable
{
    public const float DefaultCompressionDuration = .08f;

    [SerializeField] private Spring2D spring;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite extendedSprite;
    [SerializeField] private Sprite compressedSprite;
    [SerializeField, Min(0f)] private float compressionDuration = DefaultCompressionDuration;

    private float restoreAt = float.NegativeInfinity;

    public SpriteRenderer Renderer => spriteRenderer;
    public Sprite ExtendedSprite => extendedSprite;
    public Sprite CompressedSprite => compressedSprite;
    public float CompressionDuration => compressionDuration;

    private void Awake() => ResolveReferences();

    private void OnEnable()
    {
        ResolveReferences();
        if (spring != null) spring.Bounced += PlayCompression;
        ShowExtended();
    }

    private void OnDisable()
    {
        if (spring != null) spring.Bounced -= PlayCompression;
    }

    private void Update()
    {
        if (Time.time < restoreAt) return;
        if (!float.IsNegativeInfinity(restoreAt)) ShowExtended();
    }

    public void Configure(Spring2D source, SpriteRenderer renderer, Sprite expanded, Sprite compressed,
        float duration = DefaultCompressionDuration)
    {
        if (spring != null) spring.Bounced -= PlayCompression;
        spring = source;
        spriteRenderer = renderer;
        extendedSprite = expanded;
        compressedSprite = compressed;
        compressionDuration = Mathf.Max(0f, duration);
        if (isActiveAndEnabled && spring != null) spring.Bounced += PlayCompression;
        ShowExtended();
    }

    public void ResetRoomState() => ShowExtended();

    private void PlayCompression()
    {
        if (spriteRenderer != null && compressedSprite != null) spriteRenderer.sprite = compressedSprite;
        restoreAt = Time.time + compressionDuration;
    }

    private void ShowExtended()
    {
        if (spriteRenderer != null && extendedSprite != null) spriteRenderer.sprite = extendedSprite;
        restoreAt = float.NegativeInfinity;
    }

    private void ResolveReferences()
    {
        if (spring == null) spring = GetComponentInParent<Spring2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnValidate() => compressionDuration = Mathf.Max(0f, compressionDuration);
}
