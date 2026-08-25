using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoomExitLabel2D : MonoBehaviour
{
    private const string LabelObjectName = "Exit Label";
    private static Font cachedFont;

    [SerializeField] private string label = "Exit";
    [SerializeField] private Color color = Color.white;
    [SerializeField] private int fontSize = 64;
    [SerializeField] private float characterSize = 0.12f;

    private void Awake()
    {
        Transform existingLabel = transform.Find(LabelObjectName);
        TextMesh textMesh = existingLabel != null
            ? existingLabel.GetComponent<TextMesh>()
            : CreateLabel();

        Configure(textMesh);
    }

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

        textMesh.text = label;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.color = color;

        if (cachedFont == null)
        {
            cachedFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Arial", "Helvetica", "Liberation Sans", "DejaVu Sans" },
                fontSize);
        }

        Font font = cachedFont;
        if (font != null)
        {
            textMesh.font = font;
            MeshRenderer renderer = textMesh.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = 1;
        }
    }
}
