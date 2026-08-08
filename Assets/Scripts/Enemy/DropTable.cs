using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DropTable
{
    public List<DropEntry> entries = new List<DropEntry>();

    public List<DropEntry> RollDrops()
    {
        List<DropEntry> drops = new List<DropEntry>();

        foreach (DropEntry entry in entries)
        {
            if (entry.item == null) continue;

            float roll = UnityEngine.Random.value;
            if (roll <= entry.dropChance)
                drops.Add(entry);
        }

        return drops;
    }
}