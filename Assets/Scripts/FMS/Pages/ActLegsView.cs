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
/// Leg colour convention (TMP rich text applied directly to ident string):
///   Past legs   → cyan  (#00FFFF)
///   Active leg  → green (#00FF00)
///   Future legs → default (no tag; white by TMP default)
///
/// Note: FmtLabel/FmtValue are NOT applied to ident strings — per-leg color tags
/// take precedence and must not be wrapped in a global FmtLabel cyan tag.
/// FmtLabel/FmtValue are applied only to structural labels (ETE, IDX) and data values.
///
/// Scratchpad interactions:
///   L1/L3/L5 (empty SP)  → copy waypoint ident to scratchpad
///   L1/L3/L5 (SP=ident)  → insert waypoint before that row
///   L1/L3/L5 (SP=DELETE) → remove waypoint at that row
///   L6                    → return to Index
///
/// Formatting: structural labels cyan (#00FFFF), values white (#FFFFFF) via FmtLabel/FmtValue.
/// </summary>
public class ActLegsView : FmsPageView, IMultiPage
{
    // ── Formatting helpers ───────────────────────────────────────────────────────
    private string FmtTitle() => "ACT LEGS";

    private string FmtLabel(string label) =>
        string.IsNullOrEmpty(label) ? label : $"<color=#00FFFF>{label}</color>";

    private string FmtValue(string value) =>
        string.IsNullOrEmpty(value) ? value : $"<color=#FFFFFF>{value}</color>";

    // ── State ────────────────────────────────────────────────────────────────────
    private const int LEGS_PER_PAGE = 3;
    private int _pageIndex;

    private int TotalPages =>
        Mathf.Max(1, Mathf.CeilToInt(Model.ActiveRoute.Count / (float)LEGS_PER_PAGE));

    // ─────────────────────────────────────────────────────────────────────────
    // FmsPageView contract
    // ─────────────────────────────────────────────────────────────────────────

    public override void Populate()
    {
        ClearAllLines();
        GetTitle()?.SetText(FmtTitle());
        GetPageNumber()?.SetText($"{_pageIndex + 1}/{TotalPages}");
        GetMessageLine()?.SetText("");

        int start = _pageIndex * LEGS_PER_PAGE;

        for (int slot = 0; slot < LEGS_PER_PAGE; slot++)
        {
            int routeIdx = start + slot;
            int lineA = slot * 2 + 1; // lines 1, 3, 5
            int lineB = lineA + 1; // lines 2, 4, 6

            if (routeIdx >= Model.ActiveRoute.Count)
            {
                SetLineLabels(lineA, "\u2014", "");
                SetLineValues(lineA, "", "");
                if (lineB < 6)
                    ClearLine(lineB);
                continue;
            }

            var wp = Model.ActiveRoute[routeIdx];
            bool past = routeIdx < Model.ActiveLegIndex;
            bool active = routeIdx == Model.ActiveLegIndex;

            // Per-leg colour applied directly to ident string — NOT via FmtLabel
            string col =
                past ? "<color=#00FFFF>"
                : active ? "<color=#00FF00>"
                : "";
            string colEnd = (past || active) ? "</color>" : "";

            // Live data for the active leg; dashes for others
            string brgStr = "\u2014";
            string distStr = "\u2014";
            string eteStr = "--:--";

            if (active)
            {
                float distNm = Model.DistM / 1852f;
                brgStr = $"{Model.BrgDeg:000}\u00B0";
                distStr = $"{distNm:0.0}NM";
                eteStr = Model.FormatEte(distNm, Model.IasKt);
            }

            // Ident uses inline color tag directly; structural "ETE" label uses FmtLabel
            SetLineLabels(lineA, labelL: $"{col}{wp.ident}{colEnd}", labelR: FmtLabel("ETE"));
            SetLineValues(lineA, valueL: FmtValue(brgStr), valueR: FmtValue(eteStr));

            if (lineB <= 6)
            {
                SetLineLabels(lineB, "", "");
                SetLineValues(lineB, FmtValue(distStr), "");
            }
        }

        // Line 6 always shows IDX shortcut
        SetLineLabels(6, FmtLabel("<INDEX"), "");
        SetLineValues(6, "", "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (side == 0) // Left
        {
            switch (row)
            {
                case 1:
                    HandleWaypointLsk(0);
                    break; // slot 0
                case 2: // inactive (even rows are data lines, not selectable)
                    break;
                case 3:
                    HandleWaypointLsk(1);
                    break; // slot 1
                case 4: // inactive
                    break;
                case 5:
                    HandleWaypointLsk(2);
                    break; // slot 2
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
                case 6: // inactive
                    break;
            }
        }
        // NOTE: Populate() is NOT called here — FmsPageRouter.Update() pumps it every frame.
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private handlers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Handles L1/L3/L5 scratchpad interactions for a waypoint slot.
    ///   Empty SP  → copy ident to scratchpad.
    ///   SP=DELETE → remove waypoint at slot.
    ///   SP=ident  → insert waypoint before slot.
    /// </summary>
    private void HandleWaypointLsk(int slot)
    {
        int routeIdx = _pageIndex * LEGS_PER_PAGE + slot;
        string sp = Scratchpad.CurrentText;

        if (sp.Length == 0)
        {
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
            else
            {
                Scratchpad.ShowMessage("NOT ALLOWED");
            }
            return;
        }

        // Attempt to insert the typed waypoint ident before routeIdx
        var scenario = Model.Scenario;
        if (scenario == null)
        {
            Scratchpad.ShowMessage("NO SCENARIO");
            return;
        }

        var wpDef = scenario.waypoints.Find(w =>
            string.Equals(w.ident, sp, StringComparison.OrdinalIgnoreCase)
        );

        if (wpDef == null)
        {
            Scratchpad.ShowMessage("NOT IN DATABASE");
            return;
        }

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
