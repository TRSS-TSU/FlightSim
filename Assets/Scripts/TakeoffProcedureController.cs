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
    public bool engageNavAfterProcedure = true;
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

    [Header("Departure Handoff")]
    public float navHandoffMinAltitudeFt = 2500f;
    public int navHandoffRouteIndex = 1;
    public bool allowHandoffAfterDepartureGate = true;

    public bool BeginTakeoff()
    {
        if (isRunning)
        {
            if (logProcedure)
                Debug.LogWarning(
                    "[TakeoffProcedure] BeginTakeoff ignored: procedure already running."
                );
            return false;
        }

        if (!targets)
        {
            Debug.LogWarning("[TakeoffProcedure] Cannot begin: SimTargets is not assigned.");
            return false;
        }

        if (nav)
            nav.SetNavEngaged(false);

        routine = StartCoroutine(TakeoffRoutine());
        return true;
    }

    private IEnumerator TakeoffRoutine()
    {
        isRunning = true;

        ScenarioDefinition scenario = ScenarioRuntime.Current;

        Vector3 thresholdPos;
        Vector3 departurePos;
        ScenarioDefinition.WaypointDef thresholdDef;
        ScenarioDefinition.WaypointDef departureDef;

        bool hasThreshold = TryFindTakeoffWaypointWorldPosition(
            scenario,
            runwayThresholdIdent,
            out thresholdDef,
            out thresholdPos
        );

        bool hasDeparture = TryFindTakeoffWaypointWorldPosition(
            scenario,
            departureIdent,
            out departureDef,
            out departurePos
        );

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

        // Stage 2: liftoff / gentle initial climb.
        SetTargets(
            ResolveScenarioIas(thresholdDef, liftoffIasKt),
            ResolveScenarioAltitude(thresholdDef, liftoffAltFt),
            ResolveScenarioHeading(thresholdDef, runwayHeadingDeg)
        );

        yield return WaitStage(liftoffSeconds);

        // Stage 2b: climb straight ahead until the departure gate and altitude handoff are satisfied.
        SetTargets(
            climbIasKt,
            ResolveScenarioAltitude(departureDef, departureAltFt),
            runwayHeadingDeg
        );

        if (hasDeparture)
        {
            yield return WaitUntilDepartureHandoff(
                departurePos,
                departureGateRadiusM,
                maxGateWaitSeconds
            );
        }
        else
            yield return WaitStage(climbSeconds);

        SetTargets(
            departureIasKt,
            ResolveScenarioAltitude(departureDef, departureAltFt),
            departureHeadingDeg
        );

        if (engageNavAfterProcedure && nav)
        {
            PrepareNavForDepartureHandoff();
            nav.SetNavEngaged(true);

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

    }

    private static float NormalizeHeading(float hdg)
    {
        hdg %= 360f;
        return hdg < 0f ? hdg + 360f : hdg;
    }

    private bool TryFindTakeoffWaypointWorldPosition(
        ScenarioDefinition scenario,
        string ident,
        out ScenarioDefinition.WaypointDef wp,
        out Vector3 worldPos
    )
    {
        wp = null;
        worldPos = default;

        if (!scenario || scenario.waypoints == null || string.IsNullOrWhiteSpace(ident))
            return false;

        wp = scenario.waypoints.Find(w =>
            w != null
            && w.role == ScenarioDefinition.WaypointRole.Takeoff
            && string.Equals(w.ident, ident, System.StringComparison.OrdinalIgnoreCase)
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

    private float ResolveScenarioAltitude(ScenarioDefinition.WaypointDef wp, float fallbackFt)
    {
        return wp != null && wp.targetAltFtMsl > 0f ? wp.targetAltFtMsl : fallbackFt;
    }

    private float ResolveScenarioHeading(ScenarioDefinition.WaypointDef wp, float fallbackDeg)
    {
        return wp != null && wp.targetHdgDeg > 0f ? wp.targetHdgDeg : fallbackDeg;
    }

    private float ResolveScenarioIas(ScenarioDefinition.WaypointDef wp, float fallbackKt)
    {
        if (wp == null || wp.targetIasKt <= 0f)
            return fallbackKt;

        return wp.targetIasKt <= targets.maxIasKt ? wp.targetIasKt : fallbackKt;
    }

    private IEnumerator WaitUntilDepartureHandoff(
        Vector3 waypointWorldPos,
        float gateRadiusM,
        float timeoutSeconds
    )
    {
        if (!aircraft)
            yield break;

        float startTime = Time.time;
        float radius = Mathf.Max(1f, gateRadiusM);
        bool reachedDepartureGate = false;

        while (Time.time - startTime < timeoutSeconds)
        {
            float dist = FlatDistanceTo(waypointWorldPos);
            float altitudeFt = aircraft.position.y * 3.2808399f;

            if (dist <= radius)
                reachedDepartureGate = true;

            bool inGateNow = dist <= radius;
            bool altitudeReady = altitudeFt >= navHandoffMinAltitudeFt;

            bool gateReady = inGateNow || (allowHandoffAfterDepartureGate && reachedDepartureGate);

            if (gateReady && altitudeReady)
            {
                yield break;
            }

            yield return new WaitForSeconds(0.25f);
        }

        Debug.LogWarning(
            "[TakeoffProcedure] Departure handoff wait timed out. Continuing procedure."
        );
    }

    private float FlatDistanceTo(Vector3 waypointWorldPos)
    {
        Vector3 aircraftFlat = Vector3.ProjectOnPlane(aircraft.position, Vector3.up);
        Vector3 waypointFlat = Vector3.ProjectOnPlane(waypointWorldPos, Vector3.up);
        return Vector3.Distance(aircraftFlat, waypointFlat);
    }

    private void PrepareNavForDepartureHandoff()
    {
        if (!nav || !nav.plan || nav.plan.waypoints == null || nav.plan.waypoints.Length == 0)
            return;

        nav.activeIndex = Mathf.Clamp(navHandoffRouteIndex, 0, nav.plan.waypoints.Length - 1);
        nav.ResetCaptureState();
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

            if (dist <= radius)
                yield break;

            yield return new WaitForSeconds(0.25f);
        }

        Debug.LogWarning("[TakeoffProcedure] Gate wait timed out. Continuing procedure.");
    }
}
