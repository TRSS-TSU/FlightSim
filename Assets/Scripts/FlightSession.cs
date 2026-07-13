using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum FlightPhase
{
    Preflight,
    ReadyForTakeoff,
    Takeoff,
    Enroute,
    EnteringHold,
    Holding,
    HoldExitArmed,
    Approach,
    Landing,
    Stopped,
    Results,
}

public enum ScenarioResultStatus
{
    Completed,
    Restarted,
    EndedEarly,
}

[Serializable]
public sealed class FlightPerformanceRecord
{
    public string scenarioId;
    public string scenarioName;
    public float elapsedSeconds;
    public int waypointsCompleted;
    public int routeModifications;
    public int skippedRequiredChecks;
    public int holdCircuitsCompleted;
    public bool posInitViewed;
    public bool fuelViewed;
    public bool weightViewed;
    public bool routeExecuted;
    public bool routeVerified;
    public bool touchdownReached;
    public bool finalStopReached;
    public ScenarioResultStatus status;
    public float posInitViewedAt = -1f;
    public float fuelViewedAt = -1f;
    public float weightViewedAt = -1f;
    public float routeExecutedAt = -1f;
    public float routeVerifiedAt = -1f;
}

/// <summary>Persistent MVP training state; route data remains owned by FlightPlan/FmsPageRouter.</summary>
[DefaultExecutionOrder(-900)]
public sealed class FlightSession : MonoBehaviour
{
    public static FlightSession Instance { get; private set; }
    public FlightPhase Phase { get; private set; } = FlightPhase.Preflight;
    public FlightPerformanceRecord Record { get; private set; } = new();
    public event Action<FlightPhase> PhaseChanged;
    public event Action<bool> StartAvailabilityChanged;
    public event Action RouteReviewRequired;
    public event Action HoldDecisionRequired;
    public event Action FlightCompleted;

    private FmsPageRouter router;
    private NavAutopilot nav;
    private TrainingDecisionModal modal;
    private bool exitHoldArmed;
    private bool landingAppended;
    private bool trainingTimerRunning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance)
            return;

        var go = new GameObject(nameof(FlightSession));
        DontDestroyOnLoad(go);
        go.AddComponent<FlightSession>();
    }

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ScenarioRuntime.OnChanged += StartForScenario;
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (ScenarioRuntime.Current)
            StartForScenario(ScenarioRuntime.Current);
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        ScenarioRuntime.OnChanged -= StartForScenario;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        DetachNav();
        Instance = null;
    }

    private void Update()
    {
        if (trainingTimerRunning && Phase < FlightPhase.Stopped && Time.timeScale > 0f)
            Record.elapsedSeconds += Time.deltaTime;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DetachNav();
        trainingTimerRunning = scene.name == "Master_FMS";
        router = FindFirstObjectByType<FmsPageRouter>();
        nav = FindFirstObjectByType<NavAutopilot>();
        if (nav)
            nav.WaypointSequenced += OnWaypointSequenced;
    }

    private void DetachNav()
    {
        if (nav)
            nav.WaypointSequenced -= OnWaypointSequenced;
        nav = null;
        router = null;
    }

    private void StartForScenario(ScenarioDefinition scenario)
    {
        if (!scenario)
            return;

        Record = new FlightPerformanceRecord
        {
            scenarioId = scenario.name,
            scenarioName = scenario.scenarioTitle,
        };
        exitHoldArmed = false;
        landingAppended = false;
        trainingTimerRunning = false;
        SetPhase(FlightPhase.Preflight);
        StartAvailabilityChanged?.Invoke(false);
    }

    public void MarkPageViewed(string pageId)
    {
        if (Phase >= FlightPhase.Takeoff)
            return;

        if (pageId == "PosInit")
        {
            Record.posInitViewed = true;
            Record.posInitViewedAt = FirstTimestamp(Record.posInitViewedAt);
        }
        else if (pageId == "PerfInit")
        {
            Record.fuelViewed = true;
            Record.weightViewed = true;
            Record.fuelViewedAt = FirstTimestamp(Record.fuelViewedAt);
            Record.weightViewedAt = FirstTimestamp(Record.weightViewedAt);
        }
    }

    public void NotifyRouteExecuted(bool modification)
    {
        if (Phase >= FlightPhase.Takeoff)
            return;

        Record.routeExecuted = true;
        Record.routeExecutedAt = FirstTimestamp(Record.routeExecutedAt);
        if (modification)
            Record.routeModifications++;

        Record.routeVerified = false;
        SetPhase(FlightPhase.Preflight);
        StartAvailabilityChanged?.Invoke(false);
        RouteReviewRequired?.Invoke();
        ShowModal().Show(
            "ROUTE REVIEW",
            "Before takeoff, confirm all five required checks:\nPOS INIT | FUEL | WEIGHT | EXEC | ACTIVE ROUTE REVIEW",
            "Checks Complete",
            ConfirmRouteReview,
            "Cancel",
            () => router?.ShowPage("ActLegs")
        );
    }

    /// <summary>Removes the preflight takeoff authorization when a MOD route is staged.</summary>
    public void InvalidateRouteReview()
    {
        if (Phase >= FlightPhase.Takeoff || !Record.routeVerified)
            return;

        Record.routeVerified = false;
        SetPhase(FlightPhase.Preflight);
        StartAvailabilityChanged?.Invoke(false);
    }

    public void ConfirmRouteReview()
    {
        if (Phase >= FlightPhase.Takeoff)
            return;

        Record.routeVerified = true;
        Record.routeVerifiedAt = FirstTimestamp(Record.routeVerifiedAt);
        SetPhase(FlightPhase.ReadyForTakeoff);
        StartAvailabilityChanged?.Invoke(true);
    }

    public bool TryBeginTakeoff()
    {
        if (!Record.routeVerified || Phase != FlightPhase.ReadyForTakeoff)
            return false;

        Record.skippedRequiredChecks = MissingRequiredChecks();
        SetPhase(FlightPhase.Takeoff);
        StartAvailabilityChanged?.Invoke(false);
        return true;
    }

    public void NotifyNavHandoff()
    {
        if (Phase == FlightPhase.Takeoff)
            SetPhase(FlightPhase.Enroute);
    }

    private int MissingRequiredChecks()
    {
        int missing = 0;
        if (!Record.posInitViewed) missing++;
        if (!Record.fuelViewed) missing++;
        if (!Record.weightViewed) missing++;
        if (!Record.routeExecuted) missing++;
        if (!Record.routeVerified) missing++;
        return missing;
    }

    private float FirstTimestamp(float timestamp)
    {
        return timestamp < 0f ? Record.elapsedSeconds : timestamp;
    }

    private void OnWaypointSequenced(string ident)
    {
        NotifyWaypointSequenced(ident);
    }

    /// <summary>Receives one valid leg-completion signal from NavAutopilot.</summary>
    public void NotifyWaypointSequenced(string ident)
    {
        if (string.IsNullOrEmpty(ident))
            return;

        if (Phase == FlightPhase.Takeoff)
            SetPhase(FlightPhase.Enroute);

        if (Phase < FlightPhase.Enroute || Phase >= FlightPhase.Stopped)
            return;

        Record.waypointsCompleted++;
        if (string.Equals(ident, "PENSI", StringComparison.OrdinalIgnoreCase) && Phase == FlightPhase.Enroute)
            EnterHold();
        else if (string.Equals(ident, "CUPER", StringComparison.OrdinalIgnoreCase) && Phase >= FlightPhase.EnteringHold && Phase <= FlightPhase.Holding)
            OfferHoldDecision();
        else if (string.Equals(ident, "APUCE", StringComparison.OrdinalIgnoreCase) && Phase == FlightPhase.HoldExitArmed)
            ApplyLandingTargets("ALCOME");
        else if (string.Equals(ident, "ALCOME", StringComparison.OrdinalIgnoreCase) && Phase >= FlightPhase.Holding && Phase <= FlightPhase.HoldExitArmed)
        {
            CompleteHoldCircuit();
            if (Phase == FlightPhase.Approach)
                ApplyLandingTargets("KNPA_RW25L_FINAL");
        }
        else if (string.Equals(ident, "KNPA_RW25L_FINAL", StringComparison.OrdinalIgnoreCase) && Phase == FlightPhase.Approach)
            ApplyLandingTargets("KNPA_RW25L_TOUCHDOWN");
        else if (string.Equals(ident, "KNPA_RW25L_TOUCHDOWN", StringComparison.OrdinalIgnoreCase))
        {
            Record.touchdownReached = true;
            SetPhase(FlightPhase.Landing);
            StopAtFinalWaypoint();
        }
        else if (string.Equals(ident, "KNPA_FINAL_STOP", StringComparison.OrdinalIgnoreCase))
            StopAtFinalWaypoint();
    }

    private void EnterHold()
    {
        var hold = ResolveWaypoints("PENSI", "CUPER", "POOVE", "APUCE", "ALCOME");
        if (hold.Count != 5 || !ResolveRouter())
            return;

        router.ReplaceRuntimeRoute(hold, 1);
        nav.loop = true;
        SetPhase(FlightPhase.EnteringHold);
    }

    private void OfferHoldDecision()
    {
        if (Phase == FlightPhase.EnteringHold)
            SetPhase(FlightPhase.Holding);
        if (Phase != FlightPhase.Holding)
            return;

        HoldDecisionRequired?.Invoke();
        ShowModal().Show(
            "HOLD DECISION",
            "Continue holding or begin the landing sequence after this circuit?",
            "Continue Holding",
            ContinueHolding,
            "Begin Landing",
            BeginLanding
        );
    }

    private void ContinueHolding()
    {
        RemovePensiFromHold();
    }

    private bool RemovePensiFromHold()
    {
        var hold = ResolveWaypoints("CUPER", "POOVE", "APUCE", "ALCOME");
        return hold.Count == 4 && ResolveRouter() && router.ReplaceRuntimeRoute(hold, 1);
    }

    private void CompleteHoldCircuit()
    {
        Record.holdCircuitsCompleted++;
        if (exitHoldArmed)
            SetPhase(FlightPhase.Approach);
    }

    public void BeginLanding()
    {
        if (landingAppended || !RemovePensiFromHold())
            return;

        var landing = ResolveWaypoints("KNPA_RW25L_FINAL", "KNPA_RW25L_TOUCHDOWN", "KNPA_FINAL_STOP");
        if (landing.Count != 3 || !router.AppendRuntimeRoute(landing))
            return;

        landingAppended = true;
        exitHoldArmed = true;
        nav.loop = false;
        SetPhase(FlightPhase.HoldExitArmed);
    }

    private void ApplyLandingTargets(string ident)
    {
        var waypoint = ResolveWaypoints(ident);
        if (waypoint.Count != 1 || !nav || !nav.targets)
            return;

        nav.targets.targetAltFtMsl = Mathf.Max(0f, waypoint[0].targetAltFtMsl);
        if (waypoint[0].targetIasKt > 0f)
            nav.targets.targetIasKt = waypoint[0].targetIasKt;
    }

    private void StopAtFinalWaypoint()
    {
        if (Record.finalStopReached)
            return;

        Record.finalStopReached = true;
        Record.status = ScenarioResultStatus.Completed;
        if (nav)
            nav.SetNavEngaged(false);

        var aircraft = nav ? nav.aircraft : null;
        aircraft?.GetComponent<PlaneController>()?.StopAtGround();

        SetPhase(FlightPhase.Stopped);
        FlightCompleted?.Invoke();
        ShowModal().Show("FLIGHT COMPLETE", "Flight complete. Take off again or view results?", "Takeoff Again", RestartFlight, "End", EndFlight);
    }

    private void RestartFlight()
    {
        Record.status = ScenarioResultStatus.Restarted;
        StartForScenario(ScenarioRuntime.Current);
        SceneManager.LoadScene("Master_FMS");
    }

    private void EndFlight()
    {
        if (Record.status != ScenarioResultStatus.Completed)
            Record.status = ScenarioResultStatus.EndedEarly;
        SetPhase(FlightPhase.Results);
        SceneManager.LoadScene("ScenarioResults");
    }

    public void ReturnToMenu()
    {
        StartAvailabilityChanged?.Invoke(false);
        trainingTimerRunning = false;
        Record = new FlightPerformanceRecord();
        SetPhase(FlightPhase.Preflight);
        SceneManager.LoadScene("Menu");
    }

    public void Quit()
    {
        Application.Quit();
    }

    private bool ResolveRouter()
    {
        if (!router)
            router = FindFirstObjectByType<FmsPageRouter>();
        if (!nav)
            nav = FindFirstObjectByType<NavAutopilot>();
        return router && nav;
    }

    private List<ScenarioDefinition.WaypointDef> ResolveWaypoints(params string[] idents)
    {
        var result = new List<ScenarioDefinition.WaypointDef>();
        var scenario = ScenarioRuntime.Current;
        if (!scenario || scenario.waypoints == null)
            return result;

        foreach (var ident in idents)
        {
            var waypoint = scenario.waypoints.Find(w => w != null && string.Equals(w.ident, ident, StringComparison.OrdinalIgnoreCase));
            if (waypoint != null)
                result.Add(waypoint);
        }
        return result;
    }

    private TrainingDecisionModal ShowModal()
    {
        if (modal)
            return modal;

        modal = gameObject.GetComponent<TrainingDecisionModal>();
        if (!modal)
            modal = gameObject.AddComponent<TrainingDecisionModal>();
        return modal;
    }

    private void SetPhase(FlightPhase phase)
    {
        if (Phase == phase)
            return;
        Phase = phase;
        PhaseChanged?.Invoke(Phase);
    }
}

/// <summary>One reusable modal for the three MVP decisions.</summary>
public sealed class TrainingDecisionModal : MonoBehaviour
{
    private GameObject root;
    private Text title;
    private Text body;
    private Button primary;
    private Button secondary;

    public void Show(string heading, string message, string primaryLabel, Action primaryAction, string secondaryLabel, Action secondaryAction)
    {
        EnsureUi();
        title.text = heading;
        body.text = message;
        Configure(primary, primaryLabel, primaryAction);
        Configure(secondary, secondaryLabel, secondaryAction);
        root.SetActive(true);
    }

    private void EnsureUi()
    {
        if (root)
            return;

        root = new GameObject("TrainingDecisionModal", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(root);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var panel = MakeImage(root.transform, new Color(0.02f, 0.04f, 0.08f, 0.96f), new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.7f));
        title = MakeText(panel.transform, new Vector2(0.06f, 0.65f), new Vector2(0.94f, 0.92f), 34, TextAnchor.MiddleCenter);
        body = MakeText(panel.transform, new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.64f), 24, TextAnchor.MiddleCenter);
        primary = MakeButton(panel.transform, new Vector2(0.08f, 0.08f), new Vector2(0.45f, 0.24f));
        secondary = MakeButton(panel.transform, new Vector2(0.55f, 0.08f), new Vector2(0.92f, 0.24f));
        root.SetActive(false);
    }

    private void Configure(Button button, string label, Action action)
    {
        button.onClick.RemoveAllListeners();
        button.GetComponentInChildren<Text>().text = label;
        button.onClick.AddListener(() =>
        {
            root.SetActive(false);
            action?.Invoke();
        });
    }

    private static GameObject MakeImage(Transform parent, Color color, Vector2 min, Vector2 max)
    {
        var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static Text MakeText(Transform parent, Vector2 min, Vector2 max, int size, TextAnchor alignment)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.white;
        text.fontSize = size;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button MakeButton(Transform parent, Vector2 min, Vector2 max)
    {
        var go = MakeImage(parent, new Color(0.1f, 0.45f, 0.7f, 1f), min, max);
        go.name = "DecisionButton";
        var button = go.AddComponent<Button>();
        var label = MakeText(go.transform, Vector2.zero, Vector2.one, 20, TextAnchor.MiddleCenter);
        label.name = "Label";
        return button;
    }
}
