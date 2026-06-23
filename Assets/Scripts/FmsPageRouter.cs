using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hard function key identifiers for FmsFunctionButton.
/// </summary>
public enum FmsKey
{
    Index,
    Fpln,
    Legs,
    DepArr,
    Prog,
    Dir,
    Prev,
    Next,
    Exec,
    Tune,
}

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
    public FmsRouteActivationCoordinator routeActivationCoordinator;

    // ── Page GameObjects ────────────────────────────────────────────────────────
    [Header("CDU Pages (assign GameObjects in Inspector)")]
    public GameObject pageIndex;
    public GameObject pagePosInit;
    public GameObject pageActLegs;
    public GameObject pageModLegs;
    public GameObject pageActFpln;
    public GameObject pageTune;
    public GameObject pageStatus;
    public GameObject pageProg;
    public GameObject pageGnssCtl;
    public GameObject pageVordmeCtl;
    public GameObject pageFmsCtl;
    public GameObject pageFix;
    public GameObject pageHold;
    public GameObject pageSecFpln;
    public GameObject pageDepArr;
    public GameObject pageDir;
    public GameObject pagePerf;

    // ── Internal ────────────────────────────────────────────────────────────────
    private readonly FmsModel _model = new();
    private FmsPageView _current;
    private readonly Dictionary<string, FmsPageView> _pages = new();
    private float _refreshTimer;
    private const float RefreshInterval = 0.1f; // 10 Hz
    private bool _hasPendingRouteActivation;
    private bool _pendingClearArrivalLoaded;
    private bool? _pendingArrivalLoaded;
    private string _pendingArrivalName;
    private RouteContinuitySnapshot _pendingRouteSnapshot;

    public FmsPageView CurrentPage => _current;
    public bool HasPendingRouteActivation => _hasPendingRouteActivation;

    /// <summary>
    /// Set by PerfInitView when the student stages weight data.
    /// Cleared (and confirmed) by the EXEC function key handler.
    /// </summary>
    public bool HasPendingPerf { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable() => ScenarioRuntime.OnChanged += OnScenarioChanged;

    private void OnDisable() => ScenarioRuntime.OnChanged -= OnScenarioChanged;

    private void Start()
    {
        BuildPageRegistry();

        if (!routeActivationCoordinator)
#if UNITY_2023_1_OR_NEWER
            routeActivationCoordinator = FindFirstObjectByType<FmsRouteActivationCoordinator>();
#else
            routeActivationCoordinator = FindObjectOfType<FmsRouteActivationCoordinator>();
#endif

        if (ScenarioRuntime.Current != null)
            _model.LoadFromScenario(ScenarioRuntime.Current);

        ShowPage("Index");
    }

    private void Update()
    {
        // Pump live telemetry into the model
        if (flightDataBus)
        {
            _model.IasKt = flightDataBus.iasKt;
            _model.AltFtMsl = flightDataBus.altFtMsl;
            _model.HdgDeg = flightDataBus.hdgDeg;
            _model.VsiFpm = flightDataBus.vsiFpm;
            _model.BrgDeg = flightDataBus.brgDeg;
            _model.DistM = flightDataBus.distM;
        }

        if (navAutopilot)
        {
            _model.ActiveLegIndex = navAutopilot.activeIndex;
            _model.XtkM = navAutopilot.xtkM;
        }

        // Re-render the active page at 10 Hz
        _refreshTimer += Time.deltaTime;
        if (_refreshTimer < RefreshInterval)
            return;
        _refreshTimer = 0f;
        _current?.Populate();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Page management
    // ─────────────────────────────────────────────────────────────────────────

    public void ShowPage(string pageId)
    {
        if (!_pages.TryGetValue(pageId, out var next))
            return;

        if (_current != null && _current != next)
            _current.gameObject.SetActive(false);

        _current = next;
        _current.gameObject.SetActive(true);
    }

    public void HandleFunctionKey(FmsKey key)
    {
        switch (key)
        {
            case FmsKey.Index:
                ShowPage("Index");
                break;
            case FmsKey.Fpln:
                ShowPage("ActFpln");
                break;
            case FmsKey.Legs:
                ShowPage("ActLegs");
                break;
            case FmsKey.DepArr:
                ShowPage("DepArr");
                break;
            case FmsKey.Prog:
                ShowPage("Prog");
                break;
            case FmsKey.Dir:
                ShowPage("Dir");
                break;
            case FmsKey.Tune:
                ShowPage("Tune");
                break;
            case FmsKey.Exec:
                if (_current is ActFplnView actFpln && actFpln.HasArmedMod)
                {
                    actFpln.HandleExec();
                    break;
                }
                if (_hasPendingRouteActivation)
                {
                    ExecutePendingActiveRoute();
                    break;
                }
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
            case FmsKey.Next:
                (_current as IMultiPage)?.NextPage();
                break;
            case FmsKey.Prev:
                (_current as IMultiPage)?.PrevPage();
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Accessors for page views
    // ─────────────────────────────────────────────────────────────────────────

    public FmsModel GetModel() => _model;

    public List<ScenarioDefinition.WaypointDef> GetRouteForDisplay()
        => _model.ModRoute.Count > 0 ? _model.ModRoute : _model.ActiveRoute;

    public bool HasPendingArrivalLoaded => _pendingArrivalLoaded == true;

    public bool HasPendingArrivalChange => _pendingArrivalLoaded.HasValue;

    public bool PendingArrivalLoaded => _pendingArrivalLoaded == true;

    public string PendingArrivalName => _pendingArrivalLoaded == true ? _pendingArrivalName : "";

    public FlightPlan GetFlightPlan() => flightPlan;

    public NavAutopilot GetNavAutopilot() => navAutopilot;

    public SimTargets GetSimTargets() => simTargets;

    /// <summary>
    /// Capture a continuity snapshot of the current route and nav state.
    /// Call this BEFORE mutating Model.ActiveRoute, then pass the result to
    /// CommitActiveRoute() after the mutation so the resolver can map old
    /// navigation context onto the rebuilt waypoint array.
    /// </summary>
    public RouteContinuitySnapshot CaptureRouteContinuity()
    {
        int count = _model.ActiveRoute.Count;
        int idx   = navAutopilot
            ? Mathf.Clamp(navAutopilot.activeIndex, 0, Mathf.Max(0, count - 1))
            : 0;

        var idents = new List<string>(count);
        foreach (var w in _model.ActiveRoute)
            idents.Add(w.ident);

        Vector3 flatPos = Vector3.zero;
        Vector3 flatFwd = Vector3.forward;
        if (navAutopilot && navAutopilot.aircraft)
        {
            flatPos = Vector3.ProjectOnPlane(navAutopilot.aircraft.position, Vector3.up);
            flatFwd = Vector3.ProjectOnPlane(navAutopilot.aircraft.forward,  Vector3.up);
            if (flatFwd.sqrMagnitude > 0.001f) flatFwd.Normalize();
        }

        return new RouteContinuitySnapshot
        {
            oldRouteIdents  = idents,
            oldActiveIndex  = idx,
            oldFromIdent    = (idx > 0 && count > 0) ? _model.ActiveRoute[idx - 1].ident : "",
            oldToIdent      = (idx < count)           ? _model.ActiveRoute[idx].ident     : "",
            aircraftFlatPos = flatPos,
            aircraftFlatFwd = flatFwd,
        };
    }

    /// <summary>
    /// Stage or execute route activation after Model.ActiveRoute has been mutated.
    /// Page edits stage a MOD route; EXEC performs tile preload and scene waypoint rebuild.
    /// </summary>
    public void CommitActiveRoute(RouteContinuitySnapshot snapshot,
                                   bool clearArrivalLoaded = false,
                                   bool executeNow = false)
    {
        if (clearArrivalLoaded)
        {
            _model.ArrivalLoaded = false;
            _model.ArrivalName = "";
        }

        if (executeNow)
        {
            ExecuteActiveRoute(snapshot);
            return;
        }

        _pendingRouteSnapshot = snapshot;
        _pendingClearArrivalLoaded |= clearArrivalLoaded;
        _model.ModRoute = new List<ScenarioDefinition.WaypointDef>(_model.ActiveRoute);
        _hasPendingRouteActivation = true;
        scratchpad?.ShowMessage("MOD ROUTE - EXEC", 1.5f);
    }

    public void StageActiveRoute(List<ScenarioDefinition.WaypointDef> route,
                                  RouteContinuitySnapshot snapshot,
                                  bool clearArrivalLoaded = false,
                                  bool? arrivalLoadedAfterExec = null,
                                  string arrivalNameAfterExec = "")
    {
        if (route == null || route.Count == 0)
        {
            scratchpad?.ShowMessage("NO ROUTE", 1.5f);
            return;
        }

        _model.ModRoute = new List<ScenarioDefinition.WaypointDef>(route);
        _pendingRouteSnapshot = snapshot;
        _pendingClearArrivalLoaded |= clearArrivalLoaded;
        _pendingArrivalLoaded = arrivalLoadedAfterExec ?? _pendingArrivalLoaded;
        if (arrivalLoadedAfterExec == true)
            _pendingArrivalName = arrivalNameAfterExec ?? "";
        _hasPendingRouteActivation = true;
        scratchpad?.ShowMessage("MOD ROUTE - EXEC", 1.5f);
    }

    public void ExecutePendingActiveRoute()
    {
        if (!_hasPendingRouteActivation)
        {
            scratchpad?.ShowMessage("NO MOD", 1.5f);
            return;
        }

        if (_pendingClearArrivalLoaded)
        {
            _model.ArrivalLoaded = false;
            _model.ArrivalName = "";
        }

        ExecuteActiveRoute(_pendingRouteSnapshot, _model.ModRoute);
    }

    private void ExecuteActiveRoute(RouteContinuitySnapshot snapshot,
                                    List<ScenarioDefinition.WaypointDef> routeOverride = null)
    {
        var sd = _model.Scenario;
        var routeSource = routeOverride != null && routeOverride.Count > 0
            ? routeOverride
            : _model.ActiveRoute;

        if (sd == null || routeSource.Count == 0)
        {
            scratchpad?.ShowMessage("NO ROUTE", 1.5f);
            return;
        }

        var routeCopy = new List<ScenarioDefinition.WaypointDef>(routeSource);
        bool forceFirstLeg = flightPlan == null
            || flightPlan.waypoints == null
            || flightPlan.waypoints.Length == 0;

        if (routeActivationCoordinator)
        {
            if (routeActivationCoordinator.IsExecuting)
            {
                scratchpad?.ShowMessage("EXEC BUSY", 1.5f);
                return;
            }

            if (!routeActivationCoordinator.flightPlan)
                routeActivationCoordinator.flightPlan = flightPlan;
            if (!routeActivationCoordinator.navAutopilot)
                routeActivationCoordinator.navAutopilot = navAutopilot;

            _hasPendingRouteActivation = false;
            _pendingClearArrivalLoaded = false;
            ApplyPendingModelRoute(routeCopy);

            routeActivationCoordinator.ExecuteModifiedRoute(
                sd,
                routeCopy,
                snapshot,
                forceFirstLeg,
                OnRouteActivationComplete
            );
            return;
        }

        if (!flightPlan)
        {
            scratchpad?.ShowMessage("NO FLIGHT PLAN", 1.5f);
            return;
        }

        _hasPendingRouteActivation = false;
        _pendingClearArrivalLoaded = false;
        ApplyPendingModelRoute(routeCopy);

        if (navAutopilot)
            navAutopilot.SetNavEngaged(false);

        int zoom = sd.preloadZoomOverride > 0 ? sd.preloadZoomOverride : sd.baseZoom;
        flightPlan.ActivateRouteFromFms(sd, routeCopy, zoom);

        int activeIndex = 0;
        if (navAutopilot)
        {
            activeIndex = forceFirstLeg
                ? 0
                : RouteResolver.Resolve(snapshot, routeCopy, flightPlan.waypoints);
            navAutopilot.activeIndex = activeIndex;
            navAutopilot.ResetCaptureState();
        }

        OnRouteActivationComplete(activeIndex);
    }

    private void OnRouteActivationComplete(int activeIndex)
    {
        _model.ActiveLegIndex = activeIndex;
        scratchpad?.ShowMessage("ROUTE EXEC", 1.5f);
    }

    private void ApplyPendingModelRoute(List<ScenarioDefinition.WaypointDef> routeCopy)
    {
        _model.ActiveRoute = new List<ScenarioDefinition.WaypointDef>(routeCopy);
        _model.ModRoute.Clear();

        if (_pendingArrivalLoaded.HasValue)
        {
            _model.ArrivalLoaded = _pendingArrivalLoaded.Value;
            _model.ArrivalName = _pendingArrivalLoaded.Value ? _pendingArrivalName : "";
        }

        _pendingArrivalLoaded = null;
        _pendingArrivalName = "";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildPageRegistry()
    {
        Register("Index", pageIndex);
        Register("PosInit", pagePosInit);
        Register("ActLegs", pageActLegs);
        Register("ModLegs", pageModLegs);
        Register("ActFpln", pageActFpln);
        Register("Tune", pageTune);
        Register("Status", pageStatus);
        Register("Prog", pageProg);
        Register("GnssCtl", pageGnssCtl);
        Register("VordmeCtl", pageVordmeCtl);
        Register("FmsCtl", pageFmsCtl);
        Register("Fix", pageFix);
        Register("Hold", pageHold);
        Register("SecFpln", pageSecFpln);
        Register("DepArr", pageDepArr);
        Register("Dir", pageDir);
        Register("PerfInit", pagePerf);

        // Hide all pages at startup
        foreach (var kv in _pages)
            kv.Value.gameObject.SetActive(false);
    }

    private void Register(string id, GameObject go)
    {
        if (!go)
            return;
        var view = go.GetComponent<FmsPageView>();
        if (!view)
            return;
        view.Init(_model, scratchpad, this);
        _pages[id] = view;
    }

    private void OnScenarioChanged(ScenarioDefinition sd)
    {
        _hasPendingRouteActivation = false;
        _pendingClearArrivalLoaded = false;
        _pendingArrivalLoaded = null;
        _pendingArrivalName = "";
        _model.ModRoute.Clear();
        _model.LoadFromScenario(sd);
        ShowPage("Index");
    }
}
