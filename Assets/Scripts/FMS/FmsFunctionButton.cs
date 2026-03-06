using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to a hard function key button on the CDU panel (IDX, FPLN, LEGS, PREV, NEXT, etc.).
/// Set the <see cref="key"/> enum in the Inspector to identify which function this button performs.
/// </summary>
public class FmsFunctionButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private FmsKey key;

    [SerializeField]
    private FmsPageRouter router;

    public void OnPointerClick(PointerEventData eventData)
    {
        router?.HandleFunctionKey(key);
    }
}
