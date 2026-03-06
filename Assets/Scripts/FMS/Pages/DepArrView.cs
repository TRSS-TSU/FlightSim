/// <summary>
/// CDU DEP ARR page — departure and arrival airport display.
/// Scenario 01 (KNPA → KNPA). R6 loads RNAV 25L approach fixes.
///
/// Layout:
///   L1  DEP  [KNPA]          R1  ARR  [KNPA]
///   L2  PROC  RWYS 25L/R
///   L3  HDG   220°
///   L4  ALT   3,000 FT
///   L5  ARR PROC  [RNAV 25L / RNAV 25L LOADED]
///   L6  &lt;IDX                 R6  LOAD ARR&gt;
///
/// LSK interactions:
///   R6 → append rnav25LFixes to ActiveRoute, set ArrivalLoaded=true
///   L6 → Index
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
        SetLineValues(3, FmtValue("220\u00B0"), "");

        SetLineLabels(4, FmtLabel("ALT"), "");
        SetLineValues(4, FmtValue("3,000 FT"), "");

        string arrStatus = Model.ArrivalLoaded ? "RNAV 25L LOADED" : "RNAV 25L";
        SetLineLabels(5, FmtLabel("ARR PROC"), "");
        SetLineValues(5, FmtValue(arrStatus), "");

        SetLineLabels(6, FmtLabel("<IDX"), FmtLabel("LOAD ARR>"));
        SetLineValues(6, "", "");
    }

    public override void HandleLsk(int side, int row)
    {
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
                case 3: // inactive
                    break;
                case 4: // inactive
                    break;
                case 5: // inactive
                    break;
                case 6:
                    LoadArrival();
                    break;
            }
        }
        // NOTE: Populate() is NOT called here — FmsPageRouter.Update() pumps it every frame.
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void LoadArrival()
    {
        if (Model.ArrivalLoaded)
        {
            Scratchpad.ShowMessage("ALREADY LOADED");
            return;
        }

        var sd = Model.Scenario;
        if (sd == null || sd.rnav25LFixes == null || sd.rnav25LFixes.Count == 0)
        {
            Scratchpad.ShowMessage("NO ARR DATA");
            return;
        }

        foreach (var ident in sd.rnav25LFixes)
        {
            var wp = sd.waypoints.Find(w =>
                string.Equals(w.ident, ident, System.StringComparison.OrdinalIgnoreCase)
            );
            if (wp != null)
                Model.ActiveRoute.Add(wp);
        }

        var fp = Router.GetFlightPlan();
        if (fp != null)
            fp.RebuildRoute(Model.ActiveRoute, sd.centerLatDeg, sd.centerLonDeg, sd.baseZoom);

        Model.ArrivalLoaded = true;
        Scratchpad.ShowMessage("ARR LOADED", 1.5f);
    }
}
