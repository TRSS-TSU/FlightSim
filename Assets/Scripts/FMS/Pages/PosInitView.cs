using UnityEngine;

/// <summary>
/// CDU POS INIT page — position initialisation (2-page view).
/// Use NEXT/PREV function keys to switch between page 1/2 and 2/2.
///
/// Page 1/2:
///   L1 FMS POS  [lat/lon]    R1 AIRPORT [ident]
///   L2 PILOT/REF WPT [ident]
///   L3 SET POS <
///   L6 <IDX
///
/// Page 2/2:
///   L1 FMS POS  [lat/lon]
///   L2 NAVAID   INHIBIT: NO
///   L3 VOR USAGE  YES
///   L4 DME USAGE  YES
///   L6 <IDX
///
/// LSK interactions (page 1/2):
///   LSK R4 → SET POS TO GNSS (captures scenario waypoints[0], starts 3-sec load timer)
///   LSK L6/R6 → INDEX
///</summary>
public class PosInitView : FmsPageView, IMultiPage
{
    private int _page = 1;
    private bool   _gnssPosSet = false;
    private string _confirmedPos = null;
    private bool   _posLoadComplete = false;

    public override void Populate()
    {
        GetTitle()?.SetText("POS INIT");
        GetPageNumber()?.SetText($"{_page}/2");

        ClearAllLines();

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
        SetLine(1, "FMS POS", fmsDisplay, "", "");
        SetLine(2, "Airport", Model.AirportIdent, "", "");
        SetLine(3, "PILOT/REF WPT", refWpt, "", "");
        SetLine(4, "", "", _gnssPosSet ? "COMPLETED" : "SET POS TO GNSS", gnssStr + ">");
        SetLine(5, "", "", "SET POS", _confirmedPos ?? posStr);
        SetLine(6, "<INDEX", "", "FPLN>", "");
    }

    private void PopulatePage2()
    {
        string posStr = Model.FormatLatLon(Model.FmsPosLat, Model.FmsPosLon);

        SetLine(1, "FMS POS", posStr, "", "");
        SetLine(2, "NAVAID", "INHIBIT: NO","", "");
        SetLine(3, "VOR USAGE", "YES", "",  "");
        SetLine(4, "DME USAGE", "YES", "", "");
        SetLine(6, "<INDEX", "", "FPLN", "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (_page == 1)
        {
            if (side == 0)
            {
                if (row == 6) Router.ShowPage("Index");
            }
            else // Right
            {
                if (row == 4)
                {
                    _gnssPosSet = true;
                    var wp = (Model.Scenario?.waypoints?.Count > 0) ? Model.Scenario.waypoints[0] : null;
                    _confirmedPos = wp != null
                        ? Model.FormatLatLon(wp.latDeg, wp.lonDeg)
                        : Model.FormatLatLon(Model.FmsPosLat, Model.FmsPosLon);
                    StartCoroutine(LoadPosAfterDelay());
                }
                else if (row == 6) Router.ShowPage("ActFpln");
            }
        }
        else // page 2
        {
            if (row == 6) Router.ShowPage("Index");
        }
    }

    private System.Collections.IEnumerator LoadPosAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        _posLoadComplete = true;
    }

    public void NextPage() => _page = _page == 1 ? 2 : 1;
    public void PrevPage() => _page = _page == 2 ? 1 : 2;
}
