# NAS Flight Management System Training Simulator

Unity-based iPad training simulator for practicing a simplified T-1A Flight Management System (FMS) workflow. The project prioritizes teachability, deterministic behavior, realistic cockpit workflow cues, and a maintainable Unity architecture over full avionics-system depth.

> **Training focus:** scenario load → POS INIT → ACT/MOD FPLN/LEGS + EXEC → fly guidance → navigation display awareness → approach setup.

---

## Project Goals

The simulator is being built as an MVP instructional loop for student practice with FMS/CDU procedures and basic navigation guidance.

Primary outcomes:

- Load a scenario-defined route and planning profile.
- Initialize FMS position through the CDU POS INIT workflow.
- Build, review, modify, and execute active or modified flight plans.
- Display route, aircraft, and map context on a Navigation Display (ND).
- Use deterministic flight guidance to support repeatable training.
- Support approach setup, especially RNAV/visual route practice for the Pensacola scenario.

This project is **not** intended to be a full certified avionics simulation. It is a training-oriented representation of selected workflows.

---

## Target Platform

- **Engine:** Unity 6.x LTS family
- **Primary device:** iPad / iPad Mini portrait-oriented training application
- **Input style:** Touch-driven CDU and cockpit UI controls
- **Map source:** Offline raster map tiles stored in `StreamingAssets`

---

## Core Architecture

Keep the system split into four layers:

```text
Data → Intent → Execution → Display
```

### 1. Data

Scenario and database-style inputs.

Examples:

- `ScenarioDefinition`
- scenario title and description
- route name
- route waypoint list
- center latitude / longitude
- base map zoom
- planning values such as ZFW, fuel load, departure base, runway, frequencies

### 2. Intent

FMS, route, navigation, and autopilot target calculation.

Examples:

- FMS model state
- active and modified flight plans
- active waypoint / TO waypoint
- route resolver
- nav computer
- autopilot targets

### 3. Execution

Aircraft motion and physics behavior.

Examples:

- `PlaneController`
- bank, heading, altitude, speed, and vertical speed response
- deterministic guidance execution

### 4. Display

Views and presenters only. Display code should show state, not calculate navigation.

Examples:

- CDU page views
- PFD / ND views
- route line rendering
- map tile rendering
- aircraft symbol rotation
- range labels and zoom buttons

> **Rule of the realm:** UI does not compute navigation. Navigation does not edit UI. Physics does not own route intent.

---

## Unit Invariants

Do not change these without an explicit project decision:

| Concept | Unit |
|---|---:|
| Unity world distance | meters |
| Unity scale | `1 Unity unit = 1 meter` |
| Ground plane | `y = 0` |
| Position / map distance | meters |
| Altitude | feet |
| Airspeed | knots IAS |
| Vertical speed | feet per minute |
| ND range | nautical miles |

---

## Scenario 1: NAS Pensacola Training Route

The current uploaded scenario describes a route departing NAS Pensacola and returning to KNPA.

### Overview

Route concept:

```text
KNPA → TEEZY → TRADR → BFM → VR-1020 Point A → Point E → CEW → PENSI → KNPA
```

Additional points available for student loading:

- KPNS
- PLEBE
- VICKI
- KHRT
- KVPS

Approach focus:

- RNAV 25L

Planning information:

| Field | Value |
|---|---|
| Call sign | Congo 22 |
| ZFW | 11,600 lb |
| Fuel load | 4,300 lb |
| Departure base | KNPA |
| Starting location | EOR 25R |
| Departure procedure | 1 DME past TACAN, turn left heading 220, climb and maintain 3,000 ft |
| Departure frequency | 270.8 / 120.65 |

Coordinate source:

- `Coord Log.xlsx`

Scenario text source:

- `Scenario.txt`

---

## FMS Reference Behavior

The FMS reference material describes a CDU-driven workflow where the CDU is the primary pilot interface. It includes:

- Function keys such as `FPLN`, `LEGS`, `DEP ARR`, `PERF`, `DIR`, `IDX`, `TUN`, `NEXT`, `PREV`, and `EXEC`.
- Line Select Keys (LSKs) for copying, selecting, or transferring displayed data.
- Scratchpad-based data entry.
- ACT/MOD flight plan review before execution.
- Position initialization through POS INIT.
- Flight plan creation and verification through FPLN, LEGS, and map review.
- Direct-To, waypoint insertion/deletion, route discontinuity handling, holds, offsets, and approach setup.

For the MVP, implement workflows selectively. Prefer accurate page flow and student comprehension over exhaustive avionics fidelity.

Reference document:

- `Flight Management System reference.pdf`

---

## Current Uploaded Runtime / ND Scripts

The uploaded scripts currently describe an ND-focused map and range system.

| Script | Purpose |
|---|---|
| `WebMercator.cs` | Computes Web Mercator meters-per-tile at latitude and zoom. |
| `LocalTileGrid.cs` | Loads offline raster tiles from `StreamingAssets`, anchors them in Unity world space, and rebuilds around the aircraft. |
| `TileContent.cs` | Applies loaded tile textures to tile mesh renderers. |
| `NDRangeState.cs` | Central state/event source for ND range in nautical miles. |
| `NDRangeStepper.cs` | UI plus/minus controller for 5, 10, and 20 NM ranges. |
| `NDRangeLock.cs` | Orthographic ND camera sizing helper for range-locked views. |
| `FollowAircraftCamera.cs` | Top-down camera follow behavior using ND range to compute view width. |
| `NDAircraftIconDriver.cs` | Rotates the ND aircraft icon relative to aircraft heading. |
| `NdPresenter.cs` | Draws route line and follows aircraft position for ND display. |
| `NdZoomControllerrt.cs` | Legacy/runtime zoom controller that directly adjusts camera and tile grid zoom. |

---

## Offline Map Tile Layout

Expected tile folder pattern:

```text
Assets/StreamingAssets/tiles_nd_dark_v1/{z}/{x}/{y}.png
```

The ND tile system expects:

- scenario center latitude and longitude from `ScenarioDefinition`
- Web Mercator tile indices for the scenario center
- aircraft movement measured in meters from the scenario origin
- tile paging when the aircraft crosses tile boundaries
- range-driven zoom and radius selection

Current range behavior uses 5, 10, and 20 NM options.

---

## Development Workflow

Recommended daily loop:

1. Define the narrow behavior being implemented.
2. Confirm the data source and owning layer.
3. Update one class or page at a time.
4. Validate in Play Mode with logs and visible UI behavior.
5. Commit a small Git checkpoint.

For non-trivial changes, record:

- intent
- affected files/classes
- source of truth
- expected Play Mode result
- known limitations

---

## Play Mode Validation Checklist

Use these checks after changes:

- Scenario loads the expected route and planning values.
- POS INIT shows and confirms the expected initial position.
- ACT FPLN displays origin/route/destination values from scenario data.
- ACT LEGS displays route waypoints in correct order.
- Edits create MOD state before `EXEC`.
- `EXEC` promotes MOD state to ACT state.
- Aircraft guidance follows the active leg without UI-side navigation math.
- ND range buttons update range state once per selection.
- ND camera width matches selected range.
- Tile grid loads expected z/x/y tiles with low or zero missing tiles.
- Aircraft icon rotates correctly for north-up or heading-up mode.

---

## Git Notes

Before starting work:

```bash
git status
git remote -v
git fetch
git status -sb
```

After a validated change:

```bash
git add <changed-files>
git commit -m "Describe the small validated change"
git push
```

Avoid large mystery commits. Tiny commits are breadcrumbs through the avionics forest. 🧭

---

## Known MVP Boundaries

The current MVP should avoid deep implementation of:

- full certified FMS behavior
- all SID/STAR/airway rules
- full VNAV performance modeling
- sensor blending and real RAIM logic
- all approach categories
- full FCS/autopilot certification-level behavior

Instead, model only what supports the training loop and scenario objectives.

---

## Suggested Next Milestones

1. **Scenario Runtime Integrity**
   - Verify selected scenario populates FMS model state.
   - Exit test: scenario title, route, first waypoint, and planning values appear in debug/UI.

2. **POS INIT Completion**
   - Confirm initial position from scenario or GNSS-style source.
   - Exit test: POS INIT confirmation enables flight plan workflow.

3. **ACT FPLN Route Load**
   - Load route name and first/last waypoint into ACT FPLN.
   - Exit test: ACT FPLN reflects scenario route without UI computing route data.

4. **ACT/MOD LEGS + EXEC**
   - Support waypoint review and simple modification state.
   - Exit test: edits remain MOD until EXEC promotes them to ACT.

5. **ND Map Awareness**
   - Ensure route line, aircraft icon, range, and tiles remain aligned.
   - Exit test: selected 5/10/20 NM ranges show correct camera width and tile coverage.

6. **Approach Setup**
   - Add RNAV 25L scenario approach selection flow.
   - Exit test: approach waypoints/labels appear in FMS and ND route context.

---

## Project Principle

Build the sim like a cockpit instructor would teach it: one reliable procedure at a time, each step visible, repeatable, and easy to explain.
