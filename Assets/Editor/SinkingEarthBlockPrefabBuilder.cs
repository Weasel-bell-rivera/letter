using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SinkingEarthBlockPrefabBuilder
{
    public const string PrefabPath = "Assets/Prefabs/Gameplay/Earth/SinkingEarthBlock2D.prefab";

    [MenuItem("Tools/W1/Build Sinking Earth Block Prefab")]
    public static void Build()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        GameObject root = new("SinkingEarthBlock2D");
        try
        {
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(2f, 1f);

            SurfaceSemantic2D semantic = root.AddComponent<SurfaceSemantic2D>();
            semantic.Configure(SurfaceSemantic2D.SurfaceType.DynamicSurface, false, true);
            root.AddComponent<SinkingEarthBlock2D>();

            GameObject visualObject = new("Visual");
            visualObject.transform.SetParent(root.transform, false);
            SpriteRenderer visual = visualObject.AddComponent<SpriteRenderer>();
            visual.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            visual.color = new Color(.43f, .25f, .12f, 1f);
            visual.drawMode = SpriteDrawMode.Sliced;
            visual.size = new Vector2(2f, 1f);
            visual.sortingOrder = 1;

            GameObject topMarkerObject = new("TopMarker");
            topMarkerObject.transform.SetParent(root.transform, false);
            topMarkerObject.transform.localPosition = new Vector3(0f, .42f, 0f);
            SpriteRenderer topMarker = topMarkerObject.AddComponent<SpriteRenderer>();
            topMarker.sprite = visual.sprite;
            topMarker.color = new Color(.72f, .53f, .27f, 1f);
            topMarker.drawMode = SpriteDrawMode.Sliced;
            topMarker.size = new Vector2(1.8f, .12f);
            topMarker.sortingOrder = 2;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (saved == null) throw new InvalidOperationException("Failed to save SinkingEarthBlock2D Prefab.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Built shared sinking earth block Prefab at {PrefabPath}.");
    }
}
