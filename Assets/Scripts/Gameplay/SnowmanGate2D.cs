using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class SnowmanGate2D : MonoBehaviour, IRoomResettable, IOrderedRoomResettable
{
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Color waitingColor = Color.white;
    [SerializeField] private Color satisfiedColor = new(.65f, .9f, 1f, .35f);
    private BoxCollider2D blocker;
    private GameObject stateCueRoot;
    private GameObject waitingShapeCue;
    private GameObject satisfiedShapeCue;
    public bool IsSatisfied { get; private set; }
    public int ResetOrder => 20;

    private void Awake() { blocker = GetComponent<BoxCollider2D>(); if (visual == null) visual = GetComponentInChildren<SpriteRenderer>(); ResetRoomState(); }
    public void GiveCarrot()
    {
        IsSatisfied = true;
        if (blocker != null) blocker.enabled = false;
        if (visual != null) visual.color = satisfiedColor;
        ApplyStateCue();
    }
    public void ConfigureVisual(SpriteRenderer renderer)
    {
        visual = renderer;
        RebuildStateCue();
        ApplyStateCue();
    }
    public void ResetRoomState()
    {
        IsSatisfied = false;
        if (blocker == null) blocker = GetComponent<BoxCollider2D>();
        blocker.enabled = true;
        if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
        if (visual != null) visual.color = waitingColor;
        EnsureStateCue();
        ApplyStateCue();
    }

    private void ApplyStateCue()
    {
        EnsureStateCue();
        if (waitingShapeCue != null) waitingShapeCue.SetActive(!IsSatisfied);
        if (satisfiedShapeCue != null) satisfiedShapeCue.SetActive(IsSatisfied);
    }

    private void EnsureStateCue()
    {
        if (stateCueRoot != null || visual == null) return;
        stateCueRoot = new GameObject("Snowman Gate State Shape Cue");
        stateCueRoot.transform.SetParent(visual.transform, false);
        stateCueRoot.transform.localPosition = new Vector3(0f, 0f, -.01f);

        waitingShapeCue = new GameObject("Waiting X Shape");
        waitingShapeCue.transform.SetParent(stateCueRoot.transform, false);
        CreateLine(waitingShapeCue.transform, "Slash A",
            new Vector3(-.32f, -.32f), new Vector3(.32f, .32f));
        CreateLine(waitingShapeCue.transform, "Slash B",
            new Vector3(-.32f, .32f), new Vector3(.32f, -.32f));

        satisfiedShapeCue = new GameObject("Satisfied Check Shape");
        satisfiedShapeCue.transform.SetParent(stateCueRoot.transform, false);
        CreateLine(satisfiedShapeCue.transform, "Check",
            new Vector3(-.34f, 0f), new Vector3(-.08f, -.24f), new Vector3(.38f, .3f));
    }

    private void CreateLine(Transform parent, string lineName, params Vector3[] points)
    {
        GameObject lineObject = new(lineName);
        lineObject.transform.SetParent(parent, false);
        ConfigureLine(lineObject.AddComponent<LineRenderer>(), points, .12f, Color.black,
            visual.sortingOrder + 2);
        GameObject innerObject = new("High Contrast Inner Stroke");
        innerObject.transform.SetParent(lineObject.transform, false);
        ConfigureLine(innerObject.AddComponent<LineRenderer>(), points, .055f, Color.white,
            visual.sortingOrder + 3);
    }

    private void ConfigureLine(LineRenderer line, Vector3[] points, float width, Color strokeColor,
        int sortingOrder)
    {
        line.useWorldSpace = false;
        line.loop = false;
        line.positionCount = points.Length;
        line.SetPositions(points);
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = strokeColor;
        line.endColor = strokeColor;
        line.sharedMaterial = visual.sharedMaterial;
        line.sortingLayerID = visual.sortingLayerID;
        line.sortingOrder = sortingOrder;
    }

    private void RebuildStateCue()
    {
        if (stateCueRoot != null) Destroy(stateCueRoot);
        stateCueRoot = null;
        waitingShapeCue = null;
        satisfiedShapeCue = null;
        EnsureStateCue();
    }
}
