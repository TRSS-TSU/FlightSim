# FMS Page-by-Page Unity Implementation Plan

## Description

This skill helps analyze, design, and implement **page-by-page behavior** for a **T-1A Flight Management System (FMS) / Control Display Unit (CDU)** in a Unity project.

It is intended for use when reviewing a Unity codebase, transcripts of CDU/FMS usage, button maps, reference manuals, scenario data, and related documents in order to derive a practical implementation plan for the FMS user interface and behavior.

The skill focuses on how CDU pages behave in response to user inputs, including function keys, line select keys (LSKs), PREV/NEXT paging, scratchpad transfers, route edits, direct-to operations, approach loading, tuning actions, and EXEC-based activation of pending changes.

This skill is optimized for a **training simulator MVP**, not for certification-level avionics replication. It prioritizes **teachability, determinism, realistic workflow, and maintainable Unity implementation**.

## When to use this skill

Use this skill when you need to:

- analyze the Unity codebase for existing CDU/FMS behavior
- compare the codebase against transcripts, manuals, or reference documentation
- determine what each CDU page should display
- identify how labels, values, prompts, and page states change after each button press
- separate ACT vs MOD flight plan behavior
- define how EXEC commits staged changes
- create a page-by-page implementation plan for Unity
- determine what should be built first for an MVP training workflow

## How to use this skill

When using this skill:

1. Read the current **Unity codebase** and identify any existing scripts, models, presenters, controllers, and UI bindings related to the **FMS/CDU**.

2. Review supporting documentation located in the **`/context/` folder** of the project.  
   This folder contains reference material used to derive expected CDU behavior, including:

   - **`context/Main_Transcript.txt`** — a transcript describing a real user operating the **T‑1A CDU** and explaining the workflow step‑by‑step.

   - **`context/FMS buttons.txt`** — a structural map of CDU screen elements and available input controls, including body lines, labels, values, LSK buttons, and function keys.

3. Use the files in `/context/` to understand how the CDU behaves during real operation. These files provide critical information for determining:

   - what fields exist on the CDU screen
   - what buttons the user presses
   - what values appear in each field
   - how the display changes during operation

4. Compare the **current Unity implementation** against the documented or observed CDU behavior described in the `/context/` files.

5. Work **page by page** rather than trying to solve the entire CDU system at once.

6. Separate behavior into three categories when analyzing functionality:

   - **Confirmed** — supported directly by the codebase or documentation
   - **Inferred** — derived from transcripts or observed workflow
   - **Missing / uncertain** — behavior not yet implemented or not clearly documented

7. Produce **implementation guidance** that Michael can use directly inside the Unity project to build or refine CDU page logic, including:

   - UI field updates
   - button and LSK handling
   - page transitions
   - scratchpad behavior
   - EXEC commit logic

## Core objective

The main objective of this skill is to help produce a **page-by-page Unity implementation plan** for the T-1A CDU/FMS, including:

- what each page shows on entry
- what data drives the visible fields
- what user actions change the page
- what changes are immediate vs staged
- what changes require EXEC
- what code structures should own the behavior
- how to validate each page in Unity Play Mode

## Primary analysis areas

This skill should focus first on these CDU/FMS areas:

- INDEX
- STATUS
- POS INIT
- FPLN
- LEGS
- PERF MENU
- PERF INIT
- DEP/ARR
- DIR
- TUNE
- scratchpad behavior
- CLR/DEL behavior
- PREV/NEXT paging behavior
- EXEC behavior
- route loading from scenario data
- direct-to resequencing
- approach loading and leg append behavior
- radio tuning swap/reclaim behavior
- callsign entry behavior

## What to analyze for each page

For each page under review, determine:

- the purpose of the page
- the current implementation status in the Unity codebase
- the observed or documented behavior from transcripts/manuals
- the display contract for the page
- the relevant button, LSK, and function-key actions
- the resulting state transitions
- what belongs in the data model
- what belongs in the UI/presenter/controller layer
- what must be validated in Play Mode

## Recommended output structure

When applying this skill, organize each page analysis using a structure like this:

### Page
State the CDU page name clearly.

### Purpose
Explain the function of the page in the pilot workflow.

### Current code status
Describe what already exists in the codebase, if anything.

### Observed/documented behavior
Summarize behavior confirmed by documentation, transcripts, or reference materials.

### Display contract
Define what the page should show, such as:

- title/mode
- page number
- left/right labels
- left/right values
- editable fields
- prompts
- scratchpad default state
- message line behavior
- color/state expectations if known

### Input actions and expected results
For each important input, describe:

- trigger
- preconditions
- immediate UI result
- whether the result is staged in MOD
- whether EXEC is required
- what values, labels, or fields change
- whether the page changes

### State transitions
Explain how the page or model state changes after each action.

### Recommended Unity implementation
Describe what scripts, methods, models, or bindings should own the behavior.

### Play Mode validation
Explain how Michael can test the page in Unity.

### Open questions / assumptions
Call out uncertainty clearly.

## Display-contract guidance

A CDU page implementation should generally account for:

- page title/mode text
- page number and total pages
- six LSK rows on left and right as applicable
- label text for each row
- value text for each row
- color/state differences for active, pending, editable, prompt, copied, or calculated values
- scratchpad contents
- message line contents
- page switching rules
- function line behavior near the bottom of the display

## Unity implementation guidance

When producing implementation recommendations, focus on practical Unity architecture.

At minimum, help define:

- how CDU screen fields are represented in code
- how page definitions are stored or rebuilt
- how scratchpad input/copy/transfer works
- how page-specific actions are mapped from LSK presses
- how ACT and MOD flight plans are represented separately
- how EXEC commits pending changes
- how route loading from ScenarioDefinition populates FPLN/LEGS
- how waypoint insertion/deletion works on FPLN and LEGS
- how approach and missed-approach legs are appended
- how DIR resequences the route
- how TUNE handles active/standby/swap/reclaim/callsign functions

## Working constraints

When using this skill, follow these constraints:

- optimize for teachability, determinism, and realistic workflow
- do not redesign the whole project architecture unless necessary
- prefer incremental implementation over broad rewrites
- respect existing code and project structure when possible
- if code and documentation conflict, call out the conflict clearly
- do not invent behavior casually; mark uncertain behavior as inferred
- keep recommendations buildable for a Unity MVP

## MVP target workflow

This skill should support implementation toward this training loop:

**Scenario load → POS INIT → route load on FPLN → LEGS review/edit → PERF INIT → DEP/ARR approach load → EXEC → DIR/TUNE interactions → usable training workflow**

## Suggested first implementation pass

A recommended build order is:

1. STATUS
2. POS INIT
3. FPLN route loading
4. LEGS waypoint review/insertion
5. PERF INIT weight entry
6. DEP/ARR approach loading
7. DIR direct-to resequencing
8. TUNE frequency swap/reclaim and callsign entry

## Expected result

When this skill is used correctly, the result should be a **clear page-by-page implementation plan** that Michael can use to build the T-1A CDU/FMS in Unity one behavior at a time, with each page grounded in observed workflow, documentation, and the existing project codebase.

