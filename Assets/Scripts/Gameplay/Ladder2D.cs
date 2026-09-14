using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class Ladder2D : MonoBehaviour
{
    public const float ClimbSpeed = 3f;
    public const float AlignmentSpeed = 10f;
    public const float TopExitPadding = .02f;

    [SerializeField] private BoxCollider2D triggerCollider;
    [SerializeField] private Transform visualRoot;

    public BoxCollider2D TriggerCollider => triggerCollider;
    public float CenterX => triggerCollider != null ? triggerCollider.bounds.center.x : transform.position.x;
    public float TopY => triggerCollider != null ? triggerCollider.bounds.max.y : transform.position.y;
    public bool IsUsable => isActiveAndEnabled && triggerCollider != null && triggerCollider.enabled &&
                            triggerCollider.isTrigger && Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, 0f)) < .01f;

    private void Awake() => ResolveCollider();
    private void OnValidate() => ResolveCollider();

    private void ResolveCollider()
    {
        if (triggerCollider == null) triggerCollider = GetComponent<BoxCollider2D>();
        if (triggerCollider != null) triggerCollider.isTrigger = true;
        if (visualRoot == null)
        {
            SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>(true);
            if (renderer != null) visualRoot = renderer.transform;
        }
        SyncVisualToTrigger();
    }

    private void SyncVisualToTrigger()
    {
        if (triggerCollider == null || visualRoot == null) return;
        SpriteRenderer renderer = visualRoot.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null || renderer.sprite.bounds.size.y <= 0f) return;
        float scale = triggerCollider.size.y / renderer.sprite.bounds.size.y;
        visualRoot.localPosition = triggerCollider.offset;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = new Vector3(scale, scale, 1f);
    }

    public bool Overlaps(Collider2D actor)
        => IsUsable && actor != null && actor.enabled && triggerCollider.bounds.Intersects(actor.bounds);

    public bool TryGetTopExitPosition(Rigidbody2D actorBody, BoxCollider2D actorCollider,
        out Vector2 bodyPosition)
    {
        bodyPosition = actorBody != null ? actorBody.position : default;
        if (!IsUsable || actorBody == null || actorCollider == null) return false;

        Vector2 colliderOffset = (Vector2)actorCollider.bounds.center - actorBody.position;
        Vector2 targetColliderCenter = new(CenterX, TopY + actorCollider.bounds.extents.y + TopExitPadding);
        if (!CanOccupy(actorCollider, targetColliderCenter)) return false;

        bodyPosition = targetColliderCenter - colliderOffset;
        return true;
    }

    private bool CanOccupy(BoxCollider2D actorCollider, Vector2 targetColliderCenter)
    {
        Vector2 querySize = actorCollider.bounds.size * .9f;
        foreach (Collider2D overlap in Physics2D.OverlapBoxAll(targetColliderCenter, querySize, 0f))
        {
            if (overlap == null || overlap == actorCollider || overlap == triggerCollider || overlap.isTrigger)
                continue;
            if (overlap.GetComponent<PlayerController2D>() != null ||
                overlap.GetComponent<MirrorCloneController2D>() != null)
                continue;
            return false;
        }
        return true;
    }
}
