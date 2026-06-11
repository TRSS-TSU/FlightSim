using UnityEngine;

public class TakeoffEngageButton : MonoBehaviour
{
    public NavAutopilot nav;

    public GameObject takeoffButtonToHide;

    public void Press()
    {
        if (nav)
            nav.SetNavEngaged(true);

        if (takeoffButtonToHide)
            takeoffButtonToHide.SetActive(false);

        Debug.Log("[TakeoffEngageButton] Takeoff engaged. Takeoff button hidden.");
    }
}
