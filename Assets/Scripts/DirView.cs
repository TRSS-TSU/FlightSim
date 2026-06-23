using UnityEngine;

/// <summary>
/// CDU DIR (Direct-To) page — direct waypoint navigation entry.
///
/// Layout:
///   L1  [ident entry]       DIRECT TO
///   L2–L5  (empty)
///   L6  &lt;IDX
///
/// LSK interactions:
///   L1 (empty SP)    → seed scratchpad with current TO waypoint ident
///   L1 (SP=ident)    → validate ident against scenario waypoints, then
///                       execute Direct-To (STRICT: truncate route to target WP and after)
///   L6               → return to Index
///
/// STRICT strategy: if target WP is in the active route, all legs before it are removed.
/// If not in the route, the WP is inserted at index 0. Nav resets to leg 0.
/// ArrivalLoaded is cleared (approach fixes may have been truncated).
///
/// Formatting: labels cyan (#00FFFF), values white (#FFFFFF) via FmtLabel/FmtValue.
/// </summary>
public class DirView : FmsPageView
{
    // ── Formatting helpers ───────────────────────────────────────────────────────
    private string FmtTitle() => "DIR";

    private string FmtLabel(string label) =>
        string.IsNullOrEmpty(label) ? label : $"<color=#00FFFF>{label}</color>";

    private string FmtValue(string value) =>
        string.IsNullOrEmpty(value) ? value : $"<color=#FFFFFF>{value}</color>";

    // ─────────────────────────────────────────────────────────────────────────
    // FmsPageView contract
    // ─────────────────────────────────────────────────────────────────────────

    public override void Populate()
    {
        ClearAllLines();
        GetTitle()?.SetText(FmtTitle());
        GetPageNumber()?.SetText("1/1");
        GetMessageLine()?.SetText("");

        // L1: ident entry field label and DIRECT TO header
        SetLineLabels(1, "", FmtLabel("DIRECT TO"));
        SetLineValues(1, FmtValue("[ ]"), "");

        // L2–L5 empty
        SetLineLabels(2, "", "");
        SetLineValues(2, "", "");

        SetLineLabels(3, "", "");
        SetLineValues(3, "", "");

        SetLineLabels(4, "", "");
        SetLineValues(4, "", "");

        SetLineLabels(5, "", "");
        SetLineValues(5, "", "");

        SetLineLabels(6, FmtLabel("<IDX"), "");
        SetLineValues(6, "", "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (side == 0) // Left
        {
            switch (row)
            {
                case 1:
                    HandleDirectTo();
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

    private void HandleDirectTo()
    {
        string sp = Scratchpad.CurrentText;

        if (sp.Length == 0)
        {
            // Seed scratchpad with the current TO waypoint ident (if route is active)
            int active = Model.ActiveLegIndex;
            var route = Router.GetRouteForDisplay();
            string toIdent =
                (active < route.Count) ? route[active].ident : "";
            if (!string.IsNullOrEmpty(toIdent))
                Scratchpad.Append(toIdent);
            return;
        }

        // Validate ident against scenario waypoint database
        var scenario = Model.Scenario;
        if (scenario == null)
        {
            Scratchpad.ShowMessage("NO SCENARIO");
            return;
        }

        var wpDef = scenario.waypoints.Find(w =>
            string.Equals(w.ident, sp, System.StringComparison.OrdinalIgnoreCase)
        );

        if (wpDef == null)
        {
            Scratchpad.ShowMessage("NOT IN DATABASE");
            return;
        }

        var snap = Router.CaptureRouteContinuity();

        // STRICT strategy: truncate all legs before the target waypoint.
        var editedRoute = new System.Collections.Generic.List<ScenarioDefinition.WaypointDef>(
            Router.GetRouteForDisplay()
        );

        int idx = editedRoute.FindIndex(w =>
            string.Equals(w.ident, wpDef.ident, System.StringComparison.OrdinalIgnoreCase)
        );

        if (idx < 0)
        {
            // Valid DB ident but not in current route — insert at front
            editedRoute.Insert(0, wpDef);
        }
        else if (idx > 0)
        {
            // Truncate all legs before the target (STRICT)
            editedRoute.RemoveRange(0, idx);
        }

        // Note: do NOT write Model.ActiveLegIndex here — FmsPageRouter.Update() owns it
        // and will sync it from nav.activeIndex on the next frame.

        // Approach fixes are at the end of route; they may have been truncated.
        Router.StageActiveRoute(editedRoute, snap, clearArrivalLoaded: true, arrivalLoadedAfterExec: false);

        Scratchpad.ReadAndClear();
        Scratchpad.ShowMessage("DIR MOD - EXEC", 2.0f);
        Router.ShowPage("ActLegs");
    }
}
