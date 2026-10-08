# M13 — shared longitudinal driving during post-contact slip

2026-10-08. M13/WP-175/177/178 OPEN, WP-176 PASS. No Windows build.

`ManualSkiContactDriveSystem` combines the existing `SkiForceSystem` longitudinal
force trajectory with the existing passive snow slip/spin preview. Power, drag,
snow resistance, directional grade, braking, physical stop and speed cap use the
shared definitions. Body mass must match that force definition. Accepted world
velocity is decomposed along the frozen ski axis; lateral velocity and physical
spin remain independent. The preview never snaps velocity onto heading.

For backwards longitudinal motion, propulsion brakes that motion. If it reaches
physical zero inside the interval, the remaining time uses the same shared force
solver from rest in the forward direction. There is no extra stopped tick or
launch speed. `SkiDrivenContactTrajectory` retains both force pieces, the exact
initial motion and frozen passive ramp. Coordinate extrema validate the numerical
envelope; continuous sweeps receive acceleration/yaw/error bounds.

`ManualSkiWorldTrajectory` accepts this third value-type variant. Whole and nested
slices retain the original previews rather than recalculating an impulse for a
prefix. Actual velocity norm supplies physical travel with the existing fixed
64-panel nonnegative quadrature. Quadrature error at a force kink remains an
approximation; fragment sums are not claimed bitwise equal to the whole integral.
Existing commanded-force and passive-contact query policies are preserved.

Validation:

- First build exposed four test uses of internal catalog members/an unrelated
  physiology field. Tests now use public scoped observations and the actual
  accepted odometer; no production visibility or assertion was weakened.
- Initial **13/13 .NET PASS**, then **14/14 PASS** with actual reached-contact
  response and a force-driven suffix on the same owned0.05 s world clock.
- Focused compatible selection **84/84 PASS**. Its slice/race-replay filters were
  initially named incorrectly; the subsequent corrected broad selection includes
  every manual class and Sprint/Pack/player race replay.
- Corrected broad default **364/365**: the existing
  `ManualWorldRosterLedgerTests.EntireClearRosterCommitsOnceWithoutGuessingRaceOdometer`
  allocation assertion measured5496 B. This failed qualification is retained;
  earlier327/328/440 B failure is not erased. No failing test was removed,
  disabled or relaxed.
- Native Unity **14/14** new drive tests, **17/17** original slice tests and
  **13/13** spatial contact tests PASS. Discovery1449 is not the execution count.
  Compile settled, not playing, Error Console empty.
- Tests cover rest/aligned cap parity, retained lateral velocity/raw spin, grip
  reservation, backwards braking/paid restart, actual Woodland directional grade,
  tuck/snow/wind modifiers, exact slice endpoints, body/edge sweep admission,
  real scoped physiology/session publication, reached contact/response/suffix,
  invalid mass/surface/data rejection, and0 B warm driven/sliding queries.

[Evidence](../quality/m13/manual-contact-drive-20261008/).

This is a frozen-axis longitudinal contact continuation, not a complete steering
controller. It applies no new yaw command; controlled yaw while slipping, changing
surface/cross-slope integration and unequal-force persistent contacts still need
the live runner. Reusable mats, finished actors, event/replay policy binding,
production host, broad allocation qualification and final M13/build remain OPEN.
No playable queue/manual-control completion is claimed.
