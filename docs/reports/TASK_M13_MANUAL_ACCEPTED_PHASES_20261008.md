# M13 — accepted lifecycle endpoint handoffs

2026-10-08. M13 and WP-175/177/178 OPEN; WP-176 PASS. No Windows build.

The session batch can authorize zero-time phase handoffs at actual accepted
endpoints. Its private lifecycle reads the real completed shooting session and
penalty traversal, not a supplied miss count or geometry label. Geometry then
requires full observed/credited endpoint and the same world pose observing the
explicit successor at arc0 with unchanged base credit. All requested rows and
targets validate before world mutation. Pending input/world proofs, incomplete
bouts, wrong penalty ordinals/debt and repeat departure attempts cannot grant
admission. No position, heading, velocity, odometer, physiology, clock or race
event is added by the coordinate handoff. Context world v7/session v3 records it.

Actual shooting completion authorizes its selected mat's exit. Penalty access
chooses the real first circuit or counted return; a completed circuit chooses
the next actual ordinal or return. Bout-specific traversal telemetry is exposed.
An expanded test found that Woodland's penalty entry and rejoin coincide, so
completion immediately advances the active bout pointer. Authorization must use
the endpoint's actual bout, retaining its completed traversal, and accept that
completed zero-length rejoin. That defect is fixed; the failing receipt remains.

Validation:

- Initial3/3; expanded5/6 exposed the completed-bout return issue; final focused
  .NET **77/77 PASS**, including scoped intervals/session/tick/contact/progress
  and existing Sprint/Pack/player replay regressions.
- Native Unity **6/6** new cases PASS, discovery1394; compile settled and Error
  Console empty. Warm rejected phase queries allocate0 B.
- A real Woodland bout fires five actual shots through the shared shooting
  session while every stationary tick has a full world proof. Departure changes
  only its scoped coordinate; the next manual force tick moves from rest with
  shared delivered power, with no extra wait or artificial launch speed.
- Three accepted penalty boundary snapshots validate actual debt/ordinal and
  same-pose targets. Setup uses exact clipped legacy boundary snapshots to
  isolate accepted gate authority. That clipping is not a production policy to
  brake or wait at penalty junctions and does not qualify traffic continuation.

[Evidence](../quality/m13/manual-accepted-phases-20261008/).
[Design](../adr/ADR-0038-manual-race-physical-occurrences.md).

These are accepted endpoint handoffs between ticks. General in-tick future
lifecycle authorization is still required to traverse penalty junctions without
an artificial wait when reached partway through a tick. General contact timing,
mat choice, impulse event journal, finished actors, full Pack/field/replay and
production manual controls/rig remain pending. Zero-debt/all-roster phase traffic
needs the final integrated race matrix. Earlier broad allocation qualification
remains OPEN. This does not close playable queue behavior, full manual races,
M13 or the final combined owner build.
