using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class PlayerPrefabAssetTests
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Gameplay/Characters/Player.prefab";
    private const string RegistryPath = "Assets/Resources/PlayerPrefabRegistry.asset";

    [Test]
    public void CanonicalPrefabContainsMovementInputMirrorAndAllApprovedSprites()
    {
        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        Assert.That(player, Is.Not.Null);
        Assert.That(player.GetComponent<Rigidbody2D>(), Is.Not.Null);
        Assert.That(player.GetComponent<BoxCollider2D>()?.size, Is.EqualTo(new Vector2(.8f, 1.8f)));
        Assert.That(player.GetComponent<PlayerController2D>(), Is.Not.Null);
        Assert.That(player.GetComponent<PlayerInput>()?.defaultActionMap, Is.EqualTo("Player"));
        Assert.That(player.GetComponent<MirrorPlayer2D>(), Is.Not.Null);

        PlayerVisual2D visual = player.GetComponentInChildren<PlayerVisual2D>(true);
        Assert.That(visual, Is.Not.Null);
        Assert.That(new[]
        {
            visual.IdleSprite, visual.JumpSprite, visual.WalkSpriteA, visual.WalkSpriteB,
            visual.DuckSprite, visual.FrontSprite, visual.HitSprite
        }.All(sprite => sprite != null), Is.True);

        PlayerPrefabRegistry registry = AssetDatabase.LoadAssetAtPath<PlayerPrefabRegistry>(
            RegistryPath);
        Assert.That(registry, Is.Not.Null);
        Assert.That(registry.PlayerPrefab, Is.SameAs(player));
        Assert.That(registry.IsValid(out string error), Is.True, error);
    }

    [Test]
    public void EveryLevelSceneUsesExactlyOneSpawnerAndDefaultEntranceWithoutSerializedPlayer()
    {
        string[] scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes/Levels" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
        Assert.That(scenePaths, Is.Not.Empty);

        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string path in scenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Assert.That(ComponentsInScene<PlayerController2D>(scene), Is.Empty, path);
                Assert.That(ComponentsInScene<MirrorPlayer2D>(scene), Is.Empty, path);
                Assert.That(ComponentsInScene<RoomPlayerSpawner2D>(scene), Has.Length.EqualTo(1), path);
                RoomEntrance2D[] entrances = ComponentsInScene<RoomEntrance2D>(scene);
                Assert.That(entrances.Count(entrance => entrance.IsDefault), Is.EqualTo(1), path);
                Assert.That(entrances.Select(entrance => entrance.EntranceId).Distinct().Count(),
                    Is.EqualTo(entrances.Length), path);
            }
        }
        finally
        {
            if (previousSetup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }
    }

    private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
}
