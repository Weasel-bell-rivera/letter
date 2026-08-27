using System;
using System.Collections.Generic;
using UnityEngine;

public interface ISpringBounceReceiver2D
{
    float SpringGravityMagnitude { get; }
    Vector2 SpringContactVelocity { get; }
    bool ApplySpringBounce(Vector2 outwardNormal, float launchSpeed);
}

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SurfaceSemantic2D))]
public sealed class Spring2D : MonoBehaviour, IRoomResettable
{
    public const float DefaultTopLaunchHeight = 5f;
    public const float DefaultSideLaunchSpeed = 8f;
    public const float DefaultMinimumApproachSpeed = .1f;

    [SerializeField, Min(.1f)] private float topLaunchHeight = DefaultTopLaunchHeight;
    [SerializeField, Min(.1f)] private float sideLaunchSpeed = DefaultSideLaunchSpeed;
    [SerializeField, Min(0f)] private float minimumApproachSpeed = DefaultMinimumApproachSpeed;

    private readonly Dictionary<Collider2D, MonoBehaviour> contactOwners = new();
    private readonly HashSet<MonoBehaviour> bouncedOwners = new();
    private readonly List<Collider2D> staleContacts = new();
    private readonly List<MonoBehaviour> staleOwners = new();
    private Rigidbody2D body;
    private BoxCollider2D springCollider;
    private SurfaceSemantic2D surfaceSemantic;

    public float TopLaunchHeight => topLaunchHeight;
    public float SideLaunchSpeed => sideLaunchSpeed;
    public float MinimumApproachSpeed => minimumApproachSpeed;
    public int ActiveContactCount => contactOwners.Count;
    public event Action Bounced;

    private void Awake()
    {
        ResolveReferences();
        ApplyRequiredConfiguration();
    }

    private void FixedUpdate() => PruneDestroyedContacts();

    private void OnCollisionEnter2D(Collision2D collision)
    {
        RegisterContact(collision);
        TryBounce(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        RegisterContact(collision);
        TryBounce(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        Collider2D receiverCollider = ResolveReceiverCollider(collision);
        if (receiverCollider == null || !contactOwners.Remove(receiverCollider, out MonoBehaviour owner)) return;
        if (!HasRemainingContact(owner)) bouncedOwners.Remove(owner);
    }

    public void Configure(float targetTopHeight, float horizontalLaunchSpeed, float approachThreshold)
    {
        topLaunchHeight = Mathf.Max(.1f, targetTopHeight);
        sideLaunchSpeed = Mathf.Max(.1f, horizontalLaunchSpeed);
        minimumApproachSpeed = Mathf.Max(0f, approachThreshold);
    }

    public void ResetRoomState()
    {
        contactOwners.Clear();
        bouncedOwners.Clear();
        staleContacts.Clear();
        staleOwners.Clear();
        ResolveReferences();
        ApplyRequiredConfiguration();
    }

    private void TryBounce(Collision2D collision)
    {
        if (!TryResolveReceiver(collision, out ISpringBounceReceiver2D receiver,
                out MonoBehaviour receiverBehaviour)) return;

        if (bouncedOwners.Contains(receiverBehaviour)) return;
        if (!TrySelectFace(collision, out Vector2 outwardNormal)) return;

        Vector2 contactVelocity = receiver.SpringContactVelocity;
        float inwardSpeed = -Vector2.Dot(contactVelocity, outwardNormal);
        if (inwardSpeed + .0001f < minimumApproachSpeed) return;

        float launchSpeed = Vector2.Dot(outwardNormal, Vector2.up) > .999f
            ? Mathf.Sqrt(2f * Mathf.Max(0f, receiver.SpringGravityMagnitude) * topLaunchHeight)
            : sideLaunchSpeed;
        if (launchSpeed <= 0f || !receiver.ApplySpringBounce(outwardNormal, launchSpeed)) return;

        bouncedOwners.Add(receiverBehaviour);
        Bounced?.Invoke();
    }

    private bool TrySelectFace(Collision2D collision, out Vector2 outwardNormal)
    {
        outwardNormal = Vector2.zero;
        if (springCollider == null || collision.contactCount == 0) return false;

        float bestApproach = float.NegativeInfinity;
        float bestDistance = float.PositiveInfinity;
        int bestPriority = int.MaxValue;
        Vector2 contactVelocity = TryResolveReceiver(collision, out ISpringBounceReceiver2D receiver, out _)
            ? receiver.SpringContactVelocity
            : Vector2.zero;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector2 point = transform.InverseTransformPoint(collision.GetContact(i).point);
            Vector2 center = springCollider.offset;
            Vector2 half = springCollider.size * .5f;
            float topDistance = Mathf.Abs(point.y - (center.y + half.y));
            float leftDistance = Mathf.Abs(point.x - (center.x - half.x));
            float rightDistance = Mathf.Abs(point.x - (center.x + half.x));
            float bottomDistance = Mathf.Abs(point.y - (center.y - half.y));

            float faceDistance = topDistance;
            int priority = 0;
            Vector2 localNormal = Vector2.up;
            if (leftDistance < faceDistance - .0001f)
            {
                faceDistance = leftDistance;
                priority = 1;
                localNormal = Vector2.left;
            }
            if (rightDistance < faceDistance - .0001f)
            {
                faceDistance = rightDistance;
                priority = 2;
                localNormal = Vector2.right;
            }
            if (bottomDistance < faceDistance - .0001f) continue;

            Vector2 worldNormal = transform.TransformDirection(localNormal).normalized;
            float approach = -Vector2.Dot(contactVelocity, worldNormal);
            if (approach < bestApproach - .0001f) continue;
            if (Mathf.Abs(approach - bestApproach) <= .0001f && faceDistance > bestDistance + .0001f) continue;
            if (Mathf.Abs(approach - bestApproach) <= .0001f &&
                Mathf.Abs(faceDistance - bestDistance) <= .0001f && priority >= bestPriority) continue;

            bestApproach = approach;
            bestDistance = faceDistance;
            bestPriority = priority;
            outwardNormal = worldNormal;
        }

        return outwardNormal.sqrMagnitude > .99f;
    }

    private void RegisterContact(Collision2D collision)
    {
        Collider2D receiverCollider = ResolveReceiverCollider(collision);
        if (receiverCollider == null || contactOwners.ContainsKey(receiverCollider)) return;
        if (!TryResolveReceiver(collision, out _, out MonoBehaviour receiverBehaviour)) return;
        contactOwners.Add(receiverCollider, receiverBehaviour);
    }

    private static Collider2D ResolveReceiverCollider(Collision2D collision)
    {
        Rigidbody2D receiverBody = collision.rigidbody;
        if (receiverBody == null) return null;
        if (collision.collider != null && collision.collider.attachedRigidbody == receiverBody)
            return collision.collider;
        return collision.otherCollider != null && collision.otherCollider.attachedRigidbody == receiverBody
            ? collision.otherCollider
            : null;
    }

    private static bool TryResolveReceiver(Collision2D collision, out ISpringBounceReceiver2D receiver,
        out MonoBehaviour receiverBehaviour)
    {
        receiver = null;
        receiverBehaviour = null;
        Rigidbody2D receiverBody = collision.rigidbody;
        if (receiverBody == null) return false;
        foreach (MonoBehaviour behaviour in receiverBody.GetComponents<MonoBehaviour>())
        {
            if (behaviour is not ISpringBounceReceiver2D candidate) continue;
            receiver = candidate;
            receiverBehaviour = behaviour;
            return true;
        }
        return false;
    }

    private bool HasRemainingContact(MonoBehaviour owner)
    {
        foreach (MonoBehaviour currentOwner in contactOwners.Values)
            if (currentOwner == owner) return true;
        return false;
    }

    private void PruneDestroyedContacts()
    {
        staleContacts.Clear();
        foreach (KeyValuePair<Collider2D, MonoBehaviour> pair in contactOwners)
            if (pair.Key == null) staleContacts.Add(pair.Key);
        foreach (Collider2D staleContact in staleContacts) contactOwners.Remove(staleContact);

        staleOwners.Clear();
        foreach (MonoBehaviour owner in bouncedOwners)
            if (owner == null || !HasRemainingContact(owner)) staleOwners.Add(owner);
        foreach (MonoBehaviour owner in staleOwners) bouncedOwners.Remove(owner);
    }

    private void ResolveReferences()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (springCollider == null) springCollider = GetComponent<BoxCollider2D>();
        if (surfaceSemantic == null) surfaceSemantic = GetComponent<SurfaceSemantic2D>();
    }

    private void ApplyRequiredConfiguration()
    {
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Static;
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }
        surfaceSemantic?.Configure(SurfaceSemantic2D.SurfaceType.Spring, true, true);
    }

    private void OnValidate()
    {
        topLaunchHeight = Mathf.Max(.1f, topLaunchHeight);
        sideLaunchSpeed = Mathf.Max(.1f, sideLaunchSpeed);
        minimumApproachSpeed = Mathf.Max(0f, minimumApproachSpeed);
    }
}
