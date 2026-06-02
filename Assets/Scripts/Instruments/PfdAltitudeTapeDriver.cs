using System;
using TMPro;
using UnityEngine;

public class PfdAltitudeTapeDriver : MonoBehaviour
{
    public FlightDataBus bus;
    public float pixelsPer100Ft = 68f;

    TMP_Text[] _labels;

    void Awake() => ResolveRefs();

    void OnValidate() => ResolveRefs();

    void Update()
    {
        if (!bus)
            return;

        float altitudeFt = Mathf.Max(0f, bus.altFtMsl);
        int centerAltitudeFt = Mathf.FloorToInt(altitudeFt / 100f) * 100;
        float offsetFt = altitudeFt - centerAltitudeFt;

        var rect = (RectTransform)transform;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -offsetFt * pixelsPer100Ft / 100f);

        for (int i = 0; i < _labels.Length; i++)
        {
            int labelAltitudeFt = Mathf.Max(0, centerAltitudeFt + (3 - i) * 100);
            _labels[i].text = labelAltitudeFt.ToString();
        }
    }

    void ResolveRefs()
    {
        if (!bus)
            bus = FindFirstObjectByType<FlightDataBus>();

        _labels = GetComponentsInChildren<TMP_Text>(true);
        Array.Sort(_labels, (a, b) =>
            b.rectTransform.anchoredPosition.y.CompareTo(a.rectTransform.anchoredPosition.y));
    }
}
