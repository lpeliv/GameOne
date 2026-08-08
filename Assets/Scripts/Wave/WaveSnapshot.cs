using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BudSnapshot
{
    public int budIndex;
    public float currentHP;
    public bool isDestroyed;
}

[System.Serializable]
public class WaveSnapshot
{
    public int waveIndex;
    public float playerHP;
    public List<BudSnapshot> budSnapshots = new List<BudSnapshot>();

}