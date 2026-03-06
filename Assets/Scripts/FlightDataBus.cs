using UnityEngine;

/// <summary>
/// Central read-only telemetry hub for instruments (PFD/ND/FMS UI).
/// Pulls physics truth from the aircraft Rigidbody and exposes values in training-friendly units.
///
/// Provides explicit unit-suffixed fields (preferred) AND legacy aliases (ias/alt/hdg/vsi/dist/brg)
/// to avoid breaking existing UI scripts.
/// </summary>
public class FlightDataBus : MonoBehaviour
{
    [Header("References")]
    public PlaneController plane;
    public SimTargets targets;
    public NavAutopilot nav;

    Rigidbody _rb;

    [Header("Outputs (explicit units)")]
    [Tooltip("Indicated airspeed proxy (knots). Rigidbody speed magnitude (includes vertical).")]
    public float iasKt;

    [Tooltip("Ground speed (knots). XZ-plane magnitude only.")]
    public float gsKt;

    [Tooltip("Altitude (feet MSL). Rigidbody y position (meters) converted to feet.")]
    public float altFtMsl;

    [Tooltip("Heading (degrees, 0..360).")]
    public float hdgDeg;

    [Tooltip("Track (degrees, 0..360) derived from velocity on the ground plane.")]
    public float trkDeg;

    [Tooltip("Vertical speed (feet per minute).")]
    public float vsiFpm;

    [Tooltip("Measured turn rate (degrees per second).")]
    public float turnRateDegPerSec;

    [Tooltip("Bank angle (degrees). Signed roll about Z.")]
    public float bankDeg;

    [Tooltip("NAV engaged passthrough (true when NavAutopilot is driving targets).")]
    public bool navEngaged;

    [Tooltip("Bearing to active waypoint (degrees, 0..360).")]
    public float brgDeg;

    [Tooltip("Distance to active waypoint (meters).")]
    public float distM;

    [Header("Modes (annunciations)")]
    public bool altHold;
    public bool altCapture;
    public bool vnavActive;

    [Header("Legacy Aliases (do not use for new code)")]
    public float ias => iasKt;
    public float alt => altFtMsl;
    public float hdg => hdgDeg;
    public float vsi => vsiFpm;
    public float dist => distM;
    public float brg => brgDeg;

    const float KT_PER_MS = 1.94384449f;
    const float FT_PER_M = 3.2808399f;
    const float FTMIN_PER_MS = 196.850394f;

    float _prevHdgDeg;
    bool _hdgInit;

    void Awake() => ResolveRefs();

    void OnValidate()
    {
        if (!Application.isPlaying)
            ResolveRefs();
    }

    void ResolveRefs()
    {
        if (!plane)
            plane = GetComponentInParent<PlaneController>();
        if (plane && !_rb)
            _rb = plane.GetComponent<Rigidbody>();

        if (!targets && plane)
            targets = plane.targets ? plane.targets : plane.GetComponent<SimTargets>();

        if (!nav && plane)
            nav = plane.GetComponentInParent<NavAutopilot>();
    }

    void Update()
    {
        if (!plane)
            return;
        if (!_rb)
            _rb = plane.GetComponent<Rigidbody>();
        if (!_rb)
            return;

        Vector3 v = _rb.linearVelocity;

        iasKt = v.magnitude * KT_PER_MS;

        Vector2 vXZ = new Vector2(v.x, v.z);
        gsKt = vXZ.magnitude * KT_PER_MS;

        altFtMsl = _rb.position.y * FT_PER_M;
        vsiFpm = v.y * FTMIN_PER_MS;

        float hdgNow = Mathf.Repeat(plane.transform.eulerAngles.y, 360f);
        hdgDeg = hdgNow;

        bankDeg = Mathf.DeltaAngle(0f, plane.transform.eulerAngles.z);

        if (vXZ.sqrMagnitude > 1e-4f)
        {
            float trk = Mathf.Atan2(vXZ.x, vXZ.y) * Mathf.Rad2Deg;
            trkDeg = Mathf.Repeat(trk, 360f);
        }
        else
            trkDeg = hdgDeg;

        float dt = Time.deltaTime;
        if (dt > 1e-6f)
        {
            if (!_hdgInit)
            {
                _prevHdgDeg = hdgNow;
                _hdgInit = true;
                turnRateDegPerSec = 0f;
            }
            else
            {
                float d = Mathf.DeltaAngle(_prevHdgDeg, hdgNow);
                turnRateDegPerSec = d / dt;
                _prevHdgDeg = hdgNow;
            }
        }

        navEngaged = nav ? nav.navEngaged : false;
        if (nav)
        {
            brgDeg = Mathf.Repeat(nav.activeBearing, 360f);
            distM = nav.activeDistance;
        }
        else
        {
            brgDeg = 0f;
            distM = 0f;
        }

        altHold = plane && plane.altMode == PlaneController.AltMode.Hold;
        altCapture = plane && plane.altMode == PlaneController.AltMode.Capture;
        vnavActive = plane && plane.vnavLiteEnabled && navEngaged && !altHold;
    }
}
