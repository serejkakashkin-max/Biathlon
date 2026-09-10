# TASK-085B1 — Physiological start approach and exact route handoff

Status: DONE — 2026-09-10. WP085 remains IN PROGRESS; next TASK085B2 shared field.

Added versioned StartApproachDefinition and immutable StartApproachMap binding an authored open
race corridor to the exact track, route and route requirements. The synthetic Mass Start profile
runs five laps/four PPSS bouts; its test approach is30m long. Rear-row athletes begin27m behind
the route origin. Approach translation never becomes route credit.

RacePathMotion now emits explicit OnStartApproach receipts and uses the same technique, snow,
equipment, cornering and boundary integrator on that corridor. At the line, remaining fractional
tick time continues into the main route with the existing speed and physiology. SprintAthleteSession
accepts Mass Start only with this explicit binding, retains the common start clock, records exact
StartApproachExitTime and emits StartApproachCompleted once for a rear-row athlete. Front-row
athletes have zero approach travel. Replay context exposes both the map and initial rear offset.

Updated RaceMotionTimeline, FractionalMotionTimeline, CheckpointTiming and PhysicalMotionProjection
to distinguish actual approach travel from counted route progress. Projection validates the
physical approach origin and refuses receipts extending beyond the line. This is a per-athlete
projection contract; shared physical seam admission is still TASK085B2.

Three new tests compare every initial physiology tick against the independent original session,
check same-tick fractional handoff and projected travel, repeat a complete rear-row race/events,
and reject invalid bindings/offsets. Full race in both .NET and Unity: finish17917:831395357,
approach exit131:956168637,16 penalty loops, actual travel7427000000000nm (5000m race +27m approach
+2400m penalties). The high scripted miss count is test input, not AI balance evidence.

Validation: focused .NET23/23; full .NET475/475, zero failed/skipped,6m23s. Unity MCP10/10 across
StartApproachTests3, PhysicalMotionProjectionTests2 and RaceMotionTimelineTests5; discovery475.
Console0 errors,4 retained MCP lifecycle warnings, compilation_failed=false. Existing Sprint/
Pursuit deterministic regressions pass in the full suite. No new full Unity or PlayMode run claimed.
An initial focused failure exposed the existing reflection dependency on the seven-argument
RaceMotionSegment constructor; preserving that overload fixed it without weakening the test.

Evidence: artifacts/physiology/wp085b-approach-final.trx, wp085b-full.trx,
wp085b-unity-approach.json, wp085b-unity-approach-results.xml, wp085b-unity-projection.json,
wp085b-unity-timeline.json, wp085b-console.json; hashes in docs/quality/wp085b1.

Limits/next: connect30 real grid bodies to the shared field. Current full-width exclusive junction
policy cannot admit three parallel front-row bodies as an ordinary single junction. Implement and
verify physical continuity with lateral clearance across the approach seam before field launch is
claimed. Do not teleport rear rows, hide bodies, reset physiology or count approach as lap distance.
No owner blocker. WP085, WP086 and the M7 milestone remain open.
