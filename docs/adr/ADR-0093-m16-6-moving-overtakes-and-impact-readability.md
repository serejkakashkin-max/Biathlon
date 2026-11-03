# ADR-0093 — Moving overtakes and visible physical impacts

2026-11-03. Owner extends performance task: also check shooting, repair a faster
fresh athlete following a slower moving athlete without passing, and make body
collisions visibly turn the athletes. Intentional AI decision revision; no
power/speed/stat multiplier, hidden RNG or rigidbody authority.

## Moving pass

The real production ManualScopedAiMovement explicitly excluded moving blockers
on BaseRoute/StartApproach from passing-line selection. A controlled two-athlete
single-file grid (6m spacing) on the original wide start reproduces the issue:
after 200 ticks, the faster follower remains behind, maximum gap -1.526735928m.
The test uses ordinary player input 150/1000 for the leader, unchanged athlete
statistics, real force/contact/course admission, and checks an observed speed
advantage. Adjacent-grid traffic had obscured this defect in the older stationary
obstacle integration test.

New immutable manual_ai_steering_v3 selects a moving pass only for an observed
closing body on the shared course, at least 0.2m/s achieved forward-speed
advantage and anaerobic reserve at least 0.2. These are data fields. The existing
rotated-body forecast tests both candidate sides, existing width/heading/bend
reserves bound the target, and the full physical solver decides actual movement.
If neither side is open or reserve/speed is inadequate, braking remains active.
An accepted passing line persists until the body is fully behind or the owned
occurrence changes. No lateral teleport, control snap or free propulsion.

Production/career hosts and headless CareerRaceProvider.Run select version 1.
RaceReplayInfo records optional moving_overtake_version (0/1). Missing/zero
reconstructs immutable v2, 1 reconstructs v3, including after backward seek.
Metadata rejects unknown versions; legacy canonical JSON omits zero. The AI
movement context changes to v30 only for the new policy. Generic legacy bindings
retain their supplied policy. Existing careers are not rewritten; their next
new race uses the current AI. Old recorded races keep old decisions.

Unit/integration controls cover original following, new actual pass, unmet reserve
gate, and an accepted 200-tick command journal replayed without AI. Original
occupied-side/narrow-corridor tests remain active. Performance-only state witnesses
explicitly use v2 to isolate exact query reuse from this requested AI change.

## Impact readability

The existing angular contact solver already produces spin and velocity exchange
for eccentric impacts and conserves momentum without adding energy. A new
7m/s closing-speed witness verifies both raw spins exceed 1rad/s, the fast body
loses speed, and the other receives momentum. A symmetric central impact is not
given arbitrary torque.

Add a read-only SkiImpactObservation for the strongest actual reached-body
impulse per actor per tick (delta planar velocity and signed spin). Scratch
observations are reset at BeginTick, cannot escape before commit, survive an
aborted next attempt as the prior accepted state, and clear on an ordinary next
tick. RacePresentationSnapshot detaches this observation. It never modifies a
force, pose, timer, input, route credit, physiological cost or result. The accepted
command journal re-executes the same solver and reconstructs the observation;
there is no separate random presentation event or save migration.

AthleteImpactPresentation reads the observation once per accepted tick. Versioned
athlete_impact_visual_v1 defines a minimum 0.65m/s impulse, bounded torso pitch,
signed shoulder twist/roll, arm spread and 0.8-second recovery. The existing body
root still follows the actual spun world heading. Bone reaction runs before
shooting/equipment/ground/grip corrections. Waiting, shooting, finished state and
backward time clear it. Repeated renders of the same tick cannot restart it.
This is an added visual response, not a new gameplay stun or artificial root spin.

Native tests inspect the original rig before/reaction/recovered, assert a visible
bone rotation and unchanged root, exact recovery, no repeat-trigger and no
shooting reaction. Accepted-impact transaction and journal tests cover the source.

## Qualification

Shooting uses unchanged geometric hit testing, recoil, sway, breath and bolt
controls. Focused baseline audit covers shooting/weapon, overtakes and angular
contacts. Native player/input/range/animation checks and the final full regression
are documented with results in [delivery evidence](../quality/m16-6/traffic-contact/README.md).
No test is disabled, no tolerance/work budget is relaxed, and the M16.6 milestone
remains open for owner testing. The new pass decisions can change future race
positions/times; physical capability and shooting balance are unchanged.
