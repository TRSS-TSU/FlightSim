using UnityEngine;

public class PfdHorizonPitchDriver : MonoBehaviour
{
    public FlightDataBus bus;
    public RectTransform horizon;

    [Header("Pitch Translation")]
    public float pixelsPerPitchDeg = 12f;
    public bool invertY = false;
    public float smooth = 12f;

    Vector2 _baseAnchoredPosition;
    float _currentY;

    void Awake()
    {
        ResolveRefs();

        if (horizon)
        {
            _baseAnchoredPosition = horizon.anchoredPosition;
            _currentY = _baseAnchoredPosition.y;
        }
    }

    void OnValidate()
    {
        ResolveRefs();
    }

    void Update()
    {
        if (!bus || !horizon)
            return;

        float direction = invertY ? 1f : -1f;
        float targetY = _baseAnchoredPosition.y + bus.pitchDeg * pixelsPerPitchDeg * direction;

        _currentY = Mathf.Lerp(_currentY, targetY, 1f - Mathf.Exp(-smooth * Time.deltaTime));

        horizon.anchoredPosition = new Vector2(_baseAnchoredPosition.x, _currentY);
    }

    void ResolveRefs()
    {
        if (!bus)
            bus = FindFirstObjectByType<FlightDataBus>();

        if (!horizon)
            horizon = transform as RectTransform;
    }
}
