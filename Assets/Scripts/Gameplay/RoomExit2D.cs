using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public sealed class RoomExit2D : MonoBehaviour, IRoomResettable
{
    private const float SpawnReleaseClearance = 1f;
    [SerializeField] private string targetScene;
    [SerializeField] private string targetEntranceId = "DEFAULT";
    private Collider2D trigger;
    private PlayerController2D trackedPlayer;
    private Collider2D trackedPlayerCollider;
    private bool armed;
    public bool Completed { get; private set; }
    public bool IsArmed => armed;
    public string TargetScene => targetScene;
    public string TargetEntranceId => targetEntranceId;
    public void Configure(string sceneName, string entranceId = "DEFAULT") { targetScene = sceneName; targetEntranceId = entranceId; }
    private void Awake()
    {
        trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
        Disarm();
    }

    private void FixedUpdate()
    {
        if (armed) return;
        if (trackedPlayer == null)
        {
            trackedPlayer = FindFirstObjectByType<PlayerController2D>();
            trackedPlayerCollider = trackedPlayer != null ? trackedPlayer.GetComponent<Collider2D>() : null;
        }
        if (trackedPlayerCollider == null || trigger == null) return;

        Bounds exitBounds = trigger.bounds;
        Bounds playerBounds = trackedPlayerCollider.bounds;
        bool released = playerBounds.max.x < exitBounds.min.x - SpawnReleaseClearance ||
                        playerBounds.min.x > exitBounds.max.x + SpawnReleaseClearance ||
                        playerBounds.max.y < exitBounds.min.y - SpawnReleaseClearance ||
                        playerBounds.min.y > exitBounds.max.y + SpawnReleaseClearance;
        if (released) armed = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<PlayerController2D>(out PlayerController2D player)) return;
        if (!armed) return;
        if (!string.IsNullOrWhiteSpace(targetScene) && Application.CanStreamedLevelBeLoaded(targetScene))
        {
            Completed = true;
            player.SetControlEnabled(false);
            player.GetComponent<MirrorPlayer2D>()?.RecallImmediate();
            SaveService.Instance.SetPlayerOperable(false);
            string completedSourceRoomId = gameObject.scene.name.ToUpperInvariant();
            RoomTransitionState.Request(targetScene, targetEntranceId, true,
                completedSourceRoomId);
            try
            {
                SceneManager.LoadScene(targetScene);
            }
            catch (Exception exception)
            {
                RoomTransitionState.Cancel();
                Completed = false;
                player.SetControlEnabled(true);
                SaveService.Instance.SetPlayerOperable(true);
                Debug.LogError($"Room transition failed: {exception.GetType().Name}", this);
            }
        }
    }

    public void ResetRoomState()
    {
        Completed = false;
        Disarm();
    }

    private void Disarm()
    {
        armed = false;
        trackedPlayer = null;
        trackedPlayerCollider = null;
    }
}
