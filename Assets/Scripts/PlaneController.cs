using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlaneController : MonoBehaviour
{
    [Header("References")]
    public SimTargets targets;
    public NavAutopilot nav;

    Rigidbody rb;

    [Header("Speed Hold")]
    [Tooltip("Proportional gain that turns speed error into commanded acceleration.")]
    public float speedResponse = 1.5f; // 1/s

    [Tooltip("Maximum forward acceleration/deceleration (m/s^2).")]
    public float maxAccel = 5f; // m/s^2

    [Header("Altitude Hold")]
    [Tooltip("Proportional gain that turns altitude error into commanded vertical speed.")]
    public float altitudeResponse = 1.5f; // 1/s

    [Tooltip("Maximum climb/descent rate (m/s).")]
    public float maxClimbRate = 15f; // m/s

    [Tooltip("Altitude capture band in meters (~3m ≈ 10ft). Inside this band, Vy command is 0.")]
    public float altitudeCaptureBandM = 3f;

    [Tooltip("Maximum change rate of commanded vertical speed (m/s^2).")]
    public float maxVyAccel = 3f; // m/s^2
    float vyCmdSmoothed = 0f;

    [Header("Altitude Init")]
    public bool useInitialHoldAltitude = true;
    public float initialHoldAltFtMsl = 3000f;
    const float M_TO_FT = 3.2808399f; // 1 / 0.3048

    public enum AltMode
    {
        Hold,
        Capture,
    }

    [Header("Altitude Mode")]
    [Tooltip("When within this error (ft), declare altitude captured and hold.")]
    public float altHoldToleranceFt = 5f;
    public AltMode altMode = AltMode.Capture; // start by climbing to initial target
    public bool IsAltCaptured => altMode == AltMode.Hold;

    [Tooltip("When within this error (m), declare altitude captured and hold.")]
    public float altHoldToleranceM = 5.0f; // 0.1 m ≈ 0.33 ft

    public float captureVyEpsilon = 0.3f; // m/s ~ 60 fpm
    float lastTargetAltM;

    [Header("VNAV Lite")]
    public bool vnavLiteEnabled = true;
    public float vnavMinDistanceM = 300f;
    public float vnavBlendStartM = 2000f;
    public float vnavBlendEndM = 500f;

    [Header("Turn Dynamics")]
    [Tooltip("Maximum bank angle used by the turn model (deg).")]
    public float maxBankDeg = 25f;

    [Tooltip("Maximum roll rate (deg/sec).")]
    public float maxRollRateDegPerSec = 15f;

    [Tooltip("Heading error (deg) -> bank command (deg).")]
    public float headingToBankGain = 0.5f;

    [Tooltip("If heading error is within this value, command wings level.")]
    public float wingsLevelErrorDeg = 2f;

    float currentBankDeg = 0f;

    [Header("Debug")]
    public bool enableHeadingDebug = false;
    float _hdgLogT;

    [Header("Heading Capture PD")]
    [Tooltip("P gain: heading error (deg) -> commanded yaw rate (deg/sec).")]
    public float yawRateP = 0.8f;

    [Tooltip("D gain: subtract measured yaw rate (deg/sec) to damp oscillation.")]
    public float yawRateD = 0.6f;

    [Tooltip("Limit for commanded yaw rate (deg/sec).")]
    public float maxYawRateDegPerSec = 12f;

    [Tooltip("Smooth rollout: below this heading error (deg), bank begins blending toward 0.")]
    public float rolloutStartErrDeg = 10f;

    [Tooltip("Smooth rollout: at/under this error (deg), bank fully blends to wings level.")]
    public float rolloutEndErrDeg = 2f;

    float prevYawDeg;
    bool yawInit;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!targets)
            targets = GetComponent<SimTargets>();

        rb.useGravity = false;

        // Keep the sim deterministic and planar:
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.constraints =
            RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        if (useInitialHoldAltitude && targets)
            targets.targetAltFtMsl = initialHoldAltFtMsl;

        lastTargetAltM = targets ? targets.TargetAltM : 0f;
        if (!nav)
            nav = GetComponentInParent<NavAutopilot>();
    }

    void FixedUpdate()
    {
        UpdateSpeed();
        UpdateHeading();
        UpdateAltitude();
    }

    // "Throttle" / speed hold: accel-limited, XZ only so altitude control stays independent.
    void UpdateSpeed()
    {
        Vector3 v = rb.linearVelocity;
        float vy = v.y;

        // Forward direction on the ground plane
        Vector3 fwd = transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f)
            fwd = Vector3.forward;
        fwd.Normalize();

        // Current forward ground speed (m/s)
        Vector3 vXZ = new Vector3(v.x, 0f, v.z);
        float vFwd = Vector3.Dot(vXZ, fwd);

        float target = targets ? targets.TargetSpeedMS : vFwd;
        float e_v = target - vFwd;

        // Accel command (m/s^2)
        float accel = Mathf.Clamp(e_v * speedResponse, -maxAccel, +maxAccel);
        vFwd += accel * Time.fixedDeltaTime;

        Vector3 newVXZ = fwd * vFwd;
        rb.linearVelocity = new Vector3(newVXZ.x, vy, newVXZ.z);
    }

    // Bank-based coordinated turn: heading error -> bank -> turn rate depends on ground speed.
    void UpdateHeading()
    {
        if (!targets)
            return;

        float dt = Time.fixedDeltaTime;
        float currentYaw = rb.rotation.eulerAngles.y;
        float targetYaw = targets.targetHdgDeg;

        if (!yawInit)
        {
            prevYawDeg = currentYaw;
            yawInit = true;
        }

        float yawRateMeas = Mathf.DeltaAngle(prevYawDeg, currentYaw) / dt; // deg/s
        prevYawDeg = currentYaw;

        float headingError = Mathf.DeltaAngle(currentYaw, targetYaw); // deg

        float yawRateCmd = yawRateP * headingError - yawRateD * yawRateMeas;
        yawRateCmd = Mathf.Clamp(yawRateCmd, -maxYawRateDegPerSec, +maxYawRateDegPerSec);

        Vector3 v = rb.linearVelocity;
        float groundSpeed = new Vector2(v.x, v.z).magnitude;
        groundSpeed = Mathf.Max(groundSpeed, 0.1f);

        float bankRad = Mathf.Atan((groundSpeed * (yawRateCmd * Mathf.Deg2Rad)) / 9.81f);
        float bankCmdDeg = Mathf.Clamp(bankRad * Mathf.Rad2Deg, -maxBankDeg, +maxBankDeg);

        float absErr = Mathf.Abs(headingError);
        float t = Mathf.InverseLerp(rolloutStartErrDeg, rolloutEndErrDeg, absErr); // 0..1
        float blend = Mathf.SmoothStep(0f, 1f, t); // 0 far, 1 near
        float desiredBank = Mathf.Lerp(bankCmdDeg, 0f, blend);

        currentBankDeg = Mathf.MoveTowards(currentBankDeg, desiredBank, maxRollRateDegPerSec * dt);

        float turnRateRad = 9.81f * Mathf.Tan(currentBankDeg * Mathf.Deg2Rad) / groundSpeed;
        float newYaw = currentYaw + turnRateRad * Mathf.Rad2Deg * dt;

        if (enableHeadingDebug)
            LogHeadingDebug(currentYaw, targetYaw, currentBankDeg, groundSpeed);
        rb.MoveRotation(Quaternion.Euler(0f, newYaw, -currentBankDeg));
    }

    void UpdateAltitude()
    {
        if (!targets)
            return;

        float dt = Time.fixedDeltaTime;
        float h = rb.position.y;
        float targetM = targets.TargetAltM;

        // Detect target change -> go to Capture
        if (Mathf.Abs(targetM - lastTargetAltM) > 0.01f)
            altMode = AltMode.Capture;
        lastTargetAltM = targetM;

        float e_h = targetM - h;

        // Hard capture: prevent asymptotic "never quite arrives"
        if (Mathf.Abs(e_h) <= altHoldToleranceM)
        {
            altMode = AltMode.Hold;
            vyCmdSmoothed = 0f;

            // Snap to target altitude (training-sim determinism)
            Vector3 pSnap = rb.position;
            pSnap.y = targetM;
            rb.position = pSnap;

            Vector3 vHold = rb.linearVelocity;
            vHold.y = 0f;
            rb.linearVelocity = vHold;
            return;
        }

        float vyDes = 0f;

        float vyCap = 0f;
        if (Mathf.Abs(e_h) > altitudeCaptureBandM)
            vyCap = Mathf.Clamp(e_h * altitudeResponse, -maxClimbRate, +maxClimbRate);

        vyDes = vyCap;

        if (vnavLiteEnabled && nav && nav.navEngaged)
        {
            float gs = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;
            float d = Mathf.Max(nav.activeDistance, vnavMinDistanceM);

            float vyVnav = Mathf.Clamp((e_h * gs) / d, -maxClimbRate, +maxClimbRate);

            float blend = Mathf.InverseLerp(vnavBlendStartM, vnavBlendEndM, d);
            blend = Mathf.Clamp01(blend);

            vyDes = Mathf.Lerp(vyCap, vyVnav, blend);
        }
        else // Hold
        {
            vyDes = 0f;
        }

        vyCmdSmoothed = Mathf.MoveTowards(vyCmdSmoothed, vyDes, maxVyAccel * dt);

        Vector3 v = rb.linearVelocity;
        v.y = vyCmdSmoothed;
        rb.linearVelocity = v;
    }

    void LogHeadingDebug(float currentYaw, float targetYaw, float bankDeg, float groundSpeed)
    {
        _hdgLogT += Time.fixedDeltaTime;
        if (_hdgLogT < 0.5f)
            return;
        _hdgLogT = 0f;

        float err = Mathf.DeltaAngle(currentYaw, targetYaw);
    }
}
