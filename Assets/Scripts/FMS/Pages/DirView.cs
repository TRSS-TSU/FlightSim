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
///                       execute Direct-To via the selected strategy (see TODO below)
///   L6               → return to Index
///
/// TODO: Choose Direct-To strategy before implementing route modification:
///   SIMPLE — navigate directly to typed WP; keep remaining route unchanged
///   STRICT — truncate ActiveRoute to [typed WP and all subsequent waypoints]
///   FULL   — insert typed WP at index 0; keep full existing route after it
/// Awaiting user decision. Current implementation validates the ident and
/// shows NOT IMPLEMENTED — no route modification is performed yet.
///
/// Formatting: labels cyan (#00FFFF), values white (#FFFFFF) via FmtLabel/FmtValue.
/// </summary>
public class DirView : FmsPageView
{
    // ── Formatting helpers ───────────────────────────────────────────────────────
    private string FmtTitle()             => "DIR";
    private string FmtLabel(string label) => string.IsNullOrEmpty(label) ? label : $"<color=#00FFFF>{label}</color>";
    private string FmtValue(string value) => string.IsNullOrEmpty(value) ? value : $"<color=#FFFFFF>{value}</color>";

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
        if (side == 0)  // Left
        {
            switch (row)
            {
                case 1: HandleDirectTo(); break;
                case 2: // inactive
                    break;
                case 3: // inactive
                    break;
                case 4: // inactive
                    break;
                case 5: // inactive
                    break;
                case 6: Router.ShowPage("Index"); break;
            }
        }
        else  // Right
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
            int    active = Model.ActiveLegIndex;
            string toIdent = (active < Model.ActiveRoute.Count)
                           ? Model.ActiveRoute[active].ident
                           : "";
            if (!string.IsNullOrEmpty(toIdent))
                Scratchpad.Append(toIdent);
            return;
        }

        // Validate ident against scenario waypoint database
        var scenario = Model.Scenario;
        if (scenario == null) { Scratchpad.ShowMessage("NO SCENARIO"); return; }

        var wpDef = scenario.waypoints.Find(w =>
            string.Equals(w.ident, sp, System.StringComparison.OrdinalIgnoreCase));

        if (wpDef == null) { Scratchpad.ShowMessage("NOT IN DATABASE"); return; }

        // TODO: Implement Direct-To route modification using chosen strategy:
        //   SIMPLE — Router.GetNavAutopilot().activeIndex = index of wpDef in ActiveRoute
        //   STRICT — ActiveRoute.RemoveRange(0, index); CommitRoute(); nav.activeIndex = 0
        //   FULL   — ActiveRoute.Insert(0, wpDef); CommitRoute(); nav.activeIndex = 0
        // Awaiting user decision before modifying the active route.
        Scratchpad.ShowMessage("NOT IMPLEMENTED");
    }
}
