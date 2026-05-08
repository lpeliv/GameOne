using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObstaclePlacementData
{
    public string definitionName;
    public Vector2Int rootTile;

    public int sizeX;
    public int sizeZ;

    public List<Vector2Int> occupiedTiles;

    public ObstaclePlacementData(string definitionName, Vector2Int rootTile, int sizeX, int sizeZ, List<Vector2Int> occupiedTiles)
    {
        this.definitionName = definitionName;
        this.rootTile = rootTile;
        this.sizeX = sizeX;
        this.sizeZ = sizeZ;
        this.occupiedTiles = occupiedTiles;
    }
}
