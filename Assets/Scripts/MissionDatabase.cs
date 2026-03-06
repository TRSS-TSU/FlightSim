using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class MissionDatabase : MonoBehaviour
{
    [Tooltip(
        "Export Coord Log.xlsx to CSV: Name,Lat,Lon (Lon is positive in file; we convert to West=-)."
    )]
    public TextAsset coordCsv;

    [Serializable]
    public class Waypoint
    {
        public string name;
        public double lat;
        public double lon;

        public Waypoint(string n, double la, double lo)
        {
            name = n;
            lat = la;
            lon = lo;
        }
    }

    public readonly Dictionary<string, Waypoint> Waypoints = new();
    public readonly List<Waypoint> Scenario1Route = new();

    // Scenario 1 route per Scenarios.pptx: KNPA->TEEZY->TRADR->BFM->VR1020 A-E->CEW->PENSI->KNPA :contentReference[oaicite:0]{index=0}
    static readonly string[] S1 =
    {
        "KNPA",
        "TEEZY",
        "TRADR",
        "BFM",
        "VR1020 Pt A",
        "VR1020 Pt B",
        "VR1020 Pt C",
        "VR1020 Pt D",
        "VR1020 Pt E",
        "CEW",
        "PENSI",
        "KNPA",
    };

    void Awake()
    {
        ParseWaypoints(coordCsv);
        BuildScenario1Route();
    }

    public void ParseWaypoints(TextAsset csv)
    {
        Waypoints.Clear();
        var lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++) // skip header
        {
            var c = lines[i].Split(',');
            if (c.Length < 3)
                continue;
            var name = c[0].Trim();
            if (
                string.IsNullOrEmpty(name)
                || name.StartsWith("Scenario", StringComparison.OrdinalIgnoreCase)
            )
                continue;

            if (
                !double.TryParse(
                    c[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var lat
                )
            )
                continue;
            if (
                !double.TryParse(
                    c[2],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var lonAbs
                )
            )
                continue;

            Waypoints[name] = new Waypoint(name, lat, -Math.Abs(lonAbs)); // West -> negative
        }
    }

    void BuildScenario1Route()
    {
        Scenario1Route.Clear();
        foreach (var n in S1)
            if (Waypoints.TryGetValue(n, out var wp))
                Scenario1Route.Add(wp);
            else
                Debug.LogWarning($"Missing waypoint in DB: {n}");
    }
}
