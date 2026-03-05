# FMS View Page Architecture — Developer Guide

## Overview

This document describes the standard architecture used by all CDU page view scripts in the
NAS Pensacola FMS simulation. Every page script inherits from `FmsPageView` and follows the
same structural contract so that pages are consistent, maintainable, and easy to extend.

`ActFplnView.cs` is the canonical reference implementation. When in doubt, look there first.

---

## How Pages Render

`FmsPageRouter.Update()` calls `_current.Populate()` **every frame**. This means:

- `Populate()` is a **render function only** — it reads state and writes to TMP fields. It must never change state.
- You do **not** need to call `Populate()` at the end of `HandleLsk()`. State changes you make in `HandleLsk()` are picked up automatically by the next frame's pump.
- All pages call `ClearAllLines()` at the top of `Populate()` to guarantee a clean slate before writing.

---

## CDU Layout Slots

Each page has 6 body lines. Each line has 4 TMP text fields:

```
Line N:   [Label_Left]  [Label_Right]
          [Value_Left]  [Value_Right]
```

The base class helpers write to these fields:

```csharp
SetLineLabels(int lineNumber, string labelL, string labelR)
SetLineValues(int lineNumber, string valueL, string valueR)
ClearAllLines()   // clears all 4 fields on all 6 lines
ClearLine(int)    // clears all 4 fields on one line
```

The title bar has two fields accessed via:

```csharp
GetTitle()?.SetText(...)       // top-left page title
GetPageNumber()?.SetText(...)  // top-right e.g. "1/1" or "2/2"
```

The message line (scratchpad area) is shared across all pages:

```csharp
GetMessageLine()?.SetText(...)  // always set explicitly — even to "" to clear
```

---

## Standard Page Template

Every page view script follows this structure:

```csharp
public class XxxView : FmsPageView  // add ", IMultiPage" if multi-page
{
    // ── Formatting helpers ────────────────────────────────────────────────────
    private string FmtTitle()             => "PAGE NAME";
    private string FmtLabel(string label) => string.IsNullOrEmpty(label) ? label
                                           : $"<color=#00FFFF>{label}</color>";
    private string FmtValue(string value) => string.IsNullOrEmpty(value) ? value
                                           : $"<color=#FFFFFF>{value}</color>";

    // ── State fields (private only) ───────────────────────────────────────────
    private bool _someState = false;

    // ── Populate() — render only ──────────────────────────────────────────────
    public override void Populate()
    {
        ClearAllLines();
        GetTitle()?.SetText(FmtTitle());
        GetPageNumber()?.SetText("1/1");
        GetMessageLine()?.SetText("");         // explicit clear — always present

        SetLineLabels(1, FmtLabel("LEFT LABEL"), FmtLabel("RIGHT LABEL"));
        SetLineValues(1, FmtValue(someData),     FmtValue(otherData));
        // ... lines 2–6
    }

    // ── HandleLsk() — all 12 keys addressed ──────────────────────────────────
    public override void HandleLsk(int side, int row)
    {
        if (side == 0)  // Left
        {
            switch (row)
            {
                case 1: /* action */ break;
                case 2: // inactive
                    break;
                // ... case 3–6
            }
        }
        else  // Right
        {
            switch (row)
            {
                case 1: // inactive
                    break;
                // ... case 2–6
            }
        }
        // Do NOT call Populate() here — the router pumps it every frame.
    }
}
```

---

## Formatting Conventions

| Element | Color | Where applied |
|---------|-------|---------------|
| Label text (small, above each line) | Cyan `#00FFFF` | `SetLineLabels()` arguments |
| Value text (large, main line content) | White `#FFFFFF` | `SetLineValues()` arguments |
| Page title | Plain (no tag) | `GetTitle()?.SetText()` |
| Leg idents (ActLegsView) | Per-leg inline tag | Direct TMP rich text inside string |

**Empty strings skip color wrapping.** `FmtLabel("")` and `FmtValue("")` return the
empty string unchanged — no redundant `<color>` tags are emitted.

**Leg ident coloring in ActLegsView is special.** Waypoint idents use inline `<color>` tags
directly (past = cyan, active = green, future = default/white). These are **not** wrapped in
`FmtLabel()`, because doing so would force cyan on future legs that should be white.

---

## Scratchpad Interactions

The scratchpad is shared across all pages and accessed via the `Scratchpad` property.

| Method | Use |
|--------|-----|
| `Scratchpad.CurrentText` | Read what the student has typed |
| `Scratchpad.Append(string)` | Write a value into the scratchpad (uppercase forced) |
| `Scratchpad.ReadAndClear()` | Consume the scratchpad value and clear it |
| `Scratchpad.ShowMessage(string, float?)` | Flash a system message for N seconds (default 1.5s) |

**Common two-step LSK pattern** (e.g. L2 on ActFplnView):

```
Press L2, scratchpad empty   → Append current value to scratchpad (seed)
Press L2, scratchpad non-empty → ReadAndClear() → commit to model / change state
```

**Validation pattern** (e.g. waypoint entry on ActLegsView / DirView):

```csharp
string sp = Scratchpad.CurrentText;
if (sp.Length == 0) { Scratchpad.Append(existingValue); return; }

var wp = scenario.waypoints.Find(w =>
    string.Equals(w.ident, sp, StringComparison.OrdinalIgnoreCase));
if (wp == null) { Scratchpad.ShowMessage("NOT IN DATABASE"); return; }

// commit ...
Scratchpad.ReadAndClear();
```

---

## Multi-Page Views (IMultiPage)

Pages with more than one sub-page implement the `IMultiPage` interface:

```csharp
public class XxxView : FmsPageView, IMultiPage
{
    private int _page = 1;

    public void NextPage() => _page = _page == 1 ? 2 : 1;
    public void PrevPage() => _page = _page == 2 ? 1 : 2;
}
```

The router casts via `(_current as IMultiPage)?.NextPage()` — adding or removing
`IMultiPage` from a page class requires no changes to the router.

---

## Navigation

Navigate between pages by calling:

```csharp
Router.ShowPage("PageId");
```

| Page ID | Script |
|---------|--------|
| `Index` | IndexView |
| `ActFpln` | ActFplnView |
| `ActLegs` | ActLegsView |
| `PosInit` | PosInitView |
| `PerfInit` | PerfInitView |
| `Status` | StatusView |
| `Frequency` | FrequencyView |
| `Prog` | ProgView |
| `DepArr` | DepArrView |
| `Dir` | DirView |
| `SecFpln` | StubPageView |
| `GnssCtl` | StubPageView |
| `FmsCtl` | StubPageView |
| `Fix` | StubPageView |
| `Hold` | StubPageView |
| `ModLegs` | StubPageView |

`ShowPage()` calls `SetActive(false)` on the current page (fires `OnDisable`) and
`SetActive(true)` on the next page (fires `OnEnable`). Use `OnDisable()` to reset state
when a student navigates away (see `ActFplnView.OnDisable()` → `CancelMod()`).

---

## Page-by-Page Quick Reference

### IndexView
Main menu. All 12 LSKs are active navigation buttons. L1–L6 left navigate to sub-pages; R1–R6 right navigate to a second column of pages (R1 = NOT AVAILABLE stub).

### StatusView
Display only. Shows nav database ident, date ranges, UTC clock, program ID. L6 → Index. R6 → PosInit.

### PosInitView (2-page, IMultiPage)
Page 1: FMS POS display, Airport ident, PILOT/REF WPT. R4 = SET POS TO GNSS (3-second load animation). L6 → Index, R6 → ActFpln.
Page 2: NAVAID/VOR/DME status display. L6 → Index.

### ActFplnView (state machine)
Four display states — see [`ActFplnView_Usage.md`](ActFplnView_Usage.md) for full walkthrough. L2 seeds/commits route name. EXEC key fires `HandleExec()` which calls `ApplyRoute()` to rebuild the active route.

### ActLegsView (IMultiPage)
3 waypoints per page. L1/L3/L5 are the active waypoint LSKs:
- Empty SP → copy ident to scratchpad
- SP = valid ident → insert before slot
- SP = `DELETE` → remove waypoint (route must have > 1 waypoint)

L6 → Index. All right-side LSKs are inactive.
Route changes commit immediately via `CommitRoute()` → `FlightPlan.RebuildRoute()`.

### PerfInitView (2-page, IMultiPage)
Page 1: L1 = ZFW entry, L2 = Fuel weight entry. Sets `Router.HasPendingPerf = true` when weights are staged. Student presses EXEC to confirm (router handles the EXEC key centrally). L6/R6 → Index.
Page 2: V-speeds placeholder (NOT IMPL).

### FrequencyView
L1–L5 left and R4 right copy the corresponding frequency into the scratchpad (only if scratchpad is empty). L6/R6 → Index.

### ProgView (2-page, IMultiPage)
Page 1: FROM/TO/DEST idents, distance to active WP, ETE, XTK.
Page 2: IAS, ALT, HDG, VSI live readout.
Display only — all LSKs inactive except L6/R6 → Index.

### DepArrView
Display only. DEP/ARR airports, procedure, heading, altitude (Scenario 01 hardcoded). L6 → Index. All other LSKs inactive.

### DirView (stub — route modification pending)
L1 = Direct-To ident entry. Empty SP → seed with current TO waypoint. Non-empty SP → validate ident against scenario waypoints → shows `NOT IMPLEMENTED` (route modification logic deferred — see Next Steps below).

### StubPageView
Generic NOT IMPLEMENTED placeholder. `pageTitle` field set in Inspector per GO. L6 → Index. All other LSKs inactive. Multiple scene GameObjects share this script with different `pageTitle` values.

---

## EXEC Key Workflow

The EXEC hard key is handled centrally in `FmsPageRouter.HandleFunctionKey()`:

1. If current page is `ActFplnView` → calls `actFpln.HandleExec()` directly
2. Else if `Router.HasPendingPerf` → clears flag, shows `PERF ACCEPTED`
3. Else → shows `EXEC COMPLETE`

To add EXEC support to a new page, either:
- Add a `HandleExec()` public method and handle it in the router's switch (like ActFplnView), or
- Use the `HasPendingPerf` flag pattern (like PerfInitView)

---

## Unity Scene Wiring

The following Inspector connections must be made manually after code changes.
**Do not modify the Unity scene without prior approval.**

| What | Where | Status |
|------|-------|--------|
| Dir GO script | Swap StubPageView → DirView | **Pending approval** |
| FmsPageRouter.simTargets | Wire to SimTargets on `_AircraftRoot` | Pending |
| FmsPageRouter.pagePerf | Wire to PerfInit GameObject | Pending |
| ScenarioDefinition01.asset | Set `zfwLbs = 11600`, `fuelWeightLbs = 4300` | Pending |

---

## Next Steps

The following features are ready to be implemented as the next development phase:

### 1. DirView — Direct-To Route Modification
`DirView.cs` validates idents and displays correctly but does not yet modify the active route.
Choose one of three strategies and implement in `DirView.HandleDirectTo()`:

| Strategy | Behaviour |
|----------|-----------|
| **SIMPLE** | Set `NavAutopilot.activeIndex` to the waypoint's index in the current route. No route rebuild. Use when the WP is already in the route. |
| **STRICT** | `ActiveRoute.RemoveRange(0, index)` — truncate everything before the typed WP. Then `CommitRoute()` and set `nav.activeIndex = 0`. |
| **FULL** | `ActiveRoute.Insert(0, wpDef)` — prepend the typed WP at the front. Then `CommitRoute()` and set `nav.activeIndex = 0`. Keeps existing route after it. |

### 2. MOD LEGS Page
`ModLegs` currently shows StubPageView. A full `ModLegsView.cs` would let the student
stage route edits (insert/delete waypoints) before committing with EXEC, mirroring the
ActLegs pattern but with a pending-commit buffer.

### 3. V-Speeds (PerfInit Page 2)
`PerfInitView.PopulatePage2()` shows `NOT IMPL`. Implement V-speed entry (V1, VR, V2)
as scratchpad fields with validation against gross weight ranges from the Scenario data.

### 4. Speed and Altitude Target Entry
`SimTargets` (accessible via `Router.GetSimTargets()`) holds autopilot setpoints. Pages
could write to `simTargets.targetIasKt` and `simTargets.targetAltFtMsl` from scratchpad
entries — for example, a dedicated SPD/ALT page, or inline entries on ProgView.

### 5. Color Theming via Inspector
The `<color=#00FFFF>` / `<color=#FFFFFF>` tags in `FmtLabel` / `FmtValue` produce consistent
output but override any Inspector-set color on TMP components. If a designer wants to adjust
label or value color globally, update the hex values in the formatting helpers across all
page scripts (or refactor to a shared static helper class).

### 6. SecFpln, GnssCtl, FmsCtl, Fix, Hold
These pages are still on StubPageView. Each can be promoted to a dedicated view script
following the standard template above. Check the spec documents in `context/` for the
intended layout and LSK behavior before implementing.

---

## File Locations

| Category | Path |
|----------|------|
| Page view scripts | `Assets/Scripts/FMS/Pages/` |
| Base class | `Assets/Scripts/FMS/FmsPageView.cs` |
| Router + IMultiPage | `Assets/Scripts/FMS/FmsPageRouter.cs` |
| Scratchpad | `Assets/Scripts/FMS/FmsScratchpad.cs` |
| Data model | `Assets/Scripts/FMS/FmsModel.cs` |
| Scene | `Assets/Scenes/Master_FMS.unity` |
| Spec documents | `context/` |
| This document | `docs/FMS_ViewPage_Architecture.md` |
