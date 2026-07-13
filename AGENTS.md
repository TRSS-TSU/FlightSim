## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, invoke the `skill` tool with `skill: "graphify"` before doing anything else.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- Dirty graphify-out/ files are expected after hooks or incremental updates; dirty graph files are not a reason to skip graphify. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).

<!-- CURRENT_HANDOFF_START -->
## Current Handoff

Completed this conversation:
- Confirmed Unity MCP is usable against `C:/Users/Admin/Documents/Unity/Flight Management Sim/Active/FlightSim`.
- Reviewed the NAS FMS MVP plan, then finished the hold/landing/results closeout path.
- Fixed the missed PENSI flow by promoting `Takeoff -> Enroute` on real waypoint callbacks, suppressing non-loop final-waypoint repeats, and keeping takeoff gated until `BeginTakeoff()` succeeds.
- Added Play Mode regression coverage for `PENSI -> hold -> landing -> ScenarioResults`.
- Refreshed Graphify and the edge-map doc after the fix.

Current state:
- Branch: `codex/nas-fms-mvp`.
- Latest commit: `6c06bbf fix: stabilize NAS hold transition`.
- Runtime code and the new regression test build clean.
- Unrelated Unity/package churn remains dirty and intentionally unstaged: `Assets/InputSystem_Actions.inputactions`, TextMesh Pro fallback asset, package manifests, `ProjectSettings.asset`, and `ProjectSettings/Packages/com.unity.ai.assistant/`.

Next steps:
- If the user wants the repo fully clean, decide whether to keep or discard the unrelated Unity package/editor churn before committing it.
- If they want another pass, use Unity MCP to re-run the Play Mode smoke from `Master_FMS` into `ScenarioResults`.

How to resume:
- "Continue from commit `6c06bbf`: the NAS hold/landing loop now works, the new regression test is in place, and only unrelated Unity/package files are still dirty."
<!-- CURRENT_HANDOFF_END -->
