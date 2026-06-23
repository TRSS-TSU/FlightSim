using System;
using UnityEngine;

public static class ScenarioRuntime
{
    public static ScenarioDefinition Current { get; private set; }
    public static event Action<ScenarioDefinition> OnChanged;

    public static void Set(ScenarioDefinition scenario)
    {
        Current = scenario;

        if (scenario != null)
        {
            if (!ScenarioDefinitionValidator.Validate(scenario, out var rep))
                Debug.LogError(rep);
        }

        OnChanged?.Invoke(Current);
    }
}
