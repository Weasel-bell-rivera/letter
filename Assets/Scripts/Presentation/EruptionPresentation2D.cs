using UnityEngine;
using W1.Accessibility;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class EruptionPresentation2D : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private EruptionHazard2D source;

    [Header("State visuals")]
    [SerializeField] private SpriteRenderer groundGlow;
    [SerializeField] private SpriteRenderer warningColumn;
    [SerializeField] private SpriteRenderer dangerCore;
    [SerializeField] private ParticleSystem warningSparks;
    [SerializeField] private ParticleSystem dangerFlames;
    [SerializeField] private ParticleSystem cooldownSmoke;

    [Header("Response")]
    [SerializeField, Min(1f)] private float fadeSpeed = 7f;
    [SerializeField] private float phaseOffset;

    private EruptionHazard2D.Phase previousPhase;
    private bool hasPhase;
    private float groundAmount;
    private float warningAmount;
    private float dangerAmount;
    private Vector3 dangerBaseScale;
    private MaterialPropertyBlock propertyBlock;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        ResolveSource();
        if (dangerCore != null) dangerBaseScale = dangerCore.transform.localScale;
        SetRendererAmount(groundGlow, 0f, .8f);
        SetRendererAmount(warningColumn, 0f, .86f);
        SetRendererAmount(dangerCore, 0f, 1f);
    }

    private void OnEnable()
    {
        ResolveSource();
        hasPhase = false;
        StopAndClear(warningSparks);
        StopAndClear(dangerFlames);
        StopAndClear(cooldownSmoke);
    }

    private void LateUpdate()
    {
        if (source == null)
        {
            ResolveSource();
            if (source == null) return;
        }

        EruptionHazard2D.Phase phase = source.CurrentPhase;
        if (!hasPhase || phase != previousPhase)
        {
            EnterPhase(phase);
            previousPhase = phase;
            hasPhase = true;
        }

        bool reducedMotion = !AccessibilityMotionPolicy.AllowDecorativeLoop;
        float pulse = reducedMotion ? .5f : .5f + .5f * Mathf.Sin(Time.time * 11f + phaseOffset);
        float targetGround = phase == EruptionHazard2D.Phase.Warning
            ? .34f + pulse * .24f
            : phase == EruptionHazard2D.Phase.Dangerous ? .68f : 0f;
        float targetWarning = phase == EruptionHazard2D.Phase.Warning
            ? .12f + pulse * .22f
            : phase == EruptionHazard2D.Phase.Dangerous ? .18f : 0f;
        float targetDanger = phase == EruptionHazard2D.Phase.Dangerous
            ? .72f + pulse * .16f
            : 0f;

        float step = fadeSpeed * Time.deltaTime;
        groundAmount = Mathf.MoveTowards(groundAmount, targetGround, step);
        warningAmount = Mathf.MoveTowards(warningAmount, targetWarning, step);
        dangerAmount = Mathf.MoveTowards(dangerAmount, targetDanger, step);

        SetRendererAmount(groundGlow, groundAmount, .8f);
        SetRendererAmount(warningColumn, warningAmount, .86f);
        SetRendererAmount(dangerCore, dangerAmount, 1f);
        if (dangerCore != null)
        {
            float breathe = phase == EruptionHazard2D.Phase.Dangerous && !reducedMotion
                ? 1f + Mathf.Sin(Time.time * 17f + phaseOffset) * .025f
                : 1f;
            dangerCore.transform.localScale = new Vector3(
                dangerBaseScale.x / breathe,
                dangerBaseScale.y * breathe,
                dangerBaseScale.z);
        }

        if (reducedMotion)
        {
            StopEmitting(warningSparks);
            StopEmitting(dangerFlames);
            StopEmitting(cooldownSmoke);
        }
    }

    private void OnDisable()
    {
        StopAndClear(warningSparks);
        StopAndClear(dangerFlames);
        StopAndClear(cooldownSmoke);
        if (dangerCore != null) dangerCore.transform.localScale = dangerBaseScale;
    }

    private void ResolveSource()
    {
        if (source == null) source = GetComponentInParent<EruptionHazard2D>();
    }

    private void EnterPhase(EruptionHazard2D.Phase phase)
    {
        if (phase == EruptionHazard2D.Phase.Warning)
        {
            StopEmitting(dangerFlames);
            StopEmitting(cooldownSmoke);
            Play(warningSparks);
            return;
        }

        StopEmitting(warningSparks);
        if (phase == EruptionHazard2D.Phase.Dangerous)
        {
            StopEmitting(cooldownSmoke);
            Play(dangerFlames);
            return;
        }

        StopEmitting(dangerFlames);
        if (cooldownSmoke != null)
        {
            cooldownSmoke.Clear(true);
            cooldownSmoke.Play(true);
        }
    }

    private static void Play(ParticleSystem effect)
    {
        if (effect != null && !effect.isPlaying) effect.Play(true);
    }

    private static void StopEmitting(ParticleSystem effect)
    {
        if (effect != null)
            effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private static void StopAndClear(ParticleSystem effect)
    {
        if (effect != null)
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void SetRendererAmount(SpriteRenderer renderer, float amount, float intensity)
    {
        if (renderer == null) return;
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        Color color = renderer.color;
        color.a = Mathf.Clamp01(amount);
        renderer.color = color;
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(ColorId, color);
        propertyBlock.SetFloat(IntensityId, intensity);
        renderer.SetPropertyBlock(propertyBlock);
        renderer.enabled = color.a > .005f;
    }
}
