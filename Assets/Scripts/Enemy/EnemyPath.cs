using System.Collections.Generic;
using UnityEngine;

public class EnemyPath
{
    public readonly List<Vector3> waypoints;
    public readonly Side zone;

    public EnemyPath(List<Vector3> waypoints, Side zone)
    {
        this.waypoints = waypoints;
        this.zone = zone;
    }

    public int Count => waypoints.Count;

    public Vector3 GetWaypoint(int index) => waypoints[index];
}