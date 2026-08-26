using System.Collections.Generic;
using UnityEngine;

public interface IRoomResettable { void ResetRoomState(); }
public interface IOrderedRoomResettable { int ResetOrder { get; } }

public sealed class RoomResetSystem : MonoBehaviour
{
    [SerializeField] private PlayerController2D player;
    [SerializeField] private MirrorPlayer2D mirror;
    [SerializeField] private Transform entrance;
    [SerializeField] private CameraFollow2D cameraFollow;
    private Vector3 checkpoint;
    private bool resetting;

    public PlayerController2D Player => player;
    public Vector3 Checkpoint => checkpoint;
    public Transform Entrance => entrance;

    private void Awake()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController2D>();
        if (mirror == null) mirror = FindFirstObjectByType<MirrorPlayer2D>();
        if (cameraFollow == null) cameraFollow = FindAnyObjectByType<CameraFollow2D>();
        checkpoint = entrance != null ? entrance.position : player != null ? player.transform.position : transform.position;
    }

    public void Configure(PlayerController2D target, MirrorPlayer2D mirrorSystem, Transform roomEntrance,
        CameraFollow2D roomCamera = null)
    {
        player = target;
        mirror = mirrorSystem;
        entrance = roomEntrance;
        cameraFollow = roomCamera;
        checkpoint = entrance != null ? entrance.position : target != null ? target.transform.position : transform.position;
    }

    public void SetCheckpoint(Vector3 position) => checkpoint = position;

    public void ResetRoom()
    {
        if (resetting || player == null) return;
        resetting = true;
        player.SetControlEnabled(false);
        mirror?.RecallImmediate();
        List<IRoomResettable> resettables = new();
        foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (behaviour is IRoomResettable resettable) resettables.Add(resettable);
        resettables.Sort((left, right) => ResetOrder(left).CompareTo(ResetOrder(right)));
        foreach (IRoomResettable resettable in resettables) resettable.ResetRoomState();
        player.TeleportTo(checkpoint);
        Physics2D.SyncTransforms();
        if (cameraFollow != null)
        {
            bool resetToEntrance = entrance != null &&
                Vector2.SqrMagnitude((Vector2)checkpoint - (Vector2)entrance.position) <= .0001f;
            if (resetToEntrance) cameraFollow.BeginEntryFraming();
            else cameraFollow.SnapToTarget();
        }
        player.SetControlEnabled(true);
        resetting = false;
    }

    public void OnResetRoom() => ResetRoom();

    private static int ResetOrder(IRoomResettable resettable)
        => resettable is IOrderedRoomResettable ordered ? ordered.ResetOrder : 0;
}
