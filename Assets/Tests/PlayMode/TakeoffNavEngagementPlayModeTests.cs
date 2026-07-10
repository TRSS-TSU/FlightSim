using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class TakeoffNavEngagementPlayModeTests
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public;

    [UnityTest]
    public IEnumerator Scenario01TakeoffEngagesNavAfterThreshold()
    {
        yield return SceneManager.LoadSceneAsync("Master_FMS", LoadSceneMode.Single);
        yield return null;

        Type scenarioType = RequireType("ScenarioDefinition");
        Type flightPlanType = RequireType("FlightPlan");
        Type navType = RequireType("NavAutopilot");
        Type takeoffType = RequireType("TakeoffProcedureController");
        Type targetsType = RequireType("SimTargets");
        Type scenarioRuntimeType = RequireType("ScenarioRuntime");

        UnityEngine.Object scenario = BuildScenario01(scenarioType);
        scenarioRuntimeType.GetMethod("Set", BindingFlags.Static | BindingFlags.Public)
            .Invoke(null, new object[] { scenario });

        object flightPlan = UnityEngine.Object.FindFirstObjectByType(flightPlanType);
        object nav = UnityEngine.Object.FindFirstObjectByType(navType);
        object takeoff = UnityEngine.Object.FindFirstObjectByType(takeoffType);
        object targets = UnityEngine.Object.FindFirstObjectByType(targetsType);

        Assert.NotNull(flightPlan);
        Assert.NotNull(nav);
        Assert.NotNull(takeoff);
        Assert.NotNull(targets);

        IList route = ResolveRoute(scenario, scenarioType);
        int baseZoom = (int)scenarioType.GetField("baseZoom", InstanceFlags).GetValue(scenario);
        flightPlanType.GetMethod("ActivateRouteFromFms", InstanceFlags)
            .Invoke(flightPlan, new object[] { scenario, route, baseZoom });

        Array waypoints = (Array)flightPlanType.GetField("waypoints", InstanceFlags).GetValue(flightPlan);
        Assert.Greater(waypoints.Length, 1);

        int handoffIndex = (int)takeoffType.GetField("navHandoffRouteIndex", InstanceFlags).GetValue(takeoff);
        navType.GetField("activeIndex", InstanceFlags)
            .SetValue(nav, Mathf.Clamp(handoffIndex, 0, waypoints.Length - 1));
        navType.GetMethod("ResetCaptureState", InstanceFlags).Invoke(nav, null);
        navType.GetMethod("SetNavEngaged", InstanceFlags).Invoke(nav, new object[] { false });

        AssertAltFt(targets, targetsType, 0f, "Scenario start target altitude");
        Assert.IsTrue((bool)takeoffType.GetMethod("BeginTakeoff", InstanceFlags).Invoke(takeoff, null));

        float deadline = Time.time + 35f;
        while (Time.time < deadline && !(bool)navType.GetField("navEngaged", InstanceFlags).GetValue(nav))
            yield return null;

        Assert.IsTrue(
            (bool)navType.GetField("navEngaged", InstanceFlags).GetValue(nav),
            "NAV did not engage during Scenario 01 takeoff."
        );
        Assert.AreEqual(
            Mathf.Clamp(handoffIndex, 0, waypoints.Length - 1),
            (int)navType.GetField("activeIndex", InstanceFlags).GetValue(nav)
        );
        AssertAltFt(targets, targetsType, 250f, "NAV handoff target altitude");

        deadline = Time.time + 25f;
        while (Time.time < deadline && GetAltFt(targets, targetsType) < 2999f)
            yield return null;

        AssertAltFt(targets, targetsType, 3000f, "Departure target altitude");

        float stableUntil = Time.time + 3f;
        while (Time.time < stableUntil)
        {
            AssertAltFt(targets, targetsType, 3000f, "Stable departure target altitude");
            yield return null;
        }
    }

    private static Type RequireType(string typeName)
    {
        Type type = Type.GetType(typeName + ", Assembly-CSharp");
        Assert.NotNull(type, $"Could not resolve runtime type {typeName}.");
        return type;
    }

    private static UnityEngine.Object BuildScenario01(Type scenarioType)
    {
        Type waypointType = scenarioType.GetNestedType("WaypointDef", BindingFlags.Public);
        UnityEngine.Object scenario = ScriptableObject.CreateInstance(scenarioType);

        SetField(scenario, scenarioType, "scenarioTitle", "Flight Scenario 01");
        SetField(scenario, scenarioType, "route", "T3002");
        SetField(scenario, scenarioType, "centerLatDeg", 30.3d);
        SetField(scenario, scenarioType, "centerLonDeg", -87.3d);
        SetField(scenario, scenarioType, "baseZoom", 14);

        IList waypoints = (IList)Activator.CreateInstance(
            typeof(System.Collections.Generic.List<>).MakeGenericType(waypointType)
        );
        AddWaypoint(waypoints, waypointType, "KNPA", 30.35119059d, -87.29687867d, "Route", true, 3000f, 0f, 0f);
        AddWaypoint(waypoints, waypointType, "KNPA_EOR25R_START", 30.35119059d, -87.29687867d, "AircraftStart", false, 0f, 0f, 250f);
        AddWaypoint(waypoints, waypointType, "KNPA_RW25R_THRESH", 30.34346882d, -87.32018463d, "Takeoff", false, 200f, 110f, 250f);
        AddWaypoint(waypoints, waypointType, "KNPA_DEP_1DME", 30.33d, -87.36d, "Takeoff", false, 3000f, 400f, 0f);
        AddWaypoint(waypoints, waypointType, "TEEZY", 30.18d, -87.69d, "Route", true, 3000f, 0f, 0f);
        AddWaypoint(waypoints, waypointType, "TRADR", 30.3d, -88.06d, "Route", true, 3000f, 0f, 0f);
        AddWaypoint(waypoints, waypointType, "BFM", 30.61d, -88.06d, "Route", true, 3000f, 0f, 0f);
        AddWaypoint(waypoints, waypointType, "VR1020_A", 31d, -88d, "Route", true, 3000f, 0f, 0f);
        AddWaypoint(waypoints, waypointType, "VR1020_B", 31.55d, -87.52d, "Route", true, 3000f, 0f, 0f);
        AddWaypoint(waypoints, waypointType, "VR1020_C", 32.08d, -87.4d, "Route", true, 3000f, 0f, 0f);
        AddWaypoint(waypoints, waypointType, "VR1020_D", 31.4d, -86.73d, "Route", true, 3000f, 0f, 0f);
        AddWaypoint(waypoints, waypointType, "VR1020_E", 31.1d, -86.57d, "Route", true, 3000f, 0f, 0f);
        AddWaypoint(waypoints, waypointType, "CEW", 30.83d, -86.68d, "Route", true, 3000f, 0f, 0f);
        AddWaypoint(waypoints, waypointType, "PENSI", 30.79d, -87.27d, "Route", true, 3000f, 0f, 0f);
        AddWaypoint(waypoints, waypointType, "KNPA", 30.35119059d, -87.29687867d, "Route", true, 3000f, 0f, 0f);
        SetField(scenario, scenarioType, "waypoints", waypoints);

        IList route = new System.Collections.Generic.List<string>
        {
            "KNPA",
            "TEEZY",
            "TRADR",
            "BFM",
            "VR1020_A",
            "VR1020_B",
            "VR1020_C",
            "VR1020_D",
            "VR1020_E",
            "CEW",
            "PENSI",
            "KNPA"
        };
        SetField(scenario, scenarioType, "prefillRouteIdents", route);

        return scenario;
    }

    private static void AddWaypoint(
        IList waypoints,
        Type waypointType,
        string ident,
        double latDeg,
        double lonDeg,
        string role,
        bool includeInActiveRoute,
        float targetAltFtMsl,
        float targetIasKt,
        float targetHdgDeg
    )
    {
        object waypoint = Activator.CreateInstance(waypointType);
        SetField(waypoint, waypointType, "ident", ident);
        SetField(waypoint, waypointType, "latDeg", latDeg);
        SetField(waypoint, waypointType, "lonDeg", lonDeg);
        SetField(waypoint, waypointType, "role", Enum.Parse(waypointType.GetField("role", InstanceFlags).FieldType, role));
        SetField(waypoint, waypointType, "includeInActiveRoute", includeInActiveRoute);
        SetField(waypoint, waypointType, "targetAltFtMsl", targetAltFtMsl);
        SetField(waypoint, waypointType, "targetIasKt", targetIasKt);
        SetField(waypoint, waypointType, "targetHdgDeg", targetHdgDeg);
        waypoints.Add(waypoint);
    }

    private static void SetField(object target, Type type, string fieldName, object value)
    {
        type.GetField(fieldName, InstanceFlags).SetValue(target, value);
    }

    private static IList ResolveRoute(UnityEngine.Object scenario, Type scenarioType)
    {
        Type waypointType = scenarioType.GetNestedType("WaypointDef", BindingFlags.Public);
        Type routeType = typeof(System.Collections.Generic.List<>).MakeGenericType(waypointType);
        IList route = (IList)Activator.CreateInstance(routeType);

        IList idents = (IList)scenarioType.GetField("prefillRouteIdents", InstanceFlags).GetValue(scenario);
        IList waypoints = (IList)scenarioType.GetField("waypoints", InstanceFlags).GetValue(scenario);
        FieldInfo identField = waypointType.GetField("ident", InstanceFlags);

        foreach (string ident in idents)
        {
            object match = null;
            foreach (object wp in waypoints)
            {
                if (string.Equals((string)identField.GetValue(wp), ident, StringComparison.OrdinalIgnoreCase))
                {
                    match = wp;
                    break;
                }
            }

            Assert.NotNull(match, $"Scenario route waypoint '{ident}' was not found.");
            route.Add(match);
        }

        return route;
    }

    private static float GetAltFt(object targets, Type targetsType)
    {
        return (float)targetsType.GetField("targetAltFtMsl", InstanceFlags).GetValue(targets);
    }

    private static void AssertAltFt(object targets, Type targetsType, float expected, string label)
    {
        Assert.AreEqual(expected, GetAltFt(targets, targetsType), 0.1f, label);
    }
}
