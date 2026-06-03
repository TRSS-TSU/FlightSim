# AGENTS.md - Flight Management System Development Log

## Purpose

This document serves as the architectural and refactoring log for the Flight Management System (FMS) Unity project. It tracks structural changes, design decisions, and implementation status for the CDU (Control Display Unit) and related systems.

---

## Refactor Log

### CDU Hierarchy Refactoring - COMPLETED (2026-02-06)

**Status:** ✅ COMPLETED

**Objective:** Refactor CDU display hierarchy from single-page reusable layout to multi-page structure with 17 page GameObjects following canonical template.

**Target Structure:**
- Each CDU page (Index, ActLegs, PosInit_1, etc.) has identical structure:
  - Title_Line (Title, Page_Number, Top_Border)
  - Body (Body_Line_1 … Body_Line_6, each with Label_Left, Value_Left, Label_Right, Value_Right)
- Shared_IO contains persistent elements across pages:
  - Scratchpad_Input, Scratchpad_Bracket_Left, Scratchpad_Bracket_Right, Message_Line

**Pages to Create:**
- Index (active, populated with original content)
- PosInit_1, PosInit_2, ActLegs, ModLegs, Exec, Status, Prog, Frequency, Fix, DepArr, Fpln, SecFpln, Hold, VordmeCtl, GnssCtl, FmsCtl (structure created, awaiting population)

**Constraints:**
- ✅ No RectTransform values changed (anchors, positions, sizes, pivots)
- ✅ No TMP settings changed (font asset, font size, alignment, color)
- ✅ No scripts added, modified, or removed
- ✅ Visual output pixel-identical to pre-refactoring state

**Progress:**
- [x] Phase 0: Pre-Flight Setup (AGENTS.md and docs) - COMPLETED
- [x] Phase 1: Create Shared_IO container - COMPLETED
- [x] Phase 2A: Create Index/Title_Line structure - COMPLETED
- [x] Phase 2B: Create Index/Body with 6 Body_Lines - COMPLETED
- [x] Phase 3: Create 16 empty page structures - COMPLETED
- [x] Phase 4: Final validation and testing - COMPLETED

---

## Architectural Rules - CDU UI

### Hierarchy Invariants

**Mandatory for all CDU pages:**
1. Each page GameObject must have exactly 2 children: Title_Line and Body
2. Title_Line must have exactly 3 TMP children: Title, Page_Number, Top_Border
3. Body must have exactly 6 children: Body_Line_1 through Body_Line_6
4. Each Body_Line must have exactly 4 TMP children: Label_Left, Value_Left, Label_Right, Value_Right

**Shared Elements:**
- Scratchpad and Message_Line persist across page switches (under Shared_IO)
- Only one page GameObject should be active at a time (except during transitions)

### Naming Conventions

**Page GameObjects:** PascalCase with no spaces (Index, PosInit_1, ActLegs)
**Container GameObjects:** Snake_Case (Title_Line, Body_Line_1)
**TMP GameObjects:** PascalCase with underscores (Label_Left, Value_Right)

### Future Development Guidelines

**When adding a new CDU page:**
1. Duplicate an existing page GameObject (e.g., Index)
2. Rename to new page name
3. Clear all TMP text fields
4. Populate Title_Line/Title with page name
5. Set SetActive(false) initially

**When populating a page with data:**
1. Create a PageData class (e.g., PosInitPageData)
2. Implement FmsView binding logic to populate TMP fields
3. Never modify hierarchy structure—only TMP text content

---

## Next Steps

### Immediate Follow-Up (Not Part of This Refactoring)
- [ ] Create FmsView/PageRouter script to manage page activation
- [ ] Implement page switching logic (SetActive control)
- [ ] Create PageData models for each page type
- [ ] Implement TMP text binding from PageData to UI elements
- [ ] Add LSK (Line Select Key) button system for user interaction

### Milestone B: CDU Core Skeleton
- [ ] Scratchpad input capture (keypad input handler)
- [ ] Message line display system
- [ ] FmsModel architecture

### Milestone C: ACT LEGS MVP
- [ ] Populate ActLegs page with route leg data
- [ ] Direct-to navigation support

### Milestone D: POS INIT
- [ ] Populate PosInit_1 and PosInit_2 pages
- [ ] GPS position initialization

---

## Changelog

### 2026-02-06 - CDU Hierarchy Refactoring Started
- **Updated:** FMS_Text_Display_Game_Object_Structure.md to include missing FMS_Panel element in path
- **Created:** AGENTS.md to track development progress
- **Status:** Phase 0 complete, ready for Phase 1

---

*This document is maintained by the Codex development agent and should be updated with each significant architectural change or refactoring.*
