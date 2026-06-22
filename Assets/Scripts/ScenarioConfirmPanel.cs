using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenarioConfirmPanel : MonoBehaviour
{
    [SerializeField]
    private TMP_Text descriptionText;

    [SerializeField]
    private string masterSceneName = "Master_FMS";

    [Header("Back Navigation")]
    [SerializeField]
    private GameObject hideOnNo;

    [SerializeField]
    private GameObject showOnNo;

    public void Show(ScenarioDefinition scenario)
    {
        if (scenario == null)
        {
            if (descriptionText)
                descriptionText.text = "No scenario selected.";
            return;
        }

        if (descriptionText)
        {
            descriptionText.text = string.IsNullOrEmpty(scenario.scenarioDescription)
                ? scenario.name
                : scenario.scenarioDescription;
        }

        gameObject.SetActive(true);
    }

    public void OnYes()
    {
        if (ScenarioSelection.Instance)
            ScenarioSelection.Instance.ConfirmPending();

        SceneManager.LoadScene(masterSceneName);
    }

    public void OnNo()
    {
        if (ScenarioSelection.Instance)
            ScenarioSelection.Instance.ClearPending();

        gameObject.SetActive(false);

        if (hideOnNo)
            hideOnNo.SetActive(false);

        if (showOnNo)
            showOnNo.SetActive(true);
    }
}
