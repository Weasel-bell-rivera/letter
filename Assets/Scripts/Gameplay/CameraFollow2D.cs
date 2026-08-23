using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private bool followVertical;
    [SerializeField, Min(0f)] private float smoothTime;
    [SerializeField] private bool constrainToRoomBounds;
    [SerializeField] private Rect roomBounds;

    private Camera controlledCamera;
    private Vector3 offset;
    private Vector3 followVelocity;
    private bool offsetInitialized;

    public Transform Target => target;
    public bool FollowsVertical => followVertical;
    public float SmoothTime => smoothTime;
    public bool UsesRoomBounds => constrainToRoomBounds;
    public Rect RoomBounds => roomBounds;

    public void Configure(Transform followTarget, bool vertical = false)
    {
        target = followTarget;
        followVertical = vertical;
        offset = followTarget != null ? transform.position - followTarget.position : Vector3.zero;
        offsetInitialized = followTarget != null;
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

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
    }

    private void Start()
    {
        EnsureOffset();
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null) return;
        EnsureOffset();

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
        EnsureOffset();
        followVelocity = Vector3.zero;
        transform.position = ConstrainToRoom(DesiredTargetPosition());
    }

    private void EnsureOffset()
    {
        if (target == null || offsetInitialized) return;
        offset = transform.position - target.position;
        offsetInitialized = true;
    }

    private Vector3 DesiredTargetPosition()
    {
        Vector3 desired = transform.position;
        desired.x = target.position.x + offset.x;
        if (followVertical) desired.y = target.position.y + offset.y;
        return desired;
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
