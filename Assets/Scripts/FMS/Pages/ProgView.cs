using UnityEngine;

/// <summary>
/// CDU PROGRESS page — live in-flight situational awareness (2-page view).
///
/// Page 1/2: Leg-by-leg progress (FROM / TO / DEST) and cross-track error.
///   L1 FROM  [last WP]     DIST  [nm to active]
///   L2 TO    [active WP]   ETE   [mm:ss]
///   L3 DEST  [last WP]     DIST  —
///   L4
///   L5 XTK   [±nm L/R]
///   L6 <IDX
///
/// Page 2/2: Current aircraft state snapshot.
///   L1 IAS   [kt]          HDG   [ddd°]
///   L2 ALT   [ft]          VSI   [±fpm]
///   L6 <IDX
///
/// All values update every frame from FmsModel (pumped by FmsPageRouter.Update).
/// </summary>
public class ProgView : FmsPageView, IMultiPage
{
    private int _page = 1;

    public override void Populate()
    {
        GetTitle()?.SetText("PROGRESS");
        GetPageNumber()?.SetText($"{_page}/2");

        ClearAllLines();

        if (_page == 1) PopulatePage1();
        else            PopulatePage2();
    }

    private void PopulatePage1()
    {
        var    route    = Model.ActiveRoute;
        int    active   = Model.ActiveLegIndex;

        string fromIdent = (active > 0 && active - 1 < route.Count)
                         ? route[active - 1].ident : "\u2014";
        string toIdent   = active < route.Count
                         ? route[active].ident     : "\u2014";
        string destIdent = route.Count > 0
                         ? route[route.Count - 1].ident : "\u2014";

        float  distNm  = Model.DistNm;
        string eteStr  = Model.FormatEte(distNm, Model.IasKt);
        string distStr = $"{distNm:0.0}NM";

        SetLine(1, "FROM", fromIdent, "DIST", distStr);
        SetLine(2, "TO",   toIdent,   "ETE",  eteStr);
        SetLine(3, "DEST", destIdent, "DIST", "\u2014");
        SetLine(4, "",     "",        "",     "");

        float  xtkNm  = Mathf.Abs(Model.XtkM) / 1852f;
        string xtkDir = Model.XtkM >= 0f ? "R" : "L";
        SetLine(5, "XTK", $"{xtkNm:0.00}NM {xtkDir}", "", "");
        SetLine(6, "<IDX","",                           "", "");
    }

    private void PopulatePage2()
    {
        SetLine(1, "IAS",   $"{Model.IasKt:0}KT",           "HDG",  $"{Model.HdgDeg:000}\u00B0");
        SetLine(2, "ALT",   $"{Model.AltFtMsl:0}FT",        "VSI",  $"{Model.VsiFpm:+0;-0}FPM");
        SetLine(3, "",      "",                              "",     "");
        SetLine(4, "",      "",                              "",     "");
        SetLine(5, "",      "",                              "",     "");
        SetLine(6, "<IDX",  "",                              "",     "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (row == 6) Router.ShowPage("Index");
    }

    public void NextPage() => _page = _page == 1 ? 2 : 1;
    public void PrevPage() => _page = _page == 2 ? 1 : 2;
}
