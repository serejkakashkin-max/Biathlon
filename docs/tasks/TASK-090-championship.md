# TASK-090 — Three-format championship harness

Dependencies: WP080–089; implementation prepared while final WP089 checks run.
Objective: run30-athlete Sprint, load its versioned results into qualification, run the corresponding
Pursuit, then independently construct/run30-athlete ordered-lane Mass Start. Preserve athlete
personality by stable source ID across Sprint/Pursuit qualification reorder; Mass Start uses its
own roster and no qualifying dependency. One shared factory for CLI/Unity, no duplicate physics.

CLI writes incomplete status before work, exact contexts, result JSON, qualification JSON, event
CSV and hashes; completed only after all starts/shots/laps/penalties/travel/results validate.
Repeat CLI output hashes and test the same factory/chain through Unity MCP. Report M7 gate with
full/core regression evidence and explicit scope: synthetic headless races, not presentation,
career standings or production venues. Preserve unrelated owner edits and publish state/reports.
