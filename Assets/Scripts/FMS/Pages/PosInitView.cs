using UnityEngine;

/// <summary>
/// CDU POS INIT page — position initialisation (2-page view).
/// Use NEXT/PREV function keys to switch between page 1/2 and 2/2.
///
/// Page 1/2:
///   L1  FMS POS   [lat/lon]
///   L2  Airport   [ident]
///   L3  PILOT/REF WPT  [ident]
///   R4  SET POS TO GNSS  [gnss lat/lon&gt;]
///   R5  SET POS   [confirmed pos]
///   L6  &lt;INDEX            R6  FPLN&gt;
///
/// Page 2/2:
///   L1  FMS POS   [lat/lon]
///   L2  NAVAID    INHIBIT: NO
///   L3  VOR USAGE  YES
///   L4  DME USAGE  YES
///   L6  &lt;INDEX
///
/// LSK interactions (page 1/2):
///   R4 → SET POS TO GNSS (captures scenario waypoints[0], starts 3-sec load timer)
///   L6 → INDEX    R6 → ActFpln
///
/// Formatting: labels cyan (#00FFFF), values white (#FFFFFF) via FmtLabel/FmtValue.
/// </summary>
public class PosInitView : FmsPageView, IMultiPage
{
    // ── Formatting helpers ───────────────────────────────────────────────────────
    private string FmtTitle()             => "POS INIT";
    private string FmtLabel(string label) => string.IsNullOrEmpty(label) ? label : $"<color=#00FFFF>{label}</color>";
    private string FmtValue(string value) => string.IsNullOrEmpty(value) ? value : $"<color=#FFFFFF>{value}</color>";

    // ── State ────────────────────────────────────────────────────────────────────
    private int    _page            = 1;
    private bool   _gnssPosSet     = false;
    private string _confirmedPos   = null;
    private string _initPos        = "---\u00B0--.-- ----\u00B0--.--";
    private bool   _posLoadComplete = false;

    // ─────────────────────────────────────────────────────────────────────────
    // FmsPageView contract
    // ─────────────────────────────────────────────────────────────────────────

    public override void Populate()
    {
        ClearAllLines();
        GetTitle()?.SetText(FmtTitle());
        GetPageNumber()?.SetText($"{_page}/2");
        GetMessageLine()?.SetText("");

        if (_page == 1) PopulatePage1();
        else            PopulatePage2();
    }

    private void PopulatePage1()
    {
        string posStr = Model.FormatLatLon(Model.FmsPosLat, Model.FmsPosLon);
        string refWpt = string.IsNullOrEmpty(Model.RefWptIdent) ? "\u2014" : Model.RefWptIdent;

        var wp0 = (Model.Scenario?.waypoints?.Count > 0) ? Model.Scenario.waypoints[0] : null;
        string gnssStr = wp0 != null ? Model.FormatLatLon(wp0.latDeg, wp0.lonDeg) : posStr;

        string fmsDisplay = (_posLoadComplete && _confirmedPos != null) ? _confirmedPos : posStr;

        SetLineLabels(1, FmtLabel("FMS POS"), "");
        SetLineValues(1, FmtValue(fmsDisplay), "");

        SetLineLabels(2, FmtLabel("Airport"), "");
        SetLineValues(2, FmtValue(Model.AirportIdent), "");

        SetLineLabels(3, FmtLabel("PILOT/REF WPT"), "");
        SetLineValues(3, FmtValue(refWpt), "");

        SetLineLabels(4, "", FmtLabel(_gnssPosSet ? "COMPLETED" : "SET POS TO GNSS"));
        SetLineValues(4, "", FmtValue(gnssStr + ">"));

        SetLineLabels(5, "", FmtLabel("SET POS"));
        SetLineValues(5, "", FmtValue(_confirmedPos ?? _initPos));

        SetLineLabels(6, FmtLabel("<INDEX"), FmtLabel("FPLN>"));
        SetLineValues(6, "", "");
    }

    private void PopulatePage2()
    {
        string posStr = Model.FormatLatLon(Model.FmsPosLat, Model.FmsPosLon);

        SetLineLabels(1, FmtLabel("FMS POS"), "");
        SetLineValues(1, FmtValue(posStr), "");

        SetLineLabels(2, FmtLabel("NAVAID"), "");
        SetLineValues(2, FmtValue("INHIBIT: NO"), "");

        SetLineLabels(3, FmtLabel("VOR USAGE"), "");
        SetLineValues(3, FmtValue("YES"), "");

        SetLineLabels(4, FmtLabel("DME USAGE"), "");
        SetLineValues(4, FmtValue("YES"), "");

        SetLineLabels(5, "", "");
        SetLineValues(5, "", "");

        SetLineLabels(6, FmtLabel("<INDEX"), FmtLabel("FPLN"));
        SetLineValues(6, "", "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (_page == 1)
        {
            if (side == 0)  // Left
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
                    case 4: SetPosToGnss(); break;
                    case 5: // inactive (display only)
                        break;
                    case 6: Router.ShowPage("ActFpln"); break;
                }
            }
        }
        else  // page 2
        {
            if (side == 0)  // Left
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
        }
        // NOTE: Populate() is NOT called here — FmsPageRouter.Update() pumps it every frame.
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void SetPosToGnss()
    {
        _gnssPosSet = true;
        var wp = (Model.Scenario?.waypoints?.Count > 0) ? Model.Scenario.waypoints[0] : null;
        _confirmedPos = wp != null
            ? Model.FormatLatLon(wp.latDeg, wp.lonDeg)
            : Model.FormatLatLon(Model.FmsPosLat, Model.FmsPosLon);
        StartCoroutine(LoadPosAfterDelay());
    }

    private System.Collections.IEnumerator LoadPosAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        _posLoadComplete = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IMultiPage
    // ─────────────────────────────────────────────────────────────────────────

    public void NextPage() => _page = _page == 1 ? 2 : 1;
    public void PrevPage() => _page = _page == 2 ? 1 : 2;
}
