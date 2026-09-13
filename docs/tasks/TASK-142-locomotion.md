# TASK-142 — Locomotion states and pose transitions

PASS. Dependency WP141 PASS (c1fa425), G1 approved. Full Unity PlayMode30/30, Console0 errors; see `docs/reports/TASK-142-locomotion-REPORT.md`.

Objective: make flat, climb, acceleration, sprint, tuck and technical descent visually distinct using read-only animation observations and presentation-only pose data.

Files: locomotion settings JSON, selection/blending model, procedural pose driver, original prefab, PlayMode tests and repeatable pose sampling evidence.

Constraints: no authoritative tuning/control changes; simulation owns position, accepted tuck and final sprint. Pose data is not race tuning. Full terrain contact follows WP148; corner lean follows WP143. Preserve original equipment sockets and replaceable rig contract.

Acceptance: required states selected under the correct inputs, no animation-only tuck while rejected by simulation, smooth pose/effort transitions, stable body/limbs under variable sampling, readable motion through fixed/chase Game View cameras, no simulation writes or compile errors. Test selection, transition continuity and rig limb lengths; run affected PlayMode suite and runtime captures.

Rollback: remove only locomotion consumer/data and restore generated original prefab; preserved core and owner settings are independent.
