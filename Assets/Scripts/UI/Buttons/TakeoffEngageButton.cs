using UnityEngine;

public class TakeoffEngageButton : MonoBehaviour
{
    public TakeoffProcedureController takeoffProcedure;
    public GameObject takeoffButtonToHide;

    public void Press()
    {
        if (takeoffProcedure)
            takeoffProcedure.BeginTakeoff();
        else
            Debug.LogWarning("[TakeoffEngageButton] No TakeoffProcedureController assigned.");

        if (takeoffButtonToHide)
            takeoffButtonToHide.SetActive(false);

        Debug.Log("[TakeoffEngageButton] Takeoff requested. Takeoff button hidden.");
    }
}
