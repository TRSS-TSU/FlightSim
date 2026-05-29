# PFD Altitude Area Redesign Plan

## Context

This document captures the current design discussion for improving the PFD altitude area in the NAS Flight Management System Unity project. The goal is to make the Unity altitude display better match the real/reference PFD while preserving the project architecture.

The current Unity altitude indicator is functional as a fake drum test, but visually it does not yet match the reference PFD. The reference image shows the altitude area as a layered instrument system rather than a single digital readout.

## Architecture Constraints

Maintain strict separation:

```text
PlaneController physics → FlightDataBus telemetry → PFD display scripts
```

The altitude UI must not calculate aircraft state. It should only format and display values already provided by the sim data layer.

Existing source of altitude truth:

```csharp
FlightDataBus.altFtMsl
```

Relevant existing conventions:

- Unity position and distances: meters
- Altitude display: feet MSL
- Vertical speed: feet per minute
- Speed: knots
- UI display scripts read from `FlightDataBus`, not directly from physics or nav logic

## Existing Relevant Script

Current script:

```text
PfdAltitudeReadoutDriver.cs
```

Current object path in scene:

```text
Master_FMS
└── iPad
    └── MainCanvas
        └── PFD
            └── PFD_Panel
                └── UpperView
                    └── ADI_Altitude
                        └── Altitude_Tape
                            └── PfdAltitudeReadoutDriver.cs
```

The current driver already supports multiple digit strips:

```csharp
tenThousandsDigitStrip
thousandsDigitStrip
hundredsDigitStrip
tensDigitStrip
onesDigitStrip
```

It also supports:

```csharp
digitHeight
snapThreshold
useDebugAltitude
debugAltitudeFt
```

## Reference PFD Altitude Area Observations

From the actual PFD image, the altitude area appears to include several distinct components:

1. **Right-side vertical altitude tape**
   - Dark vertical tape background.
   - White tick marks.
   - Major altitude labels such as `700`, `800`, `5000`, `100`, etc.
   - Tape appears fixed in position but values move relative to aircraft altitude.

2. **Current altitude readout box**
   - Black rectangular box centered over the altitude tape.
   - Contains rolling digit-style altitude value.
   - Digits appear like mechanical/drum-style columns.
   - Current altitude reference line passes through the center of this box.

3. **Green current altitude reference line**
   - Horizontal green line through the altitude tape/readout center.
   - Represents current altitude position on the tape.

4. **Right-side vertical speed scale**
   - Separate white scale markings to the right of the altitude tape.
   - Labels include `1`, `2`, and `4` style markings.
   - Likely driven from vertical speed in feet per minute.

5. **Selected altitude / altitude target area**
   - Top-right cyan/green altitude target readout, for example `10000`.
   - Should eventually be driven by selected altitude target, not current altitude.

## Main Design Mismatch

The current Unity altitude area looks more like a compact digital altitude box. The reference PFD shows a wider altitude system composed of:

```text
Altitude tape + centered boxed readout + green reference line + vertical speed scale
```

The fake drum readout should remain, but it should be placed inside a broader altitude tape composition.

## Proposed Unity Hierarchy

Refactor the altitude area under `ADI_Altitude` into clearer sub-objects:

```text
ADI_Altitude
├── Altitude_Tape_Background
├── Altitude_Tape_MovingTicks
├── Altitude_CurrentReferenceLine
├── Altitude_CurrentReadout_Box
│   └── RollingDigitDrums
│       ├── Drum_TenThousands
│       ├── Drum_Thousands
│       ├── Drum_Hundreds
│       ├── Drum_Tens
│       └── Drum_Ones
├── Altitude_SelectedBug
├── Altitude_SelectedReadout
└── VerticalSpeed_RightScale
    ├── VS_Scale_StaticMarks
    └── VS_Pointer
```

## Indicator Inventory

| Indicator | MVP State | Dynamic Source | Notes |
|---|---|---|---|
| Current altitude readout | Dynamic | `FlightDataBus.altFtMsl` | Use fake drum digit strips |
| Altitude tape ticks | Dynamic-lite | `FlightDataBus.altFtMsl` | Moving tick/label group or repeated pooled labels |
| Green current altitude line | Static position | None | Fixed center reference line |
| Selected altitude readout | Static first, dynamic later | `SimTargets.targetAltFtMsl` or target model | Shows target altitude, not current altitude |
| Selected altitude bug | Future dynamic | selected altitude target | Can be static placeholder for MVP |
| Vertical speed scale | Static art first | None | Build visual scale first |
| Vertical speed pointer | Dynamic later | `FlightDataBus.vsiFpm` | Separate driver later |

## Implementation Plan

### Step 1: Freeze the current working state

**Goal:** Preserve the current fake drum test before making layout changes.

Actions:

1. Duplicate the existing `ADI_Altitude` object in the scene.
2. Rename the duplicate:

```text
ADI_Altitude_Prototype_Backup
```

3. Disable the backup object.
4. Keep the active object named:

```text
ADI_Altitude
```

Validation:

- Enter Play Mode.
- Confirm no missing references or UI errors appear.
- Confirm the visible PFD still renders.

### Step 2: Build the altitude tape silhouette

**Goal:** Make the altitude area look structurally closer to the reference before refining behavior.

Actions:

1. Under `ADI_Altitude`, create:

```text
Altitude_Tape_Background
```

2. Use a dark blue/black rectangular `Image`.
3. Position it on the right side of the ADI area.
4. Make it tall enough to span from near the top of the sky area to near the lower ADI boundary.
5. Add a slightly darker inset area for the current readout region.

Suggested layout direction:

```text
Right side of ADI
├── tall dark tape rectangle
├── centered black current altitude box
└── thin green horizontal line through center
```

Validation:

- Compare the Unity PFD against the reference image.
- The altitude area should now visually read as a right-side vertical tape, not a floating box.

### Step 3: Add the fixed current altitude reference line

**Goal:** Add the green horizontal reference line through the center of the readout.

Actions:

1. Create:

```text
Altitude_CurrentReferenceLine
```

2. Add an `Image` component.
3. Use green color.
4. Set height to approximately 2 to 4 pixels.
5. Stretch horizontally across the tape and slightly into the vertical speed side.
6. Anchor it to the vertical center of the altitude readout box.

Validation:

- In Play Mode, the green line remains fixed while altitude digits/tape move around it.

### Step 4: Rebuild the current altitude readout box

**Goal:** Place the fake drum readout inside a black box matching the reference.

Actions:

1. Create:

```text
Altitude_CurrentReadout_Box
```

2. Add a black `Image` background.
3. Place it over the altitude tape at the center reference line.
4. Move the existing digit drum UI under this box:

```text
Altitude_CurrentReadout_Box
└── RollingDigitDrums
```

5. Keep the `PfdAltitudeReadoutDriver.cs` attached either to:

```text
Altitude_CurrentReadout_Box
```

or:

```text
RollingDigitDrums
```

6. Re-wire digit strip references if Unity loses them during hierarchy cleanup.

Validation:

Use debug altitude mode:

```text
useDebugAltitude = true
debugAltitudeFt = 3000
```

Expected:

- Readout displays approximately `03000` or `3000` depending final formatting choice.
- Digits remain clipped inside the black box.

### Step 5: Decide altitude formatting for MVP

**Goal:** Choose one deterministic display format before tuning rolling behavior.

Recommended MVP format:

```text
03000
```

Rationale:

- Easier to validate digit columns.
- Matches aviation-style fixed-width numerical readouts.
- Avoids blank/hidden leading digit edge cases during early testing.

Later visual refinement:

- Hide or dim leading zero for lower altitudes if desired.
- Add smaller last-two-digit presentation if the real PFD style requires it.

Validation values:

| Debug Altitude | Expected Display |
|---:|---|
| 0 | `00000` |
| 20 | `00020` |
| 300 | `00300` |
| 3000 | `03000` |
| 10000 | `10000` |

### Step 6: Refine fake drum behavior

**Goal:** Make digit movement visually believable without introducing snapping bugs.

Current risk:

- The existing `snapThreshold` can cause higher-place digits to snap too early or too late.
- This was previously noticed when the thousands digit appeared to snap from `1` to `0` while only several hundred feet in the air.

Recommended behavior:

- Ones/tens can roll continuously.
- Hundreds and thousands should only advance when lower digits approach rollover.
- For MVP training display, prioritize correctness over mechanical realism.

Practical MVP option:

```text
Use snapped integer digits for all columns except optional ones/tens rolling.
```

This avoids visual confusion during the FMS training loop.

Validation:

Scrub `debugAltitudeFt` slowly through these ranges:

```text
950 → 999 → 1000 → 1050
2950 → 2999 → 3000 → 3050
9950 → 9999 → 10000
```

Expected:

- Thousands does not roll backward unexpectedly.
- Digits change only at appropriate thresholds.

### Step 7: Add altitude tape tick marks and labels

**Goal:** Add the visual moving tape behind the current readout.

Actions:

1. Create:

```text
Altitude_Tape_MovingTicks
```

2. Use child objects for repeated ticks and labels.
3. Start with a simple range around current altitude:

```text
current altitude ± 500 ft
```

4. Use major labels every 100 ft or 500 ft depending visual density.
5. Keep the green reference line fixed.
6. Move the tick group vertically based on altitude modulo the chosen tick interval.

Suggested driver separation:

```text
PfdAltitudeReadoutDriver.cs      → boxed rolling digits only
PfdAltitudeTapeDriver.cs         → moving tape ticks/labels
PfdVerticalSpeedDriver.cs        → VSI scale/pointer
```

Do not overload one script with all altitude-area behavior.

Validation:

- At `3000 ft`, tape labels near the center should represent values around 3000.
- As altitude increases, labels move downward relative to the fixed center line.

### Step 8: Add selected altitude readout and bug

**Goal:** Add the target altitude display visible at the top of the reference altitude area.

Actions:

1. Create:

```text
Altitude_SelectedReadout
```

2. Place it above the altitude tape.
3. Use cyan or green text depending final style match.
4. Initially set static value:

```text
10000
```

5. Later bind to selected altitude target.

Possible dynamic source:

```csharp
SimTargets.targetAltFtMsl
```

Validation:

- The selected altitude readout is visually distinct from current altitude.
- It does not move with the tape.

### Step 9: Add vertical speed scale silhouette

**Goal:** Match the reference right-side scale visually before dynamic VSI behavior.

Actions:

1. Create:

```text
VerticalSpeed_RightScale
```

2. Add static white tick marks and labels:

```text
1
2
4
```

3. Place the scale immediately to the right of the altitude tape.
4. Add placeholder pointer:

```text
VS_Pointer
```

5. Keep the pointer centered for zero vertical speed.

Future dynamic source:

```csharp
FlightDataBus.vsiFpm
```

Validation:

- The right edge of the ADI now resembles the reference: altitude tape plus VSI scale.

## Recommended Script Boundaries

Keep these separate:

### `PfdAltitudeReadoutDriver.cs`

Responsibilities:

- Read `FlightDataBus.altFtMsl`.
- Drive digit strips.
- Support debug altitude.
- No tape tick layout.
- No vertical speed logic.

### `PfdAltitudeTapeDriver.cs`

Responsibilities:

- Read `FlightDataBus.altFtMsl`.
- Move/relabel altitude tape ticks.
- Keep scale centered around current altitude.

### `PfdVerticalSpeedDriver.cs`

Responsibilities:

- Read `FlightDataBus.vsiFpm`.
- Move VSI pointer.
- Clamp pointer to visible range.

### `PfdSelectedAltitudeDriver.cs`

Responsibilities:

- Read selected altitude target.
- Format selected altitude display.
- Move altitude bug if implemented.

## Suggested Next Working Session

Start with only visual layout.

Do not tune rolling digit math first.

Recommended next-session exit test:

```text
The altitude area visually resembles the reference PFD:
- right-side dark altitude tape exists
- black centered current-altitude box exists
- green reference line crosses the box
- current fake drum readout is inside the box
- vertical speed static scale exists on the far right
```

Then, in the following session, focus on behavior:

```text
- readout formatting
- digit rollover correctness
- moving tape labels
- VSI pointer movement
```

## Codex / Unity MCP Prompt For Later

```markdown
You are working in the NAS Flight Management System Unity project. Unity MCP is active.

Task: Redesign the PFD altitude area to better match the actual/reference PFD.

Start in inspection/planning mode. Do not edit code or scene objects until after reporting findings.

Scene/object path:
Master_FMS > iPad > MainCanvas > PFD > PFD_Panel > UpperView > ADI_Altitude

Current script:
PfdAltitudeReadoutDriver.cs

Architecture constraints:
- UI display scripts may read FlightDataBus values only.
- Current altitude source is FlightDataBus.altFtMsl.
- Do not change PlaneController physics, unit conventions, world scale, ND scale, or FMS nav behavior.
- Keep altitude readout, altitude tape, selected altitude, and vertical speed as separate display responsibilities where practical.

Objective:
Refactor the altitude area to visually match the reference PFD:
1. Right-side dark vertical altitude tape.
2. Centered black current altitude readout box.
3. Green horizontal current altitude reference line through the box.
4. Rolling fake drum digits inside the box.
5. Static vertical speed scale on the far right.
6. Selected altitude readout above the tape.

First implementation pass should prioritize visual structure over perfect behavior.

Inspection output required:
1. Existing children under ADI_Altitude.
2. Current PfdAltitudeReadoutDriver field bindings.
3. Missing references or hierarchy risks.
4. Proposed scene hierarchy changes.
5. Play Mode validation steps.

Suggested exit test:
In Play Mode, with debug altitude enabled at 3000 ft, the altitude readout appears inside a centered black box on the right-side altitude tape, with a green reference line crossing through it.
```
