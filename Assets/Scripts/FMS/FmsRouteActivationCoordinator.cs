using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FmsRouteActivationCoordinator : MonoBehaviour
{
    public RouteTilePreloader tilePreloader;
    public MapLoadingOverlay loadingOverlay;
    public FlightPlan flightPlan;
    public NavAutopilot navAutopilot;

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
            Debug.LogWarning("[FmsRouteActivationCoordinator] Route activation already in progress.");
            return;
        }

        StartCoroutine(
            ExecuteModifiedRouteRoutine(scenario, routeWaypoints, snapshot, forceFirstLeg, onComplete)
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
            Debug.LogWarning("[FmsRouteActivationCoordinator] Activation aborted: missing route data.");
            IsExecuting = false;
            yield break;
        }

        if (navAutopilot)
            navAutopilot.SetNavEngaged(false);

        int zoom = scenario.preloadZoomOverride > 0 ? scenario.preloadZoomOverride : scenario.baseZoom;

        if (scenario.preloadTilesOnRouteExecute && tilePreloader)
        {
            yield return tilePreloader.PreloadForRoute(scenario, routeWaypoints, zoom);
        }
        else
        {
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
                : RouteResolver.Resolve(snapshot, routeWaypoints, flightPlan ? flightPlan.waypoints : null);

            navAutopilot.activeIndex = activeIndex;
            navAutopilot.ResetCaptureState();
        }

        loadingOverlay?.Hide();
        IsExecuting = false;
        onComplete?.Invoke(activeIndex);
    }
}
