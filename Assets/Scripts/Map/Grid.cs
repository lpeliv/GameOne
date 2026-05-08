public class Grid
{
    public int gridWidth, gridHeight;
    public Tile[,] gridArray;

    public Grid(int width, int height)
    {
        this.gridWidth = width;
        this.gridHeight = height;

        gridArray = new Tile[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                gridArray[x, z] = new Tile(TileType.Obstacle, x, z);
            }
        }
    }

    public Tile GetTile(int x, int z)
    {
        return gridArray[x, z];
    }
}