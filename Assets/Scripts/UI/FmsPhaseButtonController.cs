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

    public void ShowTakeoff()
    {
        if (takeoffButton)
            takeoffButton.SetActive(true);
        if (landButton)
            landButton.SetActive(false);
    }

    void Update()
    {
        if (!nav || !plan || plan.waypoints == null || plan.waypoints.Length == 0)
            return;

        bool onLastWaypoint = nav.activeIndex >= plan.waypoints.Length - 1;
        bool nearLast = nav.activeDistance <= landShowDistanceM;

        if (onLastWaypoint && nearLast)
        {
            if (landButton)
                landButton.SetActive(true);
        }
    }
}
