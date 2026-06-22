using System.Collections;
using UnityEngine;
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
