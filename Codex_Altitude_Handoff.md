You are taking over development on the NAS Flight Management System Unity project.

We are building a “Fake Drum” altitude indicator for the PFD. Unity MCP is active, so start in planning mode by inspecting the scene and script bindings before editing anything.

## Current Scene/Object Location

Scene: `Master_FMS`

Target object path:

`iPad > MainCanvas > PFD > PFD_Panel > UpperView > ADI_Altitude > Altitude_Tape`

Script attached:

`PfdAltitudeReadoutDriver.cs`

A first test drum has already been created and wired. It uses a masked viewport with a vertical digit strip, intended to simulate a cylindrical/spinning altitude drum without using real 3D cylinders.

## Architecture Rules

Preserve strict separation:

`PlaneController physics → FlightDataBus telemetry → PFD display script`

The PFD UI must not calculate aircraft state. It may only read display-safe values from `FlightDataBus`.

Use:

`FlightDataBus.altFtMsl`

as the altitude source.

Do not change world scale or unit conventions:

- Unity position/distances: meters
- Altitude display: feet
- Vertical speed: feet per minute
- Speed: knots

## Goal

Create a realistic MVP fake drum altitude readout.

The display should show altitude in feet, similar to an aircraft rolling digit indicator.

Start simple and deterministic:

1. Verify the existing `PfdAltitudeReadoutDriver.cs` script and current scene hierarchy.
2. Confirm that `Altitude_Tape` has a valid `FlightDataBus` reference or can resolve one safely.
3. Confirm the existing `thousandsDigitStrip` binding.
4. Implement or refine one working rolling drum first.
5. Expand to multiple digit drums only after the first one validates.

## Desired MVP Behavior

For debug mode:

- `debugAltitudeFt = 0` shows thousands digit `0`
- `debugAltitudeFt = 1000` shows `1`
- `debugAltitudeFt = 3000` shows `3`
- `debugAltitudeFt = 9000` shows `9`

For live mode:

- When `useDebugAltitude = false`, the display reads `FlightDataBus.altFtMsl`.

The digit strip should move vertically based on digit value and `digitHeight`.

## Planning Output First

Before making code changes, provide:

1. What objects/components you found under `Altitude_Tape`.
2. What fields are currently wired in `PfdAltitudeReadoutDriver.cs`.
3. Any missing references or hierarchy issues.
4. A small implementation plan with a Play Mode validation test.

Do not edit scene objects or code until after this inspection plan is written.

## Suggested First Exit Test

In Play Mode, set:

- `useDebugAltitude = true`
- `debugAltitudeFt = 3000`
- `digitHeight = 60`

Expected result:

The visible thousands drum shows `3`.

If the drum moves in the wrong direction, adjust the strip offset sign, not the hierarchy.
