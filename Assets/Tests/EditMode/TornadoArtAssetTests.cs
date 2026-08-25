using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class TornadoArtAssetTests
{
    private const string ArtPath = "Assets/Art/Generated/Wind/small_tornado_3frame_handpainted.png";
    private const string PrefabPath = "Assets/Prefabs/Gameplay/Wind/MovingTornado2D.prefab";

    [Test]
    public void MovingTornadoUsesThreeGeneratedAnimationFrames()
    {
        Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(ArtPath)
            .OfType<Sprite>().ToArray();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        SpriteFrameAnimator2D animator = prefab.GetComponentInChildren<SpriteFrameAnimator2D>(true);

        Assert.That(frames, Has.Length.EqualTo(3));
        Assert.That(animator, Is.Not.Null);
        Assert.That(animator.FrameCount, Is.EqualTo(3));
        Assert.That(animator.FramesPerSecond, Is.EqualTo(8f));
        Assert.That(prefab.GetComponent<BoxCollider2D>().size, Is.EqualTo(new Vector2(.8f, .8f)));
    }
}
