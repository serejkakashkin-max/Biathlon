# TASK-071B — Physical path boundaries and continuous physiology

Status: DONE through staged primitives and TASK-071C integration, 2026-09-09.
Final acceptance evidence: docs/reports/TASK-071C-shared-sprint-field-report.md.

Dependency: TASK-071A corridor kernel. WP071 is not complete until a race field uses actual
pack placement without overlap, tunnelling, teleportation or hidden physical bonuses.

Required implementation:
1. Add a staged athlete API: preview physiology and motion without emitting/committing events;
   accept a validated constrained trajectory, preserving physiology costs and exact timestamps.
2. Subdivide a shared field tick at authored surface/range/penalty boundaries. All interacting
   athletes must use the same substep time interval. Do not interpolate route progress through
   a branch transition or resolve unrelated path origins together.
3. Map each physical branch to a stable corridor identity and common coordinate origin. Race
   distance/lap progress stays separate from corridor distance; lapped athletes share physical
   space when on the same actual loop. Penalty circuits share their physical loop.
4. Resolve entrance/rejoin transitions as checked movement. A corridor ID change must not be
   used to evade a collision at a junction; retain incoming/outgoing occupancy until clear.
5. Use actual constrained traveled distances for range obligations/checkpoints/finish fractions.
   Contacts slow motion but never refund already delivered physiological effort or grant speed.
6. Integrate cadence/perception/pace/draft/overtake policies with all-participant start snapshots.
   Preserve the M5 independent-path context policy and its replay fixtures; add a new explicit
   pack policy/context rather than silently changing old tape interpretation.

Atomic splits inside WP071 may be refined when staged code is inspected. Tests must include
single-athlete equality, uninterrupted physique/recovery through bouts, dense lane swaps,
branch arrivals/rejoins, lap overlap, replay and invalid-last-command rollback. Runtime and
zero steady-allocation evidence are required; whole-race initialization allocations are separate.
No pack milestone PASS until the actual complete race uses this orchestration.
