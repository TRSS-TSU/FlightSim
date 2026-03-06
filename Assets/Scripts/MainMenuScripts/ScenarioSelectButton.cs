using UnityEngine;

public class ScenarioSelectButton : MonoBehaviour
{
    [SerializeField]
    private ScenarioDefinition scenario;

    [SerializeField]
    private ScenarioConfirmPanel confirmPanel;

    public void Select()
    {
        ScenarioSelection.Instance.SetPending(scenario);
        confirmPanel.Show(scenario);
    }
}
