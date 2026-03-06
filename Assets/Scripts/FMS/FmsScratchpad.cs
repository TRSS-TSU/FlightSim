using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Single source of truth for the CDU scratchpad.
/// Attach to the Shared_IO GameObject and wire the TMP references in the Inspector.
/// FMS_Button_Selection and FMS_CTL_DEL_Button both call into this component.
/// </summary>
public class FmsScratchpad : MonoBehaviour
{
    [Header("TMP References")]
    [SerializeField]
    private TMP_Text scratchpadTMP;

    [Header("Rules")]
    [SerializeField]
    private int maxChars = 24;

    // ── Public state ────────────────────────────────────────────────────────────
    public string CurrentText { get; private set; } = "";

    // ── Internal ────────────────────────────────────────────────────────────────
    private string _savedText = "";
    private Coroutine _msgRoutine;

    // ─────────────────────────────────────────────────────────────────────────
    // Input methods (called by FMS_Button_Selection)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Append a character string, forcing uppercase. Ignores input during message display.</summary>
    public void Append(string s)
    {
        if (_msgRoutine != null)
            return;
        if (string.IsNullOrEmpty(s))
            return;
        if (CurrentText.Length >= maxChars)
            return;

        CurrentText += s.ToUpperInvariant();
        Refresh();
    }

    /// <summary>Append without uppercase conversion (for degree symbol, +/-).</summary>
    public void AppendRaw(string s)
    {
        if (_msgRoutine != null)
            return;
        if (string.IsNullOrEmpty(s))
            return;
        if (CurrentText.Length >= maxChars)
            return;

        CurrentText += s;
        Refresh();
    }

    /// <summary>Toggle the trailing +/- sign, or append + if none present.</summary>
    public void TogglePlusMinus()
    {
        if (_msgRoutine != null)
            return;

        if (CurrentText.Length == 0)
        {
            AppendRaw("+");
            return;
        }

        char last = CurrentText[CurrentText.Length - 1];
        if (last == '+')
            CurrentText = CurrentText.Substring(0, CurrentText.Length - 1) + "-";
        else if (last == '-')
            CurrentText = CurrentText.Substring(0, CurrentText.Length - 1) + "+";
        else
            AppendRaw("+");
        Refresh();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Delete methods (called by FMS_CTL_DEL_Button)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Backspace one character (CLR tap).</summary>
    public void Delete()
    {
        if (_msgRoutine != null)
            return;
        if (CurrentText.Length == 0)
            return;

        CurrentText = CurrentText.Substring(0, CurrentText.Length - 1);
        Refresh();
    }

    /// <summary>Clear the entire scratchpad (DEL hold).</summary>
    public void ClearAll()
    {
        if (_msgRoutine != null)
        {
            StopCoroutine(_msgRoutine);
            _msgRoutine = null;
        }
        CurrentText = "";
        Refresh();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Page-view methods
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns the current scratchpad text then clears it. Call from page views on LSK press.</summary>
    public string ReadAndClear()
    {
        string v = CurrentText;
        CurrentText = "";
        Refresh();
        return v;
    }

    /// <summary>
    /// Temporarily replace the scratchpad display with a system message.
    /// After <paramref name="duration"/> seconds the previous entry is restored.
    /// </summary>
    public void ShowMessage(string msg, float duration = 1.5f)
    {
        if (_msgRoutine != null)
            StopCoroutine(_msgRoutine);
        _savedText = CurrentText;
        _msgRoutine = StartCoroutine(MessageRoutine(msg, duration));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Internal
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator MessageRoutine(string msg, float duration)
    {
        if (scratchpadTMP)
            scratchpadTMP.text = msg;
        yield return new WaitForSecondsRealtime(duration);
        _msgRoutine = null;
        CurrentText = _savedText;
        Refresh();
    }

    private void Refresh()
    {
        if (scratchpadTMP)
            scratchpadTMP.text = CurrentText;
    }
}
