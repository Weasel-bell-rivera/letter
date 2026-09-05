using UnityEngine;

/// <summary>
/// Fire-region ground that accepts normal mirror placement, then destroys the
/// placed mirror after a deterministic warning period.
/// </summary>
public sealed class OverheatingMirrorGround2D : MonoBehaviour, IMirrorPlacementEffect2D, IRoomResettable
{
    public const float DefaultOverheatSeconds = 3f;

    [SerializeField, Min(0.1f)] private float overheatSeconds = DefaultOverheatSeconds;
    [SerializeField] private Color mirrorOverheatColor = new(1f, 0.18f, 0.03f, 1f);
    [SerializeField] private AudioSource feedbackAudio;

    private static AudioClip warningClip;
    private static AudioClip destroyedClip;
    private MirrorPlayer2D activeMirror;
    private GameObject activePlacedMirror;
    private SpriteRenderer[] mirrorRenderers;
    private Color[] initialMirrorColors;
    private float elapsed;

    public bool IsHeating { get; private set; }
    public float RemainingSeconds => IsHeating ? Mathf.Max(0f, overheatSeconds - elapsed) : 0f;
    public float OverheatSeconds => overheatSeconds;

    public void Configure(float duration)
    {
        overheatSeconds = Mathf.Max(0.1f, duration);
    }

    public void OnMirrorPlaced(MirrorPlayer2D mirror)
    {
        StopHeating(true);
        if (mirror == null || mirror.State != MirrorPlayer2D.MirrorState.Placed || mirror.PlacedMirror == null)
            return;

        activeMirror = mirror;
        activePlacedMirror = mirror.PlacedMirror;
        mirrorRenderers = activePlacedMirror.GetComponentsInChildren<SpriteRenderer>(true);
        initialMirrorColors = new Color[mirrorRenderers.Length];
        for (int i = 0; i < mirrorRenderers.Length; i++) initialMirrorColors[i] = mirrorRenderers[i].color;
        elapsed = 0f;
        IsHeating = true;
        EnsureFeedbackAudio();
        feedbackAudio.PlayOneShot(warningClip);
    }

    private void Update()
    {
        if (!IsHeating) return;
        if (activeMirror == null || activeMirror.State != MirrorPlayer2D.MirrorState.Placed ||
            activeMirror.PlacedMirror != activePlacedMirror)
        {
            StopHeating(true);
            return;
        }

        elapsed += Time.deltaTime;
        ApplyMirrorWarning(Mathf.Clamp01(elapsed / overheatSeconds));
        if (elapsed < overheatSeconds) return;

        MirrorPlayer2D mirrorToDestroy = activeMirror;
        StopHeating(false);
        EnsureFeedbackAudio();
        feedbackAudio.PlayOneShot(destroyedClip);
        mirrorToDestroy.DestroyPlacedMirrorImmediate();
    }

    public void ResetRoomState() => StopHeating(true);

    private void OnDisable() => StopHeating(true);

    private void ApplyMirrorWarning(float progress)
    {
        if (mirrorRenderers == null || initialMirrorColors == null) return;
        for (int i = 0; i < mirrorRenderers.Length; i++)
        {
            if (mirrorRenderers[i] == null) continue;
            Color target = mirrorOverheatColor;
            target.a = initialMirrorColors[i].a;
            mirrorRenderers[i].color = Color.Lerp(initialMirrorColors[i], target, progress);
        }
    }

    private void StopHeating(bool restoreVisual)
    {
        if (restoreVisual && mirrorRenderers != null && initialMirrorColors != null)
        {
            for (int i = 0; i < mirrorRenderers.Length; i++)
                if (mirrorRenderers[i] != null) mirrorRenderers[i].color = initialMirrorColors[i];
        }
        activeMirror = null;
        activePlacedMirror = null;
        mirrorRenderers = null;
        initialMirrorColors = null;
        elapsed = 0f;
        IsHeating = false;
    }

    private void EnsureFeedbackAudio()
    {
        if (feedbackAudio == null) feedbackAudio = GetComponent<AudioSource>();
        if (feedbackAudio == null) feedbackAudio = gameObject.AddComponent<AudioSource>();
        feedbackAudio.playOnAwake = false;
        feedbackAudio.spatialBlend = 0f;
        warningClip ??= CreateTone("Mirror Overheat Warning", 420f, 860f, 0.28f, 0.04f);
        destroyedClip ??= CreateTone("Mirror Overheat Break", 760f, 180f, 0.18f, 0.055f);
    }

    private static AudioClip CreateTone(string clipName, float startFrequency, float endFrequency,
        float duration, float volume)
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float progress = i / (float)Mathf.Max(1, sampleCount - 1);
            float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
            float envelope = Mathf.Sin(Mathf.PI * progress);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * volume;
        }
        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnValidate()
    {
        overheatSeconds = Mathf.Max(0.1f, overheatSeconds);
    }
}
