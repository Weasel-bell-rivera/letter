using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class LadderPlayModeTests
{
    [UnityTest]
    public IEnumerator PlayerEntersStopsAndSpaceDetachesWithoutJumpImpulse()
    {
        PlayerMovementSettings settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
        Ladder2D ladder = CreateLadder();
        PlayerController2D player = CreatePlayer(Vector2.zero, settings);
        player.SetMoveInput(Vector2.up);

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.That(player.IsClimbing, Is.True);
        Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.y,
            Is.EqualTo(Ladder2D.ClimbSpeed).Within(.05f));

        player.SetMoveInput(Vector2.zero);
        yield return new WaitForFixedUpdate();
        Assert.That(player.IsClimbing, Is.True);
        Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.y, Is.EqualTo(0f).Within(.01f));

        CharacterLadderMotor2D motor = player.GetComponent<CharacterLadderMotor2D>();
        Assert.That(motor.ProcessMovement(Vector2.zero, 1, out bool consumedJump), Is.False);
        Assert.That(consumedJump, Is.True);
        Assert.That(player.IsClimbing, Is.False);
        Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity, Is.EqualTo(Vector2.zero));

        Object.Destroy(player.gameObject);
        Object.Destroy(ladder.gameObject);
        Object.Destroy(settings);
    }

    [UnityTest]
    public IEnumerator PlayerAndMirrorCloneIndependentlyUseTheSameLadderRule()
    {
        PlayerMovementSettings settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
        Ladder2D ladder = CreateLadder();
        PlayerController2D player = CreatePlayer(new Vector2(-.1f, 0f), settings);
        MirrorCloneController2D clone = CreateClone(new Vector2(.1f, 0f), player);
        player.SetMoveInput(Vector2.up);

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.That(player.IsClimbing, Is.True);
        Assert.That(clone.IsClimbing, Is.True);
        Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.y,
            Is.EqualTo(Ladder2D.ClimbSpeed).Within(.05f));
        Assert.That(clone.GetComponent<Rigidbody2D>().linearVelocity.y,
            Is.EqualTo(Ladder2D.ClimbSpeed).Within(.05f));

        clone.Die();
        yield return null;
        Assert.That(player.IsClimbing, Is.True);

        Object.Destroy(player.gameObject);
        Object.Destroy(ladder.gameObject);
        Object.Destroy(settings);
    }

    private static Ladder2D CreateLadder()
    {
        GameObject ladderObject = new("Ladder");
        BoxCollider2D trigger = ladderObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1f, 6f);
        return ladderObject.AddComponent<Ladder2D>();
    }

    private static PlayerController2D CreatePlayer(Vector2 position, PlayerMovementSettings settings)
    {
        GameObject playerObject = new("Player");
        playerObject.transform.position = position;
        playerObject.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.8f);
        playerObject.AddComponent<Rigidbody2D>();
        PlayerController2D player = playerObject.AddComponent<PlayerController2D>();
        player.Configure(null, settings);
        return player;
    }

    private static MirrorCloneController2D CreateClone(Vector2 position, PlayerController2D source)
    {
        GameObject cloneObject = new("MirrorClone");
        cloneObject.transform.position = position;
        cloneObject.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.8f);
        cloneObject.AddComponent<Rigidbody2D>();
        MirrorCloneController2D clone = cloneObject.AddComponent<MirrorCloneController2D>();
        clone.Configure(source, Vector2.left, Vector2.down);
        Physics2D.IgnoreCollision(source.GetComponent<BoxCollider2D>(), cloneObject.GetComponent<BoxCollider2D>());
        return clone;
    }
}
