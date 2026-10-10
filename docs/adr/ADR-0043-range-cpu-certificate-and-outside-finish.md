# ADR-0043 — Range CPU work and separate outside finish

2026-10-10. Accepted within the owner's M13 correction request. M13 owner review
remains open. This does not change the Master Plan or approve M14.

## Evidence

The new owner Player.log has no exception storm. An actual first-bout Mass30
CPU diagnostic nevertheless reproduces a 320.488 ms simulation tick. Sampling
and per-sweep counters locate expensive rectangle disk-cover evaluation near
the stadium snow shoulder after shooting: one preview performs 59,392 disk
queries. Shooting setup also incorrectly forces distant athletes into full
mesh/pose detail, and the target stage is disabled and re-enabled every tick.

The previous right-hand finish branch crosses the penalty oval. The owner asks
for an independent finish informed by real stadium plans.

## Decision

Before the existing ribbon proof, test the complete occupied rectangle against
one owned convex edge capsule. All four corners must fit the SAME capsule.
Convexity then contains the entire rectangle, including the selected clearance.
The certificate must exceed the continuous motion bound plus the already
selected numerical reserve. It may use the existing soft snow shoulder and
existing course tolerance; their values do not change. A failed certificate
falls through to the existing disk, polygon and refined cover proof. It cannot
combine disconnected capsules, shrink the athlete or grant phase-specific
permission. The versioned ribbon context becomes v10. Expose clearance sample
and disk-query counts for future performance diagnostics without allocations.

Keep the normal distance-based mesh and pose detail for distant shooters. A
phase change still refreshes the pose immediately. The nearby player retains
full detail and the authoritative shooting clock/geometry remain unchanged.
Toggle the target stage only when visibility changes; suppress the chase
camera while the dedicated opaque aiming camera owns the view, restoring its
previous enabled state during setup, recovery and cleanup.

Change the final-lap finish recipe to an outside LEFT branch at main 975.548 m.
The finish is 80 m down the spur, following 63.296 m of straight approach. Its
width is 9 m, with three painted lanes over the last 50 m and a red finish line.
The unobstructed runout is 30 m. Start/finish separation is 217.038 m; the whole
finish/runout centreline stays at least 133.03 m from the penalty centreline.
The earlier laps and shooting/penalty movements retain their routes. Remove
40 tree canopies from the new corridor. Painting observes authored geometry;
it has no colliders and determines no race times.

This adapts a fictional venue using [IBU September 2026 rules, 3.5.1/3.5.1.1](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf)
and the separated stadium areas shown on the [official Lenzerheide plan](https://www.biathlon-lenzerheide.ch/microsites-assets/biathlon-lenzerheide/pdf/infos-besuch/trib%C3%BCne-wm.pdf).
It is not a replica or a claim of full IBU homologation.

## Consequences and verification

The identical bounded Mass30 diagnostic now peaks at 84.864 ms and still
completes all 30 first bouts at tick 6124. This is desktop .NET CPU evidence,
not a rendered Unity FPS guarantee. Remaining hitches require owner testing.

The faster conservative certificate can resolve intervals differently, so
deterministic context/state identities may change. Existing recordings retain
their recorded definitions; v10 recordings require the matching context.
Complete signed Mass3/Sprint1/Pursuit3 replays reproduce all world/resource
events without AI. Final-lap route length decreases by 124.452 m relative to
the previous separate-finish recipe; earlier-lap pacing is unchanged.

The captured snow-shoulder regression, folded/notched/oversized-body cases,
allocation-free warm sweep, all-format finish checks, native shooting/LOD/camera
checks and native full-environment finish-paint checks pass. Native Game View
captures show the independent corridor. No power, difficulty, hidden RNG,
lane-allocation or player steering policy is changed.
