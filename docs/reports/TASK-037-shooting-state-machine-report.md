# TASK-037 — Shooting state machine

Status: PASS, 2026-09-07. Added ShootingDefinition/lifecycle JSON, stable lane assignment,
ShootingStateMachine/Frame and appended event IDs 20–22. Bout: Outside → AwaitingLane →
Setup → Aiming ↔ BoltCycle → Exiting → Complete. Delays use ceiling(seconds × tick frequency)
and become ready on the exact deadline tick. Early trigger attempts consume no ammunition.
Lane reassignment/wrong range/nonconsecutive ticks fail before mutations. ShotAccepted means
weapon discharge only; hit/miss is intentionally not fabricated before geometry integration.

204/204 .NET and Unity EditMode tests pass; Console zero errors, four upstream warnings.
Evidence: artifacts/physiology/wp037-standalone.trx and wp037-final-{editmode,console}.json.
Timing checked at 20/30/144 Hz; identical frame/event streams; active bout and idle ticking
allocate zero measured bytes. Full earlier deterministic/CSV regressions pass.

Limits: coordinator must guarantee exclusive lane ownership. This is one standard five-round
bout primitive, not relay spare rounds/penalty rules or full race integration. Setup/bolt/exit
values are provisional data, with athlete handling modifiers deferred to WP043/WP047.
No ski balance changes. No commit because unrelated baseline edits remain. Next: WP038 geometry.

