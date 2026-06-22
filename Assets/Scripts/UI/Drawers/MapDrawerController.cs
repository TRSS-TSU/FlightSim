using System.Collections;
using TMPro;
using UnityEngine;

public class MapDrawerController : MonoBehaviour, IDrawerController
{
    [Header("Refs")]
    [SerializeField]
    private RectTransform panel; // MAP panel

    [SerializeField]
    private RectTransform openTarget; // OpenTarget

    [SerializeField]
    private RectTransform closedTarget; // CloseTarget

    [Header("UI")]
    [SerializeField]
    private TMP_Text stateLabel; // "Open" / "Close"

    [Header("Motion")]
    [SerializeField, Range(0.05f, 0.5f)]
    private float snapDuration = 0.2f;

    private RectTransform panelParent;
    private Coroutine snapCo;

    private bool isConsideredOpen;

    public float OpenY { get; private set; }
    public float ClosedY { get; private set; }

    private bool OpenIsHigher => OpenY > ClosedY; // false for MAP (opens downward in your setup)

    void Reset()
    {
        panel = GetComponent<RectTransform>();
    }

    void Awake()
    {
        if (!panel)
            panel = GetComponent<RectTransform>();
        panelParent = panel.parent as RectTransform;

        RecalculateTargets();

        // Deterministic startup UI intent (default Closed label).
        SetStateLabel(false);
        isConsideredOpen = false;
    }

    void OnRectTransformDimensionsChange()
    {
        if (panelParent)
            RecalculateTargets();
    }

    public void RecalculateTargets()
    {
        OpenY = GetTargetYInParentSpace(openTarget);
        ClosedY = GetTargetYInParentSpace(closedTarget);
    }

    private float GetTargetYInParentSpace(RectTransform target)
    {
        if (!target || !panelParent)
            return 0f;
        Vector2 local = panelParent.InverseTransformPoint(target.position);
        return local.y;
    }

    public float ClampY(float y)
    {
        float min = Mathf.Min(OpenY, ClosedY);
        float max = Mathf.Max(OpenY, ClosedY);
        return Mathf.Clamp(y, min, max);
    }

    public void SetYImmediate(float y)
    {
        Vector2 p = panel.anchoredPosition;
        p.y = ClampY(y);
        panel.anchoredPosition = p;

        float mid = (OpenY + ClosedY) * 0.5f;
        bool openNow = OpenIsHigher ? (p.y >= mid) : (p.y <= mid);

        if (openNow && !isConsideredOpen)
            DrawerGroup.RequestOpen(this);
        if (!openNow && isConsideredOpen)
            DrawerGroup.NotifyClosed(this);
        isConsideredOpen = openNow;
    }

    public void SnapToNearest()
    {
        float mid = (OpenY + ClosedY) * 0.5f;
        bool openNow = OpenIsHigher
            ? (panel.anchoredPosition.y >= mid)
            : (panel.anchoredPosition.y <= mid);
        if (openNow)
            SnapOpen();
        else
            SnapClosed();
    }

    public void SnapOpen()
    {
        SetStateLabel(true);
        DrawerGroup.RequestOpen(this);
        StartSnap(OpenY);
    }

    public void SnapClosed()
    {
        SetStateLabel(false);
        StartSnap(ClosedY);
    }

    private void StartSnap(float targetY)
    {
        if (snapCo != null)
            StopCoroutine(snapCo);
        snapCo = StartCoroutine(SnapRoutine(targetY));
    }

    private IEnumerator SnapRoutine(float targetY)
    {
        float startY = panel.anchoredPosition.y;
        targetY = ClampY(targetY);

        float t = 0f;
        while (t < snapDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / snapDuration);
            float y = Mathf.Lerp(startY, targetY, a);
            SetYImmediate(y);
            yield return null;
        }

        SetYImmediate(targetY);
        snapCo = null;
    }

    private void SetStateLabel(bool isOpen)
    {
        if (!stateLabel)
            return;
        stateLabel.text = isOpen ? "CLOSE" : "OPEN";
    }
}
