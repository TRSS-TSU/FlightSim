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

        SetLineLabels(1, "ATIS", "");
        SetLineValues(1, Model.FreqAtis, "");

        SetLineLabels(2, "GND", "");
        SetLineValues(2, Model.FreqGnd, "");

        SetLineLabels(3, "TWR", "");
        SetLineValues(3, Model.FreqTwr, "");

        SetLineLabels(4, "DEP", "RDR");
        SetLineValues(4, Model.FreqDep, Model.FreqRdr);

        SetLineLabels(5, "CLNC DEL", "");
        SetLineValues(5, Model.FreqClnc, "");

        SetLineLabels(6, "<IDX", "");
        SetLineValues(6, "", "");
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
