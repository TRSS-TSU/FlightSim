using System;
using System.Collections.Generic;
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
///   L2 (empty SP)       → select displayed scenario route and enter MOD state
///   L2 (non-empty SP)   → commit typed route name and enter MOD state
///   L6 ACT              → navigate to SecFpln
///   L6 MOD not-armed    → L6L <YES arms EXEC; L6R NO> cancels MOD
///   L6 MOD armed        → L6L <CANCEL MOD cancels; L6R OFFSET = inactive
///
/// Formatting: labels cyan (#00FFFF), values white (#FFFFFF) via FmtLabel/FmtValue.
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
    private bool _modActive = false;
    private bool _execArmed = false;

    private string origin = "";
    private string dest = "";
    private string toIdent = "";
    private int distNm = 0;

    public bool HasArmedMod => _modActive && _execArmed;

    // ─────────────────────────────────────────────────────────────────────────
    // FmsPageView contract
    // ─────────────────────────────────────────────────────────────────────────

    public override void Populate()
    {
        ClearAllLines();
        GetPageNumber()?.SetText("1/1");

        string displayRouteName = GetDisplayRouteName();

        List<ScenarioDefinition.WaypointDef> displayRoute =
            (_modActive || Model.ActiveRoute.Count == 0)
                ? BuildScenarioPrefillRoute()
                : Model.ActiveRoute;

        origin = displayRoute.Count > 0 ? displayRoute[0].ident : "----";
        dest = displayRoute.Count > 0 ? displayRoute[displayRoute.Count - 1].ident : "----";
        toIdent = displayRoute.Count > 1 ? displayRoute[1].ident : dest;
        distNm = Mathf.RoundToInt(CalculateTotalRouteDistNm(displayRoute));

        if (_modActive && _execArmed)
        {
            // ── STATE: MOD ARMED ─────────────────────────────────────────────────
            // EXEC is primed; user can CANCEL MOD or navigate away.

            GetTitle()?.SetText(FmtTitle("MOD FPLN"));
            SetLineLabels(1, FmtLabel("ORIGIN     DIST"), FmtLabel("DEST"));
            SetLineValues(1, FmtValue($"{origin, -14}{distNm, 5}"), FmtValue(dest));
            SetLineLabels(2, FmtLabel("ROUTE"), FmtLabel("ALTN"));
            SetLineValues(2, FmtValue(displayRouteName), FmtValue("----"));
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
            SetLineValues(2, FmtValue(displayRouteName), FmtValue("----"));
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
        else if (Model.ActiveRoute.Count > 0)
        {
            // ── STATE: ACT — ROUTE LOADED ────────────────────────────────────────
            // Normal ACT display with live route data.

            GetTitle()?.SetText(FmtTitle("ACT FPLN"));
            SetLineLabels(1, FmtLabel("ORIGIN     DIST"), FmtLabel("DEST"));
            SetLineValues(1, FmtValue($"{origin, -14}{distNm, 5}"), FmtValue(dest));
            SetLineLabels(2, FmtLabel("ROUTE"), FmtLabel("ALTN"));
            SetLineValues(2, FmtValue(displayRouteName), FmtValue("----"));
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
            SetLineValues(1, FmtValue($"{origin, -14}{distNm, 5}"), FmtValue(dest));
            SetLineLabels(2, FmtLabel("ROUTE"), FmtLabel("ALTN"));
            SetLineValues(2, FmtValue(displayRouteName), FmtValue("----"));
            SetLineLabels(3, FmtLabel("VIA"), FmtLabel("TO"));
            SetLineValues(3, FmtValue("VECT . RNV  32"), FmtValue("RW32"));
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
    /// L2 route selection:
    ///   Empty scratchpad -> select the displayed scenario route.
    ///   Non-empty scratchpad -> select the typed route name.
    /// </summary>
    private void HandleL2()
    {
        if (Scratchpad.CurrentText.Length == 0)
        {
            string scenarioRoute = GetScenarioRouteName();

            if (string.IsNullOrWhiteSpace(scenarioRoute))
            {
                Scratchpad.ShowMessage("NO ROUTE", 1.5f);
                return;
            }

            Scratchpad.SetText(scenarioRoute);
            return;
        }

        string routeName = Scratchpad.ReadAndClear();

        if (!IsSelectedRouteNameValid(routeName))
        {
            Scratchpad.ShowMessage("INVALID ROUTE", 1.5f);
            _pendingRouteName = null;
            return;
        }

        _pendingRouteName = routeName.Trim().ToUpperInvariant();
        _modActive = true;
        _execArmed = false;
    }

    /// <summary>Cancel MOD state and revert to ACT display.</summary>
    private void CancelMod()
    {
        _modActive = false;
        _pendingRouteName = null;
        _execArmed = false;
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

        if (!TryBuildSelectedRoute(out var selectedRoute, out string error))
        {
            Scratchpad.ShowMessage(error, 1.5f);
            return;
        }

        // 1. Capture continuity snapshot BEFORE mutating Model.ActiveRoute
        var snap = Router.CaptureRouteContinuity();

        // 2. Rebuild ActiveRoute from the validated selected route
        Model.ActiveRoute.Clear();
        Model.ActiveRoute.AddRange(selectedRoute);

        // 3. Rebuild scene waypoints, resolve active leg, reset capture state
        Router.CommitActiveRoute(snap, clearArrivalLoaded: true, executeNow: true);

        // 4. Page-local cleanup
        Scratchpad.ShowMessage("ROUTE LOADED", 1.5f);
        CancelMod();
    }

    private bool TryBuildSelectedRoute(
        out List<ScenarioDefinition.WaypointDef> selectedRoute,
        out string error
    )
    {
        selectedRoute = new List<ScenarioDefinition.WaypointDef>();
        error = "";

        var sd = Model.Scenario;
        if (sd == null)
        {
            error = "NO SCENARIO";
            return false;
        }

        if (!IsSelectedRouteNameValid(_pendingRouteName))
        {
            error = "INVALID ROUTE";
            return false;
        }

        foreach (var ident in sd.prefillRouteIdents)
        {
            var wp = FindActiveRouteWaypoint(sd, ident);
            if (wp == null)
            {
                error = "ROUTE INVALID";
                selectedRoute.Clear();
                return false;
            }

            selectedRoute.Add(wp);
        }

        if (selectedRoute.Count < 2)
        {
            error = "ROUTE INVALID";
            selectedRoute.Clear();
            return false;
        }

        return true;
    }

    private List<ScenarioDefinition.WaypointDef> BuildScenarioPrefillRoute()
    {
        var route = new List<ScenarioDefinition.WaypointDef>();
        var sd = Model?.Scenario;
        if (sd == null)
            return route;

        foreach (var ident in sd.prefillRouteIdents)
        {
            var wp = FindActiveRouteWaypoint(sd, ident);
            if (wp != null)
                route.Add(wp);
        }

        return route;
    }

    private bool IsSelectedRouteNameValid(string routeName)
    {
        string scenarioRoute = Model?.Scenario?.route;
        return !string.IsNullOrWhiteSpace(routeName)
            && !string.IsNullOrWhiteSpace(scenarioRoute)
            && string.Equals(
                routeName.Trim(),
                scenarioRoute.Trim(),
                StringComparison.OrdinalIgnoreCase
            );
    }

    private string GetDisplayRouteName()
    {
        if (_modActive)
            return _pendingRouteName ?? "";

        return GetScenarioRouteName();
    }

    private string GetScenarioRouteName()
    {
        return Model?.Scenario?.route ?? "";
    }

    private static ScenarioDefinition.WaypointDef FindActiveRouteWaypoint(
        ScenarioDefinition scenario,
        string ident
    )
    {
        if (scenario == null || string.IsNullOrWhiteSpace(ident))
            return null;

        return scenario.waypoints.Find(w =>
            w != null
            && w.includeInActiveRoute
            && string.Equals(w.ident, ident, StringComparison.OrdinalIgnoreCase)
        );
    }

    private static float CalculateTotalRouteDistNm(List<ScenarioDefinition.WaypointDef> route)
    {
        if (route == null || route.Count < 2)
            return 0f;

        float total = 0f;
        for (int i = 0; i < route.Count - 1; i++)
            total += HaversineNm(
                route[i].latDeg,
                route[i].lonDeg,
                route[i + 1].latDeg,
                route[i + 1].lonDeg
            );

        return total;
    }

    private static float HaversineNm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 3440.065;
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(lat1 * Math.PI / 180.0)
                * Math.Cos(lat2 * Math.PI / 180.0)
                * Math.Sin(dLon / 2)
                * Math.Sin(dLon / 2);
        double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        return (float)(R * c);
    }
}
