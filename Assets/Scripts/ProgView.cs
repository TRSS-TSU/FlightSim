using UnityEngine;

/// <summary>
/// CDU PROGRESS page — live in-flight situational awareness (2-page view).
///
/// Page 1/2: Leg-by-leg progress (FROM / TO / DEST) and cross-track error.
///   L1  FROM  [last WP]     DIST  [nm to active]
///   L2  TO    [active WP]   ETE   [mm:ss]
///   L3  DEST  [last WP]     DIST  —
///   L4  (empty)
///   L5  XTK   [±nm L/R]
///   L6  &lt;IDX
///
/// Page 2/2: Current aircraft state snapshot.
///   L1  IAS   [kt]          HDG   [ddd°]
///   L2  ALT   [ft]          VSI   [±fpm]
///   L3–L5  (empty)
///   L6  &lt;IDX
///
/// All values update every frame from FmsModel (pumped by FmsPageRouter.Update).
///
/// Formatting: labels cyan (#00FFFF), values white (#FFFFFF) via FmtLabel/FmtValue.
/// </summary>
public class ProgView : FmsPageView, IMultiPage
{
    // ── Formatting helpers ───────────────────────────────────────────────────────
    private string FmtTitle() => "PROGRESS";

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
        var route = Model.ActiveRoute;
        int active = Model.ActiveLegIndex;

        string fromIdent =
            (active > 0 && active - 1 < route.Count) ? route[active - 1].ident : "\u2014";
        string toIdent = active < route.Count ? route[active].ident : "\u2014";
        string destIdent = route.Count > 0 ? route[route.Count - 1].ident : "\u2014";

        float distNm = Model.DistNm;
        string eteStr = Model.FormatEte(distNm, Model.IasKt);
        string distStr = $"{distNm:0.0}NM";

        SetLineLabels(1, FmtLabel("FROM"), FmtLabel("DIST"));
        SetLineValues(1, FmtValue(fromIdent), FmtValue(distStr));

        SetLineLabels(2, FmtLabel("TO"), FmtLabel("ETE"));
        SetLineValues(2, FmtValue(toIdent), FmtValue(eteStr));

        SetLineLabels(3, FmtLabel("DEST"), FmtLabel("DIST"));
        SetLineValues(3, FmtValue(destIdent), FmtValue("\u2014"));

        SetLineLabels(4, "", "");
        SetLineValues(4, "", "");

        float xtkNm = Mathf.Abs(Model.XtkM) / 1852f;
        string xtkDir = Model.XtkM >= 0f ? "R" : "L";
        SetLineLabels(5, FmtLabel("XTK"), "");
        SetLineValues(5, FmtValue($"{xtkNm:0.00}NM {xtkDir}"), "");

        SetLineLabels(6, FmtLabel("<IDX"), "");
        SetLineValues(6, "", "");
    }

    private void PopulatePage2()
    {
        SetLineLabels(1, FmtLabel("IAS"), FmtLabel("HDG"));
        SetLineValues(1, FmtValue($"{Model.IasKt:0}KT"), FmtValue($"{Model.HdgDeg:000}\u00B0"));

        SetLineLabels(2, FmtLabel("ALT"), FmtLabel("VSI"));
        SetLineValues(2, FmtValue($"{Model.AltFtMsl:0}FT"), FmtValue($"{Model.VsiFpm:+0;-0}FPM"));

        SetLineLabels(3, "", "");
        SetLineValues(3, "", "");

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
                case 6: // inactive
                    break;
            }
        }
        // NOTE: Populate() is NOT called here — FmsPageRouter.Update() pumps it every frame.
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IMultiPage
    // ─────────────────────────────────────────────────────────────────────────

    public void NextPage() => _page = _page == 1 ? 2 : 1;

    public void PrevPage() => _page = _page == 2 ? 1 : 2;
}
