using UnityEngine;

public sealed class ConveyorVisual2D : MonoBehaviour
{
    [SerializeField] private SpriteRenderer beltRenderer;
    [SerializeField] private Transform indicatorRoot;
    [SerializeField] private Transform[] markers;
    [SerializeField, Min(.1f)] private float markerSpan = 4f;
    [SerializeField, Min(0f)] private float scrollScale = .35f;
    [SerializeField] private Color activeBeltColor = new(.12f, .24f, .3f, 1f);
    [SerializeField] private Color inactiveBeltColor = new(.28f, .3f, .32f, 1f);

    private Vector3[] initialMarkerPositions;
    private float speed;
    private bool conveyorActive;
    private bool initialized;

    public SpriteRenderer BeltRenderer => beltRenderer;
    public Transform IndicatorRoot => indicatorRoot;
    public bool IsAnimating => conveyorActive;

    private void Awake() => ResolveReferences();

    private void Update()
    {
        if (!conveyorActive || markers == null || markers.Length == 0) return;
        float halfSpan = markerSpan * .5f;
        float delta = speed * scrollScale * Time.deltaTime;
        foreach (Transform marker in markers)
        {
            if (marker == null) continue;
            Vector3 position = marker.localPosition;
            position.x = Mathf.Repeat(position.x + delta + halfSpan, markerSpan) - halfSpan;
            marker.localPosition = position;
        }
    }

    public void ConfigureReferences(SpriteRenderer belt, Transform indicators, Transform[] animatedMarkers)
    {
        beltRenderer = belt;
        indicatorRoot = indicators;
        markers = animatedMarkers;
        initialized = false;
        ResolveReferences();
    }

    public void ApplyState(int directionSign, float beltSpeed, bool active, bool resetPhase)
    {
        ResolveReferences();
        speed = Mathf.Max(0f, beltSpeed);
        conveyorActive = active;
        if (beltRenderer != null)
        {
            beltRenderer.color = active ? activeBeltColor : inactiveBeltColor;
            beltRenderer.flipX = directionSign < 0;
        }
        if (indicatorRoot != null)
        {
            Vector3 scale = indicatorRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * (directionSign < 0 ? -1f : 1f);
            indicatorRoot.localScale = scale;
        }
        if (resetPhase) ResetPhase();
    }

    private void ResolveReferences()
    {
        if (beltRenderer == null) beltRenderer = transform.Find("BeltRenderer")?.GetComponent<SpriteRenderer>();
        if (indicatorRoot == null) indicatorRoot = transform.Find("DirectionIndicator");
        if ((markers == null || markers.Length == 0) && indicatorRoot != null)
        {
            markers = new Transform[indicatorRoot.childCount];
            for (int i = 0; i < markers.Length; i++) markers[i] = indicatorRoot.GetChild(i);
        }
        if (initialized) return;
        initialMarkerPositions = markers == null ? System.Array.Empty<Vector3>() : new Vector3[markers.Length];
        for (int i = 0; i < initialMarkerPositions.Length; i++)
            initialMarkerPositions[i] = markers[i] != null ? markers[i].localPosition : Vector3.zero;
        initialized = true;
    }

    private void ResetPhase()
    {
        ResolveReferences();
        for (int i = 0; i < initialMarkerPositions.Length; i++)
            if (markers[i] != null) markers[i].localPosition = initialMarkerPositions[i];
    }

    private void OnValidate()
    {
        markerSpan = Mathf.Max(.1f, markerSpan);
        scrollScale = Mathf.Max(0f, scrollScale);
    }
}
