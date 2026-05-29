using UnityEngine;

public class AdiHorizonBankDriver : MonoBehaviour
{
    [Header("References")]
    public FlightDataBus bus;
    public RectTransform horizonImage; // ADI_Horizon_Image

    [Header("Tuning")]
    public bool invertRotation = true;
    public float smooth = 12f;

    float _z;

    void Awake()
    {
        if (horizonImage)
            _z = horizonImage.localEulerAngles.z;
    }

    void Update()
    {
        if (!bus || !horizonImage)
            return;

        float targetZ = invertRotation ? -bus.bankDeg : bus.bankDeg;

        _z = Mathf.LerpAngle(_z, targetZ, 1f - Mathf.Exp(-smooth * Time.deltaTime));

        horizonImage.localEulerAngles = new Vector3(0f, 0f, _z);
    }
}
