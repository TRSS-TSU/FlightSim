using UnityEngine;

public class NavAutopilot : MonoBehaviour
{
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

    public float forwardConeDeg = 60f; // must be within +/- this angle to count as "ahead"

    public float maxInterceptDeg = 30f;
    public float xtkToInterceptDeg = 0.02f; // deg per meter (tune)

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

    [Tooltip("Clamp lead distance to avoid huge anticipations at high speed (meters).")]
    public float maxLeadDistanceM = 2500f;

    static Vector3 Flat(Vector3 v) => Vector3.ProjectOnPlane(v, Vector3.up);

    void Awake()
    {
        if (!plan)
            plan = GetComponent<FlightPlan>();
    }

    void FixedUpdate()
    {
        if (!plan || plan.waypoints == null || plan.waypoints.Length == 0)
            return;
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

        if (plan == null || plan.waypoints == null || plan.waypoints.Length == 0)
            return;

        float bearingToWp = Mathf.Atan2(toWp.x, toWp.z) * Mathf.Rad2Deg;
        bearingToWp = (bearingToWp + 360f) % 360f;

        float desiredHeading = bearingToWp; // DIRECT-TO by default

        if (activeIndex > 0)
        {
            Vector3 A = plan.waypoints[activeIndex - 1].position;
            A.y = 0f;
            Vector3 AB = B - A;
            if (AB.sqrMagnitude > 1f)
            {
                Vector3 ABn = AB.normalized;
                Vector3 AP = P - A;
                float xtk = Vector3.Cross(ABn, AP).y;
                xtkM = xtk;

                float courseHdg = Mathf.Atan2(ABn.x, ABn.z) * Mathf.Rad2Deg;
                courseHdg = (courseHdg + 360f) % 360f;

                float intercept = Mathf.Clamp(
                    xtk * xtkToInterceptDeg,
                    -maxInterceptDeg,
                    maxInterceptDeg
                );
                desiredHeading = (courseHdg - intercept + 360f) % 360f;
            }
        }

        if (navEngaged)
            targets.targetHdgDeg = desiredHeading;

        // ND-friendly outputs: distance + bearing to active waypoint
        activeDistance = dist;
        activeBearing = bearingToWp;

        // Smart capture (use direction to waypoint, not desiredHeading)
        Vector3 forward = Flat(aircraft.forward).normalized;

        Vector3 dirToWp = (dist > 0.001f) ? (toWp / dist) : forward;

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
                    var rbTemp = aircraft.GetComponent<Rigidbody>();
                    Vector3 v = rbTemp ? rbTemp.linearVelocity : Vector3.zero;
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
            activeIndex++;
            if (activeIndex >= plan.waypoints.Length)
                activeIndex = loop ? 0 : plan.waypoints.Length - 1;

            wasNear = false;
            nearFrames = 0;
            prevDist = float.PositiveInfinity;
            advanceCooldownT = advanceCooldownSec;
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
}
