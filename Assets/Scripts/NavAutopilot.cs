using UnityEngine;

public class NavAutopilot : MonoBehaviour
{
    public event System.Action<string> WaypointSequenced;
    [HideInInspector]
    public float activeDistance;

    [HideInInspector]
    public float activeBearing;

    [HideInInspector]
    public float xtkM; // cross-track error in meters (+ = right of track)

    public FlightPlan plan;
    public SimTargets targets;
    public Transform aircraft;

    [Header("Nav")]
    public int activeIndex = 0;
    public float captureRadius = 60f; // meters
    public bool loop = false;

    [Header("Look-Ahead LNAV")]
    [Tooltip("Time horizon for the leg look-ahead point (seconds). Larger = smoother at high speed.")]
    public float lookAheadSec = 30f;

    [Tooltip("Minimum look-ahead distance regardless of speed (meters).")]
    public float lookAheadMinM = 300f;

    Rigidbody _rb; // cached in Awake; avoids GetComponent every FixedUpdate

    [Header("Mode")]
    public bool navEngaged = false; // when true, NAV drives targetHeading

    [Header("Capture Robustness (v1)")]
    public float nearRadiusMultiplier = 1.25f; // 150 * 1.25 = 187.5m "near"
    public int minNearFrames = 3; // require a few frames near before capturing

    float prevDist = float.PositiveInfinity;
    bool wasNear = false;
    int nearFrames = 0;

    [Header("Advance Debounce")]
    public float advanceCooldownSec = 0.5f;
    float advanceCooldownT = 0f;

    [Header("Turn Anticipation")]
    [Tooltip(
        "Enable turn anticipation (lead distance) so NAV begins the next leg before the waypoint on course changes."
    )]
    public bool enableTurnAnticipation = true;

    [Tooltip(
        "Bank angle used for lead-distance computation (deg). Should match PlaneController maxBankDeg for realism."
    )]
    public float anticipationBankDeg = 25f;

    [Tooltip("Ignore tiny course changes below this value (deg).")]
    public float minCourseChangeDeg = 10f;

    [Tooltip("Clamp lead distance to avoid huge anticipations at high speed (meters). Set ~6000 for 240 kt scenarios.")]
    public float maxLeadDistanceM = 6000f;

    static Vector3 Flat(Vector3 v) => Vector3.ProjectOnPlane(v, Vector3.up);

    void Awake()
    {
        if (!plan)
            plan = GetComponent<FlightPlan>();
        if (aircraft)
            _rb = aircraft.GetComponent<Rigidbody>();
        if (!_rb)
            _rb = GetComponent<Rigidbody>();
    }

    void OnEnable()  => ScenarioRuntime.OnChanged += OnScenarioChanged;
    void OnDisable() => ScenarioRuntime.OnChanged -= OnScenarioChanged;

    void OnScenarioChanged(ScenarioDefinition sd)
    {
        // Defer index reset until FlightPlan has built the new waypoints
        if (plan)
            plan.OnRouteBuilt += OnRouteReady;
    }

    void OnRouteReady()
    {
        if (plan) plan.OnRouteBuilt -= OnRouteReady; // one-shot
        activeIndex = 0;
        ResetCaptureState();
    }

    void FixedUpdate()
    {
        if (!plan || plan.waypoints == null || plan.waypoints.Length == 0)
        {
            // Freeze heading to current aircraft yaw so no stale intercept is held
            if (navEngaged && targets && aircraft)
                targets.targetHdgDeg = aircraft.eulerAngles.y;
            return;
        }
        if (!targets || !aircraft)
            return;

        xtkM = 0f;

        if (advanceCooldownT > 0f)
            advanceCooldownT -= Time.fixedDeltaTime;
        activeIndex = Mathf.Clamp(activeIndex, 0, plan.waypoints.Length - 1);
        Transform wp = plan.waypoints[activeIndex];

        Vector3 P = Flat(aircraft.position);
        Vector3 B = Flat(wp.position);

        Vector3 toWp = B - P;
        float dist = toWp.magnitude;

        float nearRadius = captureRadius * nearRadiusMultiplier;

        if (dist <= nearRadius)
        {
            wasNear = true;
            nearFrames++;
        }

        float bearingToWp = Mathf.Atan2(toWp.x, toWp.z) * Mathf.Rad2Deg;
        bearingToWp = (bearingToWp + 360f) % 360f;

        float desiredHeading = bearingToWp; // DIRECT-TO by default (index 0)

        if (activeIndex > 0)
        {
            Vector3 A  = Flat(plan.waypoints[activeIndex - 1].position);
            Vector3 AB = B - A;
            if (AB.sqrMagnitude > 1f)
            {
                Vector3 ABn = AB.normalized;
                Vector3 AP  = P - A;

                // XTK — signed cross-track error in meters (+ = right of track); for telemetry only
                xtkM = Vector3.Cross(ABn, AP).y;

                // Look-ahead LNAV: steer toward a point on the leg centerline that is
                // (lookAheadSec * groundSpeed) meters ahead of the aircraft's along-track
                // projection. This naturally reduces the intercept angle at high speed,
                // preventing the S-turn oscillation caused by a fixed XTK gain.
                float gsMs      = _rb
                    ? new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.z).magnitude
                    : 30f;
                float lookAheadM = Mathf.Max(lookAheadMinM, gsMs * lookAheadSec);

                float   along  = Vector3.Dot(AP, ABn);               // along-track progress
                Vector3 proj   = A + ABn * Mathf.Max(0f, along);     // nearest on-leg point (no backtrack)
                Vector3 target = proj + ABn * lookAheadM;            // look-ahead point on leg

                Vector3 toTarget = target - P;
                desiredHeading   = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                desiredHeading   = (desiredHeading + 360f) % 360f;
            }
        }

        if (navEngaged)
            targets.targetHdgDeg = desiredHeading;

        // ND-friendly outputs: distance + bearing to active waypoint
        activeDistance = dist;
        activeBearing = bearingToWp;

        // Passed-waypoint detection (robust at high speed / tight turns):
        // For leg A->B, if (B-P) · (B-A)^  < 0, we are beyond the plane through B perpendicular to the leg.
        bool passedWaypoint = false;
        if (activeIndex > 0)
        {
            Vector3 A2 = Flat(plan.waypoints[activeIndex - 1].position);
            Vector3 AB2 = B - A2;
            if (AB2.sqrMagnitude > 1f)
            {
                Vector3 ABn2 = AB2.normalized;
                passedWaypoint = Vector3.Dot(toWp, ABn2) < 0f; // toWp = B - P
            }
        }

        // Turn anticipation (lead distance): if a course change is coming, begin switching to the next leg
        // before reaching the waypoint. Lead distance:
        //   R = V^2 / (g * tan(phi_max))
        //   lead = R * tan(deltaChi/2)
        bool anticipateAdvance = false;
        float leadDistanceM = 0f;
        if (
            enableTurnAnticipation
            && advanceCooldownT <= 0f
            && activeIndex > 0
            && activeIndex < plan.waypoints.Length - 1
        )
        {
            Vector3 A3 = Flat(plan.waypoints[activeIndex - 1].position);
            Vector3 B3 = B; // active waypoint
            Vector3 C3 = Flat(plan.waypoints[activeIndex + 1].position);

            Vector3 inbound = (B3 - A3);
            Vector3 outbound = (C3 - B3);

            if (inbound.sqrMagnitude > 1f && outbound.sqrMagnitude > 1f)
            {
                inbound.Normalize();
                outbound.Normalize();

                // Course change angle (0..180)
                float deltaChi = Vector3.Angle(inbound, outbound);

                if (deltaChi >= minCourseChangeDeg)
                {
                    Vector3 v = _rb ? _rb.linearVelocity : Vector3.zero;
                    float gs = new Vector2(v.x, v.z).magnitude;

                    // If Rigidbody isn't present or speed is tiny, fall back to "no anticipation"
                    if (gs > 0.5f)
                    {
                        float phi = Mathf.Max(1f, anticipationBankDeg) * Mathf.Deg2Rad; // prevent tan(0)
                        float R = (gs * gs) / (9.81f * Mathf.Tan(phi));
                        leadDistanceM = Mathf.Min(
                            maxLeadDistanceM,
                            R * Mathf.Tan(0.5f * deltaChi * Mathf.Deg2Rad)
                        );

                        // Switch early when within lead distance of the waypoint
                        anticipateAdvance = dist <= leadDistanceM;
                    }
                }
            }
        }

        bool movingAwayAfterNear = wasNear && nearFrames >= minNearFrames && dist > prevDist;
        bool inRadius = dist <= captureRadius;

        if (
            (inRadius || movingAwayAfterNear || passedWaypoint || anticipateAdvance)
            && advanceCooldownT <= 0f
        )
        {
            string sequencedIdent = wp ? wp.name.Replace("WP_", "") : "";
            int previousIndex = activeIndex;
            int nextIndex = activeIndex + 1;
            if (nextIndex >= plan.waypoints.Length)
                nextIndex = loop ? 0 : plan.waypoints.Length - 1;

            if (nextIndex == previousIndex)
            {
                prevDist = dist;
                return;
            }

            activeIndex = nextIndex;

            wasNear = false;
            nearFrames = 0;
            prevDist = float.PositiveInfinity;
            advanceCooldownT = advanceCooldownSec;
            WaypointSequenced?.Invoke(sequencedIdent);
        }
        else
        {
            prevDist = dist;
        }

        Debug.DrawLine(aircraft.position, wp.position, Color.black);
    }

    public void SetNavEngaged(bool on)
    {
        navEngaged = on;

        // When disengaging NAV, freeze the target to current heading
        // so we don’t “snap back” to some old value.
        if (!navEngaged && targets && aircraft)
            targets.targetHdgDeg = aircraft.eulerAngles.y;
    }

    public void ToggleNav() => SetNavEngaged(!navEngaged);

    public void ResetCaptureState()
    {
        wasNear = false;
        nearFrames = 0;
        prevDist = float.PositiveInfinity;
        advanceCooldownT = 0f;
    }

    /// <summary>
    /// After a route rebuild, find the nav index that best preserves flight continuity.
    ///
    /// Strategy:
    ///   1. Search new waypoints[] for a GameObject named "WP_{priorTargetIdent}".
    ///      This handles inserts/deletes anywhere in the route and Direct-To.
    ///   2. If not found (waypoint deleted or full route replacement), fall back to
    ///      the nearest waypoint that lies ahead of the aircraft (forward-cone dot test).
    ///   3. If nothing is ahead, pick the nearest waypoint overall.
    ///
    /// Call this immediately after FlightPlan.RebuildRoute(), then call ResetCaptureState().
    /// </summary>
    public int FindBestLegIndex(string priorTargetIdent)
    {
        if (plan == null || plan.waypoints == null || plan.waypoints.Length == 0)
            return 0;

        // 1. Ident match — waypoints are named "WP_{ident}" by FlightPlan.RebuildRoute
        if (!string.IsNullOrEmpty(priorTargetIdent))
        {
            string searchName = "WP_" + priorTargetIdent;
            for (int i = 0; i < plan.waypoints.Length; i++)
            {
                if (plan.waypoints[i] &&
                    string.Equals(plan.waypoints[i].name, searchName,
                        System.StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }

        // 2. Nearest-ahead fallback (prior ident gone — use aircraft position)
        if (!aircraft)
            return 0;

        Vector3 P   = Vector3.ProjectOnPlane(aircraft.position, Vector3.up);
        Vector3 fwd = Vector3.ProjectOnPlane(aircraft.forward,  Vector3.up);
        bool hasFwd = fwd.sqrMagnitude > 0.001f;
        if (hasFwd) fwd.Normalize();

        int   bestIdx   = 0;
        float bestDist  = float.MaxValue;
        bool  foundAhead = false;

        for (int i = 0; i < plan.waypoints.Length; i++)
        {
            if (!plan.waypoints[i]) continue;
            Vector3 toWp = Vector3.ProjectOnPlane(plan.waypoints[i].position, Vector3.up) - P;
            float   dist = toWp.magnitude;
            bool    ahead = hasFwd && Vector3.Dot(fwd, toWp) > 0f;

            if (!foundAhead)
            {
                if (ahead)  { foundAhead = true; bestIdx = i; bestDist = dist; }
                else if (dist < bestDist) { bestIdx = i; bestDist = dist; }
            }
            else if (ahead && dist < bestDist)
            {
                bestIdx  = i;
                bestDist = dist;
            }
        }

        return bestIdx;
    }
}
