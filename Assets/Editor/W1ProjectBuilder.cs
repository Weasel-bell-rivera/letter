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
        BuildInputActions(); BuildFire001(); BuildCenterScenes(); PlayerPrefabBuilder.BuildFromCommandLine(); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
    }

    [MenuItem("Tools/W1/Build Center Placeholder Rooms")]
    public static void BuildCenterRooms()
    {
        Directory.CreateDirectory("Assets/Scenes/Levels/Center");
        PlayerMovementSettings settings = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>("Assets/Settings/Player/DefaultPlayerMovement.asset");
        if (settings == null) throw new FileNotFoundException("Default player movement settings are required.");
        BuildCenterScenes(); PlayerPrefabBuilder.BuildFromCommandLine(); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
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


    private static void BuildCenterScenes()
    {
        BuildCenter001();
        BuildCenter002Placeholder();
        AddBuildScene("Assets/Scenes/Levels/Center/Center_001.unity");
        AddBuildScene("Assets/Scenes/Levels/Center/Center_002.unity");
    }

    private static void BuildCenter001()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = CreateCamera(new Color(.035f, .055f, .075f), 5f);
        GameObject root = new("CENTER-001 Mirror Beginning");
        GameObject entrance = Marker("Entrance", new Vector2(-13f, -2.1f), root.transform);

        Ground("Continuous Ground", new Vector2(0f, -3.5f), new Vector2(28f, 1f), root.transform);

        CameraFollow2D cameraFollow = camera.gameObject.AddComponent<CameraFollow2D>();
        cameraFollow.Configure(null);

        GameObject pickup = Box("Mirror Ability Pickup", new Vector2(0f, -1.8f), new Vector2(.55f, 1.5f), new Color(.25f, .95f, 1f), true, root.transform);
        pickup.GetComponent<BoxCollider2D>().size = new Vector2(.8f, 9f);
        pickup.GetComponent<BoxCollider2D>().isTrigger = true;
        pickup.AddComponent<MirrorAbilityPickup2D>();

        GameObject guideRoot = new("Mirror Practice Guide"); guideRoot.transform.SetParent(root.transform);
        GameObject place = MouseIndicator("Place Indicator - Left", new Vector2(5f, .8f), new Color(.2f, .85f, 1f), true, guideRoot.transform);
        GameObject recall = MouseIndicator("Recall Indicator - Right", new Vector2(5f, .8f), new Color(1f, .65f, .2f), false, guideRoot.transform);
        GameObject complete = Box("Practice Complete Indicator", new Vector2(5f, .8f), new Vector2(1.1f, .22f), new Color(.3f, 1f, .45f), false, guideRoot.transform);
        complete.SetActive(false);
        MirrorTutorialGuide2D guide = guideRoot.AddComponent<MirrorTutorialGuide2D>(); guide.Configure(null, place, recall, complete);

        GameObject exit = Box("Exit to CENTER-002", new Vector2(13.2f, -2f), new Vector2(.65f, 2f), new Color(.3f, 1f, .45f), true, root.transform);
        exit.GetComponent<BoxCollider2D>().isTrigger = true; exit.AddComponent<RoomExit2D>().Configure("Center_002");
        RoomResetSystem reset = root.AddComponent<RoomResetSystem>();
        PlayerRoomAuthoring.ConfigureRoom(root, entrance.transform, reset, cameraFollow);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Levels/Center/Center_001.unity");
    }

    private static void BuildCenter002Placeholder()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = CreateCamera(new Color(.03f, .045f, .065f), 5f);
        GameObject root = new("CENTER-002 Placeholder - No Gameplay");
        GameObject entrance = Marker("Entrance from CENTER-001", new Vector2(-4f, -2.1f), root.transform);
        Ground("Placeholder Ground", new Vector2(0f, -3.5f), new Vector2(12f, 1f), root.transform);
        CameraFollow2D cameraFollow = camera.gameObject.AddComponent<CameraFollow2D>(); cameraFollow.Configure(null);
        RoomResetSystem reset = root.AddComponent<RoomResetSystem>();
        PlayerRoomAuthoring.ConfigureRoom(root, entrance.transform, reset, cameraFollow);
        Box("End of Approved Content", new Vector2(4.8f, -1.5f), new Vector2(.35f, 3f), new Color(.25f, .3f, .4f), true, root.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Levels/Center/Center_002.unity");
    }

    private static Camera CreateCamera(Color background, float size)
    {
        GameObject cameraGo = new("Main Camera"); Camera camera = cameraGo.AddComponent<Camera>(); cameraGo.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = size; cameraGo.transform.position = new Vector3(0f, 0f, -10f); camera.backgroundColor = background;
        return camera;
    }

    private static GameObject Ground(string name, Vector2 position, Vector2 size, Transform parent)
    {
        GameObject ground = Box(name, position, size, new Color(.16f, .2f, .24f), true, parent); ground.AddComponent<MirrorSurface2D>().kind = MirrorSurface2D.SurfaceKind.Ground; return ground;
    }

    private static GameObject Marker(string name, Vector2 position, Transform parent)
    { GameObject marker = new(name); marker.transform.SetParent(parent); marker.transform.position = position; return marker; }

    private static GameObject MouseIndicator(string name, Vector2 position, Color color, bool left, Transform parent)
    {
        GameObject root = Box(name, position, new Vector2(1f, 1.25f), new Color(color.r, color.g, color.b, .35f), false, parent);
        GameObject button = Box(left ? "Left Button" : "Right Button", position + new Vector2(left ? -.25f : .25f, .35f), new Vector2(.42f, .42f), color, false, root.transform);
        button.transform.position = position + new Vector2(left ? -.25f : .25f, .35f);
        return root;
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
