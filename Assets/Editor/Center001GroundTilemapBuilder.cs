using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Center001GroundTilemapBuilder
{
    private const string ScenePath = "Assets/Scenes/Levels/Center/Center_001.unity";
    private const string TexturePath = "Assets/Art/Center/Tiles/center_stone_ground_v1.png";
    private const string TilePath = "Assets/Tiles/Center/CenterStoneGround.asset";

    [MenuItem("Tools/W1/Center/Apply CENTER-001 Ground Tilemap")]
    public static void Apply()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject room = scene.GetRootGameObjects()
            .Single(root => root.name == "CENTER-001 Mirror Beginning");

        Transform oldGround = room.transform.Find("Continuous Ground");
        if (oldGround != null) Object.DestroyImmediate(oldGround.gameObject);

        Transform gridTransform = EnsureChild(room.transform, "Grid");
        Grid grid = GetOrAdd<Grid>(gridTransform.gameObject);
        grid.cellSize = Vector3.one;

        Transform terrainTransform = EnsureChild(gridTransform, "Terrain");
        Tilemap terrain = GetOrAdd<Tilemap>(terrainTransform.gameObject);
        if (terrainTransform.gameObject.GetComponent<TilemapRenderer>() == null)
            terrainTransform.gameObject.AddComponent<TilemapRenderer>();

        ConfigureCollisionAndSemantics(terrainTransform.gameObject);
        Tile tile = CreateOrUpdateTile();

        terrain.ClearAllTiles();
        for (int x = -14; x <= 13; x++)
            terrain.SetTile(new Vector3Int(x, -4, 0), tile);

        terrain.CompressBounds();
        terrain.RefreshAllTiles();
        TilemapCollider2D tilemapCollider = terrain.GetComponent<TilemapCollider2D>();
        tilemapCollider.ProcessTilemapChanges();
        Physics2D.SyncTransforms();
        CompositeCollider2D composite = terrain.GetComponent<CompositeCollider2D>();
        composite.GenerateGeometry();

        Require(composite.pathCount > 0, "CENTER_001 Terrain collider geometry was not generated.");
        Require(composite.bounds.min.x <= -13.99f && composite.bounds.max.x >= 13.99f,
            "CENTER_001 Terrain must continuously cover x=-14..14.");
        Require(Mathf.Approximately(composite.bounds.max.y, -3f),
            "CENTER_001 Terrain top must remain at y=-3.");

        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), $"Failed to save {ScenePath}.");
        AssetDatabase.SaveAssets();
        Debug.Log("CENTER_001 ground converted to a 28-cell Terrain Tilemap.");
    }

    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) return child;
        GameObject created = new(name);
        created.transform.SetParent(parent, false);
        return created.transform;
    }

    private static void ConfigureCollisionAndSemantics(GameObject terrain)
    {
        Rigidbody2D body = GetOrAdd<Rigidbody2D>(terrain);
        body.bodyType = RigidbodyType2D.Static;
        if (terrain.GetComponent<CompositeCollider2D>() == null)
            terrain.AddComponent<CompositeCollider2D>();
        TilemapCollider2D collider = GetOrAdd<TilemapCollider2D>(terrain);
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;

        SurfaceSemantic2D semantic = GetOrAdd<SurfaceSemantic2D>(terrain);
        semantic.Configure(SurfaceSemantic2D.SurfaceType.StaticSolid, true, true);

        MirrorSurface2D mirror = GetOrAdd<MirrorSurface2D>(terrain);
        mirror.kind = MirrorSurface2D.SurfaceKind.Ground;
        mirror.safe = true;
    }

    private static Tile CreateOrUpdateTile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(TilePath));
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(TexturePath).OfType<Sprite>().Single();
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, TilePath);
        }

        tile.name = Path.GetFileNameWithoutExtension(TilePath);
        tile.sprite = sprite;
        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}
