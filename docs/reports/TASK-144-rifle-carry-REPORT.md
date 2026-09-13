# TASK-144 — Rifle carry and range-ready transfer

2026-09-13. WP144 PASS. Next WP145 prone; M11 in progress.

Added explicit serialized rifle/left-pole/right-pole bindings to the original prefab. A separate presentation consumer keeps the rifle on the back while skiing, reaches with the right hand, swings the prop around the body and brings the support hand onto the fore-end. Exit reverses the transfer. Rifle and pole parents remain stable; each sample restores their placement from typed sockets without name searches or accumulated offsets. Poles stage beside the athlete during handling. Versioned original presentation data controls transfer windows, clearance and grips.

The consumer reads exact setup/exit elapsed and duration ticks; Aiming/BoltCycle remain ready. It cannot delay shots or change phase deadlines. Original range-ready anchor was moved into the arms' reachable envelope. Dedicated shooting camera, authoritative aim, shooting tuning and controls are unchanged.

Unity full PlayMode **36/36 PASS**. New3 tests cover multiple tick-rate durations, transfer/return continuity, prop identity/parent/root invariance, both ready grips, and actual race setup/aim/bolt/exit observations. Full suite includes complete Sprint/Pursuit/Mass Start and input/camera regression. Native Console **0 errors/0 warnings**, compilation_failed=false. Retained initial compile namespace typo was fixed before tests; no failing tests were disabled.

Game View evidence in `docs/quality/m11/wp144/`: five transfer fractions0/.25/.5/.75/1 (review capture), chase tick400 and an actual half-setup at tick2049 under the scripted playtest command policy. A temporary observer camera inspected the body at the range; product cameras were not changed. Tests/XML and Console saved alongside captures.

Limits: body remains upright pending WP145/146; this package does not claim prone pose, shoulder/cheek alignment, final pole contact or skinning quality. Pole planting/staging refinement belongs to WP148. Existing rigid body geometry and schematic venue are development assets. No new Windows executable is claimed yet. OWNER-FEEL-001 remains OPEN for WP229/230/232; no numerical shooting changes.
