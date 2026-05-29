using UnityEngine;

public class PfdAltitudeReadoutDriver : MonoBehaviour
{
    [Header("Data")]
    public FlightDataBus bus;

    [Header("Digit Drums")]
    public RectTransform tenThousandsDigitStrip;
    public RectTransform thousandsDigitStrip;
    public RectTransform hundredsDigitStrip;
    public RectTransform tensDigitStrip;
    public RectTransform onesDigitStrip;

    [Tooltip("Distance in pixels from one digit center to the next, including any layout spacing.")]
    public float digitHeight = 63f;

    [Range(0f, 1f)]
    public float snapThreshold = 0.25f;

    [Header("Debug")]
    public bool useDebugAltitude = false;
    public float debugAltitudeFt = 3000f;

    void Awake()
    {
        ResolveBus();
    }

    void OnValidate()
    {
        if (!bus)
            ResolveBus();
    }

    void Update()
    {
        float altitudeFt = useDebugAltitude ? debugAltitudeFt : (bus ? bus.altFtMsl : 0f);

        altitudeFt = Mathf.Max(0f, altitudeFt);

        float tenThousandsValue = altitudeFt / 10000f;
        float thousandsValue = Mathf.Repeat(altitudeFt / 1000f, 10f);
        float hundredsValue = Mathf.Repeat(altitudeFt / 100f, 10f);
        float tensValue = Mathf.Repeat(altitudeFt / 10f, 10f);
        float onesValue = Mathf.Repeat(altitudeFt, 10f);

        SetSnappedDrumValue(tenThousandsDigitStrip, tenThousandsValue, false);
        SetSnappedDrumValue(thousandsDigitStrip, thousandsValue, true);
        SetSnappedDrumValue(hundredsDigitStrip, hundredsValue, true);
        SetSnappedDrumValue(tensDigitStrip, tensValue, true);
        SetDrumValue(onesDigitStrip, onesValue);
    }

    void SetSnappedDrumValue(RectTransform strip, float digitValue, bool wraps)
    {
        float snapOffset = Mathf.Clamp01(snapThreshold);
        float snappedValue = Mathf.Floor(digitValue + snapOffset);

        if (wraps)
            snappedValue = Mathf.Repeat(snappedValue, 10f);

        SetDrumValue(strip, snappedValue);
    }

    void SetDrumValue(RectTransform strip, float digitValue)
    {
        if (!strip || digitHeight <= 0f)
            return;

        int digitCount = Mathf.Max(1, strip.childCount);
        float centerOffset = (digitCount - 1) * 0.5f * digitHeight;
        float clampedValue = Mathf.Clamp(digitValue, 0f, digitCount - 1);
        float y = clampedValue * digitHeight - centerOffset;

        strip.anchoredPosition = new Vector2(strip.anchoredPosition.x, y);
    }

    void ResolveBus()
    {
        if (bus)
            return;

        bus = GetComponentInParent<FlightDataBus>();
        if (!bus && transform.root)
            bus = transform.root.GetComponentInChildren<FlightDataBus>(true);
    }
}
