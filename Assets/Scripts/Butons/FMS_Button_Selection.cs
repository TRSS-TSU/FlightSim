using UnityEngine;

/// <summary>
/// CDU alphanumeric keypad button handler.
/// Routes all key input through FmsScratchpad instead of writing to TMP directly.
/// Wire the <see cref="scratchpad"/> reference in the Inspector (Shared_IO/FmsScratchpad).
/// </summary>
public class FMS_Button_Selection : MonoBehaviour
{
    [SerializeField] private FmsScratchpad scratchpad;

    public void OnKey(string key)
    {
        scratchpad?.Append(key);
    }

    public void OnDegree()    => scratchpad?.AppendRaw("\u00B0"); // °

    public void OnPlusMinus() => scratchpad?.TogglePlusMinus();
}
