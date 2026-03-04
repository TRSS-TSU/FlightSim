using UnityEngine;

/// <summary>
/// CDU INDEX page (main menu).
/// Each LSK navigates to the corresponding sub-page.
///
/// Layout:
///   L1 STATUS       R1 GNSS POS  (stub)
///   L2 POS INIT     R2 FREQUENCY
///   L3 PERF INIT    R3 FIX       (stub)
///   L4 GNSS CTL     R4 HOLD      (stub)
///   L5 FMS CTL      R5 PROG
///   L6 —            R6 SEC FPLN  (stub)
/// </summary>
public class IndexView : FmsPageView
{
    public override void Populate()
    {
        GetTitle()?.SetText("INDEX");
        GetPageNumber()?.SetText("1/1");

        SetLineLabels(1, "MCDU MENU", "GNSS POS>");
        SetLineValues(1, "", "");

        SetLineLabels(2, "<STATUS", "FREQUENCY>");
        SetLineValues(2, "", "");

        SetLineLabels(3, "<POS INIT", "FIX>");
        SetLineValues(3, "", "");

        SetLineLabels(4, "<PERF INIT", "HOLD>");
        SetLineValues(4, "", "");

        SetLineLabels(5, "<GNSS CTL", "PROG>");
        SetLineValues(5, "", "");

        SetLineLabels(6, "<FMS CTL", "SEC FPLN>");
        SetLineValues(6, "", "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (side == 0) // Left
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
        else // Right
        {
            switch (row)
            {
                case 1: Scratchpad.ShowMessage("NOT AVAILABLE");   break;
                case 2: Router.ShowPage("Frequency");              break;
                case 3: Router.ShowPage("Fix");                    break;
                case 4: Router.ShowPage("Hold");                   break;
                case 5: Router.ShowPage("Prog");                   break;
                case 6: Router.ShowPage("SecFpln");                break;
            }
        }
    }
}
