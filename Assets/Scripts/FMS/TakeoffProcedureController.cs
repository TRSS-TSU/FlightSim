using System.Collections;
using UnityEngine;

public class TakeoffProcedureController : MonoBehaviour
{
    [Header("References")]
    public SimTargets targets;
    public NavAutopilot nav;
    public Transform aircraft;

    [Header("Runway / Departure Headings")]
    public float runwayHeadingDeg = 250f;
    public float departureHeadingDeg = 220f;

    [Header("Stage Targets")]
    public float rollIasKt = 60f;
    public float liftoffIasKt = 110f;
    public float climbIasKt = 130f;
    public float departureIasKt = 150f;

    public float runwayAltFt = 0f;
    public float liftoffAltFt = 250f;
    public float departureAltFt = 3000f;

    [Header("Stage Timing")]
    public float rollSeconds = 4f;
    public float liftoffSeconds = 5f;
    public float climbSeconds = 8f;
    public float departureTurnSeconds = 5f;

    [Header("Options")]
    public bool engageNavAfterProcedure = false;
    public bool logProcedure = true;

    Coroutine routine;
    bool isRunning;

    public bool IsRunning => isRunning;

    [Header("Waypoint Gates")]
    public FlightPlan flightPlan;
    public float thresholdGateRadiusM = 10f;
    public float departureGateRadiusM = 20f;
    public float maxGateWaitSeconds = 180f;

    public string runwayThresholdIdent = "KNPA_RW25R_THRESH";
    public string departureIdent = "KNPA_DEP_1DME";

    public void BeginTakeoff()
    {
        if (isRunning)
        {
            if (logProcedure)
                Debug.LogWarning(
                    "[TakeoffProcedure] BeginTakeoff ignored: procedure already running."
                );
            return;
        }

        if (!targets)
        {
            Debug.LogWarning("[TakeoffProcedure] Cannot begin: SimTargets is not assigned.");
            return;
        }

        if (nav)
            nav.SetNavEngaged(false);

        routine = StartCoroutine(TakeoffRoutine());
    }

    private IEnumerator TakeoffRoutine()
    {
        isRunning = true;

        ScenarioDefinition scenario = ScenarioRuntime.Current;

        Vector3 thresholdPos;
        Vector3 departurePos;

        bool hasThreshold = TryFindScenarioWaypointWorldPosition(
            scenario,
            runwayThresholdIdent,
            out thresholdPos
        );

        bool hasDeparture = TryFindScenarioWaypointWorldPosition(
            scenario,
            departureIdent,
            out departurePos
        );

        if (logProcedure)
        {
            Debug.Log(
                $"[TakeoffProcedure] Begin takeoff roll. "
                    + $"thresholdFound={hasThreshold} departureFound={hasDeparture}"
            );
        }

        // Stage 1: slow roll along runway heading.
        SetTargets(rollIasKt, runwayAltFt, runwayHeadingDeg);

        if (hasThreshold)
            yield return WaitUntilNearWaypoint(
                thresholdPos,
                thresholdGateRadiusM,
                maxGateWaitSeconds
            );
        else
            yield return WaitStage(rollSeconds);

        if (logProcedure)
            Debug.Log("[TakeoffProcedure] Threshold gate reached. Liftoff stage.");

        // Stage 2: liftoff / gentle initial climb.
        SetTargets(liftoffIasKt, liftoffAltFt, runwayHeadingDeg);

        yield return WaitStage(liftoffSeconds);

        // Stage 2b: climb straight ahead until the departure gate is reached.
        SetTargets(climbIasKt, departureAltFt, runwayHeadingDeg);

        if (hasDeparture)
            yield return WaitUntilNearWaypoint(
                departurePos,
                departureGateRadiusM,
                maxGateWaitSeconds
            );
        else
            yield return WaitStage(climbSeconds);

        if (logProcedure)
            Debug.Log("[TakeoffProcedure] Departure gate reached. Turn heading 220.");

        // Stage 3: departure turn.
        SetTargets(departureIasKt, departureAltFt, departureHeadingDeg);
        yield return WaitStage(departureTurnSeconds);

        if (engageNavAfterProcedure && nav)
        {
            nav.SetNavEngaged(true);

            if (logProcedure)
                Debug.Log("[TakeoffProcedure] NAV engaged. Route handoff complete.");
        }

        isRunning = false;
        routine = null;
    }

    private IEnumerator WaitStage(float seconds)
    {
        float endTime = Time.time + Mathf.Max(0f, seconds);

        while (Time.time < endTime)
            yield return null;
    }

    private void SetTargets(float iasKt, float altFt, float hdgDeg)
    {
        targets.targetIasKt = Mathf.Max(0f, iasKt);
        targets.targetAltFtMsl = Mathf.Max(0f, altFt);
        targets.targetHdgDeg = NormalizeHeading(hdgDeg);

        if (logProcedure)
        {
            Debug.Log(
                $"[TakeoffProcedure] Targets: IAS={targets.targetIasKt:F0} "
                    + $"ALT={targets.targetAltFtMsl:F0} HDG={targets.targetHdgDeg:F0}"
            );
        }
    }

    private static float NormalizeHeading(float hdg)
    {
        hdg %= 360f;
        return hdg < 0f ? hdg + 360f : hdg;
    }

    private bool TryFindScenarioWaypointWorldPosition(
        ScenarioDefinition scenario,
        string ident,
        out Vector3 worldPos
    )
    {
        worldPos = default;

        if (!scenario || scenario.waypoints == null || string.IsNullOrWhiteSpace(ident))
            return false;

        ScenarioDefinition.WaypointDef wp = scenario.waypoints.Find(w =>
            w != null && string.Equals(w.ident, ident, System.StringComparison.OrdinalIgnoreCase)
        );

        if (wp == null)
            return false;

        if (!flightPlan)
        {
            Debug.LogWarning("[TakeoffProcedure] FlightPlan is not assigned.");
            return false;
        }

        return flightPlan.TryGetWaypointWorldPosition(scenario, wp, out worldPos);
    }

    private IEnumerator WaitUntilNearWaypoint(
        Vector3 waypointWorldPos,
        float gateRadiusM,
        float timeoutSeconds
    )
    {
        if (!aircraft)
            yield break;

        float startTime = Time.time;
        float radius = Mathf.Max(1f, gateRadiusM);

        while (Time.time - startTime < timeoutSeconds)
        {
            Vector3 aircraftFlat = Vector3.ProjectOnPlane(aircraft.position, Vector3.up);
            Vector3 waypointFlat = Vector3.ProjectOnPlane(waypointWorldPos, Vector3.up);

            float dist = Vector3.Distance(aircraftFlat, waypointFlat);

            if (logProcedure)
            {
                Debug.Log($"[TakeoffProcedure] Gate distance: {dist:F1}m / radius={radius:F1}m");
            }

            if (dist <= radius)
                yield break;

            yield return new WaitForSeconds(0.25f);
        }

        Debug.LogWarning("[TakeoffProcedure] Gate wait timed out. Continuing procedure.");
    }
}
