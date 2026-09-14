using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class LadderAssetTests
{
    private const string LadderPrefabPath = "Assets/Prefabs/Gameplay/Devices/Ladder2D.prefab";
    private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

    [Test]
    public void LadderPrefabIsAnExplicitVerticalNonSolidTrigger()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LadderPrefabPath);
        Assert.That(prefab, Is.Not.Null);
        Ladder2D ladder = prefab.GetComponent<Ladder2D>();
        BoxCollider2D trigger = prefab.GetComponent<BoxCollider2D>();
        Assert.That(ladder, Is.Not.Null);
        Assert.That(trigger, Is.Not.Null);
        Assert.That(trigger.isTrigger, Is.True);
        Assert.That(ladder.TriggerCollider, Is.SameAs(trigger));
        Assert.That(prefab.GetComponent<Rigidbody2D>(), Is.Null);
        Assert.That(prefab.GetComponent<SurfaceSemantic2D>(), Is.Null);
        Assert.That(prefab.transform.rotation, Is.EqualTo(Quaternion.identity));
        Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));
        Assert.That(prefab.GetComponentInChildren<SpriteRenderer>(true)?.sprite, Is.Not.Null);
    }

    [Test]
    public void PlayerMoveActionUsesUnnormalizedTwoDimensionalKeyboardComposites()
    {
        InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        InputAction move = actions?.FindAction("Player/Move");
        Assert.That(move, Is.Not.Null);
        Assert.That(move.expectedControlType, Is.EqualTo("Vector2"));
        Assert.That(move.bindings.Count(binding => binding.isComposite && binding.path == "2DVector(mode=1)"),
            Is.EqualTo(2));
        string[] paths = move.bindings.Where(binding => binding.isPartOfComposite)
            .Select(binding => binding.path).ToArray();
        Assert.That(paths, Does.Contain("<Keyboard>/w"));
        Assert.That(paths, Does.Contain("<Keyboard>/s"));
        Assert.That(paths, Does.Contain("<Keyboard>/upArrow"));
        Assert.That(paths, Does.Contain("<Keyboard>/downArrow"));
    }
}
