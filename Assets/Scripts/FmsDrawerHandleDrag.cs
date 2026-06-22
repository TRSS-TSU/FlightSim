using UnityEngine;
using UnityEngine.EventSystems;

public class FmsDrawerHandleDrag
    : MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler
{
    [SerializeField]
    private FmsDrawerController controller;

    [SerializeField]
    private RectTransform panel; // a_FMS_Panel

    [SerializeField]
    private RectTransform panelParent; // parent of a_FMS_Panel (likely FMS)

    [SerializeField]
    private Canvas canvas; // MainCanvas

    [SerializeField]
    private float flickVelocityThreshold = 800f; // UI units/sec

    private Vector2 prevLocalPointer;
    private float prevTime;
    private float smoothedVy;

    private int activePointerId = int.MinValue;
    private float startPanelY;
    private Vector2 startLocalPointer;

    private float lastRawVy;
    private float drawerTravel;

    void Reset()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId != int.MinValue)
            return; // single pointer only

        if (!controller || !panel || !panelParent || !canvas)
        {
            Debug.LogError("FmsDrawerHandleDrag missing refs.", this);
            return;
        }

        activePointerId = eventData.pointerId;
        controller.RecalculateTargets(); // in case layout changed

        drawerTravel = Mathf.Abs(controller.OpenY - controller.ClosedY);

        startPanelY = panel.anchoredPosition.y;

        // Convert pointer to local point in panelParent space (camera-aware)
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelParent,
            eventData.position,
            canvas.worldCamera,
            out startLocalPointer
        );
        prevLocalPointer = startLocalPointer;
        prevTime = Time.unscaledTime;
        smoothedVy = 0f;

        eventData.Use(); // prevent accidental clicks
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelParent,
            eventData.position,
            canvas.worldCamera,
            out Vector2 local
        );

        float deltaY = local.y - startLocalPointer.y;
        controller.SetYImmediate(startPanelY + deltaY);

        float now = Time.unscaledTime;
        float dt = now - prevTime;

        if (dt > 0.0001f)
        {
            float vy = (local.y - prevLocalPointer.y) / dt;
            smoothedVy = Mathf.Lerp(smoothedVy, vy, 0.35f);
            lastRawVy = vy;
        }

        prevLocalPointer = local;
        prevTime = now;

        eventData.Use();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        activePointerId = int.MinValue;

        // Direction-aware flick: works for drawers opening up or down.
        float dirToOpen = Mathf.Sign(controller.OpenY - controller.ClosedY); // FMS:+1, MAP:-1
        float vOpenAxis = smoothedVy * dirToOpen;

        if (vOpenAxis > flickVelocityThreshold)
            controller.SnapOpen();
        else if (vOpenAxis < -flickVelocityThreshold)
            controller.SnapClosed();
        else
            controller.SnapToNearest();

        eventData.Use();
    }
}
