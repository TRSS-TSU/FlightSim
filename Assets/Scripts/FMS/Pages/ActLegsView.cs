using System;
using UnityEngine;

/// <summary>
/// CDU ACT LEGS page — active route leg display and editing.
/// Shows 3 waypoints per page (each occupying 2 body lines). PREV/NEXT pages through the route.
///
/// Per waypoint (2 lines):
///   Line N  : Label_Left=[ident]  Value_Left=[BRG]°  Label_Right=ETE  Value_Right=[mm:ss]
///   Line N+1: Label_Left=""       Value_Left=[dist]NM Label_Right=""   Value_Right=""
///
/// Colour convention (TMP rich text):
///   Past legs   → cyan  (#00FFFF)
///   Active leg  → green (#00FF00)
///   Future legs → white (default)
///
/// Scratchpad interactions:
///   LSK L1/L3/L5 (empty SP)  → copy waypoint ident to scratchpad
///   LSK L1/L3/L5 (SP=ident)  → insert waypoint before that row
///   LSK L1/L3/L5 (SP=DELETE) → remove waypoint at that row
///   LSK L6                    → return to Index
/// </summary>
public class ActLegsView : FmsPageView, IMultiPage
{
    private const int LEGS_PER_PAGE = 3;
    private int _pageIndex;

    private int TotalPages =>
        Mathf.Max(1, Mathf.CeilToInt(Model.ActiveRoute.Count / (float)LEGS_PER_PAGE));

    public override void Populate()
    {
        GetTitle()?.SetText("ACT LEGS");
        GetPageNumber()?.SetText($"{_pageIndex + 1}/{TotalPages}");

        ClearAllLines();

        int start = _pageIndex * LEGS_PER_PAGE;

        for (int slot = 0; slot < LEGS_PER_PAGE; slot++)
        {
            int routeIdx = start + slot;
            int lineA    = slot * 2 + 1;  // lines 1, 3, 5
            int lineB    = lineA + 1;      // lines 2, 4, 6

            // Line 6 on the last slot is reserved for IDX — skip if we're on the last page's last slot
            if (lineA == 5 && lineB == 6 && slot == LEGS_PER_PAGE - 1)
            {
                // Still show waypoint on line 5, but line 6 goes to IDX
            }

            if (routeIdx >= Model.ActiveRoute.Count)
            {
                SetLineLabels(lineA, "\u2014", "");
                SetLineValues(lineA, "", "");
                if (lineB < 6) ClearLine(lineB);
                continue;
            }

            var wp     = Model.ActiveRoute[routeIdx];
            bool past   = routeIdx < Model.ActiveLegIndex;
            bool active = routeIdx == Model.ActiveLegIndex;

            // Colour tag
            string col = past   ? "<color=#00FFFF>"
                       : active ? "<color=#00FF00>"
                       :          "";
            string colEnd = (past || active) ? "</color>" : "";

            // Live data for the active leg; static for others
            string brgStr  = "\u2014";
            string distStr = "\u2014";
            string eteStr  = "--:--";

            if (active)
            {
                float distNm = Model.DistM / 1852f;
                brgStr  = $"{Model.BrgDeg:000}\u00B0";
                distStr = $"{distNm:0.0}NM";
                eteStr  = Model.FormatEte(distNm, Model.IasKt);
            }

            SetLineLabels(lineA, labelL: $"{col}{wp.ident}{colEnd}", labelR: "ETE");
            SetLineValues(lineA, valueL: brgStr, valueR: eteStr);

            if (lineB <= 6)
            {
                SetLineLabels(lineB, "", "");
                SetLineValues(lineB, distStr, "");
            }
        }

        // Line 6 always shows IDX shortcut
        SetLineLabels(6, "<IDX", "");
        SetLineValues(6, "", "");
    }

    public override void HandleLsk(int side, int row)
    {
        // Only Left LSKs 1, 3, 5 map to waypoints; L6 is IDX
        if (side == 0 && row == 6) { Router.ShowPage("Index"); return; }
        if (side != 0)             return;

        int slotFromRow = row == 1 ? 0 : row == 3 ? 1 : row == 5 ? 2 : -1;
        if (slotFromRow < 0) return;

        int routeIdx = _pageIndex * LEGS_PER_PAGE + slotFromRow;

        string sp = Scratchpad.CurrentText;

        if (sp.Length == 0)
        {
            // Copy ident to scratchpad
            if (routeIdx < Model.ActiveRoute.Count)
                Scratchpad.Append(Model.ActiveRoute[routeIdx].ident);
            return;
        }

        if (string.Equals(sp, "DELETE", StringComparison.OrdinalIgnoreCase))
        {
            if (routeIdx < Model.ActiveRoute.Count && Model.ActiveRoute.Count > 1)
            {
                Model.ActiveRoute.RemoveAt(routeIdx);
                Scratchpad.ReadAndClear();
                CommitRoute();
                _pageIndex = Mathf.Clamp(_pageIndex, 0, TotalPages - 1);
            }
            else Scratchpad.ShowMessage("NOT ALLOWED");
            return;
        }

        // Attempt to insert the typed waypoint ident before routeIdx
        var scenario = Model.Scenario;
        if (scenario == null) { Scratchpad.ShowMessage("NO SCENARIO"); return; }

        var wpDef = scenario.waypoints.Find(w =>
            string.Equals(w.ident, sp, StringComparison.OrdinalIgnoreCase));

        if (wpDef == null) { Scratchpad.ShowMessage("NOT IN DATABASE"); return; }

        int insertAt = Mathf.Clamp(routeIdx, 0, Model.ActiveRoute.Count);
        Model.ActiveRoute.Insert(insertAt, wpDef);
        Scratchpad.ReadAndClear();
        CommitRoute();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Route commit — rebuilds FlightPlan waypoints and resets NavAutopilot index
    // ─────────────────────────────────────────────────────────────────────────

    private void CommitRoute()
    {
        var fp = Router.GetFlightPlan();
        var sd = Model.Scenario;
        if (fp && sd)
            fp.RebuildRoute(Model.ActiveRoute, sd.centerLatDeg, sd.centerLonDeg, sd.baseZoom);

        var nav = Router.GetNavAutopilot();
        if (nav)
            nav.activeIndex = Mathf.Clamp(Model.ActiveLegIndex, 0, Model.ActiveRoute.Count - 1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IMultiPage
    // ─────────────────────────────────────────────────────────────────────────

    public void NextPage() => _pageIndex = (_pageIndex + 1) % TotalPages;
    public void PrevPage() => _pageIndex = (_pageIndex - 1 + TotalPages) % TotalPages;
}
