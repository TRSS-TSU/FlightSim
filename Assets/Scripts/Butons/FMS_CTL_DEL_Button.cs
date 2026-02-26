using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// CDU CLR/DEL button.
/// Tap  → backspace one character (CLR).
/// Hold → clear entire scratchpad (DEL).
/// Routes through FmsScratchpad instead of writing to TMP directly.
/// Wire the <see cref="scratchpad"/> reference in the Inspector.
/// </summary>
public class FMS_CLR_DEL_Button : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private FmsScratchpad scratchpad;

    [Header("CLR / DEL Timing")]
    [SerializeField] private float holdTimeSeconds = 0.6f;

    private bool      isPressing;
    private bool      delFired;
    private Coroutine holdRoutine;

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressing  = true;
        delFired    = false;
        holdRoutine = StartCoroutine(HoldToDelete());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        CleanupHold();
        if (!delFired)
            scratchpad?.Delete();   // CLR on tap
    }

    public void OnPointerExit(PointerEventData eventData) => CleanupHold();

    private IEnumerator HoldToDelete()
    {
        yield return new WaitForSecondsRealtime(holdTimeSeconds);
        if (!isPressing) yield break;

        scratchpad?.ClearAll();     // DEL on hold
        delFired = true;
    }

    private void CleanupHold()
    {
        isPressing = false;
        if (holdRoutine != null)
        {
            StopCoroutine(holdRoutine);
            holdRoutine = null;
        }
    }
}
