using TankDestroyer.API;

namespace TankDestroyer.Engine.Extensions;

public static class TileExtensions
{
    public static bool IsWater(this Tile tile)
    {
        return tile.TileType == TileType.Water;
    }
    
    public static bool IsLand(this Tile tile)
    {
        return !tile.IsWater();
    }
        
    public static bool IsDestroyable(this Tile tile)
    {
        return tile.TileType is TileType.Building or TileType.Tree;
    }
}