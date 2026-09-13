# TASK-142 — Locomotion states and transitions

2026-09-13. WP142 PASS; M11 in progress. Next WP143 cornering.

Implemented seven original procedural poses: idle, flat skating, climb, acceleration, sprint, accepted tuck and technical descent. Selection uses detached speed/grade/effort and accepted simulation flags. Versioned presentation JSON controls pose envelopes, cadence and transition duration. Smooth state and climb-effort blending preserves limb lengths and never moves the placement root. Original prefab includes the driver; the generated validation scene compares all seven states.

Verification: Unity full PlayMode **30/30 passed**, including locomotion selection/continuity/limb/root tests, complete Sprint/Pursuit/Mass Start and input tests. Native Console **0 errors, 0 warnings**, compilation_failed=false. Results/XML, final fixed-camera poses at 0.30/0.65 s and chase ticks 0/400/410 are retained in `docs/quality/m11/wp142/`. Earlier `states-030.png` and `detail-065.png` are before correction; `final-*` captures include corrected elbow direction, tuck hand spacing and preview framing. No tests were disabled.

Full-suite verification exposed focus-dependent synthetic gamepad events in existing device tests. A test-only scope clones/restores InputSettings, routes events to the player buffer explicitly using the installed InputSystem's internal typed Update overload, and restores the original settings object. Focus-loss product behavior is still tested. Package upgrades must recheck that harness binding. Focused input4/4 and subsequent full30/30 passed. A transient Unknown/0 test invocation was not counted. Preview regeneration now handles its already-open generated scene and refuses a dirty scene.

Authoritative simulation, controls, shooting tuning and hit geometry are unchanged. OWNER-FEEL-001 remains OPEN. No new Windows executable is claimed; build refresh follows animation integration.

Limits: original segmented development rig and simple skinning; pole contact and terrain following remain WP148, corner lean WP143, rifle/range transitions WP144–147, fatigue WP149 and measured LOD WP150. Fixed screenshots show floating recovery poles; they do not establish contact acceptance. M12 owns venue/material/rendering quality. Chase framing remains the approved M10 camera.
