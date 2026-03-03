---
name: cdu-page-view
description: Use when creating or modifying a CDU FMS page view script. Enforces hierarchy rules, SetLine patterns, and HandleLsk conventions for FmsPageView subclasses.
argument-hint: [page-name]
---

# Skill: CDU FMS Page View Script (FmsPageView)

When working on a CDU page (an `FmsPageView` subclass), follow these rules to keep behavior deterministic and consistent across pages.

## 1) Scope and responsibilities

- **View scripts render only.** Do not compute nav, route geometry, or guidance here.
- Get all computed values from the **Intent layer** (FMS model / nav computer).
- UI code may format strings and choose placeholders, but must not invent state.

## 2) Populate() contract

- `Populate()` **must call** `ClearAllLines()` first.
- After clearing, set every visible row deterministically using `SetLine(...)`.
- No side effects in `Populate()` (no mutation of FMS state).

## 3) SetLine usage

- Always use: `SetLine(n, labelL, valueL, labelR, valueR)`
- Keep it **null-safe**:
  - Prefer `value ?? ""` or `valueA ?? valueB` patterns to avoid null rendering.
- Use rows `1–6` (and any supported scratchpad/status line if your base class defines it).

## 4) HandleLsk conventions

Implement `HandleLsk(int side, int row)` with:

- `side`: `0 = Left`, `1 = Right`
- `row`: `1–6` only
- Validate inputs; ignore or log unexpected values (do not throw in production UI).

### Recommended pattern

- Decode the keypress (side/row) → route to a small private handler per row.
- Keep handlers short and deterministic (no heavy computation).

## 5) Multi-page behavior

- Pages that support multiple subpages implement `IMultiPage`:
  - Implement **only** `NextPage()` / `PrevPage()`
  - Do not overload unrelated navigation behavior into LSK handlers.
- `Populate()` must reflect current subpage state.

## 6) Coroutines and timing

- Coroutines are allowed when the page is also a `MonoBehaviour`
  - Example: `PosInitView` can run timed sequences (loading, align simulation, etc.)
- Never block `Populate()` waiting on async work; instead:
  - Update model state asynchronously
  - Then trigger a repopulate / refresh

## 7) Formatting expectations

- Keep labels stable; prefer fixed-width, CDU-style strings.
- Use consistent placeholders (e.g., `"_____"`, `"-----"`, `"*"` markers) per your project standard.
- Ensure fields that can be empty still render as empty strings, not `"null"`.

## 8) Exit test (Play Mode)

For the target page:

1. Open the CDU page.
2. Confirm all 6 rows render after `Populate()` (no stale lines).
3. Press each LSK (L/R, rows 1–6) and confirm:
   - No exceptions
   - Expected handler path triggers
4. If IMultiPage: verify Next/Prev changes page state and `Populate()` updates accordingly.

## Reference

See `CLAUDE.md` for the full hierarchy invariants (Data → Intent → Execution → Display).
