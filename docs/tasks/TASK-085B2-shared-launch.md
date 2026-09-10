# TASK-085B2 — Thirty-body physiological shared launch

Objective: compose the verified grid/common clock and approach with PackSprintFieldSession.
Dependencies: TASK085A/B1. Preserve Sprint/Pursuit output and all continuous body checks.
Use an explicit start-on-main-circuit physical policy, accepted only when the approach exactly
matches the constant terminal main-loop terrain. This avoids a fictitious branch junction; the
front and rear rows share periodic physical occupancy before and after the actual start line.
Extend field scheduling/context/results for Mass Start without a fabricated Sprint dependency.
Tests:30-body repeated first-minute launch, actual offsets/travel, common starts, invalid bindings,
physical projection, zero overlaps and prior Sprint/Pursuit regressions; Unity MCP and Console.
Acceptance: all30 leave the approach with preserved physiology and no route credit/teleport.
Risk: mapping must reject different terrain/topology. Keep this policy opt-in and exact-context
bound; old fixtures and original field behavior remain regression baselines.
