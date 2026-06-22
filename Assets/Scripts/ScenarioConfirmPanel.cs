using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenarioConfirmPanel : MonoBehaviour
{
    [SerializeField]
    private TMP_Text descriptionText;

    [SerializeField]
    private string masterSceneName = "Master_FMS";

    void Awake() => gameObject.SetActive(false);

    public void Show(ScenarioDefinition scenario)
    {
        if (scenario == null)
        {
            descriptionText.text = "No scenario selected.";
            return;
        }

        descriptionText.text = string.IsNullOrEmpty(scenario.scenarioDescription)
            ? scenario.name
            : scenario.scenarioDescription;

        gameObject.SetActive(true);
    }

    public void OnYes()
    {
        ScenarioSelection.Instance.ConfirmPending();
        SceneManager.LoadScene(masterSceneName);
    }

    public void OnNo()
    {
        ScenarioSelection.Instance.ClearPending();
        gameObject.SetActive(false);
    }
}
