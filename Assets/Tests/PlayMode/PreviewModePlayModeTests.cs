using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class PreviewModePlayModeTests
{
    private static readonly Type RuntimeType = Type.GetType("ScenarioRuntime, Assembly-CSharp");

    [TearDown]
    public void TearDown() => RuntimeType.GetMethod("Set").Invoke(null, new object[] { null });

    [Test]
    public void PreviewKeepsItsScenarioAndNormalSelectionEndsPreview()
    {
        var scenarioType = Type.GetType("ScenarioDefinition, Assembly-CSharp");
        var scenario = ScriptableObject.CreateInstance(scenarioType);
        var set = RuntimeType.GetMethod("Set");
        var beginPreview = RuntimeType.GetMethod("BeginPreview");
        var isPreview = RuntimeType.GetProperty("IsPreview", BindingFlags.Static | BindingFlags.Public);
        var current = RuntimeType.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);

        set.Invoke(null, new object[] { scenario });
        beginPreview.Invoke(null, new object[] { scenario });

        Assert.That(isPreview.GetValue(null), Is.True);
        Assert.That(current.GetValue(null), Is.SameAs(scenario));

        set.Invoke(null, new object[] { scenario });

        Assert.That(isPreview.GetValue(null), Is.False);
        Assert.That(current.GetValue(null), Is.SameAs(scenario));

        UnityEngine.Object.DestroyImmediate(scenario);
    }
}
