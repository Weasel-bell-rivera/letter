using System.Collections.Generic;
using UnityEngine;
using W1.Accessibility;

/// <summary>Presentation-only feedback driven by the shared freezing progress component.</summary>
[DisallowMultipleComponent]
public sealed class FreezingVisual2D : MonoBehaviour
{
    [SerializeField] private Color frozenTint = new(.45f, .82f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float maximumTint = .72f;
    [SerializeField, Range(0f, 1f)] private float maximumOverlayAlpha = .48f;
    [SerializeField] private float responseSpeed = 8f;

    private readonly Dictionary<SpriteRenderer, Color> baseColors = new();
    private FreezingGroundActor2D source;
    private PlayerController2D player;
    private SpriteRenderer[] renderers;
    private SpriteRenderer overlay;
    private SpriteRenderer overlayTarget;
    private float displayedAmount;

    public float DisplayedAmount => displayedAmount;

    public static FreezingVisual2D Ensure(GameObject owner)
    {
        if (owner == null) return null;
        FreezingVisual2D visual = owner.GetComponent<FreezingVisual2D>();
        return visual != null ? visual : owner.AddComponent<FreezingVisual2D>();
    }

    private void Awake()
    {
        source = GetComponent<FreezingGroundActor2D>();
        player = GetComponent<PlayerController2D>();
        RefreshRenderers();
        CreateOverlay();
        Apply(0f);
    }

    private void LateUpdate()
    {
        if (source == null) source = GetComponent<FreezingGroundActor2D>();
        float target = Mathf.Max(source != null ? source.FreezeAmount : 0f,
            player != null ? player.FrozenGroundFreezeAmount : 0f);
        displayedAmount = Mathf.MoveTowards(displayedAmount, target, responseSpeed * Time.deltaTime);
        if (renderers == null || renderers.Length == 0) RefreshRenderers();
        UpdateOverlayTarget();
        Apply(displayedAmount);
    }

    private void RefreshRenderers()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
            if (renderer != null && renderer != overlay && !baseColors.ContainsKey(renderer))
                baseColors.Add(renderer, renderer.color);
    }

    private void CreateOverlay()
    {
        GameObject overlayObject = new("FreezeOverlay");
        overlayObject.transform.SetParent(transform, false);
        overlay = overlayObject.AddComponent<SpriteRenderer>();
        overlay.color = new Color(frozenTint.r, frozenTint.g, frozenTint.b, 0f);
        UpdateOverlayTarget();
    }

    private void UpdateOverlayTarget()
    {
        SpriteRenderer target = null;
        if (renderers != null)
            foreach (SpriteRenderer renderer in renderers)
                if (renderer != null && renderer != overlay && renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.sprite != null)
                { target = renderer; break; }
        if (target == null || target == overlayTarget) { SyncOverlay(target); return; }
        overlayTarget = target;
        overlay.transform.SetParent(target.transform, false);
        overlay.transform.localPosition = Vector3.zero;
        overlay.transform.localRotation = Quaternion.identity;
        overlay.transform.localScale = Vector3.one;
        SyncOverlay(target);
    }

    private void SyncOverlay(SpriteRenderer target)
    {
        if (overlay == null) return;
        overlay.enabled = target != null && displayedAmount > .001f;
        if (target == null) return;
        overlay.sprite = target.sprite;
        overlay.flipX = target.flipX;
        overlay.flipY = target.flipY;
        overlay.sortingLayerID = target.sortingLayerID;
        overlay.sortingOrder = target.sortingOrder + 1;
        overlay.sharedMaterial = target.sharedMaterial;
    }

    private void Apply(float amount)
    {
        float tint = Mathf.Clamp01(amount) * maximumTint;
        foreach (KeyValuePair<SpriteRenderer, Color> pair in baseColors)
        {
            if (pair.Key == null) continue;
            Color value = Color.Lerp(pair.Value, frozenTint, tint);
            value.a = pair.Value.a;
            pair.Key.color = value;
        }
        if (overlay == null) return;
        float warningPulse = amount < .75f || !AccessibilityMotionPolicy.AllowDecorativeLoop
            ? 0f
            : (Mathf.Sin(Time.time * 12f) * .5f + .5f) * .1f * amount;
        overlay.color = new Color(frozenTint.r, frozenTint.g, frozenTint.b,
            Mathf.Clamp01(amount * maximumOverlayAlpha + warningPulse));
        SyncOverlay(overlayTarget);
    }

    private void OnDisable()
    {
        displayedAmount = 0f;
        foreach (KeyValuePair<SpriteRenderer, Color> pair in baseColors)
            if (pair.Key != null) pair.Key.color = pair.Value;
        if (overlay != null) overlay.enabled = false;
    }
}
