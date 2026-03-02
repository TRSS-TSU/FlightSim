using UnityEngine;

/// <summary>
/// CDU ACT FPLN page — read-only scrollable overview of the full active route.
/// Shows 5 waypoints per page (lines 1–5); line 6 is always the IDX shortcut.
/// Use PREV/NEXT to scroll through the route.
///
/// Colour convention:
///   Past legs   → cyan  (#00FFFF)
///   Active leg  → green (#00FF00)
///   Future legs → white (default)
///
/// No scratchpad interactions — editing is done on the ACT LEGS page.
/// </summary>
public class ActFplnView : FmsPageView, IMultiPage
{
    private const int WPTS_PER_PAGE = 5;
    private int _pageIndex;

    private int TotalPages =>
        Mathf.Max(1, Mathf.CeilToInt(Model.ActiveRoute.Count / (float)WPTS_PER_PAGE));

    public override void Populate()
    {
        GetTitle()?.SetText("ACT FPLN");
        GetPageNumber()?.SetText($"{_pageIndex + 1}/{TotalPages}");

        ClearAllLines();

        int start = _pageIndex * WPTS_PER_PAGE;

        for (int slot = 0; slot < WPTS_PER_PAGE; slot++)
        {
            int routeIdx = start + slot;
            int line     = slot + 1;   // lines 1–5

            if (routeIdx >= Model.ActiveRoute.Count)
            {
                SetLine(line, "\u2014", "", "", "");
                continue;
            }

            var  wp     = Model.ActiveRoute[routeIdx];
            bool past   = routeIdx < Model.ActiveLegIndex;
            bool active = routeIdx == Model.ActiveLegIndex;

            string col    = past   ? "<color=#00FFFF>"
                          : active ? "<color=#00FF00>"
                          :          "";
            string colEnd = (past || active) ? "</color>" : "";

            // Show sequential number and ident
            string label = $"{col}{routeIdx + 1}. {wp.ident}{colEnd}";

            // For the active leg, show live distance; otherwise just show lat/lon indicator
            string value = active
                ? $"{Model.DistNm:0.0}NM"
                : "";

            SetLine(line, label, value, "", "");
        }

        SetLine(6, "<IDX", "", "", "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (row == 6) Router.ShowPage("Index");
    }

    public void NextPage() => _pageIndex = (_pageIndex + 1) % TotalPages;
    public void PrevPage() => _pageIndex = (_pageIndex - 1 + TotalPages) % TotalPages;
}
