using System;
using TMPro;
using UnityEngine;

public class PfdLowerDataDriver : MonoBehaviour
{
    const string MissingWaypointText = "----";
    const string WaypointPrefix = "WP_";

    [Header("Data")]
    public FlightDataBus bus;
    public SimTargets targets;
    public NavAutopilot nav;

    [Header("Text")]
    public TMP_Text currentHeadingText;
    public TMP_Text desiredHeadingText;
    public TMP_Text nextWaypointText;
    public TMP_Text timeText;

    public float CurrentHeadingDeg => bus ? NormalizeHeading(bus.hdgDeg) : 0f;

    public float DesiredHeadingDeg
    {
        get => targets ? NormalizeHeading(targets.targetHdgDeg) : 0f;
        set
        {
            if (targets)
                targets.targetHdgDeg = NormalizeHeading(value);
        }
    }

    public string NextWaypointName
    {
        get
        {
            if (!nav || !nav.plan || nav.plan.waypoints == null || nav.plan.waypoints.Length == 0)
                return MissingWaypointText;

            int activeIndex = nav.activeIndex;
            if (activeIndex < 0 || activeIndex >= nav.plan.waypoints.Length)
                return MissingWaypointText;

            Transform waypoint = nav.plan.waypoints[activeIndex];
            if (!waypoint)
                return MissingWaypointText;

            string waypointName = waypoint.name;
            return waypointName.StartsWith(WaypointPrefix, StringComparison.Ordinal)
                ? waypointName.Substring(WaypointPrefix.Length)
                : waypointName;
        }
    }

    public string CurrentUtcTime => DateTime.UtcNow.ToString("HH:mm");

    void Awake() => ResolveRefs();

    void OnValidate()
    {
        if (!Application.isPlaying)
            ResolveRefs();
    }

    void Update()
    {
        if (currentHeadingText)
            currentHeadingText.text = FormatHeading(CurrentHeadingDeg);
        if (desiredHeadingText)
            desiredHeadingText.text = FormatHeading(DesiredHeadingDeg);
        if (nextWaypointText)
            nextWaypointText.text = NextWaypointName;
        if (timeText)
            timeText.text = CurrentUtcTime;
    }

    void ResolveRefs()
    {
        if (!bus)
            bus = FindFirstObjectByType<FlightDataBus>();
        if (!targets && bus)
            targets = bus.targets;
        if (!targets)
            targets = FindFirstObjectByType<SimTargets>();
        if (!nav && bus)
            nav = bus.nav;
        if (!nav)
            nav = FindFirstObjectByType<NavAutopilot>();
    }

    static float NormalizeHeading(float headingDeg) => Mathf.Repeat(headingDeg, 360f);

    static string FormatHeading(float headingDeg)
    {
        int roundedHeading = Mathf.RoundToInt(NormalizeHeading(headingDeg)) % 360;
        return roundedHeading.ToString("000");
    }
}
