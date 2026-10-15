# ADR-0073 — M16 observed penalty yaw tracking

2026-10-15. Reliability correction found by the complete production career soak.
M16 remains in implementation/owner review; this is not a new race physics model.

## Captured failure

The fixed-seed career completed its first season, then its second-season week7
Pursuit rejected tick15577 with an unresolved course source for generated rival
`career_m16_soak_athlete_100`. The accepted field clock stayed at15576. The first
penalty entry at5682 was valid. An assumed phase-handoff wedge was disproved.

The early trace shows the actual origin: at5740 the body yaw was+0.770rad/s;
at5760 it remained+0.763 although the AI's ordinary steering had fallen to+7.
At5780 the requested steering was-404 but actual yaw was still+0.726. The body
over-rotated, reached the inner snow boundary at about27m of the circuit and
repeatedly requested a rotation that the unchanged full-body boundary could not
admit. It retreated for thousands of paid ticks. A later local snow-window cap
change made the final source rejection visible. Widening body/course allowances,
increasing response/time limits or promoting credited arc would mask the origin.

## Decision

Add optional, authored `penalty_yaw_tracking_error_radps` to the existing manual
AI policy. The production value is0.35rad/s. On an owned penalty occurrence,
above the shared physical minimum steering speed, compare achieved yaw with the
ordinary physical pursuit request. Only a disagreement above this threshold
uses the existing bounded snow inverse to choose a steering command. That
command still pays the same drive, grip, steering, physiology and course proofs.
Low-speed nominal recovery remains available; player controls and automatic
range approach/departure are excluded. There is no new impulse or motion law.

An exploratory application to all ordinary paths made a different first-season
Mass Start arrival fail. It was discarded. The committed scope is penalty only;
the exact first-season Mass Start then completes in19440ticks. This deliberate
narrow scope avoids changing already approved ordinary-course/range behavior.

Missing/zero field retains the previous AI behavior and serialized policy
encoding. Enabled policies bind AI contextv29; existing v27/v28 policies keep
their old context revision. Signed effective commands retain their replay path.
No course geometry, body dimensions, clearance, phase gate, clock, tick budget,
athlete statistics, force solver, visual quality or animation is changed.

## Evidence

The unchanged captured prepared Pursuit now finishes twice in21411ticks with
authority SHA256 `14f635e15385712d9b13bd086e714afcd96ec6bc13e6de6b8824938b9632a010`
and identical event hash12010429436503977100. The same fixture with the optional
field disabled still rejects tick15577 and pays no candidate clock. The generated
prepared career, fixed seed and source receipt are pinned under
`dotnet/Biathlon.Tests/Fixtures/m16_penalty_yaw/`. This is a material AI decision
change; old race result hashes are not claimed to remain equal. WP-215 requires
the fresh complete three-season run on the final authority before acceptance.

## Native runtime qualification

The recorded21411/19440 race ticks above are CoreCLR receipts, not cross-runtime
golden values. Native Unity Mono completes the Mass Start in19442ticks and does
not reproduce the old CoreCLR Pursuit rejection by tick20000. Do not turn these
different floating-point trajectories into a fake native historical failure.
Both current full races are required to repeat their complete authority JSON,
hash, tick, shot count and event hash exactly within the runtime under test.

The named historical CoreCLR full-race control remains compiled and executed
on CoreCLR. A separate all-runtime primitive locks the actual invariant: the
exact captured outside penalty source must reject the complete body as
Course/UnresolvedStart, grant no proof, pay no clock/travel and preserve cursor,
body and physical motion. Committing its invalid proof must throw. This adds a
native meaningful guard check without changing any production source or tolerance.
