using UnityEngine;

public class PfdPitchNeedleDriver : MonoBehaviour
{
    public FlightDataBus bus;
    public RectTransform needle;
    public bool invertRotation;
    public float smooth = 12f;

    float _z;

    void Awake()
    {
        ResolveRefs();
        if (needle)
            _z = needle.localEulerAngles.z;
    }

    void OnValidate() => ResolveRefs();

    void Update()
    {
        if (!bus || !needle)
            return;

        float targetZ = invertRotation ? -bus.pitchDeg : bus.pitchDeg;
        _z = Mathf.LerpAngle(_z, targetZ, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        Debug.Log($"PfdPitchNeedleDriver: targetZ={targetZ:F1} _z={_z:F1}");
        needle.localEulerAngles = new Vector3(0f, 0f, _z);
    }

    void ResolveRefs()
    {
        if (!bus)
            bus = FindFirstObjectByType<FlightDataBus>();
        if (!needle)
            needle = transform as RectTransform;
    }
}
