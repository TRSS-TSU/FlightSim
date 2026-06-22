using System;
using System.Collections.Generic;
using UnityEngine;

public enum MapRasterLoadMode
{
    IndividualTiles,
    StitchedChunks,
}

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

    [Header("Map Preload")]
    [Min(0)]
    public int preloadPaddingTiles = 2;
    public int preloadZoomOverride = 0; // 0 = use baseZoom
    public bool preloadTilesOnRouteExecute = true;

    [Header("Map Raster Runtime")]
    public MapRasterLoadMode mapRasterLoadMode = MapRasterLoadMode.IndividualTiles;

    [Header("Fixed Tile Coverage")]
    public bool useFixedTileBounds = false;
    public int fixedTileZoom = 14;
    public int fixedTileMinX = 4096;
    public int fixedTileMaxX = 4300;
    public int fixedTileMinY = 6599;
    public int fixedTileMaxY = 6785;

    public enum WaypointRole
    {
        Route,
        AircraftStart,
        Takeoff,
        Approach,
        Landing,
        Hold,
        FinalStop,
    }

    [Serializable]
    public class WaypointDef
    {
        public string ident; // e.g., KNPA, TEEZY, VR1020_A
        public double latDeg;
        public double lonDeg; // West = negative
        public int distance; // Distance
        public WaypointRole role = WaypointRole.Route;
        public bool includeInActiveRoute = true;
        public float targetAltFtMsl = 0f;
        public float targetIasKt = 0f;
        public float targetHdgDeg = 0f;
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
