# M13 — reached contact response inside one owned world tick

2026-10-08. M13 and WP-175/177/178 OPEN; WP-176 PASS. No Windows build.

Scoped world intervals now admit explicitly selected terminal body contacts
only when the actual endpoint SAT gap is within the unchanged contact tolerance.
The continuous sweep still rejects earlier deep penetration, unselected contacts
and course/phase crossings. A conservative potential-contact bracket alone is
not a reached contact. Separated and deeply penetrating endpoints are rejected.

The roster binds an immutable complete mass vector and the existing deterministic
group response definition. Arrival stages a pending response at scratch poses.
No further movement or finalization is permitted until response succeeds. All
response rows validate before any scratch row changes; an unresolved three-body
chain retains the earlier paid scratch interval without publishing its provisional
impulses. Aborting discards the entire tick. A successful impulse changes velocity
and yaw rate, not position, heading, clock, odometer or progress. Physical speed
is reconstructed from world velocity and the actual observed directional grade.

The suffix can then be paid using existing-contact continuation. Two real
Woodland actors collide after an initial gap and travel the remaining time in
one fixed tick, with one complete world publication. Equal and unequal masses
produce the expected momentum-sharing speed. Dimensions, contact tolerance,
abilities, shooting and live race behavior are unchanged. Context v5 captures
the bound response/masses; spatial sweep context v2 captures terminal admission.

Validation:

- Final focused .NET **94/94 PASS**: arrival, scoped tick/world, pair/group/joint
  sweeps and existing Sprint/Pack/player replay regressions.
- Native Unity: **9/9** new arrival cases, **13/13** existing pair sweeps,
  **8/8** group response and **11/11** scoped tick cases PASS. Discovery1370;
  compile settled and Error Console empty. The combined class-filter attempt
  executed zero cases; the stated results come from separate actual class runs.
- Tests cover immutable masses, identity validation, pending response guards,
  actual endpoint versus separated/penetrating endpoint, an earlier deep
  penetration returning to a reached endpoint, failure atomicity/abort,
  complete-clock continuation and zero warm allocations.

[Evidence](../quality/m13/manual-contact-arrival-20261008/).
[Design](../adr/ADR-0038-manual-race-physical-occurrences.md).

This primitive does not yet locate the first contact of a general turning
trajectory, authorize phase gates, allocate a mat or emit the owning race's
contact events. The test fixture derives its arrival time from exact linear
geometry; it does not guess a potential-contact midpoint. The world-only commit
must still be bound to the complete session/physiology/event/replay transaction.
Playable production input remains legacy lateral/route-following. Earlier
intermittent broad allocation qualification remains OPEN; current focused passes
do not establish its cause or fix it. Next: physical interval scheduling and
surface/phase continuation, then session/replay/host integration and final build.
