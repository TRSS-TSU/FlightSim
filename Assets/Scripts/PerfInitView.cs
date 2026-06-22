/// <summary>
/// CDU PERF INIT page — performance initialisation (ZFW, fuel, gross weight).
///
/// Page 1/2:
///   L1  ZFW      [xxxxxx LBS]    R1  GROSS WT  [xxxxxx LBS]
///   L2  FUEL WT  [xxxxxx LBS]
///   L3–L5  (empty)
///   L6  &lt;IDX
///
/// Page 2/2:
///   L3  V-SPEEDS  NOT IMPL
///   L6  &lt;IDX
///
/// LSK interactions (page 1, left side only):
///   L1 empty SP  → copy current ZFW to scratchpad
///   L1 non-empty → parse and commit ZFW (sets Router.HasPendingPerf)
///   L2 empty SP  → copy current fuel weight to scratchpad
///   L2 non-empty → parse and commit fuel weight (sets Router.HasPendingPerf)
///   L6 (any page, any side) → return to Index
///   Invalid numeric input → shows "INVALID ENTRY"
///
/// After staging weights, student presses EXEC to confirm.
///
/// Formatting: labels cyan (#00FFFF), values white (#FFFFFF) via FmtLabel/FmtValue.
/// </summary>
public class PerfInitView : FmsPageView, IMultiPage
{
    // ── Formatting helpers ───────────────────────────────────────────────────────
    private string FmtTitle() => "PERF MENU";

    private string FmtLabel(string label) =>
        string.IsNullOrEmpty(label) ? label : $"<color=#00FFFF>{label}</color>";

    private string FmtValue(string value) =>
        string.IsNullOrEmpty(value) ? value : $"<color=#FFFFFF>{value}</color>";

    // ── State ────────────────────────────────────────────────────────────────────
    private int _page = 1;

    // ─────────────────────────────────────────────────────────────────────────
    // FmsPageView contract
    // ─────────────────────────────────────────────────────────────────────────

    public override void Populate()
    {
        ClearAllLines();
        GetTitle()?.SetText(FmtTitle());
        GetPageNumber()?.SetText($"{_page}/2");
        GetMessageLine()?.SetText("");

        if (_page == 1)
            PopulatePage1();
        else
            PopulatePage2();
    }

    private void PopulatePage1()
    {
        string zfw = Model.ZfwLbs > 0f ? $"{Model.ZfwLbs:0} LBS" : "------";
        string fuel = Model.FuelWeightLbs > 0f ? $"{Model.FuelWeightLbs:0} LBS" : "------";
        string gross = Model.GrossWeightLbs > 0f ? $"{Model.GrossWeightLbs:0} LBS" : "------";

        SetLineLabels(1, FmtLabel("ZFW"), FmtLabel("GROSS WT"));
        SetLineValues(1, FmtValue(zfw), FmtValue(gross));

        SetLineLabels(2, FmtLabel("FUEL WT"), "");
        SetLineValues(2, FmtValue(fuel), "");

        SetLineLabels(3, "", "");
        SetLineValues(3, "", "");

        SetLineLabels(4, "", "");
        SetLineValues(4, "", "");

        SetLineLabels(5, "", "");
        SetLineValues(5, "", "");

        SetLineLabels(6, FmtLabel("<IDX"), "");
        SetLineValues(6, "", "");
    }

    private void PopulatePage2()
    {
        SetLineLabels(1, "", "");
        SetLineValues(1, "", "");

        SetLineLabels(2, "", "");
        SetLineValues(2, "", "");

        SetLineLabels(3, FmtLabel("V-SPEEDS"), "");
        SetLineValues(3, FmtValue("NOT IMPL"), "");

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
                    if (_page == 1)
                        HandleZfw();
                    break;
                case 2:
                    if (_page == 1)
                        HandleFuel();
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
                case 1: // inactive (GROSS WT is computed, not entered)
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
        // NOTE: Populate() is NOT called here — FmsPageRouter.Update() pumps it every frame.
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleZfw()
    {
        string sp = Scratchpad.CurrentText;
        if (sp.Length == 0)
        {
            if (Model.ZfwLbs > 0f)
                Scratchpad.Append($"{Model.ZfwLbs:0}");
            return;
        }
        if (float.TryParse(sp, out float val) && val >= 0f)
        {
            Model.ZfwLbs = val;
            Router.HasPendingPerf = true;
            Scratchpad.ReadAndClear();
        }
        else
        {
            Scratchpad.ShowMessage("INVALID ENTRY");
        }
    }

    private void HandleFuel()
    {
        string sp = Scratchpad.CurrentText;
        if (sp.Length == 0)
        {
            if (Model.FuelWeightLbs > 0f)
                Scratchpad.Append($"{Model.FuelWeightLbs:0}");
            return;
        }
        if (float.TryParse(sp, out float val) && val >= 0f)
        {
            Model.FuelWeightLbs = val;
            Router.HasPendingPerf = true;
            Scratchpad.ReadAndClear();
        }
        else
        {
            Scratchpad.ShowMessage("INVALID ENTRY");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IMultiPage
    // ─────────────────────────────────────────────────────────────────────────

    public void NextPage() => _page = _page == 1 ? 2 : 1;

    public void PrevPage() => _page = _page == 2 ? 1 : 2;
}
