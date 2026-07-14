using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class RotarySliderControlPlayModeTests
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [UnityTest]
    public IEnumerator HeadingSliderStaysPendingUntilCommitAndNavCanReengage()
    {
        yield return SceneManager.LoadSceneAsync("Master_FMS", LoadSceneMode.Single);
        yield return null;

        Type rotaryType = RequireType("RotarySliderControl");
        Type targetsType = RequireType("SimTargets");
        Type navType = RequireType("NavAutopilot");

        UnityEngine.Object[] controls = UnityEngine.Object.FindObjectsByType(
            rotaryType,
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        Assert.AreEqual(2, controls.Length);

        UnityEngine.Object heading = Array.Find(
            controls,
            control => rotaryType.GetField("targetKind", InstanceFlags).GetValue(control).ToString() == "Heading"
        );
        Assert.NotNull(heading);

        UnityEngine.Object targets = UnityEngine.Object.FindFirstObjectByType(targetsType);
        UnityEngine.Object nav = UnityEngine.Object.FindFirstObjectByType(navType);
        Assert.NotNull(targets);
        Assert.NotNull(nav);

        navType.GetMethod("SetNavEngaged", InstanceFlags).Invoke(nav, new object[] { true });
        rotaryType.GetMethod("OpenAdjustment", InstanceFlags).Invoke(heading, null);
        Assert.IsFalse((bool)navType.GetField("navEngaged", InstanceFlags).GetValue(nav));

        float appliedBeforeCommit = (float)targetsType
            .GetField("targetHdgDeg", InstanceFlags)
            .GetValue(targets);
        Slider slider = (Slider)rotaryType.GetField("slider", InstanceFlags).GetValue(heading);
        slider.value = 123f;
        yield return null;

        Assert.AreEqual(
            appliedBeforeCommit,
            (float)targetsType.GetField("targetHdgDeg", InstanceFlags).GetValue(targets),
            0.01f
        );

        rotaryType.GetMethod("CommitPendingValue", InstanceFlags).Invoke(heading, null);
        Assert.AreEqual(
            123f,
            (float)targetsType.GetField("targetHdgDeg", InstanceFlags).GetValue(targets),
            0.01f
        );

        GameObject navButtonObject = GameObject.Find(
            "iPad/MainCanvas/RotaryControls/Rotary_Control_Panel/Rotary_Control_Panel_Image/Rotary_NAV_Reengage"
        );
        Assert.NotNull(navButtonObject);
        navButtonObject.GetComponent<Button>().onClick.Invoke();
        Assert.IsTrue((bool)navType.GetField("navEngaged", InstanceFlags).GetValue(nav));
    }

    [UnityTest]
    public IEnumerator DisengagedNavStillSequencesTheActiveRoute()
    {
        Type planType = RequireType("FlightPlan");
        Type targetsType = RequireType("SimTargets");
        Type navType = RequireType("NavAutopilot");

        var root = new GameObject("DisengagedNavSequenceTest");
        root.SetActive(false);
        Component plan = root.AddComponent(planType);
        Component targets = root.AddComponent(targetsType);
        Component nav = root.AddComponent(navType);

        Transform first = new GameObject("WP_FIRST").transform;
        Transform second = new GameObject("WP_SECOND").transform;
        Transform third = new GameObject("WP_THIRD").transform;
        first.position = Vector3.zero;
        second.position = new Vector3(0f, 0f, 10f);
        third.position = new Vector3(0f, 0f, 20f);
        root.transform.position = new Vector3(0f, 0f, 11f);

        planType.GetField("waypoints", InstanceFlags)
            .SetValue(plan, new[] { first, second, third });
        navType.GetField("plan", InstanceFlags).SetValue(nav, plan);
        navType.GetField("targets", InstanceFlags).SetValue(nav, targets);
        navType.GetField("aircraft", InstanceFlags).SetValue(nav, root.transform);
        navType.GetField("activeIndex", InstanceFlags).SetValue(nav, 1);

        root.SetActive(true);
        navType.GetMethod("SetNavEngaged", InstanceFlags).Invoke(nav, new object[] { false });
        navType.GetMethod("FixedUpdate", InstanceFlags).Invoke(nav, null);

        Assert.AreEqual(2, (int)navType.GetField("activeIndex", InstanceFlags).GetValue(nav));
        Assert.IsFalse((bool)navType.GetField("navEngaged", InstanceFlags).GetValue(nav));

        UnityEngine.Object.Destroy(root);
        UnityEngine.Object.Destroy(first.gameObject);
        UnityEngine.Object.Destroy(second.gameObject);
        UnityEngine.Object.Destroy(third.gameObject);
        yield return null;
    }

    private static Type RequireType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.NotNull(type, $"Required runtime type '{name}' was not found.");
        return type;
    }
}
