using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class ScenarioRuntimeBootstrap : MonoBehaviour
{
    [SerializeField]
    private ScenarioDefinition fallbackScenario;

    [SerializeField]
    private GameObject previewVisualRoot;

    private void Start()
    {
        if (previewVisualRoot)
            previewVisualRoot.SetActive(ScenarioRuntime.IsPreview);

        if (ScenarioRuntime.IsPreview)
        {
            Debug.Log("[ScenarioRuntimeBootstrap] Preview familiarization scenario active; fallback skipped.");
            return;
        }

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
