using UnityEngine;

/// <summary>
/// CDU FREQUENCY page — shows radio frequencies for Scenario 01 (NAS Pensacola).
/// Frequencies sourced from context/Scenario.txt.
///
/// Layout:
///   L1  ATIS      125.75
///   L2  GND       121.9
///   L3  TWR       120.65
///   L4  DEP       270.8       RDR  119.1
///   L5  CLNC DEL  —
///   L6  &lt;IDX
///
/// LSK press on a frequency line → copies that frequency to the scratchpad
/// (if scratchpad is empty; otherwise shows CLR SCRATCHPAD FIRST).
///
/// Formatting: labels cyan (#00FFFF), values white (#FFFFFF) via FmtLabel/FmtValue.
/// </summary>
public class FrequencyView : FmsPageView
{
    // ── Formatting helpers ───────────────────────────────────────────────────────
    private string FmtTitle()             => "FREQUENCY";
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

        SetLineLabels(1, FmtLabel("ATIS"), "");
        SetLineValues(1, FmtValue(Model.FreqAtis), "");

        SetLineLabels(2, FmtLabel("GND"), "");
        SetLineValues(2, FmtValue(Model.FreqGnd), "");

        SetLineLabels(3, FmtLabel("TWR"), "");
        SetLineValues(3, FmtValue(Model.FreqTwr), "");

        SetLineLabels(4, FmtLabel("DEP"), FmtLabel("RDR"));
        SetLineValues(4, FmtValue(Model.FreqDep), FmtValue(Model.FreqRdr));

        SetLineLabels(5, FmtLabel("CLNC DEL"), "");
        SetLineValues(5, FmtValue(Model.FreqClnc), "");

        SetLineLabels(6, FmtLabel("<IDX"), "");
        SetLineValues(6, "", "");
    }

    public override void HandleLsk(int side, int row)
    {
        if (side == 0)  // Left
        {
            switch (row)
            {
                case 1: CopyFreq(Model.FreqAtis);  break;
                case 2: CopyFreq(Model.FreqGnd);   break;
                case 3: CopyFreq(Model.FreqTwr);   break;
                case 4: CopyFreq(Model.FreqDep);   break;
                case 5: CopyFreq(Model.FreqClnc);  break;
                case 6: Router.ShowPage("Index");  break;
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
                case 4: CopyFreq(Model.FreqRdr); break;
                case 5: // inactive
                    break;
                case 6: Router.ShowPage("Index"); break;
            }
        }
        // NOTE: Populate() is NOT called here — FmsPageRouter.Update() pumps it every frame.
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Copies a frequency string to the scratchpad if it is non-empty and not a dash.</summary>
    private void CopyFreq(string freq)
    {
        if (string.IsNullOrEmpty(freq) || freq == "\u2014") return;

        if (Scratchpad.CurrentText.Length == 0)
            Scratchpad.Append(freq);
        else
            Scratchpad.ShowMessage("CLR SCRATCHPAD FIRST");
    }
}
