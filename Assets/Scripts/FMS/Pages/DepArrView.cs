/// <summary>
/// CDU DEP ARR page — departure and arrival airport display.
/// Display-only for Scenario 01 (KNPA → KNPA). No scratchpad interactions.
///
/// Layout:
///   L1  DEP      [KNPA]          R1  ARR      [KNPA]
///   L2  PROC     RWYS 25L/R
///   L3  HDG      220°
///   L4  ALT      3,000 FT
///   L5
///   L6  &lt;IDX
/// </summary>
public class DepArrView : FmsPageView
{
    public override void Populate()
    {
        GetTitle()?.SetText("DEP / ARR");
        GetPageNumber()?.SetText("1/1");

        string dep  = Model.OriginIdent.Length > 0 ? Model.OriginIdent : "----";
        string arr  = Model.DestIdent.Length   > 0 ? Model.DestIdent   : "----";

        SetLineLabels(1, "DEP", "ARR");
        SetLineValues(1, dep, arr);

        SetLineLabels(2, "PROC", "");
        SetLineValues(2, "RWYS 25L/R", "");

        SetLineLabels(3, "HDG", "");
        SetLineValues(3, "220\u00B0", "");

        SetLineLabels(4, "ALT", "");
        SetLineValues(4, "3,000 FT", "");

        SetLineLabels(5, "", "");
        SetLineValues(5, "", "");

        SetLineLabels(6, "<IDX", "");
        SetLineValues(6, "", "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (row == 6) Router.ShowPage("Index");
    }
}
