# ADR-0096 — Physical admission at an occupied interval start

2026-11-05. M16.6.12 owner crash repair. The owner Developmentplayer log rejects
ContactGeometry at tick1294 during world_cup_open_pursuit. The player(index21)
had started at tick825 but remained stationary at the launch line. Entrant28 was
scheduled at1293 and became physical on1294 at exactly the same world pose:
(88.5,46.308216361), headingpi, both velocities/spins zero. SAT gap-0.7m is real
penetration, not floating-point noise. A read-only copy of the owner career and
ordinary zero-effort commands reproduce the exact tick and pair in8seconds.
Owner profile data remains local; public portable tests use a controlled Sprint
qualification and eight synthetic identities, reproducing the same collision on
44: slots0and7 share the finite start line. No fabricated sporting result is
used for the actual-owner validation.

The stable initial pursuit line allocation is intentional: seven full-width
slots repeat for later departures. It cannot reserve a unique line for every
possible held predecessor. Disabling current-contact query reuse reproduces the
same failure at44, excluding the M16.6.10 cache. Original contact geometry and
negative penetration rejection remain unchanged. Never mask this failure by
excluding a body that has already entered, increasing tolerance, moving a skier
aside or replacing the failed physical step with unpaid time.

## Decision

Extend start_continuation_version with3. This retains the v2 complete authored
start-snow proof and adds a cold-bound physical-release gate for interval starts
and pursuits. MassStart grid bodies retain their original presence and common
gun behavior. New production factory/career races select3. Recorded0/1/2 retain
their original source behavior and command contexts, including backward seek.
Replay metadata accepts3 and rejects4; no save schema migration.

Before each complete fixed tick, preview ready but not yet released entrants in
scheduled-start order with stable field order for ties. A staged body may become
physical only if its original spawn rectangle fits among already admitted bodies
and earlier valid releases under the unchanged contact geometry/tolerance.
If occupied, keep it in stationary off-course staging. Once admitted, the body
always participates normally; stopping on course cannot erase or hide it.
Do not move the body, select a free lateral teleport or change a scheduled start.

The official lifecycle clock starts on the original scheduled tick. Held entrants
pay elapsed race time, have zero propulsion and ordinary resting physiology, and
receive no distance, free movement, shooting permission or race-time refund.
Their unchanged stationary trajectory is proved for every full paid interval.
When the position clears, normal shared drive/contact/course checks apply. A
held start lane is exposed in the detached snapshot and localized HUD.

Release ticks, held state and one-shot transition diagnostics publish only with
the accepted world tick. Preview/abort/discard cannot publish a release. The
release preview is cached for the exact accepted-source tick; fixed arrays and
struct geometry avoid per-tick allocation. Add stable event types48(StartReleaseHeld)
and49(StartReleased). Payload reports paid wait ticks; released wait remains
fixed afterwards. The existing command journal deterministically reconstructs
the gate and event transitions. The new policy/context prevents replay with the
old solver. No Unity reference enters core assemblies.

## Qualification

Portable tests cover occupied Pursuit and30second Sprint starts, eventual release,
unchanged scheduled ticks and ticking race clock, no held travel/ghost body,
bilingual waiting feedback, one-shot events, aborted preview and exact command
re-execution. A legacy v2 control still rejects at44 with and without geometry
cache reuse, documenting old policy rather than silently changing old recordings.
The owner-specific diagnostic is explicit because it consumes a private local
read-only save; the portable regression is part of the normal test assembly.
New3and historical0/1/2 metadata/geometry/replay tests remain active. Complete
current affected-start/core regression, native checks, owner replay and build
receipts are recorded in docs/quality/m16-6/start-contact.

Capabilities, force equations, shooting geometry, official distances and published
qualification gaps are unchanged. New races can have different elapsed finishing
times/order when a starting lane is physically blocked: the newly admitted wait
is intentional and visible. The gate retains the assigned line and waits for it
to clear. Existing on-course passing remains a separate physical AI decision.
M16.6 remains open for owner review; this does not authorize M17.
