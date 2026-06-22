using UnityEngine;

public class ScenarioSelection : MonoBehaviour
{
    public static ScenarioSelection Instance { get; private set; }
    public ScenarioDefinition SelectedScenario { get; private set; }
    public ScenarioDefinition PendingScenario { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetPending(ScenarioDefinition def) => PendingScenario = def;

    public void ConfirmPending()
    {
        SelectedScenario = PendingScenario;
        PendingScenario = null;

        ScenarioRuntime.Set(SelectedScenario);
    }

    public void ClearPending() => PendingScenario = null;
}
