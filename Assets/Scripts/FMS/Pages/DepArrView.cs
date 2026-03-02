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

        SetLine(1, "DEP",  dep,          "ARR",  arr);
        SetLine(2, "PROC", "RWYS 25L/R", "",     "");
        SetLine(3, "HDG",  "220\u00B0",  "",     "");
        SetLine(4, "ALT",  "3,000 FT",  "",     "");
        SetLine(5, "",     "",           "",     "");
        SetLine(6, "<IDX", "",           "",     "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (row == 6) Router.ShowPage("Index");
    }
}
