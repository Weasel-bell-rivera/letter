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
    private const string MirrorVisualPrefabPath = "Assets/Prefabs/Gameplay/Mirrors/PlacedMirror.prefab";

    [Test]
    public void PlacedMirrorVisualUsesApprovedSpriteAndHasNoPhysicalCollider()
    {
        GameObject mirrorVisual = AssetDatabase.LoadAssetAtPath<GameObject>(MirrorVisualPrefabPath);
        Assert.That(mirrorVisual, Is.Not.Null);
        SpriteRenderer renderer = mirrorVisual.GetComponentInChildren<SpriteRenderer>(true);
        Assert.That(renderer, Is.Not.Null);
        Assert.That(renderer.sprite?.name, Is.EqualTo("coin_gold_side"));
        Assert.That(renderer.sortingOrder, Is.EqualTo(20));
        Assert.That(renderer.transform.localScale, Is.EqualTo(new Vector3(.96f, .96f, 1f)));
        Assert.That(mirrorVisual.GetComponentsInChildren<Collider2D>(true), Is.Empty);
    }

    [Test]
    public void CanonicalPrefabContainsMovementInputMirrorAndAllApprovedSprites()
    {
        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        Assert.That(player, Is.Not.Null);
        Assert.That(player.GetComponent<Rigidbody2D>(), Is.Not.Null);
        BoxCollider2D bodyCollider = player.GetComponent<BoxCollider2D>();
        Assert.That(bodyCollider, Is.Not.Null);
        Assert.That(bodyCollider.size, Is.EqualTo(new Vector2(.42f, 1.42f)));
        Assert.That(bodyCollider.edgeRadius, Is.EqualTo(.04f));
        Assert.That(bodyCollider.offset, Is.EqualTo(Vector2.zero));
        Vector2 outerSize = bodyCollider.size + Vector2.one * (2f * bodyCollider.edgeRadius);
        Assert.That(outerSize.x, Is.EqualTo(.5f).Within(.00001f));
        Assert.That(outerSize.y, Is.EqualTo(1.5f).Within(.00001f));
        Assert.That(player.GetComponent<PlayerController2D>(), Is.Not.Null);
        Assert.That(player.GetComponent<CharacterLadderMotor2D>(), Is.Not.Null);
        Assert.That(player.GetComponent<PlayerInput>()?.defaultActionMap, Is.EqualTo("Player"));
        MirrorPlayer2D mirror = player.GetComponent<MirrorPlayer2D>();
        Assert.That(mirror, Is.Not.Null);
        Assert.That(mirror.MirrorVisualPrefab,
            Is.SameAs(AssetDatabase.LoadAssetAtPath<GameObject>(MirrorVisualPrefabPath)));
        SpriteRenderer mirrorRenderer = mirror.MirrorVisualPrefab.GetComponentInChildren<SpriteRenderer>();
        Assert.That(mirrorRenderer?.sprite?.name, Is.EqualTo("coin_gold_side"));

        PlayerVisual2D visual = player.GetComponentInChildren<PlayerVisual2D>(true);
        Assert.That(visual, Is.Not.Null);
        Assert.That(new[]
        {
            visual.IdleSprite, visual.JumpSprite, visual.WalkSpriteA, visual.WalkSpriteB,
            visual.DuckSprite, visual.FrontSprite, visual.HitSprite
        }.All(sprite => sprite != null), Is.True);
        Assert.That(visual.IdleFrameCount, Is.EqualTo(2));
        Assert.That(visual.WalkFrameCount, Is.EqualTo(8));
        Assert.That(visual.JumpFrameCount, Is.EqualTo(11));
        Assert.That(visual.ClimbFrameCount, Is.EqualTo(8));
        Assert.That(visual.PushFrameCount, Is.EqualTo(4));
        SerializedProperty pushFrames = new SerializedObject(visual).FindProperty("pushFrames");
        for (int index = 0; index < pushFrames.arraySize; index++)
        {
            Sprite frame = pushFrames.GetArrayElementAtIndex(index).objectReferenceValue as Sprite;
            Assert.That(AssetDatabase.GetAssetPath(frame), Is.EqualTo(
                $"Assets/Art/Characters/Player/SilhouetteV1/player_push_{index:00}.png"));
        }
        SerializedProperty climbFrames = new SerializedObject(visual).FindProperty("climbFrames");
        for (int index = 0; index < climbFrames.arraySize; index++)
        {
            Sprite frame = climbFrames.GetArrayElementAtIndex(index).objectReferenceValue as Sprite;
            Assert.That(AssetDatabase.GetAssetPath(frame), Is.EqualTo(
                $"Assets/Art/Characters/Player/SilhouetteV1/player_climb_{index:00}.png"),
                "The canonical ladder animation must not fall back to walking sprites.");
        }
        Assert.That(visual.JumpFrameVerticalOffsetCount, Is.EqualTo(visual.JumpFrameCount),
            "Every jump frame must keep a stable visual foot anchor.");
        Assert.That(visual.HitFrameCount, Is.EqualTo(4));

        PlayerPrefabRegistry registry = AssetDatabase.LoadAssetAtPath<PlayerPrefabRegistry>(
            RegistryPath);
        Assert.That(registry, Is.Not.Null);
        Assert.That(registry.PlayerPrefab, Is.SameAs(player));
        Assert.That(registry.IsValid(out string error), Is.True, error);
    }

    [Test]
    public void RoundedColliderFitsInsetEnvelopeAndRemovesSharpCorner()
    {
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath), preview);
            player.transform.position = Vector3.zero;
            PlayerVisual2D visual = player.GetComponentInChildren<PlayerVisual2D>();
            Vector3 authoredScale = visual.transform.localScale;
            player.GetComponent<PlayerController2D>().SendMessage("Awake");
            visual.SendMessage("Awake");
            Assert.That(visual.transform.localScale, Is.EqualTo(authoredScale),
                "Shrinking collision must not shrink the authored silhouette.");
            Assert.That(visual.transform.localPosition.y, Is.EqualTo(.12f).Within(.0001f));
            Physics2D.SyncTransforms();
            BoxCollider2D collider = player.GetComponent<BoxCollider2D>();
            Assert.That(collider.bounds.size.x, Is.EqualTo(.5f).Within(.001f));
            Assert.That(collider.bounds.size.y, Is.EqualTo(1.5f).Within(.001f));
            Assert.That(collider.bounds.min.y, Is.EqualTo(-.75f).Within(.001f));
            Assert.That(collider.OverlapPoint(new Vector2(0f, -.749f)), Is.True);
            Assert.That(collider.OverlapPoint(new Vector2(.249f, 0f)), Is.True);
            Assert.That(collider.OverlapPoint(new Vector2(.249f, -.749f)), Is.False,
                "The old sharp corner must be outside the rounded collision shape.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    [Test]
    public void PushPresentationQueryRejectsReleaseRetreatSupportAndStaleCratePosition()
    {
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath), preview);
            player.transform.position = new Vector3(0f, .9f, 0f);
            player.GetComponent<PlayerController2D>().SendMessage("Awake");
            PlayerVisual2D visual = player.GetComponentInChildren<PlayerVisual2D>();
            visual.SendMessage("Awake");
            GameObject crate = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Gameplay/Props/PushableCrate2D.prefab"), preview);
            crate.transform.position = new Vector3(.87f, .6f, 0f);
            Physics2D.SyncTransforms();
            var query = typeof(PlayerVisual2D).GetMethod("IsPressingCrate",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            bool Pressing(float input) => (bool)query.Invoke(visual, new object[] { input });

            Assert.That(Pressing(1f), Is.True, "Stationary side contact still shows exertion.");
            Assert.That(Pressing(0f), Is.False, "Release exits the pushing pose.");
            Assert.That(Pressing(-1f), Is.False, "Moving away must not look like pulling.");
            crate.GetComponent<PushableCrate2D>().enabled = false;
            Assert.That(Pressing(1f), Is.False);
            crate.GetComponent<PushableCrate2D>().enabled = true;
            crate.transform.position = new Vector3(8f, .6f, 0f);
            Physics2D.SyncTransforms();
            Assert.That(Pressing(1f), Is.False, "A reset/teleport cannot retain old contact.");
            crate.transform.position = new Vector3(-.87f, .6f, 0f);
            Physics2D.SyncTransforms();
            Assert.That(Pressing(-1f), Is.True, "Left contact uses the same rule.");
            crate.transform.position = new Vector3(0f, -.61f, 0f);
            Physics2D.SyncTransforms();
            Assert.That(Pressing(1f), Is.False, "Standing above a crate is not pushing its side.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
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
