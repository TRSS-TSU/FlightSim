using UnityEngine;

public class TakeoffEngageButton : MonoBehaviour
{
    public TakeoffProcedureController takeoffProcedure;
    public GameObject takeoffButtonToHide;

    public void Press()
    {
        bool started = false;

        if (FlightSession.Instance && !FlightSession.Instance.TryBeginTakeoff())
        {
            Debug.LogWarning("[TakeoffEngageButton] Route review is required before takeoff.");
            return;
        }

        if (takeoffProcedure)
            started = takeoffProcedure.BeginTakeoff();
        else
            Debug.LogWarning("[TakeoffEngageButton] No TakeoffProcedureController assigned.");

        if (started && takeoffButtonToHide)
            takeoffButtonToHide.SetActive(false);
    }
}
