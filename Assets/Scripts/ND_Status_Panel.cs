using TMPro;
using UnityEngine;

public class ND_Status_Panel : MonoBehaviour
{
    [Header("ND Status Text Fields")]
    [SerializeField]
    private TMP_Text statusLine1;

    [SerializeField]
    private TMP_Text statusLine2;

    [Header("Default Text (Startup)")]
    [SerializeField]
    private string defaultLine1 = "ROLL AP ALT";

    [SerializeField]
    private string defaultLine2 = "\u2190"; // LEFTWARDS ARROW

    private void Awake()
    {
        ApplyDefaults();
    }

    private void ApplyDefaults()
    {
        if (statusLine1 != null)
            statusLine1.text = defaultLine1;

        if (statusLine2 != null)
            statusLine2.text = defaultLine2;
    }

    public void SetStatus(string line1, string line2)
    {
        if (statusLine1 != null)
            statusLine1.text = line1;

        if (statusLine2 != null)
            statusLine2.text = line2;
    }

    public void SetLine1(string text)
    {
        if (statusLine1 != null)
            statusLine1.text = text;
    }

    public void SetLine2(string text)
    {
        if (statusLine2 != null)
            statusLine2.text = text;
    }
}
