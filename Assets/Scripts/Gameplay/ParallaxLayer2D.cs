using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class ParallaxLayer2D : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Range(0f, 1f)] private float cameraFollowFactor = .9f;
    [SerializeField] private bool followHorizontal = true;
    [SerializeField] private bool followVertical;

    private Vector3 initialLayerPosition;
    private Vector3 initialCameraPosition;

    public float CameraFollowFactor => cameraFollowFactor;
    public bool FollowsHorizontal => followHorizontal;
    public bool FollowsVertical => followVertical;

    private void Awake()
    {
        ResolveCamera();
        CaptureReferencePose();
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
            followVertical ? cameraDelta.y * cameraFollowFactor : 0f,
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
    }
}
