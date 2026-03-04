using UnityEngine;

/// <summary>
/// CDU STATUS page — shows nav database info and UTC clock.
/// Display-only; LSK L6/R6 returns to Index.
///
/// Layout:
///   L1 NAV DATA       [ident]
///   L2 ACTIVE DATA BASE [date range]
///   L3                              UTC  [HH:MM:SS]
///   L4                              DATE [DD-MMM-YY]
///   L5 PROGRAM        [id]
///   L6 <IDX
/// </summary>
public class StatusView : FmsPageView
{
    public override void Populate()
    {
        GetTitle()?.SetText("STATUS");
        GetPageNumber()?.SetText("1/1");

        string utc  = System.DateTime.UtcNow.ToString("HH:mm");
        string date = System.DateTime.UtcNow.ToString("ddMMMyy").ToUpper();

        SetLineLabels(1, "NAV DATA", "");
        SetLineValues(1, Model.NavDataIdent, "");

        SetLineLabels(2, "ACTIVE DATA BASE", "");
        SetLineValues(2, Model.ActiveDbRange, "");

        SetLineLabels(3, "SEC DATA BASE", "");
        SetLineValues(3, Model.SecDbRange, "");

        SetLineLabels(4, "UTC", "DATE");
        SetLineValues(4, utc, date);

        SetLineLabels(5, "PROGRAM", "");
        SetLineValues(5, Model.ProgramId, "");

        SetLineLabels(6, "<INDEX", "POS INIT>");
        SetLineValues(6, "", "");
    }

    public override void HandleLsk(int side, int row)
    {
         if (side == 0) // Left
        {
            switch (row)
            {
                case 6: Router.ShowPage("Index");
                break;
            }
        }
        else // Right
        {
            switch (row)
            {
                case 6: Router.ShowPage("PosInit");
                break;
            }
        }
    }
}
