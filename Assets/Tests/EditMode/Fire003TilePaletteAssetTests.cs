using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class Fire003TilePaletteAssetTests
{
    private const string FirePalettePath = "Assets/TilePalettes/Fire.prefab";
    private const string TerrainTilePath = "Assets/Tiles/Graybox/Fire003Terrain.asset";
    private const string HintTilePath = "Assets/Tiles/Graybox/Fire003MirrorHint.asset";

    [Test]
    public void FirePaletteContainsFire003AuthoringTilesExactlyOnce()
    {
        GameObject palette = AssetDatabase.LoadAssetAtPath<GameObject>(FirePalettePath);
        Tile terrainTile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
        Tile hintTile = AssetDatabase.LoadAssetAtPath<Tile>(HintTilePath);

        Assert.That(palette, Is.Not.Null);
        Assert.That(terrainTile, Is.Not.Null);
        Assert.That(hintTile, Is.Not.Null);
        Grid grid = palette.GetComponent<Grid>();
        Tilemap tilemap = palette.GetComponentInChildren<Tilemap>(true);
        Assert.That(grid, Is.Not.Null);
        Assert.That(grid.cellLayout, Is.EqualTo(GridLayout.CellLayout.Rectangle));
        Assert.That(grid.cellSize, Is.EqualTo(Vector3.one));
        Assert.That(tilemap, Is.Not.Null);

        TileBase[] usedTiles = new TileBase[tilemap.GetUsedTilesCount()];
        tilemap.GetUsedTilesNonAlloc(usedTiles);
        Assert.That(usedTiles.Count(tile => tile == terrainTile), Is.EqualTo(1));
        Assert.That(usedTiles.Count(tile => tile == hintTile), Is.EqualTo(1));
    }
}
