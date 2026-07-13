# NAS FMS MVP edge map

Generated: 2026-07-10
Graph source: `graphify-out/graph.json` (1,138 nodes, 2,101 edges, 92 communities)

```mermaid
flowchart LR
  SD["ScenarioDefinition01\nsource data"] --> SR["ScenarioRuntime"]
  SR --> FS["FlightSession\npersistent phase and metrics"]
  SD --> FR["FmsPageRouter / FmsModel\nMOD and ACT intent"]
  FMS["CDU pages and EXEC"] --> FR
  FR --> FP["FlightPlan\nruntime waypoint transforms"]
  FP --> NAV["NavAutopilot\nLNAV and sequence event"]
  NAV --> FS
  FS -->|"PENSI: replace"| FR
  FS -->|"CUPER: decision"| Modal["TrainingDecisionModal"]
  FS -->|"landing append"| FR
  NAV --> ST["SimTargets\npilot-unit targets"]
  ST --> PC["PlaneController\nphysics execution"]
  FS -->|"FINAL_STOP"| PC
  FR --> ND["NdPresenter / ACT LEGS"]
  FS --> Results["ScenarioResultsPresenter\ndisplay only"]
```

## Ownership and bindings

| Concern | Owner / binding |
| --- | --- |
| Source scenario and waypoint database | `Assets/ScriptableObjects/ScenarioDefinition01.asset`; never mutated at runtime |
| Scenario handoff | `ScenarioRuntime.Current` |
| Preflight, hold, completion, and metrics | persistent `FlightSession` created before scene load |
| CDU page view tracking | `FmsPageRouter.ShowPage` to `FlightSession.MarkPageViewed` |
| Successful EXEC / route review | `FmsPageRouter.OnRouteActivationComplete` to `FlightSession.NotifyRouteExecuted` |
| Runtime route changes | `FmsPageRouter.ReplaceRuntimeRoute` / `AppendRuntimeRoute`; list clones only |
| Valid waypoint completion | `NavAutopilot.WaypointSequenced` to `FlightSession.NotifyWaypointSequenced`; non-loop final waypoint repeats are suppressed |
| Start gate | `FmsPhaseButtonController` subscribes to `FlightSession.StartAvailabilityChanged`; existing Start button retains its `TakeoffEngageButton` reference |
| Takeoff handoff | `TakeoffProcedureController` at 250 ft then `FlightSession.NotifyNavHandoff`; route waypoint events during Takeoff also promote the session to Enroute |
| Aircraft stop | `FlightSession` requests `PlaneController.StopAtGround`; the controller owns target/velocity reset |
| Results display | `ScenarioResults` scene and `ScenarioResultsPresenter`; data is read-only |

## Scene and Inspector changes

- `Master_FMS/WorldRoot/.../TakeoffProcedureController`: `liftoffAltFt = 250`, `departureAltFt = 3000`, `navHandoffMinAltitudeFt = 250`.
- Existing Start button remains wired to `FmsPhaseButtonController.ShowTakeoff` and `TakeoffEngageButton.Press`.
- `ScenarioResults` is enabled after `Menu` and `Master_FMS` in Build Settings.
- The legacy disabled `Land_Button` still has an unbound `TakeoffEngageButton`; it remains hidden and is no longer surfaced by `FmsPhaseButtonController`.

## Validation and open risks

- Unity 6000.3.3f1 C# project builds pass for `Assembly-CSharp.csproj` and `FlightSim.PlayModeTests.csproj`.
- Direct Play Mode smoke through Unity MCP passed route review, PENSI replacement, hold route targeting, hold exit, touchdown, final stop, and asynchronous load to `ScenarioResults`.
- `Assets/Tests/PlayMode/TakeoffNavEngagementPlayModeTests.cs` now covers both the 250 ft handoff target and the PENSI -> hold -> landing -> results state loop; the Unity Test Runner API was blocked by the MCP wrapper as user-interactive, so the test was build-validated and mirrored with the direct Play Mode smoke.
- Full real-time route/geometry, map alignment, and Restart Flight visual UX remain manual acceptance checks; existing map scale and source landing geometry were not changed.
