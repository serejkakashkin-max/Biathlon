# ADR-0034 — Force-weighted manual motion preview

2026-10-06. Accepted intermediate integration under ADR-0028. Not enabled in races.

## Decision

The existing scalar-distance heading overload remains unchanged. Its uniform
within-tick distance density cannot represent acceleration from rest or an early
stop. `SkiForceTrajectory` exposes the actual frozen-force speed ramp from
`SkiForceSystem`, including the stop/speed-cap time, physical distance and prefix
state. It obtains forces through the existing solver; legacy Step and StepDuration
keep their arithmetic and output. Physical distance does not award route station.

The force-trajectory heading overload integrates speed(t) against heading(t),
splitting quadrature at speed bounds and the target-yaw-rate arrival. The planar
scale is the cosine of the actual directional grade, frozen over the interval.
The yaw request uses peak interval speed conservatively. Grip telemetry checks
the maximum of speed(t)*abs(yawRate(t)), including interior quadratic extrema.
Translation stops at the physical stop time; yaw may continue pivoting in place.
The Simpson samples remain numerical approximations, not swept-contact proofs.

`ManualSkiMotionSystem` composes local course observation, current surface metadata,
the shared force model, shared cornering capability and force-weighted heading.
It computes gravity and wind along absolute heading. The wind vector denotes air
motion toward world X/Z, so its negative dot with heading is headwind. Surface
exposure is applied once by the existing force model. Snow/risk/width metadata is
retained. The observation and metadata must refer to the same loop and local
station; a one-millimetre floor plus numerical encoding reserve is allowed.

Manual cornering uses the actual yaw rate: planarSpeed*abs(yawRate) is lateral
demand. For frozen yaw, safe slope speed is grip/(abs(yawRate)*planarScale), with
the existing braking response and force ceiling. No centre-line bend brakes a
skier whose yaw is zero. This also avoids dividing by near-zero speed to invent
curvature. The legacy centre-line cornering overload is preserved.

Force evaluation is explicit at the beginning of a surface interval, consistent
with ADR-0004. Directional grade/wind, yaw-based braking and planar scale are held
for that interval. The heading preview reports any remaining grip excess instead
of snapping away yaw momentum. This is not a complete lateral slip solver: the
future joint movement/contact solver must bound its interval and handle residual
grip excess. Tuck/technique/physiology still supply delivered power and modifiers;
this layer neither creates athlete power nor secretly applies AI bonuses.

Definitions and clock are captured in an explicitly named preview context. This
is not yet a race replay format. The preview is immutable and event-free. Legacy
contexts, commands, factories and race output remain unchanged.

## Outstanding integration

Live world pose ownership, start/private range/repeated penalty cursors, continuous
oriented bodies and course edges, contact response, AI yaw controller, v2 race
recording and host input/presentation remain required. Backward world motion does
not become positive route progress. No automatic direction correction is added.
Owner-authorized final mat docking remains a separate bounded assistance feature.

Validation: 48 focused .NET tests, 23 legacy player/Sprint/pack replay tests;
native Unity 8 force-trajectory tests and 6 composed-motion tests pass. The changed
legacy force/cornering entry points retain their regression assertions. No new
Windows build, production tuning selection or manual gameplay completion claim.

## 2026-10-07 encoded endpoint continuity

The accepted force-heading endpoint velocity now uses the same encoded final
heading and scalar speed as its accepted pose and SkiState. Interior samples
retain the continuous force trajectory. The previous raw endpoint arithmetic
could differ from the next initialized tick by nanometre/nanoradian encoding,
and the exact world-ledger continuity gate rejected that otherwise valid
second tick. A 200-tick flat acceleration/turn regression and real Pack
two-tick publication exercise the correction. Variable grade intervals and
post-contact side-slip still require their own qualified continuation; no
continuity gate is relaxed and no full-race integration claim follows.

## 2026-10-07 accepted planar velocity continuation

An explicit accepted-velocity preview treats the incoming horizontal vector
as the kinematic authority. Its norm and the currently observed directional
grade rebase the scalar coordinate consumed by the same force solver, retaining
the physical odometer. The encoded scalar rounds upward; only a 16-ulp floating
scale/cap reserve repairs representational roundoff. Significant cap excess
still rejects without changing accepted state. This is a tangent-plane scalar
conversion, not a full three-dimensional momentum/energy solver.

For heading-aligned nonzero motion, yaw rotates the accepted initial velocity
basis and the shared scalar ramp scales its magnitude. Simpson position samples
use this same basis. From rest, ordinary heading-directed force acceleration
is retained. The exact start vector is never reconstructed from a route tangent
or relaxed by the ledger's continuity gate. Numerical endpoint encoding and
trajectory reserves remain explicit. Significant side-slip or reverse-aligned
velocity must use the contact/slip continuation rather than being silently
rotated. The separate accepted-velocity context binds the new policy while the
legacy preview context/API remain unchanged.

Real Woodland Pack publication now exercises 21 jointly proven ticks for two
sessions in different selected sections, driven by existing AI yaw intent and
with changing directional grade. This still does not admit private phases,
surface boundary splitting, above-cap continuation, full manual replay or host
input. Repeated whole-runtime allocation anomalies remain unqualified even
though the final 177-test manual run and native focused cases pass.
