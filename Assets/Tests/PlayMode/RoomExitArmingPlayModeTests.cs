using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RoomExitArmingPlayModeTests
{
    [UnityTest]
    public IEnumerator ExitRequiresPlayerToLeaveReleaseZoneAfterResetBeforeTriggering()
    {
        SceneManager.LoadScene("Earth_008");
        yield return null;
        yield return new WaitForFixedUpdate();

        PlayerController2D player = Object.FindFirstObjectByType<PlayerController2D>();
        RoomExit2D[] exits = Object.FindObjectsByType<RoomExit2D>(FindObjectsSortMode.None);
        RoomExit2D exit = System.Array.Find(exits, candidate => candidate.TargetScene == "Earth_007");
        Assert.That(player, Is.Not.Null);
        Assert.That(exit, Is.Not.Null);

        exit.Configure(string.Empty);
        player.TeleportTo(exit.transform.position);
        Physics2D.SyncTransforms();
        exit.ResetRoomState();
        yield return new WaitForFixedUpdate();
        Assert.That(exit.IsArmed, Is.False);
        Assert.That(exit.Completed, Is.False);

        player.TeleportTo(new Vector3(-7f, -2.08f, 0f));
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        Assert.That(exit.IsArmed, Is.True);

        player.TeleportTo(exit.transform.position);
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        Assert.That(exit.Completed, Is.True);
    }
}
