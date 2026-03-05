# NAS_FMS_Claude_Project_Code_Refactor_Instructions

## NAS Pensacola FMS Codebase Review and View‑Page Standardization

## Project Purpose

This project directs Claude Code to assist with reviewing and improving the Unity C# codebase for the NAS Pensacola Flight Management System training simulation. The focus of the work is the review and editing of existing View page scripts so they follow a consistent rendering and interaction structure for displaying text on the simulated FMS display.

Claude Code will analyze the current implementation and expand the working pattern demonstrated in ActFplnView.cs across the remaining page View scripts. The goal is not to make every page behave identically, but to establish a consistent framework for rendering, LSK interaction handling, scratchpad interaction, and page state management.

The project is intended to reduce iterative conversations by providing Claude Code with sufficient architectural context so that it can plan and perform a full code update across the relevant scripts with minimal clarification requests.

## Codebase Location

C:\Users\Admin\Documents\Unity\Flight Management Sim\git\FlightSim

Unity project written in C# targeting iPad Mini resolution 1488 x 2266 portrait.

## Core Architecture Files

FmsModel.cs
FmsPageView.cs
FmsScratchpad.cs
FmsPageRouter.cs

ActFplnView.cs is currently the closest example of the intended architecture and should be treated as the reference pattern.

## View Pages To Review

ActLegsView.cs
DepArrView.cs
FrequencyView.cs
IndexView.cs
PerfInitView.cs
PosInitView.cs
ProgView.cs
StatusView.cs
StubPageView.cs

## Page Rendering Model

Each page writes text to TextMeshPro UI fields arranged in a standard hierarchy.

Page/TitleLine/Page_Number  
Page/TitleLine/Title

Page/Body/Body_Line_1/Label_Left  
Page/Body/Body_Line_1/Value_Left  
Page/Body/Body_Line_1/Label_Right  
Page/Body/Body_Line_1/Value_Right

Page/Body/Body_Line_2/Label_Left  
Page/Body/Body_Line_2/Value_Left  
Page/Body/Body_Line_2/Label_Right  
Page/Body/Body_Line_2/Value_Right

Page/Body/Body_Line_3/Label_Left  
Page/Body/Body_Line_3/Value_Left  
Page/Body/Body_Line_3/Label_Right  
Page/Body/Body_Line_3/Value_Right

Page/Body/Body_Line_4/Label_Left  
Page/Body/Body_Line_4/Value_Left  
Page/Body/Body_Line_4/Label_Right  
Page/Body/Body_Line_4/Value_Right

Page/Body/Body_Line_5/Label_Left  
Page/Body/Body_Line_5/Value_Left  
Page/Body/Body_Line_5/Label_Right  
Page/Body/Body_Line_5/Value_Right

Page/Body/Body_Line_6/Label_Left  
Page/Body/Body_Line_6/Value_Left  
Page/Body/Body_Line_6/Label_Right  
Page/Body/Body_Line_6/Value_Right

## Rendering Execution Model

Populate() is called every frame by the router and must only render UI state. It must not change program state.

## LSK Interaction Model

There are 12 Line Select Keys.

Left buttons 1‑6  
Right buttons 1‑6

Each page must expose handling for all 12 LSKs even if some are unused.

## Scratchpad Behavior

Scratchpad behavior varies by page. Some LSK presses seed values into the scratchpad while others require the user to manually type values.

## Page Formatting Model

Formatting helpers should exist per page so formatting can be customized later.

Example helper methods:

FmtTitle()
FmtLabel()
FmtValue()

## Unity Scene Modification Policy

Claude may suggest scene changes but must not modify Unity GameObjects without approval.

If changes are required Claude must first present a plan including exact Unity hierarchy paths and inspector fields.

After approval Claude may apply changes using MCP and must summarize modifications.

## Expected Claude Workflow

1. Analyze the codebase
2. Produce a plan for updating the View scripts
3. Apply consistent architecture across pages
4. Keep page‑specific logic intact

The goal is a consistent, maintainable View page architecture for the FMS simulation.
