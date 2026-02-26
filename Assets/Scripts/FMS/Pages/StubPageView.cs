using UnityEngine;

/// <summary>
/// Generic "not yet implemented" page.
/// Attach to: ModLegs, ActFpln, GnssCtl, VordmeCtl, FmsCtl, Fix, Hold, SecFpln, DepArr, Exec.
/// Set <see cref="pageTitle"/> in the Inspector to match the page name (e.g. "GNSS CTL").
/// LSK L6 always returns to the Index page.
/// </summary>
public class StubPageView : FmsPageView
{
    [SerializeField] private string pageTitle = "PAGE";

    public override void Populate()
    {
        GetTitle()?.SetText(pageTitle);
        GetPageNumber()?.SetText("1/1");

        ClearAllLines();
        SetLine(3, "", "NOT IMPLEMENTED", "", "");
        SetLine(6, "<IDX", "", "", "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (side == 0 && row == 6) Router.ShowPage("Index");
    }
}
