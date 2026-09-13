# TASK-145 — Prone enter/exit

2026-09-13. WP145 PASS; WP146 next. M11 remains in progress.

Added a separate body pose consumer between locomotion and rifle/hands. Versioned original pose data lowers through crouch/kneel to prone, then reverses within the simulation's exit deadline. Legs use preserved-length IK and planted foot targets; head counters torso pitch. Rifle-ready orientation stays horizontal while the body lowers. Standing observations remain unaffected in this package. No root-motion, camera or shooting-state changes.

Unity full PlayMode **37/37 PASS**, including continuous 200-sample enter/exit, unchanged standing/root, foot/knee clearance, ready rifle direction, and actual prone Aiming deadline assertions in the real-bout integration. Complete Sprint/Pursuit/Mass Start and camera/input regression passed. Native Console **0 errors/0 warnings**, compilation_failed=false. Results/XML retained in `docs/quality/m11/wp145/`.

Visual inspection found and fixed two defects: downward knee bend penetrated the surface during lowering; torso rotation pointed the ready rifle into the ground. New assertions cover both. `prone-first-review.png` is before the rifle correction; `prone-final-sequence.png` and `prone-side.png` are final inspected samples. Fractions0/.5/.7/.85/1 use the generated animation validation scene, rigs spaced1.7 m; fixed camera (6,3,8) toward (0,.6,0), orthographic2.5. Side camera relative to the final rig: (3,1,1) toward (0,.3,.1), orthographic.85.

Limits: simple segmented skinning, original development meshes, pole placement/contact and final cheek/elbow/shoulder alignment remain under WP147/148. Exact authoritative readiness is verified; production animation quality is not claimed by this package. The dedicated target camera remains independent of body motion. Windows rebuild follows integration. OWNER-FEEL-001 remains open; shooting numbers and geometric hit calculation are unchanged.
