using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class ScenarioRuntimeBootstrap : MonoBehaviour
{
    [SerializeField]
    private ScenarioDefinition fallbackScenario;

    private void Start()
    {
        if (ScenarioRuntime.Current)
            return;

        if (!fallbackScenario)
        {
            Debug.LogWarning("[ScenarioRuntimeBootstrap] No fallback scenario assigned.");
            return;
        }

        ScenarioRuntime.Set(fallbackScenario);
    }
}
