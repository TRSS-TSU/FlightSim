/// <summary>
/// CDU TUNE page — radio frequency tuning with active/standby swap and callsign entry.
///
/// Layout:
///   L1  ATIS     [active]      R1  STBY  [standby or ----]
///   L2  GND      [active]      R2  STBY  [standby or ----]
///   L3  TWR      [active]      R3  STBY  [standby or ----]
///   L4  DEP      [active]      R4  RDR   [active]
///   L5  CALLSIGN [ActiveCallsign]
///   L6  &lt;IDX
///
/// LSK interactions:
///   Lx (empty SP)        → copy active freq to scratchpad
///   Lx (SP = valid freq) → swap: active = SP, standby = old active; clear SP
///   Lx (SP = invalid)    → ShowMessage("INVALID FREQ")
///   Rx (empty SP)        → copy standby to scratchpad (or "----" if empty)
///   Rx (SP = valid freq) → set standby = SP, clear SP
///   L5 (SP filled)       → set ActiveCallsign = SP.ToUpper(), clear SP
///   L6                   → Index
///
/// Frequency validation: float in range [108.0, 400.0].
///
/// Formatting: labels cyan (#00FFFF), values white (#FFFFFF) via FmtLabel/FmtValue.
/// </summary>
public class TuneView : FmsPageView
{
    // ── Formatting helpers ───────────────────────────────────────────────────────
    private string FmtTitle() => "TUNE";

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

        string stbyAtis = string.IsNullOrEmpty(Model.StandbyFreqAtis)
            ? "----"
            : Model.StandbyFreqAtis;
        string stbyGnd = string.IsNullOrEmpty(Model.StandbyFreqGnd) ? "----" : Model.StandbyFreqGnd;
        string stbyTwr = string.IsNullOrEmpty(Model.StandbyFreqTwr) ? "----" : Model.StandbyFreqTwr;

        SetLineLabels(1, FmtLabel("ATIS"), FmtLabel("STBY"));
        SetLineValues(1, FmtValue(Model.FreqAtis), FmtValue(stbyAtis));

        SetLineLabels(2, FmtLabel("GND"), FmtLabel("STBY"));
        SetLineValues(2, FmtValue(Model.FreqGnd), FmtValue(stbyGnd));

        SetLineLabels(3, FmtLabel("TWR"), FmtLabel("STBY"));
        SetLineValues(3, FmtValue(Model.FreqTwr), FmtValue(stbyTwr));

        SetLineLabels(4, FmtLabel("DEP"), FmtLabel("RDR"));
        SetLineValues(4, FmtValue(Model.FreqDep), FmtValue(Model.FreqRdr));

        SetLineLabels(5, FmtLabel("CALLSIGN"), "");
        SetLineValues(5, FmtValue(Model.ActiveCallsign), "");

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
                    HandleActiveFreq(ref Model.FreqAtis, ref Model.StandbyFreqAtis);
                    break;
                case 2:
                    HandleActiveFreq(ref Model.FreqGnd, ref Model.StandbyFreqGnd);
                    break;
                case 3:
                    HandleActiveFreq(ref Model.FreqTwr, ref Model.StandbyFreqTwr);
                    break;
                case 4:
                    HandleActiveFreq(ref Model.FreqDep, ref Model.StandbyFreqDep);
                    break;
                case 5:
                    HandleCallsign();
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
                case 1:
                    HandleStandbyFreq(ref Model.FreqAtis, ref Model.StandbyFreqAtis);
                    break;
                case 2:
                    HandleStandbyFreq(ref Model.FreqGnd, ref Model.StandbyFreqGnd);
                    break;
                case 3:
                    HandleStandbyFreq(ref Model.FreqTwr, ref Model.StandbyFreqTwr);
                    break;
                case 4:
                    HandleStandbyFreq(ref Model.FreqRdr, ref Model.StandbyFreqRdr);
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

    /// <summary>
    /// Left LSK on a frequency row:
    ///   empty SP  → copy active to scratchpad
    ///   valid freq → swap active ↔ standby, clear SP
    ///   invalid   → ShowMessage
    /// </summary>
    private void HandleActiveFreq(ref string active, ref string standby)
    {
        string sp = Scratchpad.CurrentText;
        if (sp.Length == 0)
        {
            Scratchpad.Append(active);
            return;
        }
        if (IsValidFreq(sp))
        {
            string old = active;
            active = sp;
            standby = old;
            Scratchpad.ReadAndClear();
        }
        else
        {
            Scratchpad.ShowMessage("INVALID FREQ");
        }
    }

    /// <summary>
    /// Right LSK on a frequency row:
    ///   empty SP  → copy standby to scratchpad (or "----")
    ///   valid freq → set standby = SP, clear SP
    ///   invalid   → ShowMessage
    /// </summary>
    private void HandleStandbyFreq(ref string active, ref string standby)
    {
        string sp = Scratchpad.CurrentText;
        if (sp.Length == 0)
        {
            string display = string.IsNullOrEmpty(standby) ? "----" : standby;
            Scratchpad.Append(display);
            return;
        }
        if (IsValidFreq(sp))
        {
            standby = sp;
            Scratchpad.ReadAndClear();
        }
        else
        {
            Scratchpad.ShowMessage("INVALID FREQ");
        }
    }

    private void HandleCallsign()
    {
        string sp = Scratchpad.CurrentText;
        if (sp.Length == 0)
            return;
        Model.ActiveCallsign = sp.ToUpper();
        Scratchpad.ReadAndClear();
    }

    private static bool IsValidFreq(string s) =>
        float.TryParse(
            s,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float f
        )
        && f >= 108.0f
        && f <= 400.0f;
}
