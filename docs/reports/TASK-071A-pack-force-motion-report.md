# TASK-071A — Simultaneous pack force motion report

Status: PASS for this atomic kernel task, 2026-09-08. WP071 remains IN PROGRESS.
Added PackForceSession/Command/Frame and PackForceTests. The session owns its fixed-roster
positions and calculates each athlete's draft/forces from one immutable start arrangement.
Lateral proposals are rate-bounded and pay the shared propulsion loss; one SeparationResolver
then checks simultaneous trajectories and commits all positions/contact episodes together.
Caller cannot inject a replacement position through a control command.

356/356 standalone and Unity EditMode pass; Console zero errors/four upstream warnings.
A 12-athlete, 1,000-tick catch-up fixture verifies every pair's continuous trajectory, shared
force/draft equality, constrained motion never exceeding the proposal, and exact replay events/
frames. Further tests cover lateral changes, invalid-last-row atomicity, reversed-roster per-ID
results and zero .NET warmed pack allocation. Existing pinned race/shooting regressions pass.

Limitations are explicit: this is a fixed-corridor, whole-tick force kernel, not the complete
Sprint pack orchestrator. Caller supplies continuous-physiology-derived power and whole-step
valid terrain/minimum width. Lateral effort surcharge must be accounted for upstream; this
kernel applies propulsion loss and grants no extra power. Dynamic route boundary substeps,
range/penalty junction occupancy and lap-overlap mapping are TASK-071B. No 30-athlete complete
pack-race or M6 PASS claim. M5 independent-path behavior remains unchanged.

Next: TASK-071B staged athlete preview/commit and coordinated race-path integration. Its task
file records required boundary, replay-context and shared-physics constraints. No owner gate.
