using UnityEngine;

[DisallowMultipleComponent]
public sealed class MirrorCloneReadabilityHalo2D : MonoBehaviour
{
    [SerializeField] private SpriteRenderer halo;
    [SerializeField, Min(1f)] private float fadeSpeed = 5f;
    [SerializeField, Min(.05f)] private float reacquireInterval = .2f;
    [SerializeField] private Color haloColor = new(.18f, .78f, 1f, .28f);

    private MirrorCloneController2D target;
    private MaterialPropertyBlock propertyBlock;
    private Vector3 baseScale;
    private float amount;
    private float nextReacquireTime;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        if (halo != null)
        {
            baseScale = halo.transform.localScale;
            halo.enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (target == null && Time.unscaledTime >= nextReacquireTime)
        {
            target = FindFirstObjectByType<MirrorCloneController2D>();
            nextReacquireTime = Time.unscaledTime + reacquireInterval;
        }

        float targetAmount = target != null ? haloColor.a : 0f;
        amount = Mathf.MoveTowards(amount, targetAmount, fadeSpeed * Time.deltaTime);
        if (target != null)
            transform.position = target.transform.position + new Vector3(0f, .04f, 0f);

        if (halo == null) return;
        float pulse = 1f + Mathf.Sin(Time.time * 4.8f) * .025f;
        halo.transform.localScale = baseScale * pulse;

        Color color = haloColor;
        color.a = amount;
        halo.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(ColorId, color);
        propertyBlock.SetFloat(IntensityId, .65f);
        halo.SetPropertyBlock(propertyBlock);
        halo.enabled = amount > .005f;
    }

    private void OnDisable()
    {
        target = null;
        amount = 0f;
        if (halo != null)
        {
            halo.enabled = false;
            halo.transform.localScale = baseScale;
        }
    }
}
