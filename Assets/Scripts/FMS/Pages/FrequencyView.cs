using UnityEngine;

/// <summary>
/// CDU FREQUENCY page — shows radio frequencies for Scenario 01 (NAS Pensacola).
/// Frequencies sourced from context/Scenario.txt.
///
/// Layout:
///   L1 ATIS     125.75
///   L2 GND      121.9
///   L3 TWR      120.65
///   L4 DEP      270.8       RDR  119.1
///   L5 CLNC DEL —
///   L6 <IDX
///
/// LSK press on a frequency line → copies that frequency to the scratchpad.
/// </summary>
public class FrequencyView : FmsPageView
{
    public override void Populate()
    {
        GetTitle()?.SetText("FREQUENCY");
        GetPageNumber()?.SetText("1/1");

        SetLine(1, "ATIS",     Model.FreqAtis, "",    "");
        SetLine(2, "GND",      Model.FreqGnd,  "",    "");
        SetLine(3, "TWR",      Model.FreqTwr,  "",    "");
        SetLine(4, "DEP",      Model.FreqDep,  "RDR", Model.FreqRdr);
        SetLine(5, "CLNC DEL", Model.FreqClnc, "",    "");
        SetLine(6, "<IDX",     "",             "",    "");
    }

    public override void HandleLsk(int side, int row)
    {
        string freq = "";

        if (side == 0)
        {
            switch (row)
            {
                case 1: freq = Model.FreqAtis; break;
                case 2: freq = Model.FreqGnd;  break;
                case 3: freq = Model.FreqTwr;  break;
                case 4: freq = Model.FreqDep;  break;
                case 5: freq = Model.FreqClnc; break;
                case 6: Router.ShowPage("Index"); return;
            }
        }
        else // Right
        {
            switch (row)
            {
                case 4: freq = Model.FreqRdr; break;
                case 6: Router.ShowPage("Index"); return;
            }
        }

        if (!string.IsNullOrEmpty(freq) && freq != "\u2014")
        {
            if (Scratchpad.CurrentText.Length == 0)
                Scratchpad.Append(freq);
            else
                Scratchpad.ShowMessage("CLR SCRATCHPAD FIRST");
        }
    }
}
