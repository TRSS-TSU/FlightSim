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

        // If startup route building is enabled, let LoadScenario/BuildWhenTileGridReady
        // handle the snap after the tile grid is ready.
        if (autoBuildScenarioOnStart && waypoints.Length == 0)
        {
            LoadScenario(ScenarioRuntime.Current);
            return;
        }

        if (snapAircraftToScenarioStartOnLoad)
        {
            SnapAircraftToScenarioStart(ScenarioRuntime.Current);
            Physics.SyncTransforms();
            StabilizeAircraftAfterSnap();
            StartCoroutine(StabilizeAircraftNextFixedUpdate());
        }
    }

    private void LoadScenario(ScenarioDefinition s)
    {
        if (!s)
            return;

        currentScenario = s;

        if (autoBuildScenarioOnStart)
        {
            StopAllCoroutines();
            StartCoroutine(BuildWhenTileGridReady(s));
            return;
        }

        if (snapAircraftToScenarioStartOnLoad)
        {
            SnapAircraftToScenarioStart(s);
            Physics.SyncTransforms();
            StabilizeAircraftAfterSnap();
            StartCoroutine(StabilizeAircraftNextFixedUpdate());
        }
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

        // Important:
        // Snap only after the tile grid has had a chance to initialize.
        // This keeps spawn conversion consistent with calibration conversion.
        if (snapAircraftToScenarioStartOnLoad)
        {
            SnapAircraftToScenarioStart(s);
            Physics.SyncTransforms();
            StabilizeAircraftAfterSnap();
            yield return new WaitForFixedUpdate();
            StabilizeAircraftAfterSnap();
        }

        var route = ResolveRouteFromIdents(s, s.prefillRouteIdents);
        RebuildRoute(route, s.centerLatDeg, s.centerLonDeg, z);
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

            Vector3 worldPos = parent.TransformPoint(localPos);
            worldPos.y = 0f; // ground invariant: ground y = 0

            aircraftRoot.position = worldPos;

            float startHeading = NormalizeHeading(startDef.targetHdgDeg);

            aircraftRoot.rotation = Quaternion.Euler(0f, startHeading, 0f);

            SimTargets targets = aircraftRoot.GetComponent<SimTargets>();
            if (targets)
            {
                targets.targetIasKt = Mathf.Max(0f, startDef.targetIasKt);
                targets.targetAltFtMsl = Mathf.Max(0f, startDef.targetAltFtMsl);
                targets.targetHdgDeg = startHeading;
            }

            NavAutopilot nav = aircraftRoot.GetComponent<NavAutopilot>();
            if (nav)
            {
                nav.SetNavEngaged(false);
                nav.activeIndex = 0;
                nav.ResetCaptureState();

                // Re-assert after SetNavEngaged(false), just to keep the runway heading authoritative.
                if (targets)
                    targets.targetHdgDeg = startHeading;
            }

            Rigidbody rb = aircraftRoot.GetComponent<Rigidbody>();
            if (rb)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                rb.position = aircraftRoot.position;
                rb.rotation = aircraftRoot.rotation;
                rb.Sleep();
            }

            PlaneController plane = aircraftRoot.GetComponent<PlaneController>();
            if (plane)
                plane.ArmParkedPoseHold(worldPos, startHeading);

            return;
        }

        SnapAircraftToFirstWaypoint();
    }

    private static float NormalizeHeading(float hdg)
    {
        hdg %= 360f;
        return hdg < 0f ? hdg + 360f : hdg;
    }

    private IEnumerator StabilizeAircraftNextFixedUpdate()
    {
        yield return new WaitForFixedUpdate();
        StabilizeAircraftAfterSnap();
    }

    private void StabilizeAircraftAfterSnap()
    {
        if (!aircraftRoot)
            return;

        Vector3 p = aircraftRoot.position;
        p.y = 0f;
        aircraftRoot.position = p;

        Rigidbody rb = aircraftRoot.GetComponent<Rigidbody>();
        if (!rb)
            return;

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        rb.position = aircraftRoot.position;
        rb.rotation = aircraftRoot.rotation;

        rb.Sleep();
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

    public bool TryWorldPositionToLatLon(
        ScenarioDefinition scenario,
        Vector3 worldPos,
        out double latDeg,
        out double lonDeg
    )
    {
        latDeg = 0d;
        lonDeg = 0d;

        if (!scenario)
            return false;

        int z = GetEffectiveZoom(scenario);
        float tileSizeM = GetEffectiveTileSizeM(scenario.centerLatDeg, z);

        if (tileSizeM <= 0.001f || trainingWorldScale <= 0.001f)
            return false;

        Transform parent = waypointParent ? waypointParent : transform;

        Vector3 localPos = parent.InverseTransformPoint(worldPos);

        var center = LatLonToTileXYFrac(scenario.centerLatDeg, scenario.centerLonDeg, z);

        double dxTiles = localPos.x / (tileSizeM * trainingWorldScale);
        double dyTiles = -localPos.z / (tileSizeM * trainingWorldScale);

        double tileX = center.x + dxTiles;
        double tileY = center.y + dyTiles;

        TileXYFracToLatLon(tileX, tileY, z, out latDeg, out lonDeg);
        return true;
    }

    private static void TileXYFracToLatLon(
        double tileX,
        double tileY,
        int z,
        out double latDeg,
        out double lonDeg
    )
    {
        double n = 1 << z;

        lonDeg = tileX / n * 360.0 - 180.0;

        double y = Math.PI * (1.0 - 2.0 * tileY / n);
        latDeg = Math.Atan(Math.Sinh(y)) * 180.0 / Math.PI;
    }

    public bool TryGetWaypointWorldPosition(
        ScenarioDefinition scenario,
        ScenarioDefinition.WaypointDef wpDef,
        out Vector3 worldPos
    )
    {
        worldPos = default;

        if (!scenario || wpDef == null)
            return false;

        int z = GetEffectiveZoom(scenario);
        float tileSizeM = GetEffectiveTileSizeM(scenario.centerLatDeg, z);

        Vector3 localPos = WaypointDefToLocalPosition(
            wpDef,
            scenario.centerLatDeg,
            scenario.centerLonDeg,
            z,
            tileSizeM
        );

        Transform parent = waypointParent ? waypointParent : transform;
        worldPos = parent.TransformPoint(localPos);
        worldPos.y = 0f;

        return true;
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
        if (s && s.preloadZoomOverride > 0)
            return s.preloadZoomOverride;

        return s ? s.baseZoom : 14;
    }

    private float GetEffectiveTileSizeM(double centerLat, int zoom)
    {
        if (tileGrid && tileGrid.z == zoom && tileGrid.tileSizeM > 0.1f)
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
