using UnityEngine;

/// <summary>
/// CDU ACT FPLN page — route loading workflow.
///
/// Layout (ACT state):
///   L1 ""  [OriginIdent]    [TotalDistNm]   ""   [DestIdent]
///   L2 (empty — route name appears here after selection)
///   L3–L5 (empty)
///   L6 "&lt;IDX"
///
/// Layout (MOD state):
///   L1 same as ACT
///   L2 ""  ""  ""  [selectedRouteName]
///   L6 "---- Load New Route ----"  "&lt;CANCEL MOD"  ""  "No&gt;"
///   Title: "MOD FPLN"
///   Message_Line: "Exec"
///
/// LSK interactions:
///   L2 (empty scratchpad)  → copy scenario route name to scratchpad
///   L2 (non-empty SP)      → commit SP to line 2 VR, enter MOD state
///   L6 (any, MOD active)   → cancel MOD, revert to ACT
///   L6 (any, ACT)          → navigate to Index
///   R6 (MOD active)        → apply route → rebuild FlightPlan → revert to ACT
/// </summary>
public class ActFplnView : FmsPageView
{
    // ── MOD state ───────────────────────────────────────────────────────────────
    private bool   _modActive        = false;
    private string _pendingRouteName = null;

    // ─────────────────────────────────────────────────────────────────────────
    // FmsPageView contract
    // ─────────────────────────────────────────────────────────────────────────

    public override void Populate()
    {
        ClearAllLines();

        GetTitle()?.SetText(_modActive ? "MOD FPLN" : "ACT FPLN");
        GetPageNumber()?.SetText("1/1");

        // ── Line 1: origin / dest / total distance ───────────────────────────
        string origin = Model.ActiveRoute.Count > 0
            ? Model.ActiveRoute[0].ident
            : "----";
        string dest = Model.ActiveRoute.Count > 1
            ? Model.ActiveRoute[Model.ActiveRoute.Count - 1].ident
            : "----";
        int distNm = Mathf.RoundToInt(Model.TotalRouteDistNm);
        string valLeft = $"{origin,-14}{distNm,5}";

        SetLine(1, "", valLeft, "", dest);

        // ── Line 2: selected route name (visible in MOD state) ───────────────
        SetLine(2, "", "", "", _pendingRouteName ?? "");

        // ── Lines 3–5: reserved / empty ──────────────────────────────────────
        // (left for future use)

        // ── Line 6: context-sensitive ────────────────────────────────────────
        if (_modActive)
        {
            SetLine(6, "---- Load New Route ----", "<CANCEL MOD", "", "No>");
        }
        else
        {
            SetLine(6, "<IDX", "", "", "");
        }

        // ── Message_Line: "Exec" during MOD, cleared otherwise ───────────────
        GetMessageLine()?.SetText(_modActive ? "Exec" : "");
    }

    public override void HandleLsk(int side, int row)
    {
        switch (row)
        {
            case 2 when side == 0:
                HandleL2();
                break;

            case 6:
                if (_modActive)
                {
                    if (side == 0) CancelMod();     // <CANCEL MOD
                    else           ApplyRoute();     // No>
                }
                else
                {
                    Router.ShowPage("Index");
                }
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void OnDisable() => CancelMod();   // auto-cancel MOD on page navigation

    // ─────────────────────────────────────────────────────────────────────────
    // Private handlers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Two-step L2 interaction:
    ///   Empty scratchpad → copy route name to scratchpad.
    ///   Non-empty scratchpad → commit to line 2 VR and enter MOD state.
    /// </summary>
    private void HandleL2()
    {
        if (Scratchpad.CurrentText.Length == 0)
        {
            // Step A: seed the scratchpad with the scenario route name
            string routeName = Model.Scenario?.route ?? "";
            if (!string.IsNullOrEmpty(routeName))
                Scratchpad.Append(routeName);
            else
                Scratchpad.ShowMessage("NO ROUTE");
        }
        else
        {
            // Step B: commit scratchpad content → enter MOD
            _pendingRouteName = Scratchpad.ReadAndClear();
            _modActive = true;
        }
    }

    /// <summary>Cancel MOD state and revert to ACT display.</summary>
    private void CancelMod()
    {
        _modActive        = false;
        _pendingRouteName = null;
    }

    /// <summary>
    /// Apply the selected route: rebuild Model.ActiveRoute from
    /// ScenarioDefinition.prefillRouteIdents, then commit to FlightPlan
    /// and NavAutopilot. Mirrors ActLegsView.CommitRoute() pattern.
    /// </summary>
    private void ApplyRoute()
    {
        var sd = Model.Scenario;
        if (sd == null) { Scratchpad.ShowMessage("NO SCENARIO"); return; }

        // Rebuild ActiveRoute from scenario prefill list
        Model.ActiveRoute.Clear();
        foreach (var ident in sd.prefillRouteIdents)
        {
            var wp = sd.waypoints.Find(w =>
                string.Equals(w.ident, ident, System.StringComparison.OrdinalIgnoreCase));
            if (wp != null) Model.ActiveRoute.Add(wp);
        }
        Model.ActiveLegIndex = 0;

        // Commit to FlightPlan (rebuilds scene waypoint objects)
        var fp = Router.GetFlightPlan();
        if (fp) fp.RebuildRoute(Model.ActiveRoute, sd.centerLatDeg, sd.centerLonDeg, sd.baseZoom);

        // Reset autopilot leg index
        var nav = Router.GetNavAutopilot();
        if (nav) nav.activeIndex = 0;

        CancelMod();
        Scratchpad.ShowMessage("ROUTE LOADED", 1.5f);
    }
}
