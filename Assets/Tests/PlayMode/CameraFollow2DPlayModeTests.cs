using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class CameraFollow2DPlayModeTests
{
    [UnityTest]
    public IEnumerator LeftAndBottomEntryAxesAcquireIndependently()
    {
        CameraFollow2D follow = CreateCameraAndTarget(new Vector2(-4f, -4f), out Transform target);
        Vector3 entryCameraPosition = follow.transform.position;

        yield return null;
        Assert.That(follow.transform.position, Is.EqualTo(entryCameraPosition));

        target.position = new Vector3(1f, -4f, 0f);
        yield return null;
        Assert.That(follow.transform.position.x, Is.EqualTo(1f).Within(.001f));
        Assert.That(follow.transform.position.y, Is.EqualTo(0f).Within(.001f),
            "Vertical entry framing must remain held until its own composition line is crossed.");

        target.position = new Vector3(2f, 0f, 0f);
        yield return null;
        Assert.That(follow.transform.position.x, Is.EqualTo(2f).Within(.001f));
        Assert.That(follow.transform.position.y, Is.EqualTo(.56f).Within(.001f));

        Object.Destroy(follow.gameObject);
        Object.Destroy(target.gameObject);
    }

    [UnityTest]
    public IEnumerator RightAndTopEntriesUseMirroredAcquisitionRules()
    {
        CameraFollow2D follow = CreateCameraAndTarget(new Vector2(4f, 4f), out Transform target);
        Vector3 entryCameraPosition = follow.transform.position;

        yield return null;
        Assert.That(follow.transform.position, Is.EqualTo(entryCameraPosition));

        target.position = new Vector3(-1f, 4f, 0f);
        yield return null;
        Assert.That(follow.transform.position.x, Is.EqualTo(-1f).Within(.001f));
        Assert.That(follow.transform.position.y, Is.EqualTo(0f).Within(.001f));

        target.position = new Vector3(-2f, -1f, 0f);
        yield return null;
        Assert.That(follow.transform.position.x, Is.EqualTo(-2f).Within(.001f));
        Assert.That(follow.transform.position.y, Is.EqualTo(-.44f).Within(.001f));

        Object.Destroy(follow.gameObject);
        Object.Destroy(target.gameObject);
    }

    private static CameraFollow2D CreateCameraAndTarget(Vector2 targetPosition, out Transform target)
    {
        GameObject cameraObject = new("Camera");
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
        follow.ConfigureDamping(0f);

        GameObject targetObject = new("Player");
        targetObject.transform.position = targetPosition;
        target = targetObject.transform;
        follow.Configure(target, true);
        follow.BeginEntryFraming();
        return follow;
    }
}
