using UnityEngine;

public class FmsPhaseButtonController : MonoBehaviour
{
    public GameObject takeoffButton;
    public GameObject landButton;

    public NavAutopilot nav;
    public FlightPlan plan;

    public float landShowDistanceM = 2500f;

    public RouteTileAuditLogger tileAuditLogger;

    void Awake()
    {
        if (takeoffButton)
            takeoffButton.SetActive(false);
        if (landButton)
            landButton.SetActive(false);
    }

    private void OnEnable()
    {
        if (FlightSession.Instance)
            FlightSession.Instance.StartAvailabilityChanged += SetTakeoffVisible;
    }

    private void OnDisable()
    {
        if (FlightSession.Instance)
            FlightSession.Instance.StartAvailabilityChanged -= SetTakeoffVisible;
    }

    public void ShowTakeoff()
    {
        bool verified = FlightSession.Instance && FlightSession.Instance.Record.routeVerified && FlightSession.Instance.Phase == FlightPhase.ReadyForTakeoff;
        SetTakeoffVisible(verified);
        if (landButton)
            landButton.SetActive(false);
    }

    private void SetTakeoffVisible(bool visible)
    {
        if (takeoffButton)
            takeoffButton.SetActive(visible);
    }

}
