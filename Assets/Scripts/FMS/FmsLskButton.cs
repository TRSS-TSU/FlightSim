using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to each Line Select Key (LSK) button on the CDU panel.
/// Routes click events to the currently active page view.
/// Set <see cref="side"/> and <see cref="row"/> in the Inspector.
/// </summary>
public class FmsLskButton : MonoBehaviour, IPointerClickHandler
{
    [Header("LSK Identity")]
    [Tooltip("0 = Left column, 1 = Right column.")]
    [SerializeField] private int side;

    [Tooltip("Row 1 (top) through 6 (bottom).")]
    [SerializeField] private int row;

    [Header("Router")]
    [SerializeField] private FmsPageRouter router;

    public void OnPointerClick(PointerEventData eventData)
    {
        router?.CurrentPage?.HandleLsk(side, row);
    }
}
