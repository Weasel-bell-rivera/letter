using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public sealed class FlowingEdgeLine2D : MonoBehaviour
{
    [SerializeField] private Vector2 localStart;
    [SerializeField] private Vector2 localEnd = Vector2.right;
    [SerializeField, Min(.01f)] private float width = .4f;
    [SerializeField] private Material material;
    [SerializeField] private int sortingOrder = 2;

    public Vector2 LocalStart => localStart;
    public Vector2 LocalEnd => localEnd;

    public void Configure(Vector2 start, Vector2 end, float lineWidth, Material lineMaterial, int order)
    {
        localStart = start;
        localEnd = end;
        width = Mathf.Max(.01f, lineWidth);
        material = lineMaterial;
        sortingOrder = order;
        ApplyConfiguration();
    }

    private void Awake() => ApplyConfiguration();

    private void OnEnable() => ApplyConfiguration();

    private void OnValidate()
    {
        width = Mathf.Max(.01f, width);
        ApplyConfiguration();
    }

    private void ApplyConfiguration()
    {
        LineRenderer line = GetComponent<LineRenderer>();
        if (line == null) return;

        line.useWorldSpace = false;
        line.positionCount = 2;
        line.SetPosition(0, new Vector3(localStart.x, localStart.y, 0f));
        line.SetPosition(1, new Vector3(localEnd.x, localEnd.y, 0f));
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 0;
        line.numCornerVertices = 2;
        line.textureMode = LineTextureMode.Stretch;
        line.alignment = LineAlignment.TransformZ;
        line.sortingOrder = sortingOrder;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        if (material != null) line.sharedMaterial = material;
    }
}
