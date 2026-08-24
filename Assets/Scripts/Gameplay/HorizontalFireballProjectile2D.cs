using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class HorizontalFireballProjectile2D : MonoBehaviour, IRoomResettable, IOrderedRoomResettable
{
    private Rigidbody2D body;
    private Collider2D damageTrigger;
    private Vector2 direction;
    private float speed;
    private float lifetimeRemaining;
    private float cameraExitMargin;
    private bool launched;
    private bool consumed;
    private HorizontalFireballEnemy2D owner;

    public int ResetOrder => -100;
    public bool IsLaunched => launched && !consumed;
    public Vector2 Direction => direction;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        damageTrigger = GetComponent<Collider2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        damageTrigger.isTrigger = true;
    }

    public void Launch(HorizontalFireballEnemy2D source, Vector2 position, float horizontalDirection,
        float movementSpeed, float maximumLifetime, float exitMargin)
    {
        owner = source;
        direction = horizontalDirection >= 0f ? Vector2.right : Vector2.left;
        speed = movementSpeed;
        lifetimeRemaining = maximumLifetime;
        cameraExitMargin = exitMargin;
        consumed = false;
        launched = true;
        transform.position = position;
        body.position = position;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * direction.x,
            transform.localScale.y, transform.localScale.z);
        gameObject.SetActive(true);
    }

    private void FixedUpdate()
    {
        if (!IsLaunched) return;
        lifetimeRemaining = Mathf.Max(0f, lifetimeRemaining - Time.fixedDeltaTime);
        if (lifetimeRemaining <= 0f || IsBeyondCameraBounds())
        {
            Consume();
            return;
        }

        float distance = speed * Time.fixedDeltaTime;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(body.position,
            Mathf.Max(.05f, damageTrigger.bounds.extents.x * .9f), direction, distance);
        foreach (RaycastHit2D hit in hits)
        {
            Collider2D other = hit.collider;
            if (ShouldIgnore(other)) continue;
            if (TryDamageCharacter(other)) return;
            if (!other.isTrigger)
            {
                Consume();
                return;
            }
        }
        body.MovePosition(body.position + direction * distance);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsLaunched || ShouldIgnore(other)) return;
        if (TryDamageCharacter(other)) return;
        if (!other.isTrigger) Consume();
    }

    private bool TryDamageCharacter(Collider2D other)
    {
        MirrorCloneController2D clone = other.GetComponentInParent<MirrorCloneController2D>();
        if (clone != null)
        {
            Consume();
            clone.Die();
            return true;
        }

        if (other.GetComponentInParent<PlayerController2D>() == null) return false;
        Consume();
        FindAnyObjectByType<RoomResetSystem>()?.ResetRoom();
        return true;
    }

    private bool ShouldIgnore(Collider2D other)
    {
        if (other == null || other == damageTrigger) return true;
        if (owner != null && other.transform.IsChildOf(owner.transform)) return true;
        if (other.GetComponentInParent<HorizontalFireballProjectile2D>() != null) return true;
        if (other.GetComponentInParent<MirrorPlayer2D>() != null &&
            other.GetComponentInParent<PlayerController2D>() == null &&
            other.GetComponentInParent<MirrorCloneController2D>() == null) return true;
        return false;
    }

    private bool IsBeyondCameraBounds()
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic) return false;
        float halfHeight = camera.orthographicSize;
        float halfWidth = halfHeight * camera.aspect;
        Vector2 cameraPosition = camera.transform.position;
        Vector2 position = body.position;
        return position.x < cameraPosition.x - halfWidth - cameraExitMargin ||
               position.x > cameraPosition.x + halfWidth + cameraExitMargin ||
               position.y < cameraPosition.y - halfHeight - cameraExitMargin ||
               position.y > cameraPosition.y + halfHeight + cameraExitMargin;
    }

    private void Consume()
    {
        if (consumed) return;
        consumed = true;
        launched = false;
        owner?.NotifyProjectileConsumed(this);
        Destroy(gameObject);
    }

    public void ResetRoomState() => Consume();
}
