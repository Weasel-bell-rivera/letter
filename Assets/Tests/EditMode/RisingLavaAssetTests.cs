using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class RisingLavaAssetTests
{
    private const string PrefabPath = "Assets/Prefabs/Gameplay/Hazards/RisingLava2D.prefab";
    private const string ArtPath = "Assets/Art/Generated/Fire/lava_rising_handpainted.png";

    [Test]
    public void PrefabUsesTriggerHazardAndApprovedDefaultCycle()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            RisingLava2D lava = root.GetComponent<RisingLava2D>();
            BoxCollider2D trigger = root.GetComponentInChildren<BoxCollider2D>(true);
            Hazard2D hazard = root.GetComponentInChildren<Hazard2D>(true);
            SpriteRenderer visual = root.GetComponentInChildren<SpriteRenderer>(true);
            Sprite art = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
            SerializedObject serialized = new(lava);

            Assert.That(lava, Is.Not.Null);
            Assert.That(trigger, Is.Not.Null.And.Property("isTrigger").True);
            Assert.That(hazard, Is.Not.Null);
            Assert.That(visual.sprite, Is.SameAs(art));
            Assert.That(serialized.FindProperty("warningDuration").floatValue, Is.EqualTo(1f));
            Assert.That(serialized.FindProperty("risingDuration").floatValue, Is.EqualTo(2f));
            Assert.That(serialized.FindProperty("topHoldDuration").floatValue, Is.EqualTo(1.5f));
            Assert.That(serialized.FindProperty("fallingDuration").floatValue, Is.EqualTo(2f));
            Assert.That(serialized.FindProperty("bottomHoldDuration").floatValue, Is.EqualTo(2.5f));
            Assert.That(serialized.FindProperty("initialPhase").enumValueIndex,
                Is.EqualTo((int)RisingLava2D.Phase.Warning));
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
