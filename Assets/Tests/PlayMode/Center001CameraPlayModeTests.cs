using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class Center001CameraPlayModeTests
{
    [SetUp]
    public void ResetRunState() => MirrorAbilityState.ResetForTests();

    [UnityTest]
    public IEnumerator SpawnBindsCurrentPlayerAndCameraFollowsWithinExplicitBounds()
    {
        SceneManager.LoadScene("Center_001");
        yield return null;
        yield return new WaitForFixedUpdate();

        Camera camera = Camera.main;
        CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
        PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
        RoomPlayerSpawner2D spawner = Object.FindAnyObjectByType<RoomPlayerSpawner2D>();
        camera.aspect = 16f / 9f;

        Assert.That(follow.Target, Is.SameAs(player.transform));
        Assert.That(spawner.RoomCamera, Is.SameAs(follow));
        Assert.That(camera.orthographicSize, Is.EqualTo(7f).Within(.001f));

        player.TeleportTo(new Vector3(-13f, -2.1f, 0f));
        follow.SnapToTarget();
        Assert.That(camera.transform.position.x - camera.orthographicSize * camera.aspect,
            Is.EqualTo(-14f).Within(.001f));
        Assert.That(camera.transform.position.y, Is.EqualTo(-1.5f).Within(.001f));

        player.TeleportTo(new Vector3(0f, -2.1f, 0f));
        follow.SnapToTarget();
        Assert.That(camera.transform.position.x, Is.EqualTo(0f).Within(.001f));
        Assert.That(camera.transform.position.y, Is.EqualTo(-1.5f).Within(.001f));

        player.TeleportTo(new Vector3(13f, -2.1f, 0f));
        follow.SnapToTarget();
        Assert.That(camera.transform.position.x + camera.orthographicSize * camera.aspect,
            Is.EqualTo(14f).Within(.001f));
        Assert.That(camera.transform.position.y, Is.EqualTo(-1.5f).Within(.001f));
    }

    [UnityTest]
    public IEnumerator ResetKeepsTargetAndSnapsBackToEntranceComposition()
    {
        SceneManager.LoadScene("Center_001");
        yield return null;
        yield return new WaitForFixedUpdate();

        Camera camera = Camera.main;
        camera.aspect = 16f / 9f;
        CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
        PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
        RoomResetSystem reset = Object.FindAnyObjectByType<RoomResetSystem>();

        player.TeleportTo(new Vector3(10f, -2.1f, 0f));
        follow.SnapToTarget();
        reset.ResetRoom();

        Assert.That(follow.Target, Is.SameAs(player.transform));
        Assert.That(player.transform.position, Is.EqualTo(reset.Entrance.position));
        Assert.That(camera.transform.position.x - camera.orthographicSize * camera.aspect,
            Is.EqualTo(-14f).Within(.001f));
        Assert.That(camera.transform.position.y, Is.EqualTo(-1.5f).Within(.001f));
    }
}
