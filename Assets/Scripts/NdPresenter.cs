using UnityEngine;

public class NdPresenter : MonoBehaviour
{
    [Header("Refs")]
    public Transform aircraft;
    public FlightPlan flightPlan;
    public Camera ndCamera;

    [Header("Route Line")]
    [SerializeField]
    private float routeLineY = 5f;

    [SerializeField]
    private float routeLineWidth = 30f;

    [Header("ND Objects (in ND layer)")]
    public LineRenderer routeLine; // ND_RouteLine
    public Transform waypointParent; // ND_Waypoints

    void OnEnable()
    {
        if (flightPlan)
            flightPlan.OnRouteBuilt += BuildRouteLine;
    }

    void OnDisable()
    {
        if (flightPlan)
            flightPlan.OnRouteBuilt -= BuildRouteLine;
    }

    void Start()
    {
        BuildRouteLine();
    }

    void LateUpdate()
    {
        if (!aircraft || !ndCamera)
            return;

        Vector3 p = aircraft.position;

        // Follow from above (position only; rotation stays Inspector-owned)
        ndCamera.transform.position = new Vector3(p.x, p.y + 120, p.z);
    }

    void BuildRouteLine()
    {
        if (!flightPlan || flightPlan.waypoints == null || routeLine == null)
            return;

        routeLine.positionCount = flightPlan.waypoints.Length;

        for (int i = 0; i < flightPlan.waypoints.Length; i++)
        {
            Vector3 pos = flightPlan.waypoints[i].position;

            // Keep world X/Z, lift line above map/ground.
            routeLine.SetPosition(i, new Vector3(pos.x, routeLineY, pos.z));
        }

        routeLine.startWidth = routeLineWidth;
        routeLine.endWidth = routeLineWidth;
    }
}
