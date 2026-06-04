using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlightPlan : MonoBehaviour
{
    [Header("Runtime Output (ACTIVE plan)")]
    public Transform[] waypoints = Array.Empty<Transform>();

    [Header("Scene References")]
    [SerializeField]
    private LocalTileGrid tileGrid;

    [SerializeField]
    private Transform waypointParent;

    [SerializeField]
    private Transform aircraftRoot;

    [Header("Training Scale")]
    [Tooltip(
        "Compresses scenario waypoint spacing for training gameplay. Keep parent transforms at scale 1."
    )]
    [Range(0.02f, 1f)]
    public float trainingWorldScale = 0.04f;

    [Header("Startup Behavior")]
    [SerializeField]
    private bool autoBuildScenarioOnStart = false;

    [SerializeField]
    private bool snapAircraftToScenarioStartOnLoad = true;

    [Header("Debug")]
    [SerializeField]
    private bool logBuild = true;

    [SerializeField]
    private float gizmoRadiusM = 20f;

    /// <summary>Fired after waypoints[] is fully built.</summary>
    public event System.Action OnRouteBuilt;

    private readonly List<Transform> spawned = new();
    private readonly List<ScenarioDefinition.WaypointDef> currentRouteDefs = new();

    private ScenarioDefinition currentScenario;
    private double currentCenterLat;
    private double currentCenterLon;
    private int currentZoom;

    private void OnEnable() => ScenarioRuntime.OnChanged += LoadScenario;

    private void OnDisable() => ScenarioRuntime.OnChanged -= LoadScenario;

    private void Start()
    {
        if (ScenarioRuntime.Current == null)
            return;

        if (snapAircraftToScenarioStartOnLoad)
            SnapAircraftToScenarioStart(ScenarioRuntime.Current);

        if (autoBuildScenarioOnStart && waypoints.Length == 0)
            LoadScenario(ScenarioRuntime.Current);
    }

    private void LoadScenario(ScenarioDefinition s)
    {
        if (!s)
            return;

        currentScenario = s;

        if (snapAircraftToScenarioStartOnLoad)
            SnapAircraftToScenarioStart(s);

        if (!autoBuildScenarioOnStart)
            return;

        StopAllCoroutines();
        StartCoroutine(BuildWhenTileGridReady(s));
    }

    private IEnumerator BuildWhenTileGridReady(ScenarioDefinition s)
    {
        const float readyTileSizeM = 1000f;
        const float timeoutSec = 2f;

        float elapsed = 0f;
        while (tileGrid && tileGrid.tileSizeM < readyTileSizeM && elapsed < timeoutSec)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        int z = tileGrid ? tileGrid.z : s.baseZoom;
        var route = ResolveRouteFromIdents(s, s.prefillRouteIdents);
        RebuildRoute(route, s.centerLatDeg, s.centerLonDeg, z);

        if (logBuild)
            Debug.Log($"[FlightPlan] Built startup route: {waypoints.Length} @ z={z}");
    }

    public void SnapAircraftToScenarioStart(ScenarioDefinition s)
    {
        if (!aircraftRoot || !s)
            return;

        int z = GetEffectiveZoom(s);
        float tileSizeM = GetEffectiveTileSizeM(s.centerLatDeg, z);
        var startDef = FindAircraftStartDef(s);

        if (startDef != null)
        {
            Vector3 localPos = WaypointDefToLocalPosition(
                startDef,
                s.centerLatDeg,
                s.centerLonDeg,
                z,
                tileSizeM
            );
            Transform parent = waypointParent ? waypointParent : transform;
            aircraftRoot.position = parent.TransformPoint(localPos) + Vector3.up * 0.45f;

            if (Mathf.Abs(startDef.targetHdgDeg) > 0.001f)
                aircraftRoot.rotation = Quaternion.Euler(0f, startDef.targetHdgDeg, 0f);

            Rigidbody rb = aircraftRoot.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }

            if (logBuild)
                Debug.Log($"[FlightPlan] Snapped aircraft to scenario start {startDef.ident}");
            return;
        }

        SnapAircraftToFirstWaypoint();
    }

    public void ActivateRouteFromFms(
        ScenarioDefinition scenario,
        List<ScenarioDefinition.WaypointDef> routeWaypoints,
        int zoom
    )
    {
        if (!scenario || routeWaypoints == null || routeWaypoints.Count == 0)
        {
            if (logBuild)
                Debug.LogWarning("[FlightPlan] ActivateRouteFromFms aborted: missing route data.");
            return;
        }

        currentScenario = scenario;
        RebuildRoute(routeWaypoints, scenario.centerLatDeg, scenario.centerLonDeg, zoom);
    }

    /// <summary>
    /// Rebuild the active waypoint list from a student-edited route.
    /// Runs synchronously once the tile grid is initialized.
    /// </summary>
    public void RebuildRoute(
        List<ScenarioDefinition.WaypointDef> newWpts,
        double centerLat,
        double centerLon,
        int zoom
    )
    {
        StopAllCoroutines();
        ClearSpawned();

        currentCenterLat = centerLat;
        currentCenterLon = centerLon;
        currentZoom = zoom;

        currentRouteDefs.Clear();
        if (newWpts != null)
            currentRouteDefs.AddRange(newWpts);

        float tileSizeM = GetEffectiveTileSizeM(centerLat, zoom);

        if (newWpts != null)
        {
            foreach (var wpDef in newWpts)
            {
                if (wpDef == null || string.IsNullOrWhiteSpace(wpDef.ident))
                    continue;

                var localPos = WaypointDefToLocalPosition(
                    wpDef,
                    centerLat,
                    centerLon,
                    zoom,
                    tileSizeM
                );
                spawned.Add(SpawnWaypointTransform(wpDef, localPos, "WP"));
            }
        }

        waypoints = spawned.ToArray();

        if (logBuild)
            Debug.Log($"[FlightPlan] RebuildRoute: {waypoints.Length} waypoints @ z={zoom}");

        OnRouteBuilt?.Invoke();
    }

    public void RebuildCurrentRoute()
    {
        if (currentRouteDefs.Count == 0)
        {
            if (currentScenario != null && autoBuildScenarioOnStart)
                LoadScenario(currentScenario);

            return;
        }

        RebuildRoute(currentRouteDefs, currentCenterLat, currentCenterLon, currentZoom);
    }

    private void SnapAircraftToFirstWaypoint()
    {
        if (!aircraftRoot)
            return;
        if (waypoints.Length == 0 || !waypoints[0])
            return;

        aircraftRoot.position = waypoints[0].position + Vector3.up * 1.5f;

        if (logBuild)
            Debug.Log($"[FlightPlan] Snapped aircraft to {waypoints[0].name}");
    }

    private ScenarioDefinition.WaypointDef FindAircraftStartDef(ScenarioDefinition s)
    {
        if (!s || s.waypoints == null)
            return null;

        return s.waypoints.Find(w =>
            w != null && w.role == ScenarioDefinition.WaypointRole.AircraftStart
        );
    }

    private List<ScenarioDefinition.WaypointDef> ResolveRouteFromIdents(
        ScenarioDefinition s,
        List<string> idents
    )
    {
        var route = new List<ScenarioDefinition.WaypointDef>();
        if (!s || idents == null)
            return route;

        foreach (var ident in idents)
        {
            if (string.IsNullOrWhiteSpace(ident))
                continue;

            var wpDef = s.waypoints.Find(w =>
                string.Equals(w.ident, ident, StringComparison.OrdinalIgnoreCase)
            );

            if (wpDef != null)
                route.Add(wpDef);
            else if (logBuild)
                Debug.LogWarning(
                    $"[FlightPlan] Missing waypoint '{ident}' in ScenarioDefinition.waypoints"
                );
        }

        return route;
    }

    private void ClearSpawned()
    {
        waypoints = Array.Empty<Transform>();

        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i])
                Destroy(spawned[i].gameObject);

        spawned.Clear();
    }

    private int GetEffectiveZoom(ScenarioDefinition s)
    {
        if (tileGrid && tileGrid.z > 0)
            return tileGrid.z;
        return s ? s.baseZoom : 14;
    }

    private float GetEffectiveTileSizeM(double centerLat, int zoom)
    {
        if (tileGrid && tileGrid.tileSizeM > 0.1f)
            return tileGrid.tileSizeM;
        return WebMercator.MetersPerTile(centerLat, zoom);
    }

    private Vector3 WaypointDefToLocalPosition(
        ScenarioDefinition.WaypointDef wpDef,
        double centerLat,
        double centerLon,
        int zoom,
        float tileSizeM
    )
    {
        var center = LatLonToTileXYFrac(centerLat, centerLon, zoom);
        var tile = LatLonToTileXYFrac(wpDef.latDeg, wpDef.lonDeg, zoom);
        float dxTiles = (float)(tile.x - center.x);
        float dyTiles = (float)(tile.y - center.y);

        return new Vector3(
            dxTiles * tileSizeM * trainingWorldScale,
            0f,
            -dyTiles * tileSizeM * trainingWorldScale
        );
    }

    private Transform SpawnWaypointTransform(
        ScenarioDefinition.WaypointDef wpDef,
        Vector3 localPos,
        string prefix
    )
    {
        var go = new GameObject($"{prefix}_{wpDef.ident}");
        go.transform.SetParent(waypointParent ? waypointParent : transform, false);
        go.transform.localPosition = localPos;
        return go.transform;
    }

    private static (double x, double y) LatLonToTileXYFrac(double latDeg, double lonDeg, int z)
    {
        double latRad = latDeg * Math.PI / 180.0;
        int n = 1 << z;

        double x = (lonDeg + 180.0) / 360.0 * n;
        double y =
            (1.0 - Math.Log(Math.Tan(latRad) + (1.0 / Math.Cos(latRad))) / Math.PI) / 2.0 * n;

        return (x, y);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < waypoints.Length; i++)
        {
            var wp = waypoints[i];
            if (!wp)
                continue;

            Gizmos.DrawSphere(wp.position, gizmoRadiusM);

            if (i > 0 && waypoints[i - 1])
                Gizmos.DrawLine(waypoints[i - 1].position, wp.position);
        }
    }
#endif
}
