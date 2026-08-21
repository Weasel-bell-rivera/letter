using System.Collections.Generic;
using UnityEngine;

public interface IRoomResettable { void ResetRoomState(); }

public sealed class RoomResetSystem : MonoBehaviour
{
    [SerializeField] private PlayerController2D player;
    [SerializeField] private MirrorPlayer2D mirror;
    [SerializeField] private Transform entrance;
    private Vector3 checkpoint;
    private bool resetting;

    public PlayerController2D Player => player;
    public Vector3 Checkpoint => checkpoint;

    private void Awake()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController2D>();
        if (mirror == null) mirror = FindFirstObjectByType<MirrorPlayer2D>();
        checkpoint = entrance != null ? entrance.position : player.transform.position;
    }

    public void Configure(PlayerController2D target, MirrorPlayer2D mirrorSystem, Transform roomEntrance)
    { player = target; mirror = mirrorSystem; entrance = roomEntrance; checkpoint = entrance.position; }

    public void SetCheckpoint(Vector3 position) => checkpoint = position;

    public void ResetRoom()
    {
        if (resetting || player == null) return;
        resetting = true;
        player.SetControlEnabled(false);
        mirror?.RecallImmediate();
        foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (behaviour is IRoomResettable resettable) resettable.ResetRoomState();
        player.TeleportTo(checkpoint);
        Physics2D.SyncTransforms();
        player.SetControlEnabled(true);
        resetting = false;
    }

    public void OnResetRoom() => ResetRoom();
}
