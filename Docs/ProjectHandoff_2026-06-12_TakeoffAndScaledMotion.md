# NAS Flight Management System — Project Handoff

Date: 2026-06-12 14:29 CDT
Project: `C:\Users\Admin\Documents\Unity\Flight Management Sim\Current\FlightSim`
Repository: <https://github.com/TRSS-TSU/FlightSim.git>

## Current status

The compressed-world takeoff flow is working again.

Confirmed by user test:

- Aircraft remains parked before Takeoff.
- Takeoff procedure starts when Takeoff is pressed.
- Aircraft rolls down runway visually at an appropriate compressed-world speed.
- Threshold-gated takeoff/climb procedure works.
- Autopilot NAV remained disengaged after takeoff, which is currently intended until route handoff is explicitly designed.

## Key design invariants to preserve

- 1 Unity unit = 1 meter.
- Ground `y = 0`.
- Position/distance values are meters.
- Altitude targets/display are feet.
- IAS targets/display are knots.
- Vertical speed display is fpm.
- UI does not compute nav.
- Nav does not edit UI.
- Preserve Data → Intent → Execution → Display separation.

## Important current behavior

- `_AircraftRoot` starts at `KNPA_EOR25R_START`.
- Initial runway heading remains `250` before takeoff.
- Aircraft is held stationary before Takeoff using `PlaneController.ArmParkedPoseHold(...)`.
- Pressing Takeoff starts `TakeoffProcedureController.BeginTakeoff()`.
- NAV remains inactive during takeoff and after the current procedure because `engageNavAfterProcedure` should remain false until route handoff is deliberately designed.
- Aircraft rolls toward `KNPA_RW25R_THRESH`.
- Climb is gated by threshold arrival; it does not start immediately at brake release.
- Takeoff IAS targets are believable training values:
  - Roll IAS: 60 kt
  - Liftoff IAS: 110 kt
  - Climb IAS: 130 kt
  - Departure IAS: 150 kt

## Fix summary

### Problem

The map/waypoint world is compressed using `FlightPlan.trainingWorldScale`/training movement scale. Earlier direct IAS-to-Rigidbody movement made the aircraft cross the compressed map too quickly. A `worldSpeedScale` concept was introduced so displayed IAS remains realistic while actual execution movement is scaled.

After that, the aircraft stopped rolling/taking off. Diagnostics showed realistic commanded values but near-zero actual Rigidbody ground speed. Dynamic physics/collision response was suppressing the commanded movement, while stale yaw-rate state allowed the heading controller to rotate the aircraft away from the runway.

### Resolution

`PlaneController` now owns deterministic training-sim pose integration:

- Uses an internal `forwardSpeedMps` command state.
- Applies scaled movement directly from commanded speed.
- Sets the Rigidbody kinematic so dynamic physics does not suppress training motion.
- Resets speed, pitch, bank, vertical-speed, and yaw state on parked snap/hold.
- Exposes commanded motion for display/diagnostic consumers:
  - `CurrentGroundSpeedMps`
  - `CurrentVerticalSpeedMps`

Kinematic Rigidbody warnings were then removed by guarding unsupported velocity/angular-velocity writes behind `!rb.isKinematic`.

## Modified files

### `Assets/Scripts/PlaneController.cs`

- Added deterministic commanded speed state.
- Added `CurrentGroundSpeedMps` and `CurrentVerticalSpeedMps` accessors.
- Uses `worldSpeedScale` only for actual movement speed, not for displayed/training IAS targets.
- Uses kinematic pose integration with `MovePosition`/direct pose update.
- Resets controller state during parked pose hold.
- Avoids unsupported `linearVelocity`/`angularVelocity` writes on kinematic Rigidbody.

### `Assets/Scripts/FlightPlan.cs`

- Startup scenario snap/stabilize no longer writes velocity/angular velocity when Rigidbody is kinematic.
- Preserves snap to scenario start and runway heading authority.

### `Assets/Scripts/FlightDataBus.cs`

- Display pipeline now reads commanded ground/vertical speed from `PlaneController` when available.
- IAS remains unscaled for pilot display/training values.
- GS reflects compressed execution/world speed.
- VSI reflects commanded vertical-speed state during kinematic climb.

### `Assets/Scripts/FMS/TakeoffProcedureController.cs`

- Added takeoff gate diagnostics showing distance, closing rate, ground speed, position, yaw, IAS, and movement scale.
- Diagnostic ground speed now uses `PlaneController.CurrentGroundSpeedMps` when available.

## Verification already performed

Command-line build:

```text
dotnet build Assembly-CSharp.csproj -v:minimal
```

Result:

```text
Build succeeded.
0 Error(s)
1 Warning(s)
```

The remaining warning is the known Unity source-generator warning:

```text
CSC : warning CS8785: Generator 'AttributeBasedFieldGenerator' failed to generate source...
```

Unity MCP validation:

- `PlaneController.cs`: 0 errors.
- `FlightPlan.cs`: 0 errors.
- `FlightDataBus.cs`: 0 errors.
- `TakeoffProcedureController.cs`: 0 errors.

User runtime validation:

- Takeoff procedure works.
- Motion looks good.
- NAV/autopilot remained disengaged.

## Known remaining follow-ups

1. Remove or reduce verbose takeoff procedure diagnostic logging once no longer needed.
2. Design the explicit takeoff-to-route/NAV handoff instead of enabling NAV automatically.
3. Review threshold/gate radii for compressed-world feel and reliability.
4. Confirm whether `worldSpeedScale` should be standardized to `FlightPlan.trainingWorldScale`, remain separately tunable, or be scenario-specific.
5. Verify PFD/FMS displayed values during full takeoff/climb:
   - IAS should remain realistic pilot/training IAS.
   - GS should reflect compressed execution/world movement.
   - VSI should reflect climb command.
   - Altitude should remain feet MSL display from meters world altitude.
6. Consider whether deterministic kinematic aircraft motion should become the formal project convention for this training sim.

## Suggested kickoff prompt for next week

```text
Open my Unity NAS Flight Management System project at:
C:\Users\Admin\Documents\Unity\Flight Management Sim\Current\FlightSim

Use Unity MCP where helpful. Act as my Senior Unity Flight Systems + Avionics Engineer.

Start by reading:
Docs/ProjectHandoff_2026-06-12_TakeoffAndScaledMotion.md

Current status:
- Compressed-world takeoff motion is working.
- PlaneController now uses deterministic kinematic movement with worldSpeedScale for actual movement only.
- Display/training IAS should remain realistic and unscaled.
- The takeoff procedure works and Autopilot NAV remained disengaged, which is currently intended.

Next objectives:
1. Re-run a Play Mode takeoff validation and confirm no console warnings/errors.
2. Remove or quiet temporary TakeoffProcedureController diagnostic logs if the behavior remains stable.
3. Inspect and design the explicit takeoff-to-route/NAV handoff. Do not simply auto-enable NAV unless the route handoff behavior is clearly specified.
4. Review waypoint gate radii under compressed-world movement.
5. Verify PFD/FMS displays: IAS unscaled, GS scaled/compressed, VSI correct, altitude in feet.

Preserve project invariants:
- 1 Unity unit = 1 meter
- ground y = 0
- meters for position/distance
- feet for altitude
- knots IAS
- fpm VS
- UI never computes nav
- nav never edits UI
- Data → Intent → Execution → Display separation

Before changing code, report current scene/component bindings and current relevant Inspector values.
```
