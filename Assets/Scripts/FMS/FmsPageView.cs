using TMPro;
using UnityEngine;

/// <summary>
/// Abstract base class for all CDU page scripts.
/// Attach a concrete subclass to each CDU page GameObject.
///
/// Expected hierarchy per page:
///   PageName/
///     Title_Line/
///       Title         (TMP_Text)
///       Page_Number   (TMP_Text)
///       Top_Border    (TMP_Text)
///     Body/
///       Body_Line_1/
///         Label_Left  (TMP_Text)
///         Value_Left  (TMP_Text)
///         Label_Right (TMP_Text)
///         Value_Right (TMP_Text)
///       Body_Line_2 … Body_Line_6  (same structure)
/// </summary>
public abstract class FmsPageView : MonoBehaviour
{
    // Set by FmsPageRouter.Register()
    protected FmsModel Model;
    protected FmsScratchpad Scratchpad;
    protected FmsPageRouter Router;

    /// <summary>Called once by FmsPageRouter after instantiation.</summary>
    public void Init(FmsModel model, FmsScratchpad scratchpad, FmsPageRouter router)
    {
        Model = model;
        Scratchpad = scratchpad;
        Router = router;
    }

    /// <summary>Called every frame while this page is the active page. Fill TMP fields here.</summary>
    public abstract void Populate();

    /// <summary>Called when the user presses an LSK adjacent to this page.</summary>
    /// <param name="side">0 = Left column, 1 = Right column.</param>
    /// <param name="row">1–6 from top to bottom.</param>
    public abstract void HandleLsk(int side, int row);

    // ─────────────────────────────────────────────────────────────────────────
    // Protected helpers
    // ─────────────────────────────────────────────────────────────────────────

    protected TMP_Text GetTitle() => transform.Find("Title_Line/Title")?.GetComponent<TMP_Text>();

    protected TMP_Text GetPageNumber() =>
        transform.Find("Title_Line/Page_Number")?.GetComponent<TMP_Text>();

    /// <summary>
    /// Returns the shared Message_Line TMP field under Shared_IO/Message_Line.
    /// Navigates from the FmsPageRouter's transform (CDU root).
    /// </summary>
    protected TMP_Text GetMessageLine() =>
        Router?.transform.Find("Shared_IO/Message_Line")?.GetComponent<TMP_Text>();

    /// <summary>Returns the four TMP fields for a given body line (1–6).</summary>
    protected (TMP_Text labelL, TMP_Text valueL, TMP_Text labelR, TMP_Text valueR) GetLine(
        int lineNumber
    )
    {
        Transform body = transform.Find("Body");
        if (!body)
            return (null, null, null, null);

        Transform line = body.Find($"Body_Line_{lineNumber}");
        if (!line)
            return (null, null, null, null);

        return (
            line.Find("Label_Left")?.GetComponent<TMP_Text>(),
            line.Find("Value_Left")?.GetComponent<TMP_Text>(),
            line.Find("Label_Right")?.GetComponent<TMP_Text>(),
            line.Find("Value_Right")?.GetComponent<TMP_Text>()
        );
    }

    /// <summary>Set only the label fields of a body line (does not touch values).</summary>
    protected void SetLineLabels(int lineNumber, string labelL = "", string labelR = "")
    {
        var (ll, _, lr, _) = GetLine(lineNumber);
        if (ll != null)
            ll.text = labelL ?? "";
        if (lr != null)
            lr.text = labelR ?? "";
    }

    /// <summary>Set only the value fields of a body line (does not touch labels).</summary>
    protected void SetLineValues(int lineNumber, string valueL = "", string valueR = "")
    {
        var (_, vl, _, vr) = GetLine(lineNumber);
        if (vl != null)
            vl.text = valueL ?? "";
        if (vr != null)
            vr.text = valueR ?? "";
    }

    protected void ClearLine(int lineNumber)
    {
        SetLineLabels(lineNumber, "", "");
        SetLineValues(lineNumber, "", "");
    }

    /// <summary>Clear only the value fields of a body line (does not touch labels).</summary>
    protected void ClearLineValues(int lineNumber) => SetLineValues(lineNumber, "", "");

    protected void ClearAllLines()
    {
        for (int i = 1; i <= 6; i++)
            ClearLine(i);
    }

    /// <summary>Clear only the value fields on all lines (does not touch labels).</summary>
    protected void ClearAllValues()
    {
        for (int i = 1; i <= 6; i++)
            ClearLineValues(i);
    }
}
