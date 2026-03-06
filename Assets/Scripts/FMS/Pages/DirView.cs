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
            string toIdent =
                (active < Model.ActiveRoute.Count) ? Model.ActiveRoute[active].ident : "";
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

        // STRICT strategy: truncate all legs before the target waypoint.
        int idx = Model.ActiveRoute.FindIndex(w =>
            string.Equals(w.ident, wpDef.ident, System.StringComparison.OrdinalIgnoreCase)
        );

        if (idx < 0)
        {
            // Valid DB ident but not in current route — insert at front
            Model.ActiveRoute.Insert(0, wpDef);
        }
        else if (idx > 0)
        {
            // Truncate all legs before the target (STRICT)
            Model.ActiveRoute.RemoveRange(0, idx);
        }

        Model.ActiveLegIndex = 0;

        var fp = Router.GetFlightPlan();
        var sd = Model.Scenario;
        if (fp && sd != null)
            fp.RebuildRoute(Model.ActiveRoute, sd.centerLatDeg, sd.centerLonDeg, sd.baseZoom);

        var nav = Router.GetNavAutopilot();
        if (nav)
            nav.activeIndex = 0;

        // Approach fixes are at the end of route; they may have been truncated.
        Model.ArrivalLoaded = false;

        Scratchpad.ReadAndClear();
        Scratchpad.ShowMessage("DIRECT TO " + wpDef.ident, 2.0f);
        Router.ShowPage("ActLegs");
    }
}
