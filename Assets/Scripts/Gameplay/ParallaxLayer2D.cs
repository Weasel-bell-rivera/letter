using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class ParallaxLayer2D : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Range(0f, 1f)] private float cameraFollowFactor = .9f;
    [SerializeField] private bool followHorizontal = true;
    [SerializeField] private bool followVertical;
    [SerializeField] private bool useSeparateVerticalFactor;
    [SerializeField, Range(0f, 1f)] private float verticalCameraFollowFactor = .95f;
    [SerializeField] private bool useFixedReferencePose;
    [SerializeField, Tooltip("World-space camera position used when authoring this layer.")]
    private Vector3 referenceCameraPosition;
    [SerializeField, Tooltip("World-space layer position at the reference camera position.")]
    private Vector3 referenceLayerPosition;

    private Vector3 initialLayerPosition;
    private Vector3 initialCameraPosition;

    public float CameraFollowFactor => cameraFollowFactor;
    public bool FollowsHorizontal => followHorizontal;
    public bool FollowsVertical => followVertical;
    public float VerticalCameraFollowFactor => useSeparateVerticalFactor
        ? verticalCameraFollowFactor : cameraFollowFactor;

    private void Awake()
    {
        ResolveCamera();
        CaptureReferencePose();
        if (useFixedReferencePose) LateUpdate();
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
        {
            ResolveCamera();
            if (cameraTransform == null) return;
            CaptureReferencePose();
        }

        Vector3 cameraDelta = cameraTransform.position - initialCameraPosition;
        transform.position = initialLayerPosition + new Vector3(
            followHorizontal ? cameraDelta.x * cameraFollowFactor : 0f,
            followVertical ? cameraDelta.y * VerticalCameraFollowFactor : 0f,
            0f);
    }

    public void Configure(Transform targetCamera, float followFactor, bool horizontal = true, bool vertical = false)
    {
        cameraTransform = targetCamera;
        cameraFollowFactor = Mathf.Clamp01(followFactor);
        followHorizontal = horizontal;
        followVertical = vertical;
        CaptureReferencePose();
    }

    public void CaptureReferencePose()
    {
        if (useFixedReferencePose)
        {
            initialLayerPosition = referenceLayerPosition;
            initialCameraPosition = referenceCameraPosition;
            return;
        }

        initialLayerPosition = transform.position;
        initialCameraPosition = cameraTransform != null ? cameraTransform.position : Vector3.zero;
    }

    private void ResolveCamera()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void OnValidate()
    {
        cameraFollowFactor = Mathf.Clamp01(cameraFollowFactor);
        verticalCameraFollowFactor = Mathf.Clamp01(verticalCameraFollowFactor);
    }
}
