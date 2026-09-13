# TASK-147 — Sight, rifle, hands and head alignment

2026-09-13. WP147 PASS. Next WP148 contact; M11 in progress.

The ready rifle follows composed Sight.Ray at 1:1 angular scale while its butt position stays on the stance anchor. Hand IK runs after rifle rotation, with stance-specific elbow planes and a reachable prone support grip. Bounded head/cheek positioning and head direction follow the rifle in the final readiness blend. Pose and grip data remain presentation-only; no new sway, hidden RNG, camera rotation, target scaling or shot resolver input was introduced.

Unity full PlayMode **39/39 PASS**, including both stances at signed sight offsets, axis tolerance0.03 degrees, grip error below2 cm, butt/root preservation, and real-bout integration that compares every Aiming frame against the already-composed Sight (no double sway/handling). Full race/input/camera regression passes. Native Console **0 errors/0 warnings**, compilation_failed=false. Initial reach failures led to correcting the prone butt/grip envelope, without stretching bones or relaxing tolerances.

Evidence `docs/quality/m11/wp147/`: fixed prone/standing at sight(.02,.01) radians; actual neutral-input prone tick2091 and dedicated target camera immediately after a shot; tests/XML and Console. Crosshair/target displacement in that post-shot capture includes existing recoil, while the closed target records the preceding accepted ray. Fixed camera(4,2.6,5) toward(0,.7,.15), orthographic1.5, rigs2 m apart.

OWNER-FEEL-001 presentation review is recorded alongside evidence and linked from the issue. It remains OPEN. The third-person rifle previously lacked the camera's sight motion; this mismatch is corrected. The dedicated camera already followed Sight and is unchanged. Improved third-person posture alone cannot establish appropriate player difficulty. Mandatory WP229/230/232 reviews remain intact.

Limits: original rigid skinning and simple head/grip geometry; no articulated fingers. Head correction is bounded12 cm; it approximates cheek contact on this development asset. Pole/ground contact follows WP148. No final visual approval or new Windows build claim yet.
