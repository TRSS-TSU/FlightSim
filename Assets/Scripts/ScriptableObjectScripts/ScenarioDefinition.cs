using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "FMS/Scenario Definition", fileName = "ScenarioDefinition")]
public class ScenarioDefinition : ScriptableObject
{
    [Header("Scenario Info")]
    public string scenarioTitle;
    public string route;

    [TextArea(3, 6)]
    public string scenarioDescription;

    [Header("Map Anchor (center tile)")]
    public double centerLatDeg = 30.3;
    public double centerLonDeg = -87.3;

    [Header("ND Tile Set")]
    public int baseZoom = 14;

    [Serializable]
    public class WaypointDef
    {
        public string ident; // e.g., KNPA, TEEZY, VR1020_A
        public double latDeg;
        public double lonDeg; // West = negative
        public int distance; // Distance
    }

    [Header("Waypoint Database (known points)")]
    public List<WaypointDef> waypoints = new();

    [Header("Prefill Route (student modifies)")]
    public List<string> prefillRouteIdents = new();

    [Header("Approach Sets (optional)")]
    public List<string> rnav25LFixes = new();

    [Header("Performance (PERF INIT)")]
    public float zfwLbs = 0f; // Zero-fuel weight in lbs
    public float fuelWeightLbs = 0f; // Initial fuel load in lbs
}
