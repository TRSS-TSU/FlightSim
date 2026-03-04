using UnityEngine;

/// <summary>
/// CDU ACT FPLN page — route loading workflow.
///
/// Four display states driven by _modActive, _execArmed, and ActiveRoute.Count:
///   ACT no route   — !_modActive, Count == 0
///   ACT route loaded — !_modActive, Count > 0
///   MOD confirm    — _modActive, !_execArmed
///   MOD armed      — _modActive, _execArmed
///
/// Line layout (all states):
///   L1  [OriginIdent + TotalDist]          [DestIdent]
///   L2  [route name]          ALTN  [----]
///   L3                        ORIG RWY
///   L4  VIA  [DIRECT]         TO  [dest ident]
///   L5  (empty)
///   L6  (state-dependent — see Populate)
///
/// LSK interactions:
///   L2 (empty scratchpad)  → copy scenario route name to scratchpad
///   L2 (non-empty SP)      → commit SP → enter MOD state
///   L6 ACT                 → navigate to SecFpln
///   L6 MOD not-armed       → <YES arms EXEC; NO> cancels MOD
///   L6 MOD armed           → <CANCEL MOD cancels; R6 OFFSET = inactive
/// </summary>
public class ActFplnView : FmsPageView
{
    // ── MOD state ───────────────────────────────────────────────────────────────
    private bool   _modActive        = false;
    private string _pendingRouteName = null;
    private bool   _execArmed        = false;

    // ─────────────────────────────────────────────────────────────────────────
    // FmsPageView contract
    // ─────────────────────────────────────────────────────────────────────────

    public override void Populate()
    {
        ClearAllLines();
        GetPageNumber()?.SetText("1/1");

        // Seed pending route name when in ACT state and not yet set
        if (!_modActive && string.IsNullOrEmpty(_pendingRouteName))
            _pendingRouteName = Model?.Scenario?.route ?? "";

        if (_modActive && _execArmed)
        {
            // ── STATE: MOD ARMED ─────────────────────────────────────────────────
            // EXEC is primed; user can CANCEL or navigate away.
            string origin  = Model.ActiveRoute.Count > 0 ? Model.ActiveRoute[0].ident : "----";
            string dest    = Model.ActiveRoute.Count > 0 ? Model.ActiveRoute[Model.ActiveRoute.Count - 1].ident : "----";
            string toIdent = Model.ActiveRoute.Count > 1 ? Model.ActiveRoute[1].ident : "----";
            int    distNm  = Mathf.RoundToInt(Model.TotalRouteDistNm);

            GetTitle()?.SetText("MOD FPLN");
            SetLineLabels(1, "ORIGIN     DIST", "DEST");
            SetLineValues(1, $"{origin,-14}{distNm,5}", dest);
            SetLineLabels(2, "ROUTE", "ALTN");
            SetLineValues(2, _pendingRouteName ?? "", "----");
            SetLineLabels(3, "", "ORIG RWY");
            SetLineLabels(4, "VIA", "TO");
            SetLineValues(4, "DIRECT", toIdent);
            // L5 empty
            SetLineLabels(6, "", "");
            SetLineValues(6, "<CANCEL MOD", "OFFSET   ----");
            GetMessageLine()?.SetText("EXEC");
        }
        else if (_modActive)
        {
            // ── STATE: MOD CONFIRM ───────────────────────────────────────────────
            // Prompting YES / NO to load new route.
            string origin  = Model.ActiveRoute.Count > 0 ? Model.ActiveRoute[0].ident : "----";
            string dest    = Model.ActiveRoute.Count > 0 ? Model.ActiveRoute[Model.ActiveRoute.Count - 1].ident : "----";
            string toIdent = Model.ActiveRoute.Count > 1 ? Model.ActiveRoute[1].ident : "----";
            int    distNm  = Mathf.RoundToInt(Model.TotalRouteDistNm);

            GetTitle()?.SetText("MOD FPLN");
            SetLineLabels(1, "ORIGIN     DIST", "DEST");
            SetLineValues(1, $"{origin,-14}{distNm,5}", dest);
            SetLineLabels(2, "ROUTE", "ALTN");
            SetLineValues(2, _pendingRouteName ?? "", "----");
            SetLineLabels(3, "", "ORIG RWY");
            SetLineLabels(4, "VIA", "TO");
            SetLineValues(4, "DIRECT", toIdent);
            // L5 empty
            SetLineLabels(6, "---- LOAD NEW ROUTE ----", "");
            SetLineValues(6, "<YES", "NO>");
            GetMessageLine()?.SetText("EXEC");
        }
        else if (Model.ActiveRoute.Count > 0)
        {
            // ── STATE: ACT — ROUTE LOADED ────────────────────────────────────────
            // Normal ACT display with live route data.
            string origin  = Model.ActiveRoute[0].ident;
            string dest    = Model.ActiveRoute[Model.ActiveRoute.Count - 1].ident;
            string toIdent = Model.ActiveRoute.Count > 1 ? Model.ActiveRoute[1].ident : dest;
            int    distNm  = Mathf.RoundToInt(Model.TotalRouteDistNm);

            GetTitle()?.SetText("ACT FPLN");
            SetLineLabels(1, "ORIGIN     DIST", "DEST");
            SetLineValues(1, $"{origin,-14}{distNm,5}", dest);
            SetLineLabels(2, "ROUTE", "ALTN");
            SetLineValues(2, _pendingRouteName ?? "", "----");
            SetLineLabels(3, "", "ORIG RWY");
            SetLineLabels(4, "VIA", "TO");
            SetLineValues(4, "DIRECT", toIdent);
            // L5 empty
            SetLineLabels(6, "", "");
            SetLineValues(6, "<SEC FPLN", "OFFSET   ----");
            GetMessageLine()?.SetText("");
        }
        else
        {
            // ── STATE: ACT — NO ROUTE ────────────────────────────────────────────
            // Empty flight plan; awaiting route selection via L2.
            GetTitle()?.SetText("ACT FPLN");
            SetLineLabels(1, "ORIGIN     DIST", "DEST");
            SetLineValues(1, "----", "----");
            SetLineLabels(2, "ROUTE", "ALTN");
            SetLineValues(2, "", "----");
            SetLineLabels(3, "", "ORIG RWY");
            SetLineLabels(4, "VIA", "TO");
            SetLineValues(4, "DIRECT", "----");
            // L5 empty
            SetLineLabels(6, "", "");
            SetLineValues(6, "<SEC FPLN", "");
            GetMessageLine()?.SetText("");
        }
    }

    public override void HandleLsk(int side, int row)
    {
        if (_modActive && _execArmed)
        {
            // ── STATE: MOD ARMED ─────────────────────────────────────────────────
            // Only L6 left is active; all other keys ignored.
            if (row == 6 && side == 0) CancelMod();   // <CANCEL MOD
            // L6 right: OFFSET — inactive
        }
        else if (_modActive)
        {
            // ── STATE: MOD CONFIRM ───────────────────────────────────────────────
            // L6 left arms EXEC; L6 right cancels MOD. All other keys ignored.
            if (row == 6 && side == 0) _execArmed = true;  // <YES — arm EXEC
            if (row == 6 && side == 1) CancelMod();         // NO>  — cancel MOD
        }
        else if (Model.ActiveRoute.Count > 0)
        {
            // ── STATE: ACT — ROUTE LOADED ────────────────────────────────────────
            // L2 left: scratchpad seed / commit to enter MOD.
            // L6 left: navigate to SecFpln. L6 right: OFFSET — inactive.
            if (row == 2 && side == 0) HandleL2();
            if (row == 6 && side == 0) Router.ShowPage("SecFpln");
        }
        else
        {
            // ── STATE: ACT — NO ROUTE ────────────────────────────────────────────
            // L2 left: scratchpad seed / commit to enter MOD.
            // L6 left: navigate to SecFpln. L6 right: inactive (no OFFSET).
            if (row == 2 && side == 0) HandleL2();
            if (row == 6 && side == 0) Router.ShowPage("SecFpln");
        }

        Populate();
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
            _execArmed = false;
        }
    }

    /// <summary>Cancel MOD state and revert to ACT display.</summary>
    private void CancelMod()
    {
        _modActive        = false;
        _pendingRouteName = null;
        _execArmed        = false;
    }

    /// <summary>Called by the EXEC function key.</summary>
    public void HandleExec()
    {
        if (!_modActive || !_execArmed)
        {
            Scratchpad.ShowMessage("NO MOD", 1.5f);
            return;
        }
        ApplyRoute();
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
