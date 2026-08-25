using System.Collections.Generic;
using UnityEngine;

public sealed class TornadoGenerator2D : MonoBehaviour, IRoomResettable, IOrderedRoomResettable
{
    public const float DefaultSpawnInterval = 2f;
    public const int DefaultMaximumAlive = 3;

    [SerializeField] private MovingTornado2D tornadoPrefab;
    [SerializeField] private Vector2 spawnDirection = Vector2.right;
    [SerializeField, Range(.5f, 10f)] private float spawnInterval = DefaultSpawnInterval;
    [SerializeField, Range(1, 8)] private int maximumAlive = DefaultMaximumAlive;
    [SerializeField] private Vector2 spawnClearance = new(.8f, .8f);
    [SerializeField] private SpriteRenderer visual;

    private readonly List<MovingTornado2D> spawned = new();
    private float remaining;

    public int ResetOrder => -55;
    public MovingTornado2D TornadoPrefab => tornadoPrefab;
    public float SpawnInterval => spawnInterval;
    public int MaximumAlive => maximumAlive;

    private void Awake() => ResetRoomState();

    private void FixedUpdate()
    {
        spawned.RemoveAll(item => item == null);
        remaining = Mathf.Max(0f, remaining - Time.fixedDeltaTime);
        if (remaining > 0f || spawned.Count >= maximumAlive) return;
        remaining = spawnInterval;
        TrySpawn();
    }

    public void Configure(MovingTornado2D prefab, Vector2 direction, float interval, int maxAlive,
        Vector2 clearance)
    {
        tornadoPrefab = prefab;
        spawnDirection = direction.sqrMagnitude > .0001f ? direction.normalized : Vector2.right;
        spawnInterval = Mathf.Clamp(interval, .5f, 10f);
        maximumAlive = Mathf.Clamp(maxAlive, 1, 8);
        spawnClearance = new Vector2(Mathf.Max(.1f, clearance.x), Mathf.Max(.1f, clearance.y));
        ResetRoomState();
    }

    public void ConfigureVisual(SpriteRenderer renderer) => visual = renderer;

    public void ResetRoomState()
    {
        foreach (MovingTornado2D tornado in spawned.ToArray())
            if (tornado != null) tornado.RemoveImmediately();
        spawned.Clear();
        remaining = spawnInterval;
    }

    private void TrySpawn()
    {
        if (tornadoPrefab == null || !IsSpawnSpaceClear()) return;
        MovingTornado2D tornado = Instantiate(tornadoPrefab, transform.position, Quaternion.identity);
        tornado.Configure(spawnDirection, tornadoPrefab.Speed, tornadoPrefab.MaximumDistance);
        tornado.Removed += OnTornadoRemoved;
        spawned.Add(tornado);
    }

    private bool IsSpawnSpaceClear()
    {
        foreach (Collider2D overlap in Physics2D.OverlapBoxAll(transform.position, spawnClearance, 0f))
        {
            if (overlap == null || overlap.isTrigger && overlap.GetComponentInParent<TornadoGenerator2D>() == this)
                continue;
            return false;
        }
        return true;
    }

    private void OnTornadoRemoved(MovingTornado2D tornado)
    {
        if (tornado != null) tornado.Removed -= OnTornadoRemoved;
        spawned.Remove(tornado);
    }
}
