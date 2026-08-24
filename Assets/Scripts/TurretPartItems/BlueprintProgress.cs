using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BlueprintProgress
{
    private Dictionary<string, int> partsFound = new Dictionary<string, int>();
    private const int partsRequired = 3;

    public int GetPartsFound(string blueprintId)
    {
        return partsFound.TryGetValue(blueprintId, out int count) ? count : 0;
    }

    public bool IsUnlocked(string blueprintId)
    {
        return GetPartsFound(blueprintId) >= partsRequired;
    }

    public bool AddPart(string blueprintId)
    {
        if (IsUnlocked(blueprintId)) return false;

        if (!partsFound.ContainsKey(blueprintId))
            partsFound[blueprintId] = 0;

        partsFound[blueprintId]++;
        return true;
    }

    public float GetProgress(string blueprintId)
    {
        return Mathf.Clamp01((float)GetPartsFound(blueprintId) / partsRequired);
    }
}
