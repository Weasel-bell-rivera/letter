using UnityEngine;
using UnityEngine.Tilemaps;

public interface IFreezingGroundActor2D
{
    Rigidbody2D FreezingBody { get; }
    Collider2D FreezingCollider { get; }
    Vector2 FreezingUpAxis { get; }
    void SetFreezingMovementMultiplier(float multiplier);
    void CompleteFreezingGround();
}

[DefaultExecutionOrder(200)]
public sealed class FreezingGroundActor2D : MonoBehaviour, IRoomResettable, IOrderedRoomResettable
{
    public enum FreezingPhase { Clear, Accumulating, Recovering, Centering, Frozen }

    public const float FreezeRate = .5f;
    public const float RecoveryRate = .75f;
    public const float MinimumMovementMultiplier = .2f;
    public const float CenteringSpeed = 8f;

    private readonly ContactPoint2D[] contacts = new ContactPoint2D[24];
    private IFreezingGroundActor2D actor;
    private Rigidbody2D body;
    private Collider2D actorCollider;
    private float freezeAmount;
    private Tilemap lockedTilemap;
    private FreezingGroundCell2D lockedPrefabCell;
    private Vector3Int lockedCell;
    private bool hasLockedCell;
    private bool completionSent;

    public int ResetOrder => -90;
    public float FreezeAmount => freezeAmount;
    public float MovementMultiplier => Mathf.Lerp(1f, MinimumMovementMultiplier, freezeAmount);
    public bool HasLockedCell => hasLockedCell;
    public Vector3Int LockedCell => lockedCell;
    public FreezingPhase Phase { get; private set; } = FreezingPhase.Clear;

    public static FreezingGroundActor2D Ensure(GameObject owner)
    {
        if (owner == null) return null;
        FreezingGroundActor2D effect = owner.GetComponent<FreezingGroundActor2D>();
        return effect != null ? effect : owner.AddComponent<FreezingGroundActor2D>();
    }

    private void Awake() => ResolveActor();

    private void FixedUpdate()
    {
        if (!ResolveActor() || completionSent) return;
        if (Phase == FreezingPhase.Centering)
        {
            AdvanceCentering();
            return;
        }

        bool touchingFreezingGround = false;
        bool groundedOnOrdinaryGround = false;
        int count = body.GetContacts(contacts);
        for (int i = 0; i < count; i++)
        {
            ContactPoint2D contact = contacts[i];
            Collider2D other = contact.collider != null && contact.collider.attachedRigidbody == body
                ? contact.otherCollider
                : contact.collider;
            if (other == null || other == actorCollider || other.isTrigger ||
                !SurfaceSemantic2D.TryGet(other, out SurfaceSemantic2D semantic) || !semantic.IsSafe) continue;

            if (semantic.Type == SurfaceSemantic2D.SurfaceType.FreezingGround)
            {
                touchingFreezingGround = true;
                if (!hasLockedCell) TryLockCell(other, contact);
            }
        }

        if (!touchingFreezingGround && SurfaceSupport2D.TryResolve(actorCollider, gameObject,
                -actor.FreezingUpAxis, .15f, ~0, null, out SurfaceSemantic2D supportSurface, out _))
            groundedOnOrdinaryGround = supportSurface != null && supportSurface.IsSafe &&
                                       supportSurface.Type == SurfaceSemantic2D.SurfaceType.StaticSolid;

        if (touchingFreezingGround)
        {
            Phase = FreezingPhase.Accumulating;
            freezeAmount = Mathf.Min(1f, freezeAmount + FreezeRate * Time.fixedDeltaTime);
        }
        else if (groundedOnOrdinaryGround && freezeAmount > 0f)
        {
            Phase = FreezingPhase.Recovering;
            freezeAmount = Mathf.Max(0f, freezeAmount - RecoveryRate * Time.fixedDeltaTime);
            if (freezeAmount <= 0f) ClearLockedCell();
        }
        else
        {
            Phase = freezeAmount > 0f ? FreezingPhase.Accumulating : FreezingPhase.Clear;
        }

        actor.SetFreezingMovementMultiplier(MovementMultiplier);
        if (freezeAmount >= 1f)
        {
            Phase = FreezingPhase.Centering;
            actor.SetFreezingMovementMultiplier(0f);
            AdvanceCentering();
        }
    }

    private bool ResolveActor()
    {
        if (actor != null && body != null && actorCollider != null) return true;
        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            if (behaviour is not IFreezingGroundActor2D candidate) continue;
            actor = candidate;
            body = candidate.FreezingBody;
            actorCollider = candidate.FreezingCollider;
            break;
        }
        return actor != null && body != null && actorCollider != null;
    }

    private void TryLockCell(Collider2D surfaceCollider, ContactPoint2D contact)
    {
        FreezingGroundCell2D prefabCell = surfaceCollider.GetComponent<FreezingGroundCell2D>();
        if (prefabCell == null) prefabCell = surfaceCollider.GetComponentInParent<FreezingGroundCell2D>();
        if (prefabCell != null)
        {
            lockedPrefabCell = prefabCell;
            lockedCell = default;
            hasLockedCell = true;
            return;
        }

        Tilemap tilemap = surfaceCollider.GetComponent<Tilemap>();
        if (tilemap == null) tilemap = surfaceCollider.GetComponentInParent<Tilemap>();
        if (tilemap == null) return;

        Vector3 point = contact.point;
        Vector3 normal = contact.normal;
        Vector3Int first = tilemap.WorldToCell(point - normal * .01f);
        Vector3Int second = tilemap.WorldToCell(point + normal * .01f);
        if (tilemap.HasTile(first)) lockedCell = first;
        else if (tilemap.HasTile(second)) lockedCell = second;
        else return;
        lockedTilemap = tilemap;
        hasLockedCell = true;
    }

    private void AdvanceCentering()
    {
        if (body == null)
        {
            CompleteFreeze();
            return;
        }
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        if (!hasLockedCell || (lockedTilemap == null && lockedPrefabCell == null))
        {
            CompleteFreeze();
            return;
        }

        Vector2 target = body.position;
        target.x = lockedPrefabCell != null
            ? lockedPrefabCell.WorldCenter.x
            : lockedTilemap.GetCellCenterWorld(lockedCell).x;
        Vector2 next = Vector2.MoveTowards(body.position, target, CenteringSpeed * Time.fixedDeltaTime);
        body.MovePosition(next);
        if ((next - target).sqrMagnitude <= .000001f) CompleteFreeze();
    }

    private void CompleteFreeze()
    {
        if (completionSent) return;
        completionSent = true;
        Phase = FreezingPhase.Frozen;
        actor?.SetFreezingMovementMultiplier(0f);
        actor?.CompleteFreezingGround();
    }

    private void ClearLockedCell()
    {
        hasLockedCell = false;
        lockedTilemap = null;
        lockedPrefabCell = null;
        lockedCell = default;
    }

    public void ResetRoomState()
    {
        freezeAmount = 0f;
        completionSent = false;
        Phase = FreezingPhase.Clear;
        ClearLockedCell();
        ResolveActor();
        actor?.SetFreezingMovementMultiplier(1f);
    }
}
