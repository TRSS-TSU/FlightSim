# ActFplnView — Usage Walkthrough

## Overview

The ACT FPLN page is the student's primary view of the active flight plan. It does not let the
student edit individual legs (that's ACT LEGS). Instead, it lets the student **load a named route**
into the FMS via a two-step confirmation workflow. Once a route is loaded, the page shows a
high-level summary: origin, destination, total distance, via/to waypoints, and the route name.

---

## Display States

| State | When |
|---|---|
| ACT — no route | Page opens, no route has been loaded yet |
| ACT — route loaded | A route is active in the FMS |
| MOD CONFIRM | Student has committed a route name from the scratchpad |
| MOD ARMED | Student pressed YES; waiting for EXEC or CANCEL |

---

## Step-by-Step Usage

### Step 1 — Open the page (ACT — no route)

The student presses the FPLN hard key to navigate to ACT FPLN.

**What the CDU shows:**
```
         ACT FPLN           1/1
ORIGIN     DIST             DEST
  ----                      ----
ROUTE                       ALTN
                             ----
                         ORIG RWY

VIA                            TO
  DIRECT                     ----

<SEC FPLN
```

**Narrative:** The page is empty. L1 shows dashes for origin and destination because no route has
been loaded. L6 right is blank — OFFSET has no meaning without a route. The student's only useful
options are L2 to begin loading a route, or L6-left to jump to the SEC FPLN page.

---

### Step 2 — Seed the scratchpad (L2, empty scratchpad)

The student presses **L2** while the scratchpad is empty.

**What happens:** The FMS reads the scenario route name (e.g. `KNPA-KNPA`) from the
`ScenarioDefinition` and copies it into the scratchpad. The page re-renders (still ACT — no route
state; nothing has been committed yet).

**Narrative:** This is a convenience step. Rather than typing the full route name on the keypad,
the student retrieves the pre-loaded scenario name with a single keypress. If the scenario has no
route name configured, the scratchpad shows `NO ROUTE` instead.

---

### Step 3 — Commit the route name (L2, scratchpad non-empty) → MOD CONFIRM

The student presses **L2** again while the scratchpad contains the route name.

**What happens:** The scratchpad is cleared and its content is stored as `_pendingRouteName`. The
page enters **MOD CONFIRM** state. Title changes to `MOD FPLN`.

**What the CDU shows:**
```
         MOD FPLN           1/1
ORIGIN     DIST             DEST
  KNPA            0         KNPA
ROUTE                       ALTN
  KNPA-KNPA                ----
                         ORIG RWY

VIA                            TO
  DIRECT                     KNPA

---- LOAD NEW ROUTE ----
<YES                          NO>
                    EXEC
```

**Narrative:** The FMS is staging a route change. The current `ActiveRoute` is still displayed in
lines 1–4 (or dashes if no route was previously active). The student must now confirm or reject.
`EXEC` appears on the message line as a prompt — it does not activate yet.

> If the student navigates away from the page at this point (e.g. presses LEGS or IDX),
> `OnDisable()` fires and automatically cancels the MOD. The page returns to ACT state on next
> visit.

---

### Step 4a — Confirm: press YES (L6-left) → MOD ARMED

The student presses **L6-left** (`<YES`).

**What happens:** `_execArmed` is set to `true`. The page re-renders in **MOD ARMED** state.
L6 changes to `<CANCEL MOD`.

**What the CDU shows:**
```
         MOD FPLN           1/1
ORIGIN     DIST             DEST
  KNPA            0         KNPA
ROUTE                       ALTN
  KNPA-KNPA                ----
                         ORIG RWY

VIA                            TO
  DIRECT                     KNPA

<CANCEL MOD            OFFSET ----
                    EXEC
```

**Narrative:** The FMS is armed and waiting for the EXEC key. The student can still bail out by
pressing `<CANCEL MOD`. The OFFSET prompt on L6-right is displayed but inactive (no waypoint
selected to offset from).

---

### Step 4b — Reject: press NO> (L6-right) → back to ACT

The student presses **L6-right** (`NO>`) during MOD CONFIRM.

**What happens:** `CancelMod()` is called. `_modActive`, `_execArmed`, and `_pendingRouteName` are
all cleared. The page returns to ACT state (either no-route or route-loaded depending on whether
a route was already active).

**Narrative:** The student changed their mind. Nothing is written to the FMS. The existing flight
plan (if any) is untouched.

---

### Step 5a — Execute: press EXEC key → route applied

The student presses the **EXEC** hard key while the page is in MOD ARMED state.

**What happens:** `HandleExec()` is called by the router. The FMS:
1. Clears `ActiveRoute`
2. Rebuilds it from `ScenarioDefinition.prefillRouteIdents`
3. Calls `FlightPlan.RebuildRoute()` — scene waypoint objects are respawned
4. Resets `NavAutopilot.activeIndex` to 0
5. Calls `CancelMod()` — returns to ACT state
6. Shows `ROUTE LOADED` on the scratchpad for 1.5 seconds

**What the CDU shows after EXEC:**
```
         ACT FPLN           1/1
ORIGIN     DIST             DEST
  KNPA           87         KNPA
ROUTE                       ALTN
  KNPA-KNPA                ----
                         ORIG RWY

VIA                            TO
  DIRECT                    TEEZY

<SEC FPLN              OFFSET ----
ROUTE LOADED
```

**Narrative:** The route is now live. L1 shows the real total distance. L4-right shows `TEEZY`
(the first leg's TO waypoint, index 1 in `ActiveRoute`). The autopilot will begin tracking the
first leg. The student should proceed to ACT LEGS to review individual waypoints.

---

### Step 5b — Cancel armed MOD: press CANCEL MOD (L6-left) → back to ACT

The student presses **L6-left** (`<CANCEL MOD`) while in MOD ARMED state.

**What happens:** `CancelMod()` is called. Same result as pressing NO> in MOD CONFIRM. Nothing
is written to the FMS.

**Narrative:** Last-chance abort before EXEC. The student decided not to commit after arming.

---

## Key Constraints

- **EXEC only works from MOD ARMED.** If the EXEC key is pressed while `_execArmed` is false,
  the scratchpad shows `NO MOD` for 1.5 seconds and nothing happens.
- **Navigating away cancels MOD.** `OnDisable()` calls `CancelMod()` automatically. The student
  cannot arm EXEC and then switch pages — they must complete the workflow on this page.
- **Route name is cosmetic.** `_pendingRouteName` is displayed on L2 but is not validated against
  any database. Any string committed from the scratchpad is accepted.
- **L2 step A requires a scenario route name.** If `ScenarioDefinition.route` is empty or null,
  pressing L2 with an empty scratchpad shows `NO ROUTE` instead of seeding the scratchpad.
- **OFFSET is always inactive.** The L6-right `OFFSET ----` prompt is displayed in route-loaded
  states as a placeholder but no handler is wired. Pressing R6 does nothing.
