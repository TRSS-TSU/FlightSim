using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RouteTilePreloader : MonoBehaviour
{
    public LocalTileGrid tileGrid;
    public MapLoadingOverlay loadingOverlay;

    [Min(0)]
    public int defaultPaddingTiles = 2;

    [Min(1)]
    public int largeTileCountWarning = 1024;

    public IEnumerator PreloadForRoute(
        ScenarioDefinition scenario,
        IReadOnlyList<ScenarioDefinition.WaypointDef> routeWaypoints,
        int zoom,
        Action onComplete = null
    )
    {
        if (!scenario || routeWaypoints == null || routeWaypoints.Count == 0)
        {
            Debug.LogWarning("[RouteTilePreloader] Preload aborted: missing scenario or route.");
            onComplete?.Invoke();
            yield break;
        }

        if (!tileGrid)
        {
            Debug.LogWarning("[RouteTilePreloader] Preload skipped: tileGrid is not assigned.");
            onComplete?.Invoke();
            yield break;
        }

        int padding = scenario.preloadPaddingTiles >= 0
            ? scenario.preloadPaddingTiles
            : defaultPaddingTiles;

        var indexes = BuildTileIndexList(routeWaypoints, zoom, padding);

        if (indexes.Count >= largeTileCountWarning)
            Debug.LogWarning(
                $"[RouteTilePreloader] Large fixed preload z={zoom} total={indexes.Count}. Consider reducing padding or zoom."
            );

        loadingOverlay?.Show("Loading route map tiles...");
        loadingOverlay?.SetProgress(0, indexes.Count);

        yield return tileGrid.BuildFixedTileSet(
            scenario,
            indexes,
            zoom,
            (loaded, total) => loadingOverlay?.SetProgress(loaded, total)
        );

        onComplete?.Invoke();
    }

    public static List<Vector2Int> BuildTileIndexList(
        IReadOnlyList<ScenarioDefinition.WaypointDef> routeWaypoints,
        int zoom,
        int paddingTiles
    )
    {
        var result = new List<Vector2Int>();
        if (routeWaypoints == null || routeWaypoints.Count == 0)
            return result;

        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        for (int i = 0; i < routeWaypoints.Count; i++)
        {
            var wp = routeWaypoints[i];
            if (wp == null)
                continue;

            LocalTileGrid.LatLonToTileXY(wp.latDeg, wp.lonDeg, zoom, out int x, out int y);
            minX = Mathf.Min(minX, x);
            maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
        }

        if (minX == int.MaxValue)
            return result;

        minX -= paddingTiles;
        maxX += paddingTiles;
        minY -= paddingTiles;
        maxY += paddingTiles;

        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
            result.Add(new Vector2Int(x, y));

        Debug.Log(
            $"[RouteTilePreloader] Route bounds z={zoom} x={minX}..{maxX} y={minY}..{maxY} total={result.Count}"
        );

        return result;
    }
}
