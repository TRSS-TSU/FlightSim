/// <summary>
/// CDU DEP ARR page — departure and arrival airport display.
/// Display-only for Scenario 01 (KNPA → KNPA). No scratchpad interactions.
///
/// Layout:
///   L1  DEP  [KNPA]          R1  ARR  [KNPA]
///   L2  PROC  RWYS 25L/R
///   L3  HDG   220°
///   L4  ALT   3,000 FT
///   L5  (empty)
///   L6  &lt;IDX
///
/// Formatting: labels cyan (#00FFFF), values white (#FFFFFF) via FmtLabel/FmtValue.
/// </summary>
public class DepArrView : FmsPageView
{
    // ── Formatting helpers ───────────────────────────────────────────────────────
    private string FmtTitle()             => "DEP / ARR";
    private string FmtLabel(string label) => string.IsNullOrEmpty(label) ? label : $"<color=#00FFFF>{label}</color>";
    private string FmtValue(string value) => string.IsNullOrEmpty(value) ? value : $"<color=#FFFFFF>{value}</color>";

    // ─────────────────────────────────────────────────────────────────────────
    // FmsPageView contract
    // ─────────────────────────────────────────────────────────────────────────

    public override void Populate()
    {
        ClearAllLines();
        GetTitle()?.SetText(FmtTitle());
        GetPageNumber()?.SetText("1/1");
        GetMessageLine()?.SetText("");

        string dep = Model.OriginIdent.Length > 0 ? Model.OriginIdent : "----";
        string arr = Model.DestIdent.Length   > 0 ? Model.DestIdent   : "----";

        SetLineLabels(1, FmtLabel("DEP"), FmtLabel("ARR"));
        SetLineValues(1, FmtValue(dep), FmtValue(arr));

        SetLineLabels(2, FmtLabel("PROC"), "");
        SetLineValues(2, FmtValue("RWYS 25L/R"), "");

        SetLineLabels(3, FmtLabel("HDG"), "");
        SetLineValues(3, FmtValue("220\u00B0"), "");

        SetLineLabels(4, FmtLabel("ALT"), "");
        SetLineValues(4, FmtValue("3,000 FT"), "");

        SetLineLabels(5, "", "");
        SetLineValues(5, "", "");

        SetLineLabels(6, FmtLabel("<IDX"), "");
        SetLineValues(6, "", "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (side == 0)  // Left
        {
            switch (row)
            {
                case 1: // inactive
                    break;
                case 2: // inactive
                    break;
                case 3: // inactive
                    break;
                case 4: // inactive
                    break;
                case 5: // inactive
                    break;
                case 6: Router.ShowPage("Index"); break;
            }
        }
        else  // Right
        {
            switch (row)
            {
                case 1: // inactive
                    break;
                case 2: // inactive
                    break;
                case 3: // inactive
                    break;
                case 4: // inactive
                    break;
                case 5: // inactive
                    break;
                case 6: // inactive
                    break;
            }
        }
        // NOTE: Populate() is NOT called here — FmsPageRouter.Update() pumps it every frame.
    }
}
