# TASK-071B5C — Explicit branch transitions and prepared admission

Status: DONE through C1–C8 primitives and TASK-071C full race orchestration, 2026-09-09.
Final acceptance evidence: docs/reports/TASK-071C-shared-sprint-field-report.md.

B5A/B provide periodic continuous body queries and conservative exclusive fixed-branch admission.
Complete the transition protocol before connecting admission to staged Sprint athlete commits.

Authored junction ports currently define shared conflict regions, not directed travel edges.
Do not interpret any two ports in one region as a free teleport. Add validated, stable, explicit
directed connectivity with distance accounting; bind its exact definition to the new pack context.
Split trajectories at every transition, keep incoming/outgoing whole-body footprints until the
tail clears, and reject illegal connections or position changes. Existing occupancy must survive
a branch/slot change. Preserve M5 route/travel accounting and regression tapes.

In particular RangePenaltyTraversal holds main progress at penalty entry during circuits, then
travels from entry to rejoin on the main-progress axis. Mapping penalty seam directly to the
authored rejoin would silently skip that traveled segment. Resolve this in explicit physical
path mapping, with real traversal tests; do not change the old policy implicitly.

Add a prepare/validate/commit boundary so reservations, physical positions, physiology, range
obligations and events commit together. Replan followers when denial/shortening creates a
stationary suffix conflict. Test actual penalty entry/rejoin, multiple loops, full-body clearance,
permuted requests, invalid last command and exact replay before full shared-force field integration.
