using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class W1ProjectBuilder
{
    [MenuItem("Tools/W1/Rebuild Project Assets")]
    public static void BuildAll()
    {
        Directory.CreateDirectory("Assets/Settings/Player"); Directory.CreateDirectory("Assets/Scenes/Levels/Fire"); Directory.CreateDirectory("Assets/Scenes/Levels/Center");
        PlayerMovementSettings settings = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>("Assets/Settings/Player/DefaultPlayerMovement.asset");
        if (settings == null) { settings = ScriptableObject.CreateInstance<PlayerMovementSettings>(); AssetDatabase.CreateAsset(settings, "Assets/Settings/Player/DefaultPlayerMovement.asset"); }
        EditorUtility.SetDirty(settings);
        BuildInputActions(); BuildFire001(); RegisterCenterScenes(); PlayerPrefabBuilder.BuildFromCommandLine(); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
    }

    private static void BuildInputActions()
    {
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
        asset.Disable();
        foreach (InputActionMap oldMap in asset.actionMaps.ToArray()) asset.RemoveActionMap(oldMap);
        InputActionMap map = asset.AddActionMap("Player");
        InputAction move = map.AddAction("Move", InputActionType.Value); move.expectedControlType = "Axis";
        move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
        move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
        map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
        map.AddAction("PlaceMirror", InputActionType.Button, "<Mouse>/leftButton");
        map.AddAction("RecallMirror", InputActionType.Button, "<Mouse>/rightButton");
        map.AddAction("ResetRoom", InputActionType.Button, "<Keyboard>/r");
        map.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
        map.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
        EditorUtility.SetDirty(asset);
    }

    private static void BuildFire001()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject cameraGo = new("Main Camera"); Camera camera = cameraGo.AddComponent<Camera>(); cameraGo.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 4f; cameraGo.transform.position = new Vector3(0,0,-10); camera.backgroundColor = new Color(.07f,.045f,.04f);
        GameObject root = new("FIRE_001 First Heat Trench");
        GameObject entrance = new("Entrance"); entrance.transform.SetParent(root.transform); entrance.transform.position = new Vector3(-7f,-2.1f);
        GameObject leftGround = Box("Ground-Left", new Vector2(-5f,-3.5f), new Vector2(8f,1f), new Color(.18f,.16f,.15f), true, root.transform);
        GameObject rightGround = Box("Ground-Right", new Vector2(5f,-3.5f), new Vector2(8f,1f), new Color(.18f,.16f,.15f), true, root.transform);
        leftGround.AddComponent<MirrorSurface2D>().kind = MirrorSurface2D.SurfaceKind.Ground; rightGround.AddComponent<MirrorSurface2D>().kind = MirrorSurface2D.SurfaceKind.Ground;
        GameObject lava = Box("Lava-A", new Vector2(0f,-3.3f), new Vector2(2f,.6f), new Color(1f,.2f,.03f), true, root.transform); lava.GetComponent<BoxCollider2D>().isTrigger = true; lava.AddComponent<Hazard2D>();
        GameObject exit = Box("Exit", new Vector2(7.7f,-2f), new Vector2(.5f,2f), new Color(.3f,1f,.45f), true, root.transform); exit.GetComponent<BoxCollider2D>().isTrigger = true; exit.AddComponent<RoomExit2D>();
        CameraFollow2D follow = cameraGo.AddComponent<CameraFollow2D>(); follow.Configure(null);
        RoomResetSystem reset = root.AddComponent<RoomResetSystem>();
        PlayerRoomAuthoring.ConfigureRoom(root, entrance.transform, reset, follow);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, "Assets/Scenes/Levels/Fire/Fire_001.unity");
        AddBuildScene("Assets/Scenes/Levels/Fire/Fire_001.unity");
    }

    private static void RegisterCenterScenes()
    {
        AddBuildScene("Assets/Scenes/Levels/Center/Center_001.unity");
        AddBuildScene("Assets/Scenes/Levels/Center/Center_002.unity");
    }

    private static void AddBuildScene(string path)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(scene => scene.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static GameObject Box(string name, Vector2 position, Vector2 size, Color color, bool collider, Transform parent)
    {
        GameObject go = new(name); go.transform.SetParent(parent); go.transform.position = position;
        GameObject visual = new("Visual"); visual.transform.SetParent(go.transform, false);
        SpriteRenderer r = visual.AddComponent<SpriteRenderer>(); r.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); r.color = color; r.drawMode = SpriteDrawMode.Simple;
        Vector2 nativeSize = r.sprite.bounds.size; visual.transform.localScale = new Vector3(size.x / nativeSize.x, size.y / nativeSize.y, 1f);
        if (collider) { BoxCollider2D c = go.AddComponent<BoxCollider2D>(); c.size = size; }
        return go;
    }
}
