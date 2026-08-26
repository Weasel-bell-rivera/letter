using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class AmbientSpritePulse2D : MonoBehaviour
{
    [SerializeField, Min(.1f)] private float cycleSeconds = 8f;
    [SerializeField, Range(0f, .5f)] private float alphaVariation = .08f;
    [SerializeField, Range(0f, .15f)] private float scaleVariation = .02f;
    [SerializeField] private float phaseOffset;

    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private Vector3 baseScale;

    public void Configure(float seconds, float alphaAmount, float scaleAmount, float phase)
    {
        cycleSeconds = Mathf.Max(.1f, seconds);
        alphaVariation = Mathf.Clamp(alphaAmount, 0f, .5f);
        scaleVariation = Mathf.Clamp(scaleAmount, 0f, .15f);
        phaseOffset = phase;
    }

    private void Awake() => CacheBaseState();

    private void OnEnable() => CacheBaseState();

    private void Update()
    {
        if (spriteRenderer == null) CacheBaseState();
        float wave = Mathf.Sin((Time.time / cycleSeconds) * Mathf.PI * 2f + phaseOffset);
        Color color = baseColor;
        color.a = Mathf.Clamp01(baseColor.a * (1f + wave * alphaVariation));
        spriteRenderer.color = color;
        transform.localScale = baseScale * (1f + wave * scaleVariation);
    }

    private void OnDisable()
    {
        if (spriteRenderer != null) spriteRenderer.color = baseColor;
        transform.localScale = baseScale;
    }

    private void CacheBaseState()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = spriteRenderer.color;
        baseScale = transform.localScale;
    }
}
