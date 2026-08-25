using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class WindEnvironmentAssetTests
{
    private const string WindColumnPath = "Assets/Prefabs/Gameplay/Wind/WindColumn2D.prefab";
    private const string MovingTornadoPath = "Assets/Prefabs/Gameplay/Wind/MovingTornado2D.prefab";
    private const string TornadoGeneratorPath = "Assets/Prefabs/Gameplay/Wind/TornadoGenerator2D.prefab";
    private const string WindDeflectorPath = "Assets/Prefabs/Gameplay/Wind/WindDeflector2D.prefab";
    private const string WindTurbinePath =
        "Assets/Prefabs/Gameplay/Switches/WindTurbineSwitch2D.prefab";

    [Test]
    public void WindColumnPrefabHasApprovedDefaults()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindColumnPath);
        Assert.That(prefab, Is.Not.Null);
        WindColumn2D wind = prefab.GetComponent<WindColumn2D>();
        Assert.That(wind, Is.Not.Null);
        Assert.That(wind.Mode, Is.EqualTo(WindColumn2D.WindMode.Constant));
        Assert.That(wind.Direction, Is.EqualTo(Vector2.right));
        Assert.That(wind.Speed, Is.EqualTo(WindColumn2D.DefaultSpeed));
        Assert.That(prefab.GetComponent<BoxCollider2D>().isTrigger, Is.True);
    }

    [Test]
    public void UpwardWindIsNotBlockedByPlatformAtVolumeCenter()
    {
        GameObject windObject = new("Vertical Wind Test");
        GameObject receiverObject = new("Receiver");
        GameObject platformObject = new("Center Platform");
        try
        {
            BoxCollider2D volume = windObject.AddComponent<BoxCollider2D>();
            WindColumn2D wind = windObject.AddComponent<WindColumn2D>();
            wind.Configure(WindColumn2D.WindMode.Constant, Vector2.up, 4f, new Vector2(4f, 5f));

            receiverObject.transform.position = new Vector2(0f, -1.5f);
            Rigidbody2D receiverBody = receiverObject.AddComponent<Rigidbody2D>();
            receiverBody.bodyType = RigidbodyType2D.Kinematic;
            BoxCollider2D receiver = receiverObject.AddComponent<BoxCollider2D>();

            platformObject.transform.position = new Vector2(0f, .35f);
            BoxCollider2D platform = platformObject.AddComponent<BoxCollider2D>();
            platform.size = new Vector2(4f, .7f);
            Physics2D.SyncTransforms();

            Assert.That(volume.bounds.Contains(receiver.bounds.center), Is.True);
            Assert.That(platform.bounds.Contains(volume.bounds.center), Is.True,
                "Regression setup must place the old center origin inside the platform.");
            Assert.That(wind.CanReach(receiver), Is.True,
                "A platform downstream of the receiver must not suppress upward wind below it.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(platformObject);
            UnityEngine.Object.DestroyImmediate(receiverObject);
            UnityEngine.Object.DestroyImmediate(windObject);
        }
    }

    [Test]
    public void TornadoAndGeneratorPrefabsHaveExplicitReferences()
    {
        GameObject tornadoObject = AssetDatabase.LoadAssetAtPath<GameObject>(
            MovingTornadoPath);
        GameObject generatorObject = AssetDatabase.LoadAssetAtPath<GameObject>(
            TornadoGeneratorPath);
        Assert.That(tornadoObject, Is.Not.Null);
        Assert.That(generatorObject, Is.Not.Null);
        MovingTornado2D tornado = tornadoObject.GetComponent<MovingTornado2D>();
        TornadoGenerator2D generator = generatorObject.GetComponent<TornadoGenerator2D>();
        Assert.That(tornado, Is.Not.Null);
        Assert.That(tornado.Speed, Is.EqualTo(MovingTornado2D.DefaultSpeed));
        Assert.That(tornado.MaximumDistance, Is.EqualTo(MovingTornado2D.DefaultMaximumDistance));
        Assert.That(generator, Is.Not.Null);
        Assert.That(generator.TornadoPrefab, Is.EqualTo(tornado));
        Assert.That(generator.SpawnInterval, Is.EqualTo(TornadoGenerator2D.DefaultSpawnInterval));
        Assert.That(generator.MaximumAlive, Is.EqualTo(TornadoGenerator2D.DefaultMaximumAlive));
    }

    [Test]
    public void WindDeflectorPrefabUsesDeterministicNinetyDegreeOutput()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindDeflectorPath);
        Assert.That(prefab, Is.Not.Null);
        WindDeflector2D deflector = prefab.GetComponent<WindDeflector2D>();
        Assert.That(deflector, Is.Not.Null);
        Assert.That(deflector.IncomingDirection, Is.EqualTo(Vector2.right));
        Assert.That(deflector.IsClockwise, Is.False);
        Assert.That(deflector.OutputDirection, Is.EqualTo(Vector2.up));
        Assert.That(prefab.GetComponent<BoxCollider2D>().isTrigger, Is.False);
        Assert.That(prefab.transform.Find("OutputVolume").GetComponent<BoxCollider2D>().isTrigger, Is.True);
    }

    [Test]
    public void WindTurbinePrefabUsesExplicitContinuousWindReceiver()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindTurbinePath);
        Assert.That(prefab, Is.Not.Null);
        WindTurbineSwitch2D turbine = prefab.GetComponent<WindTurbineSwitch2D>();
        Assert.That(turbine, Is.Not.Null);
        Assert.That(turbine.AcceptedDirection, Is.EqualTo(Vector2.right));
        Assert.That(turbine.IsActive, Is.False);
        Assert.That(turbine.ControlledDoor, Is.Null);
        Assert.That(prefab.GetComponent<BoxCollider2D>().isTrigger, Is.True);
        Assert.That(prefab.transform.Find("RotorVisual"), Is.Not.Null);
    }
}
