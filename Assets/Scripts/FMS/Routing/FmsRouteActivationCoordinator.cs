using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FmsRouteActivationCoordinator : MonoBehaviour
{
    public MapLoadingOverlay loadingOverlay;
    public FlightPlan flightPlan;
    public NavAutopilot navAutopilot;
    public RouteTilePreloader tilePreloader;
    public RouteTileAuditLogger tileAuditLogger;
    public LocalTileChunkGrid chunkGrid;

    public UnityEvent routeActivated;

    public bool IsExecuting { get; private set; }

    public void ExecuteModifiedRoute(
        ScenarioDefinition scenario,
        List<ScenarioDefinition.WaypointDef> routeWaypoints,
        RouteContinuitySnapshot snapshot,
        bool forceFirstLeg,
        Action<int> onComplete = null
    )
    {
        if (IsExecuting)
        {
            Debug.LogWarning(
                "[FmsRouteActivationCoordinator] Route activation already in progress."
            );
            return;
        }

        StartCoroutine(
            ExecuteModifiedRouteRoutine(
                scenario,
                routeWaypoints,
                snapshot,
                forceFirstLeg,
                onComplete
            )
        );
    }

    private IEnumerator ExecuteModifiedRouteRoutine(
        ScenarioDefinition scenario,
        List<ScenarioDefinition.WaypointDef> routeWaypoints,
        RouteContinuitySnapshot snapshot,
        bool forceFirstLeg,
        Action<int> onComplete
    )
    {
        IsExecuting = true;

        if (!scenario || routeWaypoints == null || routeWaypoints.Count == 0)
        {
            Debug.LogWarning(
                "[FmsRouteActivationCoordinator] Activation aborted: missing route data."
            );
            IsExecuting = false;
            yield break;
        }

        if (navAutopilot)
            navAutopilot.SetNavEngaged(false);

        int zoom =
            scenario.preloadZoomOverride > 0 ? scenario.preloadZoomOverride : scenario.baseZoom;

        if (ShouldUseIndividualTilePreload(scenario) && tilePreloader)
        {
            tileAuditLogger?.AuditRoute(scenario, routeWaypoints, zoom);

            yield return tilePreloader.PreloadForRoute(scenario, routeWaypoints, zoom);
        }
        else
        {
            if (scenario.mapRasterLoadMode == MapRasterLoadMode.StitchedChunks)
            {
                Debug.Log(
                    "[FmsRouteActivationCoordinator] Skipping individual tile preload because map mode is StitchedChunks."
                );

                yield return WaitForChunkMapLoad();
            }

            loadingOverlay?.Show("Building flight plan...");
            yield return null;
        }

        if (flightPlan)
            flightPlan.ActivateRouteFromFms(scenario, routeWaypoints, zoom);

        int activeIndex = 0;
        if (navAutopilot)
        {
            activeIndex = forceFirstLeg
                ? 0
                : RouteResolver.Resolve(
                    snapshot,
                    routeWaypoints,
                    flightPlan ? flightPlan.waypoints : null
                );

            navAutopilot.activeIndex = activeIndex;
            navAutopilot.ResetCaptureState();
        }

        loadingOverlay?.Hide();
        IsExecuting = false;
        onComplete?.Invoke(activeIndex);
        routeActivated?.Invoke();
    }

    private static bool ShouldUseIndividualTilePreload(ScenarioDefinition scenario)
    {
        if (!scenario)
            return false;

        if (!scenario.preloadTilesOnRouteExecute)
            return false;

        return scenario.mapRasterLoadMode == MapRasterLoadMode.IndividualTiles;
    }

    private IEnumerator WaitForChunkMapLoad()
    {
        if (!chunkGrid)
            chunkGrid = FindFirstObjectByType<LocalTileChunkGrid>();

        if (!chunkGrid)
            yield break;

        if (chunkGrid.IsLoading)
            loadingOverlay?.Show("Loading map chunks...");

        while (chunkGrid.IsLoading)
            yield return null;

        if (!chunkGrid.IsLoaded)
        {
            Debug.LogWarning(
                "[FmsRouteActivationCoordinator] Chunk map loader did not report a complete load before route activation."
            );
        }
    }
}
