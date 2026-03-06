using UnityEngine;

public class SimTargets : MonoBehaviour
{
    // Authoritative (pilot units)
    [Header("Authoritative Targets (Pilot Units)")]
    public float targetIasKt = 50f; // knots IAS
    public float targetAltFtMsl = 100f; // feet MSL (world Y in v1)
    public float targetHdgDeg = 0f; // degrees (0 = north/Z+)

    [Header("Guardrails (v1)")]
    public float minIasKt = 0f;
    public float maxIasKt = 450f;
    public float minAltFt = 0.0f;
    public float maxAltFt = 10000.0f;
    public float minHdgDeg = 0f;
    public float maxHdgDeg = 360f;

    // Conversion constants (future-proofed)
    static readonly float KT_TO_MS = 0.514444f;
    static readonly float FT_TO_M = 0.3048f;

    // Derived (internal SI for controllers)
    public float TargetSpeedMS => targetIasKt * KT_TO_MS;
    public float TargetAltM => targetAltFtMsl * FT_TO_M;
    public float TargetHdgRad => targetHdgDeg * Mathf.Deg2Rad;

    // Snapshot struct for deterministic controller reads
    public struct Snapshot
    {
        public readonly float iasKt;
        public readonly float altFt;
        public readonly float hdgDeg;

        public Snapshot(float ias, float alt, float hdg)
        {
            iasKt = ias;
            altFt = alt;
            hdgDeg = hdg;
        }
    }

    public Snapshot Current => new Snapshot(targetIasKt, targetAltFtMsl, targetHdgDeg);

    void OnValidate()
    {
        // Clamp values to guardrails
        targetIasKt = Mathf.Clamp(targetIasKt, minIasKt, maxIasKt);
        targetAltFtMsl = Mathf.Clamp(targetAltFtMsl, minAltFt, maxAltFt);
        targetHdgDeg = Mathf.Clamp(targetHdgDeg, minHdgDeg, maxHdgDeg);

        // Normalize heading into [0, 360)
        targetHdgDeg %= 360f;
        if (targetHdgDeg < 0f)
            targetHdgDeg += 360f;
    }
}
