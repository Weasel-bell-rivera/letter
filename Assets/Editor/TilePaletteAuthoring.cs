using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Idempotently adds required Tile assets to a shared rectangular Palette without clearing or moving
/// manually-authored Palette cells. Editor-only authoring data must never drive runtime surface rules.
/// </summary>
public static class TilePaletteAuthoring
{
    private const int MaximumPaletteColumns = 1024;

    public static GameObject EnsureTiles(string palettePath, params TileBase[] requiredTiles)
    {
        if (string.IsNullOrWhiteSpace(palettePath) || !palettePath.StartsWith("Assets/", StringComparison.Ordinal) ||
            !palettePath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Tile Palette path must be an Assets-relative .prefab path.", nameof(palettePath));
        if (requiredTiles == null || requiredTiles.Length == 0 || requiredTiles.Any(tile => tile == null))
            throw new ArgumentException("Tile Palette synchronization requires non-null Tile assets.", nameof(requiredTiles));

        string folderPath = Path.GetDirectoryName(palettePath)?.Replace('\\', '/');
        Require(!string.IsNullOrEmpty(folderPath), $"Invalid Tile Palette folder: {palettePath}");
        EnsureAssetFolder(folderPath);

        GameObject palette = AssetDatabase.LoadAssetAtPath<GameObject>(palettePath);
        if (palette == null)
        {
            palette = GridPaletteUtility.CreateNewPalette(
                folderPath,
                Path.GetFileNameWithoutExtension(palettePath),
                GridLayout.CellLayout.Rectangle,
                GridPalette.CellSizing.Automatic,
                Vector3.one,
                GridLayout.CellSwizzle.XYZ);
            Require(palette != null && AssetDatabase.GetAssetPath(palette) == palettePath,
                $"Failed to create Tile Palette at {palettePath}.");
        }

        Grid grid = palette.GetComponent<Grid>();
        Tilemap tilemap = palette.GetComponentInChildren<Tilemap>(true);
        GridPalette settings = AssetDatabase.LoadAssetAtPath<GridPalette>(palettePath);
        Require(grid != null && tilemap != null && settings != null,
            $"Tile Palette is missing its Grid, Tilemap or GridPalette settings: {palettePath}");
        Require(grid.cellLayout == GridLayout.CellLayout.Rectangle && grid.cellSize == Vector3.one,
            $"Tile Palette must use a rectangular 1x1 grid: {palettePath}");

        foreach (TileBase tile in requiredTiles.Distinct())
        {
            if (Contains(tilemap, tile))
                continue;

            tilemap.SetTile(FindEmptyCell(tilemap), tile);
        }

        EditorUtility.SetDirty(tilemap);
        EditorUtility.SetDirty(palette);
        AssetDatabase.SaveAssetIfDirty(palette);
        return palette;
    }

    private static bool Contains(Tilemap tilemap, TileBase requiredTile)
    {
        int count = tilemap.GetUsedTilesCount();
        if (count == 0)
            return false;

        TileBase[] usedTiles = new TileBase[count];
        tilemap.GetUsedTilesNonAlloc(usedTiles);
        return usedTiles.Contains(requiredTile);
    }

    private static Vector3Int FindEmptyCell(Tilemap tilemap)
    {
        for (int x = 0; x < MaximumPaletteColumns; x++)
        {
            Vector3Int cell = new(x, 0, 0);
            if (!tilemap.HasTile(cell))
                return cell;
        }

        throw new InvalidOperationException(
            $"Tile Palette row 0 has no empty cell within {MaximumPaletteColumns} columns: {AssetDatabase.GetAssetPath(tilemap)}");
    }

    private static void EnsureAssetFolder(string folderPath)
    {
        string[] segments = folderPath.Split('/');
        string current = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string next = $"{current}/{segments[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segments[i]);
            current = next;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
