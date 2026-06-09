using UnityEngine;

public class TakeoffEngageButton : MonoBehaviour
{
    public SimTargets targets;
    public NavAutopilot nav;

    public GameObject takeoffButtonToHide;

    public float takeoffIasKt = 160f;
    public float initialAltFtMsl = 1500f;

    public void Press()
    {
        if (targets)
        {
            targets.targetIasKt = takeoffIasKt;
            targets.targetAltFtMsl = initialAltFtMsl;
        }

        if (nav)
            nav.SetNavEngaged(true);

        if (takeoffButtonToHide)
            takeoffButtonToHide.SetActive(false);

        Debug.Log("[TakeoffEngageButton] Takeoff engaged. Takeoff button hidden.");
    }
}
