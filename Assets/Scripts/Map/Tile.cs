public struct Tile
{
    public TileType type;
    public int x;
    public int z;

    public Tile(TileType tileType, int xCoord, int zCoord)
    {
        this.type = tileType;
        this.x = xCoord;
        this.z = zCoord;
    }
}