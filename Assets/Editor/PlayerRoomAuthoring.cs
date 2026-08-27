using UnityEngine;

/// <summary>
/// Keeps editor room builders aligned with the canonical runtime Player spawn architecture.
/// </summary>
public static class PlayerRoomAuthoring
{
    public static RoomEntrance2D ConfigureDefaultEntrance(Transform entrance, bool facingRight = true)
    {
        return ConfigureEntrance(entrance, SaveIds.DefaultEntrance, true, facingRight);
    }

    public static RoomEntrance2D ConfigureEntrance(Transform entrance, string entranceId,
        bool isDefault = false, bool facingRight = true)
    {
        RoomEntrance2D component = entrance.GetComponent<RoomEntrance2D>()
            ?? entrance.gameObject.AddComponent<RoomEntrance2D>();
        component.Configure(entranceId, isDefault, facingRight);
        return component;
    }

    public static RoomPlayerSpawner2D ConfigureRoom(GameObject host, Transform entrance,
        RoomResetSystem reset, CameraFollow2D cameraFollow = null, bool facingRight = true)
    {
        ConfigureDefaultEntrance(entrance, facingRight);
        RoomPlayerSpawner2D spawner = host.GetComponent<RoomPlayerSpawner2D>()
            ?? host.AddComponent<RoomPlayerSpawner2D>();
        spawner.ConfigureCamera(cameraFollow);
        reset?.Configure(null, null, entrance, cameraFollow);
        if (cameraFollow != null) cameraFollow.Configure(null, cameraFollow.FollowsVertical);
        return spawner;
    }

}
