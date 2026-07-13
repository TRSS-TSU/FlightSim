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
    public IEnumerator DeveloperTimeScalePanelClampsAndRestoresTimeScale()
    {
        Type panelType = RequireType("DeveloperTimeScalePanel");
        var panelObject = new GameObject("DeveloperTimeScalePanelTest");
        Component panel = panelObject.AddComponent(panelType);

        panelType.GetMethod("SetTimeScale", InstanceFlags).Invoke(panel, new object[] { 99f });
        Assert.AreEqual(8f, Time.timeScale);

        foreach (float expected in new[] { 0f, 1f, 2f, 4f, 8f })
        {
            panelType.GetMethod("SetTimeScale", InstanceFlags).Invoke(panel, new object[] { expected });
            Assert.AreEqual(expected, Time.timeScale);
        }

        UnityEngine.Object.Destroy(panelObject);
        yield return null;

        Assert.AreEqual(1f, Time.timeScale);
    }

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

    [UnityTest]
    public IEnumerator Scenario01PensiHoldLandingResultsLoopCompletes()
    {
        yield return SceneManager.LoadSceneAsync("Master_FMS", LoadSceneMode.Single);
        yield return null;

        Type scenarioType = RequireType("ScenarioDefinition");
        Type scenarioRuntimeType = RequireType("ScenarioRuntime");
        Type routerType = RequireType("FmsPageRouter");
        Type navType = RequireType("NavAutopilot");
        Type sessionType = RequireType("FlightSession");
        Type targetsType = RequireType("SimTargets");
        Type planeType = RequireType("PlaneController");

        UnityEngine.Object scenario = BuildScenario01(scenarioType);
        scenarioRuntimeType.GetMethod("Set", BindingFlags.Static | BindingFlags.Public)
            .Invoke(null, new object[] { scenario });
        yield return null;

        object router = UnityEngine.Object.FindFirstObjectByType(routerType);
        object nav = UnityEngine.Object.FindFirstObjectByType(navType);
        object session = sessionType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)
            .GetValue(null);
        object targets = UnityEngine.Object.FindFirstObjectByType(targetsType);
        Component plane = (Component)UnityEngine.Object.FindFirstObjectByType(planeType);

        Assert.NotNull(router);
        Assert.NotNull(nav);
        Assert.NotNull(session);
        Assert.NotNull(targets);
        Assert.NotNull(plane);

        targetsType.GetField("targetAltFtMsl", InstanceFlags).SetValue(targets, 3000f);
        targetsType.GetField("targetIasKt", InstanceFlags).SetValue(targets, 200f);

        IList route = ResolveRoute(scenario, scenarioType);
        Assert.IsTrue(
            (bool)routerType.GetMethod("ReplaceRuntimeRoute", InstanceFlags).Invoke(router, new object[] { route, 0 }),
            "Initial route did not build."
        );

        sessionType.GetMethod("NotifyRouteExecuted", InstanceFlags).Invoke(session, new object[] { false });
        sessionType.GetMethod("ConfirmRouteReview", InstanceFlags).Invoke(session, null);
        Assert.IsTrue(
            (bool)sessionType.GetMethod("TryBeginTakeoff", InstanceFlags).Invoke(session, null),
            "Route-reviewed session could not start takeoff."
        );

        sessionType.GetMethod("MarkPageViewed", InstanceFlags).Invoke(session, new object[] { "PosInit" });
        sessionType.GetMethod("MarkPageViewed", InstanceFlags).Invoke(session, new object[] { "PerfInit" });
        sessionType.GetMethod("ConfirmRouteReview", InstanceFlags).Invoke(session, null);
        AssertRecordField(session, sessionType, "posInitViewed", false);
        AssertRecordField(session, sessionType, "fuelViewed", false);
        AssertRecordField(session, sessionType, "weightViewed", false);
        AssertPhase(session, sessionType, "Takeoff");

        sessionType.GetMethod("NotifyNavHandoff", InstanceFlags).Invoke(session, null);

        sessionType.GetMethod("NotifyWaypointSequenced", InstanceFlags).Invoke(session, new object[] { "PENSI" });
        AssertPhase(session, sessionType, "EnteringHold");
        Assert.IsTrue((bool)navType.GetField("loop", InstanceFlags).GetValue(nav));
        Assert.AreEqual(1, (int)navType.GetField("activeIndex", InstanceFlags).GetValue(nav));
        AssertRoute((IList)routerType.GetMethod("GetRouteForDisplay", InstanceFlags).Invoke(router, null), "PENSI", "CUPER", "POOVE", "APUCE", "ALCOME");

        sessionType.GetMethod("NotifyWaypointSequenced", InstanceFlags).Invoke(session, new object[] { "CUPER" });
        AssertPhase(session, sessionType, "Holding");

        sessionType.GetMethod("ContinueHolding", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session, null);
        Assert.AreEqual(1, (int)navType.GetField("activeIndex", InstanceFlags).GetValue(nav));
        AssertRoute((IList)routerType.GetMethod("GetRouteForDisplay", InstanceFlags).Invoke(router, null), "CUPER", "POOVE", "APUCE", "ALCOME");

        sessionType.GetMethod("NotifyWaypointSequenced", InstanceFlags).Invoke(session, new object[] { "ALCOME" });
        AssertRecordField(session, sessionType, "holdCircuitsCompleted", 1);
        AssertPhase(session, sessionType, "Holding");
        AssertAltFt(targets, targetsType, 3000f, "Holding target altitude");

        sessionType.GetMethod("BeginLanding", InstanceFlags).Invoke(session, null);
        AssertPhase(session, sessionType, "HoldExitArmed");
        Assert.IsFalse((bool)navType.GetField("loop", InstanceFlags).GetValue(nav));
        AssertAltFt(targets, targetsType, 2000f, "Landing selection descent target altitude");
        AssertRoute(
            (IList)routerType.GetMethod("GetRouteForDisplay", InstanceFlags).Invoke(router, null),
            "CUPER",
            "POOVE",
            "APUCE",
            "ALCOME",
            "KNPA_RW25L_FINAL",
            "KNPA_RW25L_TOUCHDOWN",
            "KNPA_FINAL_STOP"
        );

        sessionType.GetMethod("NotifyWaypointSequenced", InstanceFlags).Invoke(session, new object[] { "APUCE" });
        AssertAltFt(targets, targetsType, 2000f, "ALCOME descent target altitude");

        sessionType.GetMethod("NotifyWaypointSequenced", InstanceFlags).Invoke(session, new object[] { "ALCOME" });
        AssertPhase(session, sessionType, "Approach");
        AssertAltFt(targets, targetsType, 1200f, "Final approach target altitude");

        sessionType.GetMethod("NotifyWaypointSequenced", InstanceFlags).Invoke(session, new object[] { "KNPA_RW25L_FINAL" });
        AssertAltFt(targets, targetsType, 0f, "Touchdown target altitude");
        AssertIasKt(targets, targetsType, 100f, "Touchdown approach target IAS");

        sessionType.GetMethod("NotifyWaypointSequenced", InstanceFlags).Invoke(session, new object[] { "KNPA_RW25L_TOUCHDOWN" });
        AssertPhase(session, sessionType, "Stopped");
        AssertRecordField(session, sessionType, "touchdownReached", true);
        AssertRecordField(session, sessionType, "finalStopReached", true);
        AssertRecordField(session, sessionType, "status", "Completed");
        AssertAltFt(targets, targetsType, 0f, "Stopped target altitude");
        AssertIasKt(targets, targetsType, 0f, "Stopped target IAS");
        Assert.AreEqual(0f, plane.transform.position.y, 0.001f, "Aircraft did not stop on the ground plane.");

        sessionType
            .GetMethod("EndFlight", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(session, null);
        yield return null;

        Assert.AreEqual("ScenarioResults", SceneManager.GetActiveScene().name);
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
        AddWaypoint(waypoints, waypointType, "CUPER", 30.17d, -87.15d, "Approach", false, 3000f, 0f, 0f);
        AddWaypoint(waypoints, waypointType, "POOVE", 30.11d, -87.33d, "Approach", false, 3000f, 0f, 0f);
        AddWaypoint(waypoints, waypointType, "APUCE", 30.01d, -87.47d, "Approach", false, 3000f, 0f, 0f);
        AddWaypoint(waypoints, waypointType, "ALCOME", 30.22d, -87.58d, "Approach", false, 2000f, 0f, 0f);
        AddWaypoint(waypoints, waypointType, "KNPA_RW25L_FINAL", 30.39d, -87.43d, "Approach", false, 1200f, 200f, 250f);
        AddWaypoint(waypoints, waypointType, "KNPA_RW25L_TOUCHDOWN", 30.35d, -87.31d, "Approach", false, 0f, 100f, 250f);
        AddWaypoint(waypoints, waypointType, "KNPA_FINAL_STOP", 30.35119059d, -87.29687867d, "Approach", false, 0f, 0f, 250f);
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

    private static void AssertRoute(IList route, params string[] expected)
    {
        Assert.AreEqual(expected.Length, route.Count, "Route length mismatch.");
        for (int i = 0; i < expected.Length; i++)
        {
            object waypoint = route[i];
            Assert.AreEqual(expected[i], waypoint.GetType().GetField("ident", InstanceFlags).GetValue(waypoint), $"Route waypoint {i} mismatch.");
        }
    }

    private static void AssertPhase(object session, Type sessionType, string expected)
    {
        Assert.AreEqual(expected, sessionType.GetProperty("Phase", InstanceFlags).GetValue(session).ToString());
    }

    private static void AssertRecordField(object session, Type sessionType, string fieldName, object expected)
    {
        object record = sessionType.GetProperty("Record", InstanceFlags).GetValue(session);
        object actual = record.GetType().GetField(fieldName, InstanceFlags).GetValue(record);
        if (expected is string)
            Assert.AreEqual(expected, actual.ToString());
        else
            Assert.AreEqual(expected, actual);
    }

    private static float GetAltFt(object targets, Type targetsType)
    {
        return (float)targetsType.GetField("targetAltFtMsl", InstanceFlags).GetValue(targets);
    }

    private static void AssertAltFt(object targets, Type targetsType, float expected, string label)
    {
        Assert.AreEqual(expected, GetAltFt(targets, targetsType), 0.1f, label);
    }

    private static void AssertIasKt(object targets, Type targetsType, float expected, string label)
    {
        float actual = (float)targetsType.GetField("targetIasKt", InstanceFlags).GetValue(targets);
        Assert.AreEqual(expected, actual, 0.1f, label);
    }
}
