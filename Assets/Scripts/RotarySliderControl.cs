using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class RotarySliderControl
    : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
{
    public enum TargetKind
    {
        Heading,
        Altitude
    }

    [Header("Target")]
    [SerializeField] private TargetKind targetKind;
    [SerializeField] private SimTargets targets;
    [SerializeField] private NavAutopilot nav;

    [Header("UI")]
    [SerializeField] private GameObject adjustmentPopup;
    [SerializeField] private Slider slider;
    [SerializeField] private RectTransform knobImage;
    [SerializeField] private TMP_Text pendingValueText;
    [SerializeField] private Image holdProgress;

    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float holdSeconds = 0.7f;
    [SerializeField, Min(1f)] private float stepSize = 1f;
    [SerializeField] private float minimumKnobAngle = -135f;
    [SerializeField] private float maximumKnobAngle = 135f;

    private float pendingValue;
    private float heldSeconds;
    private bool adjustmentOpen;
    private bool holding;
    private bool commitFired;

    private void Awake()
    {
        ResolveReferences();
        ConfigureSlider();
        slider?.onValueChanged.AddListener(OnSliderValueChanged);
        SetHoldProgress(0f);
        adjustmentPopup?.SetActive(false);
    }

    private void OnDestroy() => slider?.onValueChanged.RemoveListener(OnSliderValueChanged);

    private void OnDisable()
    {
        CancelCommitHold();
        adjustmentOpen = false;
        adjustmentPopup?.SetActive(false);
    }

    private void Update()
    {
        if (!holding || commitFired)
            return;

        heldSeconds += Time.unscaledDeltaTime;
        SetHoldProgress(heldSeconds / holdSeconds);
        if (heldSeconds >= holdSeconds)
            CommitPendingValue();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!adjustmentOpen)
        {
            OpenAdjustment();
            return;
        }

        heldSeconds = 0f;
        holding = true;
        commitFired = false;
        SetHoldProgress(0f);
    }

    public void OnPointerUp(PointerEventData eventData) => CancelCommitHold();

    public void OnPointerExit(PointerEventData eventData) => CancelCommitHold();

    public void OpenAdjustment()
    {
        ResolveReferences();
        if (!targets || !slider)
            return;

        nav?.SetNavEngaged(false);
        ConfigureSlider();
        pendingValue = AppliedValue;
        slider.SetValueWithoutNotify(pendingValue);
        adjustmentOpen = true;
        adjustmentPopup?.SetActive(true);
        RefreshPendingDisplay();
    }

    public void OnSliderValueChanged(float rawValue)
    {
        pendingValue = Quantize(rawValue);
        if (slider && !Mathf.Approximately(slider.value, pendingValue))
            slider.SetValueWithoutNotify(pendingValue);
        RefreshPendingDisplay();
    }

    public void CommitPendingValue()
    {
        if (!adjustmentOpen || commitFired || !targets)
            return;

        if (targetKind == TargetKind.Heading)
            targets.targetHdgDeg = Mathf.Repeat(pendingValue, 360f);
        else
            targets.targetAltFtMsl = Mathf.Clamp(pendingValue, targets.minAltFt, targets.maxAltFt);

        commitFired = true;
        holding = false;
        CloseAdjustment();
    }

    public void CloseAdjustment()
    {
        CancelCommitHold();
        adjustmentOpen = false;
        adjustmentPopup?.SetActive(false);
    }

    private float AppliedValue =>
        targetKind == TargetKind.Heading ? targets.targetHdgDeg : targets.targetAltFtMsl;

    private void ConfigureSlider()
    {
        if (!slider || !targets)
            return;

        slider.wholeNumbers = true;
        slider.minValue = targetKind == TargetKind.Heading ? targets.minHdgDeg : targets.minAltFt;
        slider.maxValue = targetKind == TargetKind.Heading
            ? Mathf.Max(slider.minValue, targets.maxHdgDeg - stepSize)
            : targets.maxAltFt;
    }

    private float Quantize(float value)
    {
        float rounded = Mathf.Round(value / stepSize) * stepSize;
        return slider ? Mathf.Clamp(rounded, slider.minValue, slider.maxValue) : rounded;
    }

    private void RefreshPendingDisplay()
    {
        if (pendingValueText)
        {
            int rounded = Mathf.RoundToInt(pendingValue);
            pendingValueText.text = targetKind == TargetKind.Heading
                ? $"HDG {rounded:000}\u00b0"
                : $"ALT {rounded:00000} FT";
        }

        if (knobImage && slider)
        {
            float normalized = Mathf.InverseLerp(slider.minValue, slider.maxValue, pendingValue);
            knobImage.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Lerp(minimumKnobAngle, maximumKnobAngle, normalized)
            );
        }
    }

    private void CancelCommitHold()
    {
        holding = false;
        heldSeconds = 0f;
        SetHoldProgress(0f);
    }

    private void SetHoldProgress(float progress)
    {
        if (holdProgress)
            holdProgress.fillAmount = Mathf.Clamp01(progress);
    }

    private void ResolveReferences()
    {
        if (!targets)
            targets = FindFirstObjectByType<SimTargets>();
        if (!nav)
            nav = FindFirstObjectByType<NavAutopilot>();
        if (!knobImage)
            knobImage = transform as RectTransform;
    }
}
