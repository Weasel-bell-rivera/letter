using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// First-pass, project-native room authoring surface. The resulting rooms remain ordinary
/// Unity Scenes made from Tilemaps and connected Prefab instances.
/// </summary>
public sealed class W1LevelEditorWindow : EditorWindow
{
    private const string LevelRoot = "Assets/Scenes/Levels";
    private const string DefaultTerrainTile = "Assets/Tiles/Graybox/Fire009Terrain.asset";
    private const string MovementSettingsPath = "Assets/Settings/Player/DefaultPlayerMovement.asset";

    private static readonly PaletteItem[] Palette =
    {
        new("Pressure Plate", "Assets/Prefabs/Gameplay/Switches/PressurePlate2D.prefab", "DynamicObjects"),
        new("Door", "Assets/Prefabs/Gameplay/Doors/Door2D.prefab", "DynamicObjects"),
        new("Moving Platform", "Assets/Prefabs/Gameplay/Platforms/MovingPlatform2D.prefab", "DynamicObjects"),
        new("Checkpoint", "Assets/Prefabs/Gameplay/Checkpoints/Checkpoint2D.prefab", "DynamicObjects"),
        new("Room Exit", "Assets/Prefabs/Gameplay/Exits/RoomExit2D.prefab", "Exits"),
    };

    private static readonly string[] StandardTilemapNames =
    {
        "Background", "Terrain", "FrozenGround", "FreezingGround", "OneWayPlatform",
        "SpecialMirrorWall", "Hazard", "Decoration", "Foreground"
    };

    [SerializeField] private string region = "Center";
    [SerializeField] private string roomId = "Center_003";
    [SerializeField] private Vector2 entrancePosition = new(-8f, -2f);
    [SerializeField] private Vector2 cameraCenter = Vector2.zero;
    [SerializeField] private Vector2 cameraSize = new(26f, 14f);
    [SerializeField] private TileBase paintTile;
    [SerializeField] private bool showRelationships = true;
    [SerializeField] private bool showCameraBounds = true;
    [SerializeField] private bool showJumpEnvelope = true;
    [SerializeField] private bool showMirrorSurfaces = true;
    [SerializeField] private bool showColliderBounds;

    private Vector2 scroll;
    private readonly List<RoomValidationIssue> issues = new();

    [MenuItem("Tools/W1/Level Editor")]
    public static void Open() => GetWindow<W1LevelEditorWindow>("W1 Level Editor");

    private void OnEnable()
    {
        SceneView.duringSceneGui += DrawSceneGuides;
        if (paintTile == null)
            paintTile = AssetDatabase.LoadAssetAtPath<TileBase>(DefaultTerrainTile);
    }

    private void OnDisable() => SceneView.duringSceneGui -= DrawSceneGuides;

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawSceneSummary();
        EditorGUILayout.Space(8f);
        DrawRoomCreation();
        EditorGUILayout.Space(8f);
        DrawTerrainTools();
        EditorGUILayout.Space(8f);
        DrawPrefabPalette();
        EditorGUILayout.Space(8f);
        DrawRelationshipTools();
        EditorGUILayout.Space(8f);
        DrawValidation();
        EditorGUILayout.EndScrollView();
    }

    private void DrawSceneSummary()
    {
        Scene scene = SceneManager.GetActiveScene();
        EditorGUILayout.LabelField("Current Room", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Scene", scene.name);
            EditorGUILayout.TextField("Path", scene.path);
            EditorGUILayout.Toggle("Dirty", scene.isDirty);
        }
        EditorGUILayout.HelpBox(
            "Edits stay in standard Scene, Tilemap and Prefab data. Use Unity Undo for authoring operations.",
            MessageType.Info);
    }

    private void DrawRoomCreation()
    {
        EditorGUILayout.LabelField("1. Room Skeleton", EditorStyles.boldLabel);
        region = EditorGUILayout.TextField("Region Folder", region);
        roomId = EditorGUILayout.TextField("Room ID", roomId);
        entrancePosition = EditorGUILayout.Vector2Field("Default Entrance", entrancePosition);
        cameraCenter = EditorGUILayout.Vector2Field("Camera Center", cameraCenter);
        cameraSize = EditorGUILayout.Vector2Field("Room Bounds", cameraSize);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Create New Room Skeleton")) CreateRoomSkeleton();
        }
    }

    private void DrawTerrainTools()
    {
        EditorGUILayout.LabelField("2. Terrain", EditorStyles.boldLabel);
        paintTile = (TileBase)EditorGUILayout.ObjectField("Selected Paint Tile", paintTile, typeof(TileBase), false);
        EditorGUILayout.HelpBox(
            "Select a Tilemap in the Hierarchy, then use Unity's Tile Palette to paint. This field provides a quick one-cell stamp at the Scene cursor.",
            MessageType.None);
        using (new EditorGUI.DisabledScope(paintTile == null || Selection.activeGameObject == null ||
                                            Selection.activeGameObject.GetComponent<Tilemap>() == null))
        {
            if (GUILayout.Button("Stamp Tile At Scene View Pivot")) StampAtScenePivot();
        }
    }

    private void DrawPrefabPalette()
    {
        EditorGUILayout.LabelField("3. Gameplay Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Instances are placed at the Scene view pivot and remain connected to their source Prefab.", MessageType.None);
        foreach (PaletteItem item in Palette)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(item.Path);
            using (new EditorGUI.DisabledScope(prefab == null || !HasRoomRoot()))
            {
                if (GUILayout.Button($"Place {item.Label}")) PlacePrefab(item, prefab);
            }
        }
    }

    private void DrawRelationshipTools()
    {
        EditorGUILayout.LabelField("4. Relationships & Guides", EditorStyles.boldLabel);
        showRelationships = EditorGUILayout.Toggle("Plate → Door Lines", showRelationships);
        showCameraBounds = EditorGUILayout.Toggle("Camera Bounds", showCameraBounds);
        showJumpEnvelope = EditorGUILayout.Toggle("Selected Jump Envelope", showJumpEnvelope);
        showMirrorSurfaces = EditorGUILayout.Toggle("Mirror Surface Bounds", showMirrorSurfaces);
        showColliderBounds = EditorGUILayout.Toggle("Selected Colliders", showColliderBounds);
        if (GUILayout.Button("Connect Selected Plate To Selected Door")) ConnectSelectedPlateAndDoor();
        using (new EditorGUI.DisabledScope(EditorApplication.isCompiling))
        {
            if (!EditorApplication.isPlaying)
            {
                if (GUILayout.Button("Preview Current Room (Play Mode)") &&
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorApplication.isPlaying = true;
            }
            else if (GUILayout.Button("Stop Preview"))
            {
                EditorApplication.isPlaying = false;
            }
        }
    }

    private void DrawValidation()
    {
        EditorGUILayout.LabelField("5. Current Room Check", EditorStyles.boldLabel);
        if (GUILayout.Button("Validate Current Room")) RunValidation();

        if (issues.Count == 0)
        {
            EditorGUILayout.HelpBox("No report yet, or no statically detectable issues were found.", MessageType.Info);
            return;
        }

        foreach (RoomValidationIssue issue in issues)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.HelpBox($"{issue.Id} · {issue.Message}", issue.Type);
                if (issue.Context != null && GUILayout.Button("Select", GUILayout.Width(52f)))
                {
                    Selection.activeObject = issue.Context;
                    EditorGUIUtility.PingObject(issue.Context);
                }
            }
        }
    }

    private void CreateRoomSkeleton()
    {
        string normalizedRegion = SanitizeToken(region);
        string normalizedRoom = NormalizeRoomId(roomId);
        if (string.IsNullOrEmpty(normalizedRegion) || string.IsNullOrEmpty(normalizedRoom))
        {
            EditorUtility.DisplayDialog("Invalid room", "Region and Room ID are required.", "OK");
            return;
        }

        string folder = $"{LevelRoot}/{normalizedRegion}";
        string path = $"{folder}/{normalizedRoom}.unity";
        if (File.Exists(path))
        {
            EditorUtility.DisplayDialog("Room already exists", path, "OK");
            return;
        }
        if (!HasMatchingRoomDocument(path))
        {
            EditorUtility.DisplayDialog("Room document required",
                $"Create and approve the matching room document under docs/rooms/{normalizedRegion.ToLowerInvariant()} before creating {normalizedRoom}.",
                "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureAssetFolder(folder);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject room = new(normalizedRoom);
        Undo.RegisterCreatedObjectUndo(room, "Create W1 room skeleton");

        GameObject grid = CreateChild(room.transform, "Grid");
        grid.AddComponent<Grid>();
        foreach (string layerName in StandardTilemapNames)
        {
            GameObject layer = CreateChild(grid.transform, layerName);
            layer.AddComponent<Tilemap>();
            TilemapRenderer renderer = layer.AddComponent<TilemapRenderer>();
            if (layerName == "Decoration") renderer.enabled = false;
        }
        ConfigureTerrainLayer(grid.transform.Find("Terrain").gameObject);

        GameObject gameplay = CreateChild(room.transform, "Gameplay");
        CreateChild(gameplay.transform, "DynamicObjects");
        GameObject entrances = CreateChild(gameplay.transform, "Entrances");
        CreateChild(gameplay.transform, "Exits");

        GameObject entrance = CreateChild(entrances.transform, "Entrance-DEFAULT");
        entrance.transform.position = entrancePosition;
        PlayerRoomAuthoring.ConfigureDefaultEntrance(entrance.transform);

        GameObject systems = CreateChild(room.transform, "RoomSystems");
        RoomResetSystem reset = systems.AddComponent<RoomResetSystem>();
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance.transform, reset);

        GameObject cameraObject = new("Main Camera");
        Undo.RegisterCreatedObjectUndo(cameraObject, "Create room camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(cameraCenter.x, cameraCenter.y, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(1f, cameraSize.y * .5f);
        cameraObject.AddComponent<AudioListener>();
        CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
        follow.ConfigureBounds(Rect.MinMaxRect(cameraCenter.x - cameraSize.x * .5f,
            cameraCenter.y - cameraSize.y * .5f, cameraCenter.x + cameraSize.x * .5f,
            cameraCenter.y + cameraSize.y * .5f));
        PlayerRoomAuthoring.ConfigureRoom(systems, entrance.transform, reset, follow);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, path))
            EditorUtility.DisplayDialog("Save failed", $"Could not save {path}.", "OK");
        Selection.activeObject = room;
        RunValidation();
    }

    private static void ConfigureTerrainLayer(GameObject terrain)
    {
        Rigidbody2D body = terrain.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        terrain.AddComponent<CompositeCollider2D>();
        TilemapCollider2D collider = terrain.AddComponent<TilemapCollider2D>();
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        SurfaceSemantic2D semantic = terrain.AddComponent<SurfaceSemantic2D>();
        semantic.Configure(SurfaceSemantic2D.SurfaceType.StaticSolid, true, true);
        MirrorSurface2D mirrorSurface = terrain.AddComponent<MirrorSurface2D>();
        mirrorSurface.kind = MirrorSurface2D.SurfaceKind.Ground;
        mirrorSurface.safe = true;
    }

    private void StampAtScenePivot()
    {
        Tilemap map = Selection.activeGameObject.GetComponent<Tilemap>();
        Vector3 world = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        Vector3Int cell = map.WorldToCell(world);
        Undo.RegisterCompleteObjectUndo(map, "Stamp W1 room tile");
        map.SetTile(cell, paintTile);
        map.RefreshTile(cell);
        EditorUtility.SetDirty(map);
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        SceneView.RepaintAll();
    }

    private void PlacePrefab(PaletteItem item, GameObject prefab)
    {
        Transform parent = FindRoomChild(item.ParentName);
        if (parent == null) return;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
        Undo.RegisterCreatedObjectUndo(instance, $"Place {item.Label}");
        Undo.SetTransformParent(instance.transform, parent, $"Parent {item.Label}");
        Vector3 pivot = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        instance.transform.position = new Vector3(Mathf.Round(pivot.x * 2f) * .5f,
            Mathf.Round(pivot.y * 2f) * .5f, 0f);
        instance.name = ObjectNames.GetUniqueName(parent.Cast<Transform>().Select(child => child.name).ToArray(), item.Label);
        Selection.activeObject = instance;
        EditorSceneManager.MarkSceneDirty(instance.scene);
    }

    private static void ConnectSelectedPlateAndDoor()
    {
        PressurePlate2D plate = Selection.gameObjects.Select(go => go.GetComponent<PressurePlate2D>())
            .FirstOrDefault(component => component != null);
        Door2D door = Selection.gameObjects.Select(go => go.GetComponent<Door2D>())
            .FirstOrDefault(component => component != null);
        if (plate == null || door == null)
        {
            EditorUtility.DisplayDialog("Select two objects", "Select one PressurePlate2D and one Door2D.", "OK");
            return;
        }
        Undo.RecordObject(door, "Connect pressure plate to door");
        door.ConfigureControlSource(plate);
        EditorUtility.SetDirty(door);
        PrefabUtility.RecordPrefabInstancePropertyModifications(door);
        EditorSceneManager.MarkSceneDirty(door.gameObject.scene);
        SceneView.RepaintAll();
    }

    private void RunValidation()
    {
        issues.Clear();
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Add("RV-12", "There is no loaded room Scene.", MessageType.Error);
            return;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        T[] All<T>() where T : Component => roots.SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        if (string.IsNullOrEmpty(scene.path) || !scene.path.StartsWith(LevelRoot + "/", StringComparison.Ordinal))
            Add("RV-12", "The active Scene is not saved below Assets/Scenes/Levels.", MessageType.Warning);
        else if (!HasMatchingRoomDocument(scene.path))
            Add("RV-12", "The room has no matching design document under docs/rooms.", MessageType.Warning);

        if (All<PlayerController2D>().Length > 0)
            Add("RV-12", "Serialized Player found; rooms must use RoomPlayerSpawner2D.", MessageType.Error,
                All<PlayerController2D>()[0]);
        RequireCount("RV-12", All<RoomPlayerSpawner2D>(), 1, "RoomPlayerSpawner2D");
        RequireCount("RV-11", All<RoomResetSystem>(), 1, "RoomResetSystem");
        RequireCount("RV-10", All<CameraFollow2D>(), 1, "CameraFollow2D");

        RoomEntrance2D[] entrances = All<RoomEntrance2D>();
        if (entrances.Length == 0) Add("RV-01", "No RoomEntrance2D exists.", MessageType.Error);
        if (entrances.Count(value => value.IsDefault) != 1)
            Add("RV-12", "Exactly one default entrance is required.", MessageType.Error);
        foreach (IGrouping<string, RoomEntrance2D> duplicate in entrances.GroupBy(value => value.EntranceId,
                     StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
            Add("RV-12", $"Duplicate entrance ID: {duplicate.Key}.", MessageType.Error, duplicate.First());

        foreach (RoomExit2D exit in All<RoomExit2D>())
            if (string.IsNullOrWhiteSpace(exit.TargetScene) || string.IsNullOrWhiteSpace(exit.TargetEntranceId))
                Add("RV-06", "Exit target Scene and entrance ID must be explicit.", MessageType.Error, exit);

        foreach (Door2D door in All<Door2D>())
            if (door.ControlSource == null && (door.ControlSources == null || door.ControlSources.Length == 0))
                Add("RV-08", "Door has no explicit control source. Confirm this is intentional.", MessageType.Warning, door);

        foreach (GameObject gameplayObject in roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                     .Select(value => value.gameObject).Where(IsExpectedPrefabInstance))
            if (PrefabUtility.GetPrefabInstanceStatus(gameplayObject) == PrefabInstanceStatus.Disconnected)
                Add("RV-12", "Gameplay object has a disconnected Prefab instance.", MessageType.Error, gameplayObject);

        ValidateStructure(roots);
        ValidateEntranceClearance(entrances);

        if (issues.Count == 0)
            Add("Static", "No errors found in the implemented checks. Puzzle solvability and timing remain待验证.", MessageType.Info);
        Repaint();
        SceneView.RepaintAll();
    }

    private void ValidateStructure(GameObject[] roots)
    {
        Transform room = roots.Select(root => root.transform).FirstOrDefault(root => root.Find("Grid") != null);
        if (room == null)
        {
            Add("RV-12", "No room root with a Grid child was found.", MessageType.Error);
            return;
        }
        Transform grid = room.Find("Grid");
        foreach (string layerName in StandardTilemapNames)
            if (grid.Find(layerName) == null)
                Add("RV-12", $"Standard Tilemap layer is missing: {layerName}.", MessageType.Warning, grid);

        Tilemap terrain = grid.Find("Terrain")?.GetComponent<Tilemap>();
        if (terrain != null && (terrain.GetComponent<TilemapCollider2D>() == null ||
                                terrain.GetComponent<CompositeCollider2D>() == null ||
                                terrain.GetComponent<SurfaceSemantic2D>()?.Type != SurfaceSemantic2D.SurfaceType.StaticSolid))
            Add("RV-07", "Terrain lacks canonical collision or StaticSolid semantics.", MessageType.Error, terrain);
    }

    private void ValidateEntranceClearance(RoomEntrance2D[] entrances)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Characters/Player.prefab");
        BoxCollider2D playerCollider = prefab != null ? prefab.GetComponent<BoxCollider2D>() : null;
        if (playerCollider == null)
        {
            Add("RV-01", "Player Prefab collider could not be loaded; spawn clearance is待验证.", MessageType.Warning);
            return;
        }
        Vector2 size = Vector2.Scale(playerCollider.size, prefab.transform.localScale) * .95f;
        Physics2D.SyncTransforms();
        foreach (RoomEntrance2D entrance in entrances)
        {
            Collider2D blocking = Physics2D.OverlapBoxAll(entrance.transform.position, size, 0f)
                .FirstOrDefault(collider => collider.enabled && !collider.isTrigger);
            if (blocking != null)
                Add("RV-01", $"Entrance {entrance.EntranceId} overlaps {blocking.name}.", MessageType.Error, entrance);

            Vector2 supportOrigin = (Vector2)entrance.transform.position + Vector2.down * (size.y * .5f);
            bool supported = Physics2D.RaycastAll(supportOrigin, Vector2.down, .2f)
                .Any(hit => hit.collider != null && hit.collider.enabled && !hit.collider.isTrigger);
            if (!supported)
                Add("RV-01", $"Entrance {entrance.EntranceId} has no statically detected support.", MessageType.Warning, entrance);
        }
    }

    private void DrawSceneGuides(SceneView sceneView)
    {
        if (Application.isPlaying || !SceneManager.GetActiveScene().isLoaded) return;
        if (showRelationships)
        {
            Handles.color = new Color(.25f, .9f, .95f, .9f);
            foreach (Door2D door in FindInActiveScene<Door2D>())
            {
                IEnumerable<PressurePlate2D> sources = door.ControlSources != null && door.ControlSources.Length > 0
                    ? door.ControlSources.Where(value => value != null)
                    : door.ControlSource != null ? new[] { door.ControlSource } : Array.Empty<PressurePlate2D>();
                foreach (PressurePlate2D source in sources)
                {
                    Handles.DrawAAPolyLine(3f, source.transform.position, door.transform.position);
                    Handles.Label(Vector3.Lerp(source.transform.position, door.transform.position, .5f),
                        door.Logic.ToString().ToUpperInvariant());
                }
            }
        }

        if (showCameraBounds)
        {
            Handles.color = new Color(.3f, .7f, 1f, .8f);
            foreach (CameraFollow2D follow in FindInActiveScene<CameraFollow2D>())
            {
                SerializedObject serialized = new(follow);
                SerializedProperty enabled = serialized.FindProperty("constrainToRoomBounds");
                SerializedProperty bounds = serialized.FindProperty("roomBounds");
                if (enabled == null || bounds == null || !enabled.boolValue) continue;
                Rect rect = bounds.rectValue;
                Vector3[] points =
                {
                    new(rect.xMin, rect.yMin), new(rect.xMax, rect.yMin), new(rect.xMax, rect.yMax),
                    new(rect.xMin, rect.yMax), new(rect.xMin, rect.yMin)
                };
                Handles.DrawAAPolyLine(2f, points);
                Handles.Label(new Vector3(rect.xMin, rect.yMax), "Camera room bounds");
            }
        }

        if (showMirrorSurfaces)
        {
            Handles.color = new Color(.75f, .35f, 1f, .9f);
            foreach (MirrorSurface2D surface in FindInActiveScene<MirrorSurface2D>())
            {
                foreach (Collider2D collider in surface.GetComponents<Collider2D>())
                    Handles.DrawWireCube(collider.bounds.center, collider.bounds.size);
                Handles.Label(surface.transform.position, $"Mirror: {surface.kind}");
            }
        }

        if (showJumpEnvelope)
            DrawJumpEnvelope();

        if (showColliderBounds && Selection.activeGameObject != null)
        {
            Handles.color = new Color(1f, .75f, .2f, .9f);
            foreach (Collider2D collider in Selection.activeGameObject.GetComponentsInChildren<Collider2D>(true))
            {
                Bounds bounds = collider.bounds;
                Handles.DrawWireCube(bounds.center, bounds.size);
            }
        }
    }

    private static void DrawJumpEnvelope()
    {
        PlayerMovementSettings settings = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(MovementSettingsPath);
        if (settings == null) return;

        Transform origin = Selection.activeTransform;
        if (origin == null)
            origin = FindInActiveScene<RoomEntrance2D>().FirstOrDefault()?.transform;
        if (origin == null) return;

        const int segments = 32;
        float duration = settings.timeToApex * 2f;
        Vector3[] right = new Vector3[segments + 1];
        Vector3[] left = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float time = duration * i / segments;
            float y = settings.JumpSpeed * time - .5f * settings.Gravity * time * time;
            float x = settings.maxSpeed * time;
            right[i] = origin.position + new Vector3(x, y);
            left[i] = origin.position + new Vector3(-x, y);
        }

        Handles.color = new Color(.2f, 1f, .45f, .85f);
        Handles.DrawAAPolyLine(2f, right);
        Handles.DrawAAPolyLine(2f, left);
        Handles.Label(origin.position + Vector3.up * settings.jumpHeight,
            $"Jump apex {settings.jumpHeight:0.##}u · reliable distance {settings.ReliableJumpDistance:0.##}u");
    }

    private static T[] FindInActiveScene<T>() where T : Component => SceneManager.GetActiveScene()
        .GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

    private void RequireCount<T>(string id, T[] values, int expected, string label) where T : Component
    {
        if (values.Length != expected)
            Add(id, $"Expected {expected} {label}, found {values.Length}.", MessageType.Error,
                values.FirstOrDefault());
    }

    private void Add(string id, string message, MessageType type, UnityEngine.Object context = null) =>
        issues.Add(new RoomValidationIssue(id, message, type, context));

    private static bool HasMatchingRoomDocument(string scenePath)
    {
        string[] pieces = scenePath.Split('/');
        if (pieces.Length < 5) return false;
        string region = pieces[^2].ToLowerInvariant();
        string room = Path.GetFileNameWithoutExtension(scenePath).ToUpperInvariant();
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
        return File.Exists(Path.Combine(projectRoot, "docs", "rooms", region, room + ".md"));
    }

    private static bool IsExpectedPrefabInstance(GameObject value) =>
        value.GetComponent<Door2D>() != null || value.GetComponent<PressurePlate2D>() != null ||
        value.GetComponent<RoomExit2D>() != null;

    private static string NormalizeRoomId(string value) => string.IsNullOrWhiteSpace(value)
        ? string.Empty
        : SanitizeToken(value).Replace('-', '_');

    private static string SanitizeToken(string value) => string.IsNullOrWhiteSpace(value)
        ? string.Empty
        : new string(value.Trim().Where(character => char.IsLetterOrDigit(character) ||
            character == '_' || character == '-').ToArray());

    private static void EnsureAssetFolder(string folder)
    {
        string current = "Assets";
        foreach (string segment in folder.Split('/').Skip(1))
        {
            string next = current + "/" + segment;
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segment);
            current = next;
        }
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new(name);
        Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
        child.transform.SetParent(parent, false);
        return child;
    }

    private static bool HasRoomRoot() => SceneManager.GetActiveScene().GetRootGameObjects()
        .Any(root => root.transform.Find("Gameplay") != null && root.transform.Find("Grid") != null);

    private static Transform FindRoomChild(string childName)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform gameplay = root.transform.Find("Gameplay");
            Transform result = gameplay?.Find(childName);
            if (result != null) return result;
        }
        return null;
    }

    private readonly struct PaletteItem
    {
        public readonly string Label;
        public readonly string Path;
        public readonly string ParentName;
        public PaletteItem(string label, string path, string parentName)
        {
            Label = label;
            Path = path;
            ParentName = parentName;
        }
    }

    private readonly struct RoomValidationIssue
    {
        public readonly string Id;
        public readonly string Message;
        public readonly MessageType Type;
        public readonly UnityEngine.Object Context;
        public RoomValidationIssue(string id, string message, MessageType type, UnityEngine.Object context)
        {
            Id = id;
            Message = message;
            Type = type;
            Context = context;
        }
    }
}
