/// <summary>
/// CDU PERF INIT page — performance initialisation (ZFW, fuel, gross weight).
///
/// Page 1/2:
///   L1  ZFW      [xxxxxx LBS]    R1  GROSS WT  [xxxxxx LBS]
///   L2  FUEL WT  [xxxxxx LBS]    R2
///   L3-L5  (empty)
///   L6  &lt;IDX
///
/// Page 2/2:
///   L3  V-SPEEDS: NOT IMPL
///   L6  &lt;IDX
///
/// LSK interactions (page 1):
///   L1 — enter ZFW from scratchpad (digits only; sets Router.HasPendingPerf)
///   L2 — enter fuel weight from scratchpad (digits only; sets Router.HasPendingPerf)
///   L6 — return to Index
///   Invalid input → shows "INVALID ENTRY" on scratchpad message line
///
/// After staging weights, student presses EXEC to confirm.
/// </summary>
public class PerfInitView : FmsPageView, IMultiPage
{
    private int _page = 1;

    public override void Populate()
    {
        GetTitle()?.SetText("PERF MENU");
        GetPageNumber()?.SetText($"{_page}/2");

        ClearAllLines();

        if (_page == 1) PopulatePage1();
        else            PopulatePage2();
    }

    private void PopulatePage1()
    {
        string zfw   = Model.ZfwLbs        > 0f ? $"{Model.ZfwLbs:0} LBS"  : "------";
        string fuel  = Model.FuelWeightLbs  > 0f ? $"{Model.FuelWeightLbs:0} LBS" : "------";
        string gross = Model.GrossWeightLbs > 0f ? $"{Model.GrossWeightLbs:0} LBS" : "------";

        SetLine(1, "ZFW",     zfw,  "GROSS WT", gross);
        SetLine(2, "FUEL WT", fuel, "",         "");
        SetLine(3, "",        "",   "",         "");
        SetLine(4, "",        "",   "",         "");
        SetLine(5, "",        "",   "",         "");
        SetLine(6, "<IDX",    "",   "",         "");
    }

    private void PopulatePage2()
    {
        SetLine(1, "",            "",                  "", "");
        SetLine(2, "",            "",                  "", "");
        SetLine(3, "V-SPEEDS", "NOT IMPL",             "", "");
        SetLine(4, "",            "",                  "", "");
        SetLine(5, "",            "",                  "", "");
        SetLine(6, "<IDX",        "",                  "", "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (row == 6) { Router.ShowPage("Index"); return; }

        if (side != 0 || _page != 1) return; // only left LSKs on page 1 are interactive

        string sp = Scratchpad.CurrentText;

        if (row == 1)
        {
            if (sp.Length == 0)
            {
                // Copy current ZFW to scratchpad
                if (Model.ZfwLbs > 0f) Scratchpad.Append($"{Model.ZfwLbs:0}");
                return;
            }
            if (float.TryParse(sp, out float val) && val >= 0f)
            {
                Model.ZfwLbs        = val;
                Router.HasPendingPerf = true;
                Scratchpad.ReadAndClear();
            }
            else
            {
                Scratchpad.ShowMessage("INVALID ENTRY");
            }
            return;
        }

        if (row == 2)
        {
            if (sp.Length == 0)
            {
                // Copy current fuel to scratchpad
                if (Model.FuelWeightLbs > 0f) Scratchpad.Append($"{Model.FuelWeightLbs:0}");
                return;
            }
            if (float.TryParse(sp, out float val) && val >= 0f)
            {
                Model.FuelWeightLbs   = val;
                Router.HasPendingPerf = true;
                Scratchpad.ReadAndClear();
            }
            else
            {
                Scratchpad.ShowMessage("INVALID ENTRY");
            }
            return;
        }
    }

    public void NextPage() => _page = _page == 1 ? 2 : 1;
    public void PrevPage() => _page = _page == 2 ? 1 : 2;
}
