using UnityEngine;

public class HeadingBugDriver : MonoBehaviour
{
    public FlightDataBus bus;
    public SimTargets targets;
    public RectTransform bugBox;

    [Header("Slew Settings")]
    public float slewRateDegPerSec = 180f;

    private float smoothedDelta = 0f;

    void Update()
    {
        if (!bus || !targets || !bugBox)
            return;

        // Desired relative angle
        float desiredDelta = Mathf.DeltaAngle(bus.hdg, targets.targetHdgDeg);

        // Smooth the delta itself
        smoothedDelta = Mathf.LerpAngle(
            smoothedDelta,
            desiredDelta,
            Time.deltaTime * (slewRateDegPerSec / 90f)
        );

        // Apply rotation (negative for UI clockwise)
        bugBox.localEulerAngles = new Vector3(0f, 0f, -smoothedDelta);
    }
}
