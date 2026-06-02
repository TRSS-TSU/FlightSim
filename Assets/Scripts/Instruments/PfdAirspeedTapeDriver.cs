using TMPro;
using UnityEngine;

public class PfdAirspeedTapeDriver : MonoBehaviour
{
    [Header("Data")]
    public FlightDataBus bus;
    public SimTargets targets;

    [Header("Tape")]
    public RectTransform viewport;
    public RectTransform tapeStrip;
    public RectTransform currentSpeedPointerBox;
    public RectTransform selectedSpeedBug;
    public RectTransform selectedSpeedTopReadout;

    [Header("Text")]
    public TMP_Text currentSpeedText;
    public TMP_Text selectedSpeedText;

    [Header("Scale")]
    [Tooltip("Vertical tape movement for each knot of indicated airspeed.")]
    public float pixelsPerKnot = 4f;

    [Tooltip("Tape-strip Y position that represents zero knots.")]
    public float tapeZeroIasY = 0f;

    [Tooltip("Higher values make the tape and selected-speed bug settle more quickly.")]
    public float smooth = 12f;

    [Header("Bug Clamp")]
    public float bugClampPaddingPx = 8f;

    float _smoothedTapeY;
    float _smoothedBugY;

    void Awake()
    {
        ResolveRefs();

        if (tapeStrip)
            _smoothedTapeY = tapeStrip.anchoredPosition.y;
        if (selectedSpeedBug)
            _smoothedBugY = selectedSpeedBug.anchoredPosition.y;
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
            ResolveRefs();
    }

    void Update()
    {
        if (!bus || !targets)
            return;

        float currentIasKt = Mathf.Max(0f, bus.iasKt);
        float selectedIasKt = Mathf.Max(0f, targets.targetIasKt);

        if (currentSpeedText)
            currentSpeedText.text = Mathf.RoundToInt(currentIasKt).ToString("000");
        if (selectedSpeedText)
            selectedSpeedText.text = Mathf.RoundToInt(selectedIasKt).ToString("000");

        float smoothing = 1f - Mathf.Exp(-Mathf.Max(0f, smooth) * Time.deltaTime);

        if (tapeStrip)
        {
            float targetTapeY = tapeZeroIasY - currentIasKt * pixelsPerKnot;
            _smoothedTapeY = Mathf.Lerp(_smoothedTapeY, targetTapeY, smoothing);
            tapeStrip.anchoredPosition = new Vector2(tapeStrip.anchoredPosition.x, _smoothedTapeY);
        }

        if (selectedSpeedBug && viewport)
        {
            float halfHeight = viewport.rect.height * 0.5f;
            float limit = Mathf.Max(0f, halfHeight - Mathf.Max(0f, bugClampPaddingPx));
            float targetBugY = (selectedIasKt - currentIasKt) * pixelsPerKnot;
            targetBugY = Mathf.Clamp(targetBugY, -limit, limit);

            _smoothedBugY = Mathf.Lerp(_smoothedBugY, targetBugY, smoothing);
            selectedSpeedBug.anchoredPosition =
                new Vector2(selectedSpeedBug.anchoredPosition.x, _smoothedBugY);
        }
    }

    void ResolveRefs()
    {
        if (!bus)
            bus = FindFirstObjectByType<FlightDataBus>();
        if (!targets && bus)
            targets = bus.targets;
        if (!targets)
            targets = FindFirstObjectByType<SimTargets>();

        if (!viewport)
            viewport = FindDescendantRect("Airspeed_Tape_Viewport");
        if (!tapeStrip)
            tapeStrip = FindDescendantRect("Airspeed_Tape_Strip");
        if (!currentSpeedPointerBox)
            currentSpeedPointerBox = FindDescendantRect("CurrentSpeedPointer_Box");
        if (!selectedSpeedBug)
            selectedSpeedBug = FindDescendantRect("SelectedSpeedBug_OnTape");
        if (!selectedSpeedTopReadout)
            selectedSpeedTopReadout = FindDescendantRect("SelectedSpeedTopReadout");
        if (!currentSpeedText)
            currentSpeedText = FindDescendantText("CurrentSpeedText");
        if (!selectedSpeedText)
            selectedSpeedText = FindDescendantText("SelectedSpeedText");
    }

    RectTransform FindDescendantRect(string objectName)
    {
        Transform child = FindDescendant(transform, objectName);
        return child as RectTransform;
    }

    TMP_Text FindDescendantText(string objectName)
    {
        Transform child = FindDescendant(transform, objectName);
        return child ? child.GetComponent<TMP_Text>() : null;
    }

    static Transform FindDescendant(Transform parent, string objectName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == objectName)
                return child;

            Transform match = FindDescendant(child, objectName);
            if (match)
                return match;
        }

        return null;
    }
}
