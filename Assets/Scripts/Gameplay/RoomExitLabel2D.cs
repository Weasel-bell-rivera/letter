using UnityEngine;
using W1.Accessibility;

[DisallowMultipleComponent]
public sealed class RoomExitLabel2D : MonoBehaviour
{
    private const string LabelObjectName = "Exit Label";
    [SerializeField] private string label = "Exit";
    [SerializeField] private string localizationKey = "ui.room_exit";
    [SerializeField] private Color color = Color.white;
    [SerializeField] private int fontSize = 64;
    [SerializeField] private float characterSize = 0.12f;
    private TextMesh textMesh;
    private AccessibilityPreferencesService accessibilityService;

    private void Awake()
    {
        Transform existingLabel = transform.Find(LabelObjectName);
        textMesh = existingLabel != null
            ? existingLabel.GetComponent<TextMesh>()
            : CreateLabel();

        Configure(textMesh);
    }

    private void OnEnable()
    {
        LocalizationService.LocaleChanged += Refresh;
        accessibilityService = AccessibilityPreferencesService.Instance;
        accessibilityService.Changed += OnAccessibilityChanged;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizationService.LocaleChanged -= Refresh;
        if (accessibilityService != null)
            accessibilityService.Changed -= OnAccessibilityChanged;
        accessibilityService = null;
    }

    private void Refresh()
    {
        if (textMesh == null)
            textMesh = transform.Find(LabelObjectName)?.GetComponent<TextMesh>();
        Configure(textMesh);
    }

    private void OnAccessibilityChanged(AccessibilityPreferences preferences) => Configure(textMesh);

    private TextMesh CreateLabel()
    {
        GameObject labelObject = new GameObject(LabelObjectName);
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        return labelObject.AddComponent<TextMesh>();
    }

    private void Configure(TextMesh textMesh)
    {
        if (textMesh == null) return;

        textMesh.text = string.IsNullOrWhiteSpace(localizationKey)
            ? label
            : LocalizationService.Get(localizationKey);
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        AccessibilityPreferences preferences = accessibilityService != null
            ? accessibilityService.Current
            : AccessibilityPreferences.Default;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize * preferences.TextScaleMultiplier;
        textMesh.color = preferences.HighContrast ? Color.white : color;
        textMesh.fontStyle = preferences.HighContrast ? FontStyle.Bold : FontStyle.Normal;

        Font font = LocalizedFontProvider.GetFont();
        if (font != null)
        {
            textMesh.font = font;
            MeshRenderer renderer = textMesh.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = 1;
            renderer.enabled = true;
        }
        else
        {
            // Fail closed instead of silently binding an OS-dependent default font.
            MeshRenderer renderer = textMesh.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.enabled = false;
        }
    }
}
