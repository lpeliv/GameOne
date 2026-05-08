using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ObstacleCatalogue", menuName = "Map/Obstacle Catalogue")]
public class ObstacleCatalogue : ScriptableObject
{
    public List<ObstacleDefinition> obstacles;

    public ObstacleDefinition GetWeightedRandom()
    {
        float totalWeight = 0f;
        foreach (ObstacleDefinition def in obstacles)
            totalWeight += def.spawnWeight;
        
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach(ObstacleDefinition def in obstacles)
        {
            cumulative += def.spawnWeight;
            if (roll <= cumulative)
                return def;
        }

        return obstacles[obstacles.Count - 1];
    }

    public ObstacleDefinition GetByName(string name)
    {
        return obstacles.Find(o  => o.displayName == name);
    }
}