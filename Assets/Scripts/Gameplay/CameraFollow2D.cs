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
    private Vector3 initialRoomCameraPosition;
    private Vector2 entryCameraPosition;
    private int horizontalEntrySide;
    private int verticalEntrySide;
    private bool horizontalAcquired;
    private bool verticalAcquired;
    private bool initialRoomPositionCaptured;

    private const float VerticalViewportAnchor = .46f;
    private const float AcquisitionTolerance = .01f;

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
        horizontalAcquired = false;
        verticalAcquired = false;
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
        CaptureInitialRoomPosition();
    }

    private void Start()
    {
        if (target != null) BeginEntryFraming();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        UpdateAcquisition();
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
        horizontalAcquired = true;
        verticalAcquired = followVertical;
        transform.position = ConstrainToRoom(DesiredTargetPosition());
    }

    public void BeginEntryFraming()
    {
        if (target == null) return;
        CaptureInitialRoomPosition();
        followVelocity = Vector3.zero;
        Vector3 initialPosition = initialRoomCameraPosition;
        int initialHorizontalSide = SideOf(TargetCameraX(), initialPosition.x);
        int initialVerticalSide = followVertical ? SideOf(TargetCameraY(), initialPosition.y) : 0;
        if (alignEntryFramingToBounds)
            initialPosition = AlignEntryViewToBounds(initialPosition, initialHorizontalSide, initialVerticalSide);
        transform.position = ConstrainToRoom(initialPosition);
        entryCameraPosition = transform.position;

        horizontalEntrySide = SideOf(TargetCameraX(), entryCameraPosition.x);
        verticalEntrySide = followVertical ? SideOf(TargetCameraY(), entryCameraPosition.y) : 0;
        horizontalAcquired = horizontalEntrySide == 0;
        verticalAcquired = followVertical && verticalEntrySide == 0;
    }

    private Vector3 DesiredTargetPosition()
    {
        Vector3 desired = transform.position;
        desired.x = horizontalAcquired ? TargetCameraX() : entryCameraPosition.x;
        if (followVertical)
            desired.y = verticalAcquired ? TargetCameraY() : entryCameraPosition.y;
        return desired;
    }

    private void UpdateAcquisition()
    {
        if (!horizontalAcquired && HasCrossedEntryLine(TargetCameraX(), entryCameraPosition.x, horizontalEntrySide))
            horizontalAcquired = true;

        if (followVertical && !verticalAcquired &&
            HasCrossedEntryLine(TargetCameraY(), entryCameraPosition.y, verticalEntrySide))
            verticalAcquired = true;
    }

    private float TargetCameraX() => target.position.x + (useExplicitFramingOffset ? framingOffset.x : 0f);

    private void CaptureInitialRoomPosition()
    {
        if (initialRoomPositionCaptured) return;
        initialRoomCameraPosition = transform.position;
        initialRoomPositionCaptured = true;
    }

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

    private static int SideOf(float value, float anchor)
    {
        float delta = value - anchor;
        if (Mathf.Abs(delta) <= AcquisitionTolerance) return 0;
        return delta < 0f ? -1 : 1;
    }

    private static bool HasCrossedEntryLine(float value, float anchor, int entrySide)
    {
        if (entrySide == 0) return true;
        float delta = value - anchor;
        return entrySide < 0 ? delta >= -AcquisitionTolerance : delta <= AcquisitionTolerance;
    }

    private Vector3 AlignEntryViewToBounds(Vector3 position, int horizontalSide, int verticalSide)
    {
        if (controlledCamera == null) controlledCamera = GetComponent<Camera>();
        if (controlledCamera == null || !controlledCamera.orthographic) return position;

        float halfHeight = controlledCamera.orthographicSize;
        float halfWidth = halfHeight * controlledCamera.aspect;
        position.x = AlignAxis(position.x, entryFramingBounds.xMin, entryFramingBounds.xMax,
            halfWidth, horizontalSide);
        if (followVertical)
            position.y = AlignAxis(position.y, entryFramingBounds.yMin, entryFramingBounds.yMax,
                halfHeight, verticalSide);
        return position;
    }

    private static float AlignAxis(float fallback, float minimum, float maximum, float halfViewExtent, int entrySide)
    {
        if (entrySide == 0) return fallback;
        if (maximum - minimum <= halfViewExtent * 2f) return (minimum + maximum) * .5f;
        return entrySide < 0 ? minimum + halfViewExtent : maximum - halfViewExtent;
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
