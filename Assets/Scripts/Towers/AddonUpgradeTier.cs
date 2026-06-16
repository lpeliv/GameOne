using UnityEngine;

[System.Serializable]
public class AddonUpgradeTier
{
    public string tierName;
    public GameObject tierPrefab;
    public float damageMultiplier = 1.5f;
    public float rangeMultiplier = 1.2f;
    public float fireRateMultiplier = 1.2f;
    public int upgradeCost;
    public float rotationSpeedMultiplier = 1.2f;
}