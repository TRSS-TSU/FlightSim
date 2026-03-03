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

        SetLine(1, "NAV DATA", Model.NavDataIdent, "", "");
        SetLine(2, "ACTIVE DATA BASE", Model.ActiveDbRange, "", "");
        SetLine(3, "SEC DATA BASE", Model.SecDbRange, "", "");
        SetLine(4, "UTC", utc, "DATE", date);
        SetLine(5, "PROGRAM", Model.ProgramId, "", "");
        SetLine(6, "<INDEX", "", "POS INIT>", "");
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
