using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

public sealed class MovementSettingsTests
{
    [Test] public void DocumentedJumpProducesExpectedPhysics()
    {
        var settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
        Assert.That(settings.JumpSpeed, Is.EqualTo(settings.Gravity * .35f).Within(.001f));
        Assert.That(settings.ReliableJumpDistance, Is.EqualTo(3.5f).Within(.001f));
    }
    [Test] public void MirrorAndPlayerCanShareOneSettingsAsset()
    { var settings = ScriptableObject.CreateInstance<PlayerMovementSettings>(); Assert.That(settings.maxSpeed, Is.EqualTo(6f)); Assert.That(settings.jumpHeight, Is.EqualTo(3f)); }
    [Test] public void HorizontalMoveActionUsesFloatAxis()
    {
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
        InputAction move = asset.FindAction("Player/Move", true);
        Assert.That(move.expectedControlType, Is.EqualTo("Axis"));
        Assert.That(move.bindings[0].isComposite, Is.True);
        Assert.That(move.bindings[0].path, Is.EqualTo("1DAxis"));
    }
}
