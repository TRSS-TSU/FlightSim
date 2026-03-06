using UnityEngine;

/// <summary>
/// Generic "not yet implemented" page.
/// Shared by all CDU pages not yet assigned a dedicated view script.
/// Set <see cref="pageTitle"/> in the Inspector to match the page name (e.g. "GNSS CTL").
/// LSK L6 always returns to the Index page. All other LSKs are inactive.
///
/// Formatting: labels cyan (#00FFFF), values white (#FFFFFF) via FmtLabel/FmtValue.
/// </summary>
public class StubPageView : FmsPageView
{
    // ── Formatting helpers ───────────────────────────────────────────────────────
    private string FmtTitle() => pageTitle;

    private string FmtLabel(string label) =>
        string.IsNullOrEmpty(label) ? label : $"<color=#00FFFF>{label}</color>";

    private string FmtValue(string value) =>
        string.IsNullOrEmpty(value) ? value : $"<color=#FFFFFF>{value}</color>";

    // ── Inspector ────────────────────────────────────────────────────────────────
    [SerializeField]
    private string pageTitle = "PAGE";

    // ─────────────────────────────────────────────────────────────────────────
    // FmsPageView contract
    // ─────────────────────────────────────────────────────────────────────────

    public override void Populate()
    {
        ClearAllLines();
        GetTitle()?.SetText(FmtTitle());
        GetPageNumber()?.SetText("1/1");
        GetMessageLine()?.SetText("");

        SetLineValues(3, FmtValue("NOT IMPLEMENTED"), "");

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
}
