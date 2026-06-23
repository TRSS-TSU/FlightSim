/// <summary>
/// CDU DEP ARR page — departure and arrival airport display.
/// Scenario 01 (KNPA → KNPA). R5/R6 stage RNAV approach fixes.
///
/// Layout:
///   L1  DEP  [KNPA]          R1  ARR  [KNPA]
///   L2  PROC  RWYS 25L/R
///   L3  HDG   220°                    R3 LOAD 7L&gt;
///   L4  ALT   3,000 FT                R4 LOAD 25L&gt;
///   L5  ARR PROC  [RNAV 25L]          R5 REMOVE ARR&gt;
///   L6  &lt;IDX
///
/// LSK interactions:
///   R3/R4 → stage selected RNAV fixes as a MOD route; EXEC commits ArrivalLoaded=true
///   R5 → remove staged/active arrival, L6 → Index
///
/// Formatting: labels cyan (#00FFFF), values white (#FFFFFF) via FmtLabel/FmtValue.
///</summary>
public class DepArrView : FmsPageView
{
    // ── Formatting helpers ───────────────────────────────────────────────────────
    private string FmtTitle() => "DEP / ARR";

    private string FmtLabel(string label) =>
        string.IsNullOrEmpty(label) ? label : $"<color=#00FFFF>{label}</color>";

    private string FmtValue(string value) =>
        string.IsNullOrEmpty(value) ? value : $"<color=#FFFFFF>{value}</color>";

    // ─────────────────────────────────────────────────────────────────────────
    // FmsPageView contract
    // ─────────────────────────────────────────────────────────────────────────

    public override void Populate()
    {
        if (Model == null || Router == null)
            return;

        ClearAllLines();
        GetTitle()?.SetText(FmtTitle());
        GetPageNumber()?.SetText("1/1");
        GetMessageLine()?.SetText("");

        string dep = Model.OriginIdent.Length > 0 ? Model.OriginIdent : "----";
        string arr = Model.DestIdent.Length > 0 ? Model.DestIdent : "----";

        SetLineLabels(1, FmtLabel("DEP"), FmtLabel("ARR"));
        SetLineValues(1, FmtValue(dep), FmtValue(arr));

        SetLineLabels(2, FmtLabel("PROC"), "");
        SetLineValues(2, FmtValue("RWYS 25L/R"), "");

        SetLineLabels(3, FmtLabel("HDG"), "");
        SetLineValues(3, FmtValue("220\u00B0"), FmtValue("LOAD 7L>"));

        SetLineLabels(4, FmtLabel("ALT"), "");
        SetLineValues(4, FmtValue("3,000 FT"), FmtValue("LOAD 25L>"));

        string arrStatus = GetArrivalStatus("RNAV 25L");
        SetLineLabels(5, FmtLabel("ARR PROC"), "");
        SetLineValues(5, FmtValue(arrStatus), HasArrivalForRemoval() ? FmtValue("REMOVE ARR>") : "");

        SetLineLabels(6, FmtLabel("<IDX"), "");
        SetLineValues(6, "", "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (Model == null || Router == null)
            return;

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
                case 3:
                    LoadArrival("RNAV 7L", Model.Scenario?.rnav7LFixes);
                    break;
                case 4:
                    LoadArrival("RNAV 25L", Model.Scenario?.rnav25LFixes);
                    break;
                case 5:
                    RemoveArrival();
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

    private string GetArrivalStatus(string name)
    {
        if (Router.HasPendingArrivalChange)
        {
            if (!Router.PendingArrivalLoaded)
                return "ARR REMOVE MOD";

            return string.IsNullOrWhiteSpace(Router.PendingArrivalName) ? "ARR MOD" : $"{Router.PendingArrivalName} MOD";
        }

        if (Model.ArrivalLoaded)
            return string.IsNullOrWhiteSpace(Model.ArrivalName) ? "ARR LOADED" : $"{Model.ArrivalName} LOADED";

        return name;
    }

    private void LoadArrival(string arrivalName, System.Collections.Generic.List<string> fixes)
    {
        if (IsCurrentArrival(arrivalName))
        {
            Scratchpad.ShowMessage("ALREADY LOADED");
            return;
        }

        var sd = Model.Scenario;
        if (sd == null || fixes == null || fixes.Count == 0)
        {
            Scratchpad.ShowMessage("NO ARR DATA");
            return;
        }

        var snap = Router.CaptureRouteContinuity();
        var editedRoute = new System.Collections.Generic.List<ScenarioDefinition.WaypointDef>(
            Router.GetRouteForDisplay()
        );
        RemoveArrivalFixes(editedRoute, sd);

        if (editedRoute.Count == 0)
        {
            Scratchpad.ShowMessage("NO ROUTE");
            return;
        }

        foreach (var ident in fixes)
        {
            var wp = sd.waypoints.Find(w =>
                string.Equals(w.ident, ident, System.StringComparison.OrdinalIgnoreCase)
            );
            if (wp == null)
            {
                Scratchpad.ShowMessage("ARR DATA INVALID");
                return;
            }

            editedRoute.Add(wp);
        }

        Router.StageActiveRoute(
            editedRoute,
            snap,
            arrivalLoadedAfterExec: true,
            arrivalNameAfterExec: arrivalName
        );
        Scratchpad.ShowMessage("ARR MOD - EXEC", 1.5f);
    }

    private void RemoveArrival()
    {
        var sd = Model.Scenario;
        if (sd == null || !HasArrivalForRemoval())
        {
            Scratchpad.ShowMessage("NO ARR");
            return;
        }

        var snap = Router.CaptureRouteContinuity();
        var editedRoute = new System.Collections.Generic.List<ScenarioDefinition.WaypointDef>(
            Router.GetRouteForDisplay()
        );
        RemoveArrivalFixes(editedRoute, sd);

        Router.StageActiveRoute(
            editedRoute,
            snap,
            arrivalLoadedAfterExec: false,
            arrivalNameAfterExec: ""
        );
        Scratchpad.ShowMessage("ARR DEL - EXEC", 1.5f);
    }

    private bool HasArrivalForRemoval()
        => Model.ArrivalLoaded || Router.HasPendingArrivalChange;

    private bool IsCurrentArrival(string arrivalName)
    {
        if (Router.HasPendingArrivalChange)
            return Router.PendingArrivalLoaded
                && string.Equals(
                    Router.PendingArrivalName,
                    arrivalName,
                    System.StringComparison.OrdinalIgnoreCase
                );

        return Model.ArrivalLoaded
            && string.Equals(
                Model.ArrivalName,
                arrivalName,
                System.StringComparison.OrdinalIgnoreCase
            );
    }

    private static void RemoveArrivalFixes(
        System.Collections.Generic.List<ScenarioDefinition.WaypointDef> route,
        ScenarioDefinition scenario
    )
    {
        route.RemoveAll(w => w != null && IsArrivalFix(scenario, w.ident));
    }

    private static bool IsArrivalFix(ScenarioDefinition scenario, string ident)
    {
        if (scenario == null || string.IsNullOrWhiteSpace(ident))
            return false;

        return ContainsIdent(scenario.rnav25LFixes, ident)
            || ContainsIdent(scenario.rnav7LFixes, ident);
    }

    private static bool ContainsIdent(System.Collections.Generic.List<string> idents, string ident)
    {
        if (idents == null)
            return false;

        return idents.Exists(x =>
            string.Equals(x, ident, System.StringComparison.OrdinalIgnoreCase)
        );
    }
}
