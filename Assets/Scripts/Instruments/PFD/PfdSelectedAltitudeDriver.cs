using TMPro;
using UnityEngine;

public class PfdSelectedAltitudeDriver : MonoBehaviour
{
    public FlightDataBus bus;
    public TMP_Text valueText;
    public RectTransform selectedBug;

    [Header("Tape Mapping")]
    public float pixelsPer100Ft = 68f;
    public float bugLimitY = 275f;
    public float smooth = 12f;

    float _bugY;

    void Awake()
    {
        ResolveRefs();
        if (selectedBug)
            _bugY = selectedBug.anchoredPosition.y;
    }

    void OnValidate() => ResolveRefs();

    void Update()
    {
        if (!bus)
            return;

        if (valueText)
            valueText.text = Mathf.Max(0f, bus.selectedAltFtMsl).ToString("00000");

        if (!selectedBug)
            return;

        float deltaFt = bus.selectedAltFtMsl - bus.altFtMsl;
        float targetY = Mathf.Clamp(deltaFt * pixelsPer100Ft / 100f, -bugLimitY, bugLimitY);
        _bugY = Mathf.Lerp(_bugY, targetY, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        selectedBug.anchoredPosition = new Vector2(selectedBug.anchoredPosition.x, _bugY);
    }

    void ResolveRefs()
    {
        if (!bus)
            bus = FindFirstObjectByType<FlightDataBus>();
        if (!valueText)
            valueText = transform.Find("Value_Text")?.GetComponent<TMP_Text>();
        if (!selectedBug && transform.parent)
            selectedBug = transform.parent.Find("Altitude_SelectedBug") as RectTransform;
    }
}
