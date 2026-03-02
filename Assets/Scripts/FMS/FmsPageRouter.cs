using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hard function key identifiers for FmsFunctionButton.
/// </summary>
public enum FmsKey { Idx, Fpln, Legs, DepArr, Prog, Dir, Prev, Next, Exec }

/// <summary>
/// Implemented by any FmsPageView that supports PREV/NEXT paging within itself.
/// </summary>
public interface IMultiPage
{
    void NextPage();
    void PrevPage();
}

/// <summary>
/// Central CDU controller. Manages page activation and pumps live telemetry into FmsModel.
/// Attach to the CDU root GameObject and wire all references in the Inspector.
/// </summary>
public class FmsPageRouter : MonoBehaviour
{
    // ── Scene references ────────────────────────────────────────────────────────
    [Header("Sim References")]
    public NavAutopilot navAutopilot;
    public FlightDataBus flightDataBus;
    public FlightPlan flightPlan;
    public FmsScratchpad scratchpad;
    public SimTargets simTargets;

    // ── Page GameObjects ────────────────────────────────────────────────────────
    [Header("CDU Pages (assign GameObjects in Inspector)")]
    public GameObject pageIndex;
    public GameObject pagePosInit;
    public GameObject pageActLegs;
    public GameObject pageModLegs;
    public GameObject pageActFpln;
    public GameObject pageFrequency;
    public GameObject pageStatus;
    public GameObject pageProg;
    public GameObject pageGnssCtl;
    public GameObject pageVordmeCtl;
    public GameObject pageFmsCtl;
    public GameObject pageFix;
    public GameObject pageHold;
    public GameObject pageSecFpln;
    public GameObject pageDepArr;
    public GameObject pageExec;
    public GameObject pagePerf;

    // ── Internal ────────────────────────────────────────────────────────────────
    private readonly FmsModel _model = new();
    private FmsPageView _current;
    private readonly Dictionary<string, FmsPageView> _pages = new();

    public FmsPageView CurrentPage => _current;

    /// <summary>
    /// Set by PerfInitView when the student stages weight data.
    /// Cleared (and confirmed) by the EXEC function key handler.
    /// </summary>
    public bool HasPendingPerf { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable()  => ScenarioRuntime.OnChanged += OnScenarioChanged;
    private void OnDisable() => ScenarioRuntime.OnChanged -= OnScenarioChanged;

    private void Start()
    {
        BuildPageRegistry();

        if (ScenarioRuntime.Current != null)
            _model.LoadFromScenario(ScenarioRuntime.Current);

        ShowPage("Index");
    }

    private void Update()
    {
        // Pump live telemetry into the model
        if (flightDataBus)
        {
            _model.IasKt    = flightDataBus.iasKt;
            _model.AltFtMsl = flightDataBus.altFtMsl;
            _model.HdgDeg   = flightDataBus.hdgDeg;
            _model.VsiFpm   = flightDataBus.vsiFpm;
            _model.BrgDeg   = flightDataBus.brgDeg;
            _model.DistM    = flightDataBus.distM;
        }

        if (navAutopilot)
        {
            _model.ActiveLegIndex = navAutopilot.activeIndex;
            _model.XtkM           = navAutopilot.xtkM;
        }

        // Re-render the active page
        _current?.Populate();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Page management
    // ─────────────────────────────────────────────────────────────────────────

    public void ShowPage(string pageId)
    {
        if (!_pages.TryGetValue(pageId, out var next)) return;

        if (_current != null && _current != next)
            _current.gameObject.SetActive(false);

        _current = next;
        _current.gameObject.SetActive(true);
    }

    public void HandleFunctionKey(FmsKey key)
    {
        switch (key)
        {
            case FmsKey.Idx:    ShowPage("Index");     break;
            case FmsKey.Fpln:   ShowPage("ActFpln");   break;
            case FmsKey.Legs:   ShowPage("ActLegs");   break;
            case FmsKey.DepArr: ShowPage("DepArr");    break;
            case FmsKey.Prog:   ShowPage("Prog");      break;
            case FmsKey.Dir:    ShowPage("ActFpln");   break;
            case FmsKey.Exec:
                if (HasPendingPerf)
                {
                    HasPendingPerf = false;
                    scratchpad?.ShowMessage("PERF ACCEPTED", 1.5f);
                }
                else
                {
                    scratchpad?.ShowMessage("EXEC COMPLETE", 1.5f);
                }
                break;
            case FmsKey.Next:   (_current as IMultiPage)?.NextPage(); break;
            case FmsKey.Prev:   (_current as IMultiPage)?.PrevPage(); break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Accessors for page views
    // ─────────────────────────────────────────────────────────────────────────

    public FmsModel      GetModel()        => _model;
    public FlightPlan    GetFlightPlan()   => flightPlan;
    public NavAutopilot  GetNavAutopilot() => navAutopilot;
    public SimTargets    GetSimTargets()   => simTargets;

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildPageRegistry()
    {
        Register("Index",     pageIndex);
        Register("PosInit",   pagePosInit);
        Register("ActLegs",   pageActLegs);
        Register("ModLegs",   pageModLegs);
        Register("ActFpln",   pageActFpln);
        Register("Frequency", pageFrequency);
        Register("Status",    pageStatus);
        Register("Prog",      pageProg);
        Register("GnssCtl",   pageGnssCtl);
        Register("VordmeCtl", pageVordmeCtl);
        Register("FmsCtl",    pageFmsCtl);
        Register("Fix",       pageFix);
        Register("Hold",      pageHold);
        Register("SecFpln",   pageSecFpln);
        Register("DepArr",    pageDepArr);
        Register("Exec",      pageExec);
        Register("PerfInit",  pagePerf);

        // Hide all pages at startup
        foreach (var kv in _pages)
            kv.Value.gameObject.SetActive(false);
    }

    private void Register(string id, GameObject go)
    {
        if (!go) return;
        var view = go.GetComponent<FmsPageView>();
        if (!view) return;
        view.Init(_model, scratchpad, this);
        _pages[id] = view;
    }

    private void OnScenarioChanged(ScenarioDefinition sd)
    {
        _model.LoadFromScenario(sd);
        ShowPage("Index");
    }
}
