using UnityEngine;
using W1.Accessibility;

/// <summary>Presentation-only animation for a wind column; gameplay remains in WindColumn2D.</summary>
public sealed class WindColumnVisual2D : MonoBehaviour
{
    [SerializeField] private SpriteRenderer haze;
    [SerializeField] private SpriteRenderer farStreaks;
    [SerializeField] private SpriteRenderer nearStreaks;
    [SerializeField] private Vector2 worldSize = new(6f, 1f);
    [SerializeField] private float driftSpeed = 2.2f;

    private float phase;
    private float warningPulse;
    private WindColumn2D.WindState state;
    private bool blowing = true;
    private Vector2 direction = Vector2.right;
    private Material farMaterial;
    private Material nearMaterial;

    private void Awake()
    {
        farMaterial = CreateRuntimeMaterial(farStreaks);
        nearMaterial = CreateRuntimeMaterial(nearStreaks);
        ApplySizes();
    }

    private void OnDestroy()
    {
        if (farMaterial != null) Destroy(farMaterial);
        if (nearMaterial != null) Destroy(nearMaterial);
    }

    public void Configure(SpriteRenderer hazeRenderer, SpriteRenderer farRenderer,
        SpriteRenderer nearRenderer, Vector2 size)
    {
        haze = hazeRenderer;
        farStreaks = farRenderer;
        nearStreaks = nearRenderer;
        SetWorldSize(size);
        ApplyState(WindColumn2D.WindState.Blowing, true);
    }

    public void SetWorldSize(Vector2 size)
    {
        worldSize = new Vector2(Mathf.Max(.1f, size.x), Mathf.Max(.1f, size.y));
        ApplySizes();
    }

    public void ApplyState(WindColumn2D.WindState windState, bool isBlowing)
    {
        state = windState;
        blowing = isBlowing;
        if (haze != null)
        {
            haze.enabled = false;
            haze.color = Color.clear;
        }
        ApplyStreakColor();
    }

    public void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= .0001f) return;
        this.direction = direction.normalized;
        transform.right = this.direction;
        ApplySizes();
    }

    private void Update()
    {
        if (!AccessibilityMotionPolicy.AllowDecorativeLoop)
        {
            ApplyReducedMotionShape();
            ApplyStreakColor(false);
            return;
        }
        if (farStreaks != null) farStreaks.enabled = true;
        if (nearStreaks != null) nearStreaks.enabled = true;
        float rate = blowing ? driftSpeed : state == WindColumn2D.WindState.Warning ? driftSpeed * .7f : .12f;
        phase += Time.deltaTime * rate;
        warningPulse += Time.deltaTime * (state == WindColumn2D.WindState.Warning ? 5.5f : 1.5f);
        if (farMaterial != null)
            farMaterial.mainTextureOffset = new Vector2(Mathf.Repeat(phase * .055f, 1f),
                Mathf.Sin(phase * .4f) * .015f);
        if (nearMaterial != null)
            nearMaterial.mainTextureOffset = new Vector2(Mathf.Repeat(phase * .11f + .37f, 1f),
                Mathf.Sin(phase * .65f + .8f) * .025f);
        ApplyStreakColor(true);
    }

    private void ApplyReducedMotionShape()
    {
        // Two bands = blowing, upper band only = calm, lower band only = warning.
        // Direction remains encoded by the oriented streak sprites themselves.
        if (farStreaks != null)
            farStreaks.enabled = state != WindColumn2D.WindState.Warning;
        if (nearStreaks != null)
            nearStreaks.enabled = state != WindColumn2D.WindState.Calm;
    }

    private void ApplyStreakColor(bool animate = true)
    {
        float pulse = animate ? .88f + Mathf.Sin(phase * 2f) * .12f : .88f;
        float warningGlow = animate ? .68f + Mathf.Sin(warningPulse) * .22f : .68f;
        if (farStreaks != null) farStreaks.color = blowing
            ? new Color(.45f, .9f, 1f, .4f * pulse)
            : state == WindColumn2D.WindState.Warning
                ? new Color(.65f, .95f, 1f, .2f * warningGlow)
                : new Color(.5f, .75f, .85f, .055f);
        if (nearStreaks != null) nearStreaks.color = blowing
            ? new Color(.88f, 1f, 1f, .68f * pulse)
            : state == WindColumn2D.WindState.Warning
                ? new Color(1f, .86f, .38f, .42f * warningGlow)
                : new Color(.65f, .82f, .88f, .08f);
    }

    private void ApplySizes()
    {
        Vector2 localSize = Mathf.Abs(direction.y) > Mathf.Abs(direction.x)
            ? new Vector2(worldSize.y, worldSize.x)
            : worldSize;
        float bandHeight = Mathf.Clamp(localSize.y * .16f, .28f, .58f);
        float offset = Mathf.Min(localSize.y * .25f, 1.05f);
        Size(farStreaks, new Vector2(localSize.x * .92f, bandHeight), SpriteDrawMode.Tiled);
        Size(nearStreaks, new Vector2(localSize.x * .86f, bandHeight * .82f), SpriteDrawMode.Tiled);
        if (farStreaks != null) farStreaks.transform.localPosition = new Vector3(0f, offset, 0f);
        if (nearStreaks != null) nearStreaks.transform.localPosition = new Vector3(0f, -offset, 0f);
    }

    private static void Size(SpriteRenderer renderer, Vector2 targetSize, SpriteDrawMode mode)
    {
        if (renderer == null || renderer.sprite == null) return;
        renderer.drawMode = mode;
        renderer.tileMode = SpriteTileMode.Continuous;
        renderer.size = targetSize;
        renderer.transform.localScale = Vector3.one;
    }

    private static Material CreateRuntimeMaterial(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sharedMaterial == null) return null;
        Material material = new(renderer.sharedMaterial) { name = renderer.sharedMaterial.name + " (Wind Runtime)" };
        renderer.material = material;
        return material;
    }
}
