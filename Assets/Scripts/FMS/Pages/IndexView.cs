using UnityEngine;

/// <summary>
/// CDU INDEX page (main menu).
/// Each LSK navigates to the corresponding sub-page.
///
/// Layout:
///   L1  MCDU MENU         R1  GNSS POS&gt;  (stub)
///   L2  &lt;STATUS           R2  FREQUENCY&gt;
///   L3  &lt;POS INIT         R3  FIX&gt;       (stub)
///   L4  &lt;PERF INIT        R4  HOLD&gt;      (stub)
///   L5  &lt;GNSS CTL         R5  PROG&gt;
///   L6  &lt;FMS CTL          R6  SEC FPLN&gt;  (stub)
///
/// Formatting: labels cyan (#00FFFF), values white (#FFFFFF) via FmtLabel/FmtValue.
/// </summary>
public class IndexView : FmsPageView
{
    // ── Formatting helpers ───────────────────────────────────────────────────────
    private string FmtTitle()             => "INDEX";
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

        SetLineLabels(1, FmtLabel("MCDU MENU"), FmtLabel("GNSS POS>"));
        SetLineValues(1, "", "");

        SetLineLabels(2, FmtLabel("<STATUS"), FmtLabel("FREQUENCY>"));
        SetLineValues(2, "", "");

        SetLineLabels(3, FmtLabel("<POS INIT"), FmtLabel("FIX>"));
        SetLineValues(3, "", "");

        SetLineLabels(4, FmtLabel("<PERF INIT"), FmtLabel("HOLD>"));
        SetLineValues(4, "", "");

        SetLineLabels(5, FmtLabel("<GNSS CTL"), FmtLabel("PROG>"));
        SetLineValues(5, "", "");

        SetLineLabels(6, FmtLabel("<FMS CTL"), FmtLabel("SEC FPLN>"));
        SetLineValues(6, "", "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (side == 0)  // Left
        {
            switch (row)
            {
                case 1: Router.ShowPage("McduMenu");  break;
                case 2: Router.ShowPage("Status");    break;
                case 3: Router.ShowPage("PosInit");   break;
                case 4: Router.ShowPage("PerfInit");  break;
                case 5: Router.ShowPage("GnssCtl");   break;
                case 6: Router.ShowPage("FmsCtl");    break;
            }
        }
        else  // Right
        {
            switch (row)
            {
                case 1: Scratchpad.ShowMessage("NOT AVAILABLE"); break;
                case 2: Router.ShowPage("Frequency");            break;
                case 3: Router.ShowPage("Fix");                  break;
                case 4: Router.ShowPage("Hold");                 break;
                case 5: Router.ShowPage("Prog");                 break;
                case 6: Router.ShowPage("SecFpln");              break;
            }
        }
        // NOTE: Populate() is NOT called here — FmsPageRouter.Update() pumps it every frame.
    }
}
