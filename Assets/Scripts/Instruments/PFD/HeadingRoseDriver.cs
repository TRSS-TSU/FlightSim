using UnityEngine;

public class HeadingRoseDriver : MonoBehaviour
{
    public FlightDataBus bus;
    public RectTransform compassRose; // Compass_Rose
    public RectTransform headingBugBox; // your pivot box (optional)

    [Range(0f, 360f)]
    public float bugHeading = 0f; // v1 manual
    public float smooth = 10f;

    float _roseZ;

    void Awake()
    {
        if (compassRose)
            _roseZ = compassRose.localEulerAngles.z;
    }

    void Update()
    {
        if (!bus || !compassRose)
            return;

        float targetZ = Mathf.Repeat(bus.hdg, 360f);
        _roseZ = Mathf.LerpAngle(_roseZ, targetZ, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        compassRose.localEulerAngles = new Vector3(0, 0, _roseZ);

        if (headingBugBox)
            headingBugBox.localEulerAngles = new Vector3(0, 0, bugHeading);
    }
}
