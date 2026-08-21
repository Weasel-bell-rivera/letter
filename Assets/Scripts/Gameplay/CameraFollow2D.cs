using UnityEngine;

public sealed class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private bool followVertical;
    private Vector3 offset;

    public void Configure(Transform followTarget, bool vertical = false)
    {
        target = followTarget;
        followVertical = vertical;
        offset = transform.position - followTarget.position;
    }

    private void Start()
    {
        if (target != null) offset = transform.position - target.position;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 next = transform.position;
        next.x = target.position.x + offset.x;
        if (followVertical) next.y = target.position.y + offset.y;
        transform.position = next;
    }
}
