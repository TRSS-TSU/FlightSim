using UnityEngine;

public class PfdVerticalSpeedScaleDriver : MonoBehaviour
{
    public FlightDataBus bus;
    public RectTransform pointer;
    public float smooth = 12f;

    float _pointerY;

    void Awake()
    {
        ResolveRefs();
        if (pointer)
            _pointerY = pointer.anchoredPosition.y;
    }

    void OnValidate() => ResolveRefs();

    void Update()
    {
        if (!bus || !pointer)
            return;

        float targetY = MapVsiToY(bus.vsiFpm);
        _pointerY = Mathf.Lerp(_pointerY, targetY, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        pointer.anchoredPosition = new Vector2(pointer.anchoredPosition.x, _pointerY);
    }

    static float MapVsiToY(float vsiFpm)
    {
        float sign = Mathf.Sign(vsiFpm);
        float magnitude = Mathf.Abs(vsiFpm);

        if (magnitude <= 1000f)
            return sign * Mathf.Lerp(0f, 80f, magnitude / 1000f);
        if (magnitude <= 2000f)
            return sign * Mathf.Lerp(80f, 155f, (magnitude - 1000f) / 1000f);

        return sign * Mathf.Lerp(155f, 255f, Mathf.Clamp01((magnitude - 2000f) / 2000f));
    }

    void ResolveRefs()
    {
        if (!bus)
            bus = FindFirstObjectByType<FlightDataBus>();
        if (!pointer)
            pointer = transform.Find("VS_Pointer") as RectTransform;
    }
}
