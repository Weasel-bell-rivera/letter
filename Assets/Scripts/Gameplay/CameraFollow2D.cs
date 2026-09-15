using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private bool followVertical;
    [SerializeField, Min(0f)] private float smoothTime;
    [SerializeField] private Vector2 framingOffset;
    [SerializeField] private bool useExplicitFramingOffset;
    [SerializeField] private bool constrainToRoomBounds;
    [SerializeField] private Rect roomBounds;
    [SerializeField] private bool alignEntryFramingToBounds;
    [SerializeField] private Rect entryFramingBounds;

    private Camera controlledCamera;
    private Vector3 followVelocity;
    private const float VerticalViewportAnchor = .46f;

    public Transform Target => target;
    public bool FollowsVertical => followVertical;
    public float SmoothTime => smoothTime;
    public Vector2 FramingOffset => framingOffset;
    public bool UsesExplicitFramingOffset => useExplicitFramingOffset;
    public bool UsesRoomBounds => constrainToRoomBounds;
    public Rect RoomBounds => roomBounds;
    public bool AlignsEntryFramingToBounds => alignEntryFramingToBounds;
    public Rect EntryFramingBounds => entryFramingBounds;

    public void Configure(Transform followTarget, bool vertical = false)
    {
        followVertical = vertical;
        BindTarget(followTarget);
    }

    public void ConfigureFraming(Vector2 cameraCenterOffset)
    {
        framingOffset = cameraCenterOffset;
        useExplicitFramingOffset = true;
    }

    public void BindTarget(Transform followTarget)
    {
        target = followTarget;
        followVelocity = Vector3.zero;
        if (target != null) SnapToTarget();
    }

    public void ConfigureDamping(float seconds)
    {
        smoothTime = Mathf.Max(0f, seconds);
    }

    public void ConfigureBounds(Rect bounds)
    {
        if (bounds.width <= 0f || bounds.height <= 0f)
            throw new System.ArgumentOutOfRangeException(nameof(bounds), "Camera room bounds must have positive width and height.");

        roomBounds = bounds;
        constrainToRoomBounds = true;
    }

    public void ClearBounds()
    {
        constrainToRoomBounds = false;
    }

    public void ConfigureEntryFramingBounds(Rect bounds)
    {
        if (bounds.width <= 0f || bounds.height <= 0f)
            throw new System.ArgumentOutOfRangeException(nameof(bounds), "Camera entry framing bounds must have positive width and height.");

        entryFramingBounds = bounds;
        alignEntryFramingToBounds = true;
    }

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
    }

    private void Start()
    {
        if (target != null) BeginEntryFraming();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = ConstrainToRoom(DesiredTargetPosition());
        Vector3 next = smoothTime > 0f
            ? Vector3.SmoothDamp(transform.position, desired, ref followVelocity, smoothTime,
                Mathf.Infinity, Time.unscaledDeltaTime)
            : desired;

        transform.position = ConstrainToRoom(next);
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        followVelocity = Vector3.zero;
        transform.position = ConstrainToRoom(DesiredTargetPosition());
    }

    // Compatibility for existing callers: entry framing now immediately follows the Player.
    public void BeginEntryFraming() => SnapToTarget();

    private Vector3 DesiredTargetPosition()
    {
        Vector3 desired = transform.position;
        desired.x = TargetCameraX();
        if (followVertical) desired.y = TargetCameraY();
        return desired;
    }

    private float TargetCameraX() => target.position.x + (useExplicitFramingOffset ? framingOffset.x : 0f);

    private float TargetCameraY()
    {
        if (useExplicitFramingOffset) return target.position.y + framingOffset.y;
        if (controlledCamera == null) controlledCamera = GetComponent<Camera>();
        float halfHeight = controlledCamera != null && controlledCamera.orthographic
            ? controlledCamera.orthographicSize
            : 0f;
        float playerOffsetFromCenter = (VerticalViewportAnchor - .5f) * halfHeight * 2f;
        return target.position.y - playerOffsetFromCenter;
    }

    private Vector3 ConstrainToRoom(Vector3 position)
    {
        if (!constrainToRoomBounds) return position;
        if (controlledCamera == null) controlledCamera = GetComponent<Camera>();
        if (controlledCamera == null || !controlledCamera.orthographic) return position;

        float halfHeight = controlledCamera.orthographicSize;
        float halfWidth = halfHeight * controlledCamera.aspect;
        position.x = ClampAxis(position.x, roomBounds.xMin, roomBounds.xMax, halfWidth);
        position.y = ClampAxis(position.y, roomBounds.yMin, roomBounds.yMax, halfHeight);
        return position;
    }

    private static float ClampAxis(float desired, float minimum, float maximum, float halfViewExtent)
    {
        float minimumCenter = minimum + halfViewExtent;
        float maximumCenter = maximum - halfViewExtent;
        return minimumCenter <= maximumCenter
            ? Mathf.Clamp(desired, minimumCenter, maximumCenter)
            : (minimum + maximum) * .5f;
    }
}
