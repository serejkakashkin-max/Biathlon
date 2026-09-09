# TASK-071B5A/B — Periodic occupancy and fixed-branch admission

Status: PASS for the bounded A/B tasks, 2026-09-08. WP071 and parent TASK-071B5 remain IN PROGRESS.

Changed `PhysicalOccupancySystem.cs`, `JunctionAdmissionSession.cs`, the appended event types
in `RaceEvent.cs`, two corresponding EditMode fixtures, ADR-0011 and task/state documentation.

Periodic queries use the existing OccupancySystem relative-motion rectangles and data-driven
body/clearance dimensions. Lapped athletes share physical space; closed seams and complete
circuits retain continuous occupancy. At most one circuit per linear common-time substep and
body separation smaller than loop length are explicit preconditions. Five images suffice under
these bounds. Open loops do not wrap. Batch queries validate all identities and choose earliest
contact with ordinal athlete-pair ties. They include the stationary suffix after shortening.

JunctionAdmissionSession owns a canonical roster and refuses discontinuous starts or branch
changes. It retains incumbent reservations for a complete common interval, grants newcomers
all requested regions or none by stable ID, and holds whole-body inclusive guards until clear.
Denied entrants stop. If that creates a conflict with a follower, no positions, clock, ownership,
admission results or events are committed; the caller must replan and retry. Initial conflicting
owners are rejected. Reserved/released events are reproducible, including same-interval crossings
whose endpoints are outside. Event payload is the junction index in the exact sidecar context;
initial positions and derived owners must also be bound by the future pack replay context.

Validation: 379/379 standalone and Unity MCP EditMode, zero Console errors and four upstream
warnings; no project compile warnings. Added 11 tests covering lapped/seam bodies, nanometre
touches, continuous longitudinal/lateral crossing, full circuits and second periodic images,
fixed-seed sampled geometry, invalid batches, stable order, stationary follower conflicts,
incumbent priority, whole-body release, illegal identity/branch changes and exact event replay.
Warmed .NET allocation checks pass at zero; the existing Unity allocation counter limitation
still applies, so Unity zero alone is not used as allocation evidence. Captures:
`artifacts/physiology/wp071b5b-final-*`. Existing deterministic Sprint/scenario fingerprints pass.
Unity MCP PlayMode also passes 6/6; post-Play Console has zero errors and one upstream warning.
`git diff --check` passes (pre-existing tracked files emit CRLF conversion warnings).

No force, physiology, shooting, tuning or old M5 replay behavior changed. This is conservative
exclusive-region admission, with no fairness/starvation guarantee and no width/force solver.
It is not yet connected to SprintAthleteSession or shared physical firing lanes. Actual branch
transitions and coordinated prepared commits remain TASK-071B5C. Existing range traversal still
travels entry-to-rejoin after penalties; physical mapping must preserve that distance instead
of teleporting directly from the penalty seam to the rejoin anchor. No owner gate is active.
