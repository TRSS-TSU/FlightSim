using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class McpStubUI : MonoBehaviour
{
    public SimTargets targets;

    public Slider speed;
    public Slider altitude;
    public Slider heading;

    public TMP_Text speedLabel;
    public TMP_Text altitudeLabel;
    public TMP_Text headingLabel;

    void Start()
    {
        if (!targets)
            return;

        // Initialize sliders from SimTargets (aviation units)
        speed.value = targets.targetIasKt;
        altitude.value = targets.targetAltFtMsl;
        heading.value = targets.targetHdgDeg;

        // Bind slider changes back into SimTargets
        speed.onValueChanged.AddListener(v =>
        {
            targets.targetIasKt = v;
            Refresh();
        });

        altitude.onValueChanged.AddListener(v =>
        {
            targets.targetAltFtMsl = v;
            Refresh();
        });

        heading.onValueChanged.AddListener(v =>
        {
            targets.targetHdgDeg = Mathf.Repeat(v, 360f);
            Refresh();
        });

        Refresh();
    }

    void Refresh()
    {
        if (!targets)
            return;

        if (speedLabel)
            speedLabel.text = $"SPD {targets.targetIasKt:0}";
        if (altitudeLabel)
            altitudeLabel.text = $"ALT {targets.targetAltFtMsl:0}";
        if (headingLabel)
            headingLabel.text = $"HDG {targets.targetHdgDeg:0}";
    }
}
