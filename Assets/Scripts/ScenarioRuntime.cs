using System;
using UnityEngine;

public static class ScenarioRuntime
{
    public static ScenarioDefinition Current { get; private set; }
    public static bool IsPreview { get; private set; }
    public static event Action<ScenarioDefinition> OnChanged;

    public static void Set(ScenarioDefinition scenario)
    {
        IsPreview = false;
        Current = scenario;

        if (scenario != null)
        {
            if (!ScenarioDefinitionValidator.Validate(scenario, out var rep))
                Debug.LogError(rep);
        }

        OnChanged?.Invoke(Current);
    }

    public static void BeginPreview(ScenarioDefinition scenario)
    {
        IsPreview = true;
        Current = scenario;

        if (scenario != null && !ScenarioDefinitionValidator.Validate(scenario, out var rep))
            Debug.LogError(rep);

        OnChanged?.Invoke(Current);
    }

    public static void EndPreview()
    {
        if (!IsPreview)
            return;

        IsPreview = false;
        Current = null;
        OnChanged?.Invoke(null);
    }
}
