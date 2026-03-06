using System;
using System.Collections.Generic;

/// <summary>
/// Central data store for all CDU/FMS state.
/// Pure C# class — no MonoBehaviour lifecycle. Held and updated by FmsPageRouter.
/// </summary>
public class FmsModel
{
    // ── Flight Plan ────────────────────────────────────────────────────────────
    public List<ScenarioDefinition.WaypointDef> ActiveRoute = new();
    public int ActiveLegIndex; // mirrors NavAutopilot.activeIndex
    public string OriginIdent = "";
    public string DestIdent = "";

    // ── POS INIT ───────────────────────────────────────────────────────────────
    public double FmsPosLat;
    public double FmsPosLon;
    public string AirportIdent = "";
    public string RefWptIdent = "";

    // ── Frequency (pre-populated from Scenario.txt / Scenario 01 values) ───────
    public string FreqAtis = "125.75";
    public string FreqGnd = "121.9";
    public string FreqTwr = "120.65";
    public string FreqDep = "270.8";
    public string FreqClnc = "\u2014"; // em-dash
    public string FreqRdr = "119.1";

    // ── TUNE page — standby frequencies + callsign ─────────────────────────────
    public string StandbyFreqAtis = "";
    public string StandbyFreqGnd = "";
    public string StandbyFreqTwr = "";
    public string StandbyFreqDep = "";
    public string StandbyFreqClnc = "";
    public string StandbyFreqRdr = "";
    public string ActiveCallsign = "CONGO22";

    // ── DEP/ARR approach loading ───────────────────────────────────────────────
    public bool ArrivalLoaded = false;

    // ── Performance (PERF INIT) ────────────────────────────────────────────────
    public float ZfwLbs;
    public float FuelWeightLbs;
    public float GrossWeightLbs => ZfwLbs + FuelWeightLbs;

    // ── Status ─────────────────────────────────────────────────────────────────
    public string NavDataIdent = "JB-AMER";
    public string ActiveDbRange = "19JAN26 18MAR26";
    public string SecDbRange = "22JAN26 18FEB26";
    public string ProgramId = "SCID 832-4117-142";

    // ── Live telemetry (pumped by FmsPageRouter.Update every frame) ────────────
    public float IasKt;
    public float AltFtMsl;
    public float HdgDeg;
    public float VsiFpm;
    public float BrgDeg; // bearing to active waypoint (degrees)
    public float DistM; // distance to active waypoint (meters)
    public float XtkM; // cross-track error in meters (+ve = right of track)

    // ── Scenario reference ─────────────────────────────────────────────────────
    public ScenarioDefinition Scenario { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Initialisation
    // ─────────────────────────────────────────────────────────────────────────

    public void LoadFromScenario(ScenarioDefinition sd)
    {
        if (sd == null)
            return;
        Scenario = sd;

        ActiveRoute = new List<ScenarioDefinition.WaypointDef>();
        foreach (var ident in sd.prefillRouteIdents)
        {
            var wpDef = sd.waypoints.Find(w =>
                string.Equals(w.ident, ident, StringComparison.OrdinalIgnoreCase)
            );
            if (wpDef != null)
                ActiveRoute.Add(wpDef);
        }

        OriginIdent = ActiveRoute.Count > 0 ? ActiveRoute[0].ident : "";
        DestIdent = ActiveRoute.Count > 1 ? ActiveRoute[ActiveRoute.Count - 1].ident : "";
        AirportIdent = OriginIdent;

        if (sd.waypoints.Count > 0)
        {
            FmsPosLat = sd.waypoints[0].latDeg;
            FmsPosLon = sd.waypoints[0].lonDeg;
        }

        ActiveLegIndex = 0;

        ZfwLbs = sd.zfwLbs;
        FuelWeightLbs = sd.fuelWeightLbs;

        ArrivalLoaded = false;
        ActiveCallsign = "CONGO22";
        StandbyFreqAtis = "";
        StandbyFreqGnd = "";
        StandbyFreqTwr = "";
        StandbyFreqDep = "";
        StandbyFreqClnc = "";
        StandbyFreqRdr = "";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Formatting helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns a CDU-style lat/lon string: N30°21.00 W087°19.20</summary>
    public string FormatLatLon(double lat, double lon)
    {
        char ns = lat >= 0 ? 'N' : 'S';
        char ew = lon >= 0 ? 'E' : 'W';
        double aLat = Math.Abs(lat);
        double aLon = Math.Abs(lon);
        int dLat = (int)aLat;
        double mLat = (aLat - dLat) * 60.0;
        int dLon = (int)aLon;
        double mLon = (aLon - dLon) * 60.0;
        return $"{ns}{dLat:D2}\u00B0{mLat:00.00} {ew}{dLon:D3}\u00B0{mLon:00.00}";
    }

    /// <summary>Distance to active waypoint in nautical miles.</summary>
    public float DistNm => DistM / 1852f;

    /// <summary>Sum of great-circle distances across all active route legs, in nautical miles.</summary>
    public float TotalRouteDistNm
    {
        get
        {
            float total = 0f;
            for (int i = 0; i < ActiveRoute.Count - 1; i++)
                total += HaversineNm(
                    ActiveRoute[i].latDeg,
                    ActiveRoute[i].lonDeg,
                    ActiveRoute[i + 1].latDeg,
                    ActiveRoute[i + 1].lonDeg
                );
            return total;
        }
    }

    private static float HaversineNm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 3440.065; // Earth radius in nautical miles
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(lat1 * Math.PI / 180.0)
                * Math.Cos(lat2 * Math.PI / 180.0)
                * Math.Sin(dLon / 2)
                * Math.Sin(dLon / 2);
        double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        return (float)(R * c);
    }

    /// <summary>Format ETE as mm:ss from a distance (nm) and speed (kt).</summary>
    public string FormatEte(float distNm, float speedKt)
    {
        if (speedKt < 1f)
            return "--:--";
        float totalMin = distNm / speedKt * 60f;
        int min = (int)totalMin;
        int sec = (int)((totalMin - min) * 60f);
        return $"{min:D2}:{sec:D2}";
    }
}
