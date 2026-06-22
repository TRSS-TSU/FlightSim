using UnityEngine;

public class Tile
{
    public Vector2Int WorldTileIndex;
    public TileContent Content;

    public Tile(Vector2Int index, TileContent content)
    {
        WorldTileIndex = index;
        Content = content;
    }
}
