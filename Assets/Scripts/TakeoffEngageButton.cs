using UnityEngine;

public class TakeoffEngageButton : MonoBehaviour
{
    public TakeoffProcedureController takeoffProcedure;
    public GameObject takeoffButtonToHide;

    public void Press()
    {
        bool started = false;

        if (takeoffProcedure)
            started = takeoffProcedure.BeginTakeoff();
        else
            Debug.LogWarning("[TakeoffEngageButton] No TakeoffProcedureController assigned.");

        if (started && takeoffButtonToHide)
            takeoffButtonToHide.SetActive(false);

        Debug.Log(
            started
                ? "[TakeoffEngageButton] Takeoff requested. Takeoff button hidden."
                : "[TakeoffEngageButton] Takeoff request rejected. Takeoff button remains visible."
        );
    }
}
