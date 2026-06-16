using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuildPhase
{
    public string phaseName;
    public GameObject phaseObject;
    public List<Transform> nailPoints;
    public GameObject hitPointPrefabOverride;
}