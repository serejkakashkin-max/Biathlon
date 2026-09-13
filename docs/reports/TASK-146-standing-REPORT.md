# TASK-146 — Standing stance and transitions

2026-09-13. WP146 PASS. Next WP147 aim/IK; M11 in progress.

Standing setup blends into planted feet, turned hips/torso and a shoulder-level rifle-ready position. The head counters the torso turn; rifle direction and placement root stay stable. Exit reverses the pose inside existing phase deadlines. Data lives in the versioned presentation JSON, with finite values and envelope validation. Prone transitions remain separate; no authoritative timing, aim, camera or hit-geometry changes.

Unity full PlayMode **38/38 PASS**. New standing test checks 200-sample continuity, feet clearance, both reachable grips, shoulder proximity, head/rifle direction and exact return/root invariance. Real-race integration now covers both Sprint shooting bouts and confirms standing/prone posture at actual Aiming deadlines. Complete Sprint/Pursuit/Mass Start and device/camera regression pass. Final Console **0 errors/0 warnings**, compilation_failed=false.

Evidence in `docs/quality/m11/wp146/`: fixed fractions0/.5/.75/1, actual second-bout standing at tick6267, native dedicated-camera capture, JSON and test/XML. The scripted aim1000x1000 policy deliberately aims outside the target bank; the blank target background in `dedicated-camera.png` reflects that extreme sight direction, not a pose-driven camera change. A temporary observer camera inspects the athlete; product cameras stay unchanged. Initial capture sought Aiming while firing every tick (which immediately enters BoltCycle); corrected capture selects BoltCycle and bounds the loop by completion.

Limits: shoulder placement is established, but elbow support, cheek contact and exact sight-axis following belong to WP147. Original rigid skinning and staged poles remain development quality; contact refinement WP148. No Windows rebuild claim yet. OWNER-FEEL-001 remains OPEN; no shooting retuning.
