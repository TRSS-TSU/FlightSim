using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlightPlan : MonoBehaviour
{
    [Header("Runtime Output (ACTIVE plan)")]
    public Transform[] waypoints = Array.Empty<Transform>();

    [Header("Scene References")]
    [SerializeField] private LocalTileGrid tileGrid;        // GroundRoot/LocalTileGrid
    [SerializeField] private Transform waypointParent;      // e.g., GroundRoot/WaypointRoot
    [SerializeField] private Transform aircraftRoot;        // AircraftRoot (optional)

    [Header("Debug")]
    [SerializeField] private bool logBuild = true;
    [SerializeField] private float gizmoRadiusM = 20f;

    private readonly List<Transform> spawned = new();

    private void OnEnable()  => ScenarioRuntime.OnChanged += LoadScenario;
    private void OnDisable() => ScenarioRuntime.OnChanged -= LoadScenario;

    private void Start()
    {
        // Catch up if scenario was selected in Menu scene.
        if (ScenarioRuntime.Current != null && waypoints.Length == 0)
            LoadScenario(ScenarioRuntime.Current);
    }

    private void LoadScenario(ScenarioDefinition s)
    {
        if (!s) return;

        StopAllCoroutines();
        StartCoroutine(BuildWhenTileGridReady(s));
    }

    private IEnumerator BuildWhenTileGridReady(ScenarioDefinition s)
    {
        // Wait until LocalTileGrid has applied scenario (tileSizeM becomes "large").
        const float readyTileSizeM = 1000f;
        const float timeoutSec = 2f;

        float elapsed = 0f;
        while (tileGrid && tileGrid.tileSizeM < readyTileSizeM && elapsed < timeoutSec)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ClearSpawned();

        int z = tileGrid ? tileGrid.z : s.baseZoom;
        float tileSizeM = tileGrid ? tileGrid.tileSizeM : WebMercator.MetersPerTile(s.centerLatDeg, z);

        if (!tileGrid && logBuild)
            Debug.LogWarning("[FlightPlan] tileGrid not assigned; using scenario.baseZoom + WebMercator.");

        var center = LatLonToTileXYFrac(s.centerLatDeg, s.centerLonDeg, z);

        foreach (var ident in s.prefillRouteIdents)
        {
            if (string.IsNullOrWhiteSpace(ident)) continue;

            var wpDef = s.waypoints.Find(w =>
                string.Equals(w.ident, ident, StringComparison.OrdinalIgnoreCase));

            if (wpDef == null)
            {
                Debug.LogWarning($"[FlightPlan] Missing waypoint '{ident}' in ScenarioDefinition.waypoints");
                continue;
            }

            var tile = LatLonToTileXYFrac(wpDef.latDeg, wpDef.lonDeg, z);
            float dxTiles = (float)(tile.x - center.x);
            float dyTiles = (float)(tile.y - center.y);

            // Slippy Y increases south; Unity +Z is north → flip sign into Z.
            Vector3 localPos = new(dxTiles * tileSizeM, 0f, -dyTiles * tileSizeM);

            var go = new GameObject($"WP_{wpDef.ident}");
            go.transform.SetParent(waypointParent ? waypointParent : transform, false);
            go.transform.localPosition = localPos;

            spawned.Add(go.transform);
        }

        waypoints = spawned.ToArray();

        SnapAircraftToFirstWaypoint();

        if (logBuild)
            Debug.Log($"[FlightPlan] Built ACTIVE plan: {waypoints.Length} @ z={z} tileSizeM={tileSizeM:0.00}");
    }

    private void SnapAircraftToFirstWaypoint()
    {
        if (!aircraftRoot) return;
        if (waypoints.Length == 0 || !waypoints[0]) return;

        aircraftRoot.position = waypoints[0].position + Vector3.up * 1.5f;

        if (logBuild)
            Debug.Log($"[FlightPlan] Snapped aircraft to {waypoints[0].name}");
    }

    /// <summary>
    /// Rebuild the active waypoint list from a student-edited route (called by ActLegsView).
    /// Runs synchronously — safe to call at runtime once the tile grid is initialised.
    /// </summary>
    public void RebuildRoute(List<ScenarioDefinition.WaypointDef> newWpts,
                              double centerLat, double centerLon, int zoom)
    {
        StopAllCoroutines();
        ClearSpawned();

        float tileSizeM = tileGrid ? tileGrid.tileSizeM
                                   : (float)WebMercator.MetersPerTile(centerLat, zoom);
        var center = LatLonToTileXYFrac(centerLat, centerLon, zoom);

        foreach (var wpDef in newWpts)
        {
            if (wpDef == null || string.IsNullOrWhiteSpace(wpDef.ident)) continue;

            var tile    = LatLonToTileXYFrac(wpDef.latDeg, wpDef.lonDeg, zoom);
            float dxTiles = (float)(tile.x - center.x);
            float dyTiles = (float)(tile.y - center.y);

            Vector3 localPos = new(dxTiles * tileSizeM, 0f, -dyTiles * tileSizeM);

            var go = new GameObject($"WP_{wpDef.ident}");
            go.transform.SetParent(waypointParent ? waypointParent : transform, false);
            go.transform.localPosition = localPos;
            spawned.Add(go.transform);
        }

        waypoints = spawned.ToArray();

        if (logBuild)
            Debug.Log($"[FlightPlan] RebuildRoute: {waypoints.Length} waypoints @ z={zoom}");
    }

    private void ClearSpawned()
    {
        waypoints = Array.Empty<Transform>();

        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) Destroy(spawned[i].gameObject);

        spawned.Clear();
    }

    private static (double x, double y) LatLonToTileXYFrac(double latDeg, double lonDeg, int z)
    {
        double latRad = latDeg * Math.PI / 180.0;
        int n = 1 << z;

        double x = (lonDeg + 180.0) / 360.0 * n;
        double y = (1.0 - Math.Log(Math.Tan(latRad) + (1.0 / Math.Cos(latRad))) / Math.PI) / 2.0 * n;

        return (x, y);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < waypoints.Length; i++)
        {
            var wp = waypoints[i];
            if (!wp) continue;

            Gizmos.DrawSphere(wp.position, gizmoRadiusM);

            if (i > 0 && waypoints[i - 1])
                Gizmos.DrawLine(waypoints[i - 1].position, wp.position);
        }
    }
#endif
}