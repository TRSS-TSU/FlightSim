using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HowToPanel_Toggle : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string isOpenParam = "isOpen";

    [Header("Scroll Reset")]
    [SerializeField]
    private ScrollRect scrollRect;

    [SerializeField]
    private float closeAnimDelay = 0.3f; // match your close anim

    [SerializeField]
    private string masterSceneName = "Master_FMS";

    [SerializeField]
    private ScenarioDefinition previewScenario;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void Open()
    {
        animator.SetBool(isOpenParam, true);
    }

    public void Close()
    {
        animator.SetBool(isOpenParam, false);
        StartCoroutine(ResetScrollAfterClose());
    }

    public void EnterPreview()
    {
        if (!previewScenario)
        {
            Debug.LogWarning("[HowToPanel] Preview scenario is not assigned.");
            return;
        }

        ScenarioRuntime.BeginPreview(previewScenario);
        SceneManager.LoadScene(masterSceneName);
    }

    private IEnumerator ResetScrollAfterClose()
    {
        yield return new WaitForSeconds(closeAnimDelay);

        if (scrollRect == null)
            yield break;

        scrollRect.StopMovement();
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f; // top
    }
}
