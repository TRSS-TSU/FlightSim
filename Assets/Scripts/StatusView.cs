using UnityEngine;

/// <summary>
/// CDU STATUS page — shows nav database info and UTC clock.
/// Display-only; L6 returns to Index, R6 navigates to PosInit.
///
/// Layout:
///   L1  NAV DATA          [ident]
///   L2  ACTIVE DATA BASE  [date range]
///   L3  SEC DATA BASE     [date range]
///   L4  UTC  [HH:MM]      DATE  [DD-MMM-YY]
///   L5  PROGRAM           [id]
///   L6  &lt;INDEX            POS INIT&gt;
///
/// Formatting: labels cyan (#00FFFF), values white (#FFFFFF) via FmtLabel/FmtValue.
/// </summary>
public class StatusView : FmsPageView
{
    // ── Formatting helpers ───────────────────────────────────────────────────────
    private string FmtTitle() => "STATUS";

    private string FmtLabel(string label) =>
        string.IsNullOrEmpty(label) ? label : $"<color=#00FFFF>{label}</color>";

    private string FmtValue(string value) =>
        string.IsNullOrEmpty(value) ? value : $"<color=#FFFFFF>{value}</color>";

    // ─────────────────────────────────────────────────────────────────────────
    // FmsPageView contract
    // ─────────────────────────────────────────────────────────────────────────

    public override void Populate()
    {
        ClearAllLines();
        GetTitle()?.SetText(FmtTitle());
        GetPageNumber()?.SetText("1/1");
        GetMessageLine()?.SetText("");

        string utc = System.DateTime.UtcNow.ToString("HH:mm");
        string date = System.DateTime.UtcNow.ToString("ddMMMyy").ToUpper();

        SetLineLabels(1, FmtLabel("NAV DATA"), "");
        SetLineValues(1, FmtValue(Model.NavDataIdent), "");

        SetLineLabels(2, FmtLabel("ACTIVE DATA BASE"), "");
        SetLineValues(2, FmtValue(Model.ActiveDbRange), "");

        SetLineLabels(3, FmtLabel("SEC DATA BASE"), "");
        SetLineValues(3, FmtValue(Model.SecDbRange), "");

        SetLineLabels(4, FmtLabel("UTC"), FmtLabel("DATE"));
        SetLineValues(4, FmtValue(utc), FmtValue(date));

        SetLineLabels(5, FmtLabel("PROGRAM"), "");
        SetLineValues(5, FmtValue(Model.ProgramId), "");

        SetLineLabels(6, FmtLabel("<INDEX"), FmtLabel("POS INIT>"));
        SetLineValues(6, "", "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (side == 0) // Left
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
                case 6:
                    Router.ShowPage("Index");
                    break;
            }
        }
        else // Right
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
                case 6:
                    Router.ShowPage("PosInit");
                    break;
            }
        }
        // NOTE: Populate() is NOT called here — FmsPageRouter.Update() pumps it every frame.
    }
}
