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
/// Scratchpad interactions (page 1/2):
///   LSK L1 (empty SP) → copy FMS POS to scratchpad
///   LSK R1 (empty SP) → copy AIRPORT ident to scratchpad
///   LSK L3 (SP with text) → SET POS (currently shows message — live SET POS requires GPS input parsing)
/// </summary>
public class PosInitView : FmsPageView, IMultiPage
{
    private int _page = 1;

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

        SetLine(1, "FMS POS",       posStr,  "AIRPORT",  Model.AirportIdent);
        SetLine(2, "PILOT/REF WPT", refWpt,  "",         "");
        SetLine(3, "SET POS <",     "",      "",         "");
        SetLine(6, "<IDX",          "",      "",         "");
    }

    private void PopulatePage2()
    {
        string posStr = Model.FormatLatLon(Model.FmsPosLat, Model.FmsPosLon);

        SetLine(1, "FMS POS",   posStr,      "",  "");
        SetLine(2, "NAVAID",    "INHIBIT: NO","",  "");
        SetLine(3, "VOR USAGE", "YES",        "",  "");
        SetLine(4, "DME USAGE", "YES",        "",  "");
        SetLine(6, "<IDX",      "",           "",  "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (_page == 1)
        {
            if (side == 0)
            {
                switch (row)
                {
                    case 1:
                        if (Scratchpad.CurrentText.Length == 0)
                            Scratchpad.Append(Model.FormatLatLon(Model.FmsPosLat, Model.FmsPosLon));
                        break;
                    case 3:
                        string entry = Scratchpad.ReadAndClear();
                        if (!string.IsNullOrEmpty(entry))
                            Scratchpad.ShowMessage("SET POS NOT SUPPORTED");
                        break;
                    case 6:
                        Router.ShowPage("Index");
                        break;
                }
            }
            else // Right
            {
                if (row == 1 && Scratchpad.CurrentText.Length == 0)
                    Scratchpad.Append(Model.AirportIdent);
                else if (row == 6)
                    Router.ShowPage("Index");
            }
        }
        else // page 2
        {
            if (row == 6) Router.ShowPage("Index");
        }
    }

    public void NextPage() => _page = _page == 1 ? 2 : 1;
    public void PrevPage() => _page = _page == 2 ? 1 : 2;
}
