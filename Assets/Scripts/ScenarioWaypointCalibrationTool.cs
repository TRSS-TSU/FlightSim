using UnityEngine;

public class ScenarioWaypointCalibrationTool : MonoBehaviour
{
    public FlightPlan flightPlan;

    [Header("Manual runway calibration markers")]
    public Transform eor25rStartMarker;
    public Transform rw25rThresholdMarker;

    [ContextMenu("Log Calibrated Runway LatLon")]
    public void LogCalibratedRunwayLatLon()
    {
        ScenarioDefinition scenario = ScenarioRuntime.Current;

        if (!scenario)
        {
            Debug.LogWarning("[ScenarioWaypointCalibration] No ScenarioRuntime.Current.");
            return;
        }

        if (!flightPlan)
        {
            Debug.LogWarning("[ScenarioWaypointCalibration] FlightPlan is not assigned.");
            return;
        }

        LogMarker("KNPA_EOR25R_START", eor25rStartMarker, scenario);
        LogMarker("KNPA_RW25R_THRESH", rw25rThresholdMarker, scenario);
    }

    private void LogMarker(string ident, Transform marker, ScenarioDefinition scenario)
    {
        if (!marker)
        {
            Debug.LogWarning($"[ScenarioWaypointCalibration] Marker missing for {ident}.");
            return;
        }

        if (
            !flightPlan.TryWorldPositionToLatLon(
                scenario,
                marker.position,
                out double lat,
                out double lon
            )
        )
        {
            Debug.LogWarning($"[ScenarioWaypointCalibration] Failed to convert {ident}.");
            return;
        }
    }
}
