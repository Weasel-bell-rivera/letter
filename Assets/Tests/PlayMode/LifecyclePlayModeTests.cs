using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools.Utils;
using System.IO;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Collections.Generic;

public sealed class LifecyclePlayModeTests
{
    [SetUp] public void ResetRunState() => MirrorAbilityState.ResetForTests();

    [UnityTest] public IEnumerator ResetReturnsPlayerAndClearsVelocity()
    {
        GameObject player = new("Player"); player.AddComponent<BoxCollider2D>(); var body = player.AddComponent<Rigidbody2D>(); var controller = player.AddComponent<PlayerController2D>();
        GameObject root = new("Room"); GameObject entrance = new("Entrance"); entrance.transform.SetParent(root.transform); var mirror = player.AddComponent<MirrorPlayer2D>(); mirror.Configure(controller); var reset = root.AddComponent<RoomResetSystem>(); reset.Configure(controller, mirror, entrance.transform);
        Vector3 start = entrance.transform.position; player.transform.position = Vector3.right * 5; body.linearVelocity = Vector2.one; reset.ResetRoom(); yield return null;
        Assert.That(player.transform.position, Is.EqualTo(start)); Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero)); Object.Destroy(root); Object.Destroy(player);
    }

    [UnityTest] public IEnumerator MirrorAbilityPickupUnlocksPlacementForCurrentRun()
    {
        GameObject ground = new("Ground"); ground.transform.position = Vector3.down; BoxCollider2D groundCollider = ground.AddComponent<BoxCollider2D>(); groundCollider.size = new Vector2(8f, 1f); ground.AddComponent<MirrorSurface2D>();
        GameObject player = new("Player"); player.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.8f); player.AddComponent<Rigidbody2D>(); PlayerController2D controller = player.AddComponent<PlayerController2D>();
        MirrorPlayer2D mirror = player.AddComponent<MirrorPlayer2D>(); mirror.Configure(controller); mirror.SetInitiallyUnlocked(false);
        GameObject pickupObject = new("Pickup"); pickupObject.transform.position = Vector3.right * 20f; pickupObject.AddComponent<BoxCollider2D>().isTrigger = true; MirrorAbilityPickup2D pickup = pickupObject.AddComponent<MirrorAbilityPickup2D>();
        yield return new WaitForFixedUpdate();
        Assert.That(mirror.TryPlace(), Is.False); Assert.That(mirror.LastFailure, Is.EqualTo(MirrorPlayer2D.PlacementFailure.NotUnlocked));
        Assert.That(pickup.TryCollect(controller), Is.True); Assert.That(MirrorAbilityState.UnlockedThisRun, Is.True);
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        Assert.That(mirror.HeldMirrorVisual, Is.Not.Null); Assert.That(mirror.HeldMirrorVisual.activeSelf, Is.True);
        Object.Destroy(ground); Object.Destroy(player); Object.Destroy(pickupObject);
    }

    [UnityTest] public IEnumerator PermanentPickupCannotRewardTwice()
    {
        GameObject player = new("Player"); player.AddComponent<BoxCollider2D>(); player.AddComponent<Rigidbody2D>(); PlayerController2D controller = player.AddComponent<PlayerController2D>();
        GameObject pickupObject = new("Pickup"); pickupObject.AddComponent<BoxCollider2D>(); PermanentPickup2D pickup = pickupObject.AddComponent<PermanentPickup2D>();
        pickup.Configure("FIRE_004:COLLECTIBLE:01", PermanentPickupType.Collectible, null);
        yield return null;
        Assert.That(pickup.TryCollect(controller), Is.True);
        Assert.That(SaveService.Instance.HasCollected("FIRE_004:COLLECTIBLE:01"), Is.True);
        Assert.That(SaveService.Instance.TryCollectPermanent("FIRE_004:COLLECTIBLE:01", PermanentPickupType.Collectible), Is.False);
        Object.Destroy(player); Object.Destroy(pickupObject);
    }

    [UnityTest] public IEnumerator WriteFailureKeepsPermanentRewardInMemoryForRetry()
    {
        SaveData state = SaveData.CreateNew();
        SaveService.Instance.ReplaceStateForTests(state, new LocalSaveStore(Path.Combine("/dev/null", "w1-save-test")));
        Assert.That(SaveService.Instance.TryCollectPermanent(SaveIds.MirrorPickup, PermanentPickupType.Ability, SaveIds.MirrorAbility), Is.True);
        yield return null;
        Assert.That(SaveService.Instance.HasAbility(SaveIds.MirrorAbility), Is.True);
        Assert.That(SaveService.Instance.HasCollected(SaveIds.MirrorPickup), Is.True);
        Assert.That(SaveService.Instance.HasUnsavedChanges, Is.True);
        Assert.That(SaveService.Instance.LastWriteError, Is.Not.Null);
    }

    [UnityTest] public IEnumerator Center001LoadsLockedAndExitsToCenter002AfterPickup()
    {
        SceneManager.LoadScene("Center_001"); yield return null; yield return new WaitForFixedUpdate();
        MirrorSurface2D[] grounds = Object.FindObjectsByType<MirrorSurface2D>(FindObjectsSortMode.None);
        Assert.That(grounds, Has.Length.EqualTo(1), "CENTER_001 must use one continuous active ground.");
        BoxCollider2D groundCollider = grounds[0].GetComponent<BoxCollider2D>();
        Assert.That(groundCollider.bounds.min.x, Is.LessThanOrEqualTo(-14f));
        Assert.That(groundCollider.bounds.max.x, Is.GreaterThanOrEqualTo(14f));
        MirrorPlayer2D mirror = Object.FindFirstObjectByType<MirrorPlayer2D>(); MirrorAbilityPickup2D pickup = Object.FindFirstObjectByType<MirrorAbilityPickup2D>();
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Unobtained));
        Assert.That(pickup.TryCollect(Object.FindFirstObjectByType<PlayerController2D>()), Is.True);
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        RoomExit2D exit = Object.FindFirstObjectByType<RoomExit2D>(); Assert.That(exit.TargetScene, Is.EqualTo("Center_002"));
    }

    [UnityTest] public IEnumerator Center001PlayerCanJumpAfterMirrorPickup()
    {
        InputSettings.BackgroundBehavior previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        Keyboard keyboard = Keyboard.current;
        bool addedKeyboard = keyboard == null;
        if (addedKeyboard) keyboard = InputSystem.AddDevice<Keyboard>();
        Mouse mouse = Mouse.current;
        bool addedMouse = mouse == null;
        if (addedMouse) mouse = InputSystem.AddDevice<Mouse>();

        SceneManager.LoadScene("Center_001");
        yield return null;
        yield return new WaitForFixedUpdate();

        PlayerController2D player = Object.FindFirstObjectByType<PlayerController2D>();
        MirrorAbilityPickup2D pickup = Object.FindFirstObjectByType<MirrorAbilityPickup2D>();
        Assert.That(player.IsGroundedNow, Is.True, "Player must start grounded for the jump regression check.");
        PressJump(keyboard);
        yield return new WaitForFixedUpdate();
        ReleaseJump(keyboard);
        player.TeleportTo(new Vector3(-13f, -2.1f, 0f));
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();

        Assert.That(pickup.TryCollect(player), Is.True);
        int jumpSequenceBefore = player.JumpSequence;
        PressJump(keyboard);
        yield return new WaitForFixedUpdate();

        Assert.That(player.JumpSequence, Is.EqualTo(jumpSequenceBefore + 1));
        Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.y, Is.GreaterThan(0f));

        ReleaseJump(keyboard);
        if (addedKeyboard) InputSystem.RemoveDevice(keyboard);
        if (addedMouse) InputSystem.RemoveDevice(mouse);
        InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
    }

    [UnityTest] public IEnumerator Center001PlayerCanJumpAfterTriggerPickupAndMirrorPlacement()
    {
        InputSettings.BackgroundBehavior previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        Keyboard keyboard = Keyboard.current;
        bool addedKeyboard = keyboard == null;
        if (addedKeyboard) keyboard = InputSystem.AddDevice<Keyboard>();
        Mouse mouse = Mouse.current;
        bool addedMouse = mouse == null;
        if (addedMouse) mouse = InputSystem.AddDevice<Mouse>();

        SceneManager.LoadScene("Center_001");
        yield return null;
        yield return new WaitForFixedUpdate();

        PlayerController2D player = Object.FindFirstObjectByType<PlayerController2D>();
        MirrorPlayer2D mirror = Object.FindFirstObjectByType<MirrorPlayer2D>();
        MirrorAbilityPickup2D pickup = Object.FindFirstObjectByType<MirrorAbilityPickup2D>();
        player.TeleportTo(pickup.transform.position);
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));

        player.TeleportTo(new Vector3(5f, -2.1f, 0f));
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        Assert.That(player.IsGroundedNow, Is.True);
        Assert.That(mirror.TryPlace(), Is.True, mirror.LastFailure.ToString());

        int playerJumpBefore = player.JumpSequence;
        PressJump(keyboard);
        yield return new WaitForFixedUpdate();

        Assert.That(player.JumpSequence, Is.EqualTo(playerJumpBefore + 1));
        Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.y, Is.GreaterThan(0f));

        ReleaseJump(keyboard);
        mirror.RecallImmediate();
        if (addedKeyboard) InputSystem.RemoveDevice(keyboard);
        if (addedMouse) InputSystem.RemoveDevice(mouse);
        InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
    }

    private static void PressJump(Keyboard keyboard)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
        InputSystem.Update();
    }

    private static void ReleaseJump(Keyboard keyboard)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();
    }
    [UnityTest] public IEnumerator HazardKillsCloneWithoutDestroyingPlayer()
    {
        GameObject player = new("Player"); player.AddComponent<BoxCollider2D>(); player.AddComponent<Rigidbody2D>(); var controller = player.AddComponent<PlayerController2D>();
        GameObject clone = new("Clone"); clone.AddComponent<BoxCollider2D>(); clone.AddComponent<Rigidbody2D>(); var cloneController = clone.AddComponent<MirrorCloneController2D>();
        bool died = false; cloneController.Died += () => died = true; cloneController.Die(); yield return null;
        Assert.That(died, Is.True); Assert.That(player, Is.Not.Null); Object.Destroy(player);
    }

    [UnityTest] public IEnumerator Fire001LoadsAndMirrorRejectsRepeatedPlacement()
    {
        UnlockMirrorForTests();
        SceneManager.LoadScene("Fire_001");
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        MirrorPlayer2D mirror = Object.FindFirstObjectByType<MirrorPlayer2D>();
        Assert.That(mirror, Is.Not.Null);
        Assert.That(mirror.TryPlace(), Is.True, mirror.LastFailure.ToString());
        PlayerController2D player = Object.FindFirstObjectByType<PlayerController2D>();
        Assert.That(player.VisualRoot.localPosition, Is.EqualTo(Vector3.zero).Using(Vector3ComparerWithEqualsOperator.Instance));
        Assert.That(mirror.Clone.transform.position, Is.EqualTo(player.transform.position).Using(Vector3ComparerWithEqualsOperator.Instance));
        SpriteRenderer playerRenderer = player.VisualRoot.GetComponentInChildren<SpriteRenderer>();
        SpriteRenderer cloneRenderer = mirror.Clone.GetComponentInChildren<SpriteRenderer>();
        Assert.That(cloneRenderer.bounds.size.x, Is.EqualTo(playerRenderer.bounds.size.x).Within(.001f));
        Assert.That(cloneRenderer.bounds.size.y, Is.EqualTo(playerRenderer.bounds.size.y).Within(.001f));
        SpriteRenderer mirrorRenderer = mirror.PlacedMirror.GetComponentInChildren<SpriteRenderer>();
        Assert.That(mirrorRenderer.bounds.min.y, Is.EqualTo(player.GetComponent<BoxCollider2D>().bounds.min.y).Within(.02f));
        Assert.That(mirror.TryPlace(), Is.False);
        mirror.RecallImmediate();
        yield return null;
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        Assert.That(mirror.TryPlace(), Is.True, mirror.LastFailure.ToString());
        mirror.RecallImmediate();
    }

    [UnityTest] public IEnumerator RecallImmediatelyDisablesCloneAndMirrorVisual()
    {
        UnlockMirrorForTests();
        SceneManager.LoadScene("Fire_001"); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        MirrorPlayer2D mirror = Object.FindFirstObjectByType<MirrorPlayer2D>();
        Assert.That(mirror.TryPlace(), Is.True, mirror.LastFailure.ToString());
        GameObject clone = mirror.Clone.gameObject; GameObject placedMirror = mirror.PlacedMirror;
        mirror.RecallImmediate();
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        Assert.That(mirror.Clone, Is.Null); Assert.That(mirror.PlacedMirror, Is.Null);
        Assert.That(clone.activeSelf, Is.False); Assert.That(placedMirror.activeSelf, Is.False);
        Assert.That(mirror.HeldMirrorVisual, Is.Not.Null); Assert.That(mirror.HeldMirrorVisual.activeSelf, Is.True);
        yield return null;
    }

    [UnityTest] public IEnumerator RecallActionRightMouseReturnsMirrorToHeldState()
    {
        UnlockMirrorForTests();
        List<InputDevice> oldMice = new();
        foreach (InputDevice device in InputSystem.devices) if (device is Mouse) oldMice.Add(device);
        foreach (InputDevice device in oldMice) InputSystem.RemoveDevice(device);
        Mouse mouse = InputSystem.AddDevice<Mouse>();
        SceneManager.LoadScene("Fire_001"); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        MirrorPlayer2D mirror = Object.FindFirstObjectByType<MirrorPlayer2D>();
        Assert.That(mirror.TryPlace(), Is.True, mirror.LastFailure.ToString());
        Assert.That(mirror.RecallInputReady, Is.True, "Recall action was not bound and enabled.");
        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.Update(); yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right));
        InputSystem.Update();
        Assert.That(mouse.rightButton.isPressed, Is.True, "Synthetic right mouse state was not applied.");
        Assert.That(mirror.RecallInputValue, Is.GreaterThan(.5f), "Recall action did not read the right mouse state.");
        yield return null;
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));
        Assert.That(mirror.Clone, Is.Null); Assert.That(mirror.PlacedMirror, Is.Null);
        Assert.That(mirror.HeldMirrorVisual, Is.Not.Null); Assert.That(mirror.HeldMirrorVisual.activeSelf, Is.True);
        InputSystem.QueueStateEvent(mouse, new MouseState()); InputSystem.Update();
    }

    [UnityTest] public IEnumerator PlaceActionLeftMouseCreatesMirrorClone()
    {
        UnlockMirrorForTests();
        List<InputDevice> oldMice = new();
        foreach (InputDevice device in InputSystem.devices) if (device is Mouse) oldMice.Add(device);
        foreach (InputDevice device in oldMice) InputSystem.RemoveDevice(device);
        Mouse mouse = InputSystem.AddDevice<Mouse>();
        SceneManager.LoadScene("Fire_001");
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        MirrorPlayer2D mirror = Object.FindFirstObjectByType<MirrorPlayer2D>();
        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Held));

        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.Update();
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        InputSystem.Update();
        Assert.That(mouse.leftButton.isPressed, Is.True, "Synthetic left mouse state was not applied.");
        yield return null;

        Assert.That(mirror.State, Is.EqualTo(MirrorPlayer2D.MirrorState.Placed), mirror.LastFailure.ToString());
        Assert.That(mirror.Clone, Is.Not.Null);
        Assert.That(mirror.PlacedMirror, Is.Not.Null);
        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.Update();
        mirror.RecallImmediate();
    }

    private static void UnlockMirrorForTests()
    {
        SaveData data = SaveData.CreateNew();
        data.unlockedAbilities.Add(SaveIds.MirrorAbility);
        data.collectedPermanentIds.Add(SaveIds.MirrorPickup);
        SaveService.Instance.ReplaceStateForTests(data);
    }
}
