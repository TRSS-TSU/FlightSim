using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RouteTileAuditLogger : MonoBehaviour
{
    public LocalTileGrid tileGrid;

    public bool writeFiles = true;

    public void AuditRoute(
        ScenarioDefinition scenario,
        IReadOnlyList<ScenarioDefinition.WaypointDef> routeWaypoints,
        int zoom
    )
    {
        if (!scenario || routeWaypoints == null || routeWaypoints.Count == 0)
        {
            Debug.LogWarning("[RouteTileAudit] Aborted: missing scenario or route.");
            return;
        }

        string folder = tileGrid ? tileGrid.tilesFolder : "tiles_nd_dark_v1";

        int padding = Mathf.Max(0, scenario.preloadPaddingTiles);
        List<Vector2Int> required = RouteTilePreloader.BuildTileIndexList(
            routeWaypoints,
            zoom,
            padding
        );

        var found = new List<string>();
        var missing = new List<string>();

        foreach (Vector2Int tile in required)
        {
            string rel = $"{folder}/{zoom}/{tile.x}/{tile.y}.png";
            string full = Path.Combine(
                Application.streamingAssetsPath,
                folder,
                zoom.ToString(),
                tile.x.ToString(),
                tile.y + ".png"
            );

            if (File.Exists(full))
                found.Add(rel);
            else
                missing.Add(rel);
        }

        Debug.Log(
            $"[RouteTileAudit] z={zoom} required={required.Count} found={found.Count} missing={missing.Count}"
        );

        if (!writeFiles)
            return;

        string outDir = Path.Combine(Application.dataPath, "../TileAudit");
        Directory.CreateDirectory(outDir);

        File.WriteAllLines(
            Path.Combine(outDir, $"required_tiles_z{zoom}.txt"),
            ToLines(folder, zoom, required)
        );
        File.WriteAllLines(Path.Combine(outDir, $"found_tiles_z{zoom}.txt"), found);
        File.WriteAllLines(Path.Combine(outDir, $"missing_tiles_z{zoom}.txt"), missing);

        Debug.Log($"[RouteTileAudit] Wrote audit files to: {outDir}");
    }

    private static IEnumerable<string> ToLines(string folder, int zoom, List<Vector2Int> tiles)
    {
        foreach (Vector2Int t in tiles)
            yield return $"{folder}/{zoom}/{t.x}/{t.y}.png";
    }
}
