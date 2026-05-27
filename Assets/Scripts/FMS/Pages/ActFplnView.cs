using UnityEngine;

/// <summary>
/// CDU ACT FPLN page — route loading workflow.
///
/// Four display states driven by _modActive, _execArmed, and ActiveRoute.Count:
///   ACT no route     — !_modActive, Count == 0
///   ACT route loaded — !_modActive, Count > 0
///   MOD confirm      — _modActive, !_execArmed
///   MOD armed        — _modActive, _execArmed
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
///   L2 (empty SP)       → copy scenario route name to scratchpad
///   L2 (non-empty SP)   → commit SP → enter MOD state
///   L6 ACT              → navigate to SecFpln
///   L6 MOD not-armed    → L6L <YES arms EXEC; L6R NO> cancels MOD
///   L6 MOD armed        → L6L <CANCEL MOD cancels; L6R OFFSET = inactive
///
/// Formatting: labels cyan (#00FFFF), values white (#FFFFFF) via FmtLabel/FmtValue.
/// _pendingRouteName is seeded inside Populate() via a one-shot lazy-init guard —
/// an intentional exception to strict render-only purity; fires only once per ACT session.
/// </summary>
public class ActFplnView : FmsPageView
{
    // ── Formatting helpers ───────────────────────────────────────────────────────
    private string FmtTitle(string title) => title;

    private string FmtLabel(string label) =>
        string.IsNullOrEmpty(label) ? label : $"<color=#00FFFF>{label}</color>";

    private string FmtValue(string value) =>
        string.IsNullOrEmpty(value) ? value : $"<color=#FFFFFF>{value}</color>";

    // ── MOD state ────────────────────────────────────────────────────────────────
    private string _pendingRouteName = null;
    private bool _init = false;
    private bool _modActive = false;
    private bool _execArmed = false;

    private string origin = "";
    private string dest = "";
    private string toIdent = "";
    private int distNm = 0;

    // ─────────────────────────────────────────────────────────────────────────
    // FmsPageView contract
    // ─────────────────────────────────────────────────────────────────────────

    public override void Populate()
    {
        ClearAllLines();
        GetPageNumber()?.SetText("1/1");

        // Seed pending route name when in ACT state and not yet set.
        // Intentional lazy-init guard — fires only once; acceptable exception to render-only purity.
        if (!_modActive && string.IsNullOrEmpty(_pendingRouteName))
            _pendingRouteName = Model?.Scenario?.route ?? "";
        origin = Model.ActiveRoute.Count > 0 ? Model.ActiveRoute[0].ident : "----";
        dest =
            Model.ActiveRoute.Count > 0
                ? Model.ActiveRoute[Model.ActiveRoute.Count - 1].ident
                : "----";
        toIdent = Model.ActiveRoute.Count > 1 ? Model.ActiveRoute[1].ident : dest;
        distNm = Mathf.RoundToInt(Model.TotalRouteDistNm);

        if (_modActive && _execArmed)
        {
            // ── STATE: MOD ARMED ─────────────────────────────────────────────────
            // EXEC is primed; user can CANCEL MOD or navigate away.

            GetTitle()?.SetText(FmtTitle("MOD FPLN"));
            SetLineLabels(1, FmtLabel("ORIGIN     DIST"), FmtLabel("DEST"));
            SetLineValues(1, FmtValue($"{origin, -14}{distNm, 5}"), FmtValue(dest));
            SetLineLabels(2, FmtLabel("ROUTE"), FmtLabel("ALTN"));
            SetLineValues(2, FmtValue(_pendingRouteName ?? ""), FmtValue("----"));
            SetLineLabels(3, FmtLabel(""), FmtLabel("ORIG  RWY"));
            SetLineValues(3, FmtValue(""), FmtValue(""));
            SetLineLabels(4, FmtLabel("VIA"), FmtLabel("TO"));
            SetLineValues(4, FmtValue("DIRECT"), FmtValue(toIdent));
            SetLineLabels(5, FmtLabel(""), FmtLabel(""));
            SetLineValues(5, FmtValue(""), FmtValue(""));
            SetLineLabels(6, "", "");
            SetLineValues(6, FmtValue("<CANCEL MOD"), FmtValue("OFFSET   ----"));
            GetMessageLine()?.SetText("FMS DR                   EXEC");
        }
        else if (_modActive)
        {
            // ── STATE: MOD CONFIRM ───────────────────────────────────────────────
            // Prompting YES / NO to load new route.

            GetTitle()?.SetText(FmtTitle("MOD FPLN"));
            SetLineLabels(1, FmtLabel("ORIGIN     DIST"), FmtLabel("DEST"));
            SetLineValues(1, FmtValue($"{origin, -14}{distNm, 5}"), FmtValue(dest));
            SetLineLabels(2, FmtLabel("ROUTE"), FmtLabel("ALTN"));
            SetLineValues(2, FmtValue(_pendingRouteName ?? ""), FmtValue("----"));
            SetLineLabels(3, FmtLabel("VIA"), FmtLabel("TO"));
            SetLineValues(3, FmtValue("VECT . RNV  32"), FmtValue("RW32"));
            SetLineLabels(4, FmtValue(""), FmtValue(""));
            SetLineValues(4, FmtValue(""), FmtValue(""));
            SetLineLabels(5, FmtValue(""), FmtValue(""));
            SetLineValues(5, FmtValue(""), FmtValue(""));
            SetLineLabels(6, FmtLabel("---- LOAD NEW ROUTE ----"), "");
            SetLineValues(6, FmtValue("<YES"), FmtValue("NO>"));
            GetMessageLine()?.SetText("FMS DR                   EXEC");
        }
        else if (_init)
        {
            // ── STATE: ACT — SCRATCHPAD SEEDED (pending L2 commit) ───────────────
            // Route name is in scratchpad; pressing L2 again will enter MOD.

            GetTitle()?.SetText(FmtTitle("ACT FPLN"));
            SetLineLabels(1, FmtLabel("ORIGIN     DIST"), FmtLabel("DEST"));
            SetLineValues(1, FmtValue($"{origin, -14}{distNm, 5}"), FmtValue(dest));
            SetLineLabels(2, FmtLabel("ROUTE"), FmtLabel("ALTN"));
            SetLineValues(2, FmtValue(_pendingRouteName ?? ""), FmtValue("----"));
            SetLineLabels(3, FmtLabel("VIA"), FmtLabel("TO"));
            SetLineValues(3, FmtValue("VECT . RNV  32"), FmtValue("RW32")); //TODO set these initial values to Green color
            SetLineLabels(4, FmtValue(""), FmtValue(""));
            SetLineValues(4, FmtValue(""), FmtValue(""));
            SetLineLabels(5, FmtValue(""), FmtValue(""));
            SetLineValues(5, FmtValue("<COPY ACTIVE"), FmtValue(""));
            SetLineLabels(6, FmtValue(""), FmtValue(""));
            SetLineValues(6, FmtValue("<SEC FPLN"), FmtValue(""));
            GetMessageLine()?.SetText("GNSS  NOT  AVAILABLE");
        }
        else if (Model.ActiveRoute.Count > 0)
        {
            // ── STATE: ACT — ROUTE LOADED ────────────────────────────────────────
            // Normal ACT display with live route data.

            GetTitle()?.SetText(FmtTitle("ACT FPLN"));
            SetLineLabels(1, FmtLabel("ORIGIN     DIST"), FmtLabel("DEST"));
            SetLineValues(1, FmtValue($"{origin, -14}{distNm, 5}"), FmtValue(dest));
            SetLineLabels(2, FmtLabel("ROUTE"), FmtLabel("ALTN"));
            SetLineValues(2, FmtValue(_pendingRouteName ?? ""), FmtValue("----"));
            SetLineLabels(3, FmtLabel("VIA"), FmtLabel("TO"));
            SetLineValues(3, FmtValue("VECT . RNV  32"), FmtValue("RW32")); //TODO set these initial values to Green color
            SetLineLabels(4, FmtValue(""), FmtValue(""));
            SetLineValues(4, FmtValue(""), FmtValue(""));
            SetLineLabels(5, FmtValue(""), FmtValue(""));
            SetLineValues(5, FmtValue("<COPY ACTIVE"), FmtValue(""));
            SetLineLabels(6, FmtValue(""), FmtValue(""));
            SetLineValues(6, FmtValue("<SEC FPLN"), FmtValue(""));
            GetMessageLine()?.SetText("DR  EXCEEDS  5MIN");
        }
        else
        {
            // ── STATE: ACT — NO ROUTE ────────────────────────────────────────────
            // Empty flight plan; awaiting route selection via L2.
            GetTitle()?.SetText(FmtTitle("ACT FPLN"));
            SetLineLabels(1, FmtLabel("ORIGIN     DIST"), FmtLabel("DEST"));
            SetLineValues(1, FmtValue("----"), FmtValue("----"));
            SetLineLabels(2, FmtLabel("ROUTE"), FmtLabel("ALTN"));
            SetLineValues(2, "", FmtValue("----"));
            SetLineLabels(3, FmtValue(""), FmtValue(""));
            SetLineLabels(4, FmtValue(""), FmtValue(""));
            SetLineValues(4, FmtValue(""), FmtValue(""));
            SetLineLabels(5, "", "");
            SetLineValues(5, FmtValue("<COPY ACTIVE"), FmtValue(""));
            SetLineLabels(6, "", "");
            SetLineValues(6, FmtValue("<SEC FPLN"), "");
            GetMessageLine()?.SetText("DR  EXCEEDS  5MIN");
        }
    }

    public override void HandleLsk(int side, int row)
    {
        if (_modActive && _execArmed)
        {
            // ── STATE: MOD ARMED ─────────────────────────────────────────────────
            // L6L: CANCEL MOD. R6: OFFSET — inactive.
            if (side == 0 && row == 6)
                CancelMod();
        }
        else if (_modActive)
        {
            // ── STATE: MOD CONFIRM ───────────────────────────────────────────────
            // L6L arms EXEC; R6R cancels MOD. All other keys inactive.
            if (side == 0 && row == 6)
                _execArmed = true; // <YES — arm EXEC
            if (side == 1 && row == 6)
                CancelMod(); // NO>  — cancel MOD
        }
        else if (_init)
        {
            // ── STATE: ACT — SCRATCHPAD SEEDED ───────────────────────────────────
            // L2: commit scratchpad value to enter MOD.
            // L6L: navigate to SecFpln. All other keys inactive.
            if (side == 0 && row == 2)
            {
                _modActive = true;
                _pendingRouteName = Scratchpad.ReadAndClear();
            }
            if (side == 0 && row == 6)
                Router.ShowPage("SecFpln");
        }
        else if (Model.ActiveRoute.Count > 0)
        {
            // ── STATE: ACT — ROUTE LOADED ────────────────────────────────────────
            // L2: scratchpad seed / commit. L6L: navigate to SecFpln.
            if (side == 0 && row == 2)
                HandleL2();
            if (side == 0 && row == 6)
                Router.ShowPage("SecFpln");
        }
        else
        {
            // ── STATE: ACT — NO ROUTE ────────────────────────────────────────────
            // L2: scratchpad seed. L6L: navigate to SecFpln. All other keys inactive.
            if (side == 0 && row == 2)
                HandleL2();
            if (side == 0 && row == 6)
                Router.ShowPage("SecFpln");
        }

        // NOTE: Populate() is NOT called here — FmsPageRouter.Update() pumps it every frame.
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void OnDisable() => CancelMod(); // auto-cancel MOD on page navigation

    // ─────────────────────────────────────────────────────────────────────────
    // Private handlers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Two-step L2 interaction:
    ///   Empty scratchpad → copy route name to scratchpad.
    ///   Non-empty scratchpad → commit to line 2 and enter MOD state.
    /// </summary>
    private void HandleL2()
    {
        if (Scratchpad.CurrentText.Length == 0)
        {
            // Step A: seed the scratchpad with the scenario route name
            string routeName = Model.Scenario?.route ?? "";
            if (!string.IsNullOrEmpty(routeName))
            {
                Scratchpad.Append(routeName);
                _init = true;
            }
            else
                Scratchpad.ShowMessage("NO ROUTE");
        }
        else
        {
            // Step B: commit scratchpad content → enter MOD
            _pendingRouteName = Scratchpad.ReadAndClear();
        }
    }

    /// <summary>Cancel MOD state and revert to ACT display.</summary>
    private void CancelMod()
    {
        _modActive = false;
        _pendingRouteName = null;
        _execArmed = false;
        _init = false;
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
    /// ScenarioDefinition.prefillRouteIdents, then commit via the shared
    /// FmsPageRouter.CommitActiveRoute() which runs the five-tier continuity
    /// resolver so the active leg is preserved whenever possible.
    /// </summary>
    private void ApplyRoute()
    {
        var sd = Model.Scenario;
        if (sd == null)
        {
            Scratchpad.ShowMessage("NO SCENARIO");
            return;
        }

        // 1. Capture continuity snapshot BEFORE mutating Model.ActiveRoute
        var snap = Router.CaptureRouteContinuity();

        // 2. Rebuild ActiveRoute from scenario prefill list
        Model.ActiveRoute.Clear();
        foreach (var ident in sd.prefillRouteIdents)
        {
            var wp = sd.waypoints.Find(w =>
                string.Equals(w.ident, ident, System.StringComparison.OrdinalIgnoreCase)
            );
            if (wp != null)
                Model.ActiveRoute.Add(wp);
        }

        // 3. Rebuild scene waypoints, resolve active leg, reset capture state
        Router.CommitActiveRoute(snap, clearArrivalLoaded: true);

        // 4. Page-local cleanup
        Scratchpad.ShowMessage("ROUTE LOADED", 1.5f);
        CancelMod();
    }
}
