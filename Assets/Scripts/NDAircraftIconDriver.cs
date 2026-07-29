using UnityEngine;

public class NDAircraftIconDriver : MonoBehaviour
{
    [Header("Sources")]
    public Transform aircraftRoot; // _AircraftRoot
    public NDRangeState rangeState;

    [Header("Options")]
    public bool northUp = true; // ND is north-up for now
    public float rotationOffsetDeg = 0f; // use if sprite points up/right/etc

    [Header("Range Sizing")]
    public float sizeAt10Nm = 1f;
    public float sizeAt5Nm = 1.3f;
    public float sizeAt2Nm = 1.6f;
    public float sizeAt1Nm = 2f;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
        if (!rangeState)
            rangeState = GetComponentInParent<NDRangeState>();
    }

    private void OnEnable()
    {
        if (rangeState != null)
        {
            rangeState.OnRangeChanged += HandleRangeChanged;
            ApplySize(rangeState.CurrentRangeNm);
        }
    }

    private void OnDisable()
    {
        if (rangeState != null)
            rangeState.OnRangeChanged -= HandleRangeChanged;
    }

    private void HandleRangeChanged(int rangeNm) => ApplySize(rangeNm);

    private void ApplySize(int rangeNm)
    {
        float multiplier = rangeNm <= 1
            ? sizeAt1Nm
            : rangeNm <= 2
                ? sizeAt2Nm
                : rangeNm <= 5
                    ? sizeAt5Nm
                    : sizeAt10Nm;

        transform.localScale = baseScale * multiplier;
    }

    void LateUpdate()
    {
        if (aircraftRoot == null)
            return;

        // Unity yaw: 0 = +Z (north), increases clockwise → matches ND nicely
        float headingDeg = aircraftRoot.eulerAngles.y;

        // For north-up ND: icon rotates with aircraft
        float iconDeg = northUp ? headingDeg : 0f;

        // UI rotates around Z axis
        transform.localRotation = Quaternion.Euler(0f, 0f, -iconDeg + rotationOffsetDeg);
    }
}
