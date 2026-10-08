# M13 — stationary shooting on the common world clock

2026-10-08. Continuation after controlled slip steering `c16794d`.
M13, playable host migration and the final review build remain OPEN.

## Problem and change

The first-event scheduler correctly reports an already-owned firing endpoint
with zero elapsed time. An athlete already aiming on that endpoint needs a real
stationary interval, however: repeatedly reporting the same zero-time boundary
would prevent a future common runner from paying the field's remaining clock.

World context v14 adds an explicit `stationaryFiringActors` argument to
`TryAppendToOwnedFirstEvent`. The marker applies only to a selected occurrence
whose end is `FiringLine`, with both observed and credited arc exactly at that
mat's full endpoint. The actual pose must independently satisfy the physical
endpoint proof. Initial world velocity, the genuine constant centre acceleration
and the complete trajectory's peak yaw must be **exactly zero**. There is no
rounding-to-rest or position/velocity correction. A marked moving or accelerating
actor returns `ShootingHold` before any clock or interval is staged.

Only the repeated endpoint notification is omitted for those proven stationary
actors. The complete candidate still passes common contact scheduling and the
same occupied-body, supplied edge, selected course, portal, local projection,
high-water and session publication checks. A hit by another skier or a changed
force cannot be hidden by a hold marker. No suppression applies to start, ordinary
route, exit, penalty or finish endpoints. Omitting the argument retains the old
zero-time notification behavior.

The per-actor flags are cold-allocated and cleared before every valid event query.
Duplicate/out-of-range indexes fail. No shooting completion, target result, mat
ownership or departure authorization is granted; the session's existing physical
shooting and five-shot phase gate remains authoritative.

## Evidence

Eleven new tests cover a fully paid unchanged shooting-body/session publication;
a stationary aiming actor and moving skier sharing one full clock in both roster
orders; forward/reverse velocity, spin and force-driven departure rejection;
ordinary/partial-cap rejection; an explicit forbidden edge; duplicate/foreign
markers, marker lifetime, unchanged default notification and zero warm allocation
over 1,000 common-clock queries/aborts.

Initial .NET **11/11 PASS**. All-manual plus Sprint/Pack/Player replay **442/442
PASS**. Unity initially found a test-source incompatibility: its bundled NUnit
does not provide `.AnyOf`. The equivalent `.EqualTo(...).Or.EqualTo(...)` replaces
that assertion; no test, bound, policy or production code was relaxed. The initial
Unity compile-error receipt is retained. Final native: **11/11** new shooting
hold plus **21/21** existing common-event tests PASS. Final refresh reports
success, engine is not compiling/playing and recent Error/Exception logs (last
minute) are empty. The saved older fixed compile error remains in log history;
the Console cache was not cleared. Receipts are in
`docs/quality/m13/shooting-hold-20261008`.

This fixes a common-clock composition prerequisite. It does not prove elimination
of the owner's playable queue, qualify a complete multi-athlete race or connect
the production host. Actual range preparation/braking/animation, the complete
runner, AI intent, course seams, reusable mat ownership, finished actors, full
v2 replay and host still require work. WP-175/177/178 OPEN; WP-176 PASS. No new
Windows build, owner gate, shooting difficulty tuning or Master Plan change.
