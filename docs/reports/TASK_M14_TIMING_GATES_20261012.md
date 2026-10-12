# M14 owner revision — timing portals and finish clearance

2026-10-12. Implementation prepared; integrated Unity verification is owned by the main review run.

Woodland has three intermediate stations per full main loop: climb top at372.323m,
range approach at735m, and range/penalty exit at935m. The last is beyond the910.005m
penalty rejoin. Final-lap route forks keep their existing checkpoint occurrence rules.
No split time is derived from the visible art: each portal and blue snow stripe is
placed at the corresponding authoritative TrackCheckpointDefinition anchor.

The new rectangular portals have vertical supports outside the ski corridor, with
the nearest footing face1.05m beyond its edge and the header bottom4.875m above
the authored snow surface. Each station uses one148-vertex static mesh, three shared
materials and one depth-tested distance label. They contain no colliders, Update
method, per-frame ground sampling or per-frame mesh allocation. After the native
environment is ready, GroundSupports(environment) embeds each foot8cm into actual
terrain. This setup adjustment leaves the painted stripe and timing anchor untouched.

Only the six existing finish gantry mesh placements have lateral_scale1.4. Their
14m span puts the nearest low footing more than2m outside the9m finish lane. Width
changes retain the original height, native textures and ground-marking elevations.
Other meshes and quality settings are unchanged. Optional lateral_scale0 retains
the previous uniform scale when reading legacy environment recipes; invalid negative
or non-finite values still fail validation.

## Real-world reference and scope

[IBU Event and Competition Rules2026, §9.2.7](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf)
requires intermediate timing after the penalty loop following shooting. §3.5.1
defines an unobstructed9m finish corridor and §3.5.1.1 a red snow finish line.
The rules do not prescribe a rectangular arch at every intermediate station: that
shape and the three-station density are the owner's visual/gameplay requirements.

The timing supplier's [biathlon installation reference](https://www.raceresult.com/en/solutions/biathlon)
describes transponder timing loops buried in snow at start, finish and intermediate
points. Our stripe is a readable visual indication, not a raised timing obstacle.

## Verification entry points

Run PlayMode CourseTimingPresentationTests (four tests): production anchors and
clear ski aperture; native terrain foot embedding without stripe movement; actual
finish resource support faces; legacy/explicit lateral-scale recipe round trip.
WoodlandCourseDressingTests additionally checks all actual portal supports against
native terrain and paid course corridors. CheckpointTimingTests/RaceReviewTests
cover the shared timing/standings operator; the complete production replay regression
must be rerun because the checkpoint layout changes replay context.

For in-world captures, CourseTimingPresentation exposes GateTransform(index),
GateMesh(index), CheckpointId(index) and CorridorHalfWidth(index). Capture the approach
to each station and finish-spur80m. Inspect the stripe, header label and full left/right
support clearance while retaining the High graphics preset.
