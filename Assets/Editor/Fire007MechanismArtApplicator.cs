using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Fire007MechanismArtApplicator
{
    private const string ScenePath = "Assets/Scenes/Levels/Fire/Fire_007.unity";
    private const string PlatePath = "Assets/Art/Generated/Fire_007/fire007_pressure_plate_v1.png";
    private const string DoorPath = "Assets/Art/Generated/Fire_007/fire007_latch_door_v1.png";

    [MenuItem("Tools/W1/Apply FIRE-007 Mechanism Art")]
    public static void Apply()
    {
        ConfigureSprite(PlatePath);
        ConfigureSprite(DoorPath);
        Sprite plateSprite = RequireAsset<Sprite>(PlatePath);
        Sprite doorSprite = RequireAsset<Sprite>(DoorPath);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        GameObject room = RequireRoot(scene, "FIRE_007 Double Latch");
        ApplyPlate(room.transform, "Plate-A", plateSprite);
        ApplyPlate(room.transform, "Plate-B", plateSprite);
        ApplyDoor(room.transform, doorSprite);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Failed to save FIRE_007 mechanism art.");
        Require(EditorSceneManager.CloseScene(scene, true), "Failed to close FIRE_007 after saving mechanism art.");
        AssetDatabase.SaveAssets();
        Debug.Log("FIRE_007 pressure plates and latch door now use fire-region illustrated sprites.");
    }

    private static void ConfigureSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Require(importer != null, $"Missing texture importer: {path}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 724f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static void ApplyPlate(Transform room, string name, Sprite sprite)
    {
        Transform plate = FindRecursive(room, name);
        Require(plate != null, $"Missing {name}.");
        Transform visual = plate.Find("Visual");
        SpriteRenderer renderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        Require(renderer != null, $"Missing {name}/Visual SpriteRenderer.");
        renderer.sprite = sprite;
        renderer.color = Color.white;
        visual.localPosition = Vector3.zero;
        visual.localScale = Fit(sprite, new Vector2(1.2f, .24f));

        PressurePlate2D behaviour = plate.GetComponent<PressurePlate2D>();
        SerializedObject serialized = new(behaviour);
        serialized.FindProperty("idleColor").colorValue = Color.white;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplyDoor(Transform room, Sprite sprite)
    {
        Transform door = FindRecursive(room, "Door-A");
        Require(door != null, "Missing Door-A.");
        Transform visual = door.Find("Visual");
        SpriteRenderer renderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        Require(renderer != null, "Missing Door-A/Visual SpriteRenderer.");
        renderer.sprite = sprite;
        renderer.color = Color.white;
        visual.localPosition = Vector3.zero;
        visual.localScale = Fit(sprite, new Vector2(1f, 2f));

        door.position = new Vector3(door.position.x, -2f, door.position.z);
        BoxCollider2D collider = door.GetComponent<BoxCollider2D>();
        Require(collider != null, "Missing Door-A BoxCollider2D.");
        collider.offset = Vector2.zero;
        collider.size = new Vector2(1f, 2f);

        Door2D behaviour = door.GetComponent<Door2D>();
        SerializedObject serialized = new(behaviour);
        serialized.FindProperty("closedColor").colorValue = Color.white;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform FindRecursive(Transform root, string name)
    {
        if (root.name == name)
            return root;
        foreach (Transform child in root)
        {
            Transform found = FindRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static Vector3 Fit(Sprite sprite, Vector2 worldSize)
    {
        Vector2 native = sprite.bounds.size;
        Require(native.x > 0f && native.y > 0f, $"Sprite {sprite.name} has invalid bounds.");
        return new Vector3(worldSize.x / native.x, worldSize.y / native.y, 1f);
    }

    private static GameObject RequireRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name)
                return root;
        throw new InvalidOperationException($"Missing room root {name}.");
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Require(asset != null, $"Missing required asset: {path}");
        return asset;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
