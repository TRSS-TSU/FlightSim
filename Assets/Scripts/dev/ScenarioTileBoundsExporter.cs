using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ScenarioTileBoundsExporter
{
    const double MercatorMaxLat = 85.05112878;
    const double MPerDegLat = 111320.0;
    const double NmToM = 1852.0;

    [MenuItem("FMS/Export Scenario Tile Ranges (z12-14)")]
    public static void Export()
    {
        var guids = AssetDatabase.FindAssets("t:ScenarioDefinition");
        var list = new List<object>();

        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var s = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(path);
            if (!s)
                continue;

            double minLat = s.centerLatDeg,
                maxLat = s.centerLatDeg;
            double minLon = s.centerLonDeg,
                maxLon = s.centerLonDeg;

            foreach (var w in s.waypoints ?? new List<ScenarioDefinition.WaypointDef>())
            {
                minLat = Math.Min(minLat, w.latDeg);
                maxLat = Math.Max(maxLat, w.latDeg);
                minLon = Math.Min(minLon, w.lonDeg);
                maxLon = Math.Max(maxLon, w.lonDeg);
            }

            // +20NM padding
            double padM = 20.0 * NmToM;
            double midLat = (minLat + maxLat) * 0.5;
            double dLat = padM / MPerDegLat;
            double dLon = padM / (MPerDegLat * Math.Cos(midLat * Math.PI / 180.0));

            minLat = ClampLat(minLat - dLat);
            maxLat = ClampLat(maxLat + dLat);
            minLon -= dLon;
            maxLon += dLon;

            var zoomRanges = new Dictionary<int, object>();
            for (int z = 12; z <= 14; z++)
            {
                var (xMin, yMax) = LonLatToTile(minLon, minLat, z);
                var (xMax, yMin) = LonLatToTile(maxLon, maxLat, z);
                zoomRanges[z] = new
                {
                    xMin,
                    xMax,
                    yMin,
                    yMax,
                };
            }

            list.Add(
                new
                {
                    title = s.scenarioTitle,
                    assetPath = path,
                    bbox = new
                    {
                        minLat,
                        minLon,
                        maxLat,
                        maxLon,
                    },
                    zoom = zoomRanges,
                }
            );
        }

        var json = JsonUtility.ToJson(
            new Wrapper { items = list.Select(o => o.ToString()).ToArray() },
            true
        );
        // JsonUtility can't serialize anonymous objects well; write a simple line-based file instead:
        var outDir = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Exports");
        Directory.CreateDirectory(outDir);
        var outPath = Path.Combine(outDir, "scenario_tile_ranges.txt");
        File.WriteAllText(outPath, PrettyPrint(list));
        Debug.Log($"Wrote: {outPath}");
    }

    static string PrettyPrint(List<object> items) =>
        JsonUtility.ToJson(
            new TextWrap { lines = items.Select(i => i.ToString()).ToArray() },
            true
        );

    [Serializable]
    class TextWrap
    {
        public string[] lines;
    }

    [Serializable]
    class Wrapper
    {
        public string[] items;
    }

    static double ClampLat(double lat) => Math.Max(-MercatorMaxLat, Math.Min(MercatorMaxLat, lat));

    static (int x, int y) LonLatToTile(double lon, double lat, int z)
    {
        double n = Math.Pow(2.0, z);
        double latRad = lat * Math.PI / 180.0;
        int x = (int)Math.Floor((lon + 180.0) / 360.0 * n);
        int y = (int)
            Math.Floor(
                (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n
            );
        return (x, y);
    }
}
